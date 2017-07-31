﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected List<SyntaxNode> GetSyntaxNodeList(SyntaxTree syntaxTree)
        {
            return GetSyntaxNodeList(syntaxTree.GetRoot(), null);
        }

        protected List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode> synList)
        {
            if (synList == null)
                synList = new List<SyntaxNode>();

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    synList = GetSyntaxNodeList(child.AsNode(), synList);
            }

            return synList;
        }

        protected int GetPositionForBinding(SyntaxTree tree)
        {
            return GetSyntaxNodeForBinding(GetSyntaxNodeList(tree)).SpanStart;
        }

        protected int GetPositionForBinding(string code)
        {
            const string tag = "/*pos*/";

            return code.IndexOf(tag, StringComparison.Ordinal) + tag.Length;
        }

        protected SyntaxNode GetSyntaxNodeForBinding(List<SyntaxNode> synList)
        {
            return GetSyntaxNodeOfTypeForBinding<SyntaxNode>(synList);
        }

        protected readonly string startString = "/*<bind>*/";
        protected readonly string endString = "/*</bind>*/";

        protected TNode GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
        {
            foreach (var node in synList.OfType<TNode>())
            {
                string exprFullText = node.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(startString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(endString))
                        if (exprFullText.EndsWith(endString, StringComparison.Ordinal))
                            return node;
                        else
                            continue;
                    else
                        return node;
                }

                if (exprFullText.EndsWith(endString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(startString))
                        if (exprFullText.StartsWith(startString, StringComparison.Ordinal))
                            return node;
                        else
                            continue;
                    else
                        return node;
                }
            }

            return null;
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
            var compilation = CreateStandardCompilation(source);
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
            var compilation = CreateStandardCompilation(testSrc, new[] { SystemCoreRef }, parseOptions: parseOptions);
            return GetSemanticInfoForTest<TNode>(compilation);
        }

        internal CompilationUtils.SemanticInfoSummary GetSemanticInfoForTestExperimental<TNode>(string testSrc, MessageID feature) where TNode : SyntaxNode
        {
            var compilation = CreateExperimentalCompilationWithMscorlib45(testSrc, feature, new[] { SystemCoreRef });
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
            var compilation = CreateStandardCompilation(testSrc, new[] { SystemCoreRef });
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var position = testSrc.IndexOf(subStrForPreprocessNameIndex, StringComparison.Ordinal);
            var nameSyntaxToBind = tree.GetRoot().FindToken(position, findInsideTrivia: true).Parent as IdentifierNameSyntax;

            return model.GetPreprocessingSymbolInfo(nameSyntaxToBind);
        }

        internal AliasSymbol GetAliasInfoForTest(string testSrc)
        {
            var compilation = CreateStandardCompilation(testSrc, new[] { SystemCoreRef });
            return GetAliasInfoForTest(compilation);
        }

        internal AliasSymbol GetAliasInfoForTest(CSharpCompilation compilation)
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            IdentifierNameSyntax syntaxToBind = GetSyntaxNodeOfTypeForBinding<IdentifierNameSyntax>(GetSyntaxNodeList(tree));

            return (AliasSymbol)model.GetAliasInfo(syntaxToBind);
        }

        protected CompilationUtils.SemanticInfoSummary GetSemanticInfoForTest(string testSrc)
        {
            return GetSemanticInfoForTest<ExpressionSyntax>(testSrc);
        }

        protected IOperation GetOperationForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            if (syntaxNode == null)
            {
                return null;
            }

            return model.GetOperationInternal(syntaxNode);
        }

        protected string GetOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode: SyntaxNode
        {
            var operation = GetOperationForTest<TSyntaxNode>(compilation);
            return operation != null ? OperationTreeVerifier.GetOperationTree(operation) : null;
        }

        protected string GetOperationTreeForTest(IOperation operation)  
        {                                                                   
            return operation != null ? OperationTreeVerifier.GetOperationTree(operation) : null;
        }

        protected string GetOperationTreeForTest<TSyntaxNode>(string testSrc, string expectedOperationTree, CSharpCompilationOptions compilationOptions = null, CSharpParseOptions parseOptions = null)
            where TSyntaxNode : SyntaxNode
        {
            var compilation = CreateStandardCompilation(testSrc, new[] { SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            return GetOperationTreeForTest<TSyntaxNode>(compilation);
        }

        protected void VerifyOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedOperationTree, Action<IOperation> AdditionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var actualOperation = GetOperationForTest<TSyntaxNode>(compilation);
            var actualOperationTree = GetOperationTreeForTest(actualOperation);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
            AdditionalOperationTreeVerifier?.Invoke(actualOperation);
        }

        protected void VerifyOperationTreeForTest<TSyntaxNode>(string testSrc, string expectedOperationTree, CSharpCompilationOptions compilationOptions = null, CSharpParseOptions parseOptions = null)
            where TSyntaxNode : SyntaxNode
        {
            var actualOperationTree = GetOperationTreeForTest<TSyntaxNode>(testSrc, expectedOperationTree, compilationOptions, parseOptions);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
        }

        protected void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedOperationTree, DiagnosticDescription[] expectedDiagnostics, Action<IOperation> AdditionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var actualDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
            actualDiagnostics.Verify(expectedDiagnostics);
            VerifyOperationTreeForTest<TSyntaxNode>(compilation, expectedOperationTree, AdditionalOperationTreeVerifier);
        }

        private static readonly MetadataReference[] s_defaultOperationReferences = new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef };

        protected void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(string testSrc, 
            string expectedOperationTree, 
            DiagnosticDescription[] expectedDiagnostics, 
            CSharpCompilationOptions compilationOptions = null, 
            CSharpParseOptions parseOptions = null, 
            MetadataReference[] additionalReferences = null,
            Action<IOperation> AdditionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var references = additionalReferences == null ? s_defaultOperationReferences : additionalReferences.Concat(s_defaultOperationReferences);
            var compilation = CreateStandardCompilation(testSrc, references, sourceFileName: "file.cs", options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier);
        }

        protected MetadataReference VerifyOperationTreeAndDiagnosticsForTestWithIL<TSyntaxNode>(string testSrc, 
            string ilSource, 
            string expectedOperationTree, 
            DiagnosticDescription[] expectedDiagnostics, 
            CSharpCompilationOptions compilationOptions = null, 
            CSharpParseOptions parseOptions = null, 
            MetadataReference[] additionalReferences = null,
            Action<IOperation> AdditionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(testSrc, expectedOperationTree, expectedDiagnostics, compilationOptions, parseOptions, new[] { ilReference }, AdditionalOperationTreeVerifier);
            return ilReference;
        }  
    }
}
