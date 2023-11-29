﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(DidChangeConfigurationNotificationHandler)), Shared]
    internal class DidChangeConfigurationNotificationHandlerFactory : ILspServiceFactory
    {
        private readonly IGlobalOptionService _globalOptionService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidChangeConfigurationNotificationHandlerFactory(
            IGlobalOptionService globalOptionService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptionService = globalOptionService;
            _listenerProvider = listenerProvider;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var clientManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            var lspLogger = lspServices.GetRequiredService<ILspServiceLogger>();
            return new DidChangeConfigurationNotificationHandler(lspLogger, _globalOptionService, clientManager, _listenerProvider);
        }
    }
}
