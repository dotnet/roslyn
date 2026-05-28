// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
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

        // Fork: when the decl phase produced its half, this phase produces the matching
        // impl half -- a minimal partial class containing just the render method body and
        // any compiler-synthesized plumbing. Otherwise (non-component, suppressed primary
        // method body, malformed primary structure, or the decl phase didn't run for any
        // other reason) fall through to the original single-file lowering path.
        if (codeDocument.GetDeclCSharpDocument() is not null &&
            TryWriteImplDocument(documentNode, codeDocument, cancellationToken, out var implDocument))
        {
            return codeDocument.WithImplCSharpDocument(implDocument);
        }

        var csharpDocument = RazorCSharpDocumentWriter.Write(documentNode, codeDocument, cancellationToken: cancellationToken);
        return codeDocument.WithImplCSharpDocument(csharpDocument);
    }

    private static bool TryWriteImplDocument(
        DocumentIntermediateNode documentNode,
        RazorCodeDocument codeDocument,
        CancellationToken cancellationToken,
        out RazorCSharpDocument implDocument)
    {
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        var primaryNamespace = documentNode.FindPrimaryNamespace();

        // Defensive: splittability was already verified by the decl phase before it
        // produced a decl document, so this should not happen in practice. If a caller
        // bypasses the decl phase or the IR shape changes between phases, fall through
        // to the single-file path rather than crash.
        if (primaryClass is null || renderMethod is null || primaryNamespace is null)
        {
            implDocument = null!;
            return false;
        }

        // Build the impl synthetic tree: brand-new spine containing just the namespace,
        // its using directives, and a partial class wrapping renderMethod plus any
        // IsSynthesizedHelper nodes lifted from primaryClass / primaryNamespace. The
        // namespace-level walk preserves original order relative to primaryClass so an
        // attribute decoration that preceded the class in the original tree continues
        // to do so. IsGenericTyped helper namespaces are also lifted as siblings of the
        // primary namespace.
        var usings = primaryNamespace.FindDescendantNodes<UsingDirectiveIntermediateNode>();
        var implDocNode = RazorCSharpDocumentWriter.CloneContainer(documentNode);
        var implNamespace = RazorCSharpDocumentWriter.CloneContainer(primaryNamespace);
        var implClass = RazorCSharpDocumentWriter.CloneContainer(primaryClass);

        implClass.Children.Add(renderMethod);
        foreach (var classChild in primaryClass.Children)
        {
            if (classChild.IsSynthesizedHelper)
            {
                implClass.Children.Add(classChild);
            }
        }

        foreach (var usingDirective in usings)
        {
            implNamespace.Children.Add(usingDirective);
        }

        foreach (var nsChild in primaryNamespace.Children)
        {
            if (nsChild == primaryClass)
            {
                implNamespace.Children.Add(implClass);
            }
            else if (nsChild.IsSynthesizedHelper)
            {
                implNamespace.Children.Add(nsChild);
            }
        }

        implDocNode.Children.Add(implNamespace);

        foreach (var docChild in documentNode.Children)
        {
            if (docChild is NamespaceDeclarationIntermediateNode { IsGenericTyped: true } genericNs)
            {
                implDocNode.Children.Add(genericNs);
            }
        }

        // Seed the impl root with the full diagnostic set (deduped by checksum) so any
        // diagnostics attached to documentNode / primaryNamespace / primaryClass themselves
        // -- which aren't reachable from the synthetic clone -- still surface on the impl
        // document.
        foreach (var diagnostic in documentNode.GetAllDiagnostics())
        {
            implDocNode.AddDiagnostic(diagnostic);
        }

        implDocument = RazorCSharpDocumentWriter.Write(implDocNode, codeDocument, cancellationToken: cancellationToken);
        return true;
    }
}