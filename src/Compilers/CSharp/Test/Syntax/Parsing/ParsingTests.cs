// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#define PARSING_TESTS_DUMP

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class ParsingTests : CSharpTestBase
    {
        private IEnumerator<SyntaxNodeOrToken> _treeEnumerator;
        private readonly ITestOutputHelper _output;

        public ParsingTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        public static void ParseAndValidate(string text, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        public static void ParseAndValidate(string text, CSharpParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text, options: options);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        public static void ParseAndValidateFirst(string text, DiagnosticDescription expectedFirstError)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Take(1).Verify(expectedFirstError);
        }

        protected virtual SyntaxTree ParseTree(string text, CSharpParseOptions options) => SyntaxFactory.ParseSyntaxTree(text, options);

        public CompilationUnitSyntax ParseFile(string text, CSharpParseOptions parseOptions = null) =>
            SyntaxFactory.ParseCompilationUnit(text, options: parseOptions);

        internal CompilationUnitSyntax ParseFileExperimental(string text, MessageID feature) =>
            ParseFile(text, parseOptions: TestOptions.Regular.WithExperimental(feature));

        protected virtual CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options) =>
            ParseTree(text, options).GetCompilationUnitRoot();

        internal void UsingStatement(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingStatement(text, options: null, expectedErrors);
        }

        internal void UsingStatement(string text, ParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var node = SyntaxFactory.ParseStatement(text, options: options);
            // we validate the text roundtrips
            Assert.Equal(text, node.ToFullString());
            var actualErrors = node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
            UsingNode(node);
        }

        internal void UsingDeclaration(string text, ParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            UsingDeclaration(text, offset: 0, options, consumeFullText: true, expectedErrors: expectedErrors);
        }

        internal void UsingDeclaration(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true, params DiagnosticDescription[] expectedErrors)
        {
            var node = SyntaxFactory.ParseMemberDeclaration(text, offset, options, consumeFullText);
            if (consumeFullText)
            {
                // we validate the text roundtrips
                Assert.Equal(text, node.ToFullString());
            }

            var actualErrors = node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
            UsingNode(node);
        }

        internal void UsingExpression(string text, ParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            UsingNode(text, SyntaxFactory.ParseExpression(text, options: options), expectedErrors);
        }

        private void UsingNode(string text, CSharpSyntaxNode node, DiagnosticDescription[] expectedErrors)
        {
            // we validate the text roundtrips
            Assert.Equal(text, node.ToFullString());
            var actualErrors = node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
            UsingNode(node);
        }

        internal void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingExpression(text, options: null, expectedErrors);
        }

        /// <summary>
        /// Parses given string and initializes a depth-first preorder enumerator.
        /// </summary>
        protected SyntaxTree UsingTree(string text, CSharpParseOptions options = null)
        {
            var tree = ParseTree(text, options);
            var nodes = EnumerateNodes(tree.GetCompilationUnitRoot());
#if PARSING_TESTS_DUMP
            nodes = nodes.ToArray(); //force eval to dump contents
#endif
            _treeEnumerator = nodes.GetEnumerator();

            return tree;
        }

        /// <summary>
        /// Parses given string and initializes a depth-first preorder enumerator.
        /// </summary>
        protected CSharpSyntaxNode UsingNode(string text)
        {
            var root = ParseNode(text, options: null);
            UsingNode(root);
            return root;
        }

        protected CSharpSyntaxNode UsingNode(string text, CSharpParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var node = ParseNode(text, options);
            UsingNode(text, node, expectedErrors);
            return node;
        }

        /// <summary>
        /// Initializes a depth-first preorder enumerator for the given node.
        /// </summary>
        protected void UsingNode(CSharpSyntaxNode root)
        {
            var nodes = EnumerateNodes(root);
#if PARSING_TESTS_DUMP
            nodes = nodes.ToArray(); //force eval to dump contents
#endif
            _treeEnumerator = nodes.GetEnumerator();
        }

        /// <summary>
        /// Moves the enumerator and asserts that the current node is of the given kind.
        /// </summary>
        [DebuggerHidden]
        protected SyntaxNodeOrToken N(SyntaxKind kind, string value = null)
        {
            Assert.True(_treeEnumerator.MoveNext());
            Assert.Equal(kind, _treeEnumerator.Current.Kind());

            if (value != null)
            {
                Assert.Equal(_treeEnumerator.Current.ToString(), value);
            }

            return _treeEnumerator.Current;
        }

        /// <summary>
        /// Moves the enumerator and asserts that the current node is of the given kind
        /// and is missing.
        /// </summary>
        [DebuggerHidden]
        protected SyntaxNodeOrToken M(SyntaxKind kind)
        {
            Assert.True(_treeEnumerator.MoveNext());
            SyntaxNodeOrToken current = _treeEnumerator.Current;
            Assert.Equal(kind, current.Kind());
            Assert.True(current.IsMissing);
            return current;
        }

        /// <summary>
        /// Moves the enumerator and asserts that the current node is of the given kind
        /// and is missing.
        /// </summary>
        [DebuggerHidden]
        protected void EOF()
        {
            if (_treeEnumerator.MoveNext())
            {
                Assert.False(true, "Found unexpected node or token of kind: " + _treeEnumerator.Current.Kind());
            }
        }

        private IEnumerable<SyntaxNodeOrToken> EnumerateNodes(CSharpSyntaxNode node)
        {
            Print(node);
            yield return node;

            var stack = new Stack<ChildSyntaxList.Enumerator>(24);
            stack.Push(node.ChildNodesAndTokens().GetEnumerator());
            Open();

            while (stack.Count > 0)
            {
                var en = stack.Pop();
                if (!en.MoveNext())
                {
                    // no more down this branch
                    Close();
                    continue;
                }

                var current = en.Current;
                stack.Push(en); // put it back on stack (struct enumerator)

                Print(current);
                yield return current;

                if (current.IsNode)
                {
                    // not token, so consider children
                    stack.Push(current.ChildNodesAndTokens().GetEnumerator());
                    Open();
                    continue;
                }
            }

            Done();
        }

        [Conditional("PARSING_TESTS_DUMP")]
        private void Print(SyntaxNodeOrToken node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.NumericLiteralToken:
                    if (node.IsMissing)
                    {
                        goto default;
                    }
                    _output.WriteLine(@"N(SyntaxKind.{0}, ""{1}"");", node.Kind(), node.ToString());
                    break;
                default:
                    _output.WriteLine("{0}(SyntaxKind.{1});", node.IsMissing ? "M" : "N", node.Kind());
                    break;
            }
        }

        [Conditional("PARSING_TESTS_DUMP")]
        private void Open()
        {
            _output.WriteLine("{");
        }

        [Conditional("PARSING_TESTS_DUMP")]
        private void Close()
        {
            _output.WriteLine("}");
        }

        [Conditional("PARSING_TESTS_DUMP")]
        private void Done()
        {
            _output.WriteLine("EOF();");
        }
    }
}
