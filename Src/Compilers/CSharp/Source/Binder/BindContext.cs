using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal static class BindContextExtensions
    {
        internal static bool IsAliasQualifier(this SyntaxNode node)
        {
            var p = node.Parent as AliasQualifiedNameSyntax;
            return p != null && p.Alias == node;
        }

        internal static bool IsInvoked(this SyntaxNode node)
        {
            var inv = node.Parent as InvocationExpressionSyntax;
            return inv != null && inv.Expression == node;
        }

        internal static bool IsInTypeOnlyContext(this SyntaxNode node)
        {
            SyntaxNode parent = node.Parent;
            if (parent != null)
            {
                switch (parent.Kind)
                {
                    case SyntaxKind.ArrayType:
                        return ((ArrayTypeSyntax)parent).ElementType == node;
                    case SyntaxKind.PointerType:
                        return ((PointerTypeSyntax)parent).ElementType == node;
                    case SyntaxKind.PredefinedType:
                        return true;
                    case SyntaxKind.NullableType:
                        return ((NullableTypeSyntax)parent).ElementType == node;
                    case SyntaxKind.GenericName:
                        return true; // all children of GenericNames are type arguments
                    case SyntaxKind.CastExpression:
                        return ((CastExpressionSyntax)parent).Type == node;
                    case SyntaxKind.ObjectCreationExpression:
                        return ((ObjectCreationExpressionSyntax)parent).Type == node;
                    case SyntaxKind.StackAllocArrayCreationExpression:
                        return ((StackAllocArrayCreationExpressionSyntax)parent).Type == node;
                    case SyntaxKind.FromClause:
                        return ((FromClauseSyntax)parent).TypeOpt == node;
                    case SyntaxKind.JoinClause:
                        return ((JoinClauseSyntax)parent).TypeOpt == node;
                    case SyntaxKind.VariableDeclaration:
                        return ((VariableDeclarationSyntax)parent).Type == node;
                    case SyntaxKind.ForEachStatement:
                        return ((ForEachStatementSyntax)parent).Type == node;
                    case SyntaxKind.CatchDeclaration:
                        return ((CatchDeclarationSyntax)parent).Type == node;
                    case SyntaxKind.AsExpression:
                    case SyntaxKind.IsExpression:
                        return ((BinaryExpressionSyntax)parent).Right == node;
                    case SyntaxKind.Argument:
                        var argList = parent.Parent as ArgumentListSyntax;
                        if (argList != null && argList.Parent != null)
                        {
                            switch (argList.Parent.Kind)
                            {
                                case SyntaxKind.TypeOfExpression:
                                case SyntaxKind.SizeOfExpression:
                                case SyntaxKind.DefaultExpression:
                                    return ((ArgumentSyntax)parent).Expression == node;
                            }
                        }
                        break;
                    case SyntaxKind.Parameter:
                        return ((ParameterSyntax)parent).TypeOpt == node;
                    case SyntaxKind.TypeConstraint:
                        return ((TypeConstraintSyntax)parent).Type == node;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)parent).ReturnType == node;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)parent).Type == node;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)parent).ReturnType == node;
                    case SyntaxKind.BaseList:
                        return true;  // children of BaseListSyntax are only types
                }
            }
            return false;
        }

        // true if expression can be either a namespace or a type
        internal static bool IsInNamespaceOrTypeContext(this SyntaxNode node)
        {
            var parent = node.Parent;
            if (parent != null)
            {
                switch (parent.Kind)
                {
                    case SyntaxKind.UsingDirective:
                        return ((UsingDirectiveSyntax)parent).Name == node;
                    case SyntaxKind.QualifiedName:
                        // left of QN is namespace or type
                        return ((QualifiedNameSyntax)parent).Left == node;
                }
            }
            return false;
        }
    }
}