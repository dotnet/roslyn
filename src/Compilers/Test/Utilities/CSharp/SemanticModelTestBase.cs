// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class SemanticModelTestBase : CSharpTestBase
    {
        protected int GetPositionForBinding(SyntaxTree tree)
        {
            return GetSyntaxNodeForBinding(GetSyntaxNodeList(tree)).SpanStart;
        }

        protected int GetPositionForBinding(string code)
        {
            const string tag = "/*pos*/";

            return code.IndexOf(tag, StringComparison.Ordinal) + tag.Length;
        }

        protected List<ExpressionSyntax> GetExprSyntaxList(SyntaxTree syntaxTree)
        {
            return GetExprSyntaxList(syntaxTree.GetRoot(), null);
        }

        private List<ExpressionSyntax> GetExprSyntaxList(SyntaxNode node, List<ExpressionSyntax> exprSynList)
        {
            if (exprSynList == null)
                exprSynList = new List<ExpressionSyntax>();

            if (node is ExpressionSyntax)
            {
                exprSynList.Add(node as ExpressionSyntax);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    exprSynList = GetExprSyntaxList(child.AsNode(), exprSynList);
            }

            return exprSynList;
        }

        protected ExpressionSyntax GetExprSyntaxForBinding(List<ExpressionSyntax> exprSynList, int index = 0)
        {
            var tagName = string.Format("bind{0}", index == 0 ? String.Empty : index.ToString());
            var startComment = string.Format("/*<{0}>*/", tagName);
            var endComment = string.Format("/*</{0}>*/", tagName);

            foreach (var exprSyntax in exprSynList)
            {
                string exprFullText = exprSyntax.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(startComment, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(endComment))
                        if (exprFullText.EndsWith(endComment, StringComparison.Ordinal))
                            return exprSyntax;
                        else
                            continue;
                    else
                        return exprSyntax;
                }

                if (exprFullText.EndsWith(endComment, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(startComment))
                        if (exprFullText.StartsWith(startComment, StringComparison.Ordinal))
                            return exprSyntax;
                        else
                            continue;
                    else
                        return exprSyntax;
                }
            }

            return null;
        }

        internal static SymbolInfo BindFirstConstructorInitializer(string source)
        {
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var constructorInitializer = GetFirstConstructorInitializer(tree.GetCompilationUnitRoot());

            Assert.NotNull(constructorInitializer);

            return model.GetSpeculativeSymbolInfo(constructorInitializer.SpanStart, constructorInitializer);
        }

        private static ConstructorInitializerSyntax GetFirstConstructorInitializer(SyntaxNode node)
        {
            Func<SyntaxNode, bool> isConstructorInitializer = n =>
                n.IsKind(SyntaxKind.BaseConstructorInitializer) || n.IsKind(SyntaxKind.ThisConstructorInitializer);
            var constructorInitializers = node.DescendantNodesAndSelf(n => !(n is ExpressionSyntax)).Where(isConstructorInitializer);
            return (ConstructorInitializerSyntax)constructorInitializers.FirstOrDefault();
        }

        protected CompilationUtils.SemanticInfoSummary GetSemanticInfoForTest<TNode>(string testSrc, CSharpParseOptions parseOptions = null) where TNode : SyntaxNode
        {
            var compilation = CreateCompilation(testSrc, parseOptions: parseOptions);
            return GetSemanticInfoForTest<TNode>(compilation);
        }

        internal CompilationUtils.SemanticInfoSummary GetSemanticInfoForTestExperimental<TNode>(string testSrc, MessageID feature, CSharpParseOptions parseOptions = null) where TNode : SyntaxNode
        {
            var compilation = CreateExperimentalCompilationWithMscorlib45(testSrc, feature, parseOptions: parseOptions);
            return GetSemanticInfoForTest<TNode>(compilation);
        }

        protected CompilationUtils.SemanticInfoSummary GetSemanticInfoForTest<TNode>(CSharpCompilation compilation) where TNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var syntaxToBind = GetSyntaxNodeOfTypeForBinding<TNode>(GetSyntaxNodeList(tree));

            return model.GetSemanticInfoSummary(syntaxToBind);
        }

        internal PreprocessingSymbolInfo GetPreprocessingSymbolInfoForTest(string testSrc, string subStrForPreprocessNameIndex)
        {
            var compilation = CreateCompilation(testSrc);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var position = testSrc.IndexOf(subStrForPreprocessNameIndex, StringComparison.Ordinal);
            var nameSyntaxToBind = tree.GetRoot().FindToken(position, findInsideTrivia: true).Parent as IdentifierNameSyntax;

            return model.GetPreprocessingSymbolInfo(nameSyntaxToBind);
        }

        internal AliasSymbol GetAliasInfoForTest(string testSrc)
        {
            var compilation = CreateCompilation(testSrc);
            return GetAliasInfoForTest(compilation);
        }

        internal AliasSymbol GetAliasInfoForTest(CSharpCompilation compilation)
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            IdentifierNameSyntax syntaxToBind = GetSyntaxNodeOfTypeForBinding<IdentifierNameSyntax>(GetSyntaxNodeList(tree));

            return model.GetAliasInfo(syntaxToBind).GetSymbol();
        }

        protected CompilationUtils.SemanticInfoSummary GetSemanticInfoForTest(string testSrc)
        {
            return GetSemanticInfoForTest<ExpressionSyntax>(testSrc);
        }
    }
}
