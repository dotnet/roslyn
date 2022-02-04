// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            TestConditional("b ? 1 : 2", "System.Int16", "System.Int32",
                // (6,26): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
                //         System.Int16 t = b ? 1 : 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1 : 2").WithArguments("int", "short").WithLocation(6, 26));
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
            TestConditional("b ? 0 : 0", "color", "System.Int32",
                // (6,19): error CS0266: Cannot implicitly convert type 'int' to 'color'. An explicit conversion exists (are you missing a cast?)
                //         color t = b ? 0 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 0 : 0").WithArguments("int", "color").WithLocation(6, 19));

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : $""x""", "System.FormattableString", "System.String",
                // (6,38): error CS0029: Cannot implicitly convert type 'string' to 'System.FormattableString'
                //         System.FormattableString t = b ? $"x" : $"x";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? $""x"" : $""x""").WithArguments("string", "System.FormattableString").WithLocation(6, 38));

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
                // (6,26): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
                //         System.Int16 t = b ? 1000000 : 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1000000 : 2").WithArguments("int", "short").WithLocation(6, 26)
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
                // (6,19): error CS0266: Cannot implicitly convert type 'int' to 'color'. An explicit conversion exists (are you missing a cast?)
                //         color t = b ? 1 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1 : 0").WithArguments("int", "color").WithLocation(6, 19)
                );

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : ""x""", "System.FormattableString", "System.String",
                // (6,38): error CS0029: Cannot implicitly convert type 'string' to 'System.FormattableString'
                //         System.FormattableString t = b ? $"x" : "x";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? $""x"" : ""x""").WithArguments("string", "System.FormattableString").WithLocation(6, 38)
                );

            // Implicit nullable conversions
            // Null literal conversions
            TestConditional(@"b ? """" : null", "System.Int64?", "System.String",
                // (6,27): error CS0029: Cannot implicitly convert type 'string' to 'long?'
                //         System.Int64? t = b ? "" : null;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? """" : null").WithArguments("string", "long?").WithLocation(6, 27)
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
        public void NonBreakingChange_01()
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
        public void NonBreakingChange_02()
        {
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
                    .VerifyDiagnostics();
            }
        }

        [Fact]
        public void NonBreakingChange_03()
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
        public void NonBreakingChange_04()
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

public enum @color {{ Red, Blue, Green }};

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

        [Theory]
        [InlineData("sbyte", "System.Int32", "System.SByte")]
        [InlineData("short", "System.Int32", "System.Int16")]
        [InlineData("int", "System.Int32", "System.Int32")]
        [InlineData("long", "System.Int64", "System.Int64")]
        [InlineData("byte", "System.Int32", "System.Byte")]
        [InlineData("ushort", "System.Int32", "System.UInt16")]
        [WorkItem(49598, "https://github.com/dotnet/roslyn/issues/49598")]
        public void IntType_01(string sourceType, string resultType1, string resultType2)
        {
            var source =
$@"class Program
{{
    static void Main()
    {{
        {sourceType}? a = 1;
        var b = true;
        var c1 = a ?? (b ? 2 : 3);
        var c2 = a ?? (true ? 2 : 3);
        var c3 = a ?? (false ? 2 : 3);
        Report(c1);
        Report(c2);
        Report(c3);
    }}
    static void Report(object obj)
    {{
        System.Console.WriteLine(""{{0}}: {{1}}"", obj.GetType().FullName, obj);
    }}
}}";
            var expectedOutput =
$@"{resultType1}: 1
{resultType2}: 1
{resultType2}: 1";
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3), expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), expectedOutput: expectedOutput);
        }

        [Theory]
        [InlineData("uint")]
        [InlineData("ulong")]
        [WorkItem(49598, "https://github.com/dotnet/roslyn/issues/49598")]
        public void IntType_02(string sourceType)
        {
            var source =
$@"class Program
{{
    static void Main()
    {{
        {sourceType}? a = 1;
        var b = true;
        var c1 = a ?? (b ? 2 : 3);
        var c2 = a ?? (true ? 2 : 3);
        var c3 = a ?? (false ? 2 : 3);
        Report(c1);
        Report(c2);
        Report(c3);
    }}
    static void Report(object obj)
    {{
        System.Console.WriteLine(""{{0}}: {{1}}"", obj.GetType().FullName, obj);
    }}
}}";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (7,18): error CS0019: Operator '??' cannot be applied to operands of type 'uint?' and 'int'
                //         var c1 = a ?? (b ? 2 : 3);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "a ?? (b ? 2 : 3)").WithArguments("??", $"{sourceType}?", "int").WithLocation(7, 18)
            };
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3)).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion())).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        [WorkItem(49598, "https://github.com/dotnet/roslyn/issues/49598")]
        public void IntType_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        char? a = 'A';
        var b = true;
        var c1 = a ?? (b ? 'B' : 'C');
        var c2 = a ?? (true ? 'B' : 'C');
        var c3 = a ?? (false ? 'B' : 'C');
        Report(c1);
        Report(c2);
        Report(c3);
    }
    static void Report(object obj)
    {
        System.Console.WriteLine(""{0}: {1}"", obj.GetType().FullName, obj);
    }
}";
            var expectedOutput =
