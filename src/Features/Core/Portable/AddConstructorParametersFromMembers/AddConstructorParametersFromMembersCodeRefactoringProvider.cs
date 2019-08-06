// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.GenerateOverrides)]
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        [ImportingConstructor]
        public AddConstructorParametersFromMembersCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await AddConstructorParametersFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        public async Task<ImmutableArray<CodeAction>> AddConstructorParametersFromMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_AddConstructorParametersFromMembers, cancellationToken))
            {
                var info = await GetSelectedMemberInfoAsync(
                    document,
                    textSpan,
                    allowPartialSelection: true,
                    cancellationToken).ConfigureAwait(false);

                if (info != null)
                {
                    var state = await State.GenerateAsync(this, info.SelectedMembers, document, cancellationToken).ConfigureAwait(false);
                    if (state?.ConstructorCandidates != null && !state.ConstructorCandidates.IsEmpty)
                    {
                        return CreateCodeActions(document, state);
                    }
                }

                return default;
            }
        }

        private ImmutableArray<CodeAction> CreateCodeActions(Document document, State state)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();
            var containingType = state.ContainingType;
            if (state.ConstructorCandidates.Length == 1)
            {
                // There will be at most 2 suggested code actions, so no need to use sub menus
                var constructorCandidate = state.ConstructorCandidates[0];
                if (CanHaveRequiredParameters(state.ConstructorCandidates[0].MissingParameters))
                {
                    result.Add(new AddConstructorParametersCodeAction(
                        document,
                        constructorCandidate,
                        containingType,
                        constructorCandidate.MissingParameters,
                        useSubMenuName: false));
                }
                result.Add(GetOptionalContructorParametersCodeAction(
                    document,
                    constructorCandidate,
                    containingType,
                    useSubMenuName: false));
            }
            else
            {
                // Create sub menus for suggested actions, one for required parameters and one for optional parameters
                var requiredParameterCodeActions = ArrayBuilder<CodeAction>.GetInstance();
                var optionalParameterCodeActions = ArrayBuilder<CodeAction>.GetInstance();
                foreach (var constructorCandidate in state.ConstructorCandidates)
                {
                    if (CanHaveRequiredParameters(constructorCandidate.Constructor.Parameters))
                    {
                        requiredParameterCodeActions.Add(new AddConstructorParametersCodeAction(
                            document,
                            constructorCandidate,
                            containingType,
                            constructorCandidate.MissingParameters,
                            useSubMenuName: true));
                    }
                    optionalParameterCodeActions.Add(GetOptionalContructorParametersCodeAction(
                        document,
                        constructorCandidate,
                        containingType,
                        useSubMenuName: true));
                }

                if (requiredParameterCodeActions.Count > 0)
                {
                    result.Add(new CodeAction.CodeActionWithNestedActions(
                        FeaturesResources.Add_parameter_to_constructor,
                        requiredParameterCodeActions.ToImmutableAndFree(),
                        isInlinable: false));
                }

                result.Add(new CodeAction.CodeActionWithNestedActions(
                    FeaturesResources.Add_optional_parameter_to_constructor,
                    optionalParameterCodeActions.ToImmutableAndFree(),
                    isInlinable: false));
            }

            return result.ToImmutableAndFree();

            // local functions
            static bool CanHaveRequiredParameters(ImmutableArray<IParameterSymbol> parameters)
                   => parameters.Length == 0 || !parameters.Last().IsOptional;

            static CodeAction GetOptionalContructorParametersCodeAction(Document document, ConstructorCandidate constructorCandidate, INamedTypeSymbol containingType, bool useSubMenuName)
            {
                var missingOptionalParameters = constructorCandidate.MissingParameters.SelectAsArray(
                    p => CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: p.RefKind,
                        isParams: p.IsParams,
                        type: p.Type,
                        name: p.Name,
                        isOptional: true,
                        hasDefaultValue: true));

                return new AddConstructorParametersCodeAction(
                    document, constructorCandidate, containingType, missingOptionalParameters, useSubMenuName);
            }
        }
    }
}
