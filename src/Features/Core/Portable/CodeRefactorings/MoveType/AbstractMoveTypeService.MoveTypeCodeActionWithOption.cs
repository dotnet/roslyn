// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class MoveTypeCodeActionWithOption : CodeActionWithOptions
        {
            private readonly SemanticDocument _document;
            private readonly State _state;
            private readonly TService _service;
            private readonly bool _renameFile;
            private readonly bool _renameType;
            private readonly bool _makeTypePartial;
            private readonly bool _makeOuterTypePartial;
            private readonly string _title;

            public MoveTypeCodeActionWithOption(
                TService service,
                SemanticDocument document,
                bool renameFile,
                bool renameType,
                bool makeTypePartial,
                bool makeOuterTypePartial,
                State state)
            {
                _document = document;
                _renameFile = renameFile;
                _renameType = renameType;
                _makeTypePartial = makeTypePartial;
                _makeOuterTypePartial = makeOuterTypePartial;
                _state = state;
                _service = service;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
            {
                if (_makeTypePartial)
                {
                    return $"Make partial definition for '{_state.TypeSymbol.Name}' in new file";
                }

                return $"Move '{_state.TypeSymbol.Name}' to new file";
            }

            public override string Title
            {
                get { return _title; }
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var moveTypeOptionsService = _document.Project.Solution.Workspace.Services.GetService<IMoveTypeOptionsService>();
                var notificationService = _document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                var projectManagementService = _document.Project.Solution.Workspace.Services.GetService<IProjectManagementService>();
                var syntaxFactsService = _document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var suggestedFileName = _state.TargetFileNameCandidate + _state.TargetFileExtension;

                return moveTypeOptionsService.GetMoveTypeOptions(suggestedFileName, _document.Document, notificationService, projectManagementService, syntaxFactsService);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                IEnumerable<CodeActionOperation> operations = null;

                var moveTypeOptions = options as MoveTypeOptionsResult;
                if (moveTypeOptions != null && !moveTypeOptions.IsCancelled)
                {
                    var editor = new Editor(_service, _document, _renameFile, _renameType, _makeTypePartial, _makeOuterTypePartial, _state, moveTypeOptions, fromDialog: true, cancellationToken: cancellationToken);
                    operations = await editor.GetOperationsAsync().ConfigureAwait(false);
                }

                return operations;
            }
        }
    }
}
