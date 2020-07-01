// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class ActiveStatementSpanProvider : IActiveStatementSpanProvider
    {
        private readonly HostWorkspaceServices _services;

        public ActiveStatementSpanProvider(HostWorkspaceServices services)
        {
            _services = services;
        }

        public async Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_services, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(client);

                var result = await client.RunRemoteAsync<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.GetBaseActiveStatementSpansAsync),
                    solution: null,
                    new object[] { documentIds },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return default;
            }
        }

        public async Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_services, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(client);

                var result = await client.RunRemoteAsync<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.GetDocumentActiveStatementSpansAsync),
                    document.Project.Solution,
                    new object[] { document.Id },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return default;
            }
        }
    }
}
