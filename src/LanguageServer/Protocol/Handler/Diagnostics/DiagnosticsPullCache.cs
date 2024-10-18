// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract partial class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn> where TDiagnosticsParams : IPartialResultParams<TReport>
{
    internal record struct DiagnosticsRequestState(Project Project, int GlobalStateVersion, RequestContext Context, IDiagnosticSource DiagnosticSource);

    /// <summary>
    /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance 
    /// changed. The <see cref="VersionStamp"/> is produced by <see cref="Project.GetDependentVersionAsync(CancellationToken)"/> while the 
    /// <see cref="Checksum"/> is produced by <see cref="Project.GetDependentChecksumAsync(CancellationToken)"/>.  The former is faster
    /// and works well for us in the normal case.  The latter still allows us to reuse diagnostics when changes happen that
    /// update the version stamp but not the content (for example, forking LSP text).
    /// </summary>
    internal class DiagnosticsPullCache(string uniqueKey) : VersionedPullCache<(int globalStateVersion, VersionStamp? dependentVersion), (int globalStateVersion, Checksum dependentChecksum), DiagnosticsRequestState, DiagnosticData>(uniqueKey)
    {
        public override async Task<(int globalStateVersion, VersionStamp? dependentVersion)> ComputeCheapVersionAsync(DiagnosticsRequestState state, CancellationToken cancellationToken)
        {
            return (state.GlobalStateVersion, await state.Project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false));
        }

        public override async Task<(int globalStateVersion, Checksum dependentChecksum)> ComputeExpensiveVersionAsync(DiagnosticsRequestState state, CancellationToken cancellationToken)
        {
            return (state.GlobalStateVersion, await state.Project.GetDependentChecksumAsync(cancellationToken).ConfigureAwait(false));
        }

        public override async Task<ImmutableArray<DiagnosticData>> ComputeDataAsync(DiagnosticsRequestState state, CancellationToken cancellationToken)
        {
            var diagnostics = await state.DiagnosticSource.GetDiagnosticsAsync(state.Context, cancellationToken).ConfigureAwait(false);
            state.Context.TraceInformation($"Found {diagnostics.Length} diagnostics for {state.DiagnosticSource.ToDisplayString()}");
            return diagnostics;
        }
    }
}
