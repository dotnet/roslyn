// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static OperatorDeclarationSyntax GenerateOperator(IMethodSymbol method)
        {
            var operatorToken = method.Name switch
            {
                WellKnownMemberNames.AdditionOperatorName => SyntaxKind.PlusToken,
                WellKnownMemberNames.BitwiseAndOperatorName => SyntaxKind.AmpersandToken,
                WellKnownMemberNames.BitwiseOrOperatorName => SyntaxKind.BarToken,
                WellKnownMemberNames.DecrementOperatorName => SyntaxKind.MinusMinusToken,
                WellKnownMemberNames.DivisionOperatorName => SyntaxKind.SlashToken,
                WellKnownMemberNames.EqualityOperatorName => SyntaxKind.EqualsEqualsToken,
                WellKnownMemberNames.ExclusiveOrOperatorName => SyntaxKind.CaretToken,
                WellKnownMemberNames.FalseOperatorName => SyntaxKind.FalseKeyword,
                WellKnownMemberNames.GreaterThanOperatorName => SyntaxKind.GreaterThanToken,
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => SyntaxKind.GreaterThanEqualsToken,
                WellKnownMemberNames.IncrementOperatorName => SyntaxKind.PlusPlusToken,
                WellKnownMemberNames.InequalityOperatorName => SyntaxKind.ExclamationEqualsToken,
                WellKnownMemberNames.LeftShiftOperatorName => SyntaxKind.LessThanLessThanToken,
                WellKnownMemberNames.LessThanOperatorName => SyntaxKind.LessThanToken,
                WellKnownMemberNames.LessThanOrEqualOperatorName => SyntaxKind.LessThanEqualsToken,
                WellKnownMemberNames.LogicalNotOperatorName => SyntaxKind.ExclamationToken,
                WellKnownMemberNames.ModulusOperatorName => SyntaxKind.PercentToken,
                WellKnownMemberNames.MultiplyOperatorName => SyntaxKind.AsteriskToken,
                WellKnownMemberNames.OnesComplementOperatorName => SyntaxKind.TildeToken,
                WellKnownMemberNames.RightShiftOperatorName => SyntaxKind.GreaterThanGreaterThanToken,
                WellKnownMemberNames.SubtractionOperatorName => SyntaxKind.MinusToken,
                WellKnownMemberNames.TrueOperatorName => SyntaxKind.TrueKeyword,
                WellKnownMemberNames.UnaryPlusOperatorName => SyntaxKind.PlusToken,
                WellKnownMemberNames.UnaryNegationOperatorName => SyntaxKind.MinusToken,
                _ => throw new ArgumentException($"Operator {method.Name} not supported in C#"),
            };

            return OperatorDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method.DeclaredAccessibility, method.GetModifiers()),
                method.ReturnType.GenerateTypeSyntax(),
                Token(operatorToken),
                GenerateParameterList(method.Parameters),
                Block(),
                expressionBody: null);
        }
    }
}
