// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenMethodGroupConversionCachingTests : CSharpTestBase
{
    const string PASS = "PASS";

    [Fact]
    public void Not_DelegateCreations_Static()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        Invoke(new D(Target), new D(Target));
    }

    static void Target() { Console.WriteLine(""FAIL""); }
    static void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Target()""
  IL_0007:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_000c:  ldnull
  IL_000d:  ldftn      ""void C.Target()""
  IL_0013:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0018:  call       ""void C.Invoke(D, D)""
  IL_001d:  ret
}
");
    }

    [Fact]
    public void Not_DelegateCreations_Instance()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(c.Target), new D(c.Target));
    }

    void Target() { Console.WriteLine(""FAIL""); }
    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""void C.Target()""
  IL_000e:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void C.Target()""
  IL_001a:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void C.Invoke(D, D)""
  IL_0024:  ret
}
");
    }

    [Fact]
    public void Not_Conversions_Instance()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(c.Target, c.Target);
    }

    void Target() { Console.WriteLine(""FAIL""); }
    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""void C.Target()""
  IL_000e:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void C.Target()""
  IL_001a:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void C.Invoke(D, D)""
  IL_0024:  ret
}
");
    }

    [Fact]
    public void Not_DelegateCreations_InstanceExtensionMethod()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(c.Target), new D(c.Target));
    }

    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}
static class E
{
    public static void Target(this C that) { Console.WriteLine(""FAIL""); }
}
";
        // ILVerify: Unrecognized arguments for delegate .ctor. { Offset = 14 }
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"), verify: Verification.FailsILVerify);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""void E.Target(C)""
  IL_000e:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void E.Target(C)""
  IL_001a:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void C.Invoke(D, D)""
  IL_0024:  ret
}
");
    }

    [Fact]
    public void Not_Conversions_InstanceExtensionMethod()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(c.Target, c.Target);
    }

    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}
static class E
{
    public static void Target(this C that) { Console.WriteLine(""FAIL""); }
}
";
        // ILVerify: Unrecognized arguments for delegate .ctor. { Offset = 14 }
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"), verify: Verification.FailsILVerify);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""void E.Target(C)""
  IL_000e:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void E.Target(C)""
  IL_001a:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void C.Invoke(D, D)""
  IL_0024:  ret
}
");
    }

    [Fact]
    public void Not_DelegateCreations_StaticExtensionMethod()
    {
        var source = @"
using System;
delegate void D(C arg);
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(E.Target), new D(E.Target));
    }

    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}
static class E
{
    public static void Target(this C that) { Console.WriteLine(""FAIL""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  4
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldnull
  IL_0006:  ldftn      ""void E.Target(C)""
  IL_000c:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""void E.Target(C)""
  IL_0018:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_001d:  callvirt   ""void C.Invoke(D, D)""
  IL_0022:  ret
}
");
    }

    [Fact]
    public void Not_InExpressionLambda0()
    {
        var source = @"
using System;
using System.Linq.Expressions;
class C
{
    public static void Main(string[] args)
    {
        Expression<Func<int, Func<int, int>>> e = x => Target;
        Console.WriteLine(e);
    }

    static int Target(int x) => 0;
}
";
        var verifier = CompileAndVerify(source
#if NETFRAMEWORK
            , expectedOutput: "x => Convert(Int32 Target(Int32).CreateDelegate(System.Func`2[System.Int32,System.Int32], null))"
#else
            , expectedOutput: "x => Convert(Int32 Target(Int32).CreateDelegate(System.Func`2[System.Int32,System.Int32], null), Func`2)"
#endif
            , symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.Main", @"
{
  // Code size      160 (0xa0)
  .maxstack  7
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldtoken    ""int C.Target(int)""
  IL_001a:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_001f:  castclass  ""System.Reflection.MethodInfo""
  IL_0024:  ldtoken    ""System.Reflection.MethodInfo""
  IL_0029:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002e:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0033:  ldtoken    ""System.Delegate System.Reflection.MethodInfo.CreateDelegate(System.Type, object)""
  IL_0038:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_003d:  castclass  ""System.Reflection.MethodInfo""
  IL_0042:  ldc.i4.2
  IL_0043:  newarr     ""System.Linq.Expressions.Expression""
  IL_0048:  dup
  IL_0049:  ldc.i4.0
  IL_004a:  ldtoken    ""System.Func<int, int>""
  IL_004f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0054:  ldtoken    ""System.Type""
  IL_0059:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005e:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0063:  stelem.ref
  IL_0064:  dup
  IL_0065:  ldc.i4.1
  IL_0066:  ldnull
  IL_0067:  ldtoken    ""object""
  IL_006c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0071:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0076:  stelem.ref
  IL_0077:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_007c:  ldtoken    ""System.Func<int, int>""
  IL_0081:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0086:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_008b:  ldc.i4.1
  IL_008c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0091:  dup
  IL_0092:  ldc.i4.0
  IL_0093:  ldloc.0
  IL_0094:  stelem.ref
  IL_0095:  call       ""System.Linq.Expressions.Expression<System.Func<int, System.Func<int, int>>> System.Linq.Expressions.Expression.Lambda<System.Func<int, System.Func<int, int>>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_009a:  call       ""void System.Console.WriteLine(object)""
  IL_009f:  ret
}
");
    }

    [Fact]
    public void Not_InExpressionLambda1()
    {
        var source = @"
using System;
using System.Linq.Expressions;
class C
{
    public static void Main(string[] args)
    {
        Func<int, Expression<Func<int, Func<int, int>>>> f = x => y => Target;
        Console.WriteLine(f(0));
    }

    static int Target(int x) => 0;
}
";
        var verifier = CompileAndVerify(source
#if NETFRAMEWORK
            , expectedOutput: "y => Convert(Int32 Target(Int32).CreateDelegate(System.Func`2[System.Int32,System.Int32], null))"
#else
            , expectedOutput: "y => Convert(Int32 Target(Int32).CreateDelegate(System.Func`2[System.Int32,System.Int32], null), Func`2)"
#endif
            , symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size      155 (0x9b)
  .maxstack  7
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""y""
  IL_000f:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0014:  stloc.0
  IL_0015:  ldtoken    ""int C.Target(int)""
  IL_001a:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_001f:  castclass  ""System.Reflection.MethodInfo""
  IL_0024:  ldtoken    ""System.Reflection.MethodInfo""
  IL_0029:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002e:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0033:  ldtoken    ""System.Delegate System.Reflection.MethodInfo.CreateDelegate(System.Type, object)""
  IL_0038:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_003d:  castclass  ""System.Reflection.MethodInfo""
  IL_0042:  ldc.i4.2
  IL_0043:  newarr     ""System.Linq.Expressions.Expression""
  IL_0048:  dup
  IL_0049:  ldc.i4.0
  IL_004a:  ldtoken    ""System.Func<int, int>""
  IL_004f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0054:  ldtoken    ""System.Type""
  IL_0059:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005e:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0063:  stelem.ref
  IL_0064:  dup
  IL_0065:  ldc.i4.1
  IL_0066:  ldnull
  IL_0067:  ldtoken    ""object""
  IL_006c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0071:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0076:  stelem.ref
  IL_0077:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_007c:  ldtoken    ""System.Func<int, int>""
  IL_0081:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0086:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_008b:  ldc.i4.1
  IL_008c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0091:  dup
  IL_0092:  ldc.i4.0
  IL_0093:  ldloc.0
  IL_0094:  stelem.ref
  IL_0095:  call       ""System.Linq.Expressions.Expression<System.Func<int, System.Func<int, int>>> System.Linq.Expressions.Expression.Lambda<System.Func<int, System.Func<int, int>>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_009a:  ret
}
");
    }

    [Fact]
    public void Not_InStaticConstructor0()
    {
        var source = @"
using System;
class C
{
    static readonly Action ManualCache = Target;
    static void Target() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C..cctor", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Target()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  stsfld     ""System.Action C.ManualCache""
  IL_0011:  ret
}
");
    }

    [Fact]
    public void Not_InStaticConstructor1()
    {
        var source = @"
using System;
struct C
{
    static readonly Action ManualCache;
    static void Target() { }

    static C()
    {
        ManualCache = Target;
    }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyNoCacheContainersIn("C"));
        verifier.VerifyIL("C..cctor", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Target()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  stsfld     ""System.Action C.ManualCache""
  IL_0011:  ret
}
");
    }

    [Fact]
    public void Not_TargetTypedNew0()
    {
        var source = @"
using System;

Action f = new(Target);
f();

static void Target() { Console.WriteLine(""PASS""); }
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("Program"));
        verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.<<Main>$>g__Target|0_0()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  callvirt   ""void System.Action.Invoke()""
  IL_0011:  ret
}
");
    }

    [Fact]
    public void Not_TargetTypedNew1()
    {
        var source = @"
#nullable enable
using System;

Action? f = new(Target);
f();

static void Target() { Console.WriteLine(""PASS""); }
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyNoCacheContainersIn("Program"));
        verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.<<Main>$>g__Target|0_0()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  callvirt   ""void System.Action.Invoke()""
  IL_0011:  ret
}
");
    }

    [Fact]
    public void Not_CSharp10()
    {
        var source = @"
var f = Target;
f();
static void Target() { }
";
        var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular10, symbolValidator: VerifyNoCacheContainersIn("Program"));
        verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Program.<<Main>$>g__Target|0_0()""
  IL_0007:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000c:  callvirt   ""void System.Action.Invoke()""
  IL_0011:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test((Action)Target);
    }

    static void Test(Action action)
    {
        action();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  call       ""void C.Test(System.Action)""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        var d = new D<int>();
        d.Test()();
    }

    public static void Target() { Console.WriteLine(""PASS""); }
}
class D<T>
{
    public Action Test()
    {
        return (Action)C.Target;
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<int>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action<int>)E<int>.Target;
        t(0);
    }
}
class E<V>
{
    public static void Target(V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action<int>)E<int>.Target<double>;
        t(0);
    }
}
class E<V>
{
    public static void Target<K>(V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<int, int>)E<int>.Target<double>;
        t(0);
    }
}
class E<V>
{
    public static V Target<K>(V v) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""int System.Func<int, int>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target<int>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target<T>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<T>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<T>.Target<double>;
        t();
    }
}
class E<V>
{
    public static void Target<N>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<double>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<T>)E<int>.Target<T>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<int>.Target<T>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped6()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<int>)E<T>.Target<int>;
        t();
    }
}
class E<K>
{
    public static V Target<V>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped7()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<T, int>)E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int System.Func<T, int>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D.Test<int>();
    }

    public static void Target<K>() { Console.WriteLine(""PASS""); }
}
class D
{
    public static void Test<T>()
    {
        var t = (Action)C.Target<T>;
        t();
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }
}
class D
{
    public void Test<T>()
    {
        var t = (Action)E<T>.Target;
        t();
    }
}
class E<K>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }
}
class D
{
    public void Test<T>()
    {
        var t = (Func<T, int>)E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int System.Func<T, int>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<long>()).Test<int>();
    }
}
class D<M>
{
    public void Test<T>()
    {
        var t = (Action<M, T>)E<M>.Target<T>;
        t(default(M), default(T));
    }
}
class E<K>
{
    public static void Target<V>(K k, V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<M>.Test<T>", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (M V_0,
                T V_1)
  IL_0000:  ldsfld     ""System.Action<M, T> D<M>.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<M>.Target<T>(M, T)""
  IL_0010:  newobj     ""System.Action<M, T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<M, T> D<M>.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""M""
  IL_0023:  ldloc.0
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""T""
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""void System.Action<M, T>.Invoke(M, T)""
  IL_0032:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
delegate void MyAction<T>();
class C
{
    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public delegate void MyAction<T>();

    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void C.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped0()
    {
        var source = @"
using System;
delegate void MyAction();
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        MyAction t = Target;
        t();
    }

    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void MyAction.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test()();
    }
}
class D<T>
{
    public delegate void MyAction();

