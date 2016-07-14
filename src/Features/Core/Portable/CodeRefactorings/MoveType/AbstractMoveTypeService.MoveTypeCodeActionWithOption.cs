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
            private readonly State _state;
            private readonly TService _service;
            private readonly bool _makeTypePartial;
            private readonly bool _makeOuterTypePartial;
            private readonly string _title;

            public MoveTypeCodeActionWithOption(
                TService service,
                State state,
                bool makeTypePartial,
                bool makeOuterTypePartial)
            {
                _service = service;
                _state = state;
                _makeTypePartial = makeTypePartial;
                _makeOuterTypePartial = makeOuterTypePartial;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
            {
                if (_makeTypePartial)
                {
                    return $"Make partial definition for '{_state.TypeName}' in new file";
                }

                return $"Move '{_state.TypeName}' to new file";
            }

            public override string Title => _title;
            public Project Project => _state.SemanticDocument.Project;
            public Document SyntacticDocument => _state.SemanticDocument.Document;

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var suggestedFileName = _state.TargetFileNameCandidate + _state.TargetFileExtension;
                var workspaceServices = _state.SemanticDocument.Project.Solution.Workspace.Services;

                var moveTypeOptionsService = workspaceServices.GetService<IMoveTypeOptionsService>();
                var notificationService = workspaceServices.GetService<INotificationService>();
                var projectManagementService = workspaceServices.GetService<IProjectManagementService>();
                var syntaxFactsService = Project.LanguageServices.GetService<ISyntaxFactsService>();

                return moveTypeOptionsService.GetMoveTypeOptions(suggestedFileName, SyntacticDocument, notificationService, projectManagementService, syntaxFactsService);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                IEnumerable<CodeActionOperation> operations = null;

                var moveTypeOptions = options as MoveTypeOptionsResult;
                if (moveTypeOptions != null && !moveTypeOptions.IsCancelled)
                {
                    // Move Type dialog is for moving types to new files.
                    // It cannot perform rename file or rename type operations.
                    var editor = new Editor(
                        _service,
                        state: _state,
                        renameFile: false,
                        renameType: false,
                        makeTypePartial: _makeTypePartial,
                        makeOuterTypePartial: _makeOuterTypePartial,
                        moveTypeOptions: moveTypeOptions,
                        fromDialog: true,
                        cancellationToken: cancellationToken);

                    operations = await editor.GetOperationsAsync().ConfigureAwait(false);
                }

                return operations;
            }
        }
    }
}
