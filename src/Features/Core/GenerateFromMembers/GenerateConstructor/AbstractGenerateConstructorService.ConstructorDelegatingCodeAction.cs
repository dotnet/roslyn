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
        private class ConstructorDelegatingCodeAction : CodeAction
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;

            public ConstructorDelegatingCodeAction(
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
                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(state.ContainingType.Language);
                var factory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var thisConstructorArguments = factory.CreateArguments(state.DelegatedConstructor.Parameters);
                var statements = new List<SyntaxNode>();

                for (var i = state.DelegatedConstructor.Parameters.Length; i < state.Parameters.Count; i++)
                {
                    var symbolName = state.SelectedMembers[i].Name;
                    var parameterName = state.Parameters[i].Name;
                    var assignExpression = factory.AssignmentStatement(
                        factory.MemberAccessExpression(
                            factory.ThisExpression(),
                            factory.IdentifierName(symbolName)),
                        factory.IdentifierName(parameterName));

                    var expressionStatement = factory.ExpressionStatement(assignExpression);
                    statements.Add(expressionStatement);
                }

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var result = await codeGenerationService.AddMethodAsync(
                    document.Project.Solution,
                    state.ContainingType,
                    CodeGenerationSymbolFactory.CreateConstructorSymbol(
                        attributes: null,
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(),
                        typeName: state.ContainingType.Name,
                        parameters: state.Parameters,
                        statements: statements,
                        thisConstructorArguments: thisConstructorArguments),
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(state.TextSpan)),
                    cancellationToken: cancellationToken)
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

                    return string.Format(FeaturesResources.GenerateDelegatingConstructor,
                        state.ContainingType.Name, parameterString);
                }
            }
        }
    }
}
