// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.Default), Shared]
    internal sealed class EditAndContinueWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IActiveStatementProvider _activeStatementProviderOpt;
        private readonly IDebuggeeModuleMetadataProvider _debugeeModuleMetadataProviderOpt;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueWorkspaceServiceFactory(
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            [Import(AllowDefault = true)]IActiveStatementProvider activeStatementProvider,
            [Import(AllowDefault = true)]IDebuggeeModuleMetadataProvider debugeeModuleMetadataProvider)
        {
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
            _activeStatementProviderOpt = activeStatementProvider;
            _debugeeModuleMetadataProviderOpt = debugeeModuleMetadataProvider;
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_debugeeModuleMetadataProviderOpt == null || _activeStatementProviderOpt == null)
            {
                return null;
            }

            return new EditAndContinueWorkspaceService(
                workspaceServices.Workspace,
                workspaceServices.Workspace.Services.GetRequiredService<IActiveStatementTrackingService>(),
                workspaceServices.Workspace.Services.GetRequiredService<ICompilationOutputsProviderService>(),
                _diagnosticService,
                _diagnosticUpdateSource,
                _activeStatementProviderOpt,
                _debugeeModuleMetadataProviderOpt);
        }
    }
}
