// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.AddConstructorParameters
{
    internal abstract partial class AbstractAddConstructorParametersService<TService, TMemberDeclarationSyntax>
    {
        private class AddConstructorParametersCodeAction : CodeAction
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;
            private readonly IList<IParameterSymbol> parameters;

            public AddConstructorParametersCodeAction(
                TService service,
                Document document,
                State state,
                IList<IParameterSymbol> parameters)
            {
                this.service = service;
                this.document = document;
                this.state = state;
                this.parameters = parameters;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var workspace = document.Project.Solution.Workspace;
                var declarationService = document.Project.LanguageServices.GetService<ISymbolDeclarationService>();
                var constructor = declarationService.GetDeclarations(state.DelegatedConstructor).Select(r => r.GetSyntax(cancellationToken)).First();

                var newConstructor = constructor;
                newConstructor = CodeGenerator.AddParameterDeclarations(newConstructor, parameters.Skip(state.DelegatedConstructor.Parameters.Length), workspace);
                newConstructor = CodeGenerator.AddStatements(newConstructor, CreateAssignStatements(state), workspace)
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                var syntaxTree = constructor.SyntaxTree;
                var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            private IEnumerable<SyntaxNode> CreateAssignStatements(
                State state)
            {
                var factory = this.document.GetLanguageService<SyntaxGenerator>();
                for (int i = state.DelegatedConstructor.Parameters.Length; i < state.Parameters.Count; i++)
                {
                    var symbolName = state.SelectedMembers[i].Name;
                    var parameterName = state.Parameters[i].Name;

                    yield return factory.ExpressionStatement(
                        factory.AssignmentStatement(
                            factory.MemberAccessExpression(factory.ThisExpression(), factory.IdentifierName(symbolName)),
                            factory.IdentifierName(parameterName)));
                }
            }

            public override string Title
            {
                get
                {
                    var parameters = state.DelegatedConstructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                    var parameterString = string.Join(", ", parameters);
                    var optional = this.parameters.First().IsOptional;

                    if (optional)
                    {
                        return string.Format(FeaturesResources.AddOptionalParametersTo,
                            state.ContainingType.Name, parameterString);
                    }
                    else
                    {
                        return string.Format(FeaturesResources.AddParametersTo,
                            state.ContainingType.Name, parameterString);
                    }
                }
            }
        }
    }
}
