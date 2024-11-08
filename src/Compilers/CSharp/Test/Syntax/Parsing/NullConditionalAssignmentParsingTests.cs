// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

        [Fact]
        public void Assignment_LeftMemberAccess_Nested()
        {
            string source = "a?.b = c = d";
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
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void Increment_LeftMemberAccess()
        {
            // Increment/decrement of a conditional access is not supported.
            string source = "a?.b++";
            UsingExpression(source, TestOptions.Regular13);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.PostIncrementExpression);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                    }
                    N(SyntaxKind.PlusPlusToken);
                }
                EOF();
            }
        }

        [Fact]
        public void NullCoalescing_LeftMemberAccess()
        {
            string source = "a?.b = c ?? d";
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
                        N(SyntaxKind.CoalesceExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.QuestionQuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
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
            string op = SyntaxFacts.GetText(kind);
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
                    N(SyntaxFacts.GetAssignmentExpression(kind));
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(kind);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void Parentheses_Assignment_LHS_01()
        {
            UsingExpression("(c?.F) = 1");

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "F");
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            EOF();
        }

        [Fact]
        public void Invocation_01()
        {
            UsingExpression("c?.M() = 1");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RefAssignment_01()
        {
            UsingExpression("c?.F = ref x");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.RefExpression);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RefReturningLambda_01()
        {
            UsingExpression("c?.F = ref int () => ref x");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void Suppression_01()
        {
            UsingExpression("a?.b! = c");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.ExclamationToken);
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
}
