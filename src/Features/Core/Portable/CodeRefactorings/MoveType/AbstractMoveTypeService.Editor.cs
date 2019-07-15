// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        /// <summary>
        /// An abstract class for different edits performed by the Move Type Code Action.
        /// </summary>
        private abstract class Editor
        {
            public Editor(
                TService service,
                State state,
                string fileName,
                CancellationToken cancellationToken)
            {
                State = state;
                Service = service;
                FileName = fileName;
                CancellationToken = cancellationToken;
            }

            protected State State { get; }
            protected TService Service { get; }
            protected string FileName { get; }
            protected CancellationToken CancellationToken { get; }
            protected SemanticDocument SemanticDocument => State.SemanticDocument;

            /// <summary>
            /// Operations performed by CodeAction.
            /// </summary>
            public virtual async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            {
                var solution = await GetModifiedSolutionAsync().ConfigureAwait(false);

                if (solution == null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(solution));
            }

            /// <summary>
            /// Incremental solution edits that correlate to code operations
            /// </summary>
            public abstract Task<Solution> GetModifiedSolutionAsync();

            public static Editor GetEditor(MoveTypeOperationKind operationKind, TService service, State state, string fileName, CancellationToken cancellationToken)
            {
                switch (operationKind)
                {
                    case MoveTypeOperationKind.MoveType:
                        return new MoveTypeEditor(service, state, fileName, cancellationToken);
                    case MoveTypeOperationKind.RenameType:
                        return new RenameTypeEditor(service, state, fileName, cancellationToken);
                    case MoveTypeOperationKind.RenameFile:
                        return new RenameFileEditor(service, state, fileName, cancellationToken);
                    case MoveTypeOperationKind.MoveTypeNamespaceScope:
                        return new MoveTypeNamespaceScopeEditor(service, state, fileName, cancellationToken);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(operationKind);
                }
            }
        }
    }
}
