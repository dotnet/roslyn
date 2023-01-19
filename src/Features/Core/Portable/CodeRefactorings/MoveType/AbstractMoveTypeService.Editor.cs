// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
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
                => operationKind switch
                {
                    MoveTypeOperationKind.MoveType => new MoveTypeEditor(service, state, fileName, cancellationToken),
                    MoveTypeOperationKind.RenameType => new RenameTypeEditor(service, state, fileName, cancellationToken),
                    MoveTypeOperationKind.RenameFile => new RenameFileEditor(service, state, fileName, cancellationToken),
                    MoveTypeOperationKind.MoveTypeNamespaceScope => new MoveTypeNamespaceScopeEditor(service, state, fileName, cancellationToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(operationKind),
                };
        }
    }
}
