// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefReadonlyReturnsTests : ParsingTests
    {
        public RefReadonlyReturnsTests(ITestOutputHelper output) : base(output) { }

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

            ParseAndValidate(text, TestOptions.Regular,
                // (9,27): error CS1003: Syntax error, '(' expected
                //     ref readonly int Field;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(", ";").WithLocation(9, 27),
                // (9,27): error CS1026: ) expected
                //     ref readonly int Field;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 27),
                // (11,41): error CS1519: Invalid token 'operator' in class, struct, or interface member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (11,41): error CS1519: Invalid token 'operator' in class, struct, or interface member declaration
                //     public static ref readonly Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (12,5): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
                // (12,5): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
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
                // (7,9): error CS1073: Unexpected token 'ref'
                //         ref readonly int local = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(7, 9),
                // (9,10): error CS1073: Unexpected token 'ref'
                //         (ref readonly int, ref readonly int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 10),
                // (9,28): error CS1073: Unexpected token 'ref'
                //         (ref readonly int, ref readonly int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 28),
                // (11,41): error CS1073: Unexpected token 'ref'
                //         System.Collections.Generic.List<ref readonly int> x = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(11, 41)
            );
        }
    }
}
