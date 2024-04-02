// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast;

internal sealed partial class CSharpAddExplicitCastCodeFixProvider
{
    private class ArgumentFixer : Fixer<ArgumentSyntax, ArgumentListSyntax, SyntaxNode>
    {
        protected override ExpressionSyntax GetExpressionOfArgument(ArgumentSyntax argument)
            => argument.Expression;

        protected override ArgumentSyntax GenerateNewArgument(ArgumentSyntax oldArgument, ITypeSymbol conversionType)
            => oldArgument.WithExpression(oldArgument.Expression.Cast(conversionType));

        protected override ArgumentListSyntax GenerateNewArgumentList(ArgumentListSyntax oldArgumentList, ArrayBuilder<ArgumentSyntax> newArguments)
            => oldArgumentList.WithArguments([.. newArguments]);

        protected override SeparatedSyntaxList<ArgumentSyntax> GetArgumentsOfArgumentList(ArgumentListSyntax argumentList)
            => argumentList.Arguments;

        protected override SymbolInfo GetSpeculativeSymbolInfo(SemanticModel semanticModel, ArgumentListSyntax newArgumentList)
        {
            var newInvocation = newArgumentList.Parent!;
            return semanticModel.GetSpeculativeSymbolInfo(newInvocation.SpanStart, newInvocation, SpeculativeBindingOption.BindAsExpression);
        }
    }
}
