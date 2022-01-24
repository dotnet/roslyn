﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.GenerateOverrides)]
    [IntentProvider(WellKnownIntents.AddConstructorParameter, LanguageNames.CSharp)]
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider, IIntentProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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

            var result = await AddConstructorParametersFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return;
            }

            var actions = GetGroupedActions(result.Value);
            context.RegisterRefactorings(actions);
        }

        private static async Task<AddConstructorParameterResult?> AddConstructorParametersFromMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
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
                    var state = await State.GenerateAsync(info.SelectedMembers, document, cancellationToken).ConfigureAwait(false);
                    if (state?.ConstructorCandidates != null && !state.ConstructorCandidates.IsEmpty)
                    {
                        return CreateCodeActions(document, state);
                    }
                }

                return null;
            }
        }

        private static ImmutableArray<CodeAction> GetGroupedActions(AddConstructorParameterResult result)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            if (result.UseSubMenu)
            {
                if (!result.RequiredParameterActions.IsDefaultOrEmpty)
                {
                    actions.Add(new CodeAction.CodeActionWithNestedActions(
                        FeaturesResources.Add_parameter_to_constructor,
                        result.RequiredParameterActions.Cast<AddConstructorParametersCodeAction, CodeAction>(),
                        isInlinable: false));
                }

                actions.Add(new CodeAction.CodeActionWithNestedActions(
                    FeaturesResources.Add_optional_parameter_to_constructor,
                    result.OptionalParameterActions.Cast<AddConstructorParametersCodeAction, CodeAction>(),
                    isInlinable: false));
            }
            else
            {
                // Not using submenus, this means we have at most a single of each action.
                if (!result.RequiredParameterActions.IsDefaultOrEmpty)
                {
                    actions.Add(result.RequiredParameterActions.Single());
                }

                actions.Add(result.OptionalParameterActions.Single());
            }

            return actions.ToImmutable();
        }

        private static AddConstructorParameterResult CreateCodeActions(Document document, State state)
        {
            using var _0 = ArrayBuilder<AddConstructorParametersCodeAction>.GetInstance(out var requiredParametersActions);
            using var _1 = ArrayBuilder<AddConstructorParametersCodeAction>.GetInstance(out var optionalParametersActions);
            var containingType = state.ContainingType;

            var useSubMenu = state.ConstructorCandidates.Length > 1;
            foreach (var constructorCandidate in state.ConstructorCandidates)
            {
                if (CanHaveRequiredParameters(constructorCandidate.Constructor.Parameters))
                {
                    requiredParametersActions.Add(new AddConstructorParametersCodeAction(
                        document,
                        constructorCandidate,
                        containingType,
                        constructorCandidate.MissingParameters,
                        useSubMenuName: useSubMenu));
                }

                optionalParametersActions.Add(GetOptionalContructorParametersCodeAction(
                    document,
                    constructorCandidate,
                    containingType,
                    useSubMenuName: useSubMenu));
            }

            return new AddConstructorParameterResult(requiredParametersActions.ToImmutable(), optionalParametersActions.ToImmutable(), useSubMenu);

            // local functions
            static bool CanHaveRequiredParameters(ImmutableArray<IParameterSymbol> parameters)
                   => parameters.Length == 0 || !parameters.Last().IsOptional;

            static AddConstructorParametersCodeAction GetOptionalContructorParametersCodeAction(Document document, ConstructorCandidate constructorCandidate, INamedTypeSymbol containingType, bool useSubMenuName)
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

        public async Task<ImmutableArray<IntentProcessorResult>> ComputeIntentAsync(Document priorDocument, TextSpan priorSelection, Document currentDocument, string? serializedIntentData, CancellationToken cancellationToken)
        {
            var addConstructorParametersResult = await AddConstructorParametersFromMembersAsync(priorDocument, priorSelection, cancellationToken).ConfigureAwait(false);
            if (addConstructorParametersResult == null)
            {
                return ImmutableArray<IntentProcessorResult>.Empty;
            }

            var actions = addConstructorParametersResult.Value.RequiredParameterActions.Concat(addConstructorParametersResult.Value.OptionalParameterActions);
            if (actions.IsEmpty)
            {
                return ImmutableArray<IntentProcessorResult>.Empty;
            }

            using var _ = ArrayBuilder<IntentProcessorResult>.GetInstance(out var results);
            foreach (var action in actions)
            {
                var changedSolution = await action.GetChangedSolutionInternalAsync(postProcessChanges: true, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(changedSolution);
                var intent = new IntentProcessorResult(changedSolution, action.Title, action.ActionName);
                results.Add(intent);
            }

            return results.ToImmutable();
        }
    }
}
