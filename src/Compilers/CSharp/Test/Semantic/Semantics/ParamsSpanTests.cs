// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParamsSpanTests : CSharpTestBase
    {
        [ConditionalFact(typeof(CoreClrOnly))]
        public void ParamsSpan_01()
        {
            var sourceA =
@"using System;
public class A
{
    public static void F1(params Span<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    public static void F2(params ReadOnlySpan<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
}";

            var compA = CreateCompilation(sourceA, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.Regular10);
            compA.VerifyDiagnostics(
                // (4,27): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F1(params Span<object> args)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params").WithArguments("params Span<T>").WithLocation(4, 27),
                // (8,27): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void F2(params ReadOnlySpan<object> args)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params").WithArguments("params Span<T>").WithLocation(8, 27));

            compA = CreateCompilation(sourceA, targetFramework: TargetFramework.NetCoreApp);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"class B
{
    static void Main()
    {
        A.F1();
        A.F1(1, 2, ""hello"");
        A.F2();
        A.F2(""span"", 3);
    }
}";

            var compB = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
            compB.VerifyDiagnostics(
                // (5,9): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         A.F1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "A.F1()").WithArguments("params Span<T>").WithLocation(5, 9),
                // (6,9): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         A.F1(1, 2, "hello");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"A.F1(1, 2, ""hello"")").WithArguments("params Span<T>").WithLocation(6, 9),
                // (7,9): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         A.F2();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "A.F2()").WithArguments("params Span<T>").WithLocation(7, 9),
                // (8,9): error CS8652: The feature 'params Span<T>' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         A.F2("span", 3);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"A.F2(""span"", 3)").WithArguments("params Span<T>").WithLocation(8, 9));

            compB = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            compB.VerifyDiagnostics();
            var verifier = CompileAndVerify(compB, verify: Verification.Skipped, expectedOutput:
@"1
2
hello
span
3
");
            verifier.VerifyIL("B.Main",
@"{
  // Code size      154 (0x9a)
  .maxstack  2
  .locals init (System.Span<object> V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""object""
  IL_0006:  newobj     ""System.Span<object>..ctor(object[])""
  IL_000b:  call       ""void A.F1(params System.Span<object>)""
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldc.i4.3
  IL_0013:  newarr     ""object""
  IL_0018:  call       ""System.Span<object>..ctor(object[])""
  IL_001d:  ldloca.s   V_0
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""ref object System.Span<object>.this[int].get""
  IL_0025:  ldc.i4.1
  IL_0026:  box        ""int""
  IL_002b:  stind.ref
  IL_002c:  ldloca.s   V_0
  IL_002e:  ldc.i4.1
  IL_002f:  call       ""ref object System.Span<object>.this[int].get""
  IL_0034:  ldc.i4.2
  IL_0035:  box        ""int""
  IL_003a:  stind.ref
  IL_003b:  ldloca.s   V_0
  IL_003d:  ldc.i4.2
  IL_003e:  call       ""ref object System.Span<object>.this[int].get""
  IL_0043:  ldstr      ""hello""
  IL_0048:  stind.ref
  IL_0049:  ldloc.0
  IL_004a:  call       ""void A.F1(params System.Span<object>)""
  IL_004f:  ldc.i4.0
  IL_0050:  newarr     ""object""
  IL_0055:  newobj     ""System.Span<object>..ctor(object[])""
  IL_005a:  call       ""System.ReadOnlySpan<object> System.Span<object>.op_Implicit(System.Span<object>)""
  IL_005f:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_0064:  ldloca.s   V_0
  IL_0066:  ldc.i4.2
  IL_0067:  newarr     ""object""
  IL_006c:  call       ""System.Span<object>..ctor(object[])""
  IL_0071:  ldloca.s   V_0
  IL_0073:  ldc.i4.0
  IL_0074:  call       ""ref object System.Span<object>.this[int].get""
  IL_0079:  ldstr      ""span""
  IL_007e:  stind.ref
  IL_007f:  ldloca.s   V_0
  IL_0081:  ldc.i4.1
  IL_0082:  call       ""ref object System.Span<object>.this[int].get""
  IL_0087:  ldc.i4.3
  IL_0088:  box        ""int""
  IL_008d:  stind.ref
  IL_008e:  ldloc.0
  IL_008f:  call       ""System.ReadOnlySpan<object> System.Span<object>.op_Implicit(System.Span<object>)""
  IL_0094:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_0099:  ret
}");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.Preview)]
        public void ParamsSpan_02(LanguageVersion languageVersion)
        {
            var sourceA =
@"using System;
public class A
{
    public static void F1(params Span<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    public static void F2(params ReadOnlySpan<object> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
}";

            var compA = CreateCompilation(sourceA, targetFramework: TargetFramework.NetCoreApp);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"using System;
class B
{
    static void Main()
    {
        A.F1(new object[0]);
        A.F1(new object[] { 1 });
        A.F1(new Span<object>(new object[] { 2, ""hello"" }));
        A.F2(new object[0]);
        A.F2(new object[] { ""span"" });
        A.F2(new ReadOnlySpan<object>(new object[] { 3 }));
    }
}";

            var compB = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), options: TestOptions.ReleaseExe);
            compB.VerifyDiagnostics();
            var verifier = CompileAndVerify(compB, verify: Verification.Skipped, expectedOutput:
@"1
2
hello
span
3
");
            verifier.VerifyIL("B.Main",
@"{
  // Code size      140 (0x8c)
  .maxstack  4
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""object""
  IL_0006:  call       ""System.Span<object> System.Span<object>.op_Implicit(object[])""
  IL_000b:  call       ""void A.F1(params System.Span<object>)""
  IL_0010:  ldc.i4.1
  IL_0011:  newarr     ""object""
  IL_0016:  dup
  IL_0017:  ldc.i4.0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  stelem.ref
  IL_001f:  call       ""System.Span<object> System.Span<object>.op_Implicit(object[])""
  IL_0024:  call       ""void A.F1(params System.Span<object>)""
  IL_0029:  ldc.i4.2
  IL_002a:  newarr     ""object""
  IL_002f:  dup
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.2
  IL_0032:  box        ""int""
  IL_0037:  stelem.ref
  IL_0038:  dup
  IL_0039:  ldc.i4.1
  IL_003a:  ldstr      ""hello""
  IL_003f:  stelem.ref
  IL_0040:  newobj     ""System.Span<object>..ctor(object[])""
  IL_0045:  call       ""void A.F1(params System.Span<object>)""
  IL_004a:  ldc.i4.0
  IL_004b:  newarr     ""object""
  IL_0050:  call       ""System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])""
  IL_0055:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_005a:  ldc.i4.1
  IL_005b:  newarr     ""object""
  IL_0060:  dup
  IL_0061:  ldc.i4.0
  IL_0062:  ldstr      ""span""
  IL_0067:  stelem.ref
  IL_0068:  call       ""System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])""
  IL_006d:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_0072:  ldc.i4.1
  IL_0073:  newarr     ""object""
  IL_0078:  dup
  IL_0079:  ldc.i4.0
  IL_007a:  ldc.i4.3
  IL_007b:  box        ""int""
  IL_0080:  stelem.ref
  IL_0081:  newobj     ""System.ReadOnlySpan<object>..ctor(object[])""
  IL_0086:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_008b:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ParamsSpan_03()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(params Span<T> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void Main()
    {
        F(1, 2, 3);
        F(""4"", ""5"", ""6"");
    }
}";
            // PROTOTYPE: Should use <PrivateImplementationDetails> to initialize
            // the new[] { 1, 2, 3 } case, to match the behavior for 'params T[]'
            // from CodeGenerator.EmitArrayInitializers().
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
3
4
5
6
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      111 (0x6f)
  .maxstack  2
  .locals init (System.Span<int> V_0,
                System.Span<string> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""int""
  IL_0008:  call       ""System.Span<int>..ctor(int[])""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.0
  IL_0010:  call       ""ref int System.Span<int>.this[int].get""
  IL_0015:  ldc.i4.1
  IL_0016:  stind.i4
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref int System.Span<int>.this[int].get""
  IL_001f:  ldc.i4.2
  IL_0020:  stind.i4
  IL_0021:  ldloca.s   V_0
  IL_0023:  ldc.i4.2
  IL_0024:  call       ""ref int System.Span<int>.this[int].get""
  IL_0029:  ldc.i4.3
  IL_002a:  stind.i4
  IL_002b:  ldloc.0
  IL_002c:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0031:  ldloca.s   V_1
  IL_0033:  ldc.i4.3
  IL_0034:  newarr     ""string""
  IL_0039:  call       ""System.Span<string>..ctor(string[])""
  IL_003e:  ldloca.s   V_1
  IL_0040:  ldc.i4.0
  IL_0041:  call       ""ref string System.Span<string>.this[int].get""
  IL_0046:  ldstr      ""4""
  IL_004b:  stind.ref
  IL_004c:  ldloca.s   V_1
  IL_004e:  ldc.i4.1
  IL_004f:  call       ""ref string System.Span<string>.this[int].get""
  IL_0054:  ldstr      ""5""
  IL_0059:  stind.ref
  IL_005a:  ldloca.s   V_1
  IL_005c:  ldc.i4.2
  IL_005d:  call       ""ref string System.Span<string>.this[int].get""
  IL_0062:  ldstr      ""6""
  IL_0067:  stind.ref
  IL_0068:  ldloc.1
  IL_0069:  call       ""void Program.F<string>(params System.Span<string>)""
  IL_006e:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ParamsSpan_04()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(params ReadOnlySpan<T> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void Main()
    {
        F(1, 2, 3);
        F(""4"", ""5"", ""6"");
    }
}";
            // PROTOTYPE: Should use <PrivateImplementationDetails> to initialize
            // the new[] { 1, 2, 3 } case, to match the behavior for 'params T[]'
            // from CodeGenerator.EmitArrayInitializers().
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
3
4
5
6
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      121 (0x79)
  .maxstack  2
  .locals init (System.Span<int> V_0,
                System.Span<string> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.3
  IL_0003:  newarr     ""int""
  IL_0008:  call       ""System.Span<int>..ctor(int[])""
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.0
  IL_0010:  call       ""ref int System.Span<int>.this[int].get""
  IL_0015:  ldc.i4.1
  IL_0016:  stind.i4
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref int System.Span<int>.this[int].get""
  IL_001f:  ldc.i4.2
  IL_0020:  stind.i4
  IL_0021:  ldloca.s   V_0
  IL_0023:  ldc.i4.2
  IL_0024:  call       ""ref int System.Span<int>.this[int].get""
  IL_0029:  ldc.i4.3
  IL_002a:  stind.i4
  IL_002b:  ldloc.0
  IL_002c:  call       ""System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)""
  IL_0031:  call       ""void Program.F<int>(params System.ReadOnlySpan<int>)""
  IL_0036:  ldloca.s   V_1
  IL_0038:  ldc.i4.3
  IL_0039:  newarr     ""string""
  IL_003e:  call       ""System.Span<string>..ctor(string[])""
  IL_0043:  ldloca.s   V_1
  IL_0045:  ldc.i4.0
  IL_0046:  call       ""ref string System.Span<string>.this[int].get""
  IL_004b:  ldstr      ""4""
  IL_0050:  stind.ref
  IL_0051:  ldloca.s   V_1
  IL_0053:  ldc.i4.1
  IL_0054:  call       ""ref string System.Span<string>.this[int].get""
  IL_0059:  ldstr      ""5""
  IL_005e:  stind.ref
  IL_005f:  ldloca.s   V_1
  IL_0061:  ldc.i4.2
  IL_0062:  call       ""ref string System.Span<string>.this[int].get""
  IL_0067:  ldstr      ""6""
  IL_006c:  stind.ref
  IL_006d:  ldloc.1
  IL_006e:  call       ""System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)""
  IL_0073:  call       ""void Program.F<string>(params System.ReadOnlySpan<string>)""
  IL_0078:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Operator()
        {
            var source =
@"using System;
class A
{
    public static A operator+(A a, params Span<A> args)
    {
        return a;
    }
    public static implicit operator B(A a, params ReadOnlySpan<B> args)
    {
        return default;
    }
}
class B
{
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,36): error CS1670: params is not valid in this context
                //     public static A operator+(A a, params Span<A> args)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(4, 36),
                // (8,38): error CS1019: Overloadable unary operator expected
                //     public static implicit operator B(A a, params ReadOnlySpan<B> args)
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(A a, params ReadOnlySpan<B> args)").WithLocation(8, 38),
                // (8,44): error CS1670: params is not valid in this context
                //     public static implicit operator B(A a, params ReadOnlySpan<B> args)
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(8, 44));
        }

        /// <summary>
        /// Optional parameters are allowed before 'params'.
        /// </summary>
        [ConditionalFact(typeof(CoreClrOnly))]
        public void OptionalParameters()
        {
            var sourceA =
@"using System;
public class A
{
    public static void F1(int x, int y = 2, params Span<int> args)
    {
        Console.WriteLine((x, y));
        foreach (var arg in args) Console.WriteLine(arg);
    }
    public static void F2<T>(T x = default, params ReadOnlySpan<T> args)
    {
        Console.WriteLine(x);
        foreach (var arg in args) Console.WriteLine(arg);
    }
}";
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics();
            var refA = comp.EmitToImageReference();

            var sourceB =
@"class B
{
    static void Main()
    {
        A.F1(1);
        A.F1(2, 3);
        A.F1(3, 4, 5);
        A.F2<int>();
        A.F2(1);
        A.F2(2, 3, 4);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"(1, 2)
(2, 3)
(3, 4)
5
0
1
2
3
4
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void IArgumentOperation_01()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(params Span<T> args) { }
    static void Main()
    {
        /*<bind>*/F(1)/*</bind>*/;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            VerifyOperationTreeAndDiagnosticsForTest<SyntaxNode>(
                comp,
@"IInvocationOperation (void Program.F<System.Int32>(params System.Span<System.Int32> args)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F(1)')
  Instance Receiver:
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: args) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'F(1)')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'F(1)')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'F(1)')
          Initializer:
            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'F(1)')
              Element Values(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
",
                DiagnosticDescription.None);

            Assert.False(true); // PROTOTYPE: Add similar test for ReadOnlySpan<T>.
        }

        [Fact]
        public void NoMissingMembers()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void F1(params Span<object> args) { }
    static void F2(params ReadOnlySpan<object> args) { }
    static void Main()
    {
        F1();
        F2();
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MissingSpanConstructor()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        internal Span(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        internal ReadOnlySpan(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void F1(params Span<object> args) { }
    static void F2(params ReadOnlySpan<object> args) { }
    static void Main()
    {
        F1();
        F2();
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (8,9): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //         F1();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1()").WithArguments("System.Span`1", ".ctor").WithLocation(8, 9),
                // (9,9): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //         F2();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2()").WithArguments("System.Span`1", ".ctor").WithLocation(9, 9));
        }

        [Fact]
        public void MissingSpanGetItem()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void F1(params Span<object> args) { }
    static void F2(params ReadOnlySpan<object> args) { }
    static void Main()
    {
        F1();
        F2();
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (8,9): error CS0656: Missing compiler required member 'System.Span`1.get_Item'
                //         F1();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1()").WithArguments("System.Span`1", "get_Item").WithLocation(8, 9),
                // (9,9): error CS0656: Missing compiler required member 'System.Span`1.get_Item'
                //         F2();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2()").WithArguments("System.Span`1", "get_Item").WithLocation(9, 9));
        }

        [Fact]
        public void MissingSpanImplicitOperator()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
        public ref T this[int index] => ref _array[index];
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void F1(params Span<object> args) { }
    static void F2(params ReadOnlySpan<object> args) { }
    static void Main()
    {
        F1();
        F2();
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (9,9): error CS0656: Missing compiler required member 'System.Span`1.op_Implicit'
                //         F2();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2()").WithArguments("System.Span`1", "op_Implicit").WithLocation(9, 9));
        }

        /// <summary>
        /// params value cannot be returned from the method since that
        /// would prevent sharing repeated allocations at the call-site.
        /// </summary>
        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_01()
        {
            var source =
@"using System;
class Program
{
    static T[] F0<T>(params T[] x0)
    {
        return x0;
    }
    static Span<T> F1<T>(params Span<T> x1)
    {
        return x1;
    }
    static Span<T> F2<T>(Span<T> x2, params Span<T> y2)
    {
        return x2;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,16): error CS8999: Cannot use params 'x1' in this context because it may prevent reuse at the call-site
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "x1").WithArguments("x1").WithLocation(10, 16));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_02()
        {
            var source =
@"using System;
class Program
{
    static void F0<T>(out T[] x0, params T[] y0)
    {
        x0 = y0;
    }
    static void F1<T>(out ReadOnlySpan<T> x1, params ReadOnlySpan<T> y1)
    {
        x1 = y1;
    }
    static void F2<T>(out ReadOnlySpan<T> x2, ReadOnlySpan<T> y2, params ReadOnlySpan<T> z2)
    {
        x2 = y2;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,14): error CS8999: Cannot use params 'y1' in this context because it may prevent reuse at the call-site
                //         x1 = y1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y1").WithArguments("y1").WithLocation(10, 14));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_03()
        {
            var source =
@"using System;
class Program
{
    static Span<T> F1<T>(params Span<T> x1)
    {
        x1 = default;
        return x1;
    }
    static void F2<T>(out ReadOnlySpan<T> x2, params ReadOnlySpan<T> y2)
    {
        y2 = default;
        x2 = y2;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (7,16): error CS8999: Cannot use params 'x1' in this context because it may prevent reuse at the call-site
                //         return x1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "x1").WithArguments("x1").WithLocation(7, 16),
                // (12,14): error CS8999: Cannot use params 'y2' in this context because it may prevent reuse at the call-site
                //         x2 = y2;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y2").WithArguments("y2").WithLocation(12, 14));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_04()
        {
            var source =
@"using System;
class Program
{
    static Span<T> F1<T>(Span<T> x1, params Span<T> y1)
    {
        x1 = y1;
        return x1;
    }
    static Span<T> F2<T>(Span<T> x2, params Span<T> y2)
    {
        x2 = y2;
        x2 = default;
        return x2;
    }
    static void F3<T>(out ReadOnlySpan<T> x3, ReadOnlySpan<T> y3, params ReadOnlySpan<T> z3)
    {
        y3 = z3;
        x3 = y3;
    }
    static void F4<T>(out ReadOnlySpan<T> x4, ReadOnlySpan<T> y4, params ReadOnlySpan<T> z4)
    {
        y4 = z4;
        y4 = default;
        x4 = y4;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,14): error CS8999: Cannot use params 'y1' in this context because it may prevent reuse at the call-site
                //         x1 = y1;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y1").WithArguments("y1").WithLocation(6, 14),
                // (11,14): error CS8999: Cannot use params 'y2' in this context because it may prevent reuse at the call-site
                //         x2 = y2;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "y2").WithArguments("y2").WithLocation(11, 14),
                // (17,14): error CS8999: Cannot use params 'z3' in this context because it may prevent reuse at the call-site
                //         y3 = z3;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "z3").WithArguments("z3").WithLocation(17, 14),
                // (22,14): error CS8999: Cannot use params 'z4' in this context because it may prevent reuse at the call-site
                //         y4 = z4;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "z4").WithArguments("z4").WithLocation(22, 14));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_05()
        {
            var source =
@"using System;
abstract class A
{
    public abstract Span<T> F<T>(params Span<T> args);
}
class B : A
{
    public override Span<T> F<T>(params Span<T> args) => args;
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (8,58): error CS8999: Cannot use params 'args' in this context because it may prevent reuse at the call-site
                //     public override Span<T> F<T>(params Span<T> args) => args;
                Diagnostic(ErrorCode.ERR_EscapeParamsSpan, "args").WithArguments("args").WithLocation(8, 58));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_06()
        {
            var source =
@"using System;
interface I
{
    Span<T> F<T>(params Span<T> args);
}
class C : I
{
    public Span<T> F<T>(Span<T> args) => args;
}";
            // PROTOTYPE: Should report: error CS8999: Cannot use params 'args' in this context because it may prevent reuse at the call-site
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void CannotReturnSpan_07()
        {
            var source =
@"using System;
interface I
{
    Span<T> F<T>(params Span<T> args);
}
class C : I
{
    Span<T> I.F<T>(Span<T> args) => args;
}";
            // PROTOTYPE: Should report: error CS8999: Cannot use params 'args' in this context because it may prevent reuse at the call-site
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics();
        }

        /// <summary>
        /// Prefer params Span or ReadOnlySpan over params T[].
        /// </summary>
        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_01()
        {
            var sourceA =
@"using System;
public class A
{
    public static void F1(params object[] args) { throw new Exception(); }
    public static void F1(params Span<object> args) { foreach (var arg in args) Console.WriteLine(arg); }
    public static void F2(params object[] args) { throw new Exception(); }
    public static void F2(params ReadOnlySpan<object> args) { foreach (var arg in args) Console.WriteLine(arg); }
}";
            var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.NetCoreApp);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class B : A
{
    static void Main()
    {
        F1(1, 2, 3);
        F2(""hello"", ""world"");
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
3
hello
world
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_02()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(params T[] args) { Console.WriteLine(""F1<T>(params T[] args)""); }
    static void F1(params Span<object> args) { Console.WriteLine(""F1(params Span<object> args)""); }
    static void F2(params object[] args) { Console.WriteLine(""F2(params object[] args)""); }
    static void F2<T>(params Span<T> args) { Console.WriteLine(""F2<T>(params Span<T> args)""); }
    static void Main()
    {
        F1(1, 2, 3);
        F2(4, 5);
    }
}";
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F1<T>(params T[] args)
F2<T>(params Span<T> args)
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_03()
        {
            var source =
@"using System;
class Program
{
    static void F1(params ReadOnlySpan<object> args) { }
    static void F1(params Span<object> args) { }
    static void F2<T>(params ReadOnlySpan<T> args) { }
    static void F2<T>(params Span<T> args) { }
    static void Main()
    {
        F1(1, 2, 3);
        F2(""hello"", ""span"");
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(params ReadOnlySpan<object>)' and 'Program.F1(params Span<object>)'
                //         F1(1, 2, 3);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(params System.ReadOnlySpan<object>)", "Program.F1(params System.Span<object>)").WithLocation(10, 9),
                // (11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2<T>(params ReadOnlySpan<T>)' and 'Program.F2<T>(params Span<T>)'
                //         F2("hello", "span");
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2<T>(params System.ReadOnlySpan<T>)", "Program.F2<T>(params System.Span<T>)").WithLocation(11, 9));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void OverloadResolution_04()
        {
            var source =
@"using System;
class Program
{
    static void F1<T>(params ReadOnlySpan<T> args) { Console.WriteLine(""F1<T>(params ReadOnlySpan<T> args)""); }
    static void F1(params Span<object> args) { Console.WriteLine(""F1(params Span<object> args)""); }
    static void F2(params ReadOnlySpan<object> args) { Console.WriteLine(""F2(params ReadOnlySpan<object> args)""); }
    static void F2<T>(params Span<T> args) { Console.WriteLine(""F2<T>(params Span<T> args)""); }
    static void Main()
    {
        F1(1, 2, 3);
        F2(""hello"", ""world"");
    }
}";
            CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"F1<T>(params ReadOnlySpan<T> args)
F2<T>(params Span<T> args)
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void RepeatedCalls_01()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(params Span<T> args)
    {
        foreach (var arg in args) Console.WriteLine(arg);
    }
    static void Main()
    {
        F(1, 2);
        int offset = 2;
        while (offset < 15)
        {
            F(offset + 1, offset + 2);
            F(offset + 3, offset + 4, offset + 5);
            offset += 5;
        }
        F(offset + 1, offset + 2, offset + 3);
    }
}";
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"1
2
3
4
5
6
7
8
9
10
11
12
13
14
15
16
17
18
19
20
");
            // PROTOTYPE: If the same buffer is re-used across multiple calls, we
            // need to clear the buffer when leaving the scope of any particular
            // use to match users' expectations around GC for the elements.
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      206 (0xce)
  .maxstack  3
  .locals init (int V_0, //offset
                System.Span<int> V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.2
  IL_0003:  newarr     ""int""
  IL_0008:  call       ""System.Span<int>..ctor(int[])""
  IL_000d:  ldloca.s   V_1
  IL_000f:  ldc.i4.0
  IL_0010:  call       ""ref int System.Span<int>.this[int].get""
  IL_0015:  ldc.i4.1
  IL_0016:  stind.i4
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref int System.Span<int>.this[int].get""
  IL_001f:  ldc.i4.2
  IL_0020:  stind.i4
  IL_0021:  ldloc.1
  IL_0022:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0027:  ldc.i4.2
  IL_0028:  stloc.0
  IL_0029:  br.s       IL_0091
  IL_002b:  ldloca.s   V_1
  IL_002d:  ldc.i4.2
  IL_002e:  newarr     ""int""
  IL_0033:  call       ""System.Span<int>..ctor(int[])""
  IL_0038:  ldloca.s   V_1
  IL_003a:  ldc.i4.0
  IL_003b:  call       ""ref int System.Span<int>.this[int].get""
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.1
  IL_0042:  add
  IL_0043:  stind.i4
  IL_0044:  ldloca.s   V_1
  IL_0046:  ldc.i4.1
  IL_0047:  call       ""ref int System.Span<int>.this[int].get""
  IL_004c:  ldloc.0
  IL_004d:  ldc.i4.2
  IL_004e:  add
  IL_004f:  stind.i4
  IL_0050:  ldloc.1
  IL_0051:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0056:  ldloca.s   V_1
  IL_0058:  ldc.i4.3
  IL_0059:  newarr     ""int""
  IL_005e:  call       ""System.Span<int>..ctor(int[])""
  IL_0063:  ldloca.s   V_1
  IL_0065:  ldc.i4.0
  IL_0066:  call       ""ref int System.Span<int>.this[int].get""
  IL_006b:  ldloc.0
  IL_006c:  ldc.i4.3
  IL_006d:  add
  IL_006e:  stind.i4
  IL_006f:  ldloca.s   V_1
  IL_0071:  ldc.i4.1
  IL_0072:  call       ""ref int System.Span<int>.this[int].get""
  IL_0077:  ldloc.0
  IL_0078:  ldc.i4.4
  IL_0079:  add
  IL_007a:  stind.i4
  IL_007b:  ldloca.s   V_1
  IL_007d:  ldc.i4.2
  IL_007e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0083:  ldloc.0
  IL_0084:  ldc.i4.5
  IL_0085:  add
  IL_0086:  stind.i4
  IL_0087:  ldloc.1
  IL_0088:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_008d:  ldloc.0
  IL_008e:  ldc.i4.5
  IL_008f:  add
  IL_0090:  stloc.0
  IL_0091:  ldloc.0
  IL_0092:  ldc.i4.s   15
  IL_0094:  blt.s      IL_002b
  IL_0096:  ldloca.s   V_1
  IL_0098:  ldc.i4.3
  IL_0099:  newarr     ""int""
  IL_009e:  call       ""System.Span<int>..ctor(int[])""
  IL_00a3:  ldloca.s   V_1
  IL_00a5:  ldc.i4.0
  IL_00a6:  call       ""ref int System.Span<int>.this[int].get""
  IL_00ab:  ldloc.0
  IL_00ac:  ldc.i4.1
  IL_00ad:  add
  IL_00ae:  stind.i4
  IL_00af:  ldloca.s   V_1
  IL_00b1:  ldc.i4.1
  IL_00b2:  call       ""ref int System.Span<int>.this[int].get""
  IL_00b7:  ldloc.0
  IL_00b8:  ldc.i4.2
  IL_00b9:  add
  IL_00ba:  stind.i4
  IL_00bb:  ldloca.s   V_1
  IL_00bd:  ldc.i4.2
  IL_00be:  call       ""ref int System.Span<int>.this[int].get""
  IL_00c3:  ldloc.0
  IL_00c4:  ldc.i4.3
  IL_00c5:  add
  IL_00c6:  stind.i4
  IL_00c7:  ldloc.1
  IL_00c8:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_00cd:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void RepeatedCalls_02()
        {
            var source =
@"using System;
class Program
{
    static T ElementAt<T>(int index, params Span<T> args)
    {
        var value = args[index];
        Console.WriteLine(""ElementAt<{0}>({1}): {2}"", typeof(T), index, value);
        return value;
    }
    static void Main()
    {
        var value = ElementAt(
            0,
            ElementAt(1, 'a', 'b', 'c'),
            ElementAt(2, 'e', 'f', 'g'),
            'h');
        Console.WriteLine(value);
    }
}";
            // No buffer re-use.
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, verify: Verification.Skipped, expectedOutput:
@"ElementAt<System.Char>(1): b
ElementAt<System.Char>(2): g
ElementAt<System.Char>(0): b
b
");
            verifier.VerifyIL("Program.Main",
@"{
  // Code size      161 (0xa1)
  .maxstack  5
  .locals init (System.Span<char> V_0,
                System.Span<char> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.3
  IL_0004:  newarr     ""char""
  IL_0009:  call       ""System.Span<char>..ctor(char[])""
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref char System.Span<char>.this[int].get""
  IL_0016:  ldc.i4.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.3
  IL_001a:  newarr     ""char""
  IL_001f:  call       ""System.Span<char>..ctor(char[])""
  IL_0024:  ldloca.s   V_1
  IL_0026:  ldc.i4.0
  IL_0027:  call       ""ref char System.Span<char>.this[int].get""
  IL_002c:  ldc.i4.s   97
  IL_002e:  stind.i2
  IL_002f:  ldloca.s   V_1
  IL_0031:  ldc.i4.1
  IL_0032:  call       ""ref char System.Span<char>.this[int].get""
  IL_0037:  ldc.i4.s   98
  IL_0039:  stind.i2
  IL_003a:  ldloca.s   V_1
  IL_003c:  ldc.i4.2
  IL_003d:  call       ""ref char System.Span<char>.this[int].get""
  IL_0042:  ldc.i4.s   99
  IL_0044:  stind.i2
  IL_0045:  ldloc.1
  IL_0046:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_004b:  stind.i2
  IL_004c:  ldloca.s   V_0
  IL_004e:  ldc.i4.1
  IL_004f:  call       ""ref char System.Span<char>.this[int].get""
  IL_0054:  ldc.i4.2
  IL_0055:  ldloca.s   V_1
  IL_0057:  ldc.i4.3
  IL_0058:  newarr     ""char""
  IL_005d:  call       ""System.Span<char>..ctor(char[])""
  IL_0062:  ldloca.s   V_1
  IL_0064:  ldc.i4.0
  IL_0065:  call       ""ref char System.Span<char>.this[int].get""
  IL_006a:  ldc.i4.s   101
  IL_006c:  stind.i2
  IL_006d:  ldloca.s   V_1
  IL_006f:  ldc.i4.1
  IL_0070:  call       ""ref char System.Span<char>.this[int].get""
  IL_0075:  ldc.i4.s   102
  IL_0077:  stind.i2
  IL_0078:  ldloca.s   V_1
  IL_007a:  ldc.i4.2
  IL_007b:  call       ""ref char System.Span<char>.this[int].get""
  IL_0080:  ldc.i4.s   103
  IL_0082:  stind.i2
  IL_0083:  ldloc.1
  IL_0084:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0089:  stind.i2
  IL_008a:  ldloca.s   V_0
  IL_008c:  ldc.i4.2
  IL_008d:  call       ""ref char System.Span<char>.this[int].get""
  IL_0092:  ldc.i4.s   104
  IL_0094:  stind.i2
  IL_0095:  ldloc.0
  IL_0096:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_009b:  call       ""void System.Console.WriteLine(char)""
  IL_00a0:  ret
}");
        }
    }
}
