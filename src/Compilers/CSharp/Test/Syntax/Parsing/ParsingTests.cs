// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class ParsingTests : CSharpTestBase
    {
        private CSharpSyntaxNode? _node;
        private IEnumerator<SyntaxNodeOrToken>? _treeEnumerator;
        private readonly ITestOutputHelper _output;

        public ParsingTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        public override void Dispose()
        {
            base.Dispose();
            VerifyEnumeratorConsumed();
        }

        private void VerifyEnumeratorConsumed()
        {
            if (_treeEnumerator != null)
            {
                var hasNext = _treeEnumerator.MoveNext();
                if (hasNext)
                {
                    DumpAndCleanup();
                    Assert.False(hasNext, $"Test contains unconsumed syntax left over from UsingNode()\r\n{(this._output as TestOutputHelper)?.Output}");
                }
            }
        }

        private bool DumpAndCleanup()
        {
            _treeEnumerator = null; // Prevent redundant errors across different test helpers
            foreach (var _ in EnumerateNodes(_node!, dump: true)) { }
            return false;
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

        protected virtual SyntaxTree ParseTree(string text, CSharpParseOptions? options) => SyntaxFactory.ParseSyntaxTree(text, options);

        public CompilationUnitSyntax ParseFile(string text, CSharpParseOptions? parseOptions = null) =>
            SyntaxFactory.ParseCompilationUnit(text, options: parseOptions);

        internal CompilationUnitSyntax ParseFileExperimental(string text, MessageID feature) =>
            ParseFile(text, parseOptions: TestOptions.Regular.WithExperimental(feature));

        protected virtual CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options) =>
            ParseTree(text, options).GetCompilationUnitRoot();

        internal void UsingStatement(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingStatement(text, options: null, expectedErrors);
        }

        internal void UsingStatement(string text, ParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            var node = SyntaxFactory.ParseStatement(text, options: options);
            Validate(text, node, expectedErrors);
            UsingNode(node);
        }

        internal void UsingDeclaration(string text, ParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            UsingDeclaration(text, offset: 0, options, consumeFullText: true, expectedErrors: expectedErrors);
        }

        internal void UsingDeclaration(string text, int offset = 0, ParseOptions? options = null, bool consumeFullText = true, params DiagnosticDescription[] expectedErrors)
        {
            var node = SyntaxFactory.ParseMemberDeclaration(text, offset, options, consumeFullText);
            Debug.Assert(node is object);
            if (consumeFullText)
            {
                // we validate the text roundtrips
                Assert.Equal(text, node.ToFullString());
            }

            var actualErrors = node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
            UsingNode(node);
        }

        internal void UsingExpression(string text, ParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            UsingNode(text, SyntaxFactory.ParseExpression(text, options: options), expectedErrors);
        }

        protected void UsingNode(string text, CSharpSyntaxNode node, params DiagnosticDescription[] expectedErrors)
        {
            Validate(text, node, expectedErrors);
            UsingNode(node);
        }

        protected void Validate(string text, CSharpSyntaxNode node, params DiagnosticDescription[] expectedErrors)
        {
            // we validate the text roundtrips
            Assert.Equal(text, node.ToFullString());
            var actualErrors = node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        internal void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingExpression(text, options: null, expectedErrors);
        }

        protected SyntaxTree UsingTree(string text, params DiagnosticDescription[] expectedErrors)
        {
            return UsingTree(text, options: null, expectedErrors);
        }

        protected SyntaxTree UsingTree(string text, CSharpParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            return UsingTree(ParseTree(text, options), expectedErrors);
        }

        protected SyntaxTree UsingTree(SyntaxTree tree, params DiagnosticDescription[] expectedErrors)
        {
            VerifyEnumeratorConsumed();
            _node = tree.GetCompilationUnitRoot();
            var actualErrors = _node.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
            var nodes = EnumerateNodes(_node, dump: false);
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
            VerifyEnumeratorConsumed();
            _node = root;
            var nodes = EnumerateNodes(root, dump: false);
            _treeEnumerator = nodes.GetEnumerator();
        }

        /// <summary>
        /// Moves the enumerator and asserts that the current node is of the given kind.
        /// </summary>
        [DebuggerHidden]
        protected SyntaxNodeOrToken N(SyntaxKind kind, string? value = null)
        {
            try
            {
                Assert.True(_treeEnumerator!.MoveNext());
                var current = _treeEnumerator.Current;

                Assert.Equal(kind, current.Kind());
                Assert.False(current.IsMissing);

                if (value != null)
                {
                    Assert.Equal(current.ToString(), value);
                }

                return current;
            }
            catch when (DumpAndCleanup())
            {
                throw;
            }
        }

        /// <summary>
        /// Moves the enumerator and asserts that the current node is of the given kind
        /// and is missing.
        /// </summary>
        [DebuggerHidden]
        protected SyntaxNodeOrToken M(SyntaxKind kind)
        {
            try
            {
                Assert.True(_treeEnumerator!.MoveNext());
                SyntaxNodeOrToken current = _treeEnumerator.Current;
                Assert.Equal(kind, current.Kind());
                Assert.True(current.IsMissing);
                return current;
            }
            catch when (DumpAndCleanup())
            {
                throw;
            }
        }

        /// <summary>
        /// Asserts that the enumerator does not have any more nodes.
        /// </summary>
        [DebuggerHidden]
        protected void EOF()
        {
            if (_treeEnumerator!.MoveNext())
            {
                var tk = _treeEnumerator.Current.Kind();
                DumpAndCleanup();
                Assert.False(true, "Found unexpected node or token of kind: " + tk);
            }
        }

        private IEnumerable<SyntaxNodeOrToken> EnumerateNodes(CSharpSyntaxNode node, bool dump)
        {
            Print(node, dump);
            yield return node;

            var stack = new Stack<ChildSyntaxList.Enumerator>(24);
            stack.Push(node.ChildNodesAndTokens().GetEnumerator());
            Open(dump);

            while (stack.Count > 0)
            {
                var en = stack.Pop();
                if (!en.MoveNext())
                {
                    // no more down this branch
                    Close(dump);
                    continue;
                }

                var current = en.Current;
                stack.Push(en); // put it back on stack (struct enumerator)

                Print(current, dump);
                yield return current;

                if (current.IsNode)
                {
                    // not token, so consider children
                    stack.Push(current.ChildNodesAndTokens().GetEnumerator());
                    Open(dump);
                    continue;
                }
            }

            Done(dump);
        }

        private void Print(SyntaxNodeOrToken node, bool dump)
        {
            if (dump)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierToken:
                    case SyntaxKind.NumericLiteralToken:
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.Utf8StringLiteralToken:
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                    case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                    case SyntaxKind.MultiLineRawStringLiteralToken:
                    case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                        if (node.IsMissing)
                        {
                            goto default;
                        }
                        var value = node.ToString().Replace("\"", "\\\"");
                        _output.WriteLine(@"N(SyntaxKind.{0}, ""{1}"");", node.Kind(), value);
                        break;
                    default:
                        _output.WriteLine("{0}(SyntaxKind.{1});", node.IsMissing ? "M" : "N", node.Kind());
                        break;
                }
            }
        }

        private void Open(bool dump)
        {
            if (dump)
            {
                _output.WriteLine("{");
            }
        }

        private void Close(bool dump)
        {
            if (dump)
            {
                _output.WriteLine("}");
            }
        }

        private void Done(bool dump)
        {
            if (dump)
            {
                _output.WriteLine("EOF();");
            }
        }

        protected static void ParseIncompleteSyntax(string text)
        {
            var tokens = getLexedTokens(text);

            var stringBuilder = new StringBuilder();
            for (int skip = 0; skip < tokens.Length; skip++)
            {
                stringBuilder.Clear();

                for (int i = 0; i < tokens.Length; i++)
                {
                    if (i == skip)
                    {
                        continue;
                    }
                    stringBuilder.Append(tokens[i].Text);
                    stringBuilder.Append(' ');

                    // Verify that we can parse and round-trip
                    _ = SyntaxFactory.ParseSyntaxTree(stringBuilder.ToString(), TestOptions.RegularPreview);
                }
            }

            static ImmutableArray<Syntax.InternalSyntax.SyntaxToken> getLexedTokens(string text)
            {
                var lexer = new Syntax.InternalSyntax.Lexer(Text.SourceText.From(text), CSharpParseOptions.Default);
                var tokensBuilder = ArrayBuilder<Syntax.InternalSyntax.SyntaxToken>.GetInstance();

                while (lexer.Lex(Syntax.InternalSyntax.LexerMode.Syntax) is var token && token.Kind != SyntaxKind.EndOfFileToken)
                {
                    tokensBuilder.Add(token);
                }

                return tokensBuilder.ToImmutableAndFree();
            }
        }
    }
}
