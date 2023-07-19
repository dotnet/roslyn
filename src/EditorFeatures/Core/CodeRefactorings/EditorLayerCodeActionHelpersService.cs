// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    [ExportWorkspaceServiceFactory(typeof(ICodeRefactoringHelpersService), ServiceLayer.Editor), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class EditorLayerCodeActionHelpersService(IInlineRenameService renameService) : IWorkspaceServiceFactory
    {
        private readonly IInlineRenameService _renameService = renameService;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new CodeActionHelpersService(this);

        private class CodeActionHelpersService(EditorLayerCodeActionHelpersService service) : ICodeRefactoringHelpersService
        {
            private readonly EditorLayerCodeActionHelpersService _service = service;

            public bool ActiveInlineRenameSession
            {
                get
                {
                    return _service._renameService.ActiveSession != null;
                }
            }
        }
    }
}
