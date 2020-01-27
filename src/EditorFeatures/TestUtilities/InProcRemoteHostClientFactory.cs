// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.RemoteHost
{
    internal static class RemoteHostOptions
    {
        public static readonly Option<bool> RemoteHostTest = new Option<bool>(
            nameof(RemoteHostOptions), nameof(RemoteHostTest), defaultValue: false);
    }

    [ExportOptionProvider, Shared]
    internal class RemoteHostOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public RemoteHostOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostOptions.RemoteHostTest);
    }

    [ExportWorkspaceService(typeof(IRemoteHostClientFactory)), Shared]
    internal class InProcRemoteHostClientFactory : IRemoteHostClientFactory
    {
        [ImportingConstructor]
        public InProcRemoteHostClientFactory()
        {
        }

        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest))
            {
                return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false);
            }

            return SpecializedTasks.Null<RemoteHostClient>();
        }
    }
}
