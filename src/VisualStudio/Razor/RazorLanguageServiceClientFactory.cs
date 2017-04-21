// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal static class RazorLanguageServiceClientFactory
    {
        public static async Task<RazorLangaugeServiceClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            var clientFactory = workspace.Services.GetRequiredService<IRemoteHostClientService>();
            var client = await clientFactory.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return client == null ? null : new RazorLangaugeServiceClient(client);
        }
    }
}
