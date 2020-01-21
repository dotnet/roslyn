// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    [ExportWorkspaceServiceFactory(typeof(ICodeRefactoringHelpersService), ServiceLayer.Default), Shared]
    internal class ServicesLayerCodeActionHelpersService : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public ServicesLayerCodeActionHelpersService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new CodeActionHelpersService();
        }

        private class CodeActionHelpersService : ICodeRefactoringHelpersService
        {
            public bool ActiveInlineRenameSession => false;
        }
    }
}
