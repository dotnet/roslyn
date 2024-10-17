// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NullConditionalAssignmentParsingTests : ParsingTests
    {
        public NullConditionalAssignmentParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void Assignment_LeftMemberAccess()
        {
            string source = "a?.b = c";
            UsingExpression(source, TestOptions.Regular13);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                EOF();
            }
        }

        [Theory]
        [InlineData(SyntaxKind.BarEqualsToken)]
        [InlineData(SyntaxKind.AmpersandEqualsToken)]
        [InlineData(SyntaxKind.CaretEqualsToken)]
        [InlineData(SyntaxKind.LessThanLessThanEqualsToken)]
        [InlineData(SyntaxKind.GreaterThanGreaterThanEqualsToken)]
        [InlineData(SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken)]
        [InlineData(SyntaxKind.PlusEqualsToken)]
        [InlineData(SyntaxKind.MinusEqualsToken)]
        [InlineData(SyntaxKind.AsteriskEqualsToken)]
        [InlineData(SyntaxKind.SlashEqualsToken)]
        [InlineData(SyntaxKind.PercentEqualsToken)]
        [InlineData(SyntaxKind.EqualsToken)]
        [InlineData(SyntaxKind.QuestionQuestionEqualsToken)]
        public void VariousAssignmentKinds_LeftMemberAccess(SyntaxKind kind)
        {
            string op = SyntaxKindFacts.GetText(kind);
            string source = $"a?.b {op} c";
            UsingExpression(source, TestOptions.Regular13);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(kind); // TODO2 fix
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                EOF();
            }
        }
    }
}
