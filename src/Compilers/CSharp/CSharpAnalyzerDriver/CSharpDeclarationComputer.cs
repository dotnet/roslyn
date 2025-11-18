// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

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
            ComputeDeclarations(model, associatedSymbol: null, model.SyntaxTree.GetRoot(cancellationToken),
                (node, level) => !node.Span.OverlapsWith(span) || InvalidLevel(level),
                getSymbol, builder, null, cancellationToken);
        }

        public static void ComputeDeclarationsInNode(
            SemanticModel model,
            ISymbol associatedSymbol,
            SyntaxNode node,
            bool getSymbol,
            ArrayBuilder<DeclarationInfo> builder,
            CancellationToken cancellationToken,
            int? levelsToCompute = null)
        {
            ComputeDeclarations(model, associatedSymbol, node, (n, level) => InvalidLevel(level), getSymbol, builder, levelsToCompute, cancellationToken);
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
            ISymbol associatedSymbol,
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
                case SyntaxKind.FileScopedNamespaceDeclaration:
                    {
                        var ns = (BaseNamespaceDeclarationSyntax)node;
                        foreach (var decl in ns.Members)
                        {
                            ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        }

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
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    {
                        if (associatedSymbol is IMethodSymbol ctor)
                        {
                            var typeDeclaration = (TypeDeclarationSyntax)node;
                            Debug.Assert(ctor.MethodKind == MethodKind.Constructor && typeDeclaration.ParameterList is object);

                            var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                            AddParameterListInitializersAndAttributes(typeDeclaration.ParameterList, codeBlocks);

                            if (typeDeclaration.BaseList?.Types.FirstOrDefault() is PrimaryConstructorBaseTypeSyntax initializer)
                            {
                                codeBlocks.Add(initializer);
                            }

                            builder.Add(GetDeclarationInfo(node, associatedSymbol, codeBlocks));
                            codeBlocks.Free();
                            return;
                        }

                        goto case SyntaxKind.InterfaceDeclaration;
                    }
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.ExtensionBlockDeclaration:
                    {
                        var t = (TypeDeclarationSyntax)node;
                        foreach (var decl in t.Members)
                        {
                            ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        }

                        var attributes = ArrayBuilder<SyntaxNode>.GetInstance();

                        AddAttributes(t.AttributeLists, attributes);
                        AddTypeParameterListAttributes(t.TypeParameterList, attributes);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        attributes.Free();
                        return;
                    }

                case SyntaxKind.EnumDeclaration:
                    {
                        var t = (EnumDeclarationSyntax)node;
                        foreach (var decl in t.Members)
                        {
                            ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                        }

                        var attributes = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddAttributes(t.AttributeLists, attributes);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        attributes.Free();
                        return;
                    }

                case SyntaxKind.EnumMemberDeclaration:
                    {
                        var t = (EnumMemberDeclarationSyntax)node;
                        var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        codeBlocks.Add(t.EqualsValue);
                        AddAttributes(t.AttributeLists, codeBlocks);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                        codeBlocks.Free();
                        return;
                    }

                case SyntaxKind.DelegateDeclaration:
                    {
                        var t = (DelegateDeclarationSyntax)node;

                        var attributes = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddAttributes(t.AttributeLists, attributes);
                        AddParameterListInitializersAndAttributes(t.ParameterList, attributes);
                        AddTypeParameterListAttributes(t.TypeParameterList, attributes);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));
                        attributes.Free();
                        return;
                    }

                case SyntaxKind.EventDeclaration:
                    {
                        var t = (EventDeclarationSyntax)node;
                        if (t.AccessorList != null)
                        {
                            foreach (var decl in t.AccessorList.Accessors)
                            {
                                ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                            }
                        }
                        var attributes = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddAttributes(t.AttributeLists, attributes);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, attributes, cancellationToken));

                        attributes.Free();
                        return;
                    }

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    {
                        var t = (BaseFieldDeclarationSyntax)node;
                        var attributes = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddAttributes(t.AttributeLists, attributes);
                        foreach (var decl in t.Declaration.Variables)
                        {
                            var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                            codeBlocks.Add(decl.Initializer);
                            codeBlocks.AddRange(attributes);

                            builder.Add(GetDeclarationInfo(model, decl, getSymbol, codeBlocks, cancellationToken));
                            codeBlocks.Free();
                        }

                        attributes.Free();
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
                            foreach (var decl in t.AccessorList.Accessors)
                            {
                                ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                            }
                        }

                        if (t.ExpressionBody != null)
                        {
                            ComputeDeclarations(model, associatedSymbol: null, t.ExpressionBody, shouldSkip, getSymbol, builder, levelsToCompute, cancellationToken);
                        }

                        var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        codeBlocks.Add(t.Initializer);
                        AddAttributes(t.AttributeLists, codeBlocks);
                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));

                        codeBlocks.Free();
                        return;
                    }

                case SyntaxKind.IndexerDeclaration:
                    {
                        var t = (IndexerDeclarationSyntax)node;
                        if (t.AccessorList != null)
                        {
                            foreach (var decl in t.AccessorList.Accessors)
                            {
                                ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                            }
                        }

                        if (t.ExpressionBody != null)
                        {
                            ComputeDeclarations(model, associatedSymbol: null, t.ExpressionBody, shouldSkip, getSymbol, builder, levelsToCompute, cancellationToken);
                        }

                        var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddParameterListInitializersAndAttributes(t.ParameterList, codeBlocks);
                        AddAttributes(t.AttributeLists, codeBlocks);

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));

                        codeBlocks.Free();
                        return;
                    }

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    {
                        var t = (AccessorDeclarationSyntax)node;
                        var blocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        blocks.AddIfNotNull(t.Body);
                        blocks.AddIfNotNull(t.ExpressionBody);
                        AddAttributes(t.AttributeLists, blocks);
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
                        var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                        AddParameterListInitializersAndAttributes(t.ParameterList, codeBlocks);
                        codeBlocks.Add(t.Body);

                        if (t is ConstructorDeclarationSyntax ctorDecl && ctorDecl.Initializer != null)
                        {
                            codeBlocks.Add(ctorDecl.Initializer);
                        }

                        var expressionBody = GetExpressionBodySyntax(t);
                        if (expressionBody != null)
                        {
                            codeBlocks.Add(expressionBody);
                        }

                        AddAttributes(t.AttributeLists, codeBlocks);

                        if (node is MethodDeclarationSyntax methodDecl && methodDecl.TypeParameterList != null)
                        {
                            AddTypeParameterListAttributes(methodDecl.TypeParameterList, codeBlocks);
                        }

                        builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));

                        codeBlocks.Free();
                        return;
                    }

                case SyntaxKind.CompilationUnit:
                    {
                        var t = (CompilationUnitSyntax)node;

                        if (associatedSymbol is IMethodSymbol)
                        {
                            var codeBlocks = ArrayBuilder<SyntaxNode>.GetInstance();
                            codeBlocks.Add(t);

                            builder.Add(GetDeclarationInfo(model, node, getSymbol, codeBlocks, cancellationToken));
                            codeBlocks.Free();
                        }
                        else
                        {
                            foreach (var decl in t.Members)
                            {
                                ComputeDeclarations(model, associatedSymbol: null, decl, shouldSkip, getSymbol, builder, newLevel, cancellationToken);
                            }

                            if (t.AttributeLists.Any())
                            {
                                var attributes = ArrayBuilder<SyntaxNode>.GetInstance();
                                AddAttributes(t.AttributeLists, attributes);
                                builder.Add(GetDeclarationInfo(model, node, getSymbol: false, attributes, cancellationToken));
                                attributes.Free();
                            }
                        }

                        return;
                    }

                default:
                    return;
            }
        }

        private static void AddAttributes(SyntaxList<AttributeListSyntax> attributeLists, ArrayBuilder<SyntaxNode> builder)
        {
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    builder.Add(attribute);
                }
            }
        }

        private static void AddParameterListInitializersAndAttributes(BaseParameterListSyntax parameterList, ArrayBuilder<SyntaxNode> builder)
        {
            if (parameterList is not null)
            {
                foreach (var parameter in parameterList.Parameters)
                {
                    AddParameterInitializersAndAttributes(parameter, builder);
                }
            }
        }

        private static void AddParameterInitializersAndAttributes(ParameterSyntax parameter, ArrayBuilder<SyntaxNode> builder)
        {
            builder.Add(parameter.Default);
            AddAttributes(parameter.AttributeLists, builder);
        }

        private static void AddTypeParameterListAttributes(TypeParameterListSyntax typeParameterList, ArrayBuilder<SyntaxNode> builder)
        {
            if (typeParameterList is not null)
            {
                foreach (var parameter in typeParameterList.Parameters)
                {
                    AddAttributes(parameter.AttributeLists, builder);
                }
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