@"System.Char: A
System.Char: A
System.Char: A";
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3), expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(49598, "https://github.com/dotnet/roslyn/issues/49598")]
        public void IntType_04()
        {
            var source =
@"class Program
{
    static void Main()
    {
        char? a = 'A';
        var b = true;
        var c1 = a ?? (b ? 0 : 0);
        var c2 = a ?? (true ? 0 : 0);
        var c3 = a ?? (false ? 0 : 0);
        Report(c1);
        Report(c2);
        Report(c3);
    }
    static void Report(object obj)
    {
        System.Console.WriteLine(""{0}: {1}"", obj.GetType().FullName, obj);
    }
}";
            var expectedOutput =
@"System.Int32: 65
System.Int32: 65
System.Int32: 65";
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3), expectedOutput: expectedOutput);
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()), expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(49627, "https://github.com/dotnet/roslyn/issues/49627")]
        public void UserDefinedConversions_01()
        {
            var source =
@"struct A
{
    public static implicit operator A(short s) => throw null;
    public static implicit operator int(A a) => throw null;
    public static A operator+(A x, A y) => throw null;
}
struct B
{
    public static implicit operator B(int i) => throw null;
}
class Program
{
    static B F(bool b, A a) 
    {
        return (b ? a : 0) + a;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3)).VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion())).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(49627, "https://github.com/dotnet/roslyn/issues/49627")]
        public void UserDefinedConversions_02()
        {
            var source =
@"struct A
{
    public static implicit operator A(int i) => throw null;
    public static implicit operator int(A a) => throw null;
    public static A operator+(A x, A y) => throw null;
}
struct B
{
    public static implicit operator B(int i) => throw null;
}
class Program
{
    static B F(bool b, A a) 
    {
        return (b ? a : 0) + a;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (15,16): error CS0029: Cannot implicitly convert type 'A' to 'B'
                //         return (b ? a : 0) + a;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(b ? a : 0) + a").WithArguments("A", "B").WithLocation(15, 16),
                // (15,17): error CS8957: Conditional expression is not valid in language version 7.3 because a common type was not found between 'A' and 'int'. To use a target-typed conversion, upgrade to language version 9.0 or greater.
                //         return (b ? a : 0) + a;
                Diagnostic(ErrorCode.ERR_NoImplicitConvTargetTypedConditional, "b ? a : 0").WithArguments("7.3", "A", "int", "9.0").WithLocation(15, 17));

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion())).VerifyDiagnostics(
                // (15,16): error CS0029: Cannot implicitly convert type 'A' to 'B'
                //         return (b ? a : 0) + a;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(b ? a : 0) + a").WithArguments("A", "B").WithLocation(15, 16));
        }

        [Fact]
        public void NaturalType_01()
        {
            var source =
@"class Program
{
    static void F(bool b, object x, string y)
    {
        _ = b ? x : y;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Equal("System.Object", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void NaturalType_02()
        {
            var source =
@"class Program
{
    static void F(bool b, int x)
    {
        int? y = b ? x : null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(expr);
            Assert.Null(typeInfo.Type);
            Assert.Equal("System.Int32?", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void DiagnosticClarity_LangVersion8()
        {
            var source = @"
class C
{
    void M(bool b)
    {
        int? i = b ? 1 : null;
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,18): error CS8957: Conditional expression is not valid in language version 8.0 because a common type was not found between 'int' and '<null>'. To use a target-typed conversion, upgrade to language version 9.0 or greater.
                //         int? i = b ? 1 : null;
                Diagnostic(ErrorCode.ERR_NoImplicitConvTargetTypedConditional, "b ? 1 : null").WithArguments("8.0", "int", "<null>", "9.0").WithLocation(6, 18));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }
    }
}
