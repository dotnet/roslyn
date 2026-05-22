// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorDirectiveClassifierPhase : RazorEnginePhaseBase, IRazorDirectiveClassifierPhase
{
    public ImmutableArray<IRazorDirectiveClassifierPass> Passes { get; private set; }

    protected override void OnInitialized()
    {
        Passes = Engine.GetFeatures<IRazorDirectiveClassifierPass>().OrderByAsArray(static x => x.Order);
    }

    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        foreach (var pass in Passes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            pass.Execute(codeDocument, documentNode, cancellationToken);
        }

        return codeDocument.WithDocumentNode(documentNode);
    }
}
