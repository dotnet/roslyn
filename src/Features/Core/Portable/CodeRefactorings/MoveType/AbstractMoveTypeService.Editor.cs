// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

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
                CancellationToken cancellationToken)
            {
                State = state;
                Service = service;
                CancellationToken = cancellationToken;
            }

            protected State State { get; }
            protected TService Service { get; }
            protected CancellationToken CancellationToken { get; }
            protected SemanticDocument SemanticDocument => State.SemanticDocument;

            /// <summary>
            /// operations performed by CodeAction.
            /// </summary>
            internal abstract Task<IEnumerable<CodeActionOperation>> GetOperationsAsync();
        }
    }
}
