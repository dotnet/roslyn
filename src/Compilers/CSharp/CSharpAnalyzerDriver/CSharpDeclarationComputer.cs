// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpDeclarationComputer : DeclarationComputer
    {
        public static void ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, bool getSymbol, List<DeclarationInfo> builder, CancellationToken cancellationToken)
        {
            ComputeDeclarations(model, model.SyntaxTree.GetRoot(cancellationToken),
                (node, level) => !node.Span.OverlapsWith(span) || InvalidLevel(level),
                getSymbol, builder, null, cancellationToken);
        }

        public static void ComputeDeclarationsInNode(SemanticModel model, SyntaxNode node, bool getSymbol, List<DeclarationInfo> builder, CancellationToken cancellationToken, int? levelsToCompute = null)
        {
            ComputeDeclarations(model, node, (n, level) => InvalidLevel(level), getSymbol, builder, levelsToCompute, cancellationToken);
        }

        private static bool InvalidLevel(int? level)
        {
            return level.HasValue && level.Value <= 0;
        }

        private static int? DecrementLevel(int? level)
        {
            return level.HasValue ? level - 1 : level;
        }

        private static void ComputeDeclarations(
            SemanticModel model,
            SyntaxNode node,
            Func<SyntaxNode, int?, bool> shouldSkip,
            bool getSymbol,
            List<DeclarationInfo> builder,
            int? levelsToCompute,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (shouldSkip(node, levelsToCompute))
            {
                return;
            }

            var newLevel = DecrementLevel(levelsToCompute);

            switch (node.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                    {
                        var ns = (NamespaceDeclarationSyntax)node;
                        foreach (var decl in ns.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken));

                        NameSyntax name = ns.Name;
                        while (name.Kind() == SyntaxKind.QualifiedName)
                        {
                            name = ((QualifiedNameSyntax)name).Left;
                            var declaredSymbol = getSymbol ? model.GetSymbolInfo(name, cancellationToken).Symbol : null;
                            builder.Add(new DeclarationInfo(name, ImmutableArray<SyntaxNode>.Empty, declaredSymbol));
                        }

                        return;
                    }

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    {
                        var t = (TypeDeclarationSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken));
                        return;
                    }

                case SyntaxKind.EnumDeclaration:
                    {
                        var t = (EnumDeclarationSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken));
                        return;
                    }

                case SyntaxKind.EnumMemberDeclaration:
                    {
                        var t = (EnumMemberDeclarationSyntax)node;
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, t.EqualsValue, cancellationToken));
                        return;
                    }

                case SyntaxKind.DelegateDeclaration:
                    {
                        var t = (DelegateDeclarationSyntax)node;
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken));
                        return;
                    }

                case SyntaxKind.EventDeclaration:
                    {
                        var t = (EventDeclarationSyntax)node;
                        foreach (var decl in t.AccessorList.Accessors) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken));
                        return;
                    }

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    {
                        var t = (BaseFieldDeclarationSyntax)node;
                        foreach (var decl in t.Declaration.Variables)
                        {
                            builder.Add(GetDeclarationInfo(model, decl, getSymbol, decl.Initializer, cancellationToken));
                        }

                        return;
                    }

                case SyntaxKind.ArrowExpressionClause:
                    {
                        // Arrow expression clause declares getter symbol for properties and indexers.
                        var parentProperty = node.Parent as BasePropertyDeclarationSyntax;
                        if (parentProperty != null)
                        {
                            builder.Add(GetExpressionBodyDeclarationInfo(parentProperty, (ArrowExpressionClauseSyntax)node, model, getSymbol, cancellationToken));
                        }

                        return;
                    }

                case SyntaxKind.PropertyDeclaration:
                    {
                        var t = (PropertyDeclarationSyntax)node;
                        if (t.AccessorList != null)
                        {
                            foreach (var decl in t.AccessorList.Accessors) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        }

                        if (t.ExpressionBody != null)
                        {
                            ComputeDeclarations(model, t.ExpressionBody, shouldSkip, getSymbol, builder, levelsToCompute, cancellationToken);
                        }

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, cancellationToken, t.Initializer));
                        return;
                    }

                case SyntaxKind.IndexerDeclaration:
                    {
                        var t = (IndexerDeclarationSyntax)node;
                        if (t.AccessorList != null)
                        {
                            foreach (var decl in t.AccessorList.Accessors)
                            {
                                ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                            }
                        }

                        if (t.ExpressionBody != null)
                        {
                            ComputeDeclarations(model, t.ExpressionBody, shouldSkip, getSymbol, builder, levelsToCompute, cancellationToken);
                        }

                        var codeBlocks = t.ParameterList != null ? t.ParameterList.Parameters.Select(p => p.Default) : SpecializedCollections.EmptyEnumerable<SyntaxNode>();

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        return;
                    }

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                    {
                        var t = (AccessorDeclarationSyntax)node;
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, t.Body, cancellationToken));
                        return;
                    }

                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                    {
                        var t = (BaseMethodDeclarationSyntax)node;
                        var codeBlocks = t.ParameterList != null ? t.ParameterList.Parameters.Select(p => p.Default) : SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                        codeBlocks = codeBlocks.Concat(t.Body);

                        var ctorDecl = t as ConstructorDeclarationSyntax;
                        if (ctorDecl != null && ctorDecl.Initializer != null)
                        {
                            codeBlocks = codeBlocks.Concat(ctorDecl.Initializer);
                        }

                        var expressionBody = GetExpressionBodySyntax(t);
                        if (expressionBody != null)
                        {
                            codeBlocks = codeBlocks.Concat(expressionBody);
                        }

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        return;
                    }

                case SyntaxKind.CompilationUnit:
                    {
                        var t = (CompilationUnitSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        return;
                    }

                default:
                    return;
            }
        }

        private static DeclarationInfo GetExpressionBodyDeclarationInfo(
            BasePropertyDeclarationSyntax declarationWithExpressionBody,
            ArrowExpressionClauseSyntax expressionBody,
            SemanticModel model,
            bool getSymbol,
            CancellationToken cancellationToken)
        {
            // TODO: use 'model.GetDeclaredSymbol(expressionBody)' when compiler is fixed to return the getter symbol for it.
            var declaredAccessor = getSymbol ? (model.GetDeclaredSymbol(declarationWithExpressionBody, cancellationToken) as IPropertySymbol)?.GetMethod : null;

            return new DeclarationInfo(
                declaredNode: expressionBody,
                executableCodeBlocks: ImmutableArray.Create<SyntaxNode>(expressionBody),
                declaredSymbol: declaredAccessor);
        }

        /// <summary>
        /// Gets the expression-body syntax from an expression-bodied member. The
        /// given syntax must be for a member which could contain an expression-body.
        /// </summary>
        internal static ArrowExpressionClauseSyntax GetExpressionBodySyntax(CSharpSyntaxNode node)
        {
            ArrowExpressionClauseSyntax arrowExpr = null;
            switch (node.Kind())
            {
                // The ArrowExpressionClause is the declaring syntax for the
                // 'get' SourcePropertyAccessorSymbol of properties and indexers.
                case SyntaxKind.ArrowExpressionClause:
                    arrowExpr = (ArrowExpressionClauseSyntax)node;
                    break;
                case SyntaxKind.MethodDeclaration:
                    arrowExpr = ((MethodDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.OperatorDeclaration:
                    arrowExpr = ((OperatorDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.ConversionOperatorDeclaration:
                    arrowExpr = ((ConversionOperatorDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.PropertyDeclaration:
                    arrowExpr = ((PropertyDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.IndexerDeclaration:
                    arrowExpr = ((IndexerDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    return null;
                default:
                    // Don't throw, just use for the assert in case this is used in the semantic model
                    ExceptionUtilities.UnexpectedValue(node.Kind());
                    break;
            }
            return arrowExpr;
        }
    }
}
