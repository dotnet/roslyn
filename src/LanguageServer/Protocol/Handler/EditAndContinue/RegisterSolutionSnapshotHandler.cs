// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.EditAndContinue;

internal readonly record struct LspSolutionSnapshotId([property: JsonPropertyName("id")] int Id);

[ExportCSharpVisualBasicStatelessLspService(typeof(RegisterSolutionSnapshotHandler)), Shared]
[Method("workspace/_vs_registerSolutionSnapshot")]
internal sealed class RegisterSolutionSnapshotHandler : ILspServiceRequestHandler<LspSolutionSnapshotId>
{
    private readonly ISolutionSnapshotRegistry _registry;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RegisterSolutionSnapshotHandler(ISolutionSnapshotRegistry registry)
    {
        _registry = registry;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public Task<LspSolutionSnapshotId> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);
        var id = _registry.RegisterSolutionSnapshot(context.Solution);
        return Task.FromResult(new LspSolutionSnapshotId(id.Id));
    }
}
