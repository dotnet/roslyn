// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IShutdownCancellationService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioShutdownCancellationService : IShutdownCancellationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioShutdownCancellationService()
        {
        }

        public CancellationToken ShutdownToken => VsShellUtilities.ShutdownToken;
    }
}
