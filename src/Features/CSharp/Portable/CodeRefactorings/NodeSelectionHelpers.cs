// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    internal static class NodeSelectionHelpers
    {
        internal static async Task<SyntaxNode> GetSelectedDeclarationOrVariableAsync(CodeRefactoringContext context)
        {
            // Consider:
            // MemberDeclaration: member that can be declared in type (those are the ones we can pull up) 
            // VariableDeclaratorSyntax: for fields the MemberDeclaration can actually represent multiple declarations, e.g. `int a = 0, b = 1;`.
            // ..Since the user might want to select & pull up only one of them (e.g. `int a = 0, [|b = 1|];` we also look for closest VariableDeclaratorSyntax.
            // Note: VariableDeclaratorSyntax needs to be fetched so that the caller could get correct field symbol,
            // because field symbol is binded to VariableDeclaratorSyntax instead of the FieldDeclarationSyntax
            return await context.TryGetRelevantNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false) as SyntaxNode ??
                await context.TryGetRelevantNodeAsync<MemberDeclarationSyntax>().ConfigureAwait(false);
        }
    }
}
