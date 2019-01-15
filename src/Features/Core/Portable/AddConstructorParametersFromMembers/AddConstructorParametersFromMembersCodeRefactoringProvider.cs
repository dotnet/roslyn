// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.GenerateOverrides)]
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await this.AddConstructorParametersFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }

        public async Task<ImmutableArray<CodeAction>> AddConstructorParametersFromMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_AddConstructorParametersFromMembers, cancellationToken))
            {
                var info = await this.GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = State.Generate(this, info.SelectedMembers);
                    if (state != null && state.DelegatedConstructor != null)
                    {
                        return CreateCodeActions(document, state).AsImmutableOrNull();
                    }
                }

                return default;
            }
        }

        /// <summary>
        /// Try to find a constructor in <paramref name="containingType"/> whose parameters is the subset of <paramref name="parameters"/> by comparing name.
        /// If multiple constructors meet the condition, the one with more parameters will be returned.
        /// It will not consider those constructors as potential candidates
        /// 1. Constructor with empty parameter list.
        /// 2. Constructor's parameter list contains 'ref' or 'params'
        /// </summary>
        protected override IMethodSymbol GetDelegatedConstructor(
            INamedTypeSymbol containingType,
            ImmutableArray<IParameterSymbol> parameters)
        {
            return containingType.InstanceConstructors
                .WhereAsArray(constructor => IsParamtersContainedInConstructor(constructor, parameters.SelectAsArray(p => p.Name)))
                .OrderByDescending(constructor => constructor.Parameters.Length)
                .FirstOrDefault();
        }

        private bool IsParamtersContainedInConstructor(
            IMethodSymbol constructor,
            ImmutableArray<string> parametersName)
        {
            var constructorParams = constructor.Parameters;
            if (constructorParams.Length > 0
                && constructorParams.All(parameter => parameter.RefKind == RefKind.None)
                && !constructorParams.Any(p => p.IsParams))
            {
                return parametersName.Except(constructorParams.Select(p => p.Name)).Any();
            }
            else
            {
                return false;
            }
        }

        private IEnumerable<CodeAction> CreateCodeActions(Document document, State state)
        {
            var lastParameter = state.DelegatedConstructor.Parameters.Last();
            if (!lastParameter.IsOptional)
            {
                yield return new AddConstructorParametersCodeAction(this, document, state, state.MissingParameters);
            }

            var missingOptionalParameters = state.MissingParameters.SelectAsArray(p => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: p.RefKind,
                isParams: p.IsParams,
                type: p.Type,
                name: p.Name,
                isOptional: true,
                hasDefaultValue: true));

            yield return new AddConstructorParametersCodeAction(this, document, state, missingOptionalParameters);
        }
    }
}
