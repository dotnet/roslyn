// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TMemberDeclarationSyntax>
    {
        private class FieldDelegatingCodeAction : CodeAction
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;

            public FieldDelegatingCodeAction(
                TService service,
                Document document,
                State state)
            {
                this.service = service;
                this.document = document;
                this.state = state;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // First, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                for (int i = 0; i < state.Parameters.Count; i++)
                {
                    parameterToExistingFieldMap[state.Parameters[i].Name] = state.SelectedMembers[i];
                }

                var factory = this.document.GetLanguageService<SyntaxGenerator>();

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var members = factory.CreateFieldDelegatingConstructor(
                    state.ContainingType.Name,
                    state.ContainingType,
                    state.Parameters,
                    parameterToExistingFieldMap,
                    parameterToNewFieldMap: null,
                    cancellationToken: cancellationToken);

                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    document.Project.Solution,
                    state.ContainingType,
                    members,
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(state.TextSpan)),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            public override string Title
            {
                get
                {
                    var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
                    var parameters = state.Parameters.Select(p => symbolDisplayService.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    if (state.DelegatedConstructor == null)
                    {
                        return string.Format(FeaturesResources.GenerateConstructor,
                            state.ContainingType.Name, parameterString);
                    }
                    else
                    {
                        return string.Format(FeaturesResources.GenerateFieldAssigningConstructor,
                            state.ContainingType.Name, parameterString);
                    }
                }
            }
        }
    }
}
