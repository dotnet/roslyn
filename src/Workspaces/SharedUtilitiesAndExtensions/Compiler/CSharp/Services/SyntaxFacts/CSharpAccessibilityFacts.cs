// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Diagnostics.CodeAnalysis;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal sealed class CSharpAccessibilityFacts : IAccessibilityFacts
{
    public static readonly IAccessibilityFacts Instance = new CSharpAccessibilityFacts();

    private CSharpAccessibilityFacts()
    {
    }

    public bool CanHaveAccessibility(SyntaxNode declaration, bool ignoreDeclarationModifiers = false)
    {
        switch (declaration.Kind())
        {
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.DelegateDeclaration:
                return ignoreDeclarationModifiers || !((MemberDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.FileKeyword);

            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.EventFieldDeclaration:
            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.InitAccessorDeclaration:
            case SyntaxKind.AddAccessorDeclaration:
            case SyntaxKind.RemoveAccessorDeclaration:
                return true;

            case SyntaxKind.VariableDeclaration:
                return declaration.Parent is BaseFieldDeclarationSyntax;

            case SyntaxKind.VariableDeclarator:
                return declaration.Parent is VariableDeclarationSyntax { Parent: BaseFieldDeclarationSyntax };

            case SyntaxKind.ConstructorDeclaration:
                // Static constructor can't have accessibility
                return ignoreDeclarationModifiers || !((ConstructorDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.StaticKeyword);

            case SyntaxKind.PropertyDeclaration:
                return ((PropertyDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

            case SyntaxKind.IndexerDeclaration:
                return ((IndexerDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

            case SyntaxKind.OperatorDeclaration:
                return ((OperatorDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

            case SyntaxKind.ConversionOperatorDeclaration:
                return ((ConversionOperatorDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

            case SyntaxKind.MethodDeclaration:
                var method = (MethodDeclarationSyntax)declaration;
                if (method.ExplicitInterfaceSpecifier != null)
                {
                    // explicit interface methods can't have accessibility.
                    return false;
                }

                if (method.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    // partial methods can't have accessibility modifiers.
                    return false;
                }

                return true;

            case SyntaxKind.EventDeclaration:
                return ((EventDeclarationSyntax)declaration).ExplicitInterfaceSpecifier == null;

            default:
                return false;
        }
    }

    public Accessibility GetAccessibility(SyntaxNode declaration)
    {
        if (!CanHaveAccessibility(declaration))
            return Accessibility.NotApplicable;

        var modifierTokens = GetModifierTokens(declaration);
        GetAccessibilityAndModifiers(modifierTokens, out var accessibility, out _, out _);
        return accessibility;
    }

    public static void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers, out bool isDefault)
    {
        accessibility = Accessibility.NotApplicable;
        modifiers = DeclarationModifiers.None;
        isDefault = false;

        foreach (var token in modifierList)
        {
            accessibility = (token.Kind(), accessibility) switch
            {
                (SyntaxKind.PublicKeyword, _) => Accessibility.Public,

                (SyntaxKind.PrivateKeyword, Accessibility.Protected) => Accessibility.ProtectedAndInternal,
                (SyntaxKind.PrivateKeyword, _) => Accessibility.Private,

                (SyntaxKind.InternalKeyword, Accessibility.Protected) => Accessibility.ProtectedOrInternal,
                (SyntaxKind.InternalKeyword, _) => Accessibility.Internal,

                (SyntaxKind.ProtectedKeyword, Accessibility.Private) => Accessibility.ProtectedAndInternal,
                (SyntaxKind.ProtectedKeyword, Accessibility.Internal) => Accessibility.ProtectedOrInternal,
                (SyntaxKind.ProtectedKeyword, _) => Accessibility.Protected,

                _ => accessibility,
            };

            modifiers |= token.Kind() switch
            {
                SyntaxKind.AbstractKeyword => DeclarationModifiers.Abstract,
                SyntaxKind.NewKeyword => DeclarationModifiers.New,
                SyntaxKind.OverrideKeyword => DeclarationModifiers.Override,
                SyntaxKind.VirtualKeyword => DeclarationModifiers.Virtual,
                SyntaxKind.StaticKeyword => DeclarationModifiers.Static,
                SyntaxKind.AsyncKeyword => DeclarationModifiers.Async,
                SyntaxKind.ConstKeyword => DeclarationModifiers.Const,
                SyntaxKind.ReadOnlyKeyword => DeclarationModifiers.ReadOnly,
                SyntaxKind.SealedKeyword => DeclarationModifiers.Sealed,
                SyntaxKind.UnsafeKeyword => DeclarationModifiers.Unsafe,
                SyntaxKind.PartialKeyword => DeclarationModifiers.Partial,
                SyntaxKind.RefKeyword => DeclarationModifiers.Ref,
                SyntaxKind.VolatileKeyword => DeclarationModifiers.Volatile,
                SyntaxKind.ExternKeyword => DeclarationModifiers.Extern,
                SyntaxKind.FileKeyword => DeclarationModifiers.File,
                SyntaxKind.RequiredKeyword => DeclarationModifiers.Required,
                SyntaxKind.FixedKeyword => DeclarationModifiers.Fixed,
                _ => DeclarationModifiers.None,
            };

            isDefault |= token.Kind() == SyntaxKind.DefaultKeyword;
        }
    }

    public static SyntaxTokenList GetModifierTokens(SyntaxNode declaration)
        => declaration switch
        {
            MemberDeclarationSyntax memberDecl => memberDecl.Modifiers,
            ParameterSyntax parameter => parameter.Modifiers,
            LocalDeclarationStatementSyntax localDecl => localDecl.Modifiers,
            LocalFunctionStatementSyntax localFunc => localFunc.Modifiers,
            AccessorDeclarationSyntax accessor => accessor.Modifiers,
            VariableDeclarationSyntax varDecl => GetModifierTokens(varDecl.GetRequiredParent()),
            VariableDeclaratorSyntax varDecl => GetModifierTokens(varDecl.GetRequiredParent()),
            AnonymousFunctionExpressionSyntax anonymous => anonymous.Modifiers,
            _ => default,
        };
}
