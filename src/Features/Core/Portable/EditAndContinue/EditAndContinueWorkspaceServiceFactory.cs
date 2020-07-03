// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.Default), Shared]
    internal sealed class EditAndContinueWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IDebuggeeModuleMetadataProvider? _debugeeModuleMetadataProvider;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueWorkspaceServiceFactory(
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            [Import(AllowDefault = true)] IDebuggeeModuleMetadataProvider? debugeeModuleMetadataProvider)
        {
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
            _debugeeModuleMetadataProvider = debugeeModuleMetadataProvider;
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_debugeeModuleMetadataProvider == null)
            {
                return null;
            }

            return new EditAndContinueWorkspaceService(
                workspaceServices.Workspace,
                _diagnosticService,
                _diagnosticUpdateSource,
                _debugeeModuleMetadataProvider);
        }
    }
}
