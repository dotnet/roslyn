// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateOverrides
{
    internal partial class GenerateOverridesCodeRefactoringProvider
    {
        private class GenerateOverridesWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly GenerateOverridesCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly ImmutableArray<ISymbol> _viableMembers;
            private readonly TextSpan _textSpan;

            public GenerateOverridesWithDialogCodeAction(
                GenerateOverridesCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> viableMembers)
            {
                _service = service;
                _document = document;
                _containingType = containingType;
                _viableMembers = viableMembers;
                _textSpan = textSpan;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var service = _service._pickMembersService_forTestingPurposes ?? _document.Project.Solution.Workspace.Services.GetService<IPickMembersService>();
                return service.PickMembers(FeaturesResources.Pick_members_to_override, _viableMembers);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled || result.Members.Length == 0)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

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
                    _document.Project.Solution,
                    _containingType,
                    members,
                    new CodeGenerationOptions(
                        afterThisLocation: afterThisLocation,
                        contextLocation: syntaxTree.GetLocation(_textSpan)),
                    cancellationToken).ConfigureAwait(false);

                return SpecializedCollections.SingletonEnumerable(
                    new ApplyChangesOperation(newDocument.Project.Solution));
            }

            private Task<ISymbol> GenerateOverrideAsync(
                SyntaxGenerator generator, ISymbol symbol,
                CancellationToken cancellationToken)
            {
                return generator.OverrideAsync(
                    symbol, _containingType, _document,
                    cancellationToken: cancellationToken);
            }

            public override string Title => FeaturesResources.Generate_overrides;
        }
    }
}
