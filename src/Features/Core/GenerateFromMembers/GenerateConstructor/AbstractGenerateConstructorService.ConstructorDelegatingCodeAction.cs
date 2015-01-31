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
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;

            public ConstructorDelegatingCodeAction(
                TService service,
                Document document,
                State state)
            {
                _service = service;
                _document = document;
                _state = state;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // First, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.ContainingType.Language);
                var factory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var thisConstructorArguments = factory.CreateArguments(_state.DelegatedConstructor.Parameters);
                var statements = new List<SyntaxNode>();

                for (var i = _state.DelegatedConstructor.Parameters.Length; i < _state.Parameters.Count; i++)
                {
                    var symbolName = _state.SelectedMembers[i].Name;
                    var parameterName = _state.Parameters[i].Name;
                    var assignExpression = factory.AssignmentStatement(
                        factory.MemberAccessExpression(
                            factory.ThisExpression(),
                            factory.IdentifierName(symbolName)),
                        factory.IdentifierName(parameterName));

                    var expressionStatement = factory.ExpressionStatement(assignExpression);
                    statements.Add(expressionStatement);
                }

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var result = await codeGenerationService.AddMethodAsync(
                    _document.Project.Solution,
                    _state.ContainingType,
                    CodeGenerationSymbolFactory.CreateConstructorSymbol(
                        attributes: null,
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(),
                        typeName: _state.ContainingType.Name,
                        parameters: _state.Parameters,
                        statements: statements,
                        thisConstructorArguments: thisConstructorArguments),
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_state.TextSpan)),
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            public override string Title
            {
                get
                {
                    var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();
                    var parameters = _state.Parameters.Select(p => symbolDisplayService.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    return string.Format(FeaturesResources.GenerateDelegatingConstructor,
                        _state.ContainingType.Name, parameterString);
                }
            }
        }
    }
}
