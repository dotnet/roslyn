// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.IncrementalParsing
{
    /// <summary>
    /// A set of tests designed to verify that incremental parsing does the right thing when dealing
    /// with grammatical ambiguities of the form F(G&lt;A, B &gt;(7)) to make sure that changing the
    /// token after the &gt; correctly deals with transforming between a method invocation and a
    /// pair of comparison expressions. (section 7.6.4.2 of the spec, and
    /// LanguageParser.ScanTypeArgumentList).
    /// </summary>
    public class GrammarAmbiguities
    {
        [Fact]
        public void GenericMethodCallInArgumentList_ToComparisons1()
        {
            VerifyReplace(@"class C { void M() { F(G<A, B>", "(7)", ");} }", "7", expectedArgumentCount: 2);
        }

        [Fact]
        public void GenericMethodCallInArgumentList_ToComparisons2()
        {
            VerifyReplace(@"class C { void M() { F(G<A, B>", "(7)", ");} }", "a", expectedArgumentCount: 2);
        }

        [Fact]
        public void GenericMethodCallInArgumentList_StaysCall1()
        {
            foreach (var r in new[] { "(", ")", "]", "}", ":", ";", ",", ".", "?", "==", "!=", "|", "^" })
            {
                VerifyReplace(@"class C { void M() { F(G<A, B>", "(7)", ");} }", r, expectedArgumentCount: 1);
            }
        }

        [Fact]
        public void GenericMethodCallInArgumentList_StaysCall2()
        {
            foreach (var r in new[] { "&&", "||", })
            {
                VerifyReplace(@"class C { void M() { F(G<A, B>", "(7)", ");} }", r, expectedArgumentCount: 1);
            }
        }

        [Fact]
        public void GenericMethodCallInArgumentList_ToCall1()
        {
            VerifyReplace(@"class C { void M() { F(G<A, B>", "7", ");} }", "(7)", expectedArgumentCount: 1);
        }

        [Fact]
        public void GenericMethodCallInArgumentList_ToCall2()
        {
            VerifyReplace(@"class C { void M() { F(G<A, B>", "7", ");} }", "(a)", expectedArgumentCount: 1);
        }

        [Fact]
        public void GenericMethodCallInArgumentList_ToCall3()
        {
            foreach (var r in new[] { "(", ")", "]", "}", ":", ";", ",", ".", "?", "==", "!=", "|", "^" })
            {
                VerifyReplace(@"class C { void M() { F(G<A, B>", "7", ");} }", r, expectedArgumentCount: 1);
            }
        }

        [Fact]
        public void GenericMethodCallInArgumentList_ToCall4()
        {
            foreach (var r in new[] { "&&", "||", })
            {
                VerifyReplace(@"class C { void M() { F(G<A, B>", "7", ");} }", r, expectedArgumentCount: 1);
            }
        }

        private void VerifyReplace(string codeBefore, string codeToBeReplaced, string codeAfter, string replacement, int expectedArgumentCount)
        {
            var start = codeBefore.Length;
            var length = codeToBeReplaced.Length;

            var code = codeBefore + codeToBeReplaced + codeAfter;

            var originalTree = SyntaxFactory.ParseSyntaxTree(code);
            Assert.False(originalTree.GetCompilationUnitRoot().ContainsDiagnostics);

            var incrementalTree = originalTree.WithReplace(start, length, replacement);

            dynamic r = incrementalTree.GetCompilationUnitRoot();
            var args = r.Members[0].Members[0].Body.Statements[0].Expression.ArgumentList.Arguments;

            VerifyIdenticalStructure(incrementalTree);
        }

        private void VerifyIdenticalStructure(SyntaxTree syntaxTree)
        {
            var incrementalRoot = syntaxTree.GetCompilationUnitRoot();
            var parsedRoot = SyntaxFactory.ParseCompilationUnit(syntaxTree.GetText().ToString());

            AssertNodesAreEquivalent(parsedRoot, incrementalRoot);
        }

        private void AssertNodesAreEquivalent(SyntaxNodeOrToken expectedNode, SyntaxNodeOrToken actualNode)
        {
            Assert.Equal(expectedNode.Kind(), actualNode.Kind());
            Assert.Equal(expectedNode.FullSpan, actualNode.FullSpan);
            Assert.Equal(expectedNode.ChildNodesAndTokens().Count, actualNode.ChildNodesAndTokens().Count);

            for (var i = 0; i < expectedNode.ChildNodesAndTokens().Count; i++)
            {
                AssertNodesAreEquivalent(expectedNode.ChildNodesAndTokens()[i],
                    actualNode.ChildNodesAndTokens()[i]);
            }
        }
    }
}
