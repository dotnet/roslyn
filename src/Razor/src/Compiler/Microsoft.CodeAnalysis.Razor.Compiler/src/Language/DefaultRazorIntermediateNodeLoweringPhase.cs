// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Converts the Razor syntax tree into intermediate representation (IR) nodes. Runs before
/// <see cref="TagHelperResolutionPhase"/>, so elements that might be tag helpers are represented
/// as unresolved nodes (<see cref="UnresolvedElementIntermediateNode"/>,
/// <see cref="UnresolvedAttributeIntermediateNode"/>).
/// </summary>
/// <remarks>
/// Pre-computes fallback forms on unresolved nodes so the resolution phase can resolve them
/// without accessing the syntax tree. Three visitor subclasses handle different file kinds:
/// <see cref="LegacyFileKindVisitor"/> (.cshtml), <see cref="ComponentFileKindVisitor"/> (.razor),
/// and <see cref="ComponentImportFileKindVisitor"/> (_Imports.razor).
/// </remarks>
internal class DefaultRazorIntermediateNodeLoweringPhase : RazorEnginePhaseBase, IRazorIntermediateNodeLoweringPhase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        // The canonical syntax tree is established by DefaultRazorTagHelperContextDiscoveryPhase,
        // which always runs before this phase in the pipeline.
        var syntaxTree = codeDocument.GetSyntaxTree();
        ThrowForMissingDocumentDependency(syntaxTree);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        documentNode.Options = codeDocument.CodeGenerationOptions;

        // The import documents should be inserted logically before the main document.
        var imports = codeDocument.GetImportSyntaxTrees();
        var importedUsings = !imports.IsEmpty
            ? ImportDirectives(documentNode, builder, syntaxTree.Options, imports)
            : [];

        // Lower the main document, appending after the imported directives.
        //
        // We need to decide up front if this document is a "component" file. This will affect how
        // lowering behaves.
        LoweringVisitor visitor;
        if (codeDocument.FileKind.IsComponentImport() &&
            syntaxTree.Options.AllowComponentFileKind)
        {
            visitor = new ComponentImportFileKindVisitor(documentNode, builder, syntaxTree.Options)
            {
                SourceDocument = syntaxTree.Source,
            };

            visitor.Visit(syntaxTree.Root);
        }
        else if (codeDocument.FileKind.IsComponent() &&
            syntaxTree.Options.AllowComponentFileKind)
        {
            visitor = new ComponentFileKindVisitor(documentNode, builder, syntaxTree.Options)
            {
                SourceDocument = syntaxTree.Source,
            };

            visitor.Visit(syntaxTree.Root);
        }
        else
        {
            visitor = new LegacyFileKindVisitor(documentNode, builder, syntaxTree.Options)
            {
                SourceDocument = syntaxTree.Source,
            };

            visitor.Visit(syntaxTree.Root);
        }

        // 1. Prioritize non-imported usings over imported ones.
        // 2. Don't import usings that already exist in primary document.
        // 3. Allow duplicate usings in primary document (C# warning).
        using var _ = ListPool<UsingReference>.GetPooledObject(out var usingReferences);
        usingReferences.AddRange(visitor.Usings);

        for (var j = importedUsings.Count - 1; j >= 0; j--)
        {
            var importedUsing = importedUsings[j];
            if (!usingReferences.Contains(importedUsing) &&
                // If the using is from the default import, avoid adding it
                // if a user using exists which is the same except for the `global::` prefix.
                (!TryRemoveGlobalPrefixFromDefaultUsing(in importedUsing, out var trimmedUsingNamespace) ||
                !Contains(usingReferences, trimmedUsingNamespace)))
            {
                usingReferences.Insert(0, importedUsing);
            }
        }

        // In each lowering piece above, namespaces were tracked. We render them here to ensure every
        // lowering action has a chance to add a source location to a namespace. Ultimately, closest wins.
        var index = 0;

        UsingDirectiveIntermediateNode lastDirective = null;
        foreach (var reference in usingReferences)
        {
            var @using = new UsingDirectiveIntermediateNode()
            {
                Content = reference.Namespace,
                Source = reference.Source,
                HasExplicitSemicolon = reference.HasExplicitSemicolon
            };

            builder.Insert(index++, @using);

            lastDirective = @using;
        }

        if (lastDirective is not null)
        {
            // Using directives can be emitted without "#line hidden" regions between them, to allow Roslyn to add
            // new directives as necessary, but we want to append one on the last using, so things go back to
            // normal for whatever comes next.
            lastDirective.AppendLineDefaultAndHidden = true;
        }

        PostProcessImportedDirectives(documentNode);

        // The document should contain all errors that currently exist in the system. This involves
        // adding the errors from the primary and imported syntax trees.
        foreach (var diagnostic in syntaxTree.Diagnostics)
        {
            documentNode.AddDiagnostic(diagnostic);
        }

        foreach (var import in imports)
        {
            foreach (var diagnostic in import.Diagnostics)
            {
                documentNode.AddDiagnostic(diagnostic);
            }
        }

        return codeDocument.WithDocumentNode(documentNode);

        static bool TryRemoveGlobalPrefixFromDefaultUsing(in UsingReference usingReference, out ReadOnlySpan<char> trimmedNamespace)
        {
            const string globalPrefix = "global::";
            if (usingReference.Source is { FilePath: null } && // the default import has null file path
                usingReference.Namespace.StartsWith(globalPrefix, StringComparison.Ordinal))
            {
                trimmedNamespace = usingReference.Namespace.AsSpan()[globalPrefix.Length..];
                return true;
            }
            trimmedNamespace = default;
            return false;
        }

        static bool Contains(List<UsingReference> usingReferences, ReadOnlySpan<char> usingNamespace)
        {
            foreach (var usingReference in usingReferences)
            {
                if (usingReference.Equals(usingNamespace))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Determines whether a tag name looks like it could be a component name (starts with uppercase).
    /// </summary>
    internal static bool LooksLikeAComponentName(DocumentIntermediateNode document, string startTagName)
    {
        var category = char.GetUnicodeCategory(startTagName, 0);

        return category is System.Globalization.UnicodeCategory.UppercaseLetter ||
            (document.Options.SupportLocalizedComponentNames &&
                (category is System.Globalization.UnicodeCategory.TitlecaseLetter or System.Globalization.UnicodeCategory.OtherLetter));
    }

    private static IReadOnlyList<UsingReference> ImportDirectives(
        DocumentIntermediateNode document,
        IntermediateNodeBuilder builder,
        RazorParserOptions options,
        ImmutableArray<RazorSyntaxTree> imports)
    {
        Debug.Assert(!imports.IsDefaultOrEmpty);

        var importsVisitor = new ImportsVisitor(document, builder, options);
        foreach (var import in imports)
        {
            importsVisitor.SourceDocument = import.Source;
            importsVisitor.Visit(import.Root);
        }

        return importsVisitor.Usings;
    }

    private static void PostProcessImportedDirectives(DocumentIntermediateNode document)
    {
        using var _ = SpecializedPools.GetPooledReferenceEqualityHashSet<DirectiveDescriptor>(out var seenDirectives);
        var references = document.FindDescendantReferences<DirectiveIntermediateNode>();

        for (var i = references.Length - 1; i >= 0; i--)
        {
            var reference = references[i];
            var directive = reference.Node;
            var descriptor = directive.Directive;
            var seenDirective = !seenDirectives.Add(descriptor);

            if (!directive.IsImported)
            {
                continue;
            }

            switch (descriptor.Kind)
            {
                case DirectiveKind.SingleLine:
                    {
                        if (seenDirective && descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                        {
                            // This directive has been overridden, it should be removed from the document.
                            break;
                        }

                        continue;
                    }

                case DirectiveKind.RazorBlock:
                case DirectiveKind.CodeBlock:
                    {
                        if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                        {
                            // A block directive cannot be imported.
                            document.AddDiagnostic(
                                RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported(descriptor.Directive));
                        }

                        break;
                    }

                default:
                    throw new InvalidOperationException(Resources.FormatUnexpectedDirectiveKind(typeof(DirectiveKind).FullName));
            }

            // Overridden and invalid imported directives make it to here. They should be removed from the document.

            reference.Remove();
        }
    }

    private struct UsingReference : IEquatable<UsingReference>
    {
        public UsingReference(string @namespace, SourceSpan? source, bool hasExplicitSemicolon)
        {
            Namespace = @namespace;
            Source = source;
            HasExplicitSemicolon = hasExplicitSemicolon;
        }
        public string Namespace { get; }

        public SourceSpan? Source { get; }

        public bool HasExplicitSemicolon { get; }

        public override bool Equals(object other)
        {
            if (other is UsingReference reference)
            {
                return Equals(reference);
            }

            return false;
        }
        public bool Equals(UsingReference other)
        {
            return string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);
        }

        public readonly bool Equals(ReadOnlySpan<char> otherNamespace)
        {
            return Namespace.AsSpan().Equals(otherNamespace, StringComparison.Ordinal);
        }

        public override int GetHashCode() => Namespace.GetHashCode();
    }

    /// <summary>
    /// Base visitor implementing shared lowering logic for all file kinds.
    /// Subclasses (<see cref="LegacyFileKindVisitor"/>, <see cref="ComponentFileKindVisitor"/>,
    /// <see cref="ComponentImportFileKindVisitor"/>) override element/attribute visitors to
    /// handle file-kind-specific semantics. Contains shared attribute value visitors, directive
    /// handling, and helper methods for source span computation.
    /// </summary>
    private class LoweringVisitor : SyntaxWalker
    {
        protected readonly IntermediateNodeBuilder _builder;
        protected readonly DocumentIntermediateNode _document;
        protected readonly List<UsingReference> _usings;
        protected readonly RazorParserOptions _options;
        /// <summary>
        /// True when currently lowering children of a unresolved attribute. Controls whether value
        /// visitors (e.g. <c>VisitCSharpExpressionLiteral</c>, <c>VisitMarkupTextLiteral</c>) produce
        /// unresolved-specific IR nodes. Set unconditionally (not just when true) to ensure correct
        /// reset between consecutive attributes.
        /// </summary>
        protected bool _insideUnresolvedAttribute;

        public LoweringVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, RazorParserOptions options)
        {
            _document = document;
            _builder = builder;
            _usings = new List<UsingReference>();
            _options = options;
        }

        public IReadOnlyList<UsingReference> Usings => _usings;

        public RazorSourceDocument SourceDocument { get; set; }

        public override void VisitRazorUsingDirective(RazorUsingDirectiveSyntax node)
        {
            VisitDirective(node, descriptor: null);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            VisitDirective(node, node.DirectiveDescriptor);
        }

        private void VisitDirective(BaseRazorDirectiveSyntax node, DirectiveDescriptor descriptor)
        {
            IntermediateNode directiveNode;

            if (descriptor != null)
            {
                var diagnostics = node.GetDiagnostics();

                // This is an extensible directive.
                if (IsMalformed(diagnostics))
                {
                    directiveNode = new MalformedDirectiveIntermediateNode()
                    {
                        DirectiveName = descriptor.Directive,
                        Directive = descriptor,
                        Source = BuildSourceSpanFromNode(node),
                    };
                }
                else
                {
                    directiveNode = new DirectiveIntermediateNode()
                    {
                        DirectiveName = descriptor.Directive,
                        Directive = descriptor,
                        Source = BuildSourceSpanFromNode(node),
                    };
                }

                for (var i = 0; i < diagnostics.Length; i++)
                {
                    directiveNode.AddDiagnostic(diagnostics[i]);
                }

                _builder.Push(directiveNode);
            }

            Visit(node.Body);

            if (descriptor != null)
            {
                _builder.Pop();
            }
        }

        public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            switch (node.ChunkGenerator)
            {
                case null:
                    base.VisitCSharpStatementLiteral(node);
                    return;
                case DirectiveTokenChunkGenerator tokenChunkGenerator:
                    _builder.Add(new DirectiveTokenIntermediateNode()
                    {
                        Content = node.GetContent(),
                        DirectiveToken = tokenChunkGenerator.Descriptor,
                        Source = BuildSourceSpanFromNode(node),
                    });
                    break;
                case AddImportChunkGenerator importChunkGenerator:
                    var namespaceImport = importChunkGenerator.Namespace.Trim();
                    var namespaceSpan = BuildSourceSpanFromNode(node);
                    _usings.Add(new UsingReference(namespaceImport, namespaceSpan, importChunkGenerator.HasExplicitSemicolon));
                    break;
                case AddTagHelperChunkGenerator addTagHelperChunkGenerator:
                    {
                        IntermediateNode directiveNode;
                        if (IsMalformed(addTagHelperChunkGenerator.Diagnostics))
                        {
                            directiveNode = new MalformedDirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }
                        else
                        {
                            directiveNode = new DirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }

                        for (var i = 0; i < addTagHelperChunkGenerator.Diagnostics.Count; i++)
                        {
                            directiveNode.AddDiagnostic(addTagHelperChunkGenerator.Diagnostics[i]);
                        }

                        _builder.Push(directiveNode);

                        _builder.Add(new DirectiveTokenIntermediateNode()
                        {
                            Content = addTagHelperChunkGenerator.LookupText,
                            DirectiveToken = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Tokens[0],
                            Source = BuildSourceSpanFromNode(node),
                        });

                        _builder.Pop();
                        break;
                    }
                case RemoveTagHelperChunkGenerator removeTagHelperChunkGenerator:
                    {
                        IntermediateNode directiveNode;
                        if (IsMalformed(removeTagHelperChunkGenerator.Diagnostics))
                        {
                            directiveNode = new MalformedDirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }
                        else
                        {
                            directiveNode = new DirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }

                        for (var i = 0; i < removeTagHelperChunkGenerator.Diagnostics.Count; i++)
                        {
                            directiveNode.AddDiagnostic(removeTagHelperChunkGenerator.Diagnostics[i]);
                        }

                        _builder.Push(directiveNode);

                        _builder.Add(new DirectiveTokenIntermediateNode()
                        {
                            Content = removeTagHelperChunkGenerator.LookupText,
                            DirectiveToken = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Tokens[0],
                            Source = BuildSourceSpanFromNode(node),
                        });

                        _builder.Pop();
                        break;
                    }
                case TagHelperPrefixDirectiveChunkGenerator tagHelperPrefixChunkGenerator:
                    {
                        IntermediateNode directiveNode;
                        if (IsMalformed(tagHelperPrefixChunkGenerator.Diagnostics))
                        {
                            directiveNode = new MalformedDirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }
                        else
                        {
                            directiveNode = new DirectiveIntermediateNode()
                            {
                                DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                                Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                                Source = BuildSourceSpanFromNode(node),
                            };
                        }

                        for (var i = 0; i < tagHelperPrefixChunkGenerator.Diagnostics.Count; i++)
                        {
                            directiveNode.AddDiagnostic(tagHelperPrefixChunkGenerator.Diagnostics[i]);
                        }

                        _builder.Push(directiveNode);

                        _builder.Add(new DirectiveTokenIntermediateNode()
                        {
                            Content = tagHelperPrefixChunkGenerator.Prefix,
                            DirectiveToken = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Tokens[0],
                            Source = BuildSourceSpanFromNode(node),
                        });

                        _builder.Pop();
                        break;
                    }
            }

            base.VisitCSharpStatementLiteral(node);
        }

        protected SourceSpan? BuildSourceSpanFromNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            return node.GetSourceSpan(SourceDocument);
        }

        protected static AttributeStructure InferAttributeStructure(MarkupAttributeBlockSyntax node)
        {
            if (node.EqualsToken.Kind == SyntaxKind.None && node.Value == null)
            {
                return AttributeStructure.Minimized;
            }

            var lastToken = node.GetLastToken();
            if (lastToken.Kind != SyntaxKind.None)
            {
                if (lastToken.Content == "\"")
                {
                    return AttributeStructure.DoubleQuotes;
                }

                if (lastToken.Content == "'")
                {
                    return AttributeStructure.SingleQuotes;
                }
            }

            // Has an equals sign but no value/quotes (e.g. `type=`).
            // Treated as DoubleQuotes by convention.
            return AttributeStructure.DoubleQuotes;
        }

        protected static SyntaxTokenList MergeTokenLists(
            SyntaxTokenList? literal1,
            SyntaxTokenList? literal2,
            SyntaxTokenList? literal3 = null,
            SyntaxTokenList? literal4 = null,
            SyntaxTokenList? literal5 = null)
        {
            using var _ = ArrayPool<SyntaxTokenList>.Shared.GetPooledArraySpan(5, out var tokenLists);
            var tokenListsCount = 0;
            var count = 0;

            if (literal1 is { } tokens1)
            {
                tokenLists[tokenListsCount++] = tokens1;
                count += tokens1.Count;
            }

            if (literal2 is { } tokens2)
            {
                tokenLists[tokenListsCount++] = tokens2;
                count += tokens2.Count;
            }

            if (literal3 is { } tokens3)
            {
                tokenLists[tokenListsCount++] = tokens3;
                count += tokens3.Count;
            }

            if (literal4 is { } tokens4)
            {
                tokenLists[tokenListsCount++] = tokens4;
                count += tokens4.Count;
            }

            if (literal5 is { } tokens5)
            {
                tokenLists[tokenListsCount++] = tokens5;
                count += tokens5.Count;
            }

            if (count == 0)
            {
                return default;
            }

            using var builder = new PooledArrayBuilder<SyntaxToken>(count);

            foreach (var tokenList in tokenLists[..tokenListsCount])
            {
                builder.AddRange(tokenList);
            }

            return builder.ToList();
        }

        /// <summary>
        ///  Simple helper struct to simplify calling code that needs to skip elements
        ///  without resorting to LINQ.
        /// </summary>
        protected readonly struct ChildNodesHelper(ChildSyntaxList list, int start = 0)
        {
            public int Count { get; } = Math.Max(list.Count - start, 0);

            public SyntaxNodeOrToken this[int index] => list[start + index];

            public ChildNodesHelper Skip(int count)
            {
                return new ChildNodesHelper(list, start + count);
            }

            public SyntaxNodeOrToken FirstOrDefault() => Count > 0 ? this[0] : default;

            public bool TryCast<TNode>(out ImmutableArray<TNode> result)
            {
                // Note that this intentionally returns true for empty lists.
                // This behavior matches the expectations of code that previously called
                // ".All(x => x is TNode)" followed by ".Cast<TNode>()" via LINQ.
                // Because "All" would return true for empty lists, this method
                // needs to do the same.

                using var builder = new PooledArrayBuilder<TNode>(Count);

                for (var i = start; i < list.Count; i++)
                {
                    if (list[i].AsNode() is not TNode node)
                    {
                        result = default;
                        return false;
                    }

                    builder.Add(node);
                }

                result = builder.ToImmutableAndClear();
                return true;
            }
        }

        protected static MarkupTextLiteralSyntax MergeAttributeValue(MarkupLiteralAttributeValueSyntax node)
        {
            var valueTokens = MergeTokenLists(
                node.Prefix?.LiteralTokens,
                node.Value?.LiteralTokens);

            var rewritten = node.Prefix?.Update(valueTokens) ?? node.Value?.Update(valueTokens);

            rewritten = (MarkupTextLiteralSyntax)rewritten?.Green.CreateRed(node, node.Position);

            if (rewritten.EditHandler is { } originalEditHandler)
            {
                rewritten = rewritten.Update(rewritten.LiteralTokens, MarkupChunkGenerator.Instance, originalEditHandler);
            }

            return rewritten;
        }

        public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
        {
            var containsExpression = false;

            var descendantNodes = node.DescendantNodes(static n => n.Parent is not CSharpCodeBlockSyntax);

            foreach (var child in descendantNodes)
            {
                if (child is CSharpImplicitExpressionSyntax or CSharpExplicitExpressionSyntax)
                {
                    containsExpression = true;
                    break;
                }
            }

            if (_insideUnresolvedAttribute)
            {
                var unresolvedNode = new UnresolvedExpressionAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    ContainsExpression = containsExpression,
                    Source = BuildSourceSpanFromNode(node),
                };

                _builder.Push(unresolvedNode);
                Visit(node.Value);
                _builder.Pop();
                return;
            }

            if (containsExpression)
            {
                _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });
            }
            else
            {
                _builder.Push(new CSharpCodeAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });
            }

            Visit(node.Value);

            _builder.Pop();
        }

        public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
        {
            if (_insideUnresolvedAttribute)
            {
                var unresolvedNode = new UnresolvedAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                };

                unresolvedNode.Children.Add(IntermediateNodeFactory.HtmlToken(
                    arg: node,
                    contentFactory: static node => node.Value?.GetContent() ?? string.Empty,
                    source: BuildSourceSpanFromNode(node.Value)));

                _builder.Add(unresolvedNode);
                return;
            }

            _builder.Push(new HtmlAttributeValueIntermediateNode()
            {
                Prefix = node.Prefix?.GetContent() ?? string.Empty,
                Source = BuildSourceSpanFromNode(node),
            });

            _builder.Add(IntermediateNodeFactory.HtmlToken(
                arg: node,
                contentFactory: static node => node.Value?.GetContent() ?? string.Empty,
                source: BuildSourceSpanFromNode(node.Value)));

            _builder.Pop();
        }

        public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
        {
            var templateNode = new TemplateIntermediateNode();
            _builder.Push(templateNode);

            base.VisitCSharpTemplateBlock(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(templateNode);
        }

        public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            if (_builder.Current is TagHelperHtmlAttributeIntermediateNode)
            {
                // If we are top level in a tag helper HTML attribute, we want to be rendered as markup.
                // This case happens for duplicate non-string bound attributes. They would be initially be categorized as
                // CSharp but since they are duplicate, they should just be markup.
                var markupLiteral = SyntaxFactory.MarkupTextLiteral(node.LiteralTokens).Green.CreateRed(node.Parent, node.Position);
                Visit(markupLiteral);
                return;
            }

            _builder.Add(IntermediateNodeFactory.CSharpToken(
                arg: node,
                contentFactory: static node => node.GetContent(),
                source: BuildSourceSpanFromNode(node)));

            base.VisitCSharpExpressionLiteral(node);
        }

        protected internal void VisitAttributeValue(SyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            var children = new ChildNodesHelper(node.ChildNodesAndTokens());
            var position = node.Position;
            SyntaxTokenList escapedAtTokens = default;
            if (children.FirstOrDefault().AsNode() is MarkupBlockSyntax { Children: [MarkupTextLiteralSyntax literalSyntax, MarkupEphemeralTextLiteralSyntax] })
            {
                // This is a special case when we have an attribute like attr="@@foo".
                // Extract the literal @ token from the first child so it can be merged with the rest of the attribute value.
                // The ephemeral @ token is ignored.
                escapedAtTokens = literalSyntax.LiteralTokens;
                children = children.Skip(1);
                position = children.Count > 0 ? children[0].Position : position;
            }

            if (children.TryCast<MarkupLiteralAttributeValueSyntax>(out var attributeLiteralArray))
            {
                using PooledArrayBuilder<SyntaxToken> builder = [];

                if (escapedAtTokens.Count > 0)
                {
                    builder.AddRange(escapedAtTokens);
                }

                foreach (var literal in attributeLiteralArray)
                {
                    var mergedValue = MergeAttributeValue(literal);
                    builder.AddRange(mergedValue.LiteralTokens);
                }

                var rewritten = SyntaxFactory.MarkupTextLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                Visit(rewritten);
            }
            else if (children.TryCast<MarkupTextLiteralSyntax>(out var markupLiteralArray))
            {
                using PooledArrayBuilder<SyntaxToken> builder = [];

                if (escapedAtTokens.Count > 0)
                {
                    builder.AddRange(escapedAtTokens);
                }

                foreach (var literal in markupLiteralArray)
                {
                    builder.AddRange(literal.LiteralTokens);
                }

                var rewritten = SyntaxFactory.MarkupTextLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                Visit(rewritten);
            }
            else if (children.TryCast<CSharpExpressionLiteralSyntax>(out var expressionLiteralArray))
            {
                using PooledArrayBuilder<SyntaxToken> builder = [];

                if (escapedAtTokens.Count > 0)
                {
                    builder.AddRange(escapedAtTokens);
                }

                SpanEditHandler editHandler = null;
                ISpanChunkGenerator generator = null;
                foreach (var literal in expressionLiteralArray)
                {
                    generator = literal.ChunkGenerator;
                    editHandler = literal.EditHandler;
                    builder.AddRange(literal.LiteralTokens);
                }

                var rewritten = SyntaxFactory.CSharpExpressionLiteral(builder.ToList(), generator, editHandler).Green.CreateRed(node.Parent, position);
                Visit(rewritten);
            }
            else
            {
                if (escapedAtTokens.Count > 0)
                {
                    // If we have escaped @ tokens but no other content to merge with,
                    // create a MarkupTextLiteral just for the escaped @ tokens
                    var rewritten = SyntaxFactory.MarkupTextLiteral(escapedAtTokens).Green.CreateRed(node.Parent, position);
                    Visit(rewritten);
                }
                else
                {
                    Visit(node);
                }
            }
        }

        protected void Combine(HtmlContentIntermediateNode node, SyntaxNode item)
        {
            node.Children.Add(IntermediateNodeFactory.HtmlToken(
                arg: item,
                contentFactory: static item => item.GetContent(),
                source: BuildSourceSpanFromNode(item)));

            if (node.Source is SourceSpan source)
            {
                node.Source = new SourceSpan(
                    source.FilePath,
                    source.AbsoluteIndex,
                    source.LineIndex,
                    source.CharacterIndex,
                    source.Length + item.Width,
                    source.LineCount,
                    source.EndCharacterIndex);
            }
        }

        /// <summary>
        /// Computes the source span covering just the attribute value content (between quotes).
        /// Handles three cases: non-empty value, empty value between quotes, and equals sign with
        /// no value node.
        /// </summary>
        protected SourceSpan? ComputeAttributeValueSourceSpan(MarkupAttributeBlockSyntax node)
        {
            if (node.Value != null)
            {
                var valueStart = node.ValuePrefix != null
                    ? node.ValuePrefix.EndPosition
                    : node.Value.Position;
                var valueEnd = node.ValueSuffix != null
                    ? node.ValueSuffix.Position
                    : node.Value.EndPosition;
                var valueLength = valueEnd - valueStart;

                if (valueLength > 0)
                {
                    var location = SourceDocument.Text.Lines.GetLinePosition(valueStart);
                    var endLocation = SourceDocument.Text.Lines.GetLinePosition(valueEnd);
                    return new SourceSpan(
                        SourceDocument.FilePath,
                        valueStart,
                        location.Line,
                        location.Character,
                        valueLength,
                        endLocation.Line - location.Line,
                        endLocation.Character);
                }
                else
                {
                    var emptyPos = node.Value.Position;
                    var location = SourceDocument.Text.Lines.GetLinePosition(emptyPos);
                    return new SourceSpan(
                        SourceDocument.FilePath,
                        emptyPos,
                        location.Line,
                        location.Character,
                        0,
                        0,
                        location.Character);
                }
            }
            else if (node.EqualsToken.Kind != SyntaxKind.None)
            {
                var valueStart = node.ValuePrefix != null
                    ? node.ValuePrefix.EndPosition
                    : node.EqualsToken.EndPosition;
                var location = SourceDocument.Text.Lines.GetLinePosition(valueStart);
                return new SourceSpan(
                    SourceDocument.FilePath,
                    valueStart,
                    location.Line,
                    location.Character,
                    0,
                    0,
                    location.Character);
            }

            return null;
        }

        /// <summary>
        /// Extracts attribute name/value pairs from an element's start tag for tag helper binding.
        /// Populates the data that <see cref="TagHelperMatchingConventions"/> uses to match tag helpers.
        /// </summary>
        protected static ImmutableArray<KeyValuePair<string, string>> ExtractAttributeData(MarkupElementSyntax node)
        {
            using var attrBuilder = new PooledArrayBuilder<KeyValuePair<string, string>>();
            if (node.MarkupStartTag != null)
            {
                foreach (var attr in node.MarkupStartTag.Attributes)
                {
                    if (attr is MarkupAttributeBlockSyntax attributeBlock)
                    {
                        attrBuilder.Add(new KeyValuePair<string, string>(
                            attributeBlock.Name.GetContent(),
                            attributeBlock.Value?.GetContent() ?? string.Empty));
                    }
                    else if (attr is MarkupMinimizedAttributeBlockSyntax minimizedAttr)
                    {
                        attrBuilder.Add(new KeyValuePair<string, string>(
                            minimizedAttr.Name.GetContent(), string.Empty));
                    }
                }
            }

            return attrBuilder.ToImmutable();
        }

        /// <summary>
        /// Aggregates child source spans into a parent source span. Uses the first child's position
        /// and sums all children's lengths. Applied to expression and template nodes after their
        /// children have been lowered.
        /// </summary>
        protected void ComputeSourceSpanFromChildren(IntermediateNode node)
        {
            if (node.Children.Count > 0)
            {
                var sourceRangeStart = node
                    .Children
                    .FirstOrDefault(child => child.Source != null)
                    ?.Source;

                if (sourceRangeStart != null)
                {
                    var contentLength = node.Children.Sum(child => child.Source?.Length ?? 0);

                    node.Source = new SourceSpan(
                        sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                        sourceRangeStart.Value.AbsoluteIndex,
                        sourceRangeStart.Value.LineIndex,
                        sourceRangeStart.Value.CharacterIndex,
                        contentLength,
                        sourceRangeStart.Value.LineCount,
                        sourceRangeStart.Value.EndCharacterIndex);
                }
            }
        }
    }

    /// <summary>
    /// Handles .cshtml files (MVC views, Razor Pages). Treats HTML markup as text content
    /// (<see cref="HtmlContentIntermediateNode"/> with merged tokens) and supports Tag Helpers.
    /// Elements inside potential tag helpers are unresolved via
    /// <see cref="UnresolvedElementIntermediateNode"/>.
    /// </summary>
    private class LegacyFileKindVisitor : LoweringVisitor
    {
        private bool _insideUnresolvedElement;
        public LegacyFileKindVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, RazorParserOptions options)
            : base(document, builder, options)
        {
        }

        /// <summary>
        /// Lowers a markup element. Creates an <see cref="UnresolvedElementIntermediateNode"/> (unresolved)
        /// because any element could match a tag helper. Extracts attribute data for tag helper binding
        /// and sets <see cref="UnresolvedElementIntermediateNode.StartTagEndIndex"/>/<see cref="UnresolvedElementIntermediateNode.BodyEndIndex"/>
        /// for boundary tracking. Markup transitions (<c>@:</c> and <c>&lt;text&gt;</c>) are not tag
        /// helpers and fall through to the base visitor.
        /// </summary>
        public override void VisitMarkupElement(MarkupElementSyntax node)
        {
            // Markup transitions (e.g., @: or <text>) are not tag helpers and should
            // fall through to the base visitor. We only check StartTag here because
            // legacy files don't support markup transitions as end tags -- that scenario
            // is only relevant for component files (handled by ComponentFileKindVisitor).
            if (node.MarkupStartTag != null && node.MarkupStartTag.IsMarkupTransition)
            {
                base.VisitMarkupElement(node);
                return;
            }

            var tagName = node.MarkupStartTag?.Name.Content ?? node.MarkupEndTag?.Name.Content ?? string.Empty;

            var attributeData = ExtractAttributeData(node);

            var isSelfClosing = false;
            if (node.MarkupStartTag != null)
            {
                var lastToken = node.MarkupStartTag.GetLastToken();
                isSelfClosing = lastToken.Parent?.GetContent().EndsWith("/>", StringComparison.Ordinal) ?? false;
            }

            var element = new UnresolvedElementIntermediateNode()
            {
                TagName = tagName,
                Source = BuildSourceSpanFromNode(node),
                IsComponent = false,
                IsEscaped = node.MarkupStartTag?.Bang is { Width: > 0 },
                IsSelfClosing = isSelfClosing,
                HasEndTag = node.MarkupEndTag != null,
                EndTagName = node.MarkupEndTag?.GetTagNameWithOptionalBang(),
                EndTagSpan = node.MarkupEndTag != null ? BuildSourceSpanFromNode(node.MarkupEndTag) : null,
                IsVoidElement = node.MarkupStartTag?.IsVoidElement() ?? false,
                StartTagNameSpan = node.MarkupStartTag?.Name.GetSourceSpan(SourceDocument),
                StartTagSpan = node.MarkupStartTag != null ? BuildSourceSpanFromNode(node.MarkupStartTag) : null,
                AttributeData = attributeData,
                HasMissingCloseAngle = node.MarkupStartTag?.CloseAngle.IsMissing ?? false,
                HasMissingEndCloseAngle = node.MarkupEndTag?.CloseAngle.IsMissing ?? false,
            };

            _builder.Push(element);

            var previousInsideFlag = _insideUnresolvedElement;
            _insideUnresolvedElement = true;

            if (node.MarkupStartTag != null)
            {
                VisitMarkupStartTag(node.MarkupStartTag);
            }

            // Now that all start-tag children have been lowered, check if any are C# expressions
            // (e.g. <div @expr>) and record this on the element so the resolution phase doesn't
            // need to scan children itself.
            foreach (var child in element.Children)
            {
                if (child is CSharpExpressionIntermediateNode or CSharpCodeIntermediateNode)
                {
                    element.HasDynamicExpressionChild = true;
                    break;
                }
            }

            _insideUnresolvedElement = false;
            element.StartTagEndIndex = element.Children.Count;

            foreach (var item in node.Body)
            {
                Visit(item);
            }

            element.BodyEndIndex = element.Children.Count;

            if (node.MarkupEndTag != null)
            {
                VisitMarkupEndTag(node.MarkupEndTag);
            }

            _insideUnresolvedElement = previousInsideFlag;
            _builder.Pop();
        }

        /// <summary>
        /// Lowers a non-minimized attribute. If inside a unresolved element (<c>_insideUnresolvedElement</c>),
        /// creates a <see cref="UnresolvedAttributeIntermediateNode"/> with two pre-lowered fallback
        /// forms: <c>AsTagHelperAttribute</c> (structured <see cref="HtmlAttributeIntermediateNode"/> with merged
        /// value tokens - used for unbound attributes when the element IS a tag helper) and
        /// <c>AsMarkupAttribute</c> (full attribute with individual tokens - used when the element is NOT
        /// a tag helper and must be unwrapped back to plain HTML markup). Handles the <c>@@</c> escape
        /// pattern in unresolved attribute values. If NOT inside an unresolved element, falls through to
        /// create a regular
        /// <see cref="HtmlAttributeIntermediateNode"/>.
        /// </summary>
        // Example
        // <input` checked="hello-world @false"`/>
        //  Name=checked
        //  Prefix= checked="
        //  Suffix="
        public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
        {
            var prefixTokens = MergeTokenLists(
                node.NamePrefix?.LiteralTokens,
                node.Name.LiteralTokens,
                node.NameSuffix?.LiteralTokens,
                new SyntaxTokenList(node.EqualsToken),
                node.ValuePrefix?.LiteralTokens);

            var position = node.NamePrefix?.Position ?? node.Name.Position;
            var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, position);

            var name = node.Name.GetContent();

            if (!_insideUnresolvedElement)
            {
                LowerAttributeAsHtml(node, name, prefix);
                return;
            }

            // Unresolved path: create deferred attribute node with fallback forms.
            var valueSourceSpan = ComputeAttributeValueSourceSpan(node);

            _builder.Push(new UnresolvedAttributeIntermediateNode()
            {
                AttributeName = name,
                IsMinimized = false,
                Source = BuildSourceSpanFromNode(node),
                ValueContent = node.Value?.GetContent(),
                ValueSourceSpan = valueSourceSpan,
                AttributeStructure = InferAttributeStructure(node),
                AttributeNameSpan = BuildSourceSpanFromNode(node.Name),
            });

            // Capture the pre-lowered fallback form (the non-tag-helper HTML form) by
            // temporarily resetting state and lowering into the unresolved node's children.
            // We then extract those children as the fallback before adding the unresolved form.
            _insideUnresolvedElement = false;
            LowerAttributeAsHtml(node, name, prefix);
            _insideUnresolvedElement = true;

            var unresolvedAttrNode = (UnresolvedAttributeIntermediateNode)_builder.Current;
            IntermediateNode legacyFallback = null;
            if (unresolvedAttrNode.Children.Count == 1)
            {
                legacyFallback = unresolvedAttrNode.Children[0];
            }
            else if (unresolvedAttrNode.Children.Count > 0)
            {
                var container = new MarkupElementIntermediateNode();
                container.Children.AddRange(unresolvedAttrNode.Children);

                legacyFallback = container;
            }

            unresolvedAttrNode.AsTagHelperAttribute = legacyFallback;
            unresolvedAttrNode.AsMarkupAttribute = legacyFallback;
            unresolvedAttrNode.Children.Clear();

            // Create HtmlAttribute with unresolved value children.
            _builder.Push(new HtmlAttributeIntermediateNode()
            {
                AttributeName = name,
                Prefix = prefix.GetContent(),
                Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                Source = BuildSourceSpanFromNode(node),
            });

            _insideUnresolvedAttribute = true;
            LowerUnresolvedAttributeValue(node.Value);
            _insideUnresolvedAttribute = false;

            _builder.Pop();

            // Store the HtmlAttribute child directly on the unresolved node for O(1) access.
            if (_builder.Current is UnresolvedAttributeIntermediateNode currentUnresolved)
            {
                currentUnresolved.HtmlAttributeNode = (HtmlAttributeIntermediateNode)currentUnresolved.Children[^1];
            }

            _builder.Pop();
        }

        /// <summary>
        /// Lowers a <see cref="MarkupAttributeBlockSyntax"/> to its non-tag-helper HTML form.
        /// Used by both the non-unresolved path (direct lowering) and the unresolved path
        /// (to capture the fallback form stored on the unresolved node).
        /// </summary>
        private void LowerAttributeAsHtml(MarkupAttributeBlockSyntax node, string name, MarkupTextLiteralSyntax prefix)
        {
            if (!_options.AllowConditionalDataDashAttributes && name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                Visit(prefix);
                Visit(node.Value);
                Visit(node.ValueSuffix);
            }
            else if (node.Value is { } blockSyntax)
            {
                var children = new ChildNodesHelper(blockSyntax.ChildNodesAndTokens());

                if (children.TryCast<MarkupLiteralAttributeValueSyntax>(out var attributeLiteralArray))
                {
                    using var builder = new PooledArrayBuilder<SyntaxToken>();

                    foreach (var literal in attributeLiteralArray)
                    {
                        var mergedValue = MergeAttributeValue(literal);
                        builder.AddRange(mergedValue.LiteralTokens);
                    }

                    var rewritten = SyntaxFactory.MarkupTextLiteral(builder.ToList());

                    var mergedLiterals = MergeTokenLists(
                        prefix?.LiteralTokens,
                        rewritten.LiteralTokens,
                        node.ValueSuffix?.LiteralTokens);

                    var mergedAttribute = SyntaxFactory.MarkupTextLiteral(mergedLiterals).Green.CreateRed(node.Parent, node.Position);
                    Visit(mergedAttribute);

                    return;
                }

                _builder.Push(new HtmlAttributeIntermediateNode()
                {
                    AttributeName = name,
                    Prefix = prefix.GetContent(),
                    Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });

                VisitAttributeValue(node.Value);

                _builder.Pop();
            }
            else
            {
                // No value -- create empty HtmlAttribute.
                _builder.Push(new HtmlAttributeIntermediateNode()
                {
                    AttributeName = name,
                    Prefix = prefix.GetContent(),
                    Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });

                VisitAttributeValue(node.Value);

                _builder.Pop();
            }
        }

        /// <summary>
        /// Lowers an attribute value inside an unresolved element. Handles the @@ escape pattern
        /// (e.g. <c>Value="@@currentCount"</c>) by merging or splitting the @ literal and remaining
        /// content. Falls through to <see cref="VisitAttributeValue"/> for non-escape cases.
        /// </summary>
        private void LowerUnresolvedAttributeValue(RazorSyntaxNode value)
        {
            if (value == null)
            {
                VisitAttributeValue(value);
                return;
            }

            // Check for @@ escape pattern in unresolved attribute values.
            var valueChildren = value.ChildNodesAndTokens();
            if (valueChildren.Count >= 2 &&
                valueChildren[0].AsNode() is MarkupBlockSyntax { Children: [MarkupTextLiteralSyntax atLiteral, MarkupEphemeralTextLiteralSyntax] })
            {
                // Check if all remaining children are literals (can merge everything).
                var allLiteral = true;
                for (var i = 1; i < valueChildren.Count; i++)
                {
                    if (valueChildren[i].AsNode() is not MarkupLiteralAttributeValueSyntax)
                    {
                        allLiteral = false;
                        break;
                    }
                }

                SyntaxNode rewritten;

                if (allLiteral)
                {
                    // All-literal: merge @ + all literal content into one token.
                    using var mergedTokens = new PooledArrayBuilder<SyntaxToken>();
                    mergedTokens.AddRange(atLiteral.LiteralTokens);
                    for (var i = 1; i < valueChildren.Count; i++)
                    {
                        var literal = (MarkupLiteralAttributeValueSyntax)valueChildren[i].AsNode();
                        var merged = MergeAttributeValue(literal);
                        mergedTokens.AddRange(merged.LiteralTokens);
                    }

                    rewritten = SyntaxFactory.MarkupTextLiteral(mergedTokens.ToList()).Green.CreateRed(value.Parent, atLiteral.Position);
                }
                else
                {
                    // Mixed: just the @ literal; remaining children are visited below.
                    rewritten = SyntaxFactory.MarkupTextLiteral(atLiteral.LiteralTokens).Green.CreateRed(value.Parent, atLiteral.Position);
                }

                var rewrittenSource = BuildSourceSpanFromNode(rewritten);
                var unresolvedNode = new UnresolvedAttributeValueIntermediateNode()
                {
                    Prefix = string.Empty,
                    Source = rewrittenSource,
                };
                unresolvedNode.Children.Add(IntermediateNodeFactory.HtmlToken(
                    arg: (MarkupTextLiteralSyntax)rewritten,
                    contentFactory: static node => node.GetContent() ?? string.Empty,
                    source: rewrittenSource));
                _builder.Add(unresolvedNode);

                if (!allLiteral)
                {
                    // Visit remaining children (expressions, etc.)
                    for (var i = 1; i < valueChildren.Count; i++)
                    {
                        Visit(valueChildren[i].AsNode());
                    }
                }
            }
            else
            {
                VisitAttributeValue(value);
            }
        }

        /// <summary>
        /// Lowers a minimized attribute (no value, e.g. <c>checked</c>, <c>disabled</c>). If inside a
        /// unresolved element, creates a <see cref="UnresolvedAttributeIntermediateNode"/> with
        /// <c>IsMinimized = true</c> and a fallback <see cref="HtmlContentIntermediateNode"/> containing
        /// the attribute name as text.
        /// </summary>
        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            if (_insideUnresolvedElement)
            {
                // Produce the fallback: what this minimized attribute looks like as plain HTML.
                // Minimized attributes are just html content (e.g. "checked" -> HtmlContent " checked").
                var fallbackTokens = MergeTokenLists(
                    node.NamePrefix?.LiteralTokens,
                    node.Name?.LiteralTokens);
                var fallbackLiteral = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(fallbackTokens).Green.CreateRed(node.Parent, node.Position);
                var fallbackSource = BuildSourceSpanFromNode(fallbackLiteral);
                var fallback = new HtmlContentIntermediateNode() { Source = fallbackSource };
                fallback.Children.Add(IntermediateNodeFactory.HtmlToken(
                    arg: fallbackLiteral,
                    contentFactory: static node => node.GetContent(),
                    fallbackSource));

                _builder.Add(new UnresolvedAttributeIntermediateNode()
                {
                    AttributeName = node.Name.GetContent(),
                    IsMinimized = true,
                    Source = BuildSourceSpanFromNode(node),
                    AttributeStructure = AttributeStructure.Minimized,
                    AttributeNameSpan = BuildSourceSpanFromNode(node.Name),
                    AsMarkupAttribute = fallback,
                });
                return;
            }

            if (!_options.AllowConditionalDataDashAttributes)
            {
                var name = node.Name.GetContent();

                if (name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
                {
                    base.VisitMarkupMinimizedAttributeBlock(node);
                    return;
                }
            }

            // Minimized attributes are just html content.
            var literals = MergeTokenLists(
                node.NamePrefix?.LiteralTokens,
                node.Name?.LiteralTokens);

            var literal = SyntaxFactory.MarkupTextLiteral(literals).Green.CreateRed(node.Parent, node.Position);

            Visit(literal);
        }

        // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
        // inside an expression.
        // Ex:
        //      @DateTime.@*This is a comment*@Now
        //
        // We need to capture this in the IR so that we can give each piece the correct source mappings
        public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
        {
            if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
            {
                base.VisitCSharpExplicitExpression(node);
                return;
            }

            var expressionNode = new CSharpExpressionIntermediateNode();

            _builder.Push(expressionNode);

            base.VisitCSharpExplicitExpression(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(expressionNode);
        }

        public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
        {
            if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
            {
                base.VisitCSharpImplicitExpression(node);
                return;
            }

            var expressionNode = new CSharpExpressionIntermediateNode();

            _builder.Push(expressionNode);

            base.VisitCSharpImplicitExpression(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(expressionNode);
        }
        public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            if (node.ChunkGenerator is null or StatementChunkGenerator)
            {
                var isAttributeValue = _builder.Current is CSharpCodeAttributeValueIntermediateNode;

                if (!isAttributeValue)
                {
                    var statementNode = new CSharpCodeIntermediateNode()
                    {
                        Source = BuildSourceSpanFromNode(node)
                    };
                    _builder.Push(statementNode);
                }

                _builder.Add(IntermediateNodeFactory.CSharpToken(
                    arg: node,
                    contentFactory: static node => node.GetContent(),
                    source: BuildSourceSpanFromNode(node)));

                if (!isAttributeValue)
                {
                    _builder.Pop();
                }
            }

            base.VisitCSharpStatementLiteral(node);
        }

        public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
        {
            if (node.ChunkGenerator == SpanChunkGenerator.Null)
            {
                return;
            }

            if (node.LiteralTokens is [{ Kind: SyntaxKind.Marker, Content.Length: 0 }])
            {
                // We don't want to create IR nodes for marker tokens.
                return;
            }

            VisitHtmlContent(node);
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            if (node.IsMarkupTransition)
            {
                // No need to visit <text> tags.
                return;
            }

            foreach (var child in node.LegacyChildren)
            {
                Visit(child);
            }
        }

        public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
        {
            if (node.IsMarkupTransition)
            {
                // No need to visit </text> tags.
                return;
            }

            foreach (var child in node.LegacyChildren)
            {
                Visit(child);
            }
        }

        private void VisitHtmlContent(SyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            var source = BuildSourceSpanFromNode(node);
            var currentChildren = _builder.Current.Children;

            // Don't merge HtmlContent across element region boundaries (start-tag -> body -> end-tag).
            // The pre-computed indices mark where each region begins, so when we're about to add
            // the first child of a new region, we must start a fresh HtmlContentIntermediateNode.
            var atBoundary = _builder.Current is UnresolvedElementIntermediateNode element
                && (currentChildren.Count == element.StartTagEndIndex
                 || currentChildren.Count == element.BodyEndIndex);

            if (!atBoundary && currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIntermediateNode)
            {
                var existingHtmlContent = (HtmlContentIntermediateNode)currentChildren[currentChildren.Count - 1];

                if (existingHtmlContent.Source == null && source == null)
                {
                    Combine(existingHtmlContent, node);
                    return;
                }

                if (source != null &&
                    existingHtmlContent.Source != null &&
                    existingHtmlContent.Source.Value.FilePath == source.Value.FilePath &&
                    existingHtmlContent.Source.Value.AbsoluteIndex + existingHtmlContent.Source.Value.Length == source.Value.AbsoluteIndex)
                {
                    Combine(existingHtmlContent, node);
                    return;
                }
            }

            var contentNode = new HtmlContentIntermediateNode()
            {
                Source = source
            };

            _builder.Push(contentNode);

            _builder.Add(IntermediateNodeFactory.HtmlToken(
                arg: node,
                contentFactory: static node => node.GetContent(),
                source));

            _builder.Pop();
        }
    }

    /// <summary>
    /// Handles .razor files (Blazor components). Treats HTML markup as structured nodes
    /// (<see cref="MarkupElementIntermediateNode"/> with tag name and attributes) and supports
    /// Components. Every element is wrapped in <see cref="UnresolvedElementIntermediateNode"/>
    /// because any element could match a component.
    /// </summary>
    private class ComponentFileKindVisitor : LoweringVisitor
    {
        public ComponentFileKindVisitor(
            DocumentIntermediateNode document,
            IntermediateNodeBuilder builder,
            RazorParserOptions options)
            : base(document, builder, options)
        {
        }

        /// <summary>
        /// Always creates an <see cref="UnresolvedElementIntermediateNode"/> because every element
        /// could be a component. Markup transitions (<c>@:</c> and <c>&lt;text&gt;</c>) are excluded
        /// and fall through to the base visitor. Extracts attribute data for tag helper binding and
        /// sets boundary indices for content region tracking.
        /// </summary>
        public override void VisitMarkupElement(MarkupElementSyntax node)
        {
            if ((node.MarkupStartTag != null && node.MarkupStartTag.IsMarkupTransition) ||
                (node.MarkupEndTag != null && node.MarkupEndTag.IsMarkupTransition))
            {
                base.VisitMarkupElement(node);
                return;
            }

            var tagName = node.MarkupStartTag?.Name.Content ?? node.MarkupEndTag?.Name.Content ?? string.Empty;

            var attributeData = ExtractAttributeData(node);

            var element = new UnresolvedElementIntermediateNode()
            {
                Source = BuildSourceSpanFromNode(node),
                TagName = tagName,
                IsComponent = true,
                IsEscaped = node.MarkupStartTag?.Bang is { Width: > 0 },
                IsSelfClosing = node.MarkupStartTag?.IsSelfClosing() ?? false,
                HasEndTag = node.MarkupEndTag != null,
                EndTagName = node.MarkupEndTag?.GetTagNameWithOptionalBang(),
                EndTagSpan = node.MarkupEndTag != null ? BuildSourceSpanFromNode(node.MarkupEndTag) : null,
                IsVoidElement = node.MarkupStartTag?.IsVoidElement() ?? false,
                StartTagNameSpan = node.MarkupStartTag?.Name.GetSourceSpan(SourceDocument),
                StartTagSpan = node.MarkupStartTag != null ? BuildSourceSpanFromNode(node.MarkupStartTag) : null,
                AttributeData = attributeData,
                HasMissingCloseAngle = node.MarkupStartTag?.CloseAngle.IsMissing ?? false,
                HasMissingEndCloseAngle = node.MarkupEndTag?.CloseAngle.IsMissing ?? false,
            };

            // Preserve diagnostics from the original ComponentFileKindVisitor logic.
            if (node.MarkupStartTag != null && node.MarkupEndTag != null && node.MarkupStartTag.IsVoidElement())
            {
                element.AddDiagnostic(
                    ComponentDiagnosticFactory.Create_UnexpectedClosingTagForVoidElement(
                        BuildSourceSpanFromNode(node.MarkupEndTag), node.MarkupEndTag.GetTagNameWithOptionalBang()));
            }
            else if (node.MarkupStartTag != null && node.MarkupEndTag == null && !node.MarkupStartTag.IsVoidElement() && !node.MarkupStartTag.IsSelfClosing())
            {
                element.AddDiagnostic(
                    ComponentDiagnosticFactory.Create_UnclosedTag(
                        BuildSourceSpanFromNode(node.MarkupStartTag), node.MarkupStartTag.GetTagNameWithOptionalBang()));
            }
            else if (node.MarkupStartTag == null && node.MarkupEndTag != null)
            {
                element.AddDiagnostic(
                    ComponentDiagnosticFactory.Create_UnexpectedClosingTag(
                        BuildSourceSpanFromNode(node.MarkupEndTag), node.MarkupEndTag.GetTagNameWithOptionalBang()));
            }

            _builder.Push(element);

            base.VisitMarkupElement(node);

            _builder.Pop();
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            // We want to skip over the other misc tokens that make up a start tag, and
            // just process the attributes.
            //
            // Visit the attributes
            foreach (var block in node.Attributes)
            {
                if (block is MarkupAttributeBlockSyntax attribute)
                {
                    VisitMarkupAttributeBlock(attribute);
                }
                else if (block is MarkupMinimizedAttributeBlockSyntax minimized)
                {
                    VisitMarkupMinimizedAttributeBlock(minimized);
                }
            }
        }

        public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
        {
            // We want to skip over the other misc tokens that make up a start tag, and
            // just process the attributes.
            //
            // Nothing to do here
        }

        // Example
        // <input` checked="hello-world @false"`/>
        //  Name=checked
        //  Prefix= checked="
        //  Suffix="
        public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
        {
            var name = node.Name.GetContent();
            var isUnresolved = _builder.Current is UnresolvedElementIntermediateNode;

            if (isUnresolved)
            {
                var valueSourceSpan = ComputeAttributeValueSourceSpan(node);
                var source = BuildSourceSpanFromNode(node);

                _builder.Push(new UnresolvedAttributeIntermediateNode()
                {
                    AttributeName = name,
                    IsMinimized = false,
                    Source = source,
                    ValueContent = node.Value?.GetContent(),
                    ValueSourceSpan = valueSourceSpan,
                    AttributeStructure = InferAttributeStructure(node),
                    AttributeNameSpan = BuildSourceSpanFromNode(node.Name),
                });

                // Capture value-only fallback (merged adjacent literal tokens) by temporarily
                // lowering into the unresolved node's children with unresolved state reset.
                var fallbackContainer = new HtmlAttributeIntermediateNode()
                {
                    AttributeName = name,
                    Prefix = SyntaxFactory.MarkupTextLiteral(MergeTokenLists(
                        node.NamePrefix?.LiteralTokens,
                        node.Name.LiteralTokens,
                        node.NameSuffix?.LiteralTokens,
                        new SyntaxTokenList(node.EqualsToken),
                        node.ValuePrefix?.LiteralTokens)).GetContent(),
                    Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                    Source = source,
                };

                if (node.Value != null)
                {
                    _builder.Push(fallbackContainer);
                    VisitAttributeValue(node.Value);
                    _builder.Pop();
                }
                var unresolvedAttrNode = (UnresolvedAttributeIntermediateNode)_builder.Current;
                // Remove fallbackContainer from children -- it was temporarily added by Push.
                unresolvedAttrNode.Children.Remove(fallbackContainer);
                unresolvedAttrNode.AsTagHelperAttribute = fallbackContainer;

                // Capture AsMarkupAttribute fallback by lowering the whole attribute in non-unresolved
                // context. Push a temporary container so _builder.Current is not an
                // UnresolvedElementIntermediateNode, which causes VisitMarkupAttributeBlock to
                // take the non-unresolved path.
                var fullFallbackContainer = new MarkupElementIntermediateNode();
                _builder.Push(fullFallbackContainer);
                Visit(node);
                _builder.Pop();
                unresolvedAttrNode.Children.Remove(fullFallbackContainer);
                unresolvedAttrNode.AsMarkupAttribute = fullFallbackContainer.Children.Count == 1
                    ? fullFallbackContainer.Children[0]
                    : fullFallbackContainer.Children.Count > 0 ? fullFallbackContainer : null;
            }

            var prefixTokens = MergeTokenLists(
                node.NamePrefix?.LiteralTokens,
                node.Name.LiteralTokens,
                node.NameSuffix?.LiteralTokens,
                new SyntaxTokenList(node.EqualsToken),
                node.ValuePrefix?.LiteralTokens);

            var position = node.NamePrefix?.Position ?? node.Name.Position;
            var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, position);

            _builder.Push(new HtmlAttributeIntermediateNode()
            {
                AttributeName = name,
                Prefix = prefix.GetContent(),
                Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                Source = BuildSourceSpanFromNode(node),
            });

            _insideUnresolvedAttribute = isUnresolved;
            if (isUnresolved &&
                node.Value?.ChildNodesAndTokens() is { Count: >= 2 } valueChildren &&
                valueChildren[0].AsNode() is MarkupBlockSyntax { Children: [MarkupTextLiteralSyntax atLiteral, MarkupEphemeralTextLiteralSyntax] } &&
                valueChildren[1].AsNode() is MarkupLiteralAttributeValueSyntax)
            {
                // @@ escape pattern: merge the escaped @ with all following literal children into one unresolved node.
                using var mergedTokens = new PooledArrayBuilder<SyntaxToken>();
                mergedTokens.AddRange(atLiteral.LiteralTokens);
                for (var i = 1; i < valueChildren.Count; i++)
                {
                    if (valueChildren[i].AsNode() is MarkupLiteralAttributeValueSyntax literal)
                    {
                        var merged = MergeAttributeValue(literal);
                        mergedTokens.AddRange(merged.LiteralTokens);
                    }
                    else
                    {
                        // Mixed content after @@ -- fall through to normal Visit
                        break;
                    }
                }

                var atPosition = atLiteral.Position;
                var rewritten = SyntaxFactory.MarkupTextLiteral(mergedTokens.ToList()).Green.CreateRed(node.Value.Parent, atPosition);
                var rewrittenSource = BuildSourceSpanFromNode(rewritten);
                var unresolvedNode = new UnresolvedAttributeValueIntermediateNode()
                {
                    Prefix = string.Empty,
                    Source = rewrittenSource,
                };
                unresolvedNode.Children.Add(IntermediateNodeFactory.HtmlToken(
                    arg: (MarkupTextLiteralSyntax)rewritten,
                    contentFactory: static node => node.GetContent() ?? string.Empty,
                    source: rewrittenSource));
                _builder.Add(unresolvedNode);
            }
            else
            {
                Visit(node.Value);
            }
            _insideUnresolvedAttribute = false;

            _builder.Pop();

            // Store the HtmlAttribute child directly on the unresolved node for O(1) access.
            if (isUnresolved && _builder.Current is UnresolvedAttributeIntermediateNode currentUnresolved)
            {
                currentUnresolved.HtmlAttributeNode = (HtmlAttributeIntermediateNode)currentUnresolved.Children[currentUnresolved.Children.Count - 1];
            }

            if (isUnresolved)
            {
                _builder.Pop();
            }
        }

        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            var prefixTokens = MergeTokenLists(
                node.NamePrefix?.LiteralTokens,
                node.Name.LiteralTokens);
            var position = node.NamePrefix?.Position ?? node.Name.Position;
            var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, position);

            var name = node.Name.GetContent();
            var source = BuildSourceSpanFromNode(node);
            var htmlAttr = new HtmlAttributeIntermediateNode()
            {
                AttributeName = name,
                Prefix = prefix.GetContent(),
                Suffix = null,
                Source = source,
            };

            if (_builder.Current is UnresolvedElementIntermediateNode)
            {
                _builder.Add(new UnresolvedAttributeIntermediateNode()
                {
                    AttributeName = name,
                    IsMinimized = true,
                    Source = source,
                    AttributeStructure = AttributeStructure.Minimized,
                    AttributeNameSpan = BuildSourceSpanFromNode(node.Name),
                    AsMarkupAttribute = htmlAttr,
                });
            }
            else
            {
                _builder.Add(htmlAttr);
            }
        }

        // Example
        // <input checked="hello-world `@false`"/>
        //  Prefix= (space)
        //  Children will contain a token for @false.
        public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
        {
            if (_builder.Current is HtmlAttributeIntermediateNode)
            {
                var attrValueSource = BuildSourceSpanFromNode(node);

                IntermediateNode childNode = _insideUnresolvedAttribute
                    ? new UnresolvedAttributeValueIntermediateNode()
                    {
                        Prefix = string.Empty,
                        Source = attrValueSource,
                    }
                    : new HtmlAttributeValueIntermediateNode()
                    {
                        Prefix = string.Empty,
                        Source = attrValueSource,
                    };

                childNode.Children.Add(IntermediateNodeFactory.HtmlToken(
                    arg: node,
                    contentFactory: static node => node.GetContent() ?? string.Empty,
                    source: attrValueSource));

                _builder.Add(childNode);
                return;
            }

            var context = node.EditHandler;
            if (node.ChunkGenerator == SpanChunkGenerator.Null)
            {
                return;
            }

            if (node.LiteralTokens is [{ Kind: SyntaxKind.Marker, Content.Length: 0 }])
            {
                // We don't want to create IR nodes for marker tokens.
                return;
            }

            // Combine chunks of HTML literal text if possible.
            var source = BuildSourceSpanFromNode(node);
            var currentChildren = _builder.Current.Children;
            if (currentChildren.Count > 0 &&
                currentChildren[currentChildren.Count - 1] is HtmlContentIntermediateNode existingHtmlContent)
            {
                if (existingHtmlContent.Source == null && source == null)
                {
                    Combine(existingHtmlContent, node);
                    return;
                }

                if (source != null &&
                    existingHtmlContent.Source != null &&
                    existingHtmlContent.Source.Value.FilePath == source.Value.FilePath &&
                    existingHtmlContent.Source.Value.AbsoluteIndex + existingHtmlContent.Source.Value.Length == source.Value.AbsoluteIndex)
                {
                    Combine(existingHtmlContent, node);
                    return;
                }
            }

            _builder.Add(new HtmlContentIntermediateNode()
            {
                Source = source,
                Children =
                {
                    IntermediateNodeFactory.HtmlToken(
                        arg: node,
                        contentFactory: static node => node.GetContent(),
                        source)
                }
            });
        }

        public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
        {
            // Comments are ignored by components. We skip over anything that appears inside.
        }
        // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
        // inside an expression.
        // Ex:
        //      @DateTime.@*This is a comment*@Now
        //
        // We need to capture this in the IR so that we can give each piece the correct source mappings
        public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
        {
            if (_builder.Current is HtmlAttributeIntermediateNode)
            {
                // This can happen inside a data- attribute
                _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                {
                    Prefix = string.Empty,
                    Source = this.BuildSourceSpanFromNode(node),
                });

                base.VisitCSharpExplicitExpression(node);

                _builder.Pop();

                return;
            }

            if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
            {
                base.VisitCSharpExplicitExpression(node);
                return;
            }

            var expressionNode = new CSharpExpressionIntermediateNode();

            _builder.Push(expressionNode);

            base.VisitCSharpExplicitExpression(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(expressionNode);
        }

        public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
        {
            if (_builder.Current is HtmlAttributeIntermediateNode)
            {
                // This can happen inside a data- attribute
                _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                {
                    Prefix = string.Empty,
                    Source = this.BuildSourceSpanFromNode(node),
                });

                base.VisitCSharpImplicitExpression(node);

                _builder.Pop();

                return;
            }

            if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
            {
                base.VisitCSharpImplicitExpression(node);
                return;
            }

            var expressionNode = new CSharpExpressionIntermediateNode();

            _builder.Push(expressionNode);

            base.VisitCSharpImplicitExpression(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(expressionNode);
        }
        public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            if (node.ChunkGenerator is null or StatementChunkGenerator)
            {
                var isAttributeValue = _builder.Current is CSharpCodeAttributeValueIntermediateNode;

                if (!isAttributeValue)
                {
                    var statementNode = new CSharpCodeIntermediateNode()
                    {
                        Source = BuildSourceSpanFromNode(node)
                    };
                    _builder.Push(statementNode);
                }

                _builder.Add(IntermediateNodeFactory.CSharpToken(
                    arg: node,
                    contentFactory: static node => node.GetContent(),
                    source: BuildSourceSpanFromNode(node)));

                if (!isAttributeValue)
                {
                    _builder.Pop();
                }
            }

            base.VisitCSharpStatementLiteral(node);
        }

    }

    private ref struct DirectiveAttributeName(string original)
    {
        // Directive attributes should start with '@' unless the descriptors are misconfigured.
        // In that case, we would have already logged an error.
        public readonly ReadOnlySpan<char> Span = original.StartsWith('@') ? original.AsSpan()[1..] : original;

        public string Text => field ??= (Span.Length < original.Length ? Span.ToString() : original);

        private bool? _hasParameter;

        public bool HasParameter => _hasParameter ??= Span.IndexOf(':') >= 0;

        public string TextWithoutParameter
            => field ??= Span.IndexOf(':') is int index && index >= 0 ? Span[..index].ToString() : Text;
    }

    /// <summary>
    /// Handles <c>_Imports.razor</c> files. Only processes directives (<c>@using</c>,
    /// <c>@namespace</c>) and code expressions. Does not support HTML elements or attributes
    /// (no components in import files) -- markup and expressions produce diagnostics.
    /// </summary>
    private class ComponentImportFileKindVisitor : LoweringVisitor
    {
        public ComponentImportFileKindVisitor(
            DocumentIntermediateNode document,
            IntermediateNodeBuilder builder,
            RazorParserOptions options)
            : base(document, builder, options)
        {
        }

        public override void VisitMarkupElement(MarkupElementSyntax node)
        {
            _document.AddDiagnostic(
                ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

            base.VisitMarkupElement(node);
        }

        public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
        {
            _document.AddDiagnostic(
                ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

            base.VisitMarkupCommentBlock(node);
        }

        public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
        {
            _document.AddDiagnostic(
                ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

            base.VisitCSharpExplicitExpression(node);
        }

        public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
        {
            // We typically don't want C# in imports files except for directives. But since Razor directive intellisense
            // is tied to C# intellisense during design time, we want to still generate and IR node for implicit expressions.
            // Otherwise, there will be no source mapping when someone types an `@` leading to no intellisense.
            if (node.FirstAncestorOrSelf<SyntaxNode>(n => n is MarkupStartTagSyntax || n is MarkupEndTagSyntax) != null)
            {
                // We don't care about implicit expression in attributes.
                return;
            }

            var expressionNode = new CSharpExpressionIntermediateNode();

            _builder.Push(expressionNode);

            base.VisitCSharpImplicitExpression(node);

            _builder.Pop();

            ComputeSourceSpanFromChildren(expressionNode);

            _document.AddDiagnostic(
                ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(expressionNode.Source));

            base.VisitCSharpImplicitExpression(node);
        }

        public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            if (node.FirstAncestorOrSelf<SyntaxNode>(n => n is CSharpImplicitExpressionSyntax) == null)
            {
                // We only care about implicit expressions.
                return;
            }

            _builder.Add(IntermediateNodeFactory.CSharpToken(
                arg: node,
                contentFactory: static node => node.GetContent(),
                source: BuildSourceSpanFromNode(node)));
        }

        public override void VisitCSharpStatement(CSharpStatementSyntax node)
        {
            _document.AddDiagnostic(
                ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

            base.VisitCSharpStatement(node);
        }
    }

    private class ImportsVisitor : LoweringVisitor
    {
        public ImportsVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, RazorParserOptions options)
            : base(document, new ImportBuilder(builder), options)
        {
        }

        private class ImportBuilder : IntermediateNodeBuilder
        {
            private readonly IntermediateNodeBuilder _innerBuilder;

            public ImportBuilder(IntermediateNodeBuilder innerBuilder)
            {
                _innerBuilder = innerBuilder;
            }

            public override IntermediateNode Current => _innerBuilder.Current;

            public override void Add(IntermediateNode node)
            {
                node.IsImported = true;
                _innerBuilder.Add(node);
            }

            public override IntermediateNode Build() => _innerBuilder.Build();

            public override void Insert(int index, IntermediateNode node)
            {
                node.IsImported = true;
                _innerBuilder.Insert(index, node);
            }

            public override IntermediateNode Pop() => _innerBuilder.Pop();

            public override void Push(IntermediateNode node)
            {
                node.IsImported = true;
                _innerBuilder.Push(node);
            }
        }
    }

    private static bool IsMalformed(IEnumerable<RazorDiagnostic> diagnostics)
        => diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);
}
