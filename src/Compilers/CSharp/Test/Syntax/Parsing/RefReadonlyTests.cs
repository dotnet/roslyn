// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1), options: TestOptions.UnsafeDebugDll);
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

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1), options: TestOptions.UnsafeDebugDll);
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
                // (9,27): error CS1003: Syntax error, '(' expected
                //     ref readonly int Field;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(").WithLocation(9, 27),
                // (9,27): error CS1026: ) expected
                //     ref readonly int Field;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 27),
                // (11,41): error CS1519: Invalid token 'operator' in class, record, struct, or interface member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (11,41): error CS1519: Invalid token 'operator' in class, record, struct, or interface member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (12,5): error CS1519: Invalid token '{' ref readonly class, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
                // (12,5): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
                // (17,5): error CS8803: Top-level statements must precede namespace and type declarations.
                //     static async ref readonly Task M<T>()
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"static async ref readonly Task M<T>()
    {
        throw null;
    }").WithLocation(17, 5),
                // (22,25): error CS1031: Type expected
                //     public ref readonly virtual int* P1 => throw null;
                Diagnostic(ErrorCode.ERR_TypeExpected, "virtual").WithLocation(22, 25),
                // (24,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(24, 1));
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
            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
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
                //         M(in x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(10, 15),
                // (10,15): error CS1026: ) expected
                //         M(in x);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(10, 15),
                // (10,15): error CS1002: ; expected
                //         M(in x);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(10, 15),
                // (10,15): error CS0106: The modifier 'readonly' is not valid for this item
                //         M(in x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(10, 15),
                // (10,25): error CS1001: Identifier expected
                //         M(in x);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(10, 25),
                // (10,25): error CS1002: ; expected
                //         M(in x);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(10, 25),
                // (10,25): error CS1513: } expected
                //         M(in x);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(10, 25));
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
}").GetParseDiagnostics().Verify(
                // (4,12): error CS1026: ) expected
                //     void M(readonly ref int p)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(4, 12),
                // (4,12): error CS1002: ; expected
                //     void M(readonly ref int p)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(4, 12),
                // (4,30): error CS1003: Syntax error, '(' expected
                //     void M(readonly ref int p)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(4, 30));
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

        [Fact]
        public void RefReadOnlyNotAllowedInParameters()
        {
            CreateCompilation(@"
class Test
{
    void M(ref readonly int p) => throw null;
}").VerifyDiagnostics(
                // (4,16): error CS1031: Type expected
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_TypeExpected, "readonly").WithLocation(4, 16),
                // (4,16): error CS1001: Identifier expected
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(4, 16),
                // (4,16): error CS1026: ) expected
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(4, 16),
                // (4,16): error CS1002: ; expected
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(4, 16),
                // (4,30): error CS1003: Syntax error, ',' expected
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(4, 30),
                // (4,10): error CS0501: 'Test.M(ref ?)' must declare a body because it is not marked abstract, extern, or partial
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("Test.M(ref ?)").WithLocation(4, 10),
                // (4,29): warning CS0169: The field 'Test.p' is never used
                //     void M(ref readonly int p) => throw null;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "p").WithArguments("Test.p").WithLocation(4, 29));
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

    }
}
