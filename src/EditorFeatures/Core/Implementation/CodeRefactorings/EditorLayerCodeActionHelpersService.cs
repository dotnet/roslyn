// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeRefactorings
{
    [ExportWorkspaceServiceFactory(typeof(ICodeRefactoringHelpersService), ServiceLayer.Editor), Shared]
    internal class EditorLayerCodeActionHelpersService : IWorkspaceServiceFactory
    {
        private readonly IInlineRenameService _renameService;

        [ImportingConstructor]
        public EditorLayerCodeActionHelpersService(IInlineRenameService renameService)
        {
            _renameService = renameService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new CodeActionHelpersService(this);
        }

        private class CodeActionHelpersService : ICodeRefactoringHelpersService
        {
            private readonly EditorLayerCodeActionHelpersService _service;

            public CodeActionHelpersService(EditorLayerCodeActionHelpersService service)
            {
                _service = service;
            }

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
