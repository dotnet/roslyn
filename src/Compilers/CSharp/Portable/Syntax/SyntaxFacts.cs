// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFacts
    {
        /// <summary>
        /// Returns true if the node is the alias of an AliasQualifiedNameSyntax
        /// </summary>
        public static bool IsAliasQualifier(SyntaxNode node)
        {
            var p = node.Parent as AliasQualifiedNameSyntax;
            return p is { Alias: node };
        }

        public static bool IsAttributeName(SyntaxNode node)
        {
            var parent = node.Parent;
            if (parent == null || !IsName(node.Kind()))
            {
                return false;
            }

            switch (parent.Kind())
            {
                case QualifiedName:
                    var qn = (QualifiedNameSyntax)parent;
                    return qn.Right == node ? IsAttributeName(parent) : false;

                case AliasQualifiedName:
                    var an = (AliasQualifiedNameSyntax)parent;
                    return an.Name == node ? IsAttributeName(parent) : false;
            }

            var p = node.Parent as AttributeSyntax;
            return p != null && p.Name == node;
        }

        /// <summary>
        /// Returns true if the node is the object of an invocation expression.
        /// </summary>
        public static bool IsInvoked(ExpressionSyntax node)
        {
            node = (ExpressionSyntax)SyntaxFactory.GetStandaloneExpression(node);
            var inv = node.Parent as InvocationExpressionSyntax;
            return inv != null && inv.Expression == node;
        }

        /// <summary>
        /// Returns true if the node is the object of an element access expression.
        /// </summary>
        public static bool IsIndexed(ExpressionSyntax node)
        {
            node = (ExpressionSyntax)SyntaxFactory.GetStandaloneExpression(node);
            var indexer = node.Parent as ElementAccessExpressionSyntax;
            return indexer != null && indexer.Expression == node;
        }

        public static bool IsNamespaceAliasQualifier(ExpressionSyntax node)
        {
            var parent = node.Parent as AliasQualifiedNameSyntax;
            return parent != null && parent.Alias == node;
        }

        /// <summary>
        /// Returns true if the node is in a tree location that is expected to be a type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsInTypeOnlyContext(ExpressionSyntax node)
        {
            node = SyntaxFactory.GetStandaloneExpression(node);
            var parent = node.Parent;
            if (parent != null)
            {
                switch (parent.Kind())
                {
                    case Attribute:
                        return ((AttributeSyntax)parent).Name == node;

                    case ArrayType:
                        return ((ArrayTypeSyntax)parent).ElementType == node;

                    case PointerType:
                        return ((PointerTypeSyntax)parent).ElementType == node;

                    case PredefinedType:
                        return true;

                    case NullableType:
                        return ((NullableTypeSyntax)parent).ElementType == node;

                    case TypeArgumentList:
                        // all children of GenericNames are type arguments
                        return true;

                    case CastExpression:
                        return ((CastExpressionSyntax)parent).Type == node;

                    case ObjectCreationExpression:
                        return ((ObjectCreationExpressionSyntax)parent).Type == node;

                    case StackAllocArrayCreationExpression:
                        return ((StackAllocArrayCreationExpressionSyntax)parent).Type == node;

                    case FromClause:
                        return ((FromClauseSyntax)parent).Type == node;

                    case JoinClause:
                        return ((JoinClauseSyntax)parent).Type == node;

                    case VariableDeclaration:
                        return ((VariableDeclarationSyntax)parent).Type == node;

                    case ForEachStatement:
                        return ((ForEachStatementSyntax)parent).Type == node;

                    case CatchDeclaration:
                        return ((CatchDeclarationSyntax)parent).Type == node;

                    case AsExpression:
                    case IsExpression:
                        return ((BinaryExpressionSyntax)parent).Right == node;

                    case TypeOfExpression:
                        return ((TypeOfExpressionSyntax)parent).Type == node;

                    case SizeOfExpression:
                        return ((SizeOfExpressionSyntax)parent).Type == node;

                    case DefaultExpression:
                        return ((DefaultExpressionSyntax)parent).Type == node;

                    case RefValueExpression:
                        return ((RefValueExpressionSyntax)parent).Type == node;

                    case RefType:
                        return ((RefTypeSyntax)parent).Type == node;

                    case Parameter:
                        return ((ParameterSyntax)parent).Type == node;

                    case TypeConstraint:
                        return ((TypeConstraintSyntax)parent).Type == node;

                    case MethodDeclaration:
                        return ((MethodDeclarationSyntax)parent).ReturnType == node;

                    case IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)parent).Type == node;

                    case OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)parent).ReturnType == node;

                    case ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)parent).Type == node;

                    case PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)parent).Type == node;

                    case DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)parent).ReturnType == node;

                    case EventDeclaration:
                        return ((EventDeclarationSyntax)parent).Type == node;

                    case LocalFunctionStatement:
                        return ((LocalFunctionStatementSyntax)parent).ReturnType == node;

                    case SimpleBaseType:
                        return true;

                    case CrefParameter:
                        return true;

                    case ConversionOperatorMemberCref:
                        return ((ConversionOperatorMemberCrefSyntax)parent).Type == node;

                    case ExplicitInterfaceSpecifier:
                        // #13.4.1 An explicit member implementation is a method, property, event or indexer
                        // declaration that references a fully qualified interface member name.
                        // A ExplicitInterfaceSpecifier represents the left part (QN) of the member name, so it
                        // should be treated like a QualifiedName.
                        return ((ExplicitInterfaceSpecifierSyntax)parent).Name == node;

                    case DeclarationPattern:
                        return ((DeclarationPatternSyntax)parent).Type == node;

                    case TupleElement:
                        return ((TupleElementSyntax)parent).Type == node;

                    case DeclarationExpression:
                        return ((DeclarationExpressionSyntax)parent).Type == node;

                    case IncompleteMember:
                        return ((IncompleteMemberSyntax)parent).Type == node;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if a node is in a tree location that is expected to be either a namespace or type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsInNamespaceOrTypeContext(ExpressionSyntax node)
        {
            if (node != null)
            {
                node = (ExpressionSyntax)SyntaxFactory.GetStandaloneExpression(node);
                var parent = node.Parent;
                if (parent != null)
                {
                    switch (parent.Kind())
                    {
                        case UsingDirective:
                            return ((UsingDirectiveSyntax)parent).Name == node;

                        case QualifiedName:
                            // left of QN is namespace or type.  Note: when you have "a.b.c()", then
                            // "a.b" is not a qualified name, it is a member access expression.
                            // Qualified names are only parsed when the parser knows it's a type only
                            // context.
                            return ((QualifiedNameSyntax)parent).Left == node;

                        default:
                            return IsInTypeOnlyContext(node);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Is the node the name of a named argument of an invocation, object creation expression, 
        /// constructor initializer, or element access, but not an attribute.
        /// </summary>
        public static bool IsNamedArgumentName(SyntaxNode node)
        {
            // An argument name is an IdentifierName inside a NameColon, inside an Argument, inside an ArgumentList, inside an
            // Invocation, ObjectCreation, ObjectInitializer, ElementAccess or Subpattern.

            if (!node.IsKind(IdentifierName))
            {
                return false;
            }

            var parent1 = node.Parent;
            if (parent1 == null || !parent1.IsKind(NameColon))
            {
                return false;
            }

            var parent2 = parent1.Parent;
            if (parent2.IsKind(SyntaxKind.Subpattern))
            {
                return true;
            }

            if (parent2 == null || !(parent2.IsKind(Argument) || parent2.IsKind(AttributeArgument)))
            {
                return false;
            }

            var parent3 = parent2.Parent;
            if (parent3 == null)
            {
                return false;
            }

            if (parent3.IsKind(SyntaxKind.TupleExpression))
            {
                return true;
            }

            if (!(parent3 is BaseArgumentListSyntax || parent3.IsKind(AttributeArgumentList)))
            {
                return false;
            }

            var parent4 = parent3.Parent;
            if (parent4 == null)
            {
                return false;
            }

            switch (parent4.Kind())
            {
                case InvocationExpression:
                case TupleExpression:
                case ObjectCreationExpression:
                case ObjectInitializerExpression:
                case ElementAccessExpression:
                case Attribute:
                case BaseConstructorInitializer:
                case ThisConstructorInitializer:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is the expression the initializer in a fixed statement?
        /// </summary>
        public static bool IsFixedStatementExpression(SyntaxNode node)
        {
            node = node.Parent;
            // Dig through parens because dev10 does (even though the spec doesn't say so)
            // Dig through casts because there's a special error code (CS0254) for such casts.
            while (node != null && (node.IsKind(ParenthesizedExpression) || node.IsKind(CastExpression))) node = node.Parent;
            if (node == null || !node.IsKind(EqualsValueClause)) return false;
            node = node.Parent;
            if (node == null || !node.IsKind(VariableDeclarator)) return false;
            node = node.Parent;
            if (node == null || !node.IsKind(VariableDeclaration)) return false;
            node = node.Parent;
            return node != null && node.IsKind(FixedStatement);
        }

        public static string GetText(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable:
                    return string.Empty;
                case Accessibility.Private:
                    return SyntaxFacts.GetText(PrivateKeyword);
                case Accessibility.ProtectedAndInternal:
                    return SyntaxFacts.GetText(PrivateKeyword) + " " + SyntaxFacts.GetText(ProtectedKeyword);
                case Accessibility.Internal:
                    return SyntaxFacts.GetText(InternalKeyword);
                case Accessibility.Protected:
                    return SyntaxFacts.GetText(ProtectedKeyword);
                case Accessibility.ProtectedOrInternal:
                    return SyntaxFacts.GetText(ProtectedKeyword) + " " + SyntaxFacts.GetText(InternalKeyword);
                case Accessibility.Public:
                    return SyntaxFacts.GetText(PublicKeyword);
                default:
                    throw ExceptionUtilities.UnexpectedValue(accessibility);
            }
        }

        internal static bool IsStatementExpression(SyntaxNode syntax)
        {
            // The grammar gives:
            //
            // expression-statement:
            //     statement-expression ;
            //
            // statement-expression:
            //     invocation-expression
            //     object-creation-expression
            //     assignment
            //     post-increment-expression
            //     post-decrement-expression
            //     pre-increment-expression
            //     pre-decrement-expression
            //     await-expression

            switch (syntax.Kind())
            {
                case InvocationExpression:
                case ObjectCreationExpression:
                case SimpleAssignmentExpression:
                case AddAssignmentExpression:
                case SubtractAssignmentExpression:
                case MultiplyAssignmentExpression:
                case DivideAssignmentExpression:
                case ModuloAssignmentExpression:
                case AndAssignmentExpression:
                case OrAssignmentExpression:
                case ExclusiveOrAssignmentExpression:
                case LeftShiftAssignmentExpression:
                case RightShiftAssignmentExpression:
                case CoalesceAssignmentExpression:
                case PostIncrementExpression:
                case PostDecrementExpression:
                case PreIncrementExpression:
                case PreDecrementExpression:
                case AwaitExpression:
                    return true;

                case ConditionalAccessExpression:
                    var access = (ConditionalAccessExpressionSyntax)syntax;
                    return IsStatementExpression(access.WhenNotNull);

                // Allow missing IdentifierNames; they will show up in error cases
                // where there is no statement whatsoever.

                case IdentifierName:
                    return syntax.IsMissing;

                default:
                    return false;
            }
        }

        [System.Obsolete("IsLambdaBody API is obsolete", true)]
        public static bool IsLambdaBody(SyntaxNode node)
        {
            return LambdaUtilities.IsLambdaBody(node);
        }

        internal static bool IsIdentifierVar(this Syntax.InternalSyntax.SyntaxToken node)
        {
            return node.ContextualKind == SyntaxKind.VarKeyword;
        }

        internal static bool IsIdentifierVarOrPredefinedType(this Syntax.InternalSyntax.SyntaxToken node)
        {
            return node.IsIdentifierVar() || IsPredefinedType(node.Kind);
        }

        internal static bool IsDeclarationExpressionType(SyntaxNode node, out DeclarationExpressionSyntax parent)
        {
            parent = node.Parent as DeclarationExpressionSyntax;
            return node == parent?.Type;
        }

        /// <summary>
        /// Given an initializer expression infer the name of anonymous property or tuple element.
        /// Returns null if unsuccessful
        /// </summary>
        public static string TryGetInferredMemberName(this SyntaxNode syntax)
        {
            SyntaxToken nameToken;
            switch (syntax.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    nameToken = ((SingleVariableDesignationSyntax)syntax).Identifier;
                    break;

                case SyntaxKind.DeclarationExpression:
                    var declaration = (DeclarationExpressionSyntax)syntax;
                    var designationKind = declaration.Designation.Kind();
                    if (designationKind == SyntaxKind.ParenthesizedVariableDesignation ||
                        designationKind == SyntaxKind.DiscardDesignation)
                    {
                        return null;
                    }

                    nameToken = ((SingleVariableDesignationSyntax)declaration.Designation).Identifier;
                    break;

                case SyntaxKind.ParenthesizedVariableDesignation:
                case SyntaxKind.DiscardDesignation:
                    return null;

                default:
                    if (syntax is ExpressionSyntax expr)
                    {
                        nameToken = expr.ExtractAnonymousTypeMemberName();
                        break;
                    }
                    return null;
            }

            return nameToken.ValueText;
        }

        /// <summary>
        /// Checks whether the element name is reserved.
        ///
        /// For example:
        /// "Item3" is reserved (at certain positions).
        /// "Rest", "ToString" and other members of System.ValueTuple are reserved (in any position).
        /// Names that are not reserved return false.
        /// </summary>
        public static bool IsReservedTupleElementName(string elementName)
        {
            return TupleTypeSymbol.IsElementNameReserved(elementName) != -1;
        }

        internal static bool HasAnyBody(this BaseMethodDeclarationSyntax declaration)
        {
            return (declaration.Body ?? (SyntaxNode)declaration.ExpressionBody) != null;
        }
    }
}
