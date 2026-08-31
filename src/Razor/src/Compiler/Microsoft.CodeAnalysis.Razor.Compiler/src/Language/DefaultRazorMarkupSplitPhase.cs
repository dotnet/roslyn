// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
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
/// The phase is always registered but only does anything when
/// <c>RazorCodeGenerationOptions.EnableMarkupSplit</c> is set; it is off by default so a host that reads
/// only the impl document keeps getting the whole component as a single file.
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

        // The split is opt-in: it produces a second (decl) C# document that only a host consuming both
        // halves -- the Razor source generator -- knows how to emit. A host that reads just the
        // implementation document (e.g. the SDK's classic, non-source-generator compilation) would
        // otherwise silently drop everything the split moved into the decl half.
        if (!codeDocument.CodeGenerationOptions.EnableMarkupSplit)
        {
            return codeDocument;
        }

        // Only components are split. A component import or legacy .cshtml has no component surface to
        // partition.
        if (codeDocument.FileKind != RazorFileKind.Component)
        {
            return codeDocument;
        }

        // Partitioning needs the classified primary structure -- the primary class, its render method, and
        // the namespace.
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        var primaryNamespace = documentNode.FindPrimaryNamespace();

        // Fallback discovery is keyed by the component's type name, so it needs a classified primary class.
        // Classification creates one unconditionally for a component file; without it there is no component
        // type to discover or split, so leave the document untouched rather than dereference a null name.
        if (primaryClass is null)
        {
            return codeDocument;
        }

        // A component whose primary method body is suppressed, or that lacks a render method or namespace,
        // can't be split here; it routes to fallback discovery keyed by its type name and builds no
        // pre-compilation shell.
        if (codeDocument.CodeGenerationOptions.SuppressPrimaryMethodBody ||
            renderMethod is null || primaryNamespace is null)
        {
            return RouteToFallbackDiscovery(shellDecl: null);
        }

        // A header/arity directive (@inherits/@implements/@typeparam) puts a base type, interfaces, or
        // type parameters on the class header. A move-based partition leaves that header on the impl half
        // as well, so combining one with class-body markup would emit the header on both partials; such a
        // document lowers as a single file instead and its descriptor comes from fallback discovery.
        if (HasUnsplittableDocumentDirective(documentNode))
        {
            return RouteToFallbackDiscovery(BuildStubDeclDocument(documentNode, primaryNamespace, primaryClass));
        }

        // Decide the split over the classified class body. Only an unroutable body (fallback) stays a
        // single document; NoSplit and SplitPlan both produce a decl.
        var decision = MarkupSplitter.Split(primaryClass, renderMethod, codeDocument.ParserOptions);
        if (decision is SplitDecision.SplitFallback)
        {
            return RouteToFallbackDiscovery(BuildStubDeclDocument(documentNode, primaryNamespace, primaryClass));
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

        // Routes the document to fallback discovery: records its namespace-qualified type name -- the
        // discovery key the source generator matches against tag-helper descriptor names -- and stashes the
        // type-shell decl (null when there is no referenceable type to build one for). A document only
        // routes here after classification, which creates the primary class unconditionally, so a class
        // name is always present; the name carries no generic arity (type parameters are held separately),
        // matching the descriptor form after arity is stripped.
        RazorCodeDocument RouteToFallbackDiscovery(DocumentIntermediateNode? shellDecl)
        {
            Debug.Assert(primaryClass?.Name is not null, "A fallback component is missing its classified primary class.");

            documentNode.DeclDocumentNode = shellDecl;
            documentNode.FallbackComponentTypeName = primaryNamespace?.Name is { } namespaceName
                ? $"{namespaceName}.{primaryClass.Name}"
                : primaryClass.Name;

            // Retain the full discoverable decl so the source generator can produce this fallback
            // component's descriptor from the already-parsed document rather than re-parsing the source
            // through a separate declaration engine. Built exactly like a split component's decl over the
            // whole class body (no plan), it is never emitted to pre-compilation -- only its syntax tree
            // is fed into slow discovery. Needs the render method (to exclude) and namespace; when either
            // is absent the generator falls back to re-parsing.
            if (renderMethod is not null && primaryNamespace is not null)
            {
                documentNode.FallbackDiscoveryDeclDocumentNode =
                    BuildDeclDocument(documentNode, primaryNamespace, primaryClass, renderMethod, plan: null);
            }

            return codeDocument.WithDocumentNode(documentNode);
        }
    }

    // Builds a bodiless "type shell" decl for a component the split left unsplit: the same synthetic
    // document -> namespace -> class spine as the full decl, but the class keeps only its name,
    // modifiers, and type parameters (names only, for generic arity) -- no base type, interfaces,
    // members, or type-parameter constraints. Emitted to pre-compilation so the component's type (and its
    // nested types, which qualify from the resolved outer type) resolve for a split component that
    // references them in C#, while carrying no discoverable surface -- no base means it isn't a
    // component, so tag-helper discovery skips it and the declaration engine owns its real descriptor.
    private static DocumentIntermediateNode BuildStubDeclDocument(
        DocumentIntermediateNode documentNode,
        NamespaceDeclarationIntermediateNode primaryNamespace,
        ClassDeclarationIntermediateNode primaryClass)
    {
        var stubDocNode = RazorCSharpDocumentWriter.CloneContainer(documentNode);

        // The shell's text must depend only on the declaration surface so it stays byte-stable across
        // markup edits, keeping pre-compilation (and therefore discovery) cached -- same reason the full
        // decl suppresses its checksum.
        if (stubDocNode.Options is { SuppressChecksum: false } stubOptions)
        {
            stubDocNode.Options = stubOptions.WithFlags(suppressChecksum: true);
        }

        var stubNamespace = RazorCSharpDocumentWriter.CloneContainer(primaryNamespace);
        var stubClass = RazorCSharpDocumentWriter.CloneContainer(primaryClass);
        stubClass.BaseType = null;
        stubClass.Interfaces = [];
        stubClass.TypeParameters = StripTypeParameterConstraints(primaryClass.TypeParameters);

        stubNamespace.Children.Add(stubClass);
        stubDocNode.Children.Add(stubNamespace);

        return stubDocNode;
    }

    // Keeps type-parameter names for generic arity while dropping constraints, whose types (e.g. a
    // constraint on the component's own nested type) the bodiless shell doesn't declare. Partial classes
    // allow constraints on the impl declaration alone, so the shell can omit them.
    private static ImmutableArray<TypeParameter> StripTypeParameterConstraints(ImmutableArray<TypeParameter> typeParameters)
    {
        if (typeParameters.IsEmpty)
        {
            return typeParameters;
        }

        var builder = ImmutableArray.CreateBuilder<TypeParameter>(typeParameters.Length);
        foreach (var typeParameter in typeParameters)
        {
            builder.Add(new TypeParameter(typeParameter.Name.Content));
        }

        return builder.MoveToImmutable();
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

        // Suppress the decl's #pragma checksum so its text tracks the declaration surface rather than the
        // raw source bytes: a markup-only edit then leaves the decl byte-identical, keeping it in the
        // pre-compilation source cache so tag-helper discovery stays cached. The impl keeps its checksum.
        if (declDocNode.Options is { SuppressChecksum: false } declOptions)
        {
            declDocNode.Options = declOptions.WithFlags(suppressChecksum: true);
        }

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