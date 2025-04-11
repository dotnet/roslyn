// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class EnumMemberGenerator
{
    internal static EnumDeclarationSyntax AddEnumMemberTo(EnumDeclarationSyntax destination, IFieldSymbol enumMember, CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        var members = new List<SyntaxNodeOrToken>();
        members.AddRange(destination.Members.GetWithSeparators());

        var member = GenerateEnumMemberDeclaration(enumMember, destination, info, cancellationToken);

        if (members.Count == 0)
        {
            members.Add(member);
        }
        else if (members.LastOrDefault().Kind() == SyntaxKind.CommaToken)
        {
            members.Add(member);
            members.Add(CommaToken);
        }
        else
        {
            var lastMember = members.Last();
            var trailingTrivia = lastMember.GetTrailingTrivia();
            members[^1] = lastMember.WithTrailingTrivia();
            members.Add(CommaToken.WithTrailingTrivia(trailingTrivia));
            members.Add(member);
        }

        return destination.EnsureOpenAndCloseBraceTokens()
            .WithMembers(SeparatedList<EnumMemberDeclarationSyntax>(members));
    }

    public static EnumMemberDeclarationSyntax GenerateEnumMemberDeclaration(
        IFieldSymbol enumMember,
        EnumDeclarationSyntax? destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<EnumMemberDeclarationSyntax>(enumMember, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var value = CreateEnumMemberValue(destination, enumMember);
        var member = EnumMemberDeclaration(enumMember.Name.ToIdentifierToken())
            .WithEqualsValue(value == null ? null : EqualsValueClause(value: value));

        return AddFormatterAndCodeGeneratorAnnotationsTo(
            ConditionallyAddDocumentationCommentTo(member, enumMember, info, cancellationToken));
    }

    private static ExpressionSyntax? CreateEnumMemberValue(
        EnumDeclarationSyntax? destination, IFieldSymbol enumMember)
    {
        if (!enumMember.HasConstantValue)
        {
            return null;
        }

        if (enumMember.ConstantValue is not byte and
            not sbyte and
            not ushort and
            not short and
            not int and
            not uint and
            not long and
            not ulong)
        {
            return null;
        }

        var value = IntegerUtilities.ToInt64(enumMember.ConstantValue);

        if (destination != null)
        {
            if (destination.Members.Count == 0)
            {
                if (value == 0)
                {
                    return null;
                }
            }
            else
            {
                // Don't generate an initializer if no other members have them, and our value
                // would be correctly inferred from our position.
                if (destination.Members.Count == value &&
                    destination.Members.All(m => m.EqualsValue == null))
                {
                    return null;
                }

                // Existing members, try to stay consistent with their style.
                var lastMember = destination.Members.LastOrDefault(m => m.EqualsValue != null);
                if (lastMember != null)
                {
                    var lastExpression = lastMember.EqualsValue!.Value;
                    if (lastExpression.Kind() == SyntaxKind.LeftShiftExpression &&
                        IntegerUtilities.HasOneBitSet(value))
                    {
                        var binaryExpression = (BinaryExpressionSyntax)lastExpression;
                        if (binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression)
                        {
                            var numericLiteral = (LiteralExpressionSyntax)binaryExpression.Left;
                            if (numericLiteral.Token.ValueText == "1")
                            {
                                // The user is left shifting ones, stick with that pattern
                                var shiftValue = IntegerUtilities.LogBase2(value);

                                // Re-use the numericLiteral text so type suffixes match too
                                return BinaryExpression(
                                    SyntaxKind.LeftShiftExpression,
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(numericLiteral.Token.Text, 1)),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(shiftValue.ToString(), shiftValue)));
                            }
                        }
                    }
                    else if (lastExpression is LiteralExpressionSyntax(SyntaxKind.NumericLiteralExpression) numericLiteral)
                    {
                        var numericToken = numericLiteral.Token;
                        var numericText = numericToken.ToString();

                        if (numericText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            // Hex
                            return LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal(numericText[..2] + value.ToString("X"), value));
                        }
                        else if (numericText.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                        {
                            return LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal(numericText[..2] + Convert.ToString(value, 2), value));
                        }
                    }
                }
            }
        }

        var namedType = enumMember.Type as INamedTypeSymbol;
        var underlyingType = namedType?.EnumUnderlyingType;

        return ExpressionGenerator.GenerateNonEnumValueExpression(
            underlyingType,
            enumMember.ConstantValue,
            canUseFieldReference: true);
    }
}
