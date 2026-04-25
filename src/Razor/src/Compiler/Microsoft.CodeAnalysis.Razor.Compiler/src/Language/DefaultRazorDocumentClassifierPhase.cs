// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DefaultRazorDocumentClassifierPhase : RazorEnginePhaseBase, IRazorDocumentClassifierPhase
{
    public ImmutableArray<IRazorDocumentClassifierPass> Passes { get; private set; }

    protected override void OnInitialized()
    {
        Passes = Engine.GetFeatures<IRazorDocumentClassifierPass>().OrderByAsArray(p => p.Order);
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
