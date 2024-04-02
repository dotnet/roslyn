// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers;

internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
{
    private class GenerateConstructorWithDialogCodeAction(
        AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
        Document document,
        TextSpan textSpan,
        INamedTypeSymbol containingType,
        Accessibility? desiredAccessibility,
        ImmutableArray<ISymbol> viableMembers,
        ImmutableArray<PickMembersOption> pickMembersOptions,
        CleanCodeGenerationOptionsProvider fallbackOptions) : CodeActionWithOptions
    {
        private readonly Document _document = document;
        private readonly INamedTypeSymbol _containingType = containingType;
        private readonly Accessibility? _desiredAccessibility = desiredAccessibility;
        private readonly AbstractGenerateConstructorFromMembersCodeRefactoringProvider _service = service;
        private readonly TextSpan _textSpan = textSpan;
        private readonly CleanCodeGenerationOptionsProvider _fallbackOptions = fallbackOptions;

        internal ImmutableArray<ISymbol> ViableMembers { get; } = viableMembers;
        internal ImmutableArray<PickMembersOption> PickMembersOptions { get; } = pickMembersOptions;

        public override string Title => FeaturesResources.Generate_constructor;

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var service = _service._pickMembersService_forTesting ?? _document.Project.Solution.Services.GetRequiredService<IPickMembersService>();

            return service.PickMembers(
                FeaturesResources.Pick_members_to_be_used_as_constructor_parameters,
                ViableMembers, PickMembersOptions);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
            object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
        {
            var result = (PickMembersResult)options;
            if (result.IsCanceled)
            {
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }

            var addNullChecksOption = result.Options.FirstOrDefault(o => o.Id == AddNullChecksId);
            if (addNullChecksOption != null)
            {
                // ILegacyGlobalOptionsWorkspaceService is guaranteed to be not null here because we have checked it before the code action is provided.
                var globalOptions = _document.Project.Solution.Services.GetRequiredService<ILegacyGlobalOptionsWorkspaceService>();

                // If we presented the 'Add null check' option, then persist whatever value
                // the user chose.  That way we'll keep that as the default for the next time
                // the user opens the dialog.
                globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(_document.Project.Language, addNullChecksOption.Value);
            }

            var addNullChecks = (addNullChecksOption?.Value ?? false);
            var state = await State.TryGenerateAsync(
                _service, _document, _textSpan, _containingType, _desiredAccessibility,
                result.Members, _fallbackOptions, cancellationToken).ConfigureAwait(false);

            if (state == null)
            {
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }

            // There was an existing constructor that matched what the user wants to create.
            // Generate it if it's the implicit, no-arg, constructor, otherwise just navigate
            // to the existing constructor
            var solution = _document.Project.Solution;
            if (state.MatchingConstructor != null)
            {
                if (state.MatchingConstructor.IsImplicitlyDeclared)
                {
                    var codeAction = new FieldDelegatingCodeAction(_service, _document, state, addNullChecks, _fallbackOptions);
                    return await codeAction.GetOperationsAsync(solution, progressTracker, cancellationToken).ConfigureAwait(false);
                }

                var constructorReference = state.MatchingConstructor.DeclaringSyntaxReferences[0];
                var constructorSyntax = await constructorReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var constructorTree = constructorSyntax.SyntaxTree;
                var constructorDocument = solution.GetRequiredDocument(constructorTree);
                return ImmutableArray.Create<CodeActionOperation>(new DocumentNavigationOperation(
                    constructorDocument.Id, constructorSyntax.SpanStart));
            }
            else
            {
                var codeAction = state.DelegatedConstructor != null
                    ? new ConstructorDelegatingCodeAction(_service, _document, state, addNullChecks, _fallbackOptions)
                    : (CodeAction)new FieldDelegatingCodeAction(_service, _document, state, addNullChecks, _fallbackOptions);

                return await codeAction.GetOperationsAsync(solution, progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
