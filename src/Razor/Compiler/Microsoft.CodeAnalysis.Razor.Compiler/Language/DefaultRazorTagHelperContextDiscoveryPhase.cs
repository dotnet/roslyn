// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed partial class DefaultRazorTagHelperContextDiscoveryPhase : RazorEnginePhaseBase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        // The canonical syntax tree is established by DefaultRazorParsingPhase (which runs before this phase).
        var syntaxTree = codeDocument.GetSyntaxTree();
        ThrowForMissingDocumentDependency(syntaxTree);

        if (!codeDocument.TryGetTagHelpers(out var tagHelpers))
        {
            if (!Engine.TryGetFeature(out ITagHelperFeature? tagHelperFeature))
            {
                // No feature, nothing to do.
                return codeDocument;
            }

            tagHelpers = tagHelperFeature.GetTagHelpers(cancellationToken);
        }

        using var _ = GetPooledVisitor(codeDocument, tagHelpers, cancellationToken, out var visitor);

        // We need to find directives in all of the *imports* as well as in the main razor file
        //
        // The imports come logically before the main razor file and are in the order they
        // should be processed.

        if (codeDocument.TryGetImportSyntaxTrees(out var imports))
        {
            foreach (var import in imports)
            {
                visitor.Visit(import);
            }
        }

        visitor.Visit(syntaxTree);

        // This will always be null for a component document.
        var tagHelperPrefix = visitor.TagHelperPrefix;

        var directiveContributions = visitor.GetDirectiveTagHelperContributions();
        var context = TagHelperDocumentContext.GetOrCreate(tagHelperPrefix, visitor.GetResults());
        return codeDocument
            .WithTagHelperContext(context)
            .WithDirectiveTagHelperContributions(directiveContributions);
    }

    internal static ReadOnlyMemory<char> GetMemoryWithoutGlobalPrefix(string s)
    {
        const string globalPrefix = "global::";

        var mem = s.AsMemory();

        if (mem.Span.StartsWith(globalPrefix.AsSpan(), StringComparison.Ordinal))
        {
            return mem[globalPrefix.Length..];
        }

        return mem;
    }

    internal abstract class DirectiveVisitor : SyntaxWalker, IPoolableObject
    {
        private bool _isInitialized;
        private string? _filePath;
        private RazorSourceDocument? _source;
        private CancellationToken _cancellationToken;

        private TagHelperCollection.Builder? _matches;
        private List<DirectiveTagHelperContribution>? _directiveContributions;

        private TagHelperCollection.Builder Matches => _matches ??= [];

        protected bool IsInitialized => _isInitialized;
        protected RazorSourceDocument Source => _source.AssumeNotNull();
        protected CancellationToken CancellationToken => _cancellationToken;

        public virtual string? TagHelperPrefix => null;

        // True when visiting the source document itself (not imports).
        [MemberNotNullWhen(true, nameof(_filePath), nameof(_source))]
        protected bool IsSourceDocument
            => _filePath is string filePath &&
               _source?.FilePath is string sourceFilePath &&
               filePath == sourceFilePath;

        public void Visit(RazorSyntaxTree tree)
        {
            _source = tree.Source;
            Visit(tree.Root);
        }

        public TagHelperCollection GetResults() => _matches?.ToCollection() ?? [];

        public ImmutableArray<DirectiveTagHelperContribution> GetDirectiveTagHelperContributions()
            => _directiveContributions is { Count: > 0 }
                ? [.. _directiveContributions]
                : [];

        protected void Initialize(string? filePath, CancellationToken cancellationToken)
        {
            _filePath = filePath;
            _cancellationToken = cancellationToken;
            _isInitialized = true;
        }

        public virtual void Reset()
        {
            if (_matches is { } matches)
            {
                matches.Dispose();
                _matches = null;
            }

            _filePath = null;
            _source = null;
            _cancellationToken = default;
            _directiveContributions = null;
            _isInitialized = false;
        }

        protected void RecordDirectiveTagHelperContribution(BaseRazorDirectiveSyntax directive, TagHelperCollection contributedTagHelpers)
        {
            // Only track directive usage in source documents, not imports.
            if (IsSourceDocument)
            {
                _directiveContributions ??= [];
                _directiveContributions.Add(new(directive.SpanStart, contributedTagHelpers));
            }
        }

        protected void AddMatch(TagHelperDescriptor tagHelper)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            Matches.Add(tagHelper);
        }

        protected void AddMatches(List<TagHelperDescriptor> tagHelpers)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var tagHelper in tagHelpers)
            {
                Matches.Add(tagHelper);
            }
        }

        protected void RemoveMatch(TagHelperDescriptor tagHelper)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            Matches.Remove(tagHelper);
        }

        protected void RemoveMatches(List<TagHelperDescriptor> tagHelpers)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var tagHelper in tagHelpers)
            {
                Matches.Remove(tagHelper);
            }
        }

        protected abstract void ProcessChunkGenerator(BaseRazorDirectiveSyntax node, ISpanChunkGenerator chunkGenerator);

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            VisitDirective(node);
        }

        public override void VisitRazorUsingDirective(RazorUsingDirectiveSyntax node)
        {
            VisitDirective(node);
        }

        private void VisitDirective(BaseRazorDirectiveSyntax node)
        {
            foreach (var child in node.DescendantNodes())
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (child is CSharpStatementLiteralSyntax { ChunkGenerator: { } chunkGenerator })
                {
                    ProcessChunkGenerator(node, chunkGenerator);
                }
            }
        }
    }

    internal sealed class TagHelperDirectiveVisitor : DirectiveVisitor
    {
        /// <summary>
        /// A larger pool of <see cref="TagHelperDescriptor"/> lists to handle scenarios where tag helpers
        /// originate from a large number of assemblies.
        /// </summary>
        private static readonly ListPool<TagHelperDescriptor> s_pool = ListPool<TagHelperDescriptor>.Create(poolSize: 100);

        /// <summary>
        /// A map from assembly name to list of <see cref="TagHelperDescriptor"/>. Lists are allocated from and returned to
        /// <see cref="s_pool"/>.
        /// </summary>
        private readonly Dictionary<string, List<TagHelperDescriptor>> _tagHelperMap = new(StringComparer.Ordinal);

        private TagHelperCollection? _tagHelpers;
        private bool _tagHelperMapComputed;
        private string? _tagHelperPrefix;

        public override string? TagHelperPrefix => _tagHelperPrefix;

        private Dictionary<string, List<TagHelperDescriptor>> TagHelperMap
        {
            get
            {
                if (!_tagHelperMapComputed)
                {
                    ComputeTagHelperMap();

                    _tagHelperMapComputed = true;
                }

                return _tagHelperMap;

                void ComputeTagHelperMap()
                {
                    var tagHelpers = _tagHelpers.AssumeNotNull();

                    string? currentAssemblyName = null;
                    List<TagHelperDescriptor>? currentTagHelpers = null;

                    // We don't want to consider components in a view document.
                    foreach (var tagHelper in tagHelpers)
                    {
                        if (!tagHelper.IsAnyComponentDocumentTagHelper())
                        {
                            if (tagHelper.AssemblyName != currentAssemblyName)
                            {
                                currentAssemblyName = tagHelper.AssemblyName;

                                if (!_tagHelperMap.TryGetValue(currentAssemblyName, out currentTagHelpers))
                                {
                                    currentTagHelpers = s_pool.Get();
                                    _tagHelperMap.Add(currentAssemblyName, currentTagHelpers);
                                }
                            }

                            currentTagHelpers!.Add(tagHelper);
                        }
                    }
                }
            }
        }

        public void Initialize(
            TagHelperCollection tagHelpers,
            string? filePath,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(!IsInitialized);

            _tagHelpers = tagHelpers;

            base.Initialize(filePath, cancellationToken);
        }

        public override void Reset()
        {
            foreach (var (_, tagHelpers) in _tagHelperMap)
            {
                s_pool.Return(tagHelpers);
            }

            _tagHelperMap.Clear();
            _tagHelperMapComputed = false;
            _tagHelpers = null;
            _tagHelperPrefix = null;

            base.Reset();
        }

        protected override void ProcessChunkGenerator(BaseRazorDirectiveSyntax node, ISpanChunkGenerator chunkGenerator)
        {
            switch (chunkGenerator)
            {
                case AddTagHelperChunkGenerator addTagHelper:
                    HandleAddTagHelper(node, addTagHelper);
                    break;
                case RemoveTagHelperChunkGenerator removeTagHelper:
                    HandleRemoveTagHelper(removeTagHelper);
                    break;
                case TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix:
                    HandleTagHelperPrefix(tagHelperPrefix);
                    break;
            }
        }

        private void HandleAddTagHelper(BaseRazorDirectiveSyntax node, AddTagHelperChunkGenerator addTagHelper)
        {
            if (addTagHelper.AssemblyName == null)
            {
                // Skip this one, it's an error
                return;
            }

            if (!TagHelperMap.TryGetValue(addTagHelper.AssemblyName, out var tagHelpers))
            {
                // No tag helpers in the assembly.
                return;
            }

            // We only record directives if we're visiting the source document, so no point collecting them either.
            var contributed = IsSourceDocument ? ListPool<TagHelperDescriptor>.Default.Get() : null;
            switch (GetMemoryWithoutGlobalPrefix(addTagHelper.TypePattern).Span)
            {
                case ['*']:
                    AddMatches(tagHelpers);
                    contributed?.AddRange(tagHelpers);
                    break;

                case [.. var pattern, '*']:
                    foreach (var tagHelper in tagHelpers)
                    {
                        if (tagHelper.Name.AsSpan().StartsWith(pattern, StringComparison.Ordinal))
                        {
                            AddMatch(tagHelper);
                            contributed?.Add(tagHelper);
                        }
                    }

                    break;

                case var pattern:
                    foreach (var tagHelper in tagHelpers)
                    {
                        if (tagHelper.Name.AsSpan().Equals(pattern, StringComparison.Ordinal))
                        {
                            AddMatch(tagHelper);
                            contributed?.Add(tagHelper);
                        }
                    }

                    break;
            }

            if (contributed is not null)
            {
                RecordDirectiveTagHelperContribution(node, TagHelperCollection.Create(contributed));
                ListPool<TagHelperDescriptor>.Default.Return(contributed);
            }
        }

        private void HandleRemoveTagHelper(RemoveTagHelperChunkGenerator removeTagHelper)
        {
            if (removeTagHelper.AssemblyName == null)
            {
                // Skip this one, it's an error
                return;
            }

            if (!TagHelperMap.TryGetValue(removeTagHelper.AssemblyName, out var nonComponentTagHelpers))
            {
                // No tag helpers in the assembly.
                return;
            }

            switch (GetMemoryWithoutGlobalPrefix(removeTagHelper.TypePattern).Span)
            {
                case ['*']:
                    RemoveMatches(nonComponentTagHelpers);
                    break;

                case [.. var pattern, '*']:
                    foreach (var tagHelper in nonComponentTagHelpers)
                    {
                        if (tagHelper.Name.AsSpan().StartsWith(pattern, StringComparison.Ordinal))
                        {
                            RemoveMatch(tagHelper);
                        }
                    }

                    break;

                case var pattern:
                    foreach (var tagHelper in nonComponentTagHelpers)
                    {
                        if (tagHelper.Name.AsSpan().Equals(pattern, StringComparison.Ordinal))
                        {
                            RemoveMatch(tagHelper);
                        }
                    }

                    break;
            }
        }

        private void HandleTagHelperPrefix(TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix)
        {
            if (!tagHelperPrefix.DirectiveText.IsNullOrEmpty())
            {
                // We only expect to see a single one of these per file, but that's enforced at another level.
                _tagHelperPrefix = tagHelperPrefix.DirectiveText;
            }
        }
    }

    internal sealed class ComponentDirectiveVisitor : DirectiveVisitor
    {
        // A map of namespaces to the list of components declared in that namespace.
        // The list values in this dictionary are pooled and are returned in Reset.
        private readonly Dictionary<ReadOnlyMemory<char>, List<TagHelperDescriptor>> _namespaceToComponentsMap = new(ReadOnlyMemoryOfCharComparer.Instance);

        // A list of components that don't have a namespace.
        // This list is pooled and is returned in Reset.
        private List<TagHelperDescriptor>? _componentsWithoutNamespace;

        public void Initialize(
            TagHelperCollection tagHelpers,
            string? filePath,
            string? currentNamespace,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(!IsInitialized);

            foreach (var component in tagHelpers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // We don't want to consider legacy tag helpers in a component document.
                if (!component.IsAnyComponentDocumentTagHelper() || IsTagHelperFromMangledClass(component))
                {
                    continue;
                }

                if (component.IsFullyQualifiedNameMatch)
                {
                    // If the component matches for a fully qualified name, using directives shouldn't matter.
                    AddMatch(component);
                    continue;
                }

                var typeNamespace = component.TypeNamespace.AsMemory();

                if (typeNamespace.IsEmpty)
                {
                    _componentsWithoutNamespace ??= ListPool<TagHelperDescriptor>.Default.Get();
                    _componentsWithoutNamespace.Add(component);
                }
                else
                {
                    if (!_namespaceToComponentsMap.TryGetValue(typeNamespace, out var components))
                    {
                        components = ListPool<TagHelperDescriptor>.Default.Get();
                        _namespaceToComponentsMap.Add(typeNamespace, components);
                    }

                    components.Add(component);
                }

                if (currentNamespace is not null && IsTypeNamespaceInScope(typeNamespace.Span, currentNamespace))
                {
                    // If the type is already in scope of the document's namespace, using isn't necessary.
                    AddMatch(component);
                }
            }

            base.Initialize(filePath, cancellationToken);
        }

        public override void Reset()
        {
            if (_componentsWithoutNamespace != null)
            {
                ListPool<TagHelperDescriptor>.Default.Return(_componentsWithoutNamespace);
                _componentsWithoutNamespace = null;
            }

            foreach (var (_, components) in _namespaceToComponentsMap)
            {
                ListPool<TagHelperDescriptor>.Default.Return(components);
            }

            _namespaceToComponentsMap.Clear();

            base.Reset();
        }

        protected override void ProcessChunkGenerator(BaseRazorDirectiveSyntax node, ISpanChunkGenerator chunkGenerator)
        {
            switch (chunkGenerator)
            {
                case AddTagHelperChunkGenerator addTagHelper:
                    ProcessAddTagHelper(node, addTagHelper);
                    break;
                case RemoveTagHelperChunkGenerator removeTagHelper:
                    ProcessRemoveTagHelper(node, removeTagHelper);
                    break;
                case TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix:
                    ProcessTagHelperPrefix(node, tagHelperPrefix);
                    break;
                case AddImportChunkGenerator { IsStatic: false } addImport:
                    ProcessAddImport(node, addImport);
                    break;
            }
        }

        private void ProcessAddTagHelper(BaseRazorDirectiveSyntax node, AddTagHelperChunkGenerator addTagHelper)
        {
            // We only add diagnostics if we're visiting the source document, not an import
            if (IsSourceDocument)
            {
                addTagHelper.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
            }
        }

        private void ProcessRemoveTagHelper(BaseRazorDirectiveSyntax node, RemoveTagHelperChunkGenerator removeTagHelper)
        {
            // We only add diagnostics if we're visiting the source document, not an import
            if (IsSourceDocument)
            {
                removeTagHelper.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
            }
        }

        private void ProcessTagHelperPrefix(BaseRazorDirectiveSyntax node, TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix)
        {
            // We only add diagnostics if we're visiting the source document, not an import
            if (IsSourceDocument)
            {
                tagHelperPrefix.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
            }
        }

        private void ProcessAddImport(BaseRazorDirectiveSyntax node, AddImportChunkGenerator addImport)
        {
            // Get the namespace from the using statement.
            var @namespace = addImport.ParsedNamespace;
            if (@namespace.Contains('='))
            {
                // We don't support usings with alias.
                return;
            }

            if (_namespaceToComponentsMap.Count == 0 && _componentsWithoutNamespace is null or { Count: 0 })
            {
                // There aren't any non-qualified components to add.
                RecordDirectiveTagHelperContribution(node, TagHelperCollection.Empty);
                return;
            }

            var contributedTagHelpers = TagHelperCollection.Empty;

            if (_componentsWithoutNamespace is { Count: > 0 } componentsWithoutNamespace)
            {
                // Add all tag helpers that have an empty type namespace
                AddMatches(componentsWithoutNamespace);
            }

            if (_namespaceToComponentsMap is { Count: > 0 } namespaceToComponentsMap)
            {
                // Remove global:: prefix from namespace.
                var normalizedNamespace = GetMemoryWithoutGlobalPrefix(@namespace);

                // Add all tag helpers with a matching namespace
                if (namespaceToComponentsMap.TryGetValue(normalizedNamespace, out var components))
                {
                    AddMatches(components);
                    if (IsSourceDocument)
                    {
                        contributedTagHelpers = TagHelperCollection.Create(components);
                    }
                }
            }

            RecordDirectiveTagHelperContribution(node, contributedTagHelpers);
        }

        // Check if a type's namespace is already in scope given the namespace of the current document.
        // E.g,
        // If the namespace of the document is `MyComponents.Components.Shared`,
        // then the types `MyComponents.FooComponent`, `MyComponents.Components.BarComponent`, `MyComponents.Components.Shared.BazComponent` are all in scope.
        // Whereas `MyComponents.SomethingElse.OtherComponent` is not in scope.
        internal static bool IsTypeNamespaceInScope(ReadOnlySpan<char> typeNamespace, string @namespace)
        {
            if (typeNamespace.IsEmpty)
            {
                // Either the typeName is not the full type name or this type is at the top level.
                return true;
            }

            if (!@namespace.StartsWith(typeNamespace, StringComparison.Ordinal))
            {
                // typeName: MyComponents.Shared.SomeCoolNamespace
                // currentNamespace: MyComponents.Shared
                return false;
            }

            if (typeNamespace.Length > @namespace.Length && typeNamespace[@namespace.Length] != '.')
            {
                // typeName: MyComponents.SharedFoo
                // currentNamespace: MyComponent.Shared
                return false;
            }

            return true;
        }

        // We need to filter out the duplicate tag helper descriptors that come from the
        // open file in the editor. We mangle the class name for its generated code, so using that here to filter these out.
        internal static bool IsTagHelperFromMangledClass(TagHelperDescriptor tagHelper)
        {
            return ComponentHelpers.IsMangledClass(tagHelper.TypeNameIdentifier);
        }
    }
}
