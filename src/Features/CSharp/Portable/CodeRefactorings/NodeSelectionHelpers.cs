// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
            var memberDeclaration = await context.TryGetRelevantNodeAsync<MemberDeclarationSyntax>().ConfigureAwait(false) as SyntaxNode;
            if (memberDeclaration == null)
            {
                return await context.TryGetRelevantNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
            }
            // since a FieldDeclarationSyntax doesn't have an associated Symbol (it can have multiple), we just get its variable declaration instead
            if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
            {
                // find the variable declarator closest to the start of the span
                var (_, span, _) = context;
                VariableDeclaratorSyntax closestDeclarator = null;
                var leastDistance = int.MaxValue;
                foreach (var candidate in fieldDeclaration.Declaration.Variables)
                {
                    var dist = Math.Min(Math.Abs(candidate.SpanStart - span.Start), Math.Abs(candidate.Span.End - span.Start));
                    if (dist < leastDistance)
                    {
                        closestDeclarator = candidate;
                        leastDistance = dist;
                    }
                }
                return closestDeclarator;
            }
            return memberDeclaration;
        }
    }
}