    public MyAction Test()
    {
        return Target<int>;
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<int> t = Target<T>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<int> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<T> t = E<T>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<T>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<T> t = E<T>.Target<int>;
        t();
    }
}
class E<V>
{
    public static void Target<N>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<T>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate T MyFunc();

    public void Test()
    {
        MyFunc t = E<T>.Target<T, int>;
        t();
    }
}
class E<K>
{
    public static V Target<V, K>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<T>.Target<T, int>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T D<T>.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped6()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate int MyFunc(T i);

    public void Test()
    {
        MyFunc t = E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int D<T>.MyFunc.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped7()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int, double>()).Test();
    }
}
class D<T, M>
{
    delegate T MyFunc(M m);

    public void Test()
    {
        MyFunc t = E<M>.Target<T>;
        t(default(M));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T, M>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (M V_0)
  IL_0000:  ldsfld     ""D<T, M>.MyFunc D<T, M>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<M>.Target<T>(M)""
  IL_0010:  newobj     ""D<T, M>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T, M>.MyFunc D<T, M>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""M""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""T D<T, M>.MyFunc.Invoke(M)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped8()
    {
        var source = @"
using System;
class C
{
    delegate void MyAction<T>();

    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void C.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }

    public static void Target<K>() { Console.WriteLine(""PASS""); }
}
class D
{
    delegate void MyAction();

    public void Test<T>()
    {
        MyAction t = C.Target<T>;
        t();
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyAction D.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""D.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyAction D.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void D.MyAction.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test<int>();
    }

    public static void Target<K, N>(K k) { Console.WriteLine(""PASS""); }
}
class D<V>
{
    delegate void MyAction<M>(M m);

    public void Test<T>()
    {
        MyAction<V> t = C.Target<V, T>;
        t(default(V));
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<V>.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (V V_0)
  IL_0000:  ldsfld     ""D<V>.MyAction<V> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<V, T>(V)""
  IL_0010:  newobj     ""D<V>.MyAction<V>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyAction<V> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""V""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void D<V>.MyAction<V>.Invoke(V)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<byte>()).Test<int>();
    }

    public static N Target<K, N>(K k, N n) { Console.WriteLine(""PASS""); return default(N); }
}
class D<V>
{
    delegate V MyFunc<M>(M m, V v);

    public void Test<T>()
    {
        MyFunc<T> t = C.Target<T, V>;
        t(default(T), default(V));
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<V>.Test<T>", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (T V_0,
                V V_1)
  IL_0000:  ldsfld     ""D<V>.MyFunc<T> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""V C.Target<T, V>(T, V)""
  IL_0010:  newobj     ""D<V>.MyFunc<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyFunc<T> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""V""
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""V D<V>.MyFunc<T>.Invoke(T, V)""
  IL_0032:  pop
  IL_0033:  ret
}
");
    }

    [Fact]
    public void Where_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<C>()).Test();
    }
}
class D<T> where T : C
{
    delegate void MyAction(T t);

    public void Test()
    {
        MyAction t = E<int>.Target<C>;
        t(null);
    }
}
class E<V>
{
    public static void Target<N>(N n) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<C>(C)""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void D<T>.MyAction.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<C>()).Test(new C());
    }
}
class D<T> where T : C
{
    delegate T MyFunc();

    public void Test<M>(M m) where M : T
    {
        MyFunc t = E<int>.Target<M>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("D");
            var container = testClass.GetTypeMember("<Test>O__1_0");
            AssertEx.NotNull(container);

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            Assert.Equal(1, m.ConstraintTypes().Length);
            Assert.Equal(testClass.TypeParameters[0], m.ConstraintTypes()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D<T>.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<Test>O__1_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<Test>O__1_0<M>.<0>__Target""
  IL_001b:  callvirt   ""T D<T>.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test(new C());
    }
}
class D
{
    delegate C MyFunc();

