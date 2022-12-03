// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(DidChangeConfigurationNotificationHandler)), Shared]
    [Method(LSP.Methods.WorkspaceDidChangeConfigurationName)]
    internal class DidChangeConfigurationNotificationHandler : ILspServiceNotificationHandler<LSP.DidChangeConfigurationParams>
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidChangeConfigurationNotificationHandler(IGlobalOptionService globalOptionService)
        {
            _globalOptionService = globalOptionService;
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public Task HandleNotificationAsync(DidChangeConfigurationParams request, RequestContext requestContext, CancellationToken cancellationToken)
        {
            var settings = request.Settings;
            return Task.CompletedTask;
        }
    }
}
