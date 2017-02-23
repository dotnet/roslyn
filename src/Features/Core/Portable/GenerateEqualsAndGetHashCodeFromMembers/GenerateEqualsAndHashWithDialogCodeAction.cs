// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Text;

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
            private readonly INamedTypeSymbol _containingType;
            private readonly ImmutableArray<ISymbol> _viableMembers;
            private readonly TextSpan _textSpan;

            public GenerateEqualsAndGetHashCodeWithDialogCodeAction(
                GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> viableMembers,
                bool generateEquals = false,
                bool generateGetHashCode = false)
            {
                _service = service;
                _document = document;
                _containingType = containingType;
                _viableMembers = viableMembers;
                _textSpan = textSpan;
                _generateEquals = generateEquals;
                _generateGetHashCode = generateGetHashCode;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var service = _document.Project.Solution.Workspace.Services.GetService<IPickMembersService>();
                return service.PickMembers(FeaturesResources.Pick_members_to_be_used_in_Equals_GetHashCode, _viableMembers);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                var action = new GenerateEqualsAndGetHashCodeAction(
                    _service, _document, _textSpan, _containingType, result.Members, _generateEquals, _generateGetHashCode);
                return await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            }

            public override string Title
                => GenerateEqualsAndGetHashCodeAction.GetTitle(_generateEquals, _generateGetHashCode);
        }
    }
}