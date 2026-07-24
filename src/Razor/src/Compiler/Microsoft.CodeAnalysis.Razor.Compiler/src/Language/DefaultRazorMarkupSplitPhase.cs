// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Splits a component into decl and impl halves after directive classification but before tag-helper
/// resolution. It partitions the classified primary class body into the markup-free "decl" surface (the
/// tag-helper descriptor: base type, interfaces, type parameters, parameters/fields/methods) and the
/// markup-bearing "impl" (the render method plus any markup-bearing methods). The decl subtree is stashed
/// on the document node for <see cref="DefaultRazorDeclCSharpLoweringPhase"/> to lower before discovery;
/// the working node is rewritten into the impl half and flows through the rest of the pipeline.
/// </summary>
/// <remarks>
/// Running before tag-helper resolution is the point: the decl half is markup-free and depends only on
/// user source, so tag-helper discovery can consume it early and stay incremental. A component with no
/// class-body markup still splits -- its whole body is the decl, its render method the impl. A component
/// whose body has markup the analysis can't route safely (a markup property, an unsupported member, an
/// <c>@inject</c>, a preprocessor directive, or unrecoverable syntax), or one carrying a header/arity
/// directive (<c>@inherits</c>/<c>@implements</c>/<c>@typeparam</c>) -- whose base type, interfaces, or
/// type parameters a move-based partition would leave duplicated on the impl header -- is left as a single
/// document for the fallback lowering.
/// </remarks>
internal sealed class DefaultRazorMarkupSplitPhase : RazorEnginePhaseBase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        // The split applies only to components: a component import or legacy .cshtml has no component
        // surface to partition, and a suppressed primary method body is meant to be a single document.
        if (codeDocument.FileKind != RazorFileKind.Component ||
            codeDocument.CodeGenerationOptions.SuppressPrimaryMethodBody)
        {
            return codeDocument;
        }

        // Partitioning needs the classified primary structure -- the primary class, its render method, and
        // the namespace. A document without them isn't a well-formed component to split, so it lowers as a
        // single file.
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        var primaryNamespace = documentNode.FindPrimaryNamespace();
        if (primaryClass is null || renderMethod is null || primaryNamespace is null)
        {
            return codeDocument;
        }

        // A header/arity directive (@inherits/@implements/@typeparam) puts a base type, interfaces, or
        // type parameters on the class header. A move-based partition leaves that header on the impl half
        // as well, so combining one with class-body markup would emit the header on both partials; such a
        // document lowers as a single file instead.
        if (HasUnsplittableDocumentDirective(documentNode))
        {
            return codeDocument;
        }

        // Decide the split over the classified class body. Only an unroutable body (fallback) stays a
        // single document; NoSplit and SplitPlan both produce a decl.
        var decision = MarkupSplitter.Split(primaryClass, renderMethod, codeDocument.ParserOptions);
        if (decision is SplitDecision.SplitFallback)
        {
            return codeDocument;
        }

        var plan = decision as SplitDecision.SplitPlan;

        // BuildDeclDocument captures the decl's view of the class body first because MakeImplInPlace then
        // rewrites that shared primary class in place. The decl subtree keeps its leaf nodes by reference,
        // so DefaultRazorDeclCSharpLoweringPhase lowers it while those nodes still hold their classified
        // form, ahead of the resolution and optimization passes that mutate them.
        var declDocNode = BuildDeclDocument(documentNode, primaryNamespace, primaryClass, renderMethod, plan);
        MakeImplInPlace(primaryClass, renderMethod, plan);
        StripClassAttributesFromImpl(documentNode, primaryNamespace);

        documentNode.DeclDocumentNode = declDocNode;

        return codeDocument.WithDocumentNode(documentNode);
    }

    // Builds the markup-free decl document: a synthetic document -> namespace -> class spine that shares
    // every kept leaf node with the original by reference. A split plan contributes each member's decl
    // pieces; without a plan (a markup-free body) every non-render, non-synthesized member stays in decl.
    private static DocumentIntermediateNode BuildDeclDocument(
        DocumentIntermediateNode documentNode,
        NamespaceDeclarationIntermediateNode primaryNamespace,
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        SplitDecision.SplitPlan? plan)
    {
        var declDocNode = RazorCSharpDocumentWriter.CloneContainer(documentNode);
        var declNamespace = RazorCSharpDocumentWriter.CloneContainer(primaryNamespace);
        var declClass = RazorCSharpDocumentWriter.CloneContainer(primaryClass);

        if (plan is not null)
        {
            foreach (var member in plan.Members)
            {
                foreach (var piece in member.DeclPieces)
                {
                    declClass.Children.Add(piece);
                }
            }
        }
        else
        {
            foreach (var classChild in primaryClass.Children)
            {
                if (ReferenceEquals(classChild, renderMethod) || classChild.IsSynthesizedHelper)
                {
                    continue;
                }

                declClass.Children.Add(classChild);
            }
        }

        foreach (var nsChild in primaryNamespace.Children)
        {
            if (nsChild.IsSynthesizedHelper)
            {
                continue;
            }

            declNamespace.Children.Add(ReferenceEquals(nsChild, primaryClass) ? declClass : nsChild);
        }

        foreach (var docChild in documentNode.Children)
        {
            // Type-inference helper namespaces are compiler plumbing for the impl half only.
            if (docChild is NamespaceDeclarationIntermediateNode { IsGenericTyped: true })
            {
                continue;
            }

            declDocNode.Children.Add(ReferenceEquals(docChild, primaryNamespace) ? declNamespace : docChild);
        }

        // Diagnostics attached to the document / namespace / class nodes themselves aren't reachable from
        // the synthetic clone; surface them on the decl root (deduped by checksum).
        foreach (var diagnostic in documentNode.GetAllDiagnostics())
        {
            declDocNode.AddDiagnostic(diagnostic);
        }

        return declDocNode;
    }

    // Rewrites the primary class into the impl half in place: keep the render method and compiler-
    // synthesized helpers, drop the decl-only surface, and (for a split plan) append the markup-bearing
    // pieces lifted from the class body.
    private static void MakeImplInPlace(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        SplitDecision.SplitPlan? plan)
    {
        using var implChildren = new PooledArrayBuilder<IntermediateNode>();

        implChildren.Add(renderMethod);

        foreach (var child in primaryClass.Children)
        {
            if (child.IsSynthesizedHelper)
            {
                implChildren.Add(child);
            }
        }

        if (plan is not null)
        {
            foreach (var member in plan.Members)
            {
                foreach (var piece in member.ImplPieces)
                {
                    implChildren.Add(piece);
                }
            }
        }

        primaryClass.Children.Clear();

        foreach (var child in implChildren)
        {
            primaryClass.Children.Add(child);
        }
    }

    // True if the document carries a header/arity directive (@inherits/@implements/@typeparam). Walks
    // descendants because classification can nest these directives under the namespace or class.
    private static bool HasUnsplittableDocumentDirective(DocumentIntermediateNode documentNode)
    {
        foreach (var directive in documentNode.FindDescendantNodes<DirectiveIntermediateNode>())
        {
            if (directive.DirectiveName is "inherits" or "implements" or "typeparam")
            {
                return true;
            }
        }

        return false;
    }

    // Class-decoration nodes (@layout -> [Layout], @attribute -> [...], @page -> [Route]) lower to
    // namespace- or document-level nodes that decorate the class. They are the decl half's public surface
    // and are shared into the decl subtree; the same node kept in the impl half decorates the impl partial
    // too, emitting the attribute twice on the combined type -- a CS0579 for a single-instance attribute,
    // a duplicate route for @page (RouteAttribute allows multiples, so it compiles but registers twice).
    // The class body, usings, directives, and synthesized helpers stay in the impl.
    private static void StripClassAttributesFromImpl(
        DocumentIntermediateNode documentNode,
        NamespaceDeclarationIntermediateNode primaryNamespace)
    {
        RemoveClassAttributeChildren(primaryNamespace.Children);
        RemoveClassAttributeChildren(documentNode.Children);
    }

    private static void RemoveClassAttributeChildren(IntermediateNodeCollection children)
    {
        for (var i = children.Count - 1; i >= 0; i--)
        {
            // @layout/@attribute lower to a CSharpCodeIntermediateNode, @page to a RouteAttributeExtensionNode.
            // A compiler-synthesized decoration (e.g. the @rendermode attribute helper) is impl-half plumbing
            // and isn't shared into the decl, so it stays in the impl.
            if (children[i] is CSharpCodeIntermediateNode { IsSynthesizedHelper: false } or RouteAttributeExtensionNode)
            {
                children.RemoveAt(i);
            }
        }
    }
}