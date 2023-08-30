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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
    {
        private class GenerateEqualsAndGetHashCodeWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly bool _generateEquals;
            private readonly bool _generateGetHashCode;
            private readonly GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly SyntaxNode _typeDeclaration;
            private readonly INamedTypeSymbol _containingType;
            private readonly ImmutableArray<ISymbol> _viableMembers;
            private readonly ImmutableArray<PickMembersOption> _pickMembersOptions;
            private readonly CleanCodeGenerationOptionsProvider _fallbackOptions;

            public GenerateEqualsAndGetHashCodeWithDialogCodeAction(
                GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider service,
                Document document,
                SyntaxNode typeDeclaration,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> viableMembers,
                ImmutableArray<PickMembersOption> pickMembersOptions,
                CleanCodeGenerationOptionsProvider fallbackOptions,
                bool generateEquals = false,
                bool generateGetHashCode = false)
            {
                _service = service;
                _document = document;
                _typeDeclaration = typeDeclaration;
                _containingType = containingType;
                _viableMembers = viableMembers;
                _pickMembersOptions = pickMembersOptions;
                _fallbackOptions = fallbackOptions;
                _generateEquals = generateEquals;
                _generateGetHashCode = generateGetHashCode;
            }

            public override string EquivalenceKey => Title;

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var service = _service._pickMembersService_forTestingPurposes ?? _document.Project.Solution.Services.GetRequiredService<IPickMembersService>();
                return service.PickMembers(FeaturesResources.Pick_members_to_be_used_in_Equals_GetHashCode,
                    _viableMembers, _pickMembersOptions);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
                }

                // If we presented the user any options, then persist whatever values
                // the user chose to the global options.  That way we'll keep that as the default for the
                // next time the user opens the dialog.
                var implementIEqutableOption = result.Options.FirstOrDefault(o => o.Id == ImplementIEquatableId);
                var generateOperatorsOption = result.Options.FirstOrDefault(o => o.Id == GenerateOperatorsId);
                if (generateOperatorsOption != null || implementIEqutableOption != null)
                {
                    var globalOptions = _document.Project.Solution.Services.GetRequiredService<ILegacyGlobalOptionsWorkspaceService>();

                    if (generateOperatorsOption != null)
                    {
                        globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(_document.Project.Language, generateOperatorsOption.Value);
                    }

                    if (implementIEqutableOption != null)
                    {
                        globalOptions.SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(_document.Project.Language, implementIEqutableOption.Value);
                    }
                }

                var implementIEquatable = implementIEqutableOption?.Value ?? false;
                var generatorOperators = generateOperatorsOption?.Value ?? false;

                var action = new GenerateEqualsAndGetHashCodeAction(
                    _document, _typeDeclaration, _containingType, result.Members, _fallbackOptions,
                    _generateEquals, _generateGetHashCode, implementIEquatable, generatorOperators);
                return await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            }

            public override string Title
                => GenerateEqualsAndGetHashCodeAction.GetTitle(_generateEquals, _generateGetHashCode) + "...";
        }
    }
}
