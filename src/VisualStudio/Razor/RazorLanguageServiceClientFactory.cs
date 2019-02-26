// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal static class RazorLanguageServiceClientFactory
    {
        public static async Task<RazorLanguageServiceClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            var clientFactory = workspace.Services.GetRequiredService<IRemoteHostClientService>();
            var client = await clientFactory.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client is null)
            {
                return null;
            }

            var serviceName = await GetServiceNameAsync(workspace, cancellationToken).ConfigureAwait(false);
            return new RazorLanguageServiceClient(client, serviceName);
        }

        #region support a/b testing. after a/b testing, we can remove all this code
        private static string s_serviceNameDoNotAccessDirectly = null;

        private static async ValueTask<string> GetServiceNameAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (s_serviceNameDoNotAccessDirectly == null)
            {
                var x64 = workspace.Options.GetOption(OOP64Bit);
                if (!x64)
                {
                    var experimentationServiceFactory = workspace.Services.GetRequiredService<IExperimentationServiceFactory>();
                    var experimentationService = await experimentationServiceFactory.GetExperimentationServiceAsync(cancellationToken).ConfigureAwait(false);
                    x64 = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.RoslynOOP64bit);
                }

                Interlocked.CompareExchange(
                    ref s_serviceNameDoNotAccessDirectly, x64 ? "razorLanguageService64" : "razorLanguageService", null);
            }

            return s_serviceNameDoNotAccessDirectly;
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
