// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    internal class WorkspaceSymbolsHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSymbolsHandlerProvider(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new WorkspaceSymbolsHandler(_asyncListener));
        }
    }
}
