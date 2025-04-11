// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal sealed class CSharpDeclarationComparer : IComparer<SyntaxNode>
{
    private static readonly Dictionary<SyntaxKind, int> s_kindPrecedenceMap = new(SyntaxFacts.EqualityComparer)
    {
        { SyntaxKind.FieldDeclaration, 0 },
        { SyntaxKind.ConstructorDeclaration, 1 },
        { SyntaxKind.DestructorDeclaration, 2 },
        { SyntaxKind.IndexerDeclaration, 3 },
        { SyntaxKind.PropertyDeclaration, 4 },
        { SyntaxKind.EventFieldDeclaration, 5 },
        { SyntaxKind.EventDeclaration, 6 },
        { SyntaxKind.MethodDeclaration, 7 },
        { SyntaxKind.OperatorDeclaration, 8 },
        { SyntaxKind.ConversionOperatorDeclaration, 9 },
        { SyntaxKind.EnumDeclaration, 10 },
        { SyntaxKind.InterfaceDeclaration, 11 },
        { SyntaxKind.StructDeclaration, 12 },
        { SyntaxKind.ClassDeclaration, 13 },
        { SyntaxKind.RecordDeclaration, 14 },
        { SyntaxKind.RecordStructDeclaration, 15 },
        { SyntaxKind.DelegateDeclaration, 16 }
    };

    private static readonly Dictionary<SyntaxKind, int> s_operatorPrecedenceMap = new(SyntaxFacts.EqualityComparer)
    {
        { SyntaxKind.PlusToken, 0 },
        { SyntaxKind.MinusToken, 1 },
        { SyntaxKind.ExclamationToken, 2 },
        { SyntaxKind.TildeToken, 3 },
        { SyntaxKind.PlusPlusToken, 4 },
        { SyntaxKind.MinusMinusToken, 5 },
        { SyntaxKind.AsteriskToken, 6 },
        { SyntaxKind.SlashToken, 7 },
        { SyntaxKind.PercentToken, 8 },
        { SyntaxKind.AmpersandToken, 9 },
        { SyntaxKind.BarToken, 10 },
        { SyntaxKind.CaretToken, 11 },
        { SyntaxKind.LessThanLessThanToken, 12 },
        { SyntaxKind.GreaterThanGreaterThanToken, 13 },
        { SyntaxKind.EqualsEqualsToken, 14 },
        { SyntaxKind.ExclamationEqualsToken, 15 },
        { SyntaxKind.LessThanToken, 16 },
        { SyntaxKind.GreaterThanToken, 17 },
        { SyntaxKind.LessThanEqualsToken, 18 },
        { SyntaxKind.GreaterThanEqualsToken, 19 },
        { SyntaxKind.TrueKeyword, 20 },
        { SyntaxKind.FalseKeyword, 21 },
        { SyntaxKind.GreaterThanGreaterThanGreaterThanToken, 22 },
    };

    public static readonly CSharpDeclarationComparer WithNamesInstance = new(includeName: true);
    public static readonly CSharpDeclarationComparer WithoutNamesInstance = new(includeName: false);

    private readonly bool _includeName;

    private CSharpDeclarationComparer(bool includeName)
        => _includeName = includeName;

    public int Compare(SyntaxNode? x, SyntaxNode? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        if (x.Kind() != y.Kind())
        {
            if (!s_kindPrecedenceMap.TryGetValue(x.Kind(), out var xPrecedence) ||
                !s_kindPrecedenceMap.TryGetValue(y.Kind(), out var yPrecedence))
            {
                // The containing declaration is malformed and contains a node kind we did not expect.
                // Ignore comparisons with those unexpected nodes and sort them to the end of the declaration.
                return 1;
            }

            return xPrecedence < yPrecedence ? -1 : 1;
        }

        switch (x.Kind())
        {
            case SyntaxKind.DelegateDeclaration:
                return Compare((DelegateDeclarationSyntax)x, (DelegateDeclarationSyntax)y);

            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.EventFieldDeclaration:
                return Compare((BaseFieldDeclarationSyntax)x, (BaseFieldDeclarationSyntax)y);

            case SyntaxKind.ConstructorDeclaration:
                return Compare((ConstructorDeclarationSyntax)x, (ConstructorDeclarationSyntax)y);

            case SyntaxKind.DestructorDeclaration:
                // All destructors are equal since there can only be one per named type
                return 0;

            case SyntaxKind.MethodDeclaration:
                return Compare((MethodDeclarationSyntax)x, (MethodDeclarationSyntax)y);

            case SyntaxKind.OperatorDeclaration:
                return Compare((OperatorDeclarationSyntax)x, (OperatorDeclarationSyntax)y);

            case SyntaxKind.EventDeclaration:
                return Compare((EventDeclarationSyntax)x, (EventDeclarationSyntax)y);

            case SyntaxKind.IndexerDeclaration:
                return Compare((IndexerDeclarationSyntax)x, (IndexerDeclarationSyntax)y);

            case SyntaxKind.PropertyDeclaration:
                return Compare((PropertyDeclarationSyntax)x, (PropertyDeclarationSyntax)y);

            case SyntaxKind.EnumDeclaration:
                return Compare((EnumDeclarationSyntax)x, (EnumDeclarationSyntax)y);

            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
                return Compare((BaseTypeDeclarationSyntax)x, (BaseTypeDeclarationSyntax)y);

            case SyntaxKind.ConversionOperatorDeclaration:
                return Compare((ConversionOperatorDeclarationSyntax)x, (ConversionOperatorDeclarationSyntax)y);

            case SyntaxKind.IncompleteMember:
                // Since these are incomplete members they are considered to be equal
                return 0;
            case SyntaxKind.GlobalStatement:
                // for REPL, don't mess with order, just put new one at the end.
                return 1;
            default:
                throw ExceptionUtilities.UnexpectedValue(x.Kind());
        }
    }

    private int Compare(DelegateDeclarationSyntax x, DelegateDeclarationSyntax y)
    {
        if (EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out var result))
        {
            if (_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private int Compare(BaseFieldDeclarationSyntax x, BaseFieldDeclarationSyntax y)
    {
        if (EqualConstness(x.Modifiers, y.Modifiers, out var result) &&
            EqualStaticness(x.Modifiers, y.Modifiers, out result) &&
            EqualReadOnlyness(x.Modifiers, y.Modifiers, out result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            if (_includeName)
            {
                EqualIdentifierName(
                    x.Declaration.Variables.First().Identifier,
                    y.Declaration.Variables.First().Identifier,
                    out result);
            }
        }

        return result;
    }

    private static int Compare(ConstructorDeclarationSyntax x, ConstructorDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            EqualParameterCount(x.ParameterList, y.ParameterList, out result);
        }

        return result;
    }

    private int Compare(MethodDeclarationSyntax x, MethodDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            if (!_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private static int Compare(ConversionOperatorDeclarationSyntax x, ConversionOperatorDeclarationSyntax y)
    {
        if (x.ImplicitOrExplicitKeyword.Kind() != y.ImplicitOrExplicitKeyword.Kind())
        {
            return x.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword ? -1 : 1;
        }

        EqualParameterCount(x.ParameterList, y.ParameterList, out var result);

        return result;
    }

    private static int Compare(OperatorDeclarationSyntax x, OperatorDeclarationSyntax y)
    {
        if (EqualOperatorPrecedence(x.OperatorToken, y.OperatorToken, out var result))
        {
            EqualParameterCount(x.ParameterList, y.ParameterList, out result);
        }

        return result;
    }

    private int Compare(EventDeclarationSyntax x, EventDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            if (_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private static int Compare(IndexerDeclarationSyntax x, IndexerDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            EqualParameterCount(x.ParameterList, y.ParameterList, out result);
        }

        return result;
    }

    private int Compare(PropertyDeclarationSyntax x, PropertyDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            if (_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private int Compare(EnumDeclarationSyntax x, EnumDeclarationSyntax y)
    {
        if (EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out var result))
        {
            if (_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private int Compare(BaseTypeDeclarationSyntax x, BaseTypeDeclarationSyntax y)
    {
        if (EqualStaticness(x.Modifiers, y.Modifiers, out var result) &&
            EqualAccessibility(x, x.Modifiers, y, y.Modifiers, out result))
        {
            if (_includeName)
            {
                EqualIdentifierName(x.Identifier, y.Identifier, out result);
            }
        }

        return result;
    }

    private static bool ContainsToken(SyntaxTokenList list, SyntaxKind kind)
        => list.Contains(token => token.Kind() == kind);

    private enum Accessibility
    {
        Public,
        Protected,
        ProtectedInternal,
        Internal,
        PrivateProtected,
        Private
    }

    private static int GetAccessibilityPrecedence(SyntaxTokenList modifiers, SyntaxNode? parent)
    {
        if (ContainsToken(modifiers, SyntaxKind.PublicKeyword))
        {
            return (int)Accessibility.Public;
        }
        else if (ContainsToken(modifiers, SyntaxKind.ProtectedKeyword))
        {
            if (ContainsToken(modifiers, SyntaxKind.InternalKeyword))
            {
                return (int)Accessibility.ProtectedInternal;
            }

            if (ContainsToken(modifiers, SyntaxKind.PrivateKeyword))
            {
                return (int)Accessibility.PrivateProtected;
            }

            return (int)Accessibility.Protected;
        }
        else if (ContainsToken(modifiers, SyntaxKind.InternalKeyword))
        {
            return (int)Accessibility.Internal;
        }
        else if (ContainsToken(modifiers, SyntaxKind.PrivateKeyword))
        {
            return (int)Accessibility.Private;
        }

        // Determine default accessibility: This declaration is internal if we traverse up
        // the syntax tree and don't find a containing named type.
        for (var node = parent; node != null; node = node.Parent)
        {
            if (node.Kind() == SyntaxKind.InterfaceDeclaration)
            {
                // All interface members are public
                return (int)Accessibility.Public;
            }
            else if (node.Kind() is SyntaxKind.StructDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration)
            {
                // Members and nested types default to private
                return (int)Accessibility.Private;
            }
        }

        return (int)Accessibility.Internal;
    }

    private static bool BothHaveModifier(SyntaxTokenList x, SyntaxTokenList y, SyntaxKind modifierKind, out int comparisonResult)
    {
        var xHasModifier = ContainsToken(x, modifierKind);
        var yHasModifier = ContainsToken(y, modifierKind);

        if (xHasModifier == yHasModifier)
        {
            comparisonResult = 0;
            return true;
        }

        comparisonResult = xHasModifier ? -1 : 1;
        return false;
    }

    private static bool EqualStaticness(SyntaxTokenList x, SyntaxTokenList y, out int comparisonResult)
        => BothHaveModifier(x, y, SyntaxKind.StaticKeyword, out comparisonResult);

    private static bool EqualConstness(SyntaxTokenList x, SyntaxTokenList y, out int comparisonResult)
        => BothHaveModifier(x, y, SyntaxKind.ConstKeyword, out comparisonResult);

    private static bool EqualReadOnlyness(SyntaxTokenList x, SyntaxTokenList y, out int comparisonResult)
        => BothHaveModifier(x, y, SyntaxKind.ReadOnlyKeyword, out comparisonResult);

    private static bool EqualAccessibility(SyntaxNode x, SyntaxTokenList xModifiers, SyntaxNode y, SyntaxTokenList yModifiers, out int comparisonResult)
    {
        var xAccessibility = GetAccessibilityPrecedence(xModifiers, x.Parent ?? y.Parent);
        var yAccessibility = GetAccessibilityPrecedence(yModifiers, y.Parent ?? x.Parent);

        comparisonResult = xAccessibility - yAccessibility;
        return comparisonResult == 0;
    }

    private static bool EqualIdentifierName(SyntaxToken x, SyntaxToken y, out int comparisonResult)
    {
        comparisonResult = string.Compare(x.ValueText, y.ValueText, StringComparison.OrdinalIgnoreCase);
        return comparisonResult == 0;
    }

    private static bool EqualOperatorPrecedence(SyntaxToken x, SyntaxToken y, out int comparisonResult)
    {
        s_operatorPrecedenceMap.TryGetValue(x.Kind(), out var xPrecedence);
        s_operatorPrecedenceMap.TryGetValue(y.Kind(), out var yPrecedence);

        comparisonResult = xPrecedence - yPrecedence;
        return comparisonResult == 0;
    }

    private static bool EqualParameterCount(BaseParameterListSyntax x, BaseParameterListSyntax y, out int comparisonResult)
    {
        var xParameterCount = x.Parameters.Count;
        var yParameterCount = y.Parameters.Count;

        comparisonResult = xParameterCount - yParameterCount;

        return comparisonResult == 0;
    }
}
