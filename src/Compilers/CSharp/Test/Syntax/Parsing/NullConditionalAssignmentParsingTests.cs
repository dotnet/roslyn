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
        public static TheoryData<CSharpParseOptions> AllTestOptions { get; } = new TheoryData<CSharpParseOptions>([TestOptions.Regular13, TestOptions.RegularPreview]);

        public NullConditionalAssignmentParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Assignment_LeftMemberAccess(CSharpParseOptions parseOptions)
        {
            string source = "a?.b = c";
            UsingExpression(source, parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Assignment_LeftMemberAccess_Nested_01(CSharpParseOptions parseOptions)
        {
            string source = "a?.b = c = d";
            UsingExpression(source, parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Assignment_LeftMemberAccess_Nested_02(CSharpParseOptions parseOptions)
        {
            string source = "a?.b = c = d = e";
            UsingExpression(source, parseOptions);
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
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Assignment_LeftMemberAccess_Nested_03(CSharpParseOptions parseOptions)
        {
            string source = "a?.b = c?[d] = e?.f";
            UsingExpression(source, parseOptions);
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
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.ElementBindingExpression);
                            {
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "f");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Increment_LeftMemberAccess(CSharpParseOptions parseOptions)
        {
            // Increment/decrement of a conditional access is not supported.
            string source = "a?.b++";
            UsingExpression(source, parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void PreIncrement_ConditionalElementAccess(CSharpParseOptions parseOptions)
        {
            // Increment/decrement of a conditional access is not supported.
            string source = "--a?[b]";
            UsingExpression(source, parseOptions);
            N(SyntaxKind.PreDecrementExpression);
            {
                N(SyntaxKind.MinusMinusToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ElementBindingExpression);
                    {
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
            }
            EOF();
        }

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void NullCoalescing_LeftMemberAccess(CSharpParseOptions parseOptions)
        {
            string source = "a?.b = c ?? d";
            UsingExpression(source, parseOptions);

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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Parentheses_Assignment_LHS_01(CSharpParseOptions parseOptions)
        {
            UsingExpression("(c?.F) = 1", parseOptions);

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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Invocation_01(CSharpParseOptions parseOptions)
        {
            UsingExpression("c?.M() = 1", parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void RefAssignment_01(CSharpParseOptions parseOptions)
        {
            UsingExpression("c?.F = ref x", parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void RefReturningLambda_01(CSharpParseOptions parseOptions)
        {
            UsingExpression("c?.F = ref int () => ref x", parseOptions);
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

        [Theory]
        [MemberData(nameof(AllTestOptions))]
        public void Suppression_01(CSharpParseOptions parseOptions)
        {
            UsingExpression("a?.b! = c", parseOptions);
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
