// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private sealed class MoveTypeCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly TService _service;
            private readonly MoveTypeOperationKind _operationKind;
            private readonly string _title;
            private readonly string _fileName;

            public MoveTypeCodeAction(
                TService service,
                State state,
                MoveTypeOperationKind operationKind,
                string fileName)
            {
                _state = state;
                _service = service;
                _operationKind = operationKind;
                _fileName = fileName;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
                => _operationKind switch
                {
                    MoveTypeOperationKind.MoveType => string.Format(FeaturesResources.Move_type_to_0, _fileName),
                    MoveTypeOperationKind.RenameType => string.Format(FeaturesResources.Rename_type_to_0, _state.DocumentNameWithoutExtension),
                    MoveTypeOperationKind.RenameFile => string.Format(FeaturesResources.Rename_file_to_0, _fileName),
                    MoveTypeOperationKind.MoveTypeNamespaceScope => string.Empty,
                    _ => throw ExceptionUtilities.UnexpectedValue(_operationKind),
                };

            public override string Title => _title;

            protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
                IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var editor = Editor.GetEditor(_operationKind, _service, _state, _fileName, cancellationToken);
                return await editor.GetOperationsAsync().ConfigureAwait(false);
            }
        }
    }
}
