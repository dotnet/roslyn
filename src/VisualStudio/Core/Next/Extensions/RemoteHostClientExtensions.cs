// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Remote;
using static Microsoft.VisualStudio.LanguageServices.Remote.RemoteHostClient;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class RemoteHostClientExtensions
    {
        public static Task<Session> CreateCodeAnalysisServiceSessionAsync(this RemoteHostClient client, Solution solution, CancellationToken cancellationToken)
        {
            return CreateCodeAnalysisServiceSessionAsync(client, solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        public static Task<Session> CreateCodeAnalysisServiceSessionAsync(this RemoteHostClient client, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            return client.CreateServiceSessionAsync(WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, cancellationToken);
        }
    }
}
