// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal partial class GenerateConstructorFromMembersCodeRefactoringProvider
    {
        private class ConstructorDelegatingCodeAction : CodeAction
        {
            private readonly GenerateConstructorFromMembersCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _addNullChecks;

            public ConstructorDelegatingCodeAction(
                GenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document,
                State state,
                bool addNullChecks)
            {
                _service = service;
                _document = document;
                _state = state;
                _addNullChecks = addNullChecks;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // First, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                var project = _document.Project;
                var languageServices = project.Solution.Workspace.Services.GetLanguageServices(_state.ContainingType.Language);

                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var factory = languageServices.GetService<SyntaxGenerator>();
                var codeGenerationService = languageServices.GetService<ICodeGenerationService>();

                var thisConstructorArguments = factory.CreateArguments(
                    _state.Parameters.Take(_state.DelegatedConstructor.Parameters.Length).ToImmutableArray());

                var nullCheckStatements = ArrayBuilder<SyntaxNode>.GetInstance();
                var assignStatements = ArrayBuilder<SyntaxNode>.GetInstance();

                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var useThrowExpressions = options.GetOption(CodeStyleOptions.PreferThrowExpression).Value;

                for (var i = _state.DelegatedConstructor.Parameters.Length; i < _state.Parameters.Length; i++)
                {
                    var symbolName = _state.SelectedMembers[i].Name;
                    var parameter = _state.Parameters[i];

                    var fieldAccess = factory.MemberAccessExpression(
                        factory.ThisExpression(),
                        factory.IdentifierName(symbolName));

                    factory.AddAssignmentStatements(
                        semanticModel, parameter, fieldAccess,
                        _addNullChecks, useThrowExpressions,
                        nullCheckStatements, assignStatements);
                }

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // If the user has selected a set of members (i.e. TextSpan is not empty), then we will
                // choose the right location (i.e. null) to insert the constructor.  However, if they're 
                // just invoking the feature manually at a specific location, then we'll insert the 
                // members at that specific place in the class/struct.
                var afterThisLocation = _state.TextSpan.IsEmpty
                    ? syntaxTree.GetLocation(_state.TextSpan)
                    : null;

                var statements = nullCheckStatements.ToImmutableAndFree().Concat(assignStatements.ToImmutableAndFree());
                var result = await codeGenerationService.AddMethodAsync(
                    _document.Project.Solution,
                    _state.ContainingType,
                    CodeGenerationSymbolFactory.CreateConstructorSymbol(
                        attributes: default,
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(),
                        typeName: _state.ContainingType.Name,
                        parameters: _state.Parameters,
                        statements: statements,
                        thisConstructorArguments: thisConstructorArguments),
                    new CodeGenerationOptions(
                        contextLocation: syntaxTree.GetLocation(_state.TextSpan),
                        afterThisLocation: afterThisLocation),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return await AddNavigationAnnotationAsync(result, cancellationToken).ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();
                    var parameters = _state.Parameters.Select(p => symbolDisplayService.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    return string.Format(FeaturesResources.Generate_delegating_constructor_0_1,
                        _state.ContainingType.Name, parameterString);
                }
            }
        }
    }
}
