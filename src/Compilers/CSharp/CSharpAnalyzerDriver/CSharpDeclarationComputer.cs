// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpDeclarationComputer : DeclarationComputer
    {
        public static void ComputeDeclarationsInSpan(
            SemanticModel model,
            TextSpan span,
            bool getSymbol,
            ArrayBuilder<DeclarationInfo> builder,
            CancellationToken cancellationToken)
        {
            ComputeDeclarations(model, model.SyntaxTree.GetRoot(cancellationToken),
                (node, level) => !node.Span.OverlapsWith(span) || InvalidLevel(level),
                getSymbol, builder, null, cancellationToken);
        }

        public static void ComputeDeclarationsInNode(
            SemanticModel model,
            SyntaxNode node,
            bool getSymbol,
            ArrayBuilder<DeclarationInfo> builder,
            CancellationToken cancellationToken,
            int? levelsToCompute = null)
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
            ArrayBuilder<DeclarationInfo> builder,
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
                        var declInfo = GetDeclarationInfo(model, node, getSymbol, cancellationToken);
                        builder.Add(declInfo);

                        NameSyntax name = ns.Name;
                        INamespaceSymbol nsSymbol = declInfo.DeclaredSymbol as INamespaceSymbol;
                        while (name.Kind() == SyntaxKind.QualifiedName)
                        {
                            name = ((QualifiedNameSyntax)name).Left;
                            var declaredSymbol = getSymbol ? nsSymbol?.ContainingNamespace : null;
                            builder.Add(new DeclarationInfo(name, ImmutableArray<SyntaxNode>.Empty, declaredSymbol));
                            nsSymbol = declaredSymbol;
                        }

                        return;
                    }

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    {
                        var t = (TypeDeclarationSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        var attributes = GetAttributes(t.AttributeLists).Concat(GetTypeParameterListAttributes(t.TypeParameterList));
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        return;
                    }

                case SyntaxKind.EnumDeclaration:
                    {
                        var t = (EnumDeclarationSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        var attributes = GetAttributes(t.AttributeLists);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        return;
                    }

                case SyntaxKind.EnumMemberDeclaration:
                    {
                        var t = (EnumMemberDeclarationSyntax)node;
                        var attributes = GetAttributes(t.AttributeLists);
                        var codeBlocks = SpecializedCollections.SingletonEnumerable(t.EqualsValue).Concat(attributes);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        return;
                    }

                case SyntaxKind.DelegateDeclaration:
                    {
                        var t = (DelegateDeclarationSyntax)node;
                        var attributes = GetAttributes(t.AttributeLists)
                            .Concat(GetParameterListInitializersAndAttributes(t.ParameterList))
                            .Concat(GetTypeParameterListAttributes(t.TypeParameterList));
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        return;
                    }

                case SyntaxKind.EventDeclaration:
                    {
                        var t = (EventDeclarationSyntax)node;
                        if (t.AccessorList != null)
                        {
                            foreach (var decl in t.AccessorList.Accessors) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        }
                        var attributes = GetAttributes(t.AttributeLists);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        return;
                    }

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    {
                        var t = (BaseFieldDeclarationSyntax)node;
                        var attributes = GetAttributes(t.AttributeLists);
                        foreach (var decl in t.Declaration.Variables)
                        {
                            var codeBlocks = SpecializedCollections.SingletonEnumerable(decl.Initializer).Concat(attributes);
                            builder.Add(GetDeclarationInfo(model, decl, getSymbol, codeBlocks, cancellationToken));
                        }

                        return;
                    }

                case SyntaxKind.ArrowExpressionClause:
                    {
                        // Arrow expression clause declares getter symbol for properties and indexers.
                        if (node.Parent is BasePropertyDeclarationSyntax parentProperty)
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

                        var attributes = GetAttributes(t.AttributeLists);
                        var codeBlocks = SpecializedCollections.SingletonEnumerable(t.Initializer).Concat(attributes);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
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

                        var codeBlocks = GetParameterListInitializersAndAttributes(t.ParameterList);
                        var attributes = GetAttributes(t.AttributeLists);
                        codeBlocks = codeBlocks.Concat(attributes);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        return;
                    }

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                    {
                        var t = (AccessorDeclarationSyntax)node;
                        var blocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        blocks.AddIfNotNull(t.Body);
                        blocks.AddIfNotNull(t.ExpressionBody);
                        blocks.AddRange(GetAttributes(t.AttributeLists));
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, blocks, cancellationToken));
                        blocks.Free();

                        return;
                    }

                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                    {
                        var t = (BaseMethodDeclarationSyntax)node;
                        var codeBlocks = GetParameterListInitializersAndAttributes(t.ParameterList);
                        codeBlocks = codeBlocks.Concat(t.Body);

                        if (t is ConstructorDeclarationSyntax {
                            Initializer: {
                            }
                        } ctorDecl)
                        {
                            codeBlocks = codeBlocks.Concat(ctorDecl.Initializer);
                        }

                        var expressionBody = GetExpressionBodySyntax(t);
                        if (expressionBody != null)
                        {
                            codeBlocks = codeBlocks.Concat(expressionBody);
                        }

                        codeBlocks = codeBlocks.Concat(GetAttributes(t.AttributeLists));

                        if (node is MethodDeclarationSyntax methodDecl && methodDecl.TypeParameterList != null)
                        {
                            codeBlocks = codeBlocks.Concat(GetTypeParameterListAttributes(methodDecl.TypeParameterList));
                        }

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        return;
                    }

                case SyntaxKind.CompilationUnit:
                    {
                        var t = (CompilationUnitSyntax)node;
                        foreach (var decl in t.Members) ComputeDeclarations(model, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);

                        if (t.AttributeLists.Any())
                        {
                            var attributes = GetAttributes(t.AttributeLists);
                            builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        }
                        return;
                    }

                default:
                    return;
            }
        }

        private static IEnumerable<SyntaxNode> GetAttributes(SyntaxList<AttributeListSyntax> attributeLists)
        {
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    yield return attribute;
                }
            }
        }

        private static IEnumerable<SyntaxNode> GetParameterListInitializersAndAttributes(BaseParameterListSyntax parameterList) =>
            parameterList != null ?
            parameterList.Parameters.SelectMany(p => GetParameterInitializersAndAttributes(p)) :
            SpecializedCollections.EmptyEnumerable<SyntaxNode>();

        private static IEnumerable<SyntaxNode> GetParameterInitializersAndAttributes(ParameterSyntax parameter) =>
            SpecializedCollections.SingletonEnumerable(parameter.Default).Concat(GetAttributes(parameter.AttributeLists));

        private static IEnumerable<SyntaxNode> GetTypeParameterListAttributes(TypeParameterListSyntax typeParameterList) =>
            typeParameterList != null ?
            typeParameterList.Parameters.SelectMany(p => GetAttributes(p.AttributeLists)) :
            SpecializedCollections.EmptyEnumerable<SyntaxNode>();

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
                    arrowExpr = ((ConstructorDeclarationSyntax)node).ExpressionBody;
                    break;
                case SyntaxKind.DestructorDeclaration:
                    arrowExpr = ((DestructorDeclarationSyntax)node).ExpressionBody;
                    break;
                default:
                    // Don't throw, just use for the assert in case this is used in the semantic model
                    ExceptionUtilities.UnexpectedValue(node.Kind());
                    break;
            }
            return arrowExpr;
        }
    }
}
