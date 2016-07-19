// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class MoveTypeCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly TService _service;

            private readonly bool _renameFile;
            private readonly string _title;
            private readonly bool _renameType;

            public MoveTypeCodeAction(
                TService service,
                State state,
                bool renameFile,
                bool renameType)
            {
                _renameFile = renameFile;
                _renameType = renameType;
                _state = state;
                _service = service;
                _title = CreateDisplayText();
            }

            private string CreateDisplayText()
            {
                if (_renameFile)
                {
                    return string.Format(
                        FeaturesResources.RenameFileTo_0,
                        _state.TargetFileNameCandidate);
                }
                else if (_renameType)
                {
                    return string.Format(
                        FeaturesResources.RenameTypeTo_0, _state.DocumentName);
                }

                return string.Format(
                    FeaturesResources.MoveTypeTo_0,
                    _state.TargetFileNameCandidate);
            }

            public override string Title => _title;

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var editor = new Editor(_service, _state, _renameFile, _renameType, cancellationToken: cancellationToken);
                return await editor.GetOperationsAsync().ConfigureAwait(false);
            }
        }
    }
}
