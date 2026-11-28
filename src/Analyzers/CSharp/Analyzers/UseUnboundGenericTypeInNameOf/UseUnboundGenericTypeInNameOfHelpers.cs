// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseUnboundGenericTypeInNameOf;

using static SyntaxFactory;

internal static class UseUnboundGenericTypeInNameOfHelpers
{
    private static readonly SyntaxNodeOrToken s_omittedArgument = (SyntaxNodeOrToken)OmittedTypeArgument();

    public static TSyntax ConvertToUnboundGenericNameof<TSyntax>(TSyntax syntax)
        where TSyntax : SyntaxNode
    {
        return syntax.ReplaceNodes(
            syntax.DescendantNodes().OfType<TypeArgumentListSyntax>(),
            (original, current) =>
            {
                if (current.Arguments.Any(a => a.Kind() != SyntaxKind.OmittedTypeArgument))
                {
                    var list = NodeOrTokenList(current.Arguments.GetWithSeparators().Select(
                        t => t.IsToken ? t.AsToken().WithoutTrivia() : s_omittedArgument));
                    return current.WithArguments(SeparatedList<TypeSyntax>(list));
                }

                return current;
            });
    }
}
