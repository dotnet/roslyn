// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
    {
        private class GenerateEqualsAndGetHashCodeWithDialogCodeAction(
            GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider service,
            Document document,
            SyntaxNode typeDeclaration,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> viableMembers,
            ImmutableArray<PickMembersOption> pickMembersOptions,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            ILegacyGlobalOptionsWorkspaceService globalOptions,
            bool generateEquals = false,
            bool generateGetHashCode = false) : CodeActionWithOptions
        {
            private readonly GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider _service = service;

            public override string EquivalenceKey => Title;

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var service = _service._pickMembersService_forTestingPurposes ?? document.Project.Solution.Services.GetRequiredService<IPickMembersService>();
                return service.PickMembers(FeaturesResources.Pick_members_to_be_used_in_Equals_GetHashCode,
                    viableMembers, pickMembersOptions);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled)
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();

                var solution = document.Project.Solution;

                // If we presented the user any options, then persist whatever values
                // the user chose to the global options.  That way we'll keep that as the default for the
                // next time the user opens the dialog.
                var implementIEqutableOption = result.Options.FirstOrDefault(o => o.Id == ImplementIEquatableId);
                var generateOperatorsOption = result.Options.FirstOrDefault(o => o.Id == GenerateOperatorsId);
                if (generateOperatorsOption != null || implementIEqutableOption != null)
                {
                    if (generateOperatorsOption != null)
                    {
                        globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(document.Project.Language, generateOperatorsOption.Value);
                    }

                    if (implementIEqutableOption != null)
                    {
                        globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(document.Project.Language, implementIEqutableOption.Value);
                    }
                }

                var implementIEquatable = implementIEqutableOption?.Value ?? false;
                var generatorOperators = generateOperatorsOption?.Value ?? false;

                var action = new GenerateEqualsAndGetHashCodeAction(
                    document, typeDeclaration, containingType, result.Members, fallbackOptions,
                    generateEquals, generateGetHashCode, implementIEquatable, generatorOperators);
                return await action.GetOperationsAsync(solution, new ProgressTracker(), cancellationToken).ConfigureAwait(false);
            }

            public override string Title
                => GenerateEqualsAndGetHashCodeAction.GetTitle(generateEquals, generateGetHashCode) + "...";
        }
    }
}
