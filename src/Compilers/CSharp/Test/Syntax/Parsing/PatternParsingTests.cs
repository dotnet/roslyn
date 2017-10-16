﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternParsingTexts : ParsingTests
    {
        public PatternParsingTexts(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CasePatternVersusFeatureFlag()
        {
            var test = @"
class C 
{
    public static void Main(string[] args)
    {
        switch ((int) args[0][0])
        {
            case 1:
            case 2 when args.Length == 2:
            case 1<<2:
            case string s:
            default:
                break;
        }
        bool b = args[0] is string s;
    }
}
";
            CreateStandardCompilation(test, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (9,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case 2 when args.Length == 2:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case 2 when args.Length == 2:").WithArguments("pattern matching", "7.0").WithLocation(9, 13),
                // (11,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case string s:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case string s:").WithArguments("pattern matching", "7.0").WithLocation(11, 13),
                // (15,18): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         bool b = args[0] is string s;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "args[0] is string s").WithArguments("pattern matching", "7.0").WithLocation(15, 18),
                // (11,18): error CS8121: An expression of type 'int' cannot be handled by a pattern of type 'string'.
                //             case string s:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("int", "string").WithLocation(11, 18),
                // (11,25): error CS0136: A local or parameter named 's' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case string s:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "s").WithArguments("s").WithLocation(11, 25)
            );
        }

        [Fact]
        public void ThrowExpression_Good()
        {
            var test = @"using System;
class C
{
    public static void Sample(bool b, string s)
    {
        void NeverReturnsFunction() => throw new NullReferenceException();
        int x = b ? throw new NullReferenceException() : 1;
        x = b ? 2 : throw new NullReferenceException();
        s = s ?? throw new NullReferenceException();
        NeverReturnsFunction();
        throw new NullReferenceException() ?? throw new NullReferenceException() ?? throw null;
    }
    public static void NeverReturns() => throw new NullReferenceException();
}";
            CreateStandardCompilation(test).VerifyDiagnostics();
            CreateStandardCompilation(test, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,14): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         void NeverReturnsFunction() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "NeverReturnsFunction").WithArguments("local functions", "7.0").WithLocation(6, 14),
                // (6,40): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         void NeverReturnsFunction() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException()").WithArguments("throw expression", "7.0").WithLocation(6, 40),
                // (7,21): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         int x = b ? throw new NullReferenceException() : 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException()").WithArguments("throw expression", "7.0").WithLocation(7, 21),
                // (8,21): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         x = b ? 2 : throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException()").WithArguments("throw expression", "7.0").WithLocation(8, 21),
                // (9,18): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         s = s ?? throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException()").WithArguments("throw expression", "7.0").WithLocation(9, 18),
                // (11,47): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         throw new NullReferenceException() ?? throw new NullReferenceException() ?? throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException() ?? throw null").WithArguments("throw expression", "7.0").WithLocation(11, 47),
                // (11,85): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         throw new NullReferenceException() ?? throw new NullReferenceException() ?? throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw null").WithArguments("throw expression", "7.0").WithLocation(11, 85),
                // (13,42): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //     public static void NeverReturns() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw new NullReferenceException()").WithArguments("throw expression", "7.0").WithLocation(13, 42)
                );
        }

        [Fact]
        public void ThrowExpression_Bad()
        {
            var test = @"using System;
class C
{
    public static void Sample(bool b, string s)
    {
        // throw expression at wrong precedence
        s = s + throw new NullReferenceException();
        if (b || throw new NullReferenceException()) { }

        // throw expression where not permitted
        var z = from x in throw new NullReferenceException() select x;
        M(throw new NullReferenceException());
        throw throw null;
        (int, int) w = (1, throw null);
        return throw null;
    }
    static void M(string s) {}
}";
            CreateStandardCompilation(test).VerifyDiagnostics(
                // (7,17): error CS1525: Invalid expression term 'throw'
                //         s = s + throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "throw new NullReferenceException()").WithArguments("throw").WithLocation(7, 17),
                // (8,18): error CS1525: Invalid expression term 'throw'
                //         if (b || throw new NullReferenceException()) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "throw new NullReferenceException()").WithArguments("throw").WithLocation(8, 18),
                // (11,27): error CS8115: A throw expression is not allowed in this context.
                //         var z = from x in throw new NullReferenceException() select x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(11, 27),
                // (12,11): error CS8115: A throw expression is not allowed in this context.
                //         M(throw new NullReferenceException());
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(12, 11),
                // (13,15): error CS8115: A throw expression is not allowed in this context.
                //         throw throw null;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(13, 15),
                // (14,9): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, int) w = (1, throw null);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(14, 9),
                // (14,28): error CS8115: A throw expression is not allowed in this context.
                //         (int, int) w = (1, throw null);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(14, 28),
                // (14,24): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, int) w = (1, throw null);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, throw null)").WithArguments("System.ValueTuple`2").WithLocation(14, 24),
                // (15,16): error CS8115: A throw expression is not allowed in this context.
                //         return throw null;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(15, 16),
                // (14,9): warning CS0162: Unreachable code detected
                //         (int, int) w = (1, throw null);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "(").WithLocation(14, 9)
                );
        }

        [Fact]
        public void ThrowExpression()
        {
            UsingTree(@"
class C
{
    int x = y ?? throw null;
}", options: TestOptions.Regular);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.CoalesceExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.QuestionQuestionToken);
                                        N(SyntaxKind.ThrowExpression);
                                        {
                                            N(SyntaxKind.ThrowKeyword);
                                            N(SyntaxKind.NullLiteralExpression);
                                            {
                                                N(SyntaxKind.NullKeyword);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(14785, "https://github.com/dotnet/roslyn/issues/14785")]
        public void IsPatternPrecedence_1()
        {
            UsingNode(SyntaxFactory.ParseExpression("A is B < C, D > [ ]"));
            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(14785, "https://github.com/dotnet/roslyn/issues/14785")]
        public void IsPatternPrecedence_2()
        {
            UsingNode(SyntaxFactory.ParseExpression("A < B > C"));
            N(SyntaxKind.GreaterThanExpression);
            {
                N(SyntaxKind.LessThanExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }

                }
                N(SyntaxKind.GreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            EOF();
        }

        [Fact, WorkItem(14785, "https://github.com/dotnet/roslyn/issues/14785")]
        public void IsPatternPrecedence_3()
        {
            SyntaxFactory.ParseExpression("e is A<B> && e").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B> || e").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B> ^ e").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B> | e").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B> & e").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B>[]").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("new { X = e is A<B> }").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("e is A<B>").GetDiagnostics().Verify();

            SyntaxFactory.ParseExpression("(item is Dictionary<string, object>[])").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("A is B < C, D > [ ]").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("A is B < C, D > [ ] E").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("A < B > C").GetDiagnostics().Verify();
        }

        [Fact]
        public void QueryContextualPatternVariable_01()
        {
            SyntaxFactory.ParseExpression("from s in a where s is string where s.Length > 1 select s").GetDiagnostics().Verify();
            SyntaxFactory.ParseExpression("M(out int? x)").GetDiagnostics().Verify();
        }

        [Fact]
        public void TypeDisambiguation_01()
        {
            UsingStatement(@"
                var r = from s in a
                        where s is X<T> // should disambiguate as a type here
                        where M(s)
                        select s as X<T>;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "r");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.QueryExpression);
                            {
                                N(SyntaxKind.FromClause);
                                {
                                    N(SyntaxKind.FromKeyword);
                                    N(SyntaxKind.IdentifierToken, "s");
                                    N(SyntaxKind.InKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.QueryBody);
                                {
                                    N(SyntaxKind.WhereClause);
                                    {
                                        N(SyntaxKind.WhereKeyword);
                                        N(SyntaxKind.IsExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "s");
                                            }
                                            N(SyntaxKind.IsKeyword);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "X");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "T");
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                    N(SyntaxKind.WhereClause);
                                    {
                                        N(SyntaxKind.WhereKeyword);
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "M");
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "s");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.SelectClause);
                                    {
                                        N(SyntaxKind.SelectKeyword);
                                        N(SyntaxKind.AsExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "s");
                                            }
                                            N(SyntaxKind.AsKeyword);
                                            N(SyntaxKind.GenericName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "X");
                                                N(SyntaxKind.TypeArgumentList);
                                                {
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "T");
                                                    }
                                                    N(SyntaxKind.GreaterThanToken);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypeDisambiguation_02()
        {
            UsingStatement(@"
                var r = a is X<T> // should disambiguate as a type here
                        is bool;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "r");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsExpression);
                            {
                                N(SyntaxKind.IsExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.IsKeyword);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.BoolKeyword);
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypeDisambiguation_03()
        {
            UsingStatement(@"
                var r = a is X<T> // should disambiguate as a type here
                        > Z;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "r");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.GreaterThanExpression);
                            {
                                N(SyntaxKind.IsExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.IsKeyword);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Z");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence00()
        {
            UsingExpression("A is B << C");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.LeftShiftExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.LessThanLessThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence01()
        {
            UsingExpression("A is 1 << 2");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.LeftShiftExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.LessThanLessThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence02()
        {
            UsingExpression("A is null < B");
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence02b()
        {
            UsingExpression("A is B < C");
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence03()
        {
            UsingExpression("A is null == B");
            N(SyntaxKind.EqualsExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.EqualsEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence04()
        {
            UsingExpression("A is null & B");
            N(SyntaxKind.BitwiseAndExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.AmpersandToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence05()
        {
            UsingExpression("A is null && B");
            N(SyntaxKind.LogicalAndExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.AmpersandAmpersandToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence05b()
        {
            UsingExpression("A is null || B");
            N(SyntaxKind.LogicalOrExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
                N(SyntaxKind.BarBarToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            EOF();
        }

        [Fact, WorkItem(15734, "https://github.com/dotnet/roslyn/issues/15734")]
        public void PatternExpressionPrecedence06()
        {
            UsingStatement(@"switch (e) {
case 1 << 2:
case B << C:
case null < B:
case null == B:
case null & B:
case null && B:
    break;
}");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.LeftShiftExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                            N(SyntaxKind.LessThanLessThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.LeftShiftExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.LessThanLessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.BitwiseAndExpression);
                        {
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                            N(SyntaxKind.AmpersandToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.LogicalAndExpression);
                        {
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
                            }
                            N(SyntaxKind.AmpersandAmpersandToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(21515, "https://github.com/dotnet/roslyn/issues/21515")]
        public void PatternExpressionPrecedence07()
        {
            // This should actually be error-free.
            UsingStatement(@"switch (array) {
case KeyValuePair<string, DateTime>[] pairs1:
case KeyValuePair<String, DateTime>[] pairs2:
    break;
}");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "array");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "KeyValuePair");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.StringKeyword);
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "DateTime");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "pairs1");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "KeyValuePair");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "String");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "DateTime");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "pairs2");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NameofInPattern_01()
        {
            // This should actually be error-free, because `nameof` might be a type.
            UsingStatement(@"switch (e) { case nameof n: ; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nameof");
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "n");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NameofInPattern_02()
        {
            // This should actually be error-free; a constant pattern with nameof(n) as the constant.
            UsingStatement(@"switch (e) { case nameof(n): ; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nameof");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "n");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NameofInPattern_03()
        {
            // This should actually be error-free; a constant pattern with nameof(n) as the constant.
            UsingStatement(@"switch (e) { case nameof(n) when true: ; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "nameof");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "n");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        N(SyntaxKind.WhenClause);
                        {
                            N(SyntaxKind.WhenKeyword);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_01()
        {
            UsingStatement(@"switch (e) { case (((3))): ; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "3");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_02()
        {
            UsingStatement(@"switch (e) { case (((3))) when true: ; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "3");
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.WhenClause);
                        {
                            N(SyntaxKind.WhenKeyword);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_03()
        {
            UsingStatement(@"switch (e) { case (x: ((3))): ; }",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (x: ((3))): ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(x: ((3)))").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeconstructionPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SubpatternElement);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.ParenthesizedExpression);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "3");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_04()
        {
            UsingStatement(@"switch (e) { case (((x: 3))): ; }",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (((x: 3))): ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(((x: 3)))").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19),
                // (1,20): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (((x: 3))): ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "((x: 3))").WithArguments("recursive patterns", "patterns2").WithLocation(1, 20),
                // (1,21): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (((x: 3))): ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(x: 3)").WithArguments("recursive patterns", "patterns2").WithLocation(1, 21)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeconstructionPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SubpatternElement);
                            {
                                N(SyntaxKind.DeconstructionPattern);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.SubpatternElement);
                                    {
                                        N(SyntaxKind.DeconstructionPattern);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.SubpatternElement);
                                            {
                                                N(SyntaxKind.NameColon);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "x");
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                }
                                                N(SyntaxKind.ConstantPattern);
                                                {
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "3");
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void RecursivePattern_01()
        {
            UsingStatement(@"switch (e) { case T(X: 3, Y: 4){L: 5} p: ; }",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case T(X: 3, Y: 4){L: 5} p: ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "T(X: 3, Y: 4){L: 5} p").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeconstructionPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SubpatternElement);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "3");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.SubpatternElement);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "4");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.PropertySubpattern);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SubpatternElement);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "L");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "5");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "p");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void BrokenPattern_06()
        {
            UsingStatement(@"switch (e) { case (: ; }",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (: ; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(: ").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19),
                // (1,20): error CS1001: Identifier expected
                // switch (e) { case (: ; }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 20),
                // (1,22): error CS1026: ) expected
                // switch (e) { case (: ; }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 22),
                // (1,22): error CS1003: Syntax error, ':' expected
                // switch (e) { case (: ; }
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":", ";").WithLocation(1, 22)
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeconstructionPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void BrokenPattern_07()
        {
            UsingStatement(@"switch (e) { case (",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19),
                // (1,20): error CS1026: ) expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 20),
                // (1,20): error CS1003: Syntax error, ':' expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(1, 20),
                // (1,20): error CS1513: } expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 20)
            );
        }

        [Fact]
        public void ParenthesizedExpression_07()
        {
            UsingStatement(@"switch (e) { case (): }",
                // (1,19): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                // switch (e) { case (): }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "()").WithArguments("recursive patterns", "patterns2").WithLocation(1, 19)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeconstructionPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void BrokenPattern_08()
        {
            UsingStatement(@"switch (e) { case",
            // (1,18): error CS1733: Expected expression
            // switch (e) { case
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 18),
                // (1,18): error CS1003: Syntax error, ':' expected
                // switch (e) { case
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(1, 18),
                // (1,18): error CS1513: } expected
                // switch (e) { case
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 18)
                );
        }

        //[Fact]
        //public void ParenthesizedExpression_04()
        //{
        //    UsingStatement(@"switch (e) { case (((x: 3))): ; }");
        //}
    }
}
