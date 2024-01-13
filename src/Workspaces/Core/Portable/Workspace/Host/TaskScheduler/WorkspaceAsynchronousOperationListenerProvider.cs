// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceAsynchronousOperationListenerProvider), ServiceLayer.Default)]
    [Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class WorkspaceAsynchronousOperationListenerProvider(IAsynchronousOperationListenerProvider listenerProvider) : IWorkspaceAsynchronousOperationListenerProvider
    {
        private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);

        public IAsynchronousOperationListener GetListener()
            => _listener;
    }
}