    public void Test<M>(M m) where M : C
    {
        MyFunc t = E<int>.Target<M>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var globalNs = module.GlobalNamespace;
            var mainClass = globalNs.GetTypeMember("C");
            var container = globalNs.GetMember<NamedTypeSymbol>("D.<Test>O__1_0");
            AssertEx.NotNull(container);

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            Assert.Equal(1, m.ConstraintTypes().Length);
            Assert.Equal(mainClass, m.ConstraintTypes()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyFunc D.<Test>O__1_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyFunc D.<Test>O__1_0<M>.<0>__Target""
  IL_001b:  callvirt   ""C D.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test(0);
    }
}
class D
{
    public void Test<M>(M m) where M : struct
    {
        Func<M?> t = E.Target<M?>;
        t();
    }
}
class E
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D.<Test>O__0_0");
            AssertEx.NotNull(container);

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            AssertEx.NotNull(m);
            Assert.True(m.IsValueType);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<M?> D.<Test>O__0_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M? E.Target<M?>()""
  IL_0010:  newobj     ""System.Func<M?>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<M?> D.<Test>O__0_0<M>.<0>__Target""
  IL_001b:  callvirt   ""M? System.Func<M?>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        var t = (Action<C>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action<C> t = E.Target;
        t(null);
    }
}
static class E
{
    public static void Target<T>(this T t) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<C>(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped2()
    {
        var source = @"
using System;
static class E
{
    static void Test()
    {
        Action<int> t = Target<int>;
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> E.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<int>(int)""
  IL_000e:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<int> E.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D<C>.Test();
    }
}
class D<T> where T : C
{
    public static void Test()
    {
        var t = (Action<T>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D<int>.Test(0);
    }
}
class D<K>
{
    public static void Test(K k)
    {
        Action<K> t = E.Target;
        t(k);
    }
}
static class E
{
    public static void Target<T>(this T t) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<K>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<K> D<K>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<K>(K)""
  IL_0010:  newobj     ""System.Action<K>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<K> D<K>.<>O.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""void System.Action<K>.Invoke(K)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped2()
    {
        var source = @"
using System;
static class E
{
    class F<T>
    {
        void Test()
        {
            Action<T> t = Target<T>;
        }
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.F<T>.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.F<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.F<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test(0);
    }

    static void Test<T>(T arg)
    {
        var t = (Action<T>)E.Target;
        t(arg);
    }
}
static class E
{
    public static void Target<M>(this M m) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<T>(T)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test<C>();
    }

    static void Test<T>() where T : C
    {
        var t = (Action<T>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped2()
    {
        var source = @"
using System;
static class E
{
    static void Test<T>()
    {
        Action<T> t = Target<T>;
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.Test<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action test = () => Invoke(Target);
        test();
    }

    static void Invoke(Action a) => a();

    static void Target() => Console.WriteLine(""PASS"");
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  call       ""void C.Invoke(System.Action)""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action test = () => ((Action)D.Target)();
        test();
    }
}
class D
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test();
    }
}
class D<T>
{
    public static void Test()
    {
        Action test = () =>
        {
            ((Func<T>)Target)();
        };
        test();
    }

    static T Target() { Console.WriteLine(""PASS""); return default(T); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test();
    }
}
class D<T>
{
    delegate T MyFunc();    
    public static void Test()
    {
        Func<MyFunc> a = () => E.Target<T>;
        a()();
    }
}
class E
{
    public static V Target<V>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c.<Test>b__1_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void Lambda_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test(0);
    }
}
class D<T>
{
    public static void Test<G>(G g)
    {
        Action test = () =>
        {
            ((Func<T>)Target<G>)();
        };
        test();
    }

    static T Target<K>() { Console.WriteLine(""PASS""); return default(T); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target<G>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<Test>O__0_0<G>.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Lambda_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D.Test(0);
    }
}
class D
{
    public static void Test<G>(G g)
    {
        Func<Func<G>> a = () => E<G>.Target;
        a()();
    }
}
class E<V>
{
    public static V Target() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<G> D.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""G E<G>.Target()""
  IL_0010:  newobj     ""System.Func<G>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<G> D.<Test>O__0_0<G>.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    void Test0() { var t = (Action)D.Target<T>; }
    void Test1() { Action t = D.Target<T>; }
}
class D
{
    public static void Target<V>() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "System.Action <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C<T>.Test1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped1()
    {
        var source = @"
using System;
class C<T>
{
    void Test0() { var t = (Func<T>)Target<T>; }
    void Test1() { Func<T> t = Target<T>; }
    static V Target<V>() { return default(V); }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "System.Func<T> <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T>.Target<T>()""
  IL_000e:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C<T>.Test1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T>.Target<T>()""
  IL_000e:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped2()
    {
        var source = @"
using System;
class C<T, V>
{
    delegate T MyFunc();
    void Test0() { var t = (MyFunc)Target; }
    void Test1() { MyFunc t = Target; }
    static T Target() { return default(T); }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "C<T, V>.MyFunc <0>__Target"
        ));
        verifier.VerifyIL("C<T, V>.Test0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""C<T, V>.MyFunc C<T, V>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T, V>.Target()""
  IL_000e:  newobj     ""C<T, V>.MyFunc..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""C<T, V>.MyFunc C<T, V>.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C<T, V>.Test1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""C<T, V>.MyFunc C<T, V>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T, V>.Target()""
  IL_000e:  newobj     ""C<T, V>.MyFunc..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""C<T, V>.MyFunc C<T, V>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped3_CLRSignature()
    {
        var source = @"
#nullable enable
using System;
class C<T>
{
    void Test0() { var t = (Func<T?>)Target<T?>; }
    void Test1() { Func<T> t = Target<T>; }
    static V Target<V>() { return default(V); }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "System.Func<T?> <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T>.Target<T>()""
  IL_000e:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C<T>.Test1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""T C<T>.Target<T>()""
  IL_000e:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped4_CLRSignature()
    {
        var source = @"
using System;
class C<T>
{
    void Test0() { var t = (Func<(T x, T y)>)Target<(T x, T y)>; }
    void Test1() { Func<(T a, T b)> t = Target<(T c, T d)>; }
    static V Target<V>() { return default(V); }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "System.Func<(T x, T y)> <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, T> C<T>.Target<System.ValueTuple<T, T>>()""
  IL_000e:  newobj     ""System.Func<System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C<T>.Test1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, T> C<T>.Target<System.ValueTuple<T, T>>()""
  IL_000e:  newobj     ""System.Func<System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped5_AnonymousDelegate()
    {
        var source = @"
class C
{
    void Test0<T>(T t) { G0(Target<int>); }
    void Test1<T>(T t) { G1(Target<int>); }

    void G0(System.Delegate d) { }
    void G1(System.Delegate d) { }

    static dynamic Target<G>(ref G g) => 0;
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "<>F{00000001}<System.Int32, System.Object> <0>__Target"
        ));
        verifier.VerifyIL("C.Test0<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<int>(ref int)""
  IL_0011:  newobj     ""<>F{00000001}<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_001c:  call       ""void C.G0(System.Delegate)""
  IL_0021:  ret
}
");
        verifier.VerifyIL("C.Test1<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<int>(ref int)""
  IL_0011:  newobj     ""<>F{00000001}<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_001c:  call       ""void C.G1(System.Delegate)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<V>()
    {
        var t0 = (Action)D.Target<V>;
        Action t1 = D.Target<V>;
    }
}
class D
{
    public static void Target<B>() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__0_0", arity: 1
            , "System.Action <0>__Target"
        ));
        verifier.VerifyIL("C.Test<V>", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Test>O__0_0<V>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D.Target<V>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Test>O__0_0<V>.<0>__Target""
  IL_0018:  ldsfld     ""System.Action C.<Test>O__0_0<V>.<0>__Target""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void D.Target<V>()""
  IL_0026:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action C.<Test>O__0_0<V>.<0>__Target""
  IL_0030:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped1()
    {
        var source = @"
using System;
class C<T>
{
    void Test<V>()
    {
        var t0 = (Func<T, V>)D<V>.Target<T>;
        Func<T, V> t1 = D<V>.Target<T>;
    }
}
class D<B>
{
    public static B Target<H>(H h) => default(B);
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__0_0", arity: 1
            , "System.Func<T, V> <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test<V>", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""V D<V>.Target<T>(T)""
  IL_000e:  newobj     ""System.Func<T, V>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0018:  ldsfld     ""System.Func<T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""V D<V>.Target<T>(T)""
  IL_0026:  newobj     ""System.Func<T, V>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Func<T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0030:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped2()
    {
        var source = @"
using System;
class C<A, T>
{
    delegate O MyFunc(int num);
    class O { }
    void Test<V>() where V : O
    {
        var t0 = (MyFunc)D.Target<V>;
        MyFunc t1 = D.Target<V>;
    }
}
static class D
{
    public static B Target<B>(this int num) => default(B);
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__2_0", arity: 1
            , "C<A, T>.MyFunc <0>__Target"
        ));
        verifier.VerifyIL("C<A, T>.Test<V>", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""C<A, T>.MyFunc C<A, T>.<Test>O__2_0<V>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""V D.Target<V>(int)""
  IL_000e:  newobj     ""C<A, T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""C<A, T>.MyFunc C<A, T>.<Test>O__2_0<V>.<0>__Target""
  IL_0018:  ldsfld     ""C<A, T>.MyFunc C<A, T>.<Test>O__2_0<V>.<0>__Target""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""V D.Target<V>(int)""
  IL_0026:  newobj     ""C<A, T>.MyFunc..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""C<A, T>.MyFunc C<A, T>.<Test>O__2_0<V>.<0>__Target""
  IL_0030:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped3_CLRSignature()
    {
        var source = @"
using System;
class C<T>
{
    void Test<V>()
    {
        var t0 = (Func<object, T, V>)D<V>.Target<T>;
        Func<dynamic, T, V> t1 = D<V>.Target<T>;
    }
}
class D<B>
{
    public static B Target<H>(dynamic o, H h) => default(B);
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__0_0", arity: 1
            , "System.Func<System.Object, T, V> <0>__Target"
        ));
        verifier.VerifyIL("C<T>.Test<V>", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<object, T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""V D<V>.Target<T>(dynamic, T)""
  IL_000e:  newobj     ""System.Func<object, T, V>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<object, T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0018:  ldsfld     ""System.Func<object, T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""V D<V>.Target<T>(dynamic, T)""
  IL_0026:  newobj     ""System.Func<dynamic, T, V>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Func<object, T, V> C<T>.<Test>O__0_0<V>.<0>__Target""
  IL_0030:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped4_AnonymousDelegate()
    {
        var source = @"
class C
{
    void Test<T>(T t)
    {
        G0(Target<T>);
        G1(Target<T>);
    }

    void G0(System.Delegate d) { }
    void G1(System.Delegate d) { }

    static dynamic Target<G>(ref G g) => 0;
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__0_0", arity: 1
            , "<>F{00000001}<T, System.Object> <0>__Target"
        ));
        verifier.VerifyIL("C.Test<T>", @"
{
  // Code size       67 (0x43)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<T>(ref T)""
  IL_0011:  newobj     ""<>F{00000001}<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_001c:  call       ""void C.G0(System.Delegate)""
  IL_0021:  ldarg.0
  IL_0022:  ldsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_0027:  dup
  IL_0028:  brtrue.s   IL_003d
  IL_002a:  pop
  IL_002b:  ldnull
  IL_002c:  ldftn      ""dynamic C.Target<T>(ref T)""
  IL_0032:  newobj     ""<>F{00000001}<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0037:  dup
  IL_0038:  stsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_003d:  call       ""void C.G1(System.Delegate)""
  IL_0042:  ret
}
");
    }

    [Fact]
    public void ContainersCanBeShared_TypeScoped0()
    {
        var source = @"
using System;
class A<T>
{
    class B<V>
    {
        void Test0()
        {
            Action<T> t0 = Target0;
            Action<T> t1 = Target1;
        }

        void Test1()
        {
            Action<T> t1 = Target1;
            Action<T> t2 = D<T>.Target2;
        }

        void Test2()
        {
            Action<T> t2 = D<T>.Target2;
            Action<T, V> t3 = D<T>.Target3<V>;
        }

        void Test3()
        {
            Action<T, V> t3 = D<T>.Target3<V>;
            Action<T, V> t4 = D<T>.E<V>.Target4;
        }

        void Test4()
        {
            Action<T, V> t4 = D<T>.E<V>.Target4;
            Action<T> t5 = E.Target5<T>;
        }

        void Test5()
        {
            Action<T> t5t = E.Target5<T>;
            Action<V> t5v = E.Target5<V>;
        }

        static void Target0(T t) { }
    }

    static void Target1(T t) { }
}
class D<K>
{
    public static void Target2(K k) { }

    public static void Target3<M>(K k, M m) { }

    public class E<P>
    {
        public static void Target4(K k, P p) { }
    }
}
static class E
{
    public static void Target5<N>(this N n) { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("A.B.<>O", arity: 0
            , "System.Action<T> <0>__Target0"
            , "System.Action<T> <1>__Target1"
            , "System.Action<T> <2>__Target2"
            , "System.Action<T, V> <3>__Target3"
            , "System.Action<T, V> <4>__Target4"
            , "System.Action<T> <5>__Target5"
            , "System.Action<V> <6>__Target5"
        ));
        verifier.VerifyIL("A<T>.B<V>.Test0", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<0>__Target0""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void A<T>.B<V>.Target0(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<0>__Target0""
  IL_0018:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<1>__Target1""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void A<T>.Target1(T)""
  IL_0026:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<1>__Target1""
  IL_0030:  ret
}
");
        verifier.VerifyIL("A<T>.B<V>.Test1", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<1>__Target1""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void A<T>.Target1(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<1>__Target1""
  IL_0018:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<2>__Target2""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void D<T>.Target2(T)""
  IL_0026:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<2>__Target2""
  IL_0030:  ret
}
");
        verifier.VerifyIL("A<T>.B<V>.Test2", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<2>__Target2""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D<T>.Target2(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<2>__Target2""
  IL_0018:  ldsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<3>__Target3""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void D<T>.Target3<V>(T, V)""
  IL_0026:  newobj     ""System.Action<T, V>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<3>__Target3""
  IL_0030:  ret
}
");
        verifier.VerifyIL("A<T>.B<V>.Test3", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<3>__Target3""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D<T>.Target3<V>(T, V)""
  IL_000e:  newobj     ""System.Action<T, V>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<3>__Target3""
  IL_0018:  ldsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<4>__Target4""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void D<T>.E<V>.Target4(T, V)""
  IL_0026:  newobj     ""System.Action<T, V>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<4>__Target4""
  IL_0030:  ret
}
");
        verifier.VerifyIL("A<T>.B<V>.Test4", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<4>__Target4""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void D<T>.E<V>.Target4(T, V)""
  IL_000e:  newobj     ""System.Action<T, V>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T, V> A<T>.B<V>.<>O.<4>__Target4""
  IL_0018:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<5>__Target5""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void E.Target5<T>(T)""
  IL_0026:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<5>__Target5""
  IL_0030:  ret
}
");
        verifier.VerifyIL("A<T>.B<V>.Test5", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> A<T>.B<V>.<>O.<5>__Target5""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target5<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> A<T>.B<V>.<>O.<5>__Target5""
  IL_0018:  ldsfld     ""System.Action<V> A<T>.B<V>.<>O.<6>__Target5""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void E.Target5<V>(V)""
  IL_0026:  newobj     ""System.Action<V>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<V> A<T>.B<V>.<>O.<6>__Target5""
  IL_0030:  ret
}
");
    }

    [Fact]
    public void ContainersCanBeShared_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>()
    {
        Action    a = Target0<T>;
        Action<T> b = Target1;
        Action    c = D<T>.Target2;
        Action<C> d = E.Target3<T>;
    }

    static void Target0<T>() { }
    static void Target1<T>(T t) { }

    class D<K>
    {
        public static void Target2() { }
    }
}
static class E
{
    public static void Target3<V>(this C c) { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Test>O__0_0", arity: 1
            , "System.Action <0>__Target0"
            , "System.Action<T> <1>__Target1"
            , "System.Action <2>__Target2"
            , "System.Action<C> <3>__Target3"
        ));
        verifier.VerifyIL("C.Test<T>", @"
{
  // Code size       97 (0x61)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Test>O__0_0<T>.<0>__Target0""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target0<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Test>O__0_0<T>.<0>__Target0""
  IL_0018:  ldsfld     ""System.Action<T> C.<Test>O__0_0<T>.<1>__Target1""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void C.Target1<T>(T)""
  IL_0026:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T> C.<Test>O__0_0<T>.<1>__Target1""
  IL_0030:  ldsfld     ""System.Action C.<Test>O__0_0<T>.<2>__Target2""
  IL_0035:  brtrue.s   IL_0048
  IL_0037:  ldnull
  IL_0038:  ldftn      ""void C.D<T>.Target2()""
  IL_003e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0043:  stsfld     ""System.Action C.<Test>O__0_0<T>.<2>__Target2""
  IL_0048:  ldsfld     ""System.Action<C> C.<Test>O__0_0<T>.<3>__Target3""
  IL_004d:  brtrue.s   IL_0060
  IL_004f:  ldnull
  IL_0050:  ldftn      ""void E.Target3<T>(C)""
  IL_0056:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_005b:  stsfld     ""System.Action<C> C.<Test>O__0_0<T>.<3>__Target3""
  IL_0060:  ret
}
");
    }

    [Fact]
    public void ContainersCanBeShared_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Target0<T>() { }
    public static void Target1<T>(T t) { }

    public class D<T>
    {
        public static void Target2() { }
    }
}
static class E
{
    static void Test<T>()
    {
        Action    a = C.Target0<T>;
        Action<T> b = C.Target1;
        Action    c = C.D<T>.Target2;
        Action<C> d = Target3<T>;
    }

    public static void Target3<T>(this C c) { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("E.<Test>O__0_0", arity: 1
            , "System.Action <0>__Target0"
            , "System.Action<T> <1>__Target1"
            , "System.Action <2>__Target2"
            , "System.Action<C> <3>__Target3"
        ));
        verifier.VerifyIL("E.Test<T>", @"
{
  // Code size       97 (0x61)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action E.<Test>O__0_0<T>.<0>__Target0""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target0<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action E.<Test>O__0_0<T>.<0>__Target0""
  IL_0018:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<1>__Target1""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void C.Target1<T>(T)""
  IL_0026:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<1>__Target1""
  IL_0030:  ldsfld     ""System.Action E.<Test>O__0_0<T>.<2>__Target2""
  IL_0035:  brtrue.s   IL_0048
  IL_0037:  ldnull
  IL_0038:  ldftn      ""void C.D<T>.Target2()""
  IL_003e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0043:  stsfld     ""System.Action E.<Test>O__0_0<T>.<2>__Target2""
  IL_0048:  ldsfld     ""System.Action<C> E.<Test>O__0_0<T>.<3>__Target3""
  IL_004d:  brtrue.s   IL_0060
  IL_004f:  ldnull
  IL_0050:  ldftn      ""void E.Target3<T>(C)""
  IL_0056:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_005b:  stsfld     ""System.Action<C> E.<Test>O__0_0<T>.<3>__Target3""
  IL_0060:  ret
}
");
    }

    [Fact]
    public void ContainersCanBeShared_SkippingUnused()
    {
        var source = @"
using System;
class C
{
    public static void Target<T>(T t) { }
}
static class E
{
    static void Test<T>()
    {
        void LF2<G>()
        {
            void LF3()
            {
                Action<T> d = C.Target<T>;
                static void LF4 () { Action<T> d = C.Target<T>; }

                LF4();
            }
                
            void LF5()
            {
                Action<T> d = C.Target<T>;
            }

            LF3(); LF5();
        }
        
        LF2<int>();
    }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("E.<Test>O__0_0", arity: 1
            , "System.Action<T> <0>__Target"
        ));
        verifier.VerifyIL("E.<Test>g__LF3|0_1<T, G>", @"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0018:  call       ""void E.<Test>g__LF4|0_3<T, G>()""
  IL_001d:  ret
}
");
        verifier.VerifyIL("E.<Test>g__LF4|0_3<T, G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("E.<Test>g__LF5|0_2<T, G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void ContainersCanBeShared_LocalFunctions()
    {
        var source = @"
using System;
static class E
{
    static void Test<T>()
    {
        void Owner<G>()
        {
            void LF1()
            {
                Action d = LF2;
                static void LF2() { Console.Write(""PA""); }

                d();
            }
                
            void LF3()
            {
                Action d = LF2;
                static void LF2() { Console.Write(""SS""); }

                d();
            }

            LF1(); LF3();
        }
        
        Owner<int>();
    }

    static void Main(string[] args) { Test<int>(); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS, symbolValidator: VerifyCacheContainer("E.<Owner>O__0_0", arity: 2
            , "System.Action <0>__LF2"
            , "System.Action <1>__LF2"
        ));
        verifier.VerifyIL("E.<Test>g__LF1|0_1<T, G>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action E.<Owner>O__0_0<T, G>.<0>__LF2""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.<Test>g__LF2|0_3<T, G>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action E.<Owner>O__0_0<T, G>.<0>__LF2""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
        verifier.VerifyIL("E.<Test>g__LF3|0_2<T, G>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action E.<Owner>O__0_0<T, G>.<1>__LF2""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.<Test>g__LF2|0_4<T, G>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action E.<Owner>O__0_0<T, G>.<1>__LF2""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Containers0()
    {
        var source = @"
void Owner<T>(int i)
{
    var f = Target<T>;
}
void X()
{
    void Owner<T>(string s)
    {
        var f = Target<T>;
    }
}
static void Target<T>() { }
";
        var verifier = CompileAndVerify(source, symbolValidator: static module =>
        {
            VerifyCacheContainer("Program.<Owner>O__0_0", arity: 1, "System.Action <0>__Target")(module);
            VerifyCacheContainer("Program.<Owner>O__0_1", arity: 1, "System.Action <0>__Target")(module);
        });
        verifier.VerifyIL("Program.<<Main>$>g__Owner|0_0<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<Owner>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void Program.<<Main>$>g__Target|0_2<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action Program.<Owner>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("Program.<<Main>$>g__Owner|0_3<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<Owner>O__0_1<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void Program.<<Main>$>g__Target|0_2<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action Program.<Owner>O__0_1<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Containers1()
    {
        var source = @"
class C
{
    void Owner<T>(int i)
    {
        var f = Target<T>;
    }
    void Owner<T>(string s)
    {
        var f = Target<T>;
    }
    static void Target<T>() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: static module =>
        {
            VerifyCacheContainer("C.<Owner>O__0_0", arity: 1, "System.Action <0>__Target")(module);
            VerifyCacheContainer("C.<Owner>O__1_0", arity: 1, "System.Action <0>__Target")(module);
        });
        verifier.VerifyIL("C.Owner<T>(int)", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C.Owner<T>(string)", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__1_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__1_0<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Containers2()
    {
        var source = @"
class C
{
    void Owner<T>(int i)
    {
        var f = Target<T>;
    }
    void F()
    {
        void Owner<T>(string s)
        {
            var f = Target<T>;
        }
    }
    static void Target<T>() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: static module =>
        {
            VerifyCacheContainer("C.<Owner>O__0_0", arity: 1, "System.Action <0>__Target")(module);
            VerifyCacheContainer("C.<Owner>O__1_0", arity: 1, "System.Action <0>__Target")(module);
        });
        verifier.VerifyIL("C.Owner<T>(int)", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C.<F>g__Owner|1_0<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__1_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target<T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__1_0<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Fields0()
    {
        var source = @"
void F0()
{
    var f = Target;
    static void Target() { }
}
void F1()
{
    var f = Target;
    static void Target() { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("Program.<>O", arity: 0
            , "System.Action <0>__Target"
            , "System.Action <1>__Target"
        ));
        verifier.VerifyIL("Program.<<Main>$>g__F0|0_0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void Program.<<Main>$>g__Target|0_2()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action Program.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("Program.<<Main>$>g__F1|0_1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<>O.<1>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void Program.<<Main>$>g__Target|0_3()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action Program.<>O.<1>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Fields2()
    {
        var source = @"
class C
{
    void F()
    {
        void Owner<T>()
        {
            void F0()
            {
                var f = Target<T>;
                static void Target<G>() { }
            }
            void F1()
            {
                var f = Target<T>;
                static void Target<G>() { }
            }
        }
    }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Owner>O__0_0", arity: 1
            , "System.Action <0>__Target"
            , "System.Action <1>__Target"
        ));
        verifier.VerifyIL("C.<F>g__F0|0_1<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.<F>g__Target|0_3<T, T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C.<F>g__F1|0_2<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<Owner>O__0_0<T>.<1>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.<F>g__Target|0_4<T, T>()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<Owner>O__0_0<T>.<1>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Fields3()
    {
        var source = @"
using System;
class C
{
    void F()
    {
        void Owner<T>()
        {
            Action<int> f = E<T>.Target;

            void F1()
            {
                Action<string> f = E<T>.Target;
            }
        }
    }
}
class E<T>
{
    public static void Target(int i) { }
    public static void Target(string i) { }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<Owner>O__0_0", arity: 1
            , "System.Action<System.Int32> <0>__Target"
            , "System.Action<System.String> <1>__Target"
        ));
        verifier.VerifyIL("C.<F>g__Owner|0_0<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> C.<Owner>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E<T>.Target(int)""
  IL_000e:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<int> C.<Owner>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C.<F>g__F1|0_1<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<string> C.<Owner>O__0_0<T>.<1>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E<T>.Target(string)""
  IL_000e:  newobj     ""System.Action<string>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<string> C.<Owner>O__0_0<T>.<1>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void NameAmbiguity_Fields1()
    {
        var source = @"
class C
{
    void F0()
    {
        var f = Target;
        static void Target() { }
    }
    void F1()
    {
        var f = Target;
        static void Target() { }
    }
}
";
        var verifier = CompileAndVerify(source, symbolValidator: VerifyCacheContainer("C.<>O", arity: 0
            , "System.Action <0>__Target"
            , "System.Action <1>__Target"
        ));
        verifier.VerifyIL("C.F0", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.<F0>g__Target|0_0()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_0018:  ret
}
");
        verifier.VerifyIL("C.F1", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<1>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.<F1>g__Target|1_0()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action C.<>O.<1>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void EventHandlers_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
using System.Reflection;
class C
{
    void Test()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Target;
    }

    static Assembly Target(object sender, ResolveEventArgs e) => null;
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  call       ""System.AppDomain System.AppDomain.CurrentDomain.get""
  IL_0005:  ldsfld     ""System.ResolveEventHandler C.<>O.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""System.Reflection.Assembly C.Target(object, System.ResolveEventArgs)""
  IL_0015:  newobj     ""System.ResolveEventHandler..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.ResolveEventHandler C.<>O.<0>__Target""
  IL_0020:  callvirt   ""void System.AppDomain.AssemblyResolve.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void EventHandlers_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class MyEventArgs : EventArgs { }
class C<TEventArgs> where TEventArgs : EventArgs
{
    event EventHandler<TEventArgs> SomethingHappened;
 
    void Test()
    {
        var c = new C<MyEventArgs>();
        c.SomethingHappened += D.Target;
    }

}
class D
{
    public static void Target(object sender, MyEventArgs e) { }
}
";
        CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""C<MyEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<MyEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void D.Target(object, MyEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<MyEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<MyEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_0020:  callvirt   ""void C<MyEventArgs>.SomethingHappened.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void EventHandlers_TypeScoped0()
    {
        var source = @"
using System;
class C<TEventArgs> where TEventArgs : EventArgs
{
    event EventHandler<TEventArgs> SomethingHappened;
 
    void Test()
    {
        this.SomethingHappened += Target;
    }

    static void Target(object sender, TEventArgs e) { }
}
";
        CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void C<TEventArgs>.Target(object, TEventArgs)""
  IL_0011:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_001c:  call       ""void C<TEventArgs>.SomethingHappened.add""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void EventHandlers_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<TEventArgs>() where TEventArgs : EventArgs
    {
        var d = new D<TEventArgs>();
        d.SomethingHappened += Target;
    }

    static void Target<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs { }
}
class D<TEventArgs> where TEventArgs : EventArgs
{
    public event EventHandler<TEventArgs> SomethingHappened;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<TEventArgs>", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""D<TEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<TEventArgs> C.<Test>O__0_0<TEventArgs>.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void C.Target<TEventArgs>(object, TEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<TEventArgs> C.<Test>O__0_0<TEventArgs>.<0>__Target""
  IL_0020:  callvirt   ""void D<TEventArgs>.SomethingHappened.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => Invoke(new { x = 0 }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("C.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: int x>> C.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void C.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: int x>> C.<>O.<0>__Target""
  IL_0021:  call       ""void C.Invoke<<anonymous type: int x>>(<anonymous type: int x>, System.Action<<anonymous type: int x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => (new D<int>()).Test(0);
}
class D<G>
{
    public void Test(G g) => Invoke(new { x = g }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: G x>>(<anonymous type: G x>, System.Action<<anonymous type: G x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => (new D<int>()).Test(0);
}
class D<G>
{
    public void Test(G g) => Invoke(new { x = g }, E.Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);
}
class E
{
    public static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void E.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: G x>>(<anonymous type: G x>, System.Action<<anonymous type: G x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D<int>.Test();
}
class D<G>
{
    delegate void MyAction<T>(T t);

    public static void Test() => Invoke(new { x = 0 }, Target);

    static void Invoke<T>(T t, MyAction<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""D<G>.MyAction<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: int x>>(<anonymous type: int x>, D<G>.MyAction<<anonymous type: int x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D.Test(0);
}
class D
{
    public static void Test<T>(T t) => Invoke(new { x = t }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""<>f__AnonymousType0<T>..ctor(T)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: T x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D.Target<<anonymous type: T x>>(<anonymous type: T x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: T x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: T x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_0021:  call       ""void D.Invoke<<anonymous type: T x>>(<anonymous type: T x>, System.Action<<anonymous type: T x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D.Test(0);
}
class D
{
    public static void Test<T>(T t) => Invoke(t, new { x = 0 }, Target);

    static void Invoke<T, V>(T t, V v, Action<T, V> f) => f(t, v);

    static void Target<T, V>(T t, V v) => Console.WriteLine(v);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       40 (0x28)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  ldsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  pop
  IL_0010:  ldnull
  IL_0011:  ldftn      ""void D.Target<T, <anonymous type: int x>>(T, <anonymous type: int x>)""
  IL_0017:  newobj     ""System.Action<T, <anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_0022:  call       ""void D.Invoke<T, <anonymous type: int x>>(T, <anonymous type: int x>, System.Action<T, <anonymous type: int x>>)""
  IL_0027:  ret
}
");
    }

    [Fact]
    public void AnonymousClass_AnonymousDelegate0()
    {
        var source = @"
using System;
class D
{
    public void Owner<T>()
    {
        static void Test<NotUsed>(T t)
        {
            var f = F<T>;
            var a = new { x = f };

            Invoke(a, Target);
        }
    }

    static void F<T>(ref T t) { }

    static void Invoke<T>(T t, Action<T> f) { }

    static void Target<T>(T t) { }
}
";
        CompileAndVerify(source).VerifyIL("D.<Owner>g__Test|0_0<T, NotUsed>", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""<anonymous delegate> D.<Owner>O__0_0<T>.<0>__F""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D.F<T>(ref T)""
  IL_0010:  newobj     ""<>A{00000001}<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""<anonymous delegate> D.<Owner>O__0_0<T>.<0>__F""
  IL_001b:  newobj     ""<>f__AnonymousType0<<anonymous delegate>>..ctor(<anonymous delegate>)""
  IL_0020:  ldsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D.<Owner>O__0_0<T>.<1>__Target""
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_003b
  IL_0028:  pop
  IL_0029:  ldnull
  IL_002a:  ldftn      ""void D.Target<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>)""
  IL_0030:  newobj     ""System.Action<<anonymous type: <anonymous delegate> x>>..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D.<Owner>O__0_0<T>.<1>__Target""
  IL_003b:  call       ""void D.Invoke<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>, System.Action<<anonymous type: <anonymous delegate> x>>)""
  IL_0040:  ret
}
");
    }

    [Fact]
    public void AnonymousClass_AnonymousDelegate1()
    {
        var source = @"
using System;
class D<T>
{
    public void Top<N0>()
    {
        static void Test<N1>(T t)
        {
            var f = F;
            var a = new { x = f };

            Invoke(a, Target);
        }
    }

    static void F(ref T t) { }

    static void Invoke<G>(G t, Action<G> f) { }

    static void Target<G>(G t) { }
}
";
        CompileAndVerify(source).VerifyIL("D<T>.<Top>g__Test|0_0<N0, N1>", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""<anonymous delegate> D<T>.<>O.<0>__F""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.F(ref T)""
  IL_0010:  newobj     ""<>A{00000001}<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""<anonymous delegate> D<T>.<>O.<0>__F""
  IL_001b:  newobj     ""<>f__AnonymousType0<<anonymous delegate>>..ctor(<anonymous delegate>)""
  IL_0020:  ldsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D<T>.<>O.<1>__Target""
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_003b
  IL_0028:  pop
  IL_0029:  ldnull
  IL_002a:  ldftn      ""void D<T>.Target<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>)""
  IL_0030:  newobj     ""System.Action<<anonymous type: <anonymous delegate> x>>..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D<T>.<>O.<1>__Target""
  IL_003b:  call       ""void D<T>.Invoke<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>, System.Action<<anonymous type: <anonymous delegate> x>>)""
  IL_0040:  ret
}
");
    }

    [Fact]
    public void Pointer_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    unsafe void Test()
    {
        Func<int*[]> t = Target;
        t();
    }

    unsafe static int*[] Target() => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int*[]> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target()""
  IL_0010:  newobj     ""System.Func<int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int*[]> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""int*[] System.Func<int*[]>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Pointer_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    unsafe void Test()
    {
        Func<T, int*[]> t = Target;
        t(default(T));
    }

    unsafe static int*[] Target(T t) => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int*[] System.Func<T, int*[]>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void Pointer_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    unsafe void Test<T>(T t)
    {
        Func<T, int*[]> f = Target<T>;
        f(t);
    }

    unsafe static int*[] Target<G>(G g) => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""int*[] System.Func<T, int*[]>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void Dynamic_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    void Test()
    {
        Func<dynamic> t = Target;
        t();
    }

    static dynamic Target() => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<dynamic> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target()""
  IL_0010:  newobj     ""System.Func<dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<dynamic> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""dynamic System.Func<dynamic>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Dynamic_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    void Test()
    {
        Func<T, dynamic> t = Target;
        t(default(T));
    }

    static dynamic Target(T t) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void Dynamic_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);
    }

    static dynamic Target<G>(G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void SynthesizedAnonymousDelegate_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        G(Target<int>);
    }

    void G(Delegate d) {}

    static dynamic Target<G>(ref G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<int>(ref int)""
  IL_0011:  newobj     ""<>F{00000001}<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_001c:  call       ""void C.G(System.Delegate)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void SynthesizedAnonymousDelegate_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        G(Target<T>);
    }

    void G(Delegate d) {}

    static dynamic Target<G>(ref G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<T>(ref T)""
  IL_0011:  newobj     ""<>F{00000001}<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_001c:  call       ""void C.G(System.Delegate)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void TopLevelMethod_LocalFunctions_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    void Test(int t)
    {
        Func<int, dynamic> f = Target<int>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, dynamic> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<Test>g__Target|0_0<int>(int)""
  IL_0010:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, dynamic> C.<>O.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevelMethod_LocalFunctions_NotStatic()
    {
        var source = @"
using System;
class C
{
    void Test(int t)
    {
        Func<int, dynamic> f = Target<int>;
        f(t);

        dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""dynamic C.<Test>g__Target|0_0<int>(int)""
  IL_0007:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_000c:  ldarg.1
  IL_000d:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0012:  pop
  IL_0013:  ret
}
");
    }

    [Fact]
    public void TopLevelMethod_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<Test>g__Target|0_0<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        void Test()
        {
            Func<T, dynamic> f = Target<T>;
            f(t);

            static dynamic Target<G>(G g) => 0;
        }
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<TopLevel>g__Test|0_0<T>", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_1<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void Lambda_Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        Action x = () =>
        {
            void Test()
            {
                Func<T, dynamic> f = Target<T>;
                f(t);

                static dynamic Target<G>(G g) => 0;
            }

            Test();
        };

        x();
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<>c__DisplayClass0_0<T>.<TopLevel>g__Test|1", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_2<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void Lambda_Local_LocalFunctions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        Action y = () =>
        {
            void Test() { /* Test method ordinals in generated names */ }
            Test();
        };
        Action x = () =>
        {
            void Test()
            {
                Func<T, dynamic> f = Target<T>;
                f(t);

                static dynamic Target<G>(G g) => 0;
            }

            Test();
        };

        x();
        y();
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<>c__DisplayClass0_0<T>.<TopLevel>g__Test|3", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_4<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Lambda_Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;

Action y = () =>
{
    void Test() { /* Test method ordinals in generated names */ }
    Test();
};
Action x = () =>
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }

    Test(0);
};

x();
y();
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_3<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic Program.<<Main>$>g__Target|0_4<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Lambda_Local_LocalFunctions_MethodScoped1()
    {
        var source = @"
var y = () =>
{
    void Test() { /* Test method ordinals in generated names */ }
    Test();
};
var x = () =>
{
    void Test<T>(T t)
    {
        var f = Target<int>;
        f(0);

        static dynamic Target<G>(G g)
        {
            T f = default;
            return f;
        }
    }

    Test(false);
};

x();
y();
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_3<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic Program.<<Main>$>g__Target|0_4<T, int>(int)""
  IL_0010:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_TypeScoped0()
    {
        var source = @"
(System.Action a, int _) = (Target, 0);
static void Target() { }
";
        CompileAndVerify(source).VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Program.<<Main>$>g__Target|0_0()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action Program.<>O.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_TypeScoped1()
    {
        var source = @"
var t = Target;
static (int a, int b) Target() => (0, 0);
";
        CompileAndVerify(source).VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<int, int>> Program.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<int, int> Program.<<Main>$>g__Target|0_0()""
  IL_000e:  newobj     ""System.Func<System.ValueTuple<int, int>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<System.ValueTuple<int, int>> Program.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped0()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Action a, int _) = (Target, 0);
    static void Target() { }
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Program.<<Main>$>g__Target|0_1<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped1()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Func<(T, int)> a, int _) = (Target, 0);
    static (T, int) Target() => (default(T), 0);
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<T, int>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""System.ValueTuple<T, int> Program.<<Main>$>g__Target|0_1<T>()""
  IL_0010:  newobj     ""System.Func<System.ValueTuple<T, int>>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<System.ValueTuple<T, int>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped2()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Func<T, (T, T)> a, int _) = (Target<T>, 0);
    static (T, G) Target<G>(G g) => (default(T), g);
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""System.ValueTuple<T, T> Program.<<Main>$>g__Target|0_1<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped3()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Func<T, (T, T)> a, int _) = (Target<T, T>, 0);
}
static (T, G) Target<T, G>(G g) => (default(T), g);
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""System.ValueTuple<T, T> Program.<<Main>$>g__Target|0_1<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_TypeScoped0()
    {
        var source = @"
class C<T>
{
    void Test()
    {
        var x = Target;
        static (T, T) Target(T t) => (t, t);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, T> C<T>.<Test>g__Target|0_0(T)""
  IL_000e:  newobj     ""System.Func<T, System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped0()
    {
        var source = @"
class C<T>
{
    void Test<G>()
    {
        var x = Target;
        static (T, G) Target(T t, G g) => (t, g);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test<G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, G, System.ValueTuple<T, G>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, G> C<T>.<Test>g__Target|0_0<G>(T, G)""
  IL_000e:  newobj     ""System.Func<T, G, System.ValueTuple<T, G>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, G, System.ValueTuple<T, G>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped1()
    {
        var source = @"
class C<T>
{
    void Test<G>()
    {
        var x = Target<T>;
        static (T, G, V) Target<V>(T t, G g, V v) => (t, g, v);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test<G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, G, T, System.ValueTuple<T, G, T>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, G, T> C<T>.<Test>g__Target|0_0<G, T>(T, G, T)""
  IL_000e:  newobj     ""System.Func<T, G, T, System.ValueTuple<T, G, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, G, T, System.ValueTuple<T, G, T>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped2()
    {
        var source = @"
class M<N>
{
    void F<I>()
    {
        void Test<C>()
        {
            var x = Target<N>;
            static (N, I, C, E) Target<E>(N n, I i, C c, E e) => (n, i, c, e);
        }
    }
}
";
        CompileAndVerify(source).VerifyIL("M<N>.<F>g__Test|0_0<I, C>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>> M<N>.<Test>O__0_0<I, C>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<N, I, C, N> M<N>.<F>g__Target|0_1<I, C, N>(N, I, C, N)""
  IL_000e:  newobj     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>> M<N>.<Test>O__0_0<I, C>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void TestConditionalOperatorMethodGroup()
    {
        var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Func<int> f = null;
        System.Console.WriteLine(f);
        System.Func<int> g1 = b ? f : M;
        System.Console.WriteLine(g1);
        System.Func<int> g2 = b ? M : f;
        System.Console.WriteLine(g2);
    }

    static int M()
    {
        return 0;
    }
}";
        var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular11);
        comp.VerifyDiagnostics();
        comp.VerifyIL("C.Main", @"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (System.Func<int> V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void System.Console.WriteLine(object)""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_0029
  IL_000c:  ldsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_002a
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""int C.M()""
  IL_001c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.WriteLine(object)""
  IL_002f:  brtrue.s   IL_0034
  IL_0031:  ldloc.0
  IL_0032:  br.s       IL_004f
  IL_0034:  ldsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0039:  dup
  IL_003a:  brtrue.s   IL_004f
  IL_003c:  pop
  IL_003d:  ldnull
  IL_003e:  ldftn      ""int C.M()""
  IL_0044:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0049:  dup
  IL_004a:  stsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_004f:  call       ""void System.Console.WriteLine(object)""
  IL_0054:  ret
}
");
    }

    [Fact]
    public void WinMdEventAssignment()
    {
        var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;
}

class D
{
    C c;

    void InstanceAdd()
    {
        c.Instance += Action;
    }

    void InstanceRemove()
    {
        c.Instance -= Action;
    }

    static void StaticAdd()
    {
        C.Static += Action;
    }

    static void StaticRemove()
    {
        C.Static -= Action;
    }

    static void Action()
    {
    }
}
";
        var verifier = CompileAndVerifyWithWinRt(source, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseWinMD);

        verifier.VerifyIL("D.InstanceAdd", @"
{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_000e:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void C.Instance.remove""
  IL_001a:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001f:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003a
  IL_0027:  pop
  IL_0028:  ldnull
  IL_0029:  ldftn      ""void D.Action()""
  IL_002f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0034:  dup
  IL_0035:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_003a:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_003f:  ret
}");

        verifier.VerifyIL("D.InstanceRemove", @"
{
  // Code size       50 (0x32)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  ldftn      ""void C.Instance.remove""
  IL_000c:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0011:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_002c
  IL_0019:  pop
  IL_001a:  ldnull
  IL_001b:  ldftn      ""void D.Action()""
  IL_0021:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0026:  dup
  IL_0027:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_002c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0031:  ret
}");
        verifier.VerifyIL("D.StaticAdd", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0007:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldnull
  IL_000d:  ldftn      ""void C.Static.remove""
  IL_0013:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0018:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0033
  IL_0020:  pop
  IL_0021:  ldnull
  IL_0022:  ldftn      ""void D.Action()""
  IL_0028:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002d:  dup
  IL_002e:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_0033:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0038:  ret
}");

        verifier.VerifyIL("D.StaticRemove", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_0027
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void D.Action()""
  IL_001c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_0027:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_002c:  ret
}");
    }

    [Fact]
    public void WinMdEventFieldAssignment()
    {
        var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;

    void InstanceAssign()
    {
        Instance = Action;
    }

    static void StaticAssign()
    {
        Static = Action;
    }

    static void Action()
    {
    }
}
";
        var verifier = CompileAndVerifyWithWinRt(source, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseWinMD);

        verifier.VerifyIL("C.InstanceAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""void C.Instance.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldarg.0
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldarg.0
  IL_001e:  ldftn      ""void C.Instance.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action C.<>O.<0>__Action""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action C.<>O.<0>__Action""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");

        verifier.VerifyIL("C.StaticAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldnull
  IL_001e:  ldftn      ""void C.Static.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action C.<>O.<0>__Action""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action C.<>O.<0>__Action""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");
    }

    [Fact]
    public void LockDelegate()
    {
        var text =
@"
delegate void D(string p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1 = PM)
        {
            d1(""PASS"");
        }
    }
    static partial void PM(string p2);
    static partial void PM(string p2)
    {
        System.Console.WriteLine(p2);
    }
}
";

        CompileAndVerify(text, parseOptions: TestOptions.Regular11, expectedOutput: PASS).VerifyIL("Test.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (D V_0, //d1
                D V_1,
                bool V_2)
  IL_0000:  ldsfld     ""D Test.<>O.<0>__PM""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Test.PM(string)""
  IL_0010:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D Test.<>O.<0>__PM""
  IL_001b:  dup
  IL_001c:  stloc.0
  IL_001d:  stloc.1
  IL_001e:  ldc.i4.0
  IL_001f:  stloc.2
  .try
  {
    IL_0020:  ldloc.1
    IL_0021:  ldloca.s   V_2
    IL_0023:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0028:  ldloc.0
    IL_0029:  ldstr      ""PASS""
    IL_002e:  callvirt   ""void D.Invoke(string)""
    IL_0033:  leave.s    IL_003f
  }
  finally
  {
    IL_0035:  ldloc.2
    IL_0036:  brfalse.s  IL_003e
    IL_0038:  ldloc.1
    IL_0039:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_003e:  endfinally
  }
  IL_003f:  ret
}
");
    }

    [Fact]
    public void NullCoalescingAssignmentValidRHS()
    {
        CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        Action a = null;
        (a ??= TestMethod)();
        (a ??= () => {})();
    }
    static void TestMethod() => Console.WriteLine(""In TestMethod"");
}
", parseOptions: TestOptions.Regular11, expectedOutput: @"
In TestMethod
In TestMethod
").VerifyIL("C.Main()", @"
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (System.Action V_0) //a
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0024
  IL_0006:  pop
  IL_0007:  ldsfld     ""System.Action C.<>O.<0>__TestMethod""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  pop
  IL_0010:  ldnull
  IL_0011:  ldftn      ""void C.TestMethod()""
  IL_0017:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Action C.<>O.<0>__TestMethod""
  IL_0022:  dup
  IL_0023:  stloc.0
  IL_0024:  callvirt   ""void System.Action.Invoke()""
  IL_0029:  ldloc.0
  IL_002a:  dup
  IL_002b:  brtrue.s   IL_004f
  IL_002d:  pop
  IL_002e:  ldsfld     ""System.Action C.<>c.<>9__0_0""
  IL_0033:  dup
  IL_0034:  brtrue.s   IL_004d
  IL_0036:  pop
  IL_0037:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_003c:  ldftn      ""void C.<>c.<Main>b__0_0()""
  IL_0042:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0047:  dup
  IL_0048:  stsfld     ""System.Action C.<>c.<>9__0_0""
  IL_004d:  dup
  IL_004e:  stloc.0
  IL_004f:  callvirt   ""void System.Action.Invoke()""
  IL_0054:  ret
}");
    }

    [Fact]
    public void ImplicitlyTypedVariables_01()
    {
        var source =
@"using System;
class Program
{
    static void Main()
    {
        var d1 = Main;
        Report(d1);
        var d2 = () => { };
        Report(d2);
        var d3 = delegate () { };
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine($""{d.GetType().Namespace}.{d.GetType().Name}"");
}";

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular11, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action");
        verifier.VerifyIL("Program.Main", @"
{
  // Code size      115 (0x73)
  .maxstack  2
  .locals init (System.Action V_0, //d1
                System.Action V_1, //d2
                System.Action V_2) //d3
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Action Program.<>O.<0>__Main""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void Program.Main()""
  IL_0011:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.Action Program.<>O.<0>__Main""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  call       ""void Program.Report(System.Delegate)""
  IL_0023:  nop
  IL_0024:  ldsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0038:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0043:  stloc.1
  IL_0044:  ldloc.1
  IL_0045:  call       ""void Program.Report(System.Delegate)""
  IL_004a:  nop
  IL_004b:  ldsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_006a
  IL_0053:  pop
  IL_0054:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0059:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_005f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0064:  dup
  IL_0065:  stsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_006a:  stloc.2
  IL_006b:  ldloc.2
  IL_006c:  call       ""void Program.Report(System.Delegate)""
  IL_0071:  nop
  IL_0072:  ret
}
");
    }

    [Fact]
    public void CustomModifiers_Method()
    {
        var ilSource = @"
.class public auto ansi beforefieldinit C1`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig static 
        string Method () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 6 (0x6)
        .maxstack 8

        IL_0000: ldstr ""PASS""
        IL_0005: ret
    } // end of method C1`1::Method

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C1`1::.ctor

} // end of class C1`1

.class public auto ansi beforefieldinit C2`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3`1<T>
    extends class C1`1<int32 modopt(class C2`1<!T>)>
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x205f
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void class C1`1<int32>::.ctor()
        IL_0006: ret
    } // end of method C3`1::.ctor

} // end of class C3`1
";
        var source = @"
class Test
{
    static void Main()
    {
        M<int>();
    }

    static void M<G>()
    {
        System.Func<string> x = C3<G>.Method;
        System.Console.WriteLine(x());
    }
}
";
        var compilation = CreateCompilationWithIL(source, ilSource, options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(compilation, expectedOutput: PASS);
        verifier.VerifyIL("Test.M<G>", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<string> Test.<M>O__1_0<G>.<0>__Method""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""string C1<int>.Method()""
  IL_0010:  newobj     ""System.Func<string>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<string> Test.<M>O__1_0<G>.<0>__Method""
  IL_001b:  callvirt   ""string System.Func<string>.Invoke()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void CustomModifiers_Delegate()
    {
        var ilSource = @"
.class public auto ansi beforefieldinit C1`1<T>
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested public auto ansi sealed F<T>
		extends [mscorlib]System.MulticastDelegate
	{
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor (
				object 'object',
				native int 'method'
			) runtime managed 
		{
		} // end of method F::.ctor

		.method public hidebysig newslot virtual 
			instance string Invoke () runtime managed 
		{
		} // end of method F::Invoke

		.method public hidebysig newslot virtual 
			instance class [mscorlib]System.IAsyncResult BeginInvoke (
				class [mscorlib]System.AsyncCallback callback,
				object 'object'
			) runtime managed 
		{
		} // end of method F::BeginInvoke

		.method public hidebysig newslot virtual 
			instance string EndInvoke (
				class [mscorlib]System.IAsyncResult result
			) runtime managed 
		{
		} // end of method F::EndInvoke

	} // end of class F


	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x211f
		// Code size 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method C1`1::.ctor

} // end of class C1`1

.class public auto ansi beforefieldinit C2`1<T>
    extends System.Object
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2057
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3`1<T>
    extends class C1`1<int32 modopt(class C2`1<!T>)>
{
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x205f
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void class C1`1<int32>::.ctor()
        IL_0006: ret
    } // end of method C3`1::.ctor

} // end of class C3`1
";
        var source = @"
class Test
{
    static void Main()
    {
        M<int>();
    }

    static void M<G>()
    {
        C3<G>.F x = Method;
        System.Console.WriteLine(x());
    }

    static string Method() => ""PASS"";
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test.<M>O__1_0");
            var field = Assert.Single(container.GetMembers()) as FieldSymbol;
            AssertEx.NotNull(field);

            var typeParameters = new List<TypeParameterSymbol>();
            field.Type.VisitType(static (typeSymbol, typeParameters, _) =>
            {
                if (typeSymbol is TypeParameterSymbol typeParameter)
                {
                    typeParameters.Add(typeParameter);
                }

                return false;
            },
            typeParameters, visitCustomModifiers: true);

            var typeParameter = Assert.Single(typeParameters);
            Assert.Equal("G", typeParameter.Name);
            Assert.Equal("<M>O__1_0", typeParameter.ContainingSymbol.Name);
        }
        var compilation = CreateCompilationWithIL(source, ilSource, options: TestOptions.DebugExe);
        var verifier = CompileAndVerify(compilation, expectedOutput: PASS, symbolValidator: containerValidator);
        verifier.VerifyIL("Test.M<G>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (C1<int>.F V_0) //x
  IL_0000:  nop
  IL_0001:  ldsfld     ""C1<int>.F Test.<M>O__1_0<G>.<0>__Method""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""string Test.Method()""
  IL_0011:  newobj     ""C1<int>.F..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""C1<int>.F Test.<M>O__1_0<G>.<0>__Method""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  callvirt   ""string C1<int>.F.Invoke()""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  nop
  IL_0029:  ret
}
");
    }

    private static Action<ModuleSymbol> VerifyCacheContainer(string typeName, int arity, params string[] expectedFields)
    {
        return module =>
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>(typeName);
            AssertEx.NotNull(container);
            Assert.Equal(arity, container.Arity);

            var fields = container.GetMembers().OfType<FieldSymbol>().Select(field => $"{field.Type.ToTestDisplayString()} {field.Name}").ToArray();
            AssertEx.SetEqual(expectedFields, fields);
        };
    }

    private static Action<ModuleSymbol> VerifyNoCacheContainersIn(string containingTypeName)
    {
        return module =>
        {
            var containingType = module.GlobalNamespace.GetMember<NamedTypeSymbol>(containingTypeName);
            AssertEx.NotNull(containingType);

            var nestedTypes = containingType.GetTypeMembers();
            Assert.DoesNotContain(nestedTypes, t => t.Name.StartsWith("<") && t.Name.Contains(">O"));
        };
    }

}
