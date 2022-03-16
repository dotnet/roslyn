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
  // Code size      118 (0x76)
  .maxstack  4
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""object""
  IL_0006:  newobj     ""System.Span<object>..ctor(object[])""
  IL_000b:  call       ""void A.F1(params System.Span<object>)""
  IL_0010:  ldc.i4.3
  IL_0011:  newarr     ""object""
  IL_0016:  dup
  IL_0017:  ldc.i4.0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  stelem.ref
  IL_001f:  dup
  IL_0020:  ldc.i4.1
  IL_0021:  ldc.i4.2
  IL_0022:  box        ""int""
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.2
  IL_002a:  ldstr      ""hello""
  IL_002f:  stelem.ref
  IL_0030:  newobj     ""System.Span<object>..ctor(object[])""
  IL_0035:  call       ""void A.F1(params System.Span<object>)""
  IL_003a:  ldc.i4.0
  IL_003b:  newarr     ""object""
  IL_0040:  newobj     ""System.Span<object>..ctor(object[])""
  IL_0045:  call       ""System.ReadOnlySpan<object> System.Span<object>.op_Implicit(System.Span<object>)""
  IL_004a:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_004f:  ldc.i4.2
  IL_0050:  newarr     ""object""
  IL_0055:  dup
  IL_0056:  ldc.i4.0
  IL_0057:  ldstr      ""span""
  IL_005c:  stelem.ref
  IL_005d:  dup
  IL_005e:  ldc.i4.1
  IL_005f:  ldc.i4.3
  IL_0060:  box        ""int""
  IL_0065:  stelem.ref
  IL_0066:  newobj     ""System.Span<object>..ctor(object[])""
  IL_006b:  call       ""System.ReadOnlySpan<object> System.Span<object>.op_Implicit(System.Span<object>)""
  IL_0070:  call       ""void A.F2(params System.ReadOnlySpan<object>)""
  IL_0075:  ret
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
  // Code size       68 (0x44)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0016:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_001b:  ldc.i4.3
  IL_001c:  newarr     ""string""
  IL_0021:  dup
  IL_0022:  ldc.i4.0
  IL_0023:  ldstr      ""4""
  IL_0028:  stelem.ref
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldstr      ""5""
  IL_0030:  stelem.ref
  IL_0031:  dup
  IL_0032:  ldc.i4.2
  IL_0033:  ldstr      ""6""
  IL_0038:  stelem.ref
  IL_0039:  newobj     ""System.Span<string>..ctor(string[])""
  IL_003e:  call       ""void Program.F<string>(params System.Span<string>)""
  IL_0043:  ret
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
  // Code size       78 (0x4e)
  .maxstack  4
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0016:  call       ""System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)""
  IL_001b:  call       ""void Program.F<int>(params System.ReadOnlySpan<int>)""
  IL_0020:  ldc.i4.3
  IL_0021:  newarr     ""string""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldstr      ""4""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldstr      ""5""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldstr      ""6""
  IL_003d:  stelem.ref
  IL_003e:  newobj     ""System.Span<string>..ctor(string[])""
  IL_0043:  call       ""System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)""
  IL_0048:  call       ""void Program.F<string>(params System.ReadOnlySpan<string>)""
  IL_004d:  ret
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
        /*<bind>*/F(1, 2)/*</bind>*/;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            VerifyOperationTreeAndDiagnosticsForTest<SyntaxNode>(
                comp,
@"IInvocationOperation (void Program.F<System.Int32>(params System.Span<System.Int32> args)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F(1, 2)')
  Instance Receiver:
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: args) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'F(1, 2)')
        IObjectCreationOperation (Constructor: System.Span<System.Int32>..ctor(System.Int32[]? array)) (OperationKind.ObjectCreation, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'F(1, 2)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'F(1, 2)')
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'F(1, 2)')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'F(1, 2)')
                  Initializer:
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'F(1, 2)')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer:
            null
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
",
                DiagnosticDescription.None);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void IArgumentOperation_02()
        {
            var source =
@"using System;
class A
{
    public object this[params ReadOnlySpan<int> args] => null;
}
class B
{
    public object this[params ReadOnlySpan<int> args] { set { } }
}
class Program
{
    static void Main()
    {
        var a = new A();
        var b = new B();
        /*<bind>*/b[2] = a[1]/*</bind>*/;
    }
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            VerifyOperationTreeAndDiagnosticsForTest<SyntaxNode>(
                comp,
@"ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'b[2] = a[1]')
  Left:
    IPropertyReferenceOperation: System.Object B.this[params System.ReadOnlySpan<System.Int32> args] { set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'b[2]')
      Instance Receiver:
        ILocalReferenceOperation: b (OperationKind.LocalReference, Type: B) (Syntax: 'b')
      Arguments(1):
          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: args) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b[2]')
            IInvocationOperation (System.ReadOnlySpan<System.Int32> System.Span<System.Int32>.op_Implicit(System.Span<System.Int32> span)) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'b[2]')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: span) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b[2]')
                    IObjectCreationOperation (Constructor: System.Span<System.Int32>..ctor(System.Int32[]? array)) (OperationKind.ObjectCreation, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'b[2]')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b[2]')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'b[2]')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b[2]')
                              Initializer:
                                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'b[2]')
                                  Element Values(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Right:
    IPropertyReferenceOperation: System.Object A.this[params System.ReadOnlySpan<System.Int32> args] { get; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'a[1]')
      Instance Receiver:
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: A) (Syntax: 'a')
      Arguments(1):
          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: args) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a[1]')
            IInvocationOperation (System.ReadOnlySpan<System.Int32> System.Span<System.Int32>.op_Implicit(System.Span<System.Int32> span)) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'a[1]')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: span) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a[1]')
                    IObjectCreationOperation (Constructor: System.Span<System.Int32>..ctor(System.Int32[]? array)) (OperationKind.ObjectCreation, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'a[1]')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a[1]')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'a[1]')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a[1]')
                              Initializer:
                                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'a[1]')
                                  Element Values(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
",
                DiagnosticDescription.None);
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
            comp.VerifyEmitDiagnostics();
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

        [Fact]
        public void UnexpectedSpanType_01()
        {
            var sourceA =
@"namespace System
{
    public class Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public ref T this[int index] => throw null;
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public class ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
        public ref T this[int index] => throw null;
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void F1<T>(params Span<T> args) { }
    static void F2<T>(params ReadOnlySpan<T> args) { }
    static void Main()
    {
        F1(1, 2);
        F2(string.Empty);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe);
            // PROTOTYPE: Should report errors: expecting 'ref struct'.
            comp.VerifyEmitDiagnostics();
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
  // Code size      134 (0x86)
  .maxstack  5
  .locals init (int V_0) //offset
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  dup
  IL_000b:  ldc.i4.1
  IL_000c:  ldc.i4.2
  IL_000d:  stelem.i4
  IL_000e:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0013:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0018:  ldc.i4.2
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_005e
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""int""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stelem.i4
  IL_0028:  dup
  IL_0029:  ldc.i4.1
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.2
  IL_002c:  add
  IL_002d:  stelem.i4
  IL_002e:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0033:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0038:  ldc.i4.3
  IL_0039:  newarr     ""int""
  IL_003e:  dup
  IL_003f:  ldc.i4.0
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.3
  IL_0042:  add
  IL_0043:  stelem.i4
  IL_0044:  dup
  IL_0045:  ldc.i4.1
  IL_0046:  ldloc.0
  IL_0047:  ldc.i4.4
  IL_0048:  add
  IL_0049:  stelem.i4
  IL_004a:  dup
  IL_004b:  ldc.i4.2
  IL_004c:  ldloc.0
  IL_004d:  ldc.i4.5
  IL_004e:  add
  IL_004f:  stelem.i4
  IL_0050:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0055:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_005a:  ldloc.0
  IL_005b:  ldc.i4.5
  IL_005c:  add
  IL_005d:  stloc.0
  IL_005e:  ldloc.0
  IL_005f:  ldc.i4.s   15
  IL_0061:  blt.s      IL_001c
  IL_0063:  ldc.i4.3
  IL_0064:  newarr     ""int""
  IL_0069:  dup
  IL_006a:  ldc.i4.0
  IL_006b:  ldloc.0
  IL_006c:  ldc.i4.1
  IL_006d:  add
  IL_006e:  stelem.i4
  IL_006f:  dup
  IL_0070:  ldc.i4.1
  IL_0071:  ldloc.0
  IL_0072:  ldc.i4.2
  IL_0073:  add
  IL_0074:  stelem.i4
  IL_0075:  dup
  IL_0076:  ldc.i4.2
  IL_0077:  ldloc.0
  IL_0078:  ldc.i4.3
  IL_0079:  add
  IL_007a:  stelem.i4
  IL_007b:  newobj     ""System.Span<int>..ctor(int[])""
  IL_0080:  call       ""void Program.F<int>(params System.Span<int>)""
  IL_0085:  ret
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
  // Code size       90 (0x5a)
  .maxstack  8
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""char""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.3
  IL_000b:  newarr     ""char""
  IL_0010:  dup
  IL_0011:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6 <PrivateImplementationDetails>.13E228567E8249FCE53337F25D7970DE3BD68AB2653424C7B8F9FD05E33CAEDF""
  IL_0016:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_001b:  newobj     ""System.Span<char>..ctor(char[])""
  IL_0020:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0025:  stelem.i2
  IL_0026:  dup
  IL_0027:  ldc.i4.1
  IL_0028:  ldc.i4.2
  IL_0029:  ldc.i4.3
  IL_002a:  newarr     ""char""
  IL_002f:  dup
  IL_0030:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=6 <PrivateImplementationDetails>.E1740606478DD08DD0D1888BBD631744F34BCB178606E98B24C03612E87F801C""
  IL_0035:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_003a:  newobj     ""System.Span<char>..ctor(char[])""
  IL_003f:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0044:  stelem.i2
  IL_0045:  dup
  IL_0046:  ldc.i4.2
  IL_0047:  ldc.i4.s   104
  IL_0049:  stelem.i2
  IL_004a:  newobj     ""System.Span<char>..ctor(char[])""
  IL_004f:  call       ""char Program.ElementAt<char>(int, params System.Span<char>)""
  IL_0054:  call       ""void System.Console.WriteLine(char)""
  IL_0059:  ret
}");
        }
    }
}
