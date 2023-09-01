// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateOverrides
{
    internal partial class GenerateOverridesCodeRefactoringProvider
    {
        private sealed class GenerateOverridesWithDialogCodeAction(
            GenerateOverridesCodeRefactoringProvider service,
            Document document,
            TextSpan textSpan,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> viableMembers,
            CodeAndImportGenerationOptionsProvider fallbackOptions) : CodeActionWithOptions
        {
            private readonly GenerateOverridesCodeRefactoringProvider _service = service;
            private readonly Document _document = document;
            private readonly INamedTypeSymbol _containingType = containingType;
            private readonly ImmutableArray<ISymbol> _viableMembers = viableMembers;
            private readonly TextSpan _textSpan = textSpan;
            private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions = fallbackOptions;

            public override string Title => FeaturesResources.Generate_overrides;

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var services = _document.Project.Solution.Services;
                var pickMembersService = _service._pickMembersService_forTestingPurposes ?? services.GetRequiredService<IPickMembersService>();
                var globalOptionService = services.GetService<ILegacyGlobalOptionsWorkspaceService>();

                return pickMembersService.PickMembers(
                    FeaturesResources.Pick_members_to_override,
                    _viableMembers,
                    selectAll: globalOptionService?.GenerateOverrides ?? true);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled || result.Members.Length == 0)
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                RoslynDebug.AssertNotNull(syntaxTree);

                // If the user has selected just one member then we will insert it at the current
                // location.  Otherwise, if it's many members, then we'll auto insert them as appropriate.
                var afterThisLocation = result.Members.Length == 1
                    ? syntaxTree.GetLocation(_textSpan)
                    : null;

                var generator = SyntaxGenerator.GetGenerator(_document);
                var memberTasks = result.Members.SelectAsArray(
                    m => GenerateOverrideAsync(generator, m, cancellationToken));

                var members = await Task.WhenAll(memberTasks).ConfigureAwait(false);

                var newDocument = await CodeGenerator.AddMemberDeclarationsAsync(
                    new CodeGenerationSolutionContext(
                        _document.Project.Solution,
                        new CodeGenerationContext(
                            afterThisLocation: afterThisLocation,
                            contextLocation: syntaxTree.GetLocation(_textSpan)),
                        _fallbackOptions),
                    _containingType,
                    members,
                    cancellationToken).ConfigureAwait(false);

                return new CodeActionOperation[]
                    {
                        new ApplyChangesOperation(newDocument.Project.Solution),
                        new ChangeOptionValueOperation(result.SelectedAll),
                    };
            }

            private Task<ISymbol> GenerateOverrideAsync(
                SyntaxGenerator generator, ISymbol symbol,
                CancellationToken cancellationToken)
            {
                return generator.OverrideAsync(
                    symbol, _containingType, _document,
                    cancellationToken: cancellationToken);
            }

            private sealed class ChangeOptionValueOperation(bool selectedAll) : CodeActionOperation
            {
                private readonly bool _selectedAll = selectedAll;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    var service = workspace.Services.GetService<ILegacyGlobalOptionsWorkspaceService>();
                    if (service != null)
                    {
                        service.GenerateOverrides = _selectedAll;
                    }
                }
            }
        }
    }
}
