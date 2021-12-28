// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IRemoteHostClientShutdownCancellationService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioRemoteHostClientShutdownCancellationService : IRemoteHostClientShutdownCancellationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioRemoteHostClientShutdownCancellationService()
        {
        }

        public CancellationToken ShutdownToken => VsShellUtilities.ShutdownToken;
    }
}
