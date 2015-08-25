// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.Host), Shared]
    internal sealed class EditAndContinueWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;

        [ImportingConstructor]
        public EditAndContinueWorkspaceServiceFactory(IDiagnosticAnalyzerService diagnosticService)
        {
            _diagnosticService = diagnosticService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new EditAndContinueWorkspaceService(_diagnosticService);
        }
    }
}
