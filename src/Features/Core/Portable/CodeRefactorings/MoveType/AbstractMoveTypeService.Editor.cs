// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
{
    /// <summary>
    /// An abstract class for different edits performed by the Move Type Code Action.
    /// </summary>
    private abstract class Editor(
        TService service,
        TTypeDeclarationSyntax typeDeclaration,
        string fileName,
        CancellationToken cancellationToken)
    {
        protected TService Service { get; } = service;
        protected TTypeDeclarationSyntax TypeDeclaration { get; } = typeDeclaration;
        protected string FileName { get; } = fileName;
        protected CancellationToken CancellationToken { get; } = cancellationToken;
        // protected SemanticDocument SemanticDocument => State.SemanticDocument;

        /// <summary>
        /// Operations performed by CodeAction.
        /// </summary>
        public virtual async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
        {
            var solution = await GetModifiedSolutionAsync().ConfigureAwait(false);

            if (solution == null)
            {
                return [];
            }

            return [new ApplyChangesOperation(solution)];
        }

        /// <summary>
        /// Incremental solution edits that correlate to code operations
        /// </summary>
        public abstract Task<Solution> GetModifiedSolutionAsync();

        public static Editor GetEditor(MoveTypeOperationKind operationKind, TService service, TTypeDeclarationSyntax typeDeclaration, string fileName, CancellationToken cancellationToken)
            => operationKind switch
            {
                MoveTypeOperationKind.MoveType => new MoveTypeEditor(service, typeDeclaration, fileName, cancellationToken),
                MoveTypeOperationKind.RenameType => new RenameTypeEditor(service, typeDeclaration, fileName, cancellationToken),
                MoveTypeOperationKind.RenameFile => new RenameFileEditor(service, typeDeclaration, fileName, cancellationToken),
                MoveTypeOperationKind.MoveTypeNamespaceScope => new MoveTypeNamespaceScopeEditor(service, typeDeclaration, fileName, cancellationToken),
                _ => throw ExceptionUtilities.UnexpectedValue(operationKind),
            };
    }
}
