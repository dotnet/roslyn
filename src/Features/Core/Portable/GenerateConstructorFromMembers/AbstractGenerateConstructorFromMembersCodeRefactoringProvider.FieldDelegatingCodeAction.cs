// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        private sealed class FieldDelegatingCodeAction(
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
                var parameterToExistingFieldMap = ImmutableDictionary.CreateBuilder<string, ISymbol>();
                for (var i = 0; i < state.Parameters.Length; i++)
                    parameterToExistingFieldMap[state.Parameters[i].Name] = state.SelectedMembers[i];

                var factory = document.GetRequiredLanguageService<SyntaxGenerator>();

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var syntaxTree = semanticModel.SyntaxTree;
                var preferThrowExpression = await service.PrefersThrowExpressionAsync(document, fallbackOptions, cancellationToken).ConfigureAwait(false);

                var members = factory.CreateMemberDelegatingConstructor(
                    semanticModel,
                    state.ContainingType.Name,
                    state.ContainingType,
                    state.Parameters,
                    state.Accessibility,
                    parameterToExistingFieldMap.ToImmutable(),
                    parameterToNewMemberMap: null,
                    addNullChecks: addNullChecks,
                    preferThrowExpression: preferThrowExpression,
                    generateProperties: false,
                    state.IsContainedInUnsafeType);

                // If the user has selected a set of members (i.e. TextSpan is not empty), then we will
                // choose the right location (i.e. null) to insert the constructor.  However, if they're 
                // just invoking the feature manually at a specific location, then we'll insert the 
                // members at that specific place in the class/struct.
                var afterThisLocation = state.TextSpan.IsEmpty
                    ? syntaxTree.GetLocation(state.TextSpan)
                    : null;

                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    new CodeGenerationSolutionContext(
                        document.Project.Solution,
                        new CodeGenerationContext(
                            contextLocation: syntaxTree.GetLocation(state.TextSpan),
                            afterThisLocation: afterThisLocation),
                        fallbackOptions),
                    state.ContainingType,
                    members,
                    cancellationToken).ConfigureAwait(false);

                return await AddNavigationAnnotationAsync(result, cancellationToken).ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    var parameters = state.Parameters.Select(p => service.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    if (state.DelegatedConstructor == null)
                    {
                        return string.Format(FeaturesResources.Generate_constructor_0_1,
                            state.ContainingType.Name, parameterString);
                    }
                    else
                    {
                        return string.Format(FeaturesResources.Generate_field_assigning_constructor_0_1,
                            state.ContainingType.Name, parameterString);
                    }
                }
            }
        }
    }
}
