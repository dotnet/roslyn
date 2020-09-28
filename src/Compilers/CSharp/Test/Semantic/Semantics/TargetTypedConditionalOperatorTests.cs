// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test binding of the target-typed conditional (aka ternary) operator.
    /// </summary>
    public class TargetTypedConditionalOperatorTests : CSharpTestBase
    {
        [Fact]
        public void TestImplicitConversions_Good()
        {
            // NOTE: Some of these are currently error cases, but they would become accepted (non-error) cases
            // if we extend the spec to permit target typing even when there is a natural type.  Until then,
            // they are error cases but included here for convenience.

            // Implicit constant expression conversions
            TestConditional("b ? 1 : 2", "System.Int16", "System.Int32");
            TestConditional("b ? -1L : 1UL", "System.Double", null);

            // Implicit reference conversions
            TestConditional("b ? GetB() : GetC()", "A", null);
            TestConditional("b ? Get<IOut<B>>() : Get<IOut<C>>()", "IOut<A>", null);
            TestConditional("b ? Get<IOut<IOut<B>>>() : Get<IOut<IOut<C>>>()", "IOut<IOut<A>>", null);
            TestConditional("b ? Get<IOut<B[]>>() : Get<IOut<C[]>>()", "IOut<A[]>", null);
            TestConditional("b ? Get<U>() : Get<V>()", "T", null);

            // Implicit numeric conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.Int64", null);

            // Implicit enumeration conversions
            TestConditional("b ? 0 : 0", "color", "System.Int32");

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : $""x""", "System.FormattableString", "System.String");

            // Implicit nullable conversions
            // Null literal conversions
            TestConditional("b ? 1 : null", "System.Int64?", null);

            // Boxing conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.IComparable", null);

            // User - defined implicit conversions
            TestConditional("b ? GetB() : GetC()", "X", null);

            // Anonymous function conversions
            TestConditional("b ? a=>a : b=>b", "Del", null);

            // Method group conversions
            TestConditional("b ? M1 : M2", "Del", null);

            // Pointer conversions
            TestConditional("b ? GetIntp() : GetLongp()", "void*", null);
            TestConditional("b ? null : null", "System.Int32*", null);
        }

        [Fact]
        public void TestImplicitConversions_Bad()
        {
            // Implicit constant expression conversions
            TestConditional("b ? 1000000 : 2", "System.Int16", "System.Int32",
                // (6,30): error CS0029: Cannot implicitly convert type 'int' to 'short'
                //         System.Int16 t = b ? 1000000 : 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1000000").WithArguments("int", "short").WithLocation(6, 30)
                );

            // Implicit reference conversions
            TestConditional("b ? GetB() : GetC()", "System.String", null,
                // (6,31): error CS0029: Cannot implicitly convert type 'B' to 'string'
                //         System.String t = b ? GetB() : GetC();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetB()").WithArguments("B", "string").WithLocation(6, 31),
                // (6,40): error CS0029: Cannot implicitly convert type 'C' to 'string'
                //         System.String t = b ? GetB() : GetC();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetC()").WithArguments("C", "string").WithLocation(6, 40)
                );

            // Implicit numeric conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.UInt64", null,
                // (6,43): error CS0029: Cannot implicitly convert type 'int' to 'ulong'
                //         System.UInt64 t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "ulong").WithLocation(6, 43)
                );

            // Implicit enumeration conversions
            TestConditional("b ? 1 : 0", "color", "System.Int32",
                // (6,23): error CS0029: Cannot implicitly convert type 'int' to 'color'
                //         color t = b ? 1 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "color").WithLocation(6, 23)
                );

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : ""x""", "System.FormattableString", "System.String",
                // (6,49): error CS0029: Cannot implicitly convert type 'string' to 'System.FormattableString'
                //         System.FormattableString t = b ? $"x" : "x";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""x""").WithArguments("string", "System.FormattableString").WithLocation(6, 49)
                );

            // Implicit nullable conversions
            // Null literal conversions
            TestConditional(@"b ? """" : null", "System.Int64?", "System.String",
                // (6,31): error CS0029: Cannot implicitly convert type 'string' to 'long?'
                //         System.Int64? t = b ? "" : null;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "long?").WithLocation(6, 31)
                );
            TestConditional(@"b ? 1 : """"", "System.Int64?", null,
                // (6,35): error CS0029: Cannot implicitly convert type 'string' to 'long?'
                //         System.Int64? t = b ? 1 : "";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "long?").WithLocation(6, 35)
                );

            // Boxing conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.Collections.IList", null,
                // (6,42): error CS0029: Cannot implicitly convert type 'uint' to 'System.Collections.IList'
                //         System.Collections.IList t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUInt()").WithArguments("uint", "System.Collections.IList").WithLocation(6, 42),
                // (6,54): error CS0029: Cannot implicitly convert type 'int' to 'System.Collections.IList'
                //         System.Collections.IList t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "System.Collections.IList").WithLocation(6, 54)
                );

            // User - defined implicit conversions
            TestConditional("b ? GetB() : GetD()", "X", null,
                // (6,28): error CS0619: 'D.implicit operator X(D)' is obsolete: 'D'
                //         X t = b ? GetB() : GetD();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "GetD()").WithArguments("D.implicit operator X(D)", "D").WithLocation(6, 28)
                );

            // Anonymous function conversions
            TestConditional(@"b ? a=>a : b=>""""", "Del", null,
                // (6,31): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         Del t = b ? a=>a : b=>"";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(6, 31),
                // (6,31): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Del t = b ? a=>a : b=>"";
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, @"""""").WithArguments("lambda expression").WithLocation(6, 31)
                );

            // Method group conversions
            TestConditional("b ? M1 : M3", "Del", null,
                // (6,26): error CS0123: No overload for 'M3' matches delegate 'Del'
                //         Del t = b ? M1 : M3;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M3").WithArguments("M3", "Del").WithLocation(6, 26)
                );
        }

        [Fact]
        public void NonbreakingChange()
        {
            var source = @"
class C
{
    static void M(short x) => System.Console.WriteLine(""M(short)"");
    static void M(long l) => System.Console.WriteLine(""M(long)"");
    static void Main()
    {
        bool b = true;
        M(b ? 1 : 2); // should call M(long)
    }
}
";
            foreach (var langVersion in new[] { LanguageVersion.CSharp8, MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion() })
            {
                var comp = CreateCompilation(
                    source, options: TestOptions.ReleaseExe,
                    parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
                    .VerifyDiagnostics();
                CompileAndVerify(comp, expectedOutput: "M(long)");
            }
        }

        [Fact]
        public void BreakingChange_02()
        {
            // Prior to C# 9.0, this program compiles without error, as only the overload M(long, long)
            // is a candidate. With the semantic changes in C# 9.0, both are candidates, but neither is
            // more specific.
            var source = @"
class C
{
    static void M(short x, short y) { }
    static void M(long x, long y) { }
    static void Main()
    {
        bool b = true;
        M(b ? 1 : 2, 1);
    }
}
";
            foreach (var langVersion in new[] { LanguageVersion.CSharp8, MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion() })
            {
                var comp = CreateCompilation(
                    source, options: TestOptions.ReleaseExe,
                    parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
                    .VerifyDiagnostics(
                        // (9,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(short, short)' and 'C.M(long, long)'
                        //         M(b ? 1 : 2, 1);
                        Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(short, short)", "C.M(long, long)").WithLocation(9, 9)
                    );
            }
        }

        [Fact]
        public void NonBreakingChange_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        _ = (short)(b ? 1 : 2);
    }
}
";
            foreach (var langVersion in new[] { LanguageVersion.CSharp8, MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion() })
            {
                var comp = CreateCompilation(
                    source, options: TestOptions.ReleaseExe,
                    parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
                    .VerifyDiagnostics(
                    );
            }
        }

        [Fact]
        public void NonBreakingChange_02()
        {
            var source = @"
class Program
{
    static void Main()
    {
        M(true, new A(), new B());
    }
    static void M(bool x, A a, B b)
    {
        _ = (C)(x ? a : b);
    }
}
class A
{
    public static implicit operator B(A a) { System.Console.WriteLine(""A->B""); return new B(); }
    public static implicit operator C(A a) { System.Console.WriteLine(""A->C""); return new C(); }
}
class B
{
    public static implicit operator C(B b) { System.Console.WriteLine(""B->C""); return new C(); }
}
class C { }
";
            foreach (var langVersion in new[] { LanguageVersion.CSharp8, MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion() })
            {
                var comp = CreateCompilation(
                    source, options: TestOptions.ReleaseExe,
                    parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
                    .VerifyDiagnostics(
                    );
                CompileAndVerify(comp, expectedOutput:
@"A->B
B->C");
            }
        }

        private static void TestConditional(string conditionalExpression, string targetType, string? naturalType, params DiagnosticDescription[] expectedDiagnostics)
        {
            TestConditional(conditionalExpression, targetType, naturalType, null, expectedDiagnostics);
        }

        private static void TestConditional(
            string conditionalExpression,
            string targetType,
            string? naturalType,
            CSharpParseOptions? parseOptions,
            params DiagnosticDescription[] expectedDiagnostics)
        {
            string source = $@"
class Program
{{
    unsafe void Test<T, U, V>(bool b) where T : class where U : class, T where V : class, T
    {{
        {targetType} t = {conditionalExpression};
        Use(t);
    }}

    A GetA() {{ return null; }}
    B GetB() {{ return null; }}
    C GetC() {{ return null; }}
    D GetD() {{ return null; }}
    int GetInt() {{ return 1; }}
    uint GetUInt() {{ return 1; }}
    T Get<T>() where T : class {{ return null; }}
    void Use(object t) {{ }}
    unsafe void Use(void* t) {{ }}
    unsafe int* GetIntp() {{ return null; }}
    unsafe long* GetLongp() {{ return null; }}

    static int M1(int x) => x;
    static int M2(int x) => x;
    static int M3(int x, int y) => x;
}}

public enum color {{ Red, Blue, Green }};

class A {{ }}
class B : A {{ public static implicit operator X(B self) => new X(); }}
class C : A {{ public static implicit operator X(C self) => new X(); }}
class D : A {{ [System.Obsolete(""D"", true)] public static implicit operator X(D self) => new X(); }}

class X {{ }}

interface IOut<out T> {{ }}
interface IIn<in T> {{ }}

delegate int Del(int x);
";

            parseOptions ??= TestOptions.Regular;
            parseOptions = parseOptions.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion());
            var tree = Parse(source, options: parseOptions);

            var comp = CreateCompilation(tree, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(expectedDiagnostics);

            var compUnit = tree.GetCompilationUnitRoot();
            var classC = (TypeDeclarationSyntax)compUnit.Members.First();
            var methodTest = (MethodDeclarationSyntax)classC.Members.First();
            var stmt = (LocalDeclarationStatementSyntax)methodTest.Body!.Statements.First();
            var conditionalExpr = (ConditionalExpressionSyntax)stmt.Declaration.Variables[0].Initializer!.Value;

            var model = comp.GetSemanticModel(tree);

            if (naturalType is null)
            {
                var actualType = model.GetTypeInfo(conditionalExpr).Type;
                if (actualType is { })
                {
                    Assert.NotEmpty(expectedDiagnostics);
                    Assert.Equal("?", actualType.ToTestDisplayString(includeNonNullable: false));
                }
            }
            else
            {
                Assert.Equal(naturalType, model.GetTypeInfo(conditionalExpr).Type.ToTestDisplayString(includeNonNullable: false));
            }

            var convertedType = targetType switch { "void*" => "System.Void*", _ => targetType };
            Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr).ConvertedType.ToTestDisplayString(includeNonNullable: false));

            if (!expectedDiagnostics.Any())
            {
                Assert.Equal(SpecialType.System_Boolean, model.GetTypeInfo(conditionalExpr.Condition).Type!.SpecialType);
                Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr.WhenTrue).ConvertedType.ToTestDisplayString(includeNonNullable: false)); //in parent to catch conversion
                Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr.WhenFalse).ConvertedType.ToTestDisplayString(includeNonNullable: false)); //in parent to catch conversion
            }
        }

        [Fact, WorkItem(45460, "https://github.com/dotnet/roslyn/issues/45460")]
        public void TestConstantConditional()
        {
            var source = @"
using System;
public class Program {
    static void Main()
    {
        Test1();
        Test2();
    }

    public static void Test1() {
        const bool b = true;
        uint u1 = M1<uint>(b ? 1 : 0);
        Console.WriteLine(u1); // 1
        uint s1 = M2(b ? 2 : 3);
        Console.WriteLine(s1); // 2
        uint u2 = b ? 4 : 5;
        Console.WriteLine(u2); // 4

        static uint M2(uint t) => t;
    }
    public static void Test2() {
        const bool b = true;
        short s1 = M1<short>(b ? 1 : 0);
        Console.WriteLine(s1); // 1
        short s2 = M2(b ? 2 : 3);
        Console.WriteLine(s2); // 2
        short s3 = b ? 4 : 5;
        Console.WriteLine(s3); // 4

        static short M2(short t) => t;
    }
    public static T M1<T>(T t) => t;
}";
            var expectedOutput = @"
1
2
4
1
2
4";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugExe)
                .VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
            comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), options: TestOptions.DebugExe)
                .VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(46231, "https://github.com/dotnet/roslyn/issues/46231")]
        public void TestFixedConditional()
        {
            var source = @"
public class Program {
    public unsafe static void Test(bool b, int i)
    {
        fixed (byte * p = b ? new byte[i] : null)
        {
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugDll.WithAllowUnsafe(true))
                .VerifyEmitDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), options: TestOptions.DebugDll.WithAllowUnsafe(true))
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestUsingConditional()
        {
            var source = @"
using System;
public class Program {
    public static void Test(bool b, IDisposable d)
    {
        using (IDisposable x = b ? d : null)
        {
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular8, options: TestOptions.DebugDll)
                .VerifyEmitDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), options: TestOptions.DebugDll)
                .VerifyEmitDiagnostics();
        }
    }
}
