// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Lowers the markup-free decl subtree that <see cref="DefaultRazorMarkupSplitPhase"/> stashed on the
/// document node into the "decl" C# document -- the component's public API surface -- and stashes it via
/// <c>WithDeclCSharpDocument</c>. Running before tag-helper discovery freezes the decl text ahead of
/// resolution, which is what lets the source generator feed it into the input compilation instead of
/// running a second declaration compilation. The matching "impl" half is produced by
/// <see cref="DefaultRazorCSharpLoweringPhase"/>; both are emitted as <c>partial</c> so they rejoin at
/// compile time. For anything the split phase left unsplit (a non-component, a suppressed primary body,
/// or a fallback component) no decl subtree is stashed and this phase is a no-op;
/// <see cref="DefaultRazorCSharpLoweringPhase"/> then produces the whole document as a single file.
/// </summary>
internal sealed class DefaultRazorDeclCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        if (documentNode.DeclDocumentNode is not { } declDocNode)
        {
            return codeDocument;
        }

        // The decl writer suppresses diagnostic collection because every diagnostic on the decl tree is
        // also reachable from the impl tree the final lowering phase writes; letting both report would
        // surface every Razor-detected issue twice.
        var declDocument = RazorCSharpDocumentWriter.Write(
            declDocNode,
            codeDocument,
            reportDiagnostics: false,
            isDeclarationDocument: true,
            cancellationToken: cancellationToken);

        return codeDocument.WithDeclCSharpDocument(declDocument);
    }
}
