// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Diagnostics.CodeAnalysis;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpAccessibilityFacts : IAccessibilityFacts
    {
        public static readonly IAccessibilityFacts Instance = new CSharpAccessibilityFacts();

        private CSharpAccessibilityFacts()
        {
        }

        public bool CanHaveAccessibility(SyntaxNode declaration)
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
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    var declarationKind = GetDeclarationKind(declaration);
                    return declarationKind is DeclarationKind.Field or DeclarationKind.Event;

                case SyntaxKind.ConstructorDeclaration:
                    // Static constructor can't have accessibility
                    return !((ConstructorDeclarationSyntax)declaration).Modifiers.Any(SyntaxKind.StaticKeyword);

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
                    _ => DeclarationModifiers.None,
                };

                isDefault |= token.Kind() == SyntaxKind.DefaultKeyword;
            }
        }

        public static DeclarationKind GetDeclarationKind(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.RecordDeclaration:
                    return DeclarationKind.Class;
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    return DeclarationKind.Struct;
                case SyntaxKind.InterfaceDeclaration:
                    return DeclarationKind.Interface;
                case SyntaxKind.EnumDeclaration:
                    return DeclarationKind.Enum;
                case SyntaxKind.DelegateDeclaration:
                    return DeclarationKind.Delegate;

                case SyntaxKind.MethodDeclaration:
                    return DeclarationKind.Method;
                case SyntaxKind.OperatorDeclaration:
                    return DeclarationKind.Operator;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return DeclarationKind.ConversionOperator;
                case SyntaxKind.ConstructorDeclaration:
                    return DeclarationKind.Constructor;
                case SyntaxKind.DestructorDeclaration:
                    return DeclarationKind.Destructor;

                case SyntaxKind.PropertyDeclaration:
                    return DeclarationKind.Property;
                case SyntaxKind.IndexerDeclaration:
                    return DeclarationKind.Indexer;
                case SyntaxKind.EventDeclaration:
                    return DeclarationKind.CustomEvent;
                case SyntaxKind.EnumMemberDeclaration:
                    return DeclarationKind.EnumMember;
                case SyntaxKind.CompilationUnit:
                    return DeclarationKind.CompilationUnit;
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    return DeclarationKind.Namespace;
                case SyntaxKind.UsingDirective:
                    return DeclarationKind.NamespaceImport;
                case SyntaxKind.Parameter:
                    return DeclarationKind.Parameter;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return DeclarationKind.LambdaExpression;

                case SyntaxKind.FieldDeclaration:
                    var fd = (FieldDeclarationSyntax)declaration;
                    if (fd.Declaration != null && fd.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Field;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.EventFieldDeclaration:
                    var ef = (EventFieldDeclarationSyntax)declaration;
                    if (ef.Declaration != null && ef.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Event;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.LocalDeclarationStatement:
                    var ld = (LocalDeclarationStatementSyntax)declaration;
                    if (ld.Declaration != null && ld.Declaration.Variables.Count == 1)
                    {
                        // this node is considered the declaration if it contains only one variable.
                        return DeclarationKind.Variable;
                    }
                    else
                    {
                        return DeclarationKind.None;
                    }

                case SyntaxKind.VariableDeclaration:
                    {
                        var vd = (VariableDeclarationSyntax)declaration;
                        if (vd.Variables.Count == 1 && vd.Parent == null)
                        {
                            // this node is the declaration if it contains only one variable and has no parent.
                            return DeclarationKind.Variable;
                        }
                        else
                        {
                            return DeclarationKind.None;
                        }
                    }

                case SyntaxKind.VariableDeclarator:
                    {
                        var vd = declaration.Parent as VariableDeclarationSyntax;

                        // this node is considered the declaration if it is one among many, or it has no parent
                        if (vd == null || vd.Variables.Count > 1)
                        {
                            if (ParentIsFieldDeclaration(vd))
                            {
                                return DeclarationKind.Field;
                            }
                            else if (ParentIsEventFieldDeclaration(vd))
                            {
                                return DeclarationKind.Event;
                            }
                            else
                            {
                                return DeclarationKind.Variable;
                            }
                        }

                        break;
                    }

                case SyntaxKind.AttributeList:
                    var list = (AttributeListSyntax)declaration;
                    if (list.Attributes.Count == 1)
                    {
                        return DeclarationKind.Attribute;
                    }

                    break;

                case SyntaxKind.Attribute:
                    if (declaration.Parent is not AttributeListSyntax parentList || parentList.Attributes.Count > 1)
                    {
                        return DeclarationKind.Attribute;
                    }

                    break;

                case SyntaxKind.GetAccessorDeclaration:
                    return DeclarationKind.GetAccessor;
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    return DeclarationKind.SetAccessor;
                case SyntaxKind.AddAccessorDeclaration:
                    return DeclarationKind.AddAccessor;
                case SyntaxKind.RemoveAccessorDeclaration:
                    return DeclarationKind.RemoveAccessor;
            }

            return DeclarationKind.None;
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

        public static bool ParentIsFieldDeclaration([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent.IsKind(SyntaxKind.FieldDeclaration) ?? false;

        public static bool ParentIsEventFieldDeclaration([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent.IsKind(SyntaxKind.EventFieldDeclaration) ?? false;

        public static bool ParentIsLocalDeclarationStatement([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent.IsKind(SyntaxKind.LocalDeclarationStatement) ?? false;
    }
}
