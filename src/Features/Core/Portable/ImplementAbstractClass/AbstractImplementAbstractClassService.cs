// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal abstract partial class AbstractImplementAbstractClassService<TClassSyntax> :
        IImplementAbstractClassService
        where TClassSyntax : SyntaxNode
    {
        protected AbstractImplementAbstractClassService()
        {
        }

        protected abstract bool TryInitializeState(Document document, SemanticModel model, TClassSyntax classNode, CancellationToken cancellationToken, out INamedTypeSymbol classType, out INamedTypeSymbol abstractClassType);

        public async Task<Document> ImplementAbstractClassAsync(
            Document document, SyntaxNode classNode, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_ImplementAbstractClass, cancellationToken))
            {
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var state = State.Generate(this, document, model, (TClassSyntax)classNode, cancellationToken);
                if (state == null)
                {
                    return null;
                }

                return await new Editor(document, model, state).GetEditAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> CanImplementAbstractClassAsync(
            Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return State.Generate(this, document, model, (TClassSyntax)node, cancellationToken) != null;
        }
    }
}
