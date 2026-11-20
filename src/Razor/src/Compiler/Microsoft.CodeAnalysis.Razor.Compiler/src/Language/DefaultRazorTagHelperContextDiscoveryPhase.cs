// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

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

        if (codeDocument.ParserOptions.LanguageVersion >= RazorLanguageVersion.Version_12_0 &&
            codeDocument.GetComponentAliasTargets() is null &&
            Engine.TryGetFeature(out IComponentAliasTargetFeature? aliasTargetFeature) &&
            aliasTargetFeature.GetAliasTargets(codeDocument, cancellationToken) is { } aliasTargets)
        {
            codeDocument = codeDocument.WithComponentAliasTargets(aliasTargets);
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
        var context = TagHelperDocumentContext.GetOrCreate(tagHelperPrefix, visitor.GetResults(), visitor.GetAliases());
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

        public virtual TagHelperCollection GetResults() => _matches?.ToCollection() ?? [];

        public virtual List<TagHelperAlias>? GetAliases() => null;

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
        /// A map from assembly name to list of <see cref="TagHelperDescriptor"/>. Lists are allocated from and returned to
        /// <see cref="TagHelperCollection.TagHelperDescriptorListPool.Default"/>.
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
                                    currentTagHelpers = TagHelperCollection.TagHelperDescriptorListPool.Default.Get();
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
                TagHelperCollection.TagHelperDescriptorListPool.Default.Return(tagHelpers);
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
            var contributed = IsSourceDocument ? TagHelperCollection.TagHelperDescriptorListPool.Default.Get() : null;
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
                TagHelperCollection.TagHelperDescriptorListPool.Default.Return(contributed);
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

        private readonly HashSet<ReadOnlyMemory<char>> _aliases = new(ReadOnlyMemoryOfCharComparer.Instance);
        private List<TagHelperAlias>? _aliasBindings;
        private ReadOnlyMemory<char> _currentNamespace;
        private ComponentAliasTargetMap? _aliasTargets;
        private bool _supportsComponentAliases;

        // A list of components that don't have a namespace.
        // This list is pooled and is returned in Reset.
        private List<TagHelperDescriptor>? _componentsWithoutNamespace;

        public void Initialize(
            TagHelperCollection tagHelpers,
            string? filePath,
            string? currentNamespace,
            bool supportsComponentAliases,
            ComponentAliasTargetMap? aliasTargets = null,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(!IsInitialized);

            _currentNamespace = currentNamespace.AsMemory();
            _aliasTargets = aliasTargets;
            _supportsComponentAliases = supportsComponentAliases;

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
                    _componentsWithoutNamespace ??= TagHelperCollection.TagHelperDescriptorListPool.Default.Get();
                    _componentsWithoutNamespace.Add(component);
                }
                else
                {
                    if (!_namespaceToComponentsMap.TryGetValue(typeNamespace, out var components))
                    {
                        components = TagHelperCollection.TagHelperDescriptorListPool.Default.Get();
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
            _aliases.Clear();
            _aliasBindings?.Clear();
            _currentNamespace = default;
            _aliasTargets = null;
            _supportsComponentAliases = false;

            if (_componentsWithoutNamespace != null)
            {
                TagHelperCollection.TagHelperDescriptorListPool.Default.Return(_componentsWithoutNamespace);
                _componentsWithoutNamespace = null;
            }

            foreach (var (_, components) in _namespaceToComponentsMap)
            {
                TagHelperCollection.TagHelperDescriptorListPool.Default.Return(components);
            }

            _namespaceToComponentsMap.Clear();

            base.Reset();
        }

        public override TagHelperCollection GetResults()
        {
            var results = base.GetResults();
            if (_aliases.Count == 0)
            {
                return results;
            }

            // Apply alias precedence after visiting all imports, so directive order doesn't matter.
            // An alias also shadows a real namespace with the same name in a qualified component tag.
            // Slice the existing names rather than allocating a first-identifier string per candidate.
            var removed = false;
            foreach (var component in results)
            {
                if (!IsComponentTypeDescriptor(component))
                {
                    continue;
                }

                foreach (var rule in component.TagMatchingRules)
                {
                    var tagName = (rule.ParentTag ?? rule.TagName).AsMemory();
                    var dotIndex = tagName.Span.IndexOf('.');
                    var identifier = dotIndex >= 0 ? tagName[..dotIndex] : tagName;
                    if (_aliases.Contains(identifier))
                    {
                        RemoveMatch(component);
                        removed = true;
                        break;
                    }
                }
            }

            return removed ? base.GetResults() : results;
        }

        public override List<TagHelperAlias>? GetAliases()
            => _aliasBindings;

        protected override void ProcessChunkGenerator(BaseRazorDirectiveSyntax node, ISpanChunkGenerator chunkGenerator)
        {
            switch (chunkGenerator)
            {
                case AddImportChunkGenerator { IsStatic: false } addImport:
                    ProcessAddImport(node, addImport);
                    break;
            }
        }

        private void ProcessAddImport(BaseRazorDirectiveSyntax node, AddImportChunkGenerator addImport)
        {
            // Get the namespace from the using statement.
            var @namespace = addImport.ParsedNamespace;
            var equalsIndex = @namespace.IndexOf('=');
            if (equalsIndex >= 0)
            {
                ProcessAlias(node, @namespace, equalsIndex);
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

        private void ProcessAlias(BaseRazorDirectiveSyntax node, string directive, int equalsIndex)
        {
            // ParsedNamespace has already had whitespace and comments removed by the parser.
            var alias = directive.AsMemory(0, equalsIndex);
            if (alias.Span.StartsWith('@'))
            {
                alias = alias[1..];
            }

            if (alias.IsEmpty)
            {
                RecordDirectiveTagHelperContribution(node, TagHelperCollection.Empty);
                return;
            }

            var aliasName = alias.ToString();

            // From Razor 12, an alias hides ordinary component matches that start with the same identifier.
            if (_supportsComponentAliases)
            {
                _aliases.Add(alias);
            }
            var target = directive.AsMemory(equalsIndex + 1);
            if (_aliasTargets is { } targets)
            {
                using var resolvedContributions = new PooledArrayBuilder<TagHelperDescriptor>();
                if (targets.TryGetTarget(_currentNamespace.ToString(), target.ToString(), out var resolved) &&
                    resolved.Name is { } name)
                {
                    TryAddAliasMatches(name.AsMemory(), directive, aliasName, ref resolvedContributions.AsRef(), typeAlias: resolved.IsType);
                }

                // A missing/error/non-component target is authoritative too: never search an outer scope
                // just because the nearer declaration did not produce component descriptors.
                RecordDirectiveTagHelperContribution(node, TagHelperCollection.Create(resolvedContributions.ToImmutableAndClear()));
                return;
            }

            var isGlobal = target.Span.StartsWith("global::".AsSpan(), StringComparison.Ordinal);
            if (isGlobal)
            {
                target = target["global::".Length..];
            }

            // Incomplete targets occur while typing. An empty type name would otherwise select
            // the namespace-alias path and incorrectly expose every component in that namespace.
            if (target.IsEmpty || target.Span[^1] == '.')
            {
                RecordDirectiveTagHelperContribution(node, TagHelperCollection.Empty);
                return;
            }

            using var contributions = new PooledArrayBuilder<TagHelperDescriptor>();

            // Generated usings are inside the document namespace. Search that namespace and its
            // parents before global scope, reusing a buffer rather than allocating qualified names.
            if (!isGlobal && !_currentNamespace.IsEmpty)
            {
                var firstDot = target.Span.IndexOf('.');
                var firstIdentifierLength = firstDot >= 0 ? firstDot : target.Length;
                var buffer = ArrayPool<char>.Shared.Rent(_currentNamespace.Length + 1 + target.Length);
                try
                {
                    var scope = _currentNamespace;
                    while (!scope.IsEmpty)
                    {
                        scope.Span.CopyTo(buffer);
                        buffer[scope.Length] = '.';
                        target.Span.CopyTo(buffer.AsSpan(scope.Length + 1));
                        var qualifiedTarget = buffer.AsMemory(0, scope.Length + 1 + target.Length);
                        if (TryAddAliasMatches(qualifiedTarget, directive, aliasName, ref contributions.AsRef(),
                            qualifiedTarget[..(scope.Length + 1 + firstIdentifierLength)]))
                        {
                            RecordDirectiveTagHelperContribution(node, TagHelperCollection.Create(contributions.ToImmutableAndClear()));
                            return;
                        }

                        var dot = scope.Span.LastIndexOf('.');
                        scope = dot >= 0 ? scope[..dot] : default;
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }

            TryAddAliasMatches(target, directive, aliasName, ref contributions.AsRef());
            RecordDirectiveTagHelperContribution(node, TagHelperCollection.Create(contributions.ToImmutableAndClear()));
        }

        private bool TryAddAliasMatches(
            ReadOnlyMemory<char> target,
            string directive,
            string alias,
            ref PooledArrayBuilder<TagHelperDescriptor> contributions,
            ReadOnlyMemory<char> firstIdentifier = default,
            bool? typeAlias = null)
        {
            var matched = false;

            // A type alias only needs the descriptors in its declaring namespace. Do not build
            // fully qualified names for every component just to reject almost all of them.
            var lastDot = target.Span.LastIndexOf('.');
            if (typeAlias != false && lastDot >= 0 && _namespaceToComponentsMap.TryGetValue(target[..lastDot], out var typeComponents))
            {
                matched = AddAliasedComponents(typeComponents, alias, target[(lastDot + 1)..], default, ref contributions);
            }
            else if (typeAlias != false && lastDot < 0 && _componentsWithoutNamespace is { } componentsWithoutNamespace)
            {
                matched = AddAliasedComponents(componentsWithoutNamespace, alias, target, default, ref contributions);
            }

            // Namespace aliases include subnamespaces, but unrelated namespaces require no descriptor work.
            if (typeAlias == true)
            {
                return matched;
            }

            foreach (var (typeNamespace, components) in _namespaceToComponentsMap)
            {
                // Once the first identifier resolves in a nearer scope, C# does not retry the
                // remaining path in outer scopes, even if it doesn't name a component here.
                if (!firstIdentifier.IsEmpty && IsNamespaceOrChildNamespace(typeNamespace.Span, firstIdentifier.Span))
                {
                    matched = true;
                }

                if (IsNamespaceOrChildNamespace(typeNamespace.Span, target.Span))
                {
                    AddAliasedComponents(components, alias, default, typeNamespace[target.Length..], ref contributions);
                    matched = true;
                }
            }

            return matched;
        }

        private static bool IsNamespaceOrChildNamespace(ReadOnlySpan<char> typeNamespace, ReadOnlySpan<char> target)
            => typeNamespace.Equals(target, StringComparison.Ordinal) ||
               (typeNamespace.Length > target.Length &&
                typeNamespace.StartsWith(target, StringComparison.Ordinal) &&
                typeNamespace[target.Length] == '.');

        private bool AddAliasedComponents(
            List<TagHelperDescriptor> components,
            string alias,
            ReadOnlyMemory<char> typeName,
            ReadOnlyMemory<char> namespaceSuffix,
            ref PooledArrayBuilder<TagHelperDescriptor> contributions)
        {
            var matched = false;
            // Tag matching rules omit C# identifier escapes even when the alias target needs one.
            var unescapedTypeName = typeName.Span.StartsWith('@') ? typeName[1..] : typeName;
            // A type alias supplies typeName; a namespace alias keeps the namespaceSuffix between
            // the alias and the component name, such as ".Controls" in UI.Controls.Button.
            foreach (var component in components)
            {
                CancellationToken.ThrowIfCancellationRequested();

                // Component-specific bind and child-content descriptors follow the component.
                if (!IsComponentTypeDescriptor(component) ||
                    component.TagMatchingRules is not [var firstRule, ..] ||
                    firstRule.TagName == "*")
                {
                    continue;
                }

                var componentTagName = firstRule.ParentTag ?? firstRule.TagName;
                // Matching names omit arity, so Widget would otherwise also match Widget<T>.
                // Check the type name because bind/child-content metadata doesn't carry IsGeneric.
                if (!typeName.IsEmpty)
                {
                    if (!componentTagName.AsSpan().Equals(unescapedTypeName.Span, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matched = true;
                    if (component.TypeName.AsSpan().IndexOf('<') >= 0)
                    {
                        continue;
                    }
                }

                (_aliasBindings ??= []).Add(new(component, alias, namespaceSuffix.Length, IsTypeAlias: !typeName.IsEmpty));
                matched = true;
                if (IsSourceDocument)
                {
                    contributions.Add(component);
                }
            }

            return matched;
        }

        // HTML bind helpers only classify attributes. Aliases must not rename or shadow <input>, etc.
        private static bool IsComponentTypeDescriptor(TagHelperDescriptor descriptor)
            => descriptor.IsComponentOrChildContentTagHelper() ||
               descriptor is { Kind: TagHelperKind.Bind, ClassifyAttributesOnly: false };

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
