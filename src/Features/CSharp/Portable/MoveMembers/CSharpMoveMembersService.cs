// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MoveMembers
{
    [ExportLanguageService(typeof(AbstractMoveMembersService), LanguageNames.CSharp)]
    [Shared]
    class CSharpMoveMembersService : AbstractMoveMembersService
    {
        [ImportingConstructor]
        public CSharpMoveMembersService()
        {
        }

        protected override async Task<SyntaxNode?> GetSelectedMemberNodeAsync(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var helper = document.GetRequiredLanguageService<IRefactoringHelpersService>();

            // Consider:
            // MemberDeclaration: member that can be declared in type (those are the ones we can pull up) 
            // VariableDeclaratorSyntax: for fields the MemberDeclaration can actually represent multiple declarations, e.g. `int a = 0, b = 1;`.
            // ..Since the user might want to select & pull up only one of them (e.g. `int a = 0, [|b = 1|];` we also look for closest VariableDeclaratorSyntax.
            return (await helper.GetRelevantNodesAsync<MemberDeclarationSyntax>(document, selection, cancellationToken).ConfigureAwait(false)).FirstOrDefault() as SyntaxNode ??
                (await helper.GetRelevantNodesAsync<VariableDeclaratorSyntax>(document, selection, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        }

        protected override Task<Solution> UpdateMembersWithExplicitImplementationsAsync(
            Solution unformattedSolution, IReadOnlyList<DocumentId> documentIds,
            INamedTypeSymbol extractedInterface,
            IEnumerable<ISymbol> includedMembers, Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationMap,
            CancellationToken cancellationToken)
        {
            // In C#, member implementations do not always need
            // to be explicitly added. It's safe enough to return
            // the passed in solution
            return Task.FromResult(unformattedSolution);
        }
    }
}
