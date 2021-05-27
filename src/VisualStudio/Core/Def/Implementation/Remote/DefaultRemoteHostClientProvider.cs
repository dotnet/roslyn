// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed class DefaultRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public DefaultRemoteHostClientProvider()
        {
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => SpecializedTasks.Null<RemoteHostClient>();
    }
}
