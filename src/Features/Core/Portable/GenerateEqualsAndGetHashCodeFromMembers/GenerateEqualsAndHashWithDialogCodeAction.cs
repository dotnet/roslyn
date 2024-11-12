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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;

internal sealed partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
{
    private sealed class GenerateEqualsAndGetHashCodeWithDialogCodeAction(
        GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider service,
        Document document,
        SyntaxNode typeDeclaration,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol> viableMembers,
        ImmutableArray<PickMembersOption> pickMembersOptions,
        ILegacyGlobalOptionsWorkspaceService globalOptions,
        bool generateEquals = false,
        bool generateGetHashCode = false) : CodeActionWithOptions
    {
        private readonly bool _generateEquals = generateEquals;
        private readonly bool _generateGetHashCode = generateGetHashCode;
        private readonly GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider _service = service;
        private readonly Document _document = document;
        private readonly SyntaxNode _typeDeclaration = typeDeclaration;
        private readonly INamedTypeSymbol _containingType = containingType;
        private readonly ImmutableArray<ISymbol> _viableMembers = viableMembers;
        private readonly ImmutableArray<PickMembersOption> _pickMembersOptions = pickMembersOptions;
        private readonly ILegacyGlobalOptionsWorkspaceService _globalOptions = globalOptions;

        public override string EquivalenceKey => Title;

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var service = _service._pickMembersService_forTestingPurposes ?? _document.Project.Solution.Services.GetRequiredService<IPickMembersService>();
            return service.PickMembers(FeaturesResources.Pick_members_to_be_used_in_Equals_GetHashCode,
                _viableMembers, _pickMembersOptions);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
            object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
        {
            var result = (PickMembersResult)options;
            if (result.IsCanceled)
                return [];

            var solution = _document.Project.Solution;

            // If we presented the user any options, then persist whatever values
            // the user chose to the global options.  That way we'll keep that as the default for the
            // next time the user opens the dialog.
            var implementIEqutableOption = result.Options.FirstOrDefault(o => o.Id == ImplementIEquatableId);
            var generateOperatorsOption = result.Options.FirstOrDefault(o => o.Id == GenerateOperatorsId);
            if (generateOperatorsOption != null || implementIEqutableOption != null)
            {
                if (generateOperatorsOption != null)
                {
                    _globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(_document.Project.Language, generateOperatorsOption.Value);
                }

                if (implementIEqutableOption != null)
                {
                    _globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(_document.Project.Language, implementIEqutableOption.Value);
                }
            }

            var implementIEquatable = implementIEqutableOption?.Value ?? false;
            var generatorOperators = generateOperatorsOption?.Value ?? false;

            var action = new GenerateEqualsAndGetHashCodeAction(
                _document, _typeDeclaration, _containingType, result.Members,
                _generateEquals, _generateGetHashCode, implementIEquatable, generatorOperators);
            return await action.GetOperationsAsync(solution, progressTracker, cancellationToken).ConfigureAwait(false);
        }

        public override string Title
            => GenerateEqualsAndGetHashCodeAction.GetTitle(_generateEquals, _generateGetHashCode) + "...";
    }
}
