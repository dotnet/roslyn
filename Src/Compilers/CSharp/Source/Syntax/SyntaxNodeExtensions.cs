// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SyntaxNodeExtensions
    {
        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : CSharpSyntaxNode
        {
            return (TNode)node.Green.SetAnnotations(annotations).CreateRed();
        }

        public static bool IsAnonymousFunction(this CSharpSyntaxNode syntax)
        {
            Debug.Assert(syntax != null);
            switch (syntax.Kind)
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsQuery(this CSharpSyntaxNode syntax)
        {
            Debug.Assert(syntax != null);
            switch (syntax.Kind)
            {
                case SyntaxKind.FromClause:
                case SyntaxKind.GroupClause:
                case SyntaxKind.JoinClause:
                case SyntaxKind.JoinIntoClause:
                case SyntaxKind.LetClause:
                case SyntaxKind.OrderByClause:
                case SyntaxKind.QueryContinuation:
                case SyntaxKind.QueryExpression:
                case SyntaxKind.SelectClause:
                case SyntaxKind.WhereClause:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This method is used to keep the code that generates binders in sync
        /// with the code that searches for binders.  We don't want the searcher
        /// to skip over any nodes that could have associated binders, especially
        /// if changes are made later.
        /// 
        /// "Local binder" is a vague term that refers to binders that represent
        /// scopes for names (e.g. BlockBinders) rather than binders that tweak
        /// default behaviors (e.g. FieldInitializerBinders).  Local binders are
        /// created by LocalBinderFactory.
        /// </summary>
        internal static bool CanHaveAssociatedLocalBinder(this CSharpSyntaxNode syntax)
        {
            return syntax.IsAnonymousFunction() ||
                syntax.Kind == SyntaxKind.CatchClause ||
                syntax.Kind == SyntaxKind.CatchFilterClause ||
                syntax is StatementSyntax;
        }

        /// <summary>
        /// Given an initializer expression infer the name of anonymous property.
        /// Returns None if unsuccessfull
        /// </summary>
        internal static SyntaxToken ExtractAnonymousTypeMemberName(this ExpressionSyntax input)
        {
            switch (input.Kind)
            {
                case SyntaxKind.IdentifierName:
                    return ((IdentifierNameSyntax)input).Identifier;

                case SyntaxKind.SimpleMemberAccessExpression:
                    return ExtractAnonymousTypeMemberName(((MemberAccessExpressionSyntax)input).Name);

                default:
                    return default(SyntaxToken);
            }
        }

        public static SyntaxReference GetReferenceOrNull(this CSharpSyntaxNode nodeOpt)
        {
            return (nodeOpt != null) ? nodeOpt.GetReference() : null;
        }
    }
}
