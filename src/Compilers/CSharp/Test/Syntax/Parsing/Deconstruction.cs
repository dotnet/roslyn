// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    // (T id, ...) = is a deconstruction-declaration
    
    // (T, ...) id
    // and (T, ...) [] are types
    // (E, ...) = is a deconstruction-assignment
    // (E, ...) folowed by ., +, -, etc is an expression

    public class DeconstructionTests : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void TupleType()
        {
            // (T id, ...) id is a tuple type

            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32 a, Int64 b) x;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ReturnStatement);
                            {
                                N(SyntaxKind.ReturnKeyword);
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.StringLiteralExpression);
                                        {
                                            N(SyntaxKind.StringLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
