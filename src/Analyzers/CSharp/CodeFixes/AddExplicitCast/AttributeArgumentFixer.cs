// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    internal sealed partial class CSharpAddExplicitCastCodeFixProvider
    {
        private class AttributeArgumentFixer : Fixer<AttributeArgumentSyntax, AttributeArgumentListSyntax, AttributeSyntax>
        {
            protected override ExpressionSyntax GetExpressionOfArgument(AttributeArgumentSyntax argument)
                => argument.Expression;

            protected override AttributeArgumentSyntax GenerateNewArgument(AttributeArgumentSyntax oldArgument, ITypeSymbol conversionType)
                => oldArgument.WithExpression(oldArgument.Expression.Cast(conversionType));

            protected override AttributeArgumentListSyntax GenerateNewArgumentList(AttributeArgumentListSyntax oldArgumentList, ArrayBuilder<AttributeArgumentSyntax> newArguments)
                => oldArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));

            protected override SeparatedSyntaxList<AttributeArgumentSyntax> GetArgumentsOfArgumentList(AttributeArgumentListSyntax argumentList)
                => argumentList.Arguments;

            protected override SymbolInfo GetSpeculativeSymbolInfo(SemanticModel semanticModel, AttributeArgumentListSyntax newArgumentList)
            {
                var newAttribute = (AttributeSyntax)newArgumentList.Parent!;
                return semanticModel.GetSpeculativeSymbolInfo(newAttribute.SpanStart, newAttribute);
            }
        }
    }
}
