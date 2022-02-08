// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Obsolete("Use Microsoft.CodeAnalysis.ExternalAccess.Razor.RazorRemoteHostClient instead")]
    internal static class RazorLanguageServiceClientFactory
    {
        public static async Task<RazorLanguageServiceClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            var clientFactory = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
            var client = await clientFactory.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return client == null ? null : new RazorLanguageServiceClient(client, GetServiceName(workspace));
        }

        #region support a/b testing. after a/b testing, we can remove all this code
        private static string s_serviceNameDoNotAccessDirectly = null;

        private static string GetServiceName(Workspace workspace)
        {
            if (s_serviceNameDoNotAccessDirectly == null)
            {
                var x64 = workspace.Options.GetOption(OOP64Bit);
                if (!x64)
                {
                    x64 = workspace.Services.GetService<IExperimentationService>().IsExperimentEnabled(
                        WellKnownExperimentNames.RoslynOOP64bit);
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
