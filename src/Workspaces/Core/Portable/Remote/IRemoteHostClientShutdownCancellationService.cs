// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: consider consolidating this with IThreadingContext.DisposalToken
    // https://github.com/dotnet/roslyn/issues/47840
    internal interface IRemoteHostClientShutdownCancellationService : IWorkspaceService
    {
        /// <summary>
        /// Token signaled when the host starts to shut down.
        /// </summary>
        CancellationToken ShutdownToken { get; }
    }
}
