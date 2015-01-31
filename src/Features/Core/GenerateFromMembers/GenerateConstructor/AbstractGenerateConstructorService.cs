// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TMemberDeclarationSyntax> :
            AbstractGenerateFromMembersService<TMemberDeclarationSyntax>, IGenerateConstructorService
        where TService : AbstractGenerateConstructorService<TService, TMemberDeclarationSyntax>
        where TMemberDeclarationSyntax : SyntaxNode
    {
        protected AbstractGenerateConstructorService()
        {
        }

        public async Task<IGenerateConstructorResult> GenerateConstructorAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateConstructor, cancellationToken))
            {
                var info = await GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = State.Generate((TService)this, document, textSpan, info.ContainingType, info.SelectedMembers, cancellationToken);
                    if (state != null)
                    {
                        return new GenerateConstructorResult(
                            CreateCodeRefactoring(info.SelectedDeclarations, GetCodeActions(document, state)));
                    }
                }

                return GenerateConstructorResult.Failure;
            }
        }

        private IEnumerable<CodeAction> GetCodeActions(Document document, State state)
        {
            yield return new FieldDelegatingCodeAction((TService)this, document, state);
            if (state.DelegatedConstructor != null)
            {
                yield return new ConstructorDelegatingCodeAction((TService)this, document, state);
            }
        }
    }
}
