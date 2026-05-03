// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        public static SyntaxNode WalkDownParentheses(this SyntaxNode node)
        {
            SyntaxNode current = node;
            while (current.IsKind(SyntaxKind.ParenthesizedExpression) && current.ChildNodes().FirstOrDefault() is SyntaxNode expression)
            {
                current = expression;
            }

            return current;
        }
    }
}
