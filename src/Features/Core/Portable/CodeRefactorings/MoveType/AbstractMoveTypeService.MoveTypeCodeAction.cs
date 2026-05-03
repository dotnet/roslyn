// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    private sealed class MoveTypeCodeAction : CodeAction
    {
        private readonly TService _service;
        private readonly SemanticDocument _document;
        private readonly TTypeDeclarationSyntax _typeDeclaration;
        private readonly MoveTypeOperationKind _operationKind;
        private readonly string _fileName;

        public MoveTypeCodeAction(
            TService service,
            SemanticDocument document,
            TTypeDeclarationSyntax typeDeclaration,
            MoveTypeOperationKind operationKind,
            string fileName)
        {
            _service = service;
            _document = document;
            _typeDeclaration = typeDeclaration;
            _operationKind = operationKind;
            _fileName = fileName;
            this.Title = CreateDisplayText();
        }

        private string CreateDisplayText()
            => _operationKind switch
            {
                MoveTypeOperationKind.MoveType => string.Format(FeaturesResources.Move_type_to_0, _fileName),
                MoveTypeOperationKind.RenameType => string.Format(FeaturesResources.Rename_type_to_0, GetDocumentNameWithoutExtension(_document)),
                MoveTypeOperationKind.RenameFile => string.Format(FeaturesResources.Rename_file_to_0, _fileName),
                MoveTypeOperationKind.MoveTypeNamespaceScope => string.Empty,
                _ => throw ExceptionUtilities.UnexpectedValue(_operationKind),
            };

        public override string Title { get; }

        protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var editor = Editor.GetEditor(_operationKind, _service, _document, _typeDeclaration, _fileName, cancellationToken);
            return await editor.GetOperationsAsync().ConfigureAwait(false);
        }
    }
}
