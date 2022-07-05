﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    internal static class NodeSelectionHelpers
    {
        internal static async Task<ImmutableArray<SyntaxNode>> GetSelectedDeclarationsOrVariablesAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (span.IsEmpty)
            {
                // if the span is empty then we are only selecting one "member" (which could include a field which declared multiple actual members)
                // Consider:
                // MemberDeclaration: member that can be declared in type (those are the ones we can pull up) 
                // VariableDeclaratorSyntax: for fields the MemberDeclaration can actually represent multiple declarations, e.g. `int a = 0, b = 1;`.
                // ..Since the user might want to select & pull up only one of them (e.g. `int a = 0, [|b = 1|];` we also look for closest VariableDeclaratorSyntax.
                var memberDeclaration = await context.TryGetRelevantNodeAsync<MemberDeclarationSyntax>().ConfigureAwait(false);
                if (memberDeclaration == null)
                {
                    // could not find a member, we may be directly on a variable declaration
                    var varDeclarator = await context.TryGetRelevantNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
                    return varDeclarator == null
                        ? ImmutableArray<SyntaxNode>.Empty
                        : ImmutableArray.Create<SyntaxNode>(varDeclarator);
                }
                else
                {
                    return memberDeclaration switch
                    {
                        FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Variables.AsImmutable<SyntaxNode>(),
                        EventFieldDeclarationSyntax eventFieldDeclaration => eventFieldDeclaration.Declaration.Variables.AsImmutable<SyntaxNode>(),
                        _ => ImmutableArray.Create<SyntaxNode>(memberDeclaration),
                    };
                }
            }
            else
            {
                // if the span is non-empty, then we get potentially multiple members
                // Note: even though this method handles the empty span case, we don't use it because it doesn't correctly
                // pick up on keywords before the declaration, such as "public static int".
                // We could potentially use it for every case if that behavior changes
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return await CSharpSelectedMembers.Instance.GetSelectedMembersAsync(tree, span, allowPartialSelection: true, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
