// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// For Razor components whose primary method body is not being suppressed, this phase produces
/// the "decl" C# document and stashes it on <see cref="RazorCodeDocument"/> via
/// <c>WithDeclCSharpDocument</c>. The matching "impl" half is produced later by
/// <see cref="DefaultRazorCSharpLoweringPhase"/>; both halves are emitted as <c>partial</c>
/// so they rejoin at compile time.
/// </summary>
/// <remarks>
/// <para>
/// The decl document carries the user's component API surface: the partial class declaration with
/// base type / interfaces / type parameters / user-authored class-level attributes (route,
/// layout), all properties / fields / parameters / inject members / sibling methods, and any
/// document-level metadata (source-checksum attributes, etc.). It deliberately omits the render
/// method body and any compiler-synthesized plumbing (marked with
/// <see cref="IntermediateNode.IsSynthesizedHelper"/>) so it depends only on user source -- not
/// on tag helper resolution -- and can therefore run earlier in the pipeline than the final
/// C# lowering phase.
/// </para>
/// <para>
/// The split affects only the generated C# (<c>GetDeclCSharpDocument()</c> gives the decl half;
/// <see cref="RazorCodeDocument.GetImplCSharpDocument"/> still returns whatever the final lowering
/// phase produces, which becomes the impl half in <see cref="DefaultRazorCSharpLoweringPhase"/>).
/// The original <see cref="DocumentIntermediateNode"/> is left untouched -- the decl synthetic
/// spine shares children with the original by reference rather than mutating it -- so callers
/// that walk the IR tree (e.g. <c>RazorCodeDocumentExtensions.ComponentNamespaceMatches</c>,
/// <c>ExtractToCodeBehindCodeActionResolver</c>) continue to see its pre-split shape.
/// </para>
/// <para>
/// For documents that aren't splittable (non-components, suppressed primary method body, or any
/// document missing the primary structure) this phase is a no-op; <c>GetDeclCSharpDocument()</c>
/// stays null and <see cref="DefaultRazorCSharpLoweringPhase"/> falls through to the prior
/// single-file behavior.
/// </para>
/// </remarks>
internal sealed class DefaultRazorDeclCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        var target = documentNode.Target;
        if (target == null)
        {
            var message = Resources.FormatDocumentMissingTarget(
                documentNode.DocumentKind,
                nameof(CodeTarget),
                nameof(DocumentIntermediateNode.Target));
            throw new InvalidOperationException(message);
        }

        // Skip the split for any document that shouldn't be split:
        // - Non-components: the split is component-only.
        // - SuppressPrimaryMethodBody (e.g. ProcessDeclarationOnly): caller wants the
        //   decl-shaped output as the single C# document, which the existing lowering
        //   phase will produce from the original tree.
        if (codeDocument.FileKind != RazorFileKind.Component ||
            codeDocument.CodeGenerationOptions.SuppressPrimaryMethodBody)
        {
            return codeDocument;
        }

        // Bail out if the document is missing the primary structure we'd need to split.
        // The find helpers can return null and we'd rather fall back to the single-file
        // path than crash.
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        var primaryNamespace = documentNode.FindPrimaryNamespace();
        if (primaryClass is null || renderMethod is null || primaryNamespace is null)
        {
            return codeDocument;
        }

        // Build the decl synthetic tree: shallow-clone the documentNode → primaryNamespace
        // → primaryClass spine, share every other child (renderMethod's siblings such as
        // @code blocks, secondary classes, document-level attribute nodes) by reference,
        // and skip nodes that belong in the impl half:
        //   - renderMethod
        //   - any IsSynthesizedHelper node (compiler plumbing)
        //   - IsGenericTyped namespaces (type-inference helpers)
        var declDocNode = RazorCSharpDocumentWriter.CloneContainer(documentNode);
        var declNamespace = RazorCSharpDocumentWriter.CloneContainer(primaryNamespace);
        var declClass = RazorCSharpDocumentWriter.CloneContainer(primaryClass);

        foreach (var classChild in primaryClass.Children)
        {
            if (classChild == renderMethod || classChild.IsSynthesizedHelper)
            {
                continue;
            }

            declClass.Children.Add(classChild);
        }

        foreach (var nsChild in primaryNamespace.Children)
        {
            if (nsChild.IsSynthesizedHelper)
            {
                continue;
            }

            declNamespace.Children.Add(nsChild == primaryClass ? declClass : nsChild);
        }

        foreach (var docChild in documentNode.Children)
        {
            if (docChild is NamespaceDeclarationIntermediateNode { IsGenericTyped: true })
            {
                continue;
            }

            declDocNode.Children.Add(docChild == primaryNamespace ? declNamespace : docChild);
        }

        // Seed the decl root with the full diagnostic set (deduped by checksum) so any
        // diagnostics attached to documentNode / primaryNamespace / primaryClass themselves
        // -- which aren't reachable from the synthetic clone -- still surface on the decl
        // document.
        foreach (var diagnostic in documentNode.GetAllDiagnostics())
        {
            declDocNode.AddDiagnostic(diagnostic);
        }

        // The decl writer suppresses diagnostic collection because every diagnostic on the
        // decl tree is also reachable from the original tree the impl half lowers from;
        // letting both writers report would surface every Razor-detected issue twice.
        var declDocument = RazorCSharpDocumentWriter.Write(declDocNode, codeDocument, reportDiagnostics: false, cancellationToken, isDeclarationDocument: true);

        return codeDocument.WithDeclCSharpDocument(declDocument);
    }
}
