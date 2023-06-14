// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        private sealed class ConstructorDelegatingCodeAction(
            AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
            Document document,
            State state,
            bool addNullChecks,
            CleanCodeGenerationOptionsProvider fallbackOptions) : CodeAction
        {
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // First, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                var project = document.Project;
                var languageServices = project.Solution.Services.GetLanguageServices(state.ContainingType.Language);

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var factory = languageServices.GetRequiredService<SyntaxGenerator>();
                var codeGenerationService = languageServices.GetRequiredService<ICodeGenerationService>();

                Contract.ThrowIfNull(state.DelegatedConstructor);
                var thisConstructorArguments = factory.CreateArguments(
                    state.Parameters.Take(state.DelegatedConstructor.Parameters.Length).ToImmutableArray());

                using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var nullCheckStatements);
                using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var assignStatements);

                var useThrowExpressions = await service.PrefersThrowExpressionAsync(document, fallbackOptions, cancellationToken).ConfigureAwait(false);

                for (var i = state.DelegatedConstructor.Parameters.Length; i < state.Parameters.Length; i++)
                {
                    var symbolName = state.SelectedMembers[i].Name;
                    var parameter = state.Parameters[i];

                    var fieldAccess = factory.MemberAccessExpression(
                        factory.ThisExpression(),
                        factory.IdentifierName(symbolName));

                    factory.AddAssignmentStatements(
                        semanticModel, parameter, fieldAccess,
                        addNullChecks, useThrowExpressions,
                        nullCheckStatements, assignStatements);
                }

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // If the user has selected a set of members (i.e. TextSpan is not empty), then we will
                // choose the right location (i.e. null) to insert the constructor.  However, if they're 
                // just invoking the feature manually at a specific location, then we'll insert the 
                // members at that specific place in the class/struct.
                var afterThisLocation = state.TextSpan.IsEmpty
                    ? syntaxTree.GetLocation(state.TextSpan)
                    : null;

                var statements = nullCheckStatements.ToImmutable().Concat(assignStatements.ToImmutable());
                var result = await codeGenerationService.AddMethodAsync(
                    new CodeGenerationSolutionContext(
                        document.Project.Solution,
                        new CodeGenerationContext(
                            contextLocation: syntaxTree.GetLocation(state.TextSpan),
                            afterThisLocation: afterThisLocation),
                        fallbackOptions),
                    state.ContainingType,
                    CodeGenerationSymbolFactory.CreateConstructorSymbol(
                        attributes: default,
                        accessibility: state.ContainingType.IsAbstractClass() ? Accessibility.Protected : Accessibility.Public,
                        modifiers: new DeclarationModifiers(),
                        typeName: state.ContainingType.Name,
                        parameters: state.Parameters,
                        statements: statements,
                        thisConstructorArguments: thisConstructorArguments),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return await AddNavigationAnnotationAsync(result, cancellationToken).ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    var parameters = state.Parameters.Select(p => service.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    return string.Format(FeaturesResources.Generate_delegating_constructor_0_1,
                        state.ContainingType.Name, parameterString);
                }
            }
        }
    }
}
