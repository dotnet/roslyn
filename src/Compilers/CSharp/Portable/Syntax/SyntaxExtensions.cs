// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class SyntaxExtensions
    {
        /// <summary>
        /// Gets the expression-body syntax from an expression-bodied member. The
        /// given syntax must be for a member which could contain an expression-body.
        /// </summary>
        internal static ArrowExpressionClauseSyntax? GetExpressionBodySyntax(this CSharpSyntaxNode node)
        {
            ArrowExpressionClauseSyntax? arrowExpr = null;
            switch (node.Kind())
            {
                // The ArrowExpressionClause is the declaring syntax for the
                // 'get' SourcePropertyAccessorSymbol of properties and indexers.
                case SyntaxKind.ArrowExpressionClause:
                    arrowExpr = (ArrowExpressionClauseSyntax)node;
                    break;
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    arrowExpr = ((BaseMethodDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                    arrowExpr = ((AccessorDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.PropertyDeclaration:
                    arrowExpr = ((PropertyDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.IndexerDeclaration:
                    arrowExpr = ((IndexerDeclarationSyntax)node).ExpressionBody;
                    break;
                default:
                    // Don't throw, just use for the assert in case this is used in the semantic model
                    ExceptionUtilities.UnexpectedValue(node.Kind());
                    break;
            }
            return arrowExpr;
        }

        /// <summary>
        /// Creates a new syntax token with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <param name="token">The token to normalize.</param>
        /// <param name="indentation">A sequence of whitespace characters that defines a single level of indentation.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static SyntaxToken NormalizeWhitespace(this SyntaxToken token, string indentation, bool elasticTrivia)
        {
            return SyntaxNormalizer.Normalize(token, indentation, CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, elasticTrivia);
        }

        /// <summary>
        /// Return the identifier of an out declaration argument expression.
        /// </summary>
        internal static SyntaxToken Identifier(this DeclarationExpressionSyntax self)
        {
            return ((SingleVariableDesignationSyntax)self.Designation).Identifier;
        }

        /// <summary>
        /// Creates a new syntax token with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <param name="token">The token to normalize.</param>
        /// <param name="indentation">An optional sequence of whitespace characters that defines a
        /// single level of indentation.</param>
        /// <param name="eol">An optional sequence of whitespace characters used for end of line.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static SyntaxToken NormalizeWhitespace(this SyntaxToken token,
            string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
            string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
            bool elasticTrivia = false)
        {
            return SyntaxNormalizer.Normalize(token, indentation, eol, elasticTrivia);
        }

        /// <summary>
        /// Creates a new syntax trivia list with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <param name="list">The trivia list to normalize.</param>
        /// <param name="indentation">A sequence of whitespace characters that defines a single level of indentation.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static SyntaxTriviaList NormalizeWhitespace(this SyntaxTriviaList list, string indentation, bool elasticTrivia)
        {
            return SyntaxNormalizer.Normalize(list, indentation, CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, elasticTrivia);
        }

        /// <summary>
        /// Creates a new syntax trivia list with all whitespace and end of line trivia replaced with
        /// regularly formatted trivia.
        /// </summary>
        /// <param name="list">The trivia list to normalize.</param>
        /// <param name="indentation">An optional sequence of whitespace characters that defines a
        /// single level of indentation.</param>
        /// <param name="eol">An optional sequence of whitespace characters used for end of line.</param>
        /// <param name="elasticTrivia">If true the replaced trivia is elastic trivia.</param>
        public static SyntaxTriviaList NormalizeWhitespace(this SyntaxTriviaList list,
            string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
            string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
            bool elasticTrivia = false)
        {
            return SyntaxNormalizer.Normalize(list, indentation, eol, elasticTrivia);
        }

        public static SyntaxTriviaList ToSyntaxTriviaList(this IEnumerable<SyntaxTrivia> sequence)
        {
            return SyntaxFactory.TriviaList(sequence);
        }

        internal static XmlNameAttributeElementKind GetElementKind(this XmlNameAttributeSyntax attributeSyntax)
        {
            Debug.Assert(attributeSyntax.Parent is object);
            CSharpSyntaxNode parentSyntax = attributeSyntax.Parent;
            SyntaxKind parentKind = parentSyntax.Kind();

            string parentName;
            if (parentKind == SyntaxKind.XmlEmptyElement)
            {
                var parent = (XmlEmptyElementSyntax)parentSyntax;
                parentName = parent.Name.LocalName.ValueText;
                Debug.Assert(parent.Name.Prefix is null);
            }
            else if (parentKind == SyntaxKind.XmlElementStartTag)
            {
                var parent = (XmlElementStartTagSyntax)parentSyntax;
                parentName = parent.Name.LocalName.ValueText;
                Debug.Assert(parent.Name.Prefix is null);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(parentKind);
            }

            if (DocumentationCommentXmlNames.ElementEquals(parentName, DocumentationCommentXmlNames.ParameterElementName))
            {
                return XmlNameAttributeElementKind.Parameter;
            }
            else if (DocumentationCommentXmlNames.ElementEquals(parentName, DocumentationCommentXmlNames.ParameterReferenceElementName))
            {
                return XmlNameAttributeElementKind.ParameterReference;
            }
            else if (DocumentationCommentXmlNames.ElementEquals(parentName, DocumentationCommentXmlNames.TypeParameterElementName))
            {
                return XmlNameAttributeElementKind.TypeParameter;
            }
            else if (DocumentationCommentXmlNames.ElementEquals(parentName, DocumentationCommentXmlNames.TypeParameterReferenceElementName))
            {
                return XmlNameAttributeElementKind.TypeParameterReference;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(parentName);
            }
        }

        internal static bool ReportDocumentationCommentDiagnostics(this SyntaxTree tree)
        {
            return tree.Options.DocumentationMode >= DocumentationMode.Diagnose;
        }

        /// <summary>
        /// Updates the given SimpleNameSyntax node with the given identifier token.
        /// This function is a wrapper that calls WithIdentifier on derived syntax nodes.
        /// </summary>
        /// <param name="simpleName"></param>
        /// <param name="identifier"></param>
        /// <returns>The given simple name updated with the given identifier.</returns>
        public static SimpleNameSyntax WithIdentifier(this SimpleNameSyntax simpleName, SyntaxToken identifier)
        {
            return simpleName.Kind() == SyntaxKind.IdentifierName
                ? (SimpleNameSyntax)((IdentifierNameSyntax)simpleName).WithIdentifier(identifier)
                : (SimpleNameSyntax)((GenericNameSyntax)simpleName).WithIdentifier(identifier);
        }

        internal static bool IsTypeInContextWhichNeedsDynamicAttribute(this IdentifierNameSyntax typeNode)
        {
            Debug.Assert(typeNode != null);
            return SyntaxFacts.IsInTypeOnlyContext(typeNode) && IsInContextWhichNeedsDynamicAttribute(typeNode);
        }

        internal static ExpressionSyntax SkipParens(this ExpressionSyntax expression)
        {
            while (expression.Kind() == SyntaxKind.ParenthesizedExpression)
            {
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;
            }

            return expression;
        }

        /// <summary>
        /// Returns true if the expression on the left-hand-side of an assignment causes the assignment to be a deconstruction.
        /// </summary>
        internal static bool IsDeconstructionLeft(this ExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.TupleExpression:
                    return true;
                case SyntaxKind.DeclarationExpression:
                    return ((DeclarationExpressionSyntax)node).Designation.Kind() == SyntaxKind.ParenthesizedVariableDesignation;
                default:
                    return false;
            }
        }

        internal static bool IsDeconstruction(this AssignmentExpressionSyntax self)
        {
            return self.Left.IsDeconstructionLeft();
        }

        private static bool IsInContextWhichNeedsDynamicAttribute(CSharpSyntaxNode node)
        {
            Debug.Assert(node != null);

            switch (node.Kind())
            {
                case SyntaxKind.Parameter:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.BaseList:
                case SyntaxKind.SimpleBaseType:
                case SyntaxKind.PrimaryConstructorBaseType:
                    return true;

                case SyntaxKind.Block:
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.TypeParameterConstraintClause:
                case SyntaxKind.Attribute:
                case SyntaxKind.EqualsValueClause:
                    return false;

                default:
                    return node.Parent != null && IsInContextWhichNeedsDynamicAttribute(node.Parent);
            }
        }

        public static IndexerDeclarationSyntax Update(
            this IndexerDeclarationSyntax syntax,
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken thisKeyword,
            BracketedParameterListSyntax parameterList,
            AccessorListSyntax accessorList)
        {
            return syntax.Update(
                attributeLists,
                modifiers,
                type,
                explicitInterfaceSpecifier,
                thisKeyword,
                parameterList,
                accessorList,
                expressionBody: null,
                semicolonToken: default);
        }

        public static OperatorDeclarationSyntax Update(
            this OperatorDeclarationSyntax syntax,
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            SyntaxToken operatorKeyword,
            SyntaxToken operatorToken,
            ParameterListSyntax parameterList,
            BlockSyntax block,
            SyntaxToken semicolonToken)
        {
            return syntax.Update(
                attributeLists,
                modifiers,
                returnType,
                operatorKeyword,
                operatorToken,
                parameterList,
                block,
                expressionBody: null,
                semicolonToken);
        }

        public static MethodDeclarationSyntax Update(
            this MethodDeclarationSyntax syntax,
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            BlockSyntax block,
            SyntaxToken semicolonToken)
        {
            return syntax.Update(
                attributeLists,
                modifiers,
                returnType,
                explicitInterfaceSpecifier,
                identifier,
                typeParameterList,
                parameterList,
                constraintClauses,
                block,
                expressionBody: null,
                semicolonToken);
        }

        /// <summary>
        /// If this declaration or identifier is part of a deconstruction, find the deconstruction.
        /// If found, returns either an assignment expression or a foreach variable statement.
        /// Returns null otherwise.
        /// </summary>
        internal static CSharpSyntaxNode? GetContainingDeconstruction(this ExpressionSyntax expr)
        {
            var kind = expr.Kind();
            if (kind != SyntaxKind.TupleExpression && kind != SyntaxKind.DeclarationExpression && kind != SyntaxKind.IdentifierName)
            {
                return null;
            }

            while (true)
            {
                Debug.Assert(expr.Kind() == SyntaxKind.TupleExpression || expr.Kind() == SyntaxKind.DeclarationExpression || expr.Kind() == SyntaxKind.IdentifierName);
                var parent = expr.Parent;
                if (parent == null) { return null; }

                switch (parent.Kind())
                {
                    case SyntaxKind.Argument:
                        if (parent.Parent?.Kind() == SyntaxKind.TupleExpression)
                        {
                            expr = (TupleExpressionSyntax)parent.Parent;
                            continue;
                        }
                        return null;
                    case SyntaxKind.SimpleAssignmentExpression:
                        if ((object)((AssignmentExpressionSyntax)parent).Left == expr)
                        {
                            return parent;
                        }
                        return null;
                    case SyntaxKind.ForEachVariableStatement:
                        if ((object)((ForEachVariableStatementSyntax)parent).Variable == expr)
                        {
                            return parent;
                        }
                        return null;
                    default:
                        return null;
                }
            }
        }

        internal static bool IsOutDeclaration(this DeclarationExpressionSyntax p)
        {
            return p.Parent?.Kind() == SyntaxKind.Argument
                && ((ArgumentSyntax)p.Parent).RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword;
        }

        internal static bool IsOutVarDeclaration(this DeclarationExpressionSyntax p)
        {
            return p.Designation.Kind() == SyntaxKind.SingleVariableDesignation && p.IsOutDeclaration();
        }

        /// <summary>
        /// Visits all the ArrayRankSpecifiers of a typeSyntax, invoking an action on each one in turn.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <param name="argument">The argument that is passed to the action whenever it is invoked</param>
        internal static void VisitRankSpecifiers<TArg>(this TypeSyntax type, in TArg argument, Action<ArrayRankSpecifierSyntax, TArg> action)
        {
            // Use a manual stack here to avoid deeply nested recursion which can blow the real stack
            var stack = ArrayBuilder<SyntaxNode>.GetInstance();
            stack.Push(type);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is ArrayRankSpecifierSyntax rankSpecifier)
                {
                    action(rankSpecifier, argument);
                    continue;
                }
                else
                {
                    type = (TypeSyntax)current;
                }

                switch (type.Kind())
                {
                    case SyntaxKind.ArrayType:
                        var arrayTypeSyntax = (ArrayTypeSyntax)type;
                        for (int i = arrayTypeSyntax.RankSpecifiers.Count - 1; i >= 0; i--)
                        {
                            stack.Push(arrayTypeSyntax.RankSpecifiers[i]);
                        }
                        stack.Push(arrayTypeSyntax.ElementType);
                        break;
                    case SyntaxKind.NullableType:
                        var nullableTypeSyntax = (NullableTypeSyntax)type;
                        stack.Push(nullableTypeSyntax.ElementType);
                        break;
                    case SyntaxKind.PointerType:
                        var pointerTypeSyntax = (PointerTypeSyntax)type;
                        stack.Push(pointerTypeSyntax.ElementType);
                        break;
                    case SyntaxKind.FunctionPointerType:
                        var functionPointerTypeSyntax = (FunctionPointerTypeSyntax)type;
                        for (int i = functionPointerTypeSyntax.ParameterList.Parameters.Count - 1; i >= 0; i--)
                        {
                            TypeSyntax? paramType = functionPointerTypeSyntax.ParameterList.Parameters[i].Type;
                            Debug.Assert(paramType is object);
                            stack.Push(paramType);
                        }
                        break;
                    case SyntaxKind.TupleType:
                        var tupleTypeSyntax = (TupleTypeSyntax)type;
                        for (int i = tupleTypeSyntax.Elements.Count - 1; i >= 0; i--)
                        {
                            stack.Push(tupleTypeSyntax.Elements[i].Type);
                        }
                        break;
                    case SyntaxKind.RefType:
                        var refTypeSyntax = (RefTypeSyntax)type;
                        stack.Push(refTypeSyntax.Type);
                        break;
                    case SyntaxKind.ScopedType:
                        var scopedTypeSyntax = (ScopedTypeSyntax)type;
                        stack.Push(scopedTypeSyntax.Type);
                        break;
                    case SyntaxKind.GenericName:
                        var genericNameSyntax = (GenericNameSyntax)type;
                        for (int i = genericNameSyntax.TypeArgumentList.Arguments.Count - 1; i >= 0; i--)
                        {
                            stack.Push(genericNameSyntax.TypeArgumentList.Arguments[i]);
                        }
                        break;
                    case SyntaxKind.QualifiedName:
                        var qualifiedNameSyntax = (QualifiedNameSyntax)type;
                        stack.Push(qualifiedNameSyntax.Right);
                        stack.Push(qualifiedNameSyntax.Left);
                        break;
                    case SyntaxKind.AliasQualifiedName:
                        var aliasQualifiedNameSyntax = (AliasQualifiedNameSyntax)type;
                        stack.Push(aliasQualifiedNameSyntax.Name);
                        break;
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.OmittedTypeArgument:
                    case SyntaxKind.PredefinedType:
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.Kind());
                }
            }

            stack.Free();
        }
    }
}
