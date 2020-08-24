// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostService
    {
        void InitializeGlobalState(int uiCultureLCID, int cultureLCID, CancellationToken cancellationToken);

        void InitializeTelemetrySession(string host, string serializedSession, CancellationToken cancellationToken);

        /// <summary>
        /// This is only for debugging
        /// 
        /// this lets remote side to set same logging options as VS side
        /// </summary>
        void SetLoggingFunctionIds(List<string> loggerTypes, List<string> functionIds, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronize data to OOP proactively without anyone asking for it to make most of operation
        /// faster
        /// </summary>
        Task SynchronizePrimaryWorkspaceAsync(PinnedSolutionInfo solutionInfo, Checksum checksum, int workspaceVersion, CancellationToken cancellationToken);
        Task SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken);
    }
}
