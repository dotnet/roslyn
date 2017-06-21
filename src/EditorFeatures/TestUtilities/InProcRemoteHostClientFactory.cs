// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.RemoteHost
{
    internal static class RemoteHostOptions
    {
        [ExportOption]
        public static readonly Option<bool> RemoteHostTest = new Option<bool>(
            nameof(RemoteHostOptions), nameof(RemoteHostTest), defaultValue: false);
    }

    [ExportWorkspaceService(typeof(IRemoteHostClientFactory)), Shared]
    internal class InProcRemoteHostClientFactory : IRemoteHostClientFactory
    {
        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest))
            {
                return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false, cancellationToken: cancellationToken);
            }

            return SpecializedTasks.Default<RemoteHostClient>();
        }
    }
}
