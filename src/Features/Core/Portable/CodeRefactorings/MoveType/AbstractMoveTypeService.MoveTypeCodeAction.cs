// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class MoveTypeCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly TService _service;
            private readonly OperationKind _operationKind;
            private readonly string _title;

            public MoveTypeCodeAction(
                TService service,
                State state,
                OperationKind operationKind)
            {
                _state = state;
                _service = service;
                _operationKind = operationKind;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
            {
                switch (_operationKind)
                {
                    case OperationKind.MoveType:
                        return string.Format(FeaturesResources.Move_type_to_0, _state.TargetFileNameCandidate);
                    case OperationKind.RenameType:
                        return string.Format(FeaturesResources.Rename_type_to_0, _state.DocumentName);
                    case OperationKind.RenameFile:
                        return string.Format(FeaturesResources.Rename_file_to_0, _state.TargetFileNameCandidate);
                }

                throw ExceptionUtilities.Unreachable;
            }

            public override string Title => _title;

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var editor = GetEditor(cancellationToken);
                return editor.GetOperationsAsync();
            }

            private Editor GetEditor(CancellationToken cancellationToken)
            {
                switch (_operationKind)
                {
                    case OperationKind.MoveType:
                        return new MoveTypeEditor(_service, _state, cancellationToken);
                    case OperationKind.RenameType:
                        return new RenameTypeEditor(_service, _state, cancellationToken);
                    case OperationKind.RenameFile:
                        return new RenameFileEditor(_service, _state, cancellationToken);
                }

                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
