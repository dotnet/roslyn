// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    internal static class NodeSelectionHelpers
    {
        internal static async Task<ImmutableArray<SyntaxNode>> GetSelectedDeclarationsOrVariablesAsync(CodeRefactoringContext context)
        {
            // Consider:
            // MemberDeclaration: member that can be declared in type (those are the ones we can pull up) 
            // VariableDeclaratorSyntax: for fields the MemberDeclaration can actually represent multiple declarations, e.g. `int a = 0, b = 1;`.
            // ..Since the user might want to select & pull up only one of them (e.g. `int a = 0, [|b = 1|];` we also look for closest VariableDeclaratorSyntax.
            var memberDeclarations = await context.GetRelevantNodesAsync<MemberDeclarationSyntax>().ConfigureAwait(false);
            var varDeclarators = await context.GetRelevantNodesAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
            return memberDeclarations
                .SelectAsArray<MemberDeclarationSyntax, SyntaxNode>(
                    memberDeclaration => memberDeclaration is FieldDeclarationSyntax fieldDeclaration ? fieldDeclaration.Declaration : memberDeclaration)
                .Concat(varDeclarators.Cast<SyntaxNode>())
                .AsImmutable();
        }
    }
}
