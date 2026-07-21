// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Splits a component into decl and impl halves before tag-helper resolution. For a component whose
/// <c>@code</c> mixes markup with C#, it decides the split (a pure, resolution-independent function of
/// the class-body IR -- see <see cref="MarkupSplitter"/>), produces the markup-free decl C# document
/// immediately by reusing the engine's classifier and decl-lowering phases, and rewrites the working
/// node into the impl half (the render body plus the markup-bearing members), which flows through the
/// rest of the pipeline.
/// </summary>
/// <remarks>
/// Producing the decl half here -- before tag-helper resolution -- is the point: the decl document is
/// markup-free and depends only on user source, so tag-helper discovery can consume it early and stay
/// incremental. A component with no class-body markup, or one whose shape can't be split safely (a
/// markup property, an unsupported node, a preprocessor directive, a header/arity directive, or
/// unrecoverable syntax), is left untouched for the single-document lowering. The final C# lowering
/// phase emits the rewritten node directly as the impl half, gated on
/// <see cref="DocumentIntermediateNode.IsSplitImplDocument"/>.
/// </remarks>
internal sealed class DefaultRazorMarkupSplitPhase : RazorEnginePhaseBase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        // Gate cheaply, before any parsing: the split is component-only, a suppressed primary body already
        // wants decl-shaped single-document output, and a header/arity directive (@inherits/@implements/
        // @typeparam) can't be reconciled across the two partial halves by the move-based partition.
        if (!codeDocument.FileKind.IsComponent() ||
            codeDocument.CodeGenerationOptions.SuppressPrimaryMethodBody ||
            HasUnsplittableDocumentDirective(documentNode))
        {
            return codeDocument;
        }

        // Before classification the class body still lives as raw C# interleaved with markup IR under the
        // code-block directives -- the resolution-independent input the split reasons about. A class body
        // with no markup needs no split and is filtered here without parsing anything.
        var children = CollectCodeBlockChildren(documentNode);
        if (!ContainsMarkup(children))
        {
            return codeDocument;
        }

        // Decide the split. A fallback (a markup property, an unsupported node, a directive, or
        // unrecoverable syntax) leaves the document untouched for the late single-document lowering.
        var analysis = MarkupSplitter.BuildAnalysisDocument(children);
        var parseOptions = codeDocument.ParserOptions.CSharpParseOptions ?? CSharpParseOptions.Default;
        if (MarkupSplitter.ClassifyFromAnalysis(analysis, parseOptions, cancellationToken) is not SplitDecision.SplitPlan plan)
        {
            return codeDocument;
        }

        using var declPiecesBuilder = new PooledArrayBuilder<IntermediateNode>();
        using var implPiecesBuilder = new PooledArrayBuilder<IntermediateNode>();

        foreach (var member in plan.Members)
        {
            foreach (var piece in member.DeclPieces)
            {
                declPiecesBuilder.Add(piece);
            }

            foreach (var piece in member.ImplPieces)
            {
                implPiecesBuilder.Add(piece);
            }
        }

        // Produce the decl half now, before tag-helper resolution: it is markup-free and depends only on
        // user source, which is what lets tag-helper discovery consume it early and stay incremental.
        var declCSharp = LowerDeclDocument(codeDocument, documentNode, declPiecesBuilder.ToImmutable(), cancellationToken);

        // Rewrite the working node into the impl half; it flows through the rest of the pipeline (including
        // tag-helper resolution), and the final C# lowering phase emits it directly.
        MakeImplInPlace(documentNode, implPiecesBuilder.ToImmutable());
        documentNode.IsSplitImplDocument = true;

        var result = codeDocument.WithDocumentNode(documentNode);
        if (declCSharp is not null)
        {
            result = result.WithDeclCSharpDocument(declCSharp);
        }

        return result;
    }

    // The ordered children of every @code/@functions block in the document, in source order. Before
    // classification these code-block directives hold the user's class body verbatim -- the same nodes
    // FunctionsDirectivePass later moves into the primary class -- so this is the class body the split
    // reasons about, gathered without depending on resolution or the classified structure.
    private static ImmutableArray<IntermediateNode> CollectCodeBlockChildren(DocumentIntermediateNode documentNode)
    {
        using var directives = new PooledArrayBuilder<IntermediateNodeReference<DirectiveIntermediateNode>>();

        documentNode.CollectDirectiveReferences(FunctionsDirective.Directive, ref directives.AsRef());
        documentNode.CollectDirectiveReferences(ComponentCodeDirective.Directive, ref directives.AsRef());

        if (directives.Count == 0)
        {
            return [];
        }

        directives.Sort(static (a, b) =>
            Comparer<int?>.Default.Compare(a.Node.Source?.AbsoluteIndex, b.Node.Source?.AbsoluteIndex));

        using var children = new PooledArrayBuilder<IntermediateNode>();

        foreach (var directive in directives)
        {
            foreach (var child in directive.Node.Children)
            {
                children.Add(child);
            }
        }

        return children.ToImmutableAndClear();
    }

    // True if any collected class-body node (or a descendant) is a markup transition -- a node that can
    // only be lowered after tag-helper resolution. O(nodes) with no parsing, so a markup-free class body
    // is filtered cheaply.
    private static bool ContainsMarkup(ImmutableArray<IntermediateNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (MarkupSplitter.IsMarkupNode(node) || ContainsMarkup(node))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMarkup(IntermediateNode node)
    {
        foreach (var child in node.Children)
        {
            if (MarkupSplitter.IsMarkupNode(child) || ContainsMarkup(child))
            {
                return true;
            }
        }

        return false;
    }

    // Header- and arity-shaping directives (@inherits, @implements, @typeparam) must be consistent across
    // the two partial halves. The move-based partition does not reconcile them -- it neither strips the
    // base type and interfaces from the impl header nor duplicates the type parameters onto both halves --
    // so a component that combines one of them with class-body markup takes the single-document path.
    private static bool HasUnsplittableDocumentDirective(DocumentIntermediateNode documentNode)
    {
        foreach (var child in documentNode.Children)
        {
            if (child is DirectiveIntermediateNode { DirectiveName: "inherits" or "implements" or "typeparam" })
            {
                return true;
            }
        }

        return false;
    }

    // Builds a markup-free decl document from the surface parts of the raw tree and lowers it to C# by
    // running the engine's own classifier, directive-classifier, and decl C# lowering phases on it. Reusing
    // the engine's phases (rather than a separate configuration) keeps the decl bytes identical to what the
    // single-document path would produce for the same surface.
    private RazorCSharpDocument? LowerDeclDocument(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode source,
        ImmutableArray<IntermediateNode> declPieces,
        CancellationToken cancellationToken)
    {
        var declNode = new DocumentIntermediateNode { Options = source.Options };

        foreach (var child in source.Children)
        {
            // Usings are needed by both halves; duplicate them so classifying decl doesn't reparent the
            // impl's copies.
            if (child is UsingDirectiveIntermediateNode usingDirective)
            {
                declNode.Children.Add(CloneUsing(usingDirective));
            }
        }

        if (declPieces.Length > 0)
        {
            var codeDirective = new DirectiveIntermediateNode
            {
                DirectiveName = ComponentCodeDirective.Directive.Directive,
                Directive = ComponentCodeDirective.Directive,
            };

            foreach (var piece in declPieces)
            {
                codeDirective.Children.Add(piece);
            }

            declNode.Children.Add(codeDirective);
        }

        var declCodeDoc = codeDocument.WithDocumentNode(declNode);

        // The split runs before the tag-helper rewrite phase, so the rewritten syntax tree isn't set yet.
        // The decl half is markup-free and resolution-independent, so its rewritten tree is just the parsed
        // syntax tree; seed it here so consumers that read it from the decl document (e.g. cohost diagnostic
        // filtering, which walks it for using directives) don't see a null.
        if (codeDocument.GetTagHelperRewrittenSyntaxTree() is null &&
            codeDocument.GetSyntaxTree() is { } syntaxTree)
        {
            declCodeDoc = declCodeDoc.WithTagHelperRewrittenSyntaxTree(syntaxTree);
        }

        declCodeDoc = Engine.Phases.OfType<IRazorDocumentClassifierPhase>().Single().Execute(declCodeDoc, cancellationToken);
        declCodeDoc = Engine.Phases.OfType<IRazorDirectiveClassifierPhase>().Single().Execute(declCodeDoc, cancellationToken);
        declCodeDoc = Engine.Phases.OfType<DefaultRazorDeclCSharpLoweringPhase>().Single().Execute(declCodeDoc, cancellationToken);

        return declCodeDoc.GetCSharpDocument(declarationDocument: true);
    }

    // Rewrites the working document into the impl half: the markup-bearing @code members replace the
    // class-body content of a single consolidated code-block directive; every other code-block directive
    // is dropped. The render body and any surface directives (e.g. @inject) remain in the impl half.
    private static void MakeImplInPlace(DocumentIntermediateNode documentNode, ImmutableArray<IntermediateNode> implPieces)
    {
        DirectiveIntermediateNode? primaryCodeDirective = null;

        using var toRemove = new PooledArrayBuilder<IntermediateNode>();

        foreach (var child in documentNode.Children)
        {
            if (child is DirectiveIntermediateNode directive && IsCodeBlockDirective(directive))
            {
                if (primaryCodeDirective is null)
                {
                    primaryCodeDirective = directive;
                }
                else
                {
                    toRemove.Add(directive);
                }
            }
        }

        foreach (var node in toRemove)
        {
            documentNode.Children.Remove(node);
        }

        if (primaryCodeDirective is not null)
        {
            primaryCodeDirective.Children.Clear();

            foreach (var piece in implPieces)
            {
                primaryCodeDirective.Children.Add(piece);
            }

            if (implPieces.Length == 0)
            {
                documentNode.Children.Remove(primaryCodeDirective);
            }
        }
    }

    private static bool IsCodeBlockDirective(DirectiveIntermediateNode directive)
        => directive.Directive?.Kind == DirectiveKind.CodeBlock;

    private static UsingDirectiveIntermediateNode CloneUsing(UsingDirectiveIntermediateNode node)
        => new()
        {
            Content = node.Content,
            HasExplicitSemicolon = node.HasExplicitSemicolon,
            AppendLineDefaultAndHidden = node.AppendLineDefaultAndHidden,
            Source = node.Source,
            IsImported = node.IsImported,
        };
}
