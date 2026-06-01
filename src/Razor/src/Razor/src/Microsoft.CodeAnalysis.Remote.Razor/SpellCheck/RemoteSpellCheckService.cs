// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteSpellCheckService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteSpellCheckService
{
    internal sealed class Factory : FactoryBase<IRemoteSpellCheckService>
    {
        protected override IRemoteSpellCheckService CreateService(in ServiceArgs args)
            => new RemoteSpellCheckService(in args);
    }

    private readonly ISpellCheckService _spellCheckService = args.ExportProvider.GetExportedValue<ISpellCheckService>();

    public ValueTask<int[]> GetSpellCheckRangeTriplesAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetSpellCheckRangeTriplesAsync(context, cancellationToken),
            cancellationToken);

    private async ValueTask<int[]> GetSpellCheckRangeTriplesAsync(RemoteDocumentContext context, CancellationToken cancellationToken)
    {
        return await _spellCheckService.GetSpellCheckRangeTriplesAsync(context, cancellationToken).ConfigureAwait(false);
    }
}
