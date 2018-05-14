// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private class AddConstructorParametersCodeAction : CodeAction
        {
            private readonly AddConstructorParametersFromMembersCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly IList<IParameterSymbol> _parameters;

            public AddConstructorParametersCodeAction(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                Document document,
                State state,
                IList<IParameterSymbol> parameters)
            {
                _service = service;
                _document = document;
                _state = state;
                _parameters = parameters;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var workspace = _document.Project.Solution.Workspace;
                var declarationService = _document.GetLanguageService<ISymbolDeclarationService>();
                var constructor = declarationService.GetDeclarations(_state.DelegatedConstructor).Select(r => r.GetSyntax(cancellationToken)).First();

                var newConstructor = constructor;
                newConstructor = CodeGenerator.AddParameterDeclarations(newConstructor, _parameters.Skip(_state.DelegatedConstructor.Parameters.Length), workspace);
                newConstructor = CodeGenerator.AddStatements(newConstructor, CreateAssignStatements(_state), workspace)
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                var syntaxTree = constructor.SyntaxTree;
                var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

                return Task.FromResult(_document.WithSyntaxRoot(newRoot));
            }

            private IEnumerable<SyntaxNode> CreateAssignStatements(
                State state)
            {
                var factory = _document.GetLanguageService<SyntaxGenerator>();
                for (int i = state.DelegatedConstructor.Parameters.Length; i < state.Parameters.Length; i++)
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
                    var parameters = _state.DelegatedConstructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                    var parameterString = string.Join(", ", parameters);
                    var optional = _parameters.First().IsOptional;
                    var signature = $"{_state.ContainingType.Name}({parameterString})";

                    return optional
                        ? string.Format(FeaturesResources.Add_optional_parameters_to_0, signature)
                        : string.Format(FeaturesResources.Add_parameters_to_0, signature);
                }
            }
        }
    }
}
