﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly string _fileName;

            public MoveTypeCodeAction(
                TService service,
                State state,
                OperationKind operationKind,
                string fileName)
            {
                _state = state;
                _service = service;
                _operationKind = operationKind;
                _fileName = fileName;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
            {
                switch (_operationKind)
                {
                    case OperationKind.MoveType:
                        return string.Format(FeaturesResources.Move_type_to_0, _fileName);
                    case OperationKind.RenameType:
                        return string.Format(FeaturesResources.Rename_type_to_0, _state.DocumentNameWithoutExtension);
                    case OperationKind.RenameFile:
                        return string.Format(FeaturesResources.Rename_file_to_0, _fileName);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(_operationKind);
                }
            }

            public override string Title => _title;

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var editor = GetEditor(cancellationToken);
                return await editor.GetOperationsAsync().ConfigureAwait(false);
            }

            private Editor GetEditor(CancellationToken cancellationToken)
            {
                switch (_operationKind)
                {
                    case OperationKind.MoveType:
                        return new MoveTypeEditor(_service, _state, _fileName, cancellationToken);
                    case OperationKind.RenameType:
                        return new RenameTypeEditor(_service, _state, _fileName, cancellationToken);
                    case OperationKind.RenameFile:
                        return new RenameFileEditor(_service, _state, _fileName, cancellationToken);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(_operationKind);
                }
            }

            internal override bool PerformFinalApplicabilityCheck => true;

            internal override bool IsApplicable(Workspace workspace)
            {
                switch (_operationKind)
                {
                    case OperationKind.RenameFile:
                        return workspace.CanRenameFilesDuringCodeActions(_state.SemanticDocument.Document.Project);
                }

                return true;
            }
        }
    }
}
