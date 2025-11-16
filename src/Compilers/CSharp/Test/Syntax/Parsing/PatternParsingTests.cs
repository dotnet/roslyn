// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternParsingTests : ParsingTests
    {
        private new void UsingStatement(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingStatement(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8), expectedErrors);
        }

        public PatternParsingTests(ITestOutputHelper output) : base(output)
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
            CreateCompilation(test, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (9,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case 2 when args.Length == 2:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case").WithArguments("pattern matching", "7.0").WithLocation(9, 13),
                // (11,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case string s:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case").WithArguments("pattern matching", "7.0").WithLocation(11, 13),
                // (11,18): error CS8121: An expression of type 'int' cannot be handled by a pattern of type 'string'.
                //             case string s:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("int", "string").WithLocation(11, 18),
                // (11,25): error CS0136: A local or parameter named 's' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case string s:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "s").WithArguments("s").WithLocation(11, 25),
                // (15,26): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         bool b = args[0] is string s;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "is").WithArguments("pattern matching", "7.0").WithLocation(15, 26));
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
            CreateCompilation(test).VerifyDiagnostics();
            CreateCompilation(test, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (6,14): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         void NeverReturnsFunction() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "NeverReturnsFunction").WithArguments("local functions", "7.0").WithLocation(6, 14),
                // (6,40): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         void NeverReturnsFunction() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(6, 40),
                // (7,21): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         int x = b ? throw new NullReferenceException() : 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(7, 21),
                // (8,21): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         x = b ? 2 : throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(8, 21),
                // (9,18): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         s = s ?? throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(9, 18),
                // (11,47): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         throw new NullReferenceException() ?? throw new NullReferenceException() ?? throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(11, 47),
                // (11,85): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //         throw new NullReferenceException() ?? throw new NullReferenceException() ?? throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(11, 85),
                // (13,42): error CS8059: Feature 'throw expression' is not available in C# 6. Please use language version 7.0 or greater.
                //     public static void NeverReturns() => throw new NullReferenceException();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "throw").WithArguments("throw expression", "7.0").WithLocation(13, 42)
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
            CreateCompilationWithMscorlib46(test).VerifyDiagnostics(
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

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_01()
        {
            UsingExpression("A is B***",
                // (1,10): error CS1733: Expected expression
                // A is B***
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 10)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.MultiplyExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_01b()
        {
            UsingExpression("A is B*** C");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.MultiplyExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_02()
        {
            UsingExpression("A is B***[]");
            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PointerType);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.AsteriskToken);
                        }
                        N(SyntaxKind.AsteriskToken);
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

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_03()
        {
            UsingExpression("A is B***[] C");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.PointerType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                    N(SyntaxKind.AsteriskToken);
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.AsteriskToken);
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
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_04()
        {
            UsingExpression("(B*** C, D)");
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.MultiplyExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_04b()
        {
            UsingExpression("(B*** C)");
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.MultiplyExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.PointerIndirectionExpression);
                    {
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_05()
        {
            UsingExpression("(B***[] C, D)");
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.DeclarationExpression);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.PointerType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                    N(SyntaxKind.AsteriskToken);
                                }
                                N(SyntaxKind.AsteriskToken);
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
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_06()
        {
            UsingExpression("(D, B*** C)");
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.MultiplyExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.PointerIndirectionExpression);
                        {
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_07()
        {
            UsingExpression("(D, B***[] C)");
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.DeclarationExpression);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.PointerType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                    N(SyntaxKind.AsteriskToken);
                                }
                                N(SyntaxKind.AsteriskToken);
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
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_08()
        {
            UsingStatement("switch (e) { case B*** C: break; }");
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
                        N(SyntaxKind.MultiplyExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.AsteriskToken);
                            N(SyntaxKind.PointerIndirectionExpression);
                            {
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.PointerIndirectionExpression);
                                {
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
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

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void ArrayOfPointer_09()
        {
            UsingStatement("switch (e) { case B***[] C: break; }");
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
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PointerType);
                                {
                                    N(SyntaxKind.PointerType);
                                    {
                                        N(SyntaxKind.PointerType);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                            N(SyntaxKind.AsteriskToken);
                                        }
                                        N(SyntaxKind.AsteriskToken);
                                    }
                                    N(SyntaxKind.AsteriskToken);
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
                                N(SyntaxKind.IdentifierToken, "C");
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
            var test = @"switch (e) { case (x: ((3))): ; }";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (x: ((3))): ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case (x: ((3))):')
                // class C { void M() { switch (e) { case (x: ((3))): ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case (x: ((3))):").WithArguments("case (x: ((3))):").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case (x: ((3))): ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(x: ((3)))").WithArguments("recursive patterns", "8.0").WithLocation(1, 40));
            CreateCompilation(testInMethod, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (x: ((3))): ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case (x: ((3))):')
                // class C { void M() { switch (e) { case (x: ((3))): ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case (x: ((3))):").WithArguments("case (x: ((3))):").WithLocation(1, 35));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns);
            checkNodes();

            UsingStatement(test, TestOptions.Regular8);
            checkNodes();

            void checkNodes()
            {
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
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Subpattern);
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
        }

        [Fact]
        public void ParenthesizedExpression_04()
        {
            var test = @"switch (e) { case (((x: 3))): ; }";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (((x: 3))): ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case (((x: 3))):')
                // class C { void M() { switch (e) { case (((x: 3))): ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case (((x: 3))):").WithArguments("case (((x: 3))):").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'parenthesized pattern' is not available in C# 7.3. Please use language version 9.0 or greater.
                // class C { void M() { switch (e) { case (((x: 3))): ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(").WithArguments("parenthesized pattern", "9.0").WithLocation(1, 40),
                // (1,41): error CS8370: Feature 'parenthesized pattern' is not available in C# 7.3. Please use language version 9.0 or greater.
                // class C { void M() { switch (e) { case (((x: 3))): ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(").WithArguments("parenthesized pattern", "9.0").WithLocation(1, 41),
                // (1,42): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case (((x: 3))): ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(x: 3)").WithArguments("recursive patterns", "8.0").WithLocation(1, 42));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns);
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
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
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

            var test = @"switch (e) { case T(X: 3, Y: 4){L: 5} p: ; }";
            var testWithStatement = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case T(X: 3, Y: 4){L: 5} p: ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case T(X: 3, Y: 4){L: 5} p:')
                // class C { void M() { switch (e) { case T(X: 3, Y: 4){L: 5} p: ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case T(X: 3, Y: 4){L: 5} p:").WithArguments("case T(X: 3, Y: 4){L: 5} p:").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case T(X: 3, Y: 4){L: 5} p: ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "T(X: 3, Y: 4){L: 5} p").WithArguments("recursive patterns", "8.0").WithLocation(1, 40),
                // (1,40): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
                // class C { void M() { switch (e) { case T(X: 3, Y: 4){L: 5} p: ; } } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(1, 40));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns);
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
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
                                N(SyntaxKind.Subpattern);
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
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
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
            var test = @"switch (e) { case (: ; }";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (: ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case (:')
                // class C { void M() { switch (e) { case (: ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case (:").WithArguments("case (:").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case (: ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(").WithArguments("recursive patterns", "8.0").WithLocation(1, 40),
                // (1,41): error CS1026: ) expected
                // class C { void M() { switch (e) { case (: ; } } }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ":").WithLocation(1, 41));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns,
                // (1,20): error CS1026: ) expected
                // switch (e) { case (: ; }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ":").WithLocation(1, 20));
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
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
        public void BrokenPattern_07()
        {
            var test = @"switch (e) { case (";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case ( ')
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case ( ").WithArguments("case ( ").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "( ").WithArguments("recursive patterns", "8.0").WithLocation(1, 40),
                // (1,42): error CS1026: ) expected
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 42),
                // (1,42): error CS1003: Syntax error, ':' expected
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "}").WithArguments(":").WithLocation(1, 42),
                // (1,45): error CS1513: } expected
                // class C { void M() { switch (e) { case ( } }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 45));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns,
                // (1,20): error CS1026: ) expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 20),
                // (1,20): error CS1003: Syntax error, ':' expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 20),
                // (1,20): error CS1513: } expected
                // switch (e) { case (
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 20)
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.ColonToken);
                    }
                }
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_07()
        {
            var test = @"switch (e) { case (): }";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (): } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case ():')
                // class C { void M() { switch (e) { case (): } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case ():").WithArguments("case ():").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case (): } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "()").WithArguments("recursive patterns", "8.0").WithLocation(1, 40));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns);
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 18),
                // (1,18): error CS1513: } expected
                // switch (e) { case
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 18)
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
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.ColonToken);
                    }
                }
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParenthesizedExpression_05()
        {
            var test = @"switch (e) { case (x: ): ; }";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case (x: ): ; } } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,35): error CS8070: Control cannot fall out of switch from final case label ('case (x: ):')
                // class C { void M() { switch (e) { case (x: ): ; } } }
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case (x: ):").WithArguments("case (x: ):").WithLocation(1, 35),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case (x: ): ; } } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(x: )").WithArguments("recursive patterns", "8.0").WithLocation(1, 40),
                // (1,44): error CS8504: Pattern missing
                // class C { void M() { switch (e) { case (x: ): ; } } }
                Diagnostic(ErrorCode.ERR_MissingPattern, ")").WithLocation(1, 44));

            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns,
                // (1,23): error CS8504: Pattern missing
                // switch (e) { case (x: ): ; }
                Diagnostic(ErrorCode.ERR_MissingPattern, ")").WithLocation(1, 23));
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    M(SyntaxKind.ConstantPattern);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
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
        public void EmptySwitchExpression()
        {
            var test = "1 switch {}";
            var testInMethod = @$"class C {{ void M() {{ var v = {test}; }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,32): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { var v = 1 switch {}; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "switch").WithArguments("recursive patterns", "8.0").WithLocation(1, 32),
                // (1,32): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                // class C { void M() { var v = 1 switch {}; } }
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(1, 32),
                // (1,32): error CS8506: No best type was found for the switch expression.
                // class C { void M() { var v = 1 switch {}; } }
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(1, 32));

            UsingExpression(test, TestOptions.RegularWithoutRecursivePatterns);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpression01()
        {
            var test = "1 switch {a => b, c => d}";
            var testWithStatement = @$"class C {{ void M() {{ var v = {test}; }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,32): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { var v = 1 switch {a => b, c => d}; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "switch").WithArguments("recursive patterns", "8.0").WithLocation(1, 32),
                // (1,40): error CS0103: The name 'a' does not exist in the current context
                // class C { void M() { var v = 1 switch {a => b, c => d}; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(1, 40),
                // (1,45): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { var v = 1 switch {a => b, c => d}; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 45),
                // (1,48): error CS0103: The name 'c' does not exist in the current context
                // class C { void M() { var v = 1 switch {a => b, c => d}; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(1, 48),
                // (1,53): error CS0103: The name 'd' does not exist in the current context
                // class C { void M() { var v = 1 switch {a => b, c => d}; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(1, 53));

            UsingExpression(test, TestOptions.RegularWithoutRecursivePatterns);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpression02()
        {
            var test = "1 switch { a?b:c => d }";
            var testWithStatement = @$"class C {{ void M() {{ var v = {test}; }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,32): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { var v = 1 switch { a?b:c => d }; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "switch").WithArguments("recursive patterns", "8.0").WithLocation(1, 32),
                // (1,41): error CS0103: The name 'a' does not exist in the current context
                // class C { void M() { var v = 1 switch { a?b:c => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(1, 41),
                // (1,42): error CS1003: Syntax error, '=>' expected
                // class C { void M() { var v = 1 switch { a?b:c => d }; } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(1, 42),
                // (1,42): error CS1525: Invalid expression term '?'
                // class C { void M() { var v = 1 switch { a?b:c => d }; } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(1, 42),
                // (1,43): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { var v = 1 switch { a?b:c => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 43));

            UsingExpression(test, TestOptions.RegularWithoutRecursivePatterns,
                // (1,13): error CS1003: Syntax error, '=>' expected
                // 1 switch { a?b:c => d }
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(1, 13),
                // (1,13): error CS1525: Invalid expression term '?'
                // 1 switch { a?b:c => d }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(1, 13));
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                    M(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpression03()
        {
            var test = "1 switch { (a, b, c) => d }";
            var testWithStatement = @$"class C {{ void M() {{ var v = {test}; }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,32): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "switch").WithArguments("recursive patterns", "8.0").WithLocation(1, 32),
                // (1,41): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "(a, b, c)").WithArguments("recursive patterns", "8.0").WithLocation(1, 41),
                // (1,41): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(a, b, c)").WithArguments("int", "Deconstruct").WithLocation(1, 41),
                // (1,41): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'int', with 3 out parameters and a void return type.
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(a, b, c)").WithArguments("int", "3").WithLocation(1, 41),
                // (1,42): error CS0103: The name 'a' does not exist in the current context
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(1, 42),
                // (1,45): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 45),
                // (1,48): error CS0103: The name 'c' does not exist in the current context
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(1, 48),
                // (1,54): error CS0103: The name 'd' does not exist in the current context
                // class C { void M() { var v = 1 switch { (a, b, c) => d }; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(1, 54));

            UsingExpression("1 switch { (a, b, c) => d }", TestOptions.RegularWithoutRecursivePatterns);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void BrokenRecursivePattern01()
        {
            var test = "switch (e) { case T( : Q x = n; break; } ";
            var testWithStatement = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testWithStatement, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (1,30): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 30),
                // (1,40): error CS8370: Feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "T( ").WithArguments("recursive patterns", "8.0").WithLocation(1, 40),
                // (1,40): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(1, 40),
                // (1,45): error CS0246: The type or namespace name 'Q' could not be found (are you missing a using directive or an assembly reference?)
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Q").WithArguments("Q").WithLocation(1, 45),
                // (1,43): error CS1026: ) expected
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ":").WithLocation(1, 43),
                // (1,51): error CS0103: The name 'n' does not exist in the current context
                // class C { void M() { switch (e) { case T( : Q x = n; break; }  } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(1, 51));

            // This put the parser into an infinite loop at one time. The precise diagnostics and nodes
            // are not as important as the fact that it terminates.
            UsingStatement(test, TestOptions.RegularWithoutRecursivePatterns,
                // (1,22): error CS1026: ) expected
                // switch (e) { case T( : Q x = n; break; } 
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ":").WithLocation(1, 22)
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
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Q");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "n");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
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

        [Fact, WorkItem(26000, "https://github.com/dotnet/roslyn/issues/26000")]
        public void VarIsContextualKeywordForPatterns01()
        {
            UsingStatement("switch (e) { case var: break; }");
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
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

        [Fact, WorkItem(26000, "https://github.com/dotnet/roslyn/issues/26000")]
        public void VarIsContextualKeywordForPatterns02()
        {
            UsingStatement("if (e is var) {}");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem(26000, "https://github.com/dotnet/roslyn/issues/26000")]
        public void WhenAsPatternVariable01()
        {
            UsingStatement("switch (e) { case var when: break; }",
                // (1,27): error CS1525: Invalid expression term ':'
                // switch (e) { case var when: break; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 27)
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
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                        N(SyntaxKind.WhenClause);
                        {
                            N(SyntaxKind.WhenKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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

        [Fact, WorkItem(26000, "https://github.com/dotnet/roslyn/issues/26000")]
        public void WhenAsPatternVariable02()
        {
            UsingStatement("switch (e) { case K when: break; }",
                // (1,25): error CS1525: Invalid expression term ':'
                // switch (e) { case K when: break; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 25)
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
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "K");
                            }
                        }
                        N(SyntaxKind.WhenClause);
                        {
                            N(SyntaxKind.WhenKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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

        [Fact(Skip = "This is not a reliable test, and its failure modes are hard to capture. But it is helpful to run by hand to find parser issues.")]
        public void ParseFuzz()
        {
            Random random = new Random();
            for (int i = 0; i < 4000; i++)
            {
                string source = $"class C{{void M(){{switch(e){{case {makePattern0()}:T v = e;}}}}}}";
                try
                {
                    Parse(source, options: TestOptions.RegularWithRecursivePatterns);
                    for (int j = 0; j < 30; j++)
                    {
                        int k1 = random.Next(source.Length);
                        int k2 = random.Next(source.Length);
                        string source2 = source.Substring(0, k1) + source.Substring(k2);
                        Parse(source2, options: TestOptions.RegularWithRecursivePatterns);
                    }
                }
                catch (StackOverflowException)
                {
                    Console.WriteLine("Failed on \"" + source + "\"");
                    Assert.True(false, source);
                }
                catch (OutOfMemoryException)
                {
                    Console.WriteLine("Failed on \"" + source + "\"");
                    Assert.True(false, source);
                }
            }
            return;

            string makeProps(int maxDepth, bool needNames)
            {
                int nProps = random.Next(maxDepth);
                var builder = new StringBuilder();
                for (int i = 0; i < nProps; i++)
                {
                    if (i != 0) builder.Append(", ");
                    if (needNames || random.Next(5) == 0) builder.Append("N: ");
                    builder.Append(makePattern(maxDepth - 1));
                }
                return builder.ToString();
            }
            string makePattern(int maxDepth)
            {
                if (maxDepth <= 0 || random.Next(6) == 0)
                {
                    switch (random.Next(4))
                    {
                        case 0:
                            return "_";
                        case 1:
                            return "1";
                        case 2:
                            return "T x";
                        default:
                            return "var y";
                    }
                }
                else
                {
                    // recursive pattern
                    while (true)
                    {
                        bool nameType = random.Next(2) == 0;
                        bool parensPart = random.Next(2) == 0;
                        bool propsPart = random.Next(2) == 0;
                        bool name = random.Next(2) == 0;
                        if (!parensPart && !propsPart && !(nameType && name)) continue;
                        return $"{(nameType ? "N" : "")} {(parensPart ? $"({makeProps(maxDepth, false)})" : "")} {(propsPart ? $"{{ {makeProps(maxDepth, true)} }}" : "")} {(name ? "n" : "")}";
                    }
                }
            }
            string makePattern0() => makePattern(random.Next(6));
        }

        [Fact]
        public void ArrayOfTupleType01()
        {
            UsingStatement("if (o is (int, int)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
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
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType02()
        {
            UsingStatement("if (o is (int a, int b)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
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
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType03()
        {
            UsingStatement("if (o is (int, int)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
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
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType04()
        {
            UsingStatement("if (o is (int a, int b)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
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
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType05()
        {
            UsingStatement("if (o is (Int, Int)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType06()
        {
            UsingStatement("if (o is (Int a, Int b)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int");
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int");
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType07()
        {
            UsingStatement("if (o is (Int, Int)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType08()
        {
            UsingStatement("if (o is (Int a, Int b)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType09()
        {
            UsingStatement("if (o is (S.Int, S.Int)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType10()
        {
            UsingStatement("if (o is (S.Int a, S.Int b)[]) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType11()
        {
            UsingStatement("if (o is (S.Int, S.Int)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "S");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "S");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType12()
        {
            UsingStatement("if (o is (S.Int a, S.Int b)[] q) { }");
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "S");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "S");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "q");
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ArrayOfTupleType13()
        {
            UsingStatement("switch (o) { case (int, int)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                            N(SyntaxKind.IntKeyword);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void ArrayOfTupleType14()
        {
            UsingStatement("switch (o) { case (int a, int b)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void ArrayOfTupleType15()
        {
            UsingStatement("switch (o) { case (Int, Int)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void ArrayOfTupleType16()
        {
            UsingStatement("switch (o) { case (Int a, Int b)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int");
                                        }
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void ArrayOfTupleType17()
        {
            UsingStatement("switch (o) { case (S.Int, S.Int)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.QualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.QualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void ArrayOfTupleType18()
        {
            UsingStatement("switch (o) { case (S.Int a, S.Int b)[] q: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
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
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.QualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int");
                                            }
                                        }
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.QualifiedName);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int");
                                            }
                                        }
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "q");
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
        public void RecursivePattern_00()
        {
            UsingStatement("var x = o is Type (Param: 3, Param2: 4) { Prop : 3 } x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_02()
        {
            UsingStatement("var x = o is (Param: 3, Param2: 4) { Prop : 3 } x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_03()
        {
            UsingStatement("var x = o is Type { Prop : 3 } x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_04()
        {
            UsingStatement("var x = o is { Prop : 3 } x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_05()
        {
            UsingStatement("var x = o is Type (Param: 3, Param2: 4) x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_06()
        {
            UsingStatement("var x = o is (Param: 3, Param2: 4) x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_07()
        {
            UsingStatement("var x = o is Type x;");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.DeclarationPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void RecursivePattern_08()
        {
            UsingStatement("var x = o is Type (Param: 3, Param2: 4) { Prop : 3 };");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
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
        public void RecursivePattern_09()
        {
            UsingStatement("var x = o is (Param: 3, Param2: 4) { Prop : 3 };");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
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
        public void RecursivePattern_10()
        {
            UsingStatement("var x = o is Type { Prop : 3 };");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
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
        public void RecursivePattern_11()
        {
            UsingStatement("var x = o is { Prop : 3 };");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Prop");
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
                                        N(SyntaxKind.CloseBraceToken);
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
        public void RecursivePattern_12()
        {
            UsingStatement("var x = o is Type (Param: 3, Param2: 4);");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Type");
                                    }
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
        public void RecursivePattern_13()
        {
            UsingStatement("var x = o is (Param: 3, Param2: 4);");
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
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "o");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param");
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
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Param2");
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
        public void ParenthesizedExpressionOfSwitchExpression()
        {
            UsingStatement("Console.Write((t) switch {var x => x});");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Console");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Write");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.SwitchExpression);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "t");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchExpressionArm);
                                {
                                    N(SyntaxKind.VarPattern);
                                    {
                                        N(SyntaxKind.VarKeyword, "var");
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardInSwitchExpression()
        {
            UsingExpression("e switch { _ => 1 }");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.DiscardPattern);
                    {
                        N(SyntaxKind.UnderscoreToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardInSwitchStatement_01a()
        {
            UsingStatement("switch(e) { case _: break; }");
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
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
        public void DiscardInSwitchStatement_01b()
        {
            UsingStatement("switch(e) { case _: break; }", TestOptions.Regular7_3);
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
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
        public void DiscardInSwitchStatement_02()
        {
            UsingStatement("switch(e) { case _ when true: break; }");
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
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
        public void DiscardInRecursivePattern_01()
        {
            UsingExpression("e is (_, _)");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.DiscardPattern);
                            {
                                N(SyntaxKind.UnderscoreToken);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.DiscardPattern);
                            {
                                N(SyntaxKind.UnderscoreToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void DiscardInRecursivePattern_02()
        {
            UsingExpression("e is { P: _ }");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "P");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.DiscardPattern);
                            {
                                N(SyntaxKind.UnderscoreToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NotDiscardInIsTypeExpression()
        {
            UsingExpression("e is _");
            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
            }
            EOF();
        }

        [Fact]
        public void ShortTuplePatterns()
        {
            UsingExpression(
@"e switch {
    var () => 1,
    () => 2,
    var (x) => 3,
    (1) _ => 4,
    (1) x => 5,
    (1) {} => 6,
    (Item1: 1) => 7,
    C(1) => 8
}",
                expectedErrors: new DiagnosticDescription[0]);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.ParenthesizedVariableDesignation);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.ParenthesizedVariableDesignation);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.DiscardDesignation);
                        {
                            N(SyntaxKind.UnderscoreToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "4");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "6");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Item1");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "7");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.PositionalPatternClause);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "8");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NestedShortTuplePatterns()
        {
            UsingExpression(
@"e switch {
    {X: var ()} => 1,
    {X: ()} => 2,
    {X: var (x)} => 3,
    {X: (1) _} => 4,
    {X: (1) x} => 5,
    {X: (1) {}} => 6,
    {X: (Item1: 1)} => 7,
    {X: C(1)} => 8
}",
                expectedErrors: new DiagnosticDescription[0]);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword);
                                    N(SyntaxKind.ParenthesizedVariableDesignation);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword);
                                    N(SyntaxKind.ParenthesizedVariableDesignation);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.ConstantPattern);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.DiscardDesignation);
                                    {
                                        N(SyntaxKind.UnderscoreToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "4");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.ConstantPattern);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.ConstantPattern);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "6");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.NameColon);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Item1");
                                                }
                                                N(SyntaxKind.ColonToken);
                                            }
                                            N(SyntaxKind.ConstantPattern);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "7");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.PositionalPatternClause);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Subpattern);
                                        {
                                            N(SyntaxKind.ConstantPattern);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "8");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void IsNullableArray01()
        {
            // OK, this means `(o is A[]) ? b : c` because nullable types are not permitted for a pattern's type
            UsingExpression("o is A[] ? b : c");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
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
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact]
        public void IsNullableArray02()
        {
            // error: 'cannot use nullable reference type for a pattern' or 'expected :'
            UsingExpression("o is A[] ? b && c",
                // (1,18): error CS1003: Syntax error, ':' expected
                // o is A[] ? b && c
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 18),
                // (1,18): error CS1733: Expected expression
                // o is A[] ? b && c
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 18)
                );
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
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
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.LogicalAndExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.AmpersandAmpersandToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                M(SyntaxKind.ColonToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void IsNullableArray03()
        {
            // OK, this means `(o is A[][]) ? b : c`
            UsingExpression("o is A[][] ? b : c");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
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
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact]
        public void IsNullableType01()
        {
            UsingExpression("o is A ? b : c");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact]
        public void IsNullableType02()
        {
            UsingExpression("o is A? ? b : c");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact, WorkItem(32161, "https://github.com/dotnet/roslyn/issues/32161")]
        public void ParenthesizedSwitchCase()
        {
            var text = @"
switch (e)
{
    case (0): break;
    case (-1): break;
    case (+2): break;
    case (~3): break;
}
";
            foreach (var langVersion in new[] { LanguageVersion.CSharp6, LanguageVersion.CSharp7, LanguageVersion.CSharp8 })
            {
                UsingStatement(text, options: CSharpParseOptions.Default.WithLanguageVersion(langVersion));
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
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.BreakStatement);
                        {
                            N(SyntaxKind.BreakKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.SwitchSection);
                    {
                        N(SyntaxKind.CaseSwitchLabel);
                        {
                            N(SyntaxKind.CaseKeyword);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.UnaryMinusExpression);
                                {
                                    N(SyntaxKind.MinusToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.BreakStatement);
                        {
                            N(SyntaxKind.BreakKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.SwitchSection);
                    {
                        N(SyntaxKind.CaseSwitchLabel);
                        {
                            N(SyntaxKind.CaseKeyword);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.UnaryPlusExpression);
                                {
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.BreakStatement);
                        {
                            N(SyntaxKind.BreakKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.SwitchSection);
                    {
                        N(SyntaxKind.CaseSwitchLabel);
                        {
                            N(SyntaxKind.CaseKeyword);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.BitwiseNotExpression);
                                {
                                    N(SyntaxKind.TildeToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "3");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
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
        }

        [Fact]
        public void TrailingCommaInSwitchExpression_01()
        {
            UsingExpression("1 switch { 1 => 2, }");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void TrailingCommaInSwitchExpression_02()
        {
            UsingExpression("1 switch { , }",
                // (1,12): error CS8504: Pattern missing
                // 1 switch { , }
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, '=>' expected
                // 1 switch { , }
                Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(1, 12),
                // (1,12): error CS1525: Invalid expression term ','
                // 1 switch { , }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 12)
                );
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.SwitchExpressionArm);
                {
                    M(SyntaxKind.ConstantPattern);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.EqualsGreaterThanToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void TrailingCommaInPropertyPattern_01()
        {
            UsingExpression("e is { X: 3, }");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void TrailingCommaInPropertyPattern_02()
        {
            UsingExpression("e is { , }",
                // (1,8): error CS8504: Pattern missing
                // e is { , }
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 8)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.Subpattern);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void TrailingCommaInPositionalPattern_01()
        {
            UsingExpression("e is ( X: 3, )",
                // (1,14): error CS8504: Pattern missing
                // e is ( X: 3, )
                Diagnostic(ErrorCode.ERR_MissingPattern, ")").WithLocation(1, 14)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Subpattern);
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
                        M(SyntaxKind.Subpattern);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void TrailingCommaInPositionalPattern_02()
        {
            UsingExpression("e is ( , )",
                // (1,8): error CS8504: Pattern missing
                // e is ( , )
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 8),
                // (1,10): error CS8504: Pattern missing
                // e is ( , )
                Diagnostic(ErrorCode.ERR_MissingPattern, ")").WithLocation(1, 10)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.Subpattern);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.Subpattern);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExtraCommaInSwitchExpression()
        {
            UsingExpression("e switch { 1 => 2,, }",
                // (1,19): error CS8504: Pattern missing
                // e switch { 1 => 2,, }
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 19),
                // (1,19): error CS1003: Syntax error, '=>' expected
                // e switch { 1 => 2,, }
                Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(1, 19),
                // (1,19): error CS1525: Invalid expression term ','
                // e switch { 1 => 2,, }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 19)
                );
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.SwitchExpressionArm);
                {
                    M(SyntaxKind.ConstantPattern);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.EqualsGreaterThanToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ExtraCommaInPropertyPattern()
        {
            UsingExpression("e is { A: 1,, }",
                // (1,13): error CS8504: Pattern missing
                // e is { A: 1,, }
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 13)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.Subpattern);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(33054, "https://github.com/dotnet/roslyn/issues/33054")]
        public void ParenthesizedExpressionInPattern_01()
        {
            UsingStatement(
@"switch (e) {
    case (('C') << 24) + (('g') << 16) + (('B') << 8) + 'I': break;
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
                        N(SyntaxKind.AddExpression);
                        {
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.AddExpression);
                                {
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.LeftShiftExpression);
                                        {
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CharacterLiteralExpression);
                                                {
                                                    N(SyntaxKind.CharacterLiteralToken);
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                            N(SyntaxKind.LessThanLessThanToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "24");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.LeftShiftExpression);
                                        {
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CharacterLiteralExpression);
                                                {
                                                    N(SyntaxKind.CharacterLiteralToken);
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                            N(SyntaxKind.LessThanLessThanToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "16");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.LeftShiftExpression);
                                    {
                                        N(SyntaxKind.ParenthesizedExpression);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CharacterLiteralExpression);
                                            {
                                                N(SyntaxKind.CharacterLiteralToken);
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.LessThanLessThanToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "8");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.PlusToken);
                            N(SyntaxKind.CharacterLiteralExpression);
                            {
                                N(SyntaxKind.CharacterLiteralToken);
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

        [Fact, WorkItem(33208, "https://github.com/dotnet/roslyn/issues/33208")]
        public void ParenthesizedExpressionInPattern_02()
        {
            UsingStatement(
@"switch (e) {
    case ((2) + (2)): break;
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
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(33208, "https://github.com/dotnet/roslyn/issues/33208")]
        public void ParenthesizedExpressionInPattern_03()
        {
            UsingStatement(
@"switch (e) {
    case ((2 + 2) - 2): break;
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
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SubtractExpression);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.AddExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                        N(SyntaxKind.PlusToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.MinusToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(33208, "https://github.com/dotnet/roslyn/issues/33208")]
        public void ParenthesizedExpressionInPattern_04()
        {
            UsingStatement(
@"switch (e) {
    case (2) | (2): break;
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
                        N(SyntaxKind.BitwiseOrExpression);
                        {
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.BarToken);
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                                N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(33208, "https://github.com/dotnet/roslyn/issues/33208")]
        public void ParenthesizedExpressionInPattern_05()
        {
            UsingStatement(
@"switch (e) {
    case ((2 << 2) | 2): break;
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
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.BitwiseOrExpression);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.LeftShiftExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                        N(SyntaxKind.LessThanLessThanToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.BarToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
        public void ChainedSwitchExpression_01()
        {
            UsingExpression("1 switch { 1 => 2 } switch { 2 => 3 }");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SwitchExpressionArm);
                    {
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ChainedSwitchExpression_02()
        {
            UsingExpression("a < b switch { 1 => 2 } < c switch { 2 => 3 }");
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.LessThanExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SwitchExpressionArm);
                    {
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_01()
        {
            UsingExpression("a < b switch { true => 1 }");
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SwitchExpressionArm);
                    {
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_02()
        {
            // The left-hand-side of a switch is equality, which binds more loosely than the `switch`,
            // so `b` ends up on the left of the `switch` and the `a ==` expression has a switch on the right.
            UsingExpression("a == b switch { true => 1 }");
            N(SyntaxKind.EqualsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.EqualsEqualsToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SwitchExpressionArm);
                    {
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_03()
        {
            UsingExpression("a * b switch {}");
            N(SyntaxKind.MultiplyExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_04()
        {
            UsingExpression("a + b switch {}");
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_05()
        {
            UsingExpression("-a switch {}");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.UnaryMinusExpression);
                {
                    N(SyntaxKind.MinusToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_06()
        {
            UsingExpression("(Type)a switch {}");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.CastExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_07()
        {
            UsingExpression("(Type)a++ switch {}");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.CastExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.PostIncrementExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.PlusPlusToken);
                    }
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_08()
        {
            UsingExpression("+a switch {}");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.UnaryPlusExpression);
                {
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_09()
        {
            UsingExpression("a switch {}.X",
                // (1,1): error CS1073: Unexpected token '.'
                // a switch {}.X
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "a switch {}").WithArguments(".").WithLocation(1, 1));
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_10()
        {
            UsingExpression("a switch {}[i]",
                // (1,1): error CS1073: Unexpected token '['
                // a switch {}[i]
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "a switch {}").WithArguments("[").WithLocation(1, 1));
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_11()
        {
            UsingExpression("a switch {}(b)",
                // (1,1): error CS1073: Unexpected token '('
                // a switch {}(b)
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "a switch {}").WithArguments("(").WithLocation(1, 1));
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpressionPrecedence_12()
        {
            UsingExpression("a switch {}!",
                // (1,1): error CS1073: Unexpected token '!'
                // a switch {}!
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "a switch {}").WithArguments("!").WithLocation(1, 1));
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(32749, "https://github.com/dotnet/roslyn/issues/32749")]
        public void BrokenSwitchExpression_01()
        {
            UsingExpression("(e switch {)",
                // (1,12): error CS1513: } expected
                // (e switch {)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(1, 12)
                );
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(32749, "https://github.com/dotnet/roslyn/issues/32749")]
        public void BrokenSwitchExpression_02()
        {
            UsingExpression("(e switch {,)",
                // (1,12): error CS8504: Pattern missing
                // (e switch {,)
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 12),
                // (1,12): error CS1003: Syntax error, '=>' expected
                // (e switch {,)
                Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(1, 12),
                // (1,12): error CS1525: Invalid expression term ','
                // (e switch {,)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 12),
                // (1,13): error CS1513: } expected
                // (e switch {,)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(1, 13)
                );
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.SwitchExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.SwitchExpressionArm);
                    {
                        M(SyntaxKind.ConstantPattern);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.EqualsGreaterThanToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact, WorkItem(32749, "https://github.com/dotnet/roslyn/issues/32749")]
        public void BrokenSwitchExpression_03()
        {
            UsingExpression("e switch {,",
                // (1,11): error CS8504: Pattern missing
                // e switch {,
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, '=>' expected
                // e switch {,
                Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(1, 11),
                // (1,11): error CS1525: Invalid expression term ','
                // e switch {,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 11),
                // (1,12): error CS1513: } expected
                // e switch {,
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 12)
                );
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.SwitchExpressionArm);
                {
                    M(SyntaxKind.ConstantPattern);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.EqualsGreaterThanToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(33675, "https://github.com/dotnet/roslyn/issues/33675")]
        public void ParenthesizedNamedConstantPatternInSwitchExpression()
        {
            UsingExpression("e switch { (X) => 1 }");
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(34482, "https://github.com/dotnet/roslyn/issues/34482")]
        public void SwitchCaseArmErrorRecovery_01()
        {
            UsingExpression("e switch { 1 => 1; 2 => 2 }",
                // (1,18): error CS1003: Syntax error, ',' expected
                // e switch { 1 => 1; 2 => 2 }
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 18)
                );
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(34482, "https://github.com/dotnet/roslyn/issues/34482")]
        public void SwitchCaseArmErrorRecovery_02()
        {
            UsingExpression("e switch { 1 => 1, 2 => 2; }",
                // (1,26): error CS1003: Syntax error, ',' expected
                // e switch { 1 => 1, 2 => 2; }
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 26)
                );
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(38121, "https://github.com/dotnet/roslyn/issues/38121")]
        public void GenericPropertyPattern()
        {
            UsingExpression("e is A<B> {}");
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void PrecedenceInversionWithDeclarationPattern()
        {
            UsingExpression("o is C c + d",
                // (1,10): error CS1073: Unexpected token '+'
                // o is C c + d
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "+").WithArguments("+").WithLocation(1, 10)
                );
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            EOF();
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void PrecedenceInversionWithRecursivePattern()
        {
            UsingExpression("o is {} + d",
                // (1,9): error CS1073: Unexpected token '+'
                // o is {} + d
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "+").WithArguments("+").WithLocation(1, 9)
                );
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            EOF();
        }

        [Fact]
        public void PatternCombinators_01()
        {
            var test = "_ = e is a or b;";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,26): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { _ = e is a or b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 26),
                // (1,31): error CS0103: The name 'a' does not exist in the current context
                // class C { void M() { _ = e is a or b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(1, 31),
                // (1,33): error CS8400: Feature 'or pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                // class C { void M() { _ = e is a or b; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "or").WithArguments("or pattern", "9.0").WithLocation(1, 33),
                // (1,36): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { _ = e is a or b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 36));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
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
        public void PatternCombinators_02()
        {
            var test = "_ = e is a and b;";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,26): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { _ = e is a and b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 26),
                // (1,31): error CS0103: The name 'a' does not exist in the current context
                // class C { void M() { _ = e is a and b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(1, 31),
                // (1,33): error CS8400: Feature 'and pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                // class C { void M() { _ = e is a and b; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "and").WithArguments("and pattern", "9.0").WithLocation(1, 33),
                // (1,37): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { _ = e is a and b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 37));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
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
        public void PatternCombinators_03()
        {
            var test = "_ = e is not b;";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,26): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { _ = e is not b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 26),
                // (1,31): error CS8400: Feature 'not pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                // class C { void M() { _ = e is not b; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "not").WithArguments("not pattern", "9.0").WithLocation(1, 31),
                // (1,35): error CS0103: The name 'b' does not exist in the current context
                // class C { void M() { _ = e is not b; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(1, 35));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
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
        public void PatternCombinators_04()
        {
            var test = "_ = e is not null;";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,26): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { _ = e is not null; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 26),
                // (1,31): error CS8400: Feature 'not pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                // class C { void M() { _ = e is not null; } }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "not").WithArguments("not pattern", "9.0").WithLocation(1, 31));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
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
        public void PatternCombinators_05()
        {
            var test = @"_ = e switch {
    a or b => 1,
    c and d => 2,
    not e => 3,
    not null => 4,
};";
            var testInMethod = @$"class C {{ void M() {{ {test} }} }}";

            CreateCompilation(testInMethod, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,26): error CS0103: The name 'e' does not exist in the current context
                // class C { void M() { _ = e switch {
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 26),
                // (2,5): error CS0103: The name 'a' does not exist in the current context
                //     a or b => 1,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(2, 5),
                // (2,7): error CS8400: Feature 'or pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     a or b => 1,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "or").WithArguments("or pattern", "9.0").WithLocation(2, 7),
                // (2,10): error CS0103: The name 'b' does not exist in the current context
                //     a or b => 1,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(2, 10),
                // (3,5): error CS0103: The name 'c' does not exist in the current context
                //     c and d => 2,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(3, 5),
                // (3,7): error CS8400: Feature 'and pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     c and d => 2,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "and").WithArguments("and pattern", "9.0").WithLocation(3, 7),
                // (3,11): error CS0103: The name 'd' does not exist in the current context
                //     c and d => 2,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(3, 11),
                // (4,5): error CS8400: Feature 'not pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     not e => 3,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "not").WithArguments("not pattern", "9.0").WithLocation(4, 5),
                // (4,9): error CS0103: The name 'e' does not exist in the current context
                //     not e => 3,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(4, 9),
                // (5,5): error CS8400: Feature 'not pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     not null => 4,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "not").WithArguments("not pattern", "9.0").WithLocation(5, 5));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.AndPattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.AndKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "d");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.NotPattern);
                            {
                                N(SyntaxKind.NotKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "3");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.NotPattern);
                            {
                                N(SyntaxKind.NotKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NullLiteralExpression);
                                    {
                                        N(SyntaxKind.NullKeyword);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "4");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPattern_01()
        {
            var test = @"_ = e switch {
    < 0 => 0,
    <= 1 => 1,
    > 2 => 2,
    >= 3 => 3,
    == 4 => 4,
    != 5 => 5,
};";
            CreateCompilation(test, parseOptions: TestOptions.RegularWithoutPatternCombinators).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // _ = e switch {
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, @"_ = e switch {
    < 0 => 0,
    <= 1 => 1,
    > 2 => 2,
    >= 3 => 3,
    == 4 => 4,
    != 5 => 5,
};").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,5): error CS0103: The name 'e' does not exist in the current context
                // _ = e switch {
                Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(1, 5),
                // (2,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     < 0 => 0,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "<").WithArguments("relational pattern", "9.0").WithLocation(2, 5),
                // (3,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     <= 1 => 1,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "<=").WithArguments("relational pattern", "9.0").WithLocation(3, 5),
                // (4,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     > 2 => 2,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, ">").WithArguments("relational pattern", "9.0").WithLocation(4, 5),
                // (5,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     >= 3 => 3,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, ">=").WithArguments("relational pattern", "9.0").WithLocation(5, 5),
                // (6,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     == 4 => 4,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "==").WithArguments("relational pattern", "9.0").WithLocation(6, 5),
                // (6,5): error CS1525: Invalid expression term '=='
                //     == 4 => 4,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "==").WithArguments("==").WithLocation(6, 5),
                // (7,5): error CS8400: Feature 'relational pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     != 5 => 5,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "!=").WithArguments("relational pattern", "9.0").WithLocation(7, 5),
                // (7,5): error CS1525: Invalid expression term '!='
                //     != 5 => 5,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "!=").WithArguments("!=").WithLocation(7, 5));

            UsingStatement(test, TestOptions.RegularWithoutPatternCombinators);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "3");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.EqualsEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "4");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.ExclamationEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "5");
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPatternPrecedence_01()
        {
            UsingStatement(
@"_ = e switch {
    < 0 < 0 => 0,
    == 4 < 4 => 4,
    != 5 < 5 => 5,
};",
                TestOptions.RegularWithPatternCombinators,
                // (2,9): error CS1003: Syntax error, '=>' expected
                //     < 0 < 0 => 0,
                Diagnostic(ErrorCode.ERR_SyntaxError, "<").WithArguments("=>").WithLocation(2, 9),
                // (2,9): error CS1525: Invalid expression term '<'
                //     < 0 < 0 => 0,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(2, 9),
                // (2,13): error CS1003: Syntax error, ',' expected
                //     < 0 < 0 => 0,
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(2, 13),
                // (2,13): error CS8504: Pattern missing
                //     < 0 < 0 => 0,
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(2, 13),
                // (3,10): error CS1003: Syntax error, '=>' expected
                //     == 4 < 4 => 4,
                Diagnostic(ErrorCode.ERR_SyntaxError, "<").WithArguments("=>").WithLocation(3, 10),
                // (3,10): error CS1525: Invalid expression term '<'
                //     == 4 < 4 => 4,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(3, 10),
                // (3,14): error CS1003: Syntax error, ',' expected
                //     == 4 < 4 => 4,
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(3, 14),
                // (3,14): error CS8504: Pattern missing
                //     == 4 < 4 => 4,
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(3, 14),
                // (4,10): error CS1003: Syntax error, '=>' expected
                //     != 5 < 5 => 5,
                Diagnostic(ErrorCode.ERR_SyntaxError, "<").WithArguments("=>").WithLocation(4, 10),
                // (4,10): error CS1525: Invalid expression term '<'
                //     != 5 < 5 => 5,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(4, 10),
                // (4,14): error CS1003: Syntax error, ',' expected
                //     != 5 < 5 => 5,
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(4, 14),
                // (4,14): error CS8504: Pattern missing
                //     != 5 < 5 => 5,
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(4, 14)
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            M(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.LessThanExpression);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.EqualsEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                            }
                            M(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.LessThanExpression);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "4");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.ExclamationEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "5");
                                }
                            }
                            M(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.LessThanExpression);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "5");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            M(SyntaxKind.ConstantPattern);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPatternPrecedence_02()
        {
            UsingStatement(
@"_ = e switch {
    < 0 << 0 => 0,
    == 4 << 4 => 4,
    != 5 << 5 => 5,
};",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.LeftShiftExpression);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                    N(SyntaxKind.LessThanLessThanToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.EqualsEqualsToken);
                                N(SyntaxKind.LeftShiftExpression);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "4");
                                    }
                                    N(SyntaxKind.LessThanLessThanToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "4");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "4");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.ExclamationEqualsToken);
                                N(SyntaxKind.LeftShiftExpression);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "5");
                                    }
                                    N(SyntaxKind.LessThanLessThanToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "5");
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPatternPrecedence_03()
        {
            UsingStatement(
@"_ = e is < 4;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RelationalPattern);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "4");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPatternPrecedence_04()
        {
            UsingStatement(
@"_ = e is < 4 < 4;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.LessThanExpression);
                    {
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                            }
                        }
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "4");
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RelationalPatternPrecedence_05()
        {
            UsingStatement(
@"_ = e is < 4 << 4;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RelationalPattern);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.LeftShiftExpression);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                                N(SyntaxKind.LessThanLessThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
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
        public void WhenIsNotKeywordInIsExpression()
        {
            UsingStatement(@"_ = e is T when;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "when");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void WhenIsNotKeywordInRecursivePattern()
        {
            UsingStatement(@"_ = e switch { T(X when) => 1, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.DeclarationPattern);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "X");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "when");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypePattern_01()
        {
            UsingStatement(@"_ = e is int or long;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.LongKeyword);
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
        public void TypePattern_02()
        {
            UsingStatement(@"_ = e is int or System.Int64;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "System");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int64");
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
        public void TypePattern_03()
        {
            UsingStatement(@"_ = e switch { int or long => 1, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.LongKeyword);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypePattern_04()
        {
            UsingStatement(@"_ = e switch { int or System.Int64 => 1, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Int64");
                                        }
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypePattern_05()
        {
            UsingStatement(@"_ = e switch { T(int) => 1, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.TypePattern);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TypePattern_06()
        {
            UsingStatement(@"_ = e switch { int => 1, long => 2, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.LongKeyword);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(49354, "https://github.com/dotnet/roslyn/issues/49354")]
        public void TypePattern_07()
        {
            UsingStatement(@"_ = e is (int) or string;",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
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
        public void TypePattern_08()
        {
            UsingStatement($"_ = e is (a) or b;",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
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
        public void CompoundPattern_01()
        {
            UsingStatement(@"bool isLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.LocalFunctionStatement);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.BoolKeyword);
                }
                N(SyntaxKind.IdentifierToken, "isLetter");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.CharKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ArrowExpressionClause);
                {
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.AndPattern);
                            {
                                N(SyntaxKind.RelationalPattern);
                                {
                                    N(SyntaxKind.GreaterThanEqualsToken);
                                    N(SyntaxKind.CharacterLiteralExpression);
                                    {
                                        N(SyntaxKind.CharacterLiteralToken);
                                    }
                                }
                                N(SyntaxKind.AndKeyword);
                                N(SyntaxKind.RelationalPattern);
                                {
                                    N(SyntaxKind.LessThanEqualsToken);
                                    N(SyntaxKind.CharacterLiteralExpression);
                                    {
                                        N(SyntaxKind.CharacterLiteralToken);
                                    }
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.AndPattern);
                            {
                                N(SyntaxKind.RelationalPattern);
                                {
                                    N(SyntaxKind.GreaterThanEqualsToken);
                                    N(SyntaxKind.CharacterLiteralExpression);
                                    {
                                        N(SyntaxKind.CharacterLiteralToken);
                                    }
                                }
                                N(SyntaxKind.AndKeyword);
                                N(SyntaxKind.RelationalPattern);
                                {
                                    N(SyntaxKind.LessThanEqualsToken);
                                    N(SyntaxKind.CharacterLiteralExpression);
                                    {
                                        N(SyntaxKind.CharacterLiteralToken);
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
        public void CombinatorAsDesignator_01()
        {
            UsingStatement(@"_ = e is int and;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "and");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void CombinatorAsDesignator_02()
        {
            UsingStatement(@"_ = e is int and < Z;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.LessThanToken);
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

        [Fact]
        public void CombinatorAsDesignator_03()
        {
            UsingStatement(@"_ = e is int and && b;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.LogicalAndExpression);
                    {
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "and");
                                }
                            }
                        }
                        N(SyntaxKind.AmpersandAmpersandToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void CombinatorAsDesignator_04()
        {
            UsingStatement(@"_ = e is int and int.MaxValue;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "MaxValue");
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
        public void CombinatorAsDesignator_05()
        {
            UsingStatement(@"_ = e is int and MaxValue;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MaxValue");
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
        public void CombinatorAsDesignator_06()
        {
            UsingStatement(@"_ = e is int and ?? Z;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "and");
                                }
                            }
                        }
                        N(SyntaxKind.QuestionQuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Z");
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void CombinatorAsDesignator_07()
        {
            UsingStatement(@"_ = e is int and ? a : b;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.DeclarationPattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "and");
                                }
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void PrecedenceInversionWithTypeTest()
        {
            UsingExpression("o is int + d",
                // (1,6): error CS1525: Invalid expression term 'int'
                // o is int + d
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 6)
                );
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.AddExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.PlusToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void PrecedenceInversionWithBlockLambda()
        {
            UsingExpression("() => {} + d",
                // (1,10): warning CS8848: Operator '+' cannot be used here due to precedence. Use parentheses to disambiguate.
                // () => {} + d
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "+").WithArguments("+").WithLocation(1, 10)
                );
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            EOF();
        }

        [Fact, WorkItem(10492, "https://github.com/dotnet/roslyn/issues/10492")]
        public void PrecedenceInversionWithAnonymousMethod()
        {
            UsingExpression("delegate {} + d",
                // (1,13): warning CS8848: Operator '+' cannot be used here due to precedence. Use parentheses to disambiguate.
                // delegate {} + d
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "+").WithArguments("+").WithLocation(1, 13)
                );
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.AnonymousMethodExpression);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_01()
        {
            UsingStatement(@"_ = e is (3);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_02()
        {
            UsingStatement(@"_ = e is (A);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_03()
        {
            UsingStatement(@"_ = e is (int);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_04()
        {
            UsingStatement(@"_ = e is (Item1: int);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Item1");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.TypePattern);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_05()
        {
            UsingStatement(@"_ = e is (A) x;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.CastExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void OneElementPositional_06()
        {
            UsingStatement(@"_ = e is ((A, A)) x;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.PositionalPatternClause);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Subpattern);
                                            {
                                                N(SyntaxKind.ConstantPattern);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "A");
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.Subpattern);
                                            {
                                                N(SyntaxKind.ConstantPattern);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "A");
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void ZeroElementPositional_01()
        {
            UsingStatement(@"_ = e is ();",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void ZeroElementPositional_02()
        {
            UsingStatement(@"_ = e is () x;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36515")]
        public void ZeroElementPositional_03()
        {
            UsingStatement(@"_ = e is () {};",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void CastExpressionInPattern_01()
        {
            UsingStatement(@"_ = e is (int)+1;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.CastExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.UnaryPlusExpression);
                                {
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
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

        [Theory]
        [InlineData("or")]
        [InlineData("and")]
        [InlineData("not")]
        public void CastExpressionInPattern_02(string identifier)
        {
            UsingStatement($"_ = e is (int){identifier};",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.CastExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, identifier);
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void CastExpressionInPattern_03(
            [CombinatorialValues("and", "or")] string left,
            [CombinatorialValues(SyntaxKind.AndKeyword, SyntaxKind.OrKeyword)] SyntaxKind opKind,
            [CombinatorialValues("and", "or")] string right)
        {
            UsingStatement($"_ = e is (int){left} {SyntaxFacts.GetText(opKind)} {right};",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(opKind == SyntaxKind.AndKeyword ? SyntaxKind.AndPattern : SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.CastExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, left);
                                    }
                                }
                            }
                            N(opKind);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, right);
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
        public void CastExpressionInPattern_04()
        {
            UsingStatement($"_ = e is (a)42 or b;",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.CastExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "42");
                                    }
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void CombinatorAsConstant_00(
            [CombinatorialValues("and", "or")] string left,
            [CombinatorialValues(SyntaxKind.AndKeyword, SyntaxKind.OrKeyword)] SyntaxKind opKind,
            [CombinatorialValues("and", "or")] string right)
        {
            UsingStatement($"_ = e is {left} {SyntaxFacts.GetText(opKind)} {right};",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(opKind == SyntaxKind.AndKeyword ? SyntaxKind.AndPattern : SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, left);
                                }
                            }
                            N(opKind);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, right);
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void CombinatorAsConstant_01(
            [CombinatorialValues(SyntaxKind.AndKeyword, SyntaxKind.OrKeyword)] SyntaxKind opKind,
            [CombinatorialValues("and", "or")] string right)
        {
            UsingStatement($"_ = e is (int) {SyntaxFacts.GetText(opKind)} {right};",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(opKind == SyntaxKind.AndKeyword ? SyntaxKind.AndPattern : SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(opKind);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, right);
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
        public void CombinatorAsConstant_02()
        {
            UsingStatement($"_ = e is (int) or >= 0;",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
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
        public void CombinatorAsConstant_03()
        {
            UsingStatement($"_ = e is (int)or or >= 0;",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.CastExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "or");
                                    }
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
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
        public void CombinatorAsConstant_04()
        {
            UsingStatement($"_ = e is (int) or or or >= 0;",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.ParenthesizedPattern);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TypePattern);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "or");
                                    }
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
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
        public void ConjunctiveFollowedByPropertyPattern_01()
        {
            UsingStatement(@"switch (e) { case {} and {}: break; }",
                TestOptions.RegularWithPatternCombinators
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
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
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
        public void ConjunctiveFollowedByTuplePattern_01()
        {
            UsingStatement(@"switch (e) { case {} and (): break; }",
                TestOptions.RegularWithPatternCombinators
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
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
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
        [WorkItem(42107, "https://github.com/dotnet/roslyn/issues/42107")]
        public void ParenthesizedRelationalPattern_01()
        {
            UsingStatement(@"_ = e is (>= 1);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.RelationalPattern);
                            {
                                N(SyntaxKind.GreaterThanEqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(42107, "https://github.com/dotnet/roslyn/issues/42107")]
        public void ParenthesizedRelationalPattern_02()
        {
            UsingStatement(@"_ = e switch { (>= 1) => 1 };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.RelationalPattern);
                                {
                                    N(SyntaxKind.GreaterThanEqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(42107, "https://github.com/dotnet/roslyn/issues/42107")]
        public void ParenthesizedRelationalPattern_03()
        {
            UsingStatement(@"bool isAsciiLetter(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.LocalFunctionStatement);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.BoolKeyword);
                }
                N(SyntaxKind.IdentifierToken, "isAsciiLetter");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.CharKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ArrowExpressionClause);
                {
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.AndPattern);
                                {
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.GreaterThanEqualsToken);
                                        N(SyntaxKind.CharacterLiteralExpression);
                                        {
                                            N(SyntaxKind.CharacterLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.AndKeyword);
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.LessThanEqualsToken);
                                        N(SyntaxKind.CharacterLiteralExpression);
                                        {
                                            N(SyntaxKind.CharacterLiteralToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.AndPattern);
                                {
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.GreaterThanEqualsToken);
                                        N(SyntaxKind.CharacterLiteralExpression);
                                        {
                                            N(SyntaxKind.CharacterLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.AndKeyword);
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.LessThanEqualsToken);
                                        N(SyntaxKind.CharacterLiteralExpression);
                                        {
                                            N(SyntaxKind.CharacterLiteralToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(42107, "https://github.com/dotnet/roslyn/issues/42107")]
        public void ParenthesizedRelationalPattern_04()
        {
            UsingStatement(@"_ = e is (<= 1, >= 2);",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PositionalPatternClause);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.LessThanEqualsToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.RelationalPattern);
                                    {
                                        N(SyntaxKind.GreaterThanEqualsToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void AndPatternAssociativity_01()
        {
            UsingStatement(@"_ = e is A and B and C;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.AndPattern);
                        {
                            N(SyntaxKind.AndPattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.AndKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                            }
                            N(SyntaxKind.AndKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
        public void OrPatternAssociativity_01()
        {
            UsingStatement(@"_ = e is A or B or C;",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.OrKeyword);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(43960, "https://github.com/dotnet/roslyn/issues/43960")]
        public void NamespaceQualifiedEnumConstantInSwitchCase()
        {
            var source = @"switch (e) { case global::E.A: break; }";
            UsingStatement(source,
                TestOptions.RegularWithPatternCombinators
                );
            verifyTree();
            UsingStatement(source,
                TestOptions.RegularWithoutPatternCombinators
                );
            verifyTree();

            void verifyTree()
            {
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
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.AliasQualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.GlobalKeyword);
                                    }
                                    N(SyntaxKind.ColonColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "E");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
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
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedEnumConstantInIsPattern()
        {
            var source = @"_ = e is global::E.A;";
            UsingStatement(source,
                TestOptions.RegularWithPatternCombinators
                );
            verifyTree();
            UsingStatement(source,
                TestOptions.RegularWithoutPatternCombinators
                );
            verifyTree();

            void verifyTree()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IsExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.AliasQualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.GlobalKeyword);
                                    }
                                    N(SyntaxKind.ColonColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "E");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(45757, "https://github.com/dotnet/roslyn/issues/45757")]
        public void IncompleteTuplePatternInPropertySubpattern()
        {
            var source = @"_ = this is Program { P1: (1,  }";
            var expectedErrors = new[]
            {
                // (1,32): error CS8504: Pattern missing
                // _ = this is Program { P1: (1,  }
                Diagnostic(ErrorCode.ERR_MissingPattern, "}").WithLocation(1, 32),
                // (1,32): error CS1026: ) expected
                // _ = this is Program { P1: (1,  }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 32),
                // (1,33): error CS1002: ; expected
                // _ = this is Program { P1: (1,  }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 33)
            };
            UsingStatement(source,
                TestOptions.RegularWithPatternCombinators,
                expectedErrors
                );
            verifyTree();
            UsingStatement(source,
                TestOptions.RegularWithoutPatternCombinators,
                expectedErrors
                );
            verifyTree();

            void verifyTree()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.ThisExpression);
                            {
                                N(SyntaxKind.ThisKeyword);
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Program");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "P1");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.RecursivePattern);
                                        {
                                            N(SyntaxKind.PositionalPatternClause);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Subpattern);
                                                {
                                                    N(SyntaxKind.ConstantPattern);
                                                    {
                                                        N(SyntaxKind.NumericLiteralExpression);
                                                        {
                                                            N(SyntaxKind.NumericLiteralToken, "1");
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                M(SyntaxKind.Subpattern);
                                                {
                                                    M(SyntaxKind.ConstantPattern);
                                                    {
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                    }
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(45757, "https://github.com/dotnet/roslyn/issues/45757")]
        public void IncompleteTuplePattern()
        {
            var source = @"_ = i is (1,   }";
            var expectedErrors = new[]
            {
                // (1,1): error CS1073: Unexpected token '}'
                // _ = i is (1,   }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = i is (1,   ").WithArguments("}").WithLocation(1, 1),
                // (1,16): error CS8504: Pattern missing
                // _ = i is (1,   }
                Diagnostic(ErrorCode.ERR_MissingPattern, "}").WithLocation(1, 16),
                // (1,16): error CS1026: ) expected
                // _ = i is (1,   }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(1, 16),
                // (1,16): error CS1002: ; expected
                // _ = i is (1,   }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 16)
            };
            UsingStatement(source,
                TestOptions.RegularWithPatternCombinators,
                expectedErrors
                );
            verifyTree();
            UsingStatement(source,
                TestOptions.RegularWithoutPatternCombinators,
                expectedErrors
                );
            verifyTree();

            void verifyTree()
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IsPatternExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.IsKeyword);
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PositionalPatternClause);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.ConstantPattern);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Subpattern);
                                    {
                                        M(SyntaxKind.ConstantPattern);
                                        {
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(47614, "https://github.com/dotnet/roslyn/issues/47614")]
        public void GenericTypeAsTypePatternInSwitchExpression()
        {
            UsingStatement(@"_ = e switch { List<X> => 1, List<Y> => 2, };",
                TestOptions.RegularWithPatternCombinators
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "X");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "List");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "2");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchExpression_PredefinedType()
        {
            UsingStatement(@"_ = e switch { int? => 1 };",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchStatement_PredefinedType()
        {
            UsingStatement(@"switch(a) { case int?: break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
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

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchExpression_PredefinedType_Parenthesized()
        {
            UsingStatement(@"_ = e switch { (int?) => 1 };",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchStatement_PredefinedType_Parenthesized()
        {
            UsingStatement(@"switch(a) { case (int?): break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchExpression()
        {
            UsingStatement(@"_ = e switch { a? => 1 };",
                TestOptions.RegularWithPatternCombinators
            );

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchStatement()
        {
            UsingStatement(@"switch(a) { case a?: break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
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

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchExpression_Parenthesized()
        {
            UsingStatement(@"_ = e switch { (a?) => 1 };",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.ParenthesizedPattern);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypePattern);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(48112, "https://github.com/dotnet/roslyn/issues/48112")]
        public void NullableTypeAsTypePatternInSwitchStatement_Parenthesized()
        {
            UsingStatement(@"switch(a) { case (a?): break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TypePattern);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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
        public void ConditionalAsConstantPatternInSwitchExpression()
        {
            UsingStatement(@"_ = e switch { (a?x:y) => 1 };",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.SwitchExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.SwitchKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SwitchExpressionArm);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.ParenthesizedExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.ConditionalExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                        N(SyntaxKind.QuestionToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ConditionalAsConstantPatternInSwitchStatement()
        {
            UsingStatement(@"switch(a) { case a?x:y: break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
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
        public void ConditionalAsConstantPatternInSwitchStatement_Parenthesized()
        {
            UsingStatement(@"switch(a) { case (a?x:y): break; }",
                TestOptions.RegularWithPatternCombinators
            );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
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
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.ColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(53011, "https://github.com/dotnet/roslyn/issues/53011")]
        public void InvalidPropertyPattern()
        {
            UsingExpression(@"new object() is { {}: 1 }", TestOptions.RegularWithPatternCombinators,
                // (1,21): error CS1003: Syntax error, ',' expected
                // new object() is { {}: 1 }
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 21),
                // (1,23): error CS1003: Syntax error, ',' expected
                // new object() is { {}: 1 }
                Diagnostic(ErrorCode.ERR_SyntaxError, "1").WithArguments(",").WithLocation(1, 23));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.ObjectKeyword);
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50220")]
        public void DesignatorBeforePropertyPattern()
        {
            UsingExpression("o is string s { Length: 5 }",
                // (1,13): error CS8525: A variable designator must come after a property pattern.
                // o is string s { Length: 5 }
                Diagnostic(ErrorCode.ERR_DesignatorBeforePropertyPattern, "s").WithLocation(1, 13));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Length");
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
                }
            }
            EOF();
        }
    }
}
