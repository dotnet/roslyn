// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal abstract partial class AbstractImplementAbstractClassService : IImplementAbstractClassService
    {
        protected AbstractImplementAbstractClassService()
        {
        }

        protected abstract bool TryInitializeState(Document document, SemanticModel model, SyntaxNode classNode, CancellationToken cancellationToken, out INamedTypeSymbol classType, out INamedTypeSymbol abstractClassType);

        public Task<Document> ImplementAbstractClassAsync(Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_ImplementAbstractClass, cancellationToken))
            {
                var state = State.Generate(this, document, model, node, cancellationToken);
                if (state == null)
                {
                    return SpecializedTasks.Default<Document>();
                }

                return new Editor(document, model, state).GetEditAsync(cancellationToken);
            }
        }

        public bool CanImplementAbstractClass(Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            return State.Generate(this, document, model, node, cancellationToken) != null;
        }
    }
}
