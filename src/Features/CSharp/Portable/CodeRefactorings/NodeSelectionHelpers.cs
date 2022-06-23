// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
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
            var memberDeclaration = await context.TryGetRelevantNodeAsync<MemberDeclarationSyntax>().ConfigureAwait(false);
            if (memberDeclaration == null)
            {
                return await context.TryGetRelevantNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
            }

            if (memberDeclaration is not FieldDeclarationSyntax fieldDeclaration)
            {
                return memberDeclaration;
            }

            // since a FieldDeclarationSyntax doesn't have an associated Symbol (which we will want later), we want the variable declaration they ask for
            // find the variable declarator closest to the start of the span
            var span = context.Span;
            return fieldDeclaration.Declaration.Variables.OrderBy(
                v => Math.Min(Math.Abs(v.SpanStart - span.Start), Math.Abs(v.Span.End - span.Start))).First();
        }
    }
}
