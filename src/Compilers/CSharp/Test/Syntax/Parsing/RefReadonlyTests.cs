// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefReadonlyTests : ParsingTests
    {
        public RefReadonlyTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void RefReadonlyReturn_CSharp7()
        {
            var text = @"
unsafe class Program
{
    delegate ref readonly int D1();

    static ref readonly T M<T>()
    {
        return ref (new T[1])[0];
    }

    public virtual ref readonly int* P1 => throw null;

    public ref readonly int[][] this[int i] => throw null;
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,18): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     delegate ref readonly int D1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "readonly").WithArguments("readonly references", "7.2").WithLocation(4, 18),
                // (6,16): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     static ref readonly T M<T>()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "readonly").WithArguments("readonly references", "7.2").WithLocation(6, 16),
                // (11,24): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public virtual ref readonly int* P1 => throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "readonly").WithArguments("readonly references", "7.2").WithLocation(11, 24),
                // (13,16): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public ref readonly int[][] this[int i] => throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "readonly").WithArguments("readonly references", "7.2").WithLocation(13, 16)
            );
        }

        [Fact]
        public void InArgs_CSharp7()
        {
            var text = @"
class Program
{
    static void M(in int x)
    {
    }

    int this[in int x]
    {
        get
        {
            return 1;
        }
    }

    static void Test1()
    {
        int x = 1;
        M(in x);

        _ = (new Program())[in x];
    }
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,19): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     static void M(in int x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(4, 19),
                // (8,14): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     int this[in int x]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(8, 14),
                // (19,11): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         M(in x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(19, 11),
                // (21,29): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         _ = (new Program())[in x];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(21, 29)
            );
        }

        [Fact]
        public void RefReadonlyReturn_Unexpected()
        {
            var text = @"

class Program
{
    static void Main()
    {
    }

    ref readonly int Field;

    public static ref readonly Program  operator  +(Program x, Program y)
    {
        throw null;
    }

    // this parses fine
    static async ref readonly Task M<T>()
    {
        throw null;
    }

    public ref readonly virtual int* P1 => throw null;

}
";

            ParseAndValidate(text, TestOptions.Regular9,
                // (11,41): error CS1519: Invalid token 'operator' in a member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (11,41): error CS1519: Invalid token 'operator' in a member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (11,74): error CS1001: Identifier expected
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(11, 74),
                // (13,9): error CS1014: A get or set accessor expected
                //         throw null;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "throw").WithLocation(13, 9),
                // (13,19): error CS1014: A get or set accessor expected
                //         throw null;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(13, 19),
                // (22,25): error CS1031: Type expected
                //     public ref readonly virtual int* P1 => throw null;
                Diagnostic(ErrorCode.ERR_TypeExpected, "virtual").WithLocation(22, 25));
        }

        [Fact]
        public void RefReadonlyReturn_UnexpectedBindTime()
        {
            var text = @"

class Program
{
    static void Main()
    {
        ref readonly int local = ref (new int[1])[0];

        (ref readonly int, ref readonly int Alice)? t = null;

        System.Collections.Generic.List<ref readonly int> x = null;

        Use(local);
        Use(t);
        Use(x);
    }

    static void Use<T>(T dummy)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib461(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (9,10): error CS1073: Unexpected token 'ref'
                //         (ref readonly int, ref readonly int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 10),
                // (9,28): error CS1073: Unexpected token 'ref'
                //         (ref readonly int, ref readonly int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 28),
                // (11,41): error CS1073: Unexpected token 'ref'
                //         System.Collections.Generic.List<ref readonly int> x = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(11, 41));
        }

        [Fact]
        public void RefReadOnlyLocalsAreDisallowed()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        int value = 0;
        ref int valid = ref value;
        ref readonly int invalid = ref readonly value;
    }
}").GetParseDiagnostics().Verify(
                // (8,40): error CS1525: Invalid expression term 'readonly'
                //         ref readonly int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(8, 40),
                // (8,40): error CS1002: ; expected
                //         ref readonly int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(8, 40),
                // (8,40): error CS0106: The modifier 'readonly' is not valid for this item
                //         ref readonly int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(8, 40),
                // (8,54): error CS1001: Identifier expected
                //         ref readonly int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(8, 54));
        }

        [Fact]
        public void LocalsWithRefReadOnlyExpressionsAreDisallowed()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        int value = 0;
        ref int valid = ref value;
        ref int invalid = ref readonly value;
    }
}").GetParseDiagnostics().Verify(
                // (8,31): error CS1525: Invalid expression term 'readonly'
                //         ref int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(8, 31),
                // (8,31): error CS1002: ; expected
                //         ref int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(8, 31),
                // (8,31): error CS0106: The modifier 'readonly' is not valid for this item
                //         ref int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(8, 31),
                // (8,45): error CS1001: Identifier expected
                //         ref int invalid = ref readonly value;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(8, 45));
        }

        [Fact]
        public void ReturnRefReadOnlyAreDisallowed()
        {
            CreateCompilation(@"
class Test
{
    int value = 0;

    ref readonly int Valid() => ref value;
    ref readonly int Invalid() => ref readonly value;

}").GetParseDiagnostics().Verify(
                // (7,39): error CS1525: Invalid expression term 'readonly'
                //     ref readonly int Invalid() => ref readonly value;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(7, 39),
                // (7,39): error CS1002: ; expected
                //     ref readonly int Invalid() => ref readonly value;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(7, 39),
                // (7,53): error CS1519: Invalid token ';' ref readonly class, struct, or interface member declaration
                //     ref readonly int Invalid() => ref readonly value;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(7, 53),
                // (7,53): error CS1519: Invalid token ';' ref readonly class, struct, or interface member declaration
                //     ref readonly int Invalid() => ref readonly value;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(7, 53));
        }

        [Fact]
        public void RefReadOnlyForEachAreDisallowed()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        var ar = new int[] { 1, 2, 3 };

        foreach(ref readonly v in ar)
        {
        }
    }
}").GetParseDiagnostics().Verify(
                // (8,17): error CS1525: Invalid expression term 'ref'
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(8, 17),
                // (8,21): error CS1525: Invalid expression term 'readonly'
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(8, 21),
                // (8,21): error CS1515: 'in' expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_InExpected, "readonly").WithLocation(8, 21),
                // (8,21): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "readonly").WithLocation(8, 21),
                // (8,21): error CS1525: Invalid expression term 'readonly'
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(8, 21),
                // (8,21): error CS1026: ) expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(8, 21),
                // (8,21): error CS0106: The modifier 'readonly' is not valid for this item
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(8, 21),
                // (8,32): error CS1001: Identifier expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "in").WithLocation(8, 32),
                // (8,32): error CS1003: Syntax error, ',' expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",").WithLocation(8, 32),
                // (8,35): error CS1002: ; expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ar").WithLocation(8, 35),
                // (8,37): error CS1002: ; expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 37),
                // (8,37): error CS1513: } expected
                //         foreach(ref readonly v in ar)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 37));
        }

        [Fact]
        public void RefReadOnlyAtCallSite()
        {
            CreateCompilation(@"
class Test
{
    void M(in int p)
    {
    }
    void N()
    {
        int x = 0;
        M(ref readonly x);
    }
}").GetParseDiagnostics().Verify(
                // (10,15): error CS1525: Invalid expression term 'readonly'
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(10, 15),
                // (10,15): error CS1026: ) expected
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(10, 15),
                // (10,15): error CS1002: ; expected
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(10, 15),
                // (10,15): error CS0106: The modifier 'readonly' is not valid for this item
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(10, 15),
                // (10,25): error CS1001: Identifier expected
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(10, 25),
                // (10,25): error CS1003: Syntax error, ',' expected
                //         M(ref readonly x);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(10, 25));
        }

        [Fact]
        public void InAtCallSite()
        {
            CreateCompilation(@"
class Test
{
    void M(in int p)
    {
    }
    void N()
    {
        int x = 0;
        M(in x);
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void NothingAtCallSite()
        {
            CreateCompilation(@"
class Test
{
    void M(in int p)
    {
    }
    void N()
    {
        int x = 0;
        M(x);
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void InverseReadOnlyRefShouldBeIllegal()
        {
            CreateCompilation(@"
class Test
{
    void M(readonly ref int p)
    {
    }
}").VerifyDiagnostics(
                // (4,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //     void M(readonly ref int p)
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(4, 12));
        }

        [Fact]
        public void RefReadOnlyReturnIllegalInOperators()
        {
            CreateCompilation(@"
public class Test
{
    public static ref readonly bool operator!(Test obj) => throw null;
}").GetParseDiagnostics().Verify(
                // (4,37): error CS1519: Invalid token 'operator' in class, record, struct, or interface member declaration
                //     public static ref readonly bool operator!(Test obj) => throw null;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(4, 37),
                // (4,37): error CS1519: Invalid token 'operator' in class, record, struct, or interface member declaration
                //     public static ref readonly bool operator!(Test obj) => throw null;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(4, 37),
                // (4,55): error CS8124: Tuple must contain at least two elements.
                //     public static ref readonly bool operator!(Test obj) => throw null;
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 55),
                // (4,57): error CS1519: Invalid token '=>' in class, record, struct, or interface member declaration
                //     public static ref readonly bool operator!(Test obj) => throw null;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(4, 57));
        }

        [Fact]
        public void InNotAllowedInReturnType()
        {
            CreateCompilation(@"
class Test
{
    in int M() => throw null;
}").VerifyDiagnostics(
                // (4,5): error CS1519: Invalid token 'in' in class, record, struct, or interface member declaration
                //     in int M() => throw null;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "in").WithArguments("in").WithLocation(4, 5));
        }

        [Fact, WorkItem(25264, "https://github.com/dotnet/roslyn/issues/25264")]
        public void TestNewRefArray()
        {
            UsingStatement("new ref[];",
                // (1,8): error CS1031: Type expected
                // new ref[];
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(1, 8));

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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
                    M(SyntaxKind.ArgumentList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void RefReadonlyParameter()
        {
            var source = "void M(ref readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void Readonly_Duplicate_01()
        {
            var source = "void M(ref readonly readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void Readonly_Duplicate_02()
        {
            var source = "void M(readonly readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void Readonly_Duplicate_03()
        {
            var source = "void M(readonly ref readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void Readonly_Duplicate_04()
        {
            var source = "void M(readonly readonly ref int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void Readonly_Duplicate_05()
        {
            var source = "void M(ref readonly ref readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithThis()
        {
            var source = "void M(this ref readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ThisKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithThis_02()
        {
            var source = "void M(ref this readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ThisKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithThis_03()
        {
            var source = "void M(ref readonly this int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.ThisKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithScoped_01()
        {
            var source = "void M(scoped ref readonly int p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithScoped_02()
        {
            var source = "void M(ref scoped readonly int p);";

            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source, TestOptions.RegularPreview);
            verifyNodes();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,6): error CS8112: Local function 'M(scoped ref int)' must declare a body because it is not marked 'static extern'.
                // void M(ref scoped readonly int p);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "M").WithArguments("M(scoped ref int)").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M(ref scoped readonly int p);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (1,12): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // void M(ref scoped readonly int p);
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 12),
                // (1,19): error CS9190: 'readonly' modifier must be specified after 'ref'.
                // void M(ref scoped readonly int p);
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(1, 19));

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonlyWithScoped_03()
        {
            var source = "void M(readonly scoped ref int p);";

            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source, TestOptions.RegularPreview);
            verifyNodes();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,6): error CS8112: Local function 'M(scoped ref int)' must declare a body because it is not marked 'static extern'.
                // void M(readonly scoped ref int p);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "M").WithArguments("M(scoped ref int)").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M(readonly scoped ref int p);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (1,8): error CS9190: 'readonly' modifier must be specified after 'ref'.
                // void M(readonly scoped ref int p);
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(1, 8));

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ReadonlyWithScoped()
        {
            var source = "void M(scoped readonly int p);";

            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source, TestOptions.RegularPreview);
            verifyNodes();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,6): error CS8112: Local function 'M(scoped int)' must declare a body because it is not marked 'static extern'.
                // void M(scoped readonly int p);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "M").WithArguments("M(scoped int)").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M(scoped readonly int p);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (1,8): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // void M(scoped readonly int p);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped readonly int p").WithLocation(1, 8),
                // (1,15): error CS9348: The 'readonly' modifier cannot immediately follow the 'scoped' modifier.
                // void M(scoped readonly int p);
                Diagnostic(ErrorCode.ERR_InvalidModifierAfterScoped, "readonly").WithArguments("readonly").WithLocation(1, 15),
                // (1,15): error CS9190: 'readonly' modifier must be specified after 'ref'.
                // void M(scoped readonly int p);
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(1, 15));

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ReadonlyWithScoped_02()
        {
            var source = "void M(scoped readonly ref int p);";

            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source, TestOptions.RegularPreview);
            verifyNodes();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,6): error CS8112: Local function 'M(scoped ref int)' must declare a body because it is not marked 'static extern'.
                // void M(scoped readonly ref int p);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "M").WithArguments("M(scoped ref int)").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'M' is declared but never used
                // void M(scoped readonly ref int p);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(1, 6),
                // (1,15): error CS9348: The 'readonly' modifier cannot immediately follow the 'scoped' modifier.
                // void M(scoped readonly ref int p);
                Diagnostic(ErrorCode.ERR_InvalidModifierAfterScoped, "readonly").WithArguments("readonly").WithLocation(1, 15),
                // (1,15): error CS9190: 'readonly' modifier must be specified after 'ref'.
                // void M(scoped readonly ref int p);
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(1, 15));

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonly_ScopedParameterName()
        {
            var source = "void M(ref readonly int scoped);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonly_ScopedTypeName()
        {
            var source = "void M(ref readonly scoped p);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.IdentifierToken, "p");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void RefReadonly_ScopedBothNames()
        {
            var source = "void M(ref readonly scoped scoped);";
            UsingDeclaration(source, TestOptions.Regular11);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular12);
            verifyNodes();

            UsingDeclaration(source, TestOptions.Regular9);
            verifyNodes();

            UsingDeclaration(source);
            verifyNodes();

            void verifyNodes()
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Theory, CombinatorialData]
        public void ArgumentModifier_RefReadonly(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingExpression("M(ref x, in y, ref readonly z)", TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,20): error CS1525: Invalid expression term 'readonly'
                // M(ref x, in y, ref readonly z)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(1, 20),
                // (1,20): error CS1003: Syntax error, ',' expected
                // M(ref x, in y, ref readonly z)
                Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(1, 20),
                // (1,29): error CS1003: Syntax error, ',' expected
                // M(ref x, in y, ref readonly z)
                Diagnostic(ErrorCode.ERR_SyntaxError, "z").WithArguments(",").WithLocation(1, 29));

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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "z");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ScopedThis()
        {
            var source = """
                ref struct S { }
                static class C
                {
                    public static void M(scoped this S x)
                    {
                    }
                }
                """;
            UsingTree(source);
            CreateCompilation(source).VerifyDiagnostics(
                // (4,33): error CS9348: The 'this' modifier cannot immediately follow the 'scoped' modifier.
                //     public static void M(scoped this S x)
                Diagnostic(ErrorCode.ERR_InvalidModifierAfterScoped, "this").WithArguments("this").WithLocation(4, 33));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.ThisKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ThisScoped()
        {
            var source = """
                ref struct S { }

                static class C
                {
                    public static void M(this scoped S x)
                    {
                    }
                }
                """;
            UsingTree(source);
            CreateCompilation(source).VerifyDiagnostics();

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.ThisKeyword);
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ScopedParams()
        {
            var source = """
                static class C
                {
                    public static void M(scoped params S x)
                    {
                    }
                }
                """;
            UsingTree(source);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.ParamsKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ParamsScoped()
        {
            var source = """
                static class C
                {
                    public static void M(params scoped S x)
                    {
                    }
                }
                """;
            UsingTree(source);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.ParamsKeyword);
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ArgumentModifier_ReadonlyRef(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingExpression("M(readonly ref x)", TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,3): error CS1041: Identifier expected; 'readonly' is a keyword
                // M(readonly ref x)
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "readonly").WithArguments("", "readonly").WithLocation(1, 3));

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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory, CombinatorialData]
        public void ArgumentModifier_Readonly(
            [CombinatorialValues(LanguageVersion.CSharp11, LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            UsingExpression("M(readonly x)", TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,3): error CS1041: Identifier expected; 'readonly' is a keyword
                // M(readonly x)
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "readonly").WithArguments("", "readonly").WithLocation(1, 3));

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
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }
    }
}
