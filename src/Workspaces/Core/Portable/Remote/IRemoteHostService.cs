﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteHostService
    {
        string Connect(string host, int uiCultureLCID, int cultureLCID, string serializedSession, CancellationToken cancellationToken);
        Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken);
        Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken);

        void UpdateSolutionStorageLocation(SolutionId solutionId, string storageLocation, CancellationToken cancellationToken);

        /// <summary>
        /// This is only for debugging
        /// 
        /// this lets remote side to set same logging options as VS side
        /// </summary>
        void SetLoggingFunctionIds(List<string> loggerTypes, List<string> functionIds, CancellationToken cancellationToken);

        /// <remarks>
        /// JsonRPC seems to have a problem with empty parameter lists.  So passing a dummy parameter
        /// just to make it work properly.
        /// </remarks>
        void OnGlobalOperationStarted(string unused);
        void OnGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled);
    }
}
