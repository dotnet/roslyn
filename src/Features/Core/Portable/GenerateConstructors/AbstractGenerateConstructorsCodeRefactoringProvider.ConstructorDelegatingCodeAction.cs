// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateConstructors;

using static GenerateFromMembersHelpers;

internal abstract partial class AbstractGenerateConstructorsCodeRefactoringProvider
{
    private sealed class ConstructorDelegatingCodeAction(
        AbstractGenerateConstructorsCodeRefactoringProvider service,
        Document document,
        State state,
        bool addNullChecks) : CodeAction
    {
        private readonly AbstractGenerateConstructorsCodeRefactoringProvider _service = service;
        private readonly Document _document = document;
        private readonly State _state = state;
        private readonly bool _addNullChecks = addNullChecks;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            // First, see if there are any constructors that would take the first 'n' arguments
            // we've provided.  If so, delegate to those, and then create a field for any
            // remaining arguments.  Try to match from largest to smallest.
            //
            // Otherwise, just generate a normal constructor that assigns any provided
            // parameters into fields.
            var project = _document.Project;
            var languageServices = project.Solution.Services.GetLanguageServices(_state.ContainingType.Language);

            var semanticModel = await _document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var factory = languageServices.GetRequiredService<SyntaxGenerator>();
            var codeGenerationService = languageServices.GetRequiredService<ICodeGenerationService>();

            Contract.ThrowIfNull(_state.DelegatedConstructor);
            var thisConstructorArguments = factory.CreateArguments(
                [.. _state.Parameters.Select(t => t.parameter).Take(_state.DelegatedConstructor.Parameters.Length)]);

            using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var nullCheckStatements);
            using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var assignStatements);

            var useThrowExpressions = await _service.PrefersThrowExpressionAsync(_document, cancellationToken).ConfigureAwait(false);

            for (var i = _state.DelegatedConstructor.Parameters.Length; i < _state.Parameters.Length; i++)
            {
                var (parameter, fieldOrProperty) = _state.Parameters[i];
                var symbolName = fieldOrProperty.Name;

                var fieldAccess = factory.MemberAccessExpression(
                    factory.ThisExpression(),
                    factory.IdentifierName(symbolName));

                factory.AddAssignmentStatements(
                    factory.SyntaxGeneratorInternal,
                    semanticModel, parameter, fieldAccess,
                    _addNullChecks, useThrowExpressions,
                    nullCheckStatements, assignStatements);
            }

            var syntaxTree = await _document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // If the user has selected a set of members (i.e. TextSpan is not empty), then we will
            // choose the right location (i.e. null) to insert the constructor.  However, if they're 
            // just invoking the feature manually at a specific location, then we'll insert the 
            // members at that specific place in the class/struct.
            var afterThisLocation = _state.TextSpan.IsEmpty
                ? syntaxTree.GetLocation(_state.TextSpan)
                : null;

            var statements = nullCheckStatements.ToImmutable().Concat(assignStatements.ToImmutable());
            var result = await codeGenerationService.AddMethodAsync(
                new CodeGenerationSolutionContext(
                    _document.Project.Solution,
                    new CodeGenerationContext(
                        contextLocation: syntaxTree.GetLocation(_state.TextSpan),
                        afterThisLocation: afterThisLocation)),
                _state.ContainingType,
                CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: default,
                    accessibility: _state.ContainingType.IsAbstractClass() ? Accessibility.Protected : Accessibility.Public,
                    modifiers: DeclarationModifiers.None,
                    typeName: _state.ContainingType.Name,
                    parameters: _state.Parameters.SelectAsArray(t => t.parameter),
                    statements: statements,
                    thisConstructorArguments: thisConstructorArguments),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await AddNavigationAnnotationAsync(result, cancellationToken).ConfigureAwait(false);
        }

        public override string Title
        {
            get
            {
                var parameters = _state.Parameters.Select(p => _service.ToDisplayString(p.parameter, SimpleFormat));
                var parameterString = string.Join(", ", parameters);

                return string.Format(FeaturesResources.Generate_delegating_constructor_0_1,
                    _state.ContainingType.Name, parameterString);
            }
        }
    }
}
