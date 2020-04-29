// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorRemoteHostClient
    {
        private readonly RemoteHostClient _client;
        private readonly string _serviceName;

        internal RazorRemoteHostClient(RemoteHostClient client, string serviceName)
        {
            _client = client;
            _serviceName = serviceName;
        }

        public static async Task<RazorRemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            var clientFactory = workspace.Services.GetRequiredService<IRemoteHostClientService>();
            var client = await clientFactory.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return client == null ? null : new RazorRemoteHostClient(client, GetServiceName(workspace));
        }

        public Task<Optional<T>> TryRunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => _client.TryRunRemoteAsync<T>(_serviceName, targetName, solution, arguments, callbackTarget: null, cancellationToken);

        #region support a/b testing. after a/b testing, we can remove all this code

        private static string? s_lazyServiceName = null;

        private static string GetServiceName(Workspace workspace)
        {
            if (s_lazyServiceName == null)
            {
                var x64 = workspace.Options.GetOption(OOP64Bit);
                if (!x64)
                {
                    x64 = workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(
                        WellKnownExperimentNames.RoslynOOP64bit);
                }

                Interlocked.CompareExchange(
                    ref s_lazyServiceName, x64 ? "razorLanguageService64" : "razorLanguageService", null);
            }

            return s_lazyServiceName;
        }

        public static readonly Option<bool> OOP64Bit = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(OOP64Bit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(OOP64Bit)));

        private static class InternalFeatureOnOffOptions
        {
            internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";
        }

        #endregion
    }
}
