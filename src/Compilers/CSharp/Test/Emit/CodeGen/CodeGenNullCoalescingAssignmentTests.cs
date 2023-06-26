// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.NullCoalescingAssignment)]
    public class CodeGenNullCoalescingAssignmentTests : CompilingTestBase
    {
        [Fact]
        public void LocalLvalue()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        TestNullable();
        TestObject();
        TestAsStatement();
    }
    static void TestNullable()
    {
        int? i1 = null;
        Console.WriteLine(i1 ??= GetInt());
    }
    static void TestObject()
    {
        string s1 = null;
        Console.WriteLine(s1 ??= GetString());
    }
    static void TestAsStatement()
    {
        object o = null;
        o ??= ""As Statement"";
        Console.WriteLine(o);
    }
    static int GetInt()
    {
        Console.WriteLine(""In GetInt"");
        return 0;
    }
    static string GetString()
    {
        Console.WriteLine(""In GetString"");
        return ""Test"";
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"
In GetInt
0
In GetString
Test
As Statement
");

            verifier.VerifyIL("C.TestNullable()", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (int? V_0, //i1
                int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""int int?.GetValueOrDefault()""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool int?.HasValue.get""
  IL_0017:  brtrue.s   IL_002a
  IL_0019:  call       ""int C.GetInt()""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.1
  IL_0022:  call       ""int?..ctor(int)""
  IL_0027:  ldloc.1
  IL_0028:  br.s       IL_002b
  IL_002a:  ldloc.1
  IL_002b:  call       ""void System.Console.WriteLine(int)""
  IL_0030:  ret
}");

            // When the optimizer is on, it turns the local into a stack local, so we don't see assignments.
            verifier.VerifyIL("C.TestObject()", expectedIL: @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000a
  IL_0004:  pop
  IL_0005:  call       ""string C.GetString()""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  ret
}
");

            verifier.VerifyIL("C.TestAsStatement()", expectedIL: @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_000b
  IL_0005:  ldstr      ""As Statement""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  call       ""void System.Console.WriteLine(object)""
  IL_0011:  ret
}
");

            // With the optimizer off, the local is not elided
            CompileAndVerify(source, options: TestOptions.DebugDll).VerifyIL("C.TestObject()", expectedIL: @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (string V_0) //s1
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_000f
  IL_0007:  pop
  IL_0008:  call       ""string C.GetString()""
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  nop
  IL_0015:  ret
}
");
        }

        [Fact]
        public void FieldLvalue()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;
    object f2 = null;

    public static void Main()
    {
        TestInt();
        TestObject();
    }
    static void TestInt()
    {
        var c = new C();
        Console.WriteLine(c.f1 ??= GetInt());
    }
    public static void TestObject()
    {
        var c = new C();
        Console.WriteLine(c.f2 ??= GetObject());
    }
    static int GetInt()
    {
        Console.WriteLine(""In GetInt"");
        return 0;
    }
    static object GetObject()
    {
        Console.WriteLine(""In GetObject"");
        return ""object"";
    }
}
", expectedOutput: @"
In GetInt
0
In GetObject
object
");
            verifier.VerifyIL("C.TestInt()", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (C V_0,
                int V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldflda     ""int? C.f1""
  IL_000c:  call       ""int int?.GetValueOrDefault()""
  IL_0011:  stloc.1
  IL_0012:  ldloc.0
  IL_0013:  ldflda     ""int? C.f1""
  IL_0018:  call       ""bool int?.HasValue.get""
  IL_001d:  brtrue.s   IL_0034
  IL_001f:  call       ""int C.GetInt()""
  IL_0024:  stloc.1
  IL_0025:  ldloc.0
  IL_0026:  ldloc.1
  IL_0027:  newobj     ""int?..ctor(int)""
  IL_002c:  stfld      ""int? C.f1""
  IL_0031:  ldloc.1
  IL_0032:  br.s       IL_0035
  IL_0034:  ldloc.1
  IL_0035:  call       ""void System.Console.WriteLine(int)""
  IL_003a:  ret
}");

            verifier.VerifyIL("C.TestObject()", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (C V_0,
                object V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""object C.f2""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_001e
  IL_000f:  pop
  IL_0010:  ldloc.0
  IL_0011:  call       ""object C.GetObject()""
  IL_0016:  dup
  IL_0017:  stloc.1
  IL_0018:  stfld      ""object C.f2""
  IL_001d:  ldloc.1
  IL_001e:  call       ""void System.Console.WriteLine(object)""
  IL_0023:  ret
}");
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/11036")]
        public void IndexerLvalue()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        TestInt();
        TestObject();
    }
    static void TestInt()
    {
        int?[] a = new int?[1];
        Console.WriteLine(a[GetInt()] ??= 1);
        Console.WriteLine(a[GetInt()] ??= 2);
    }
    static void TestObject()
    {
        object[] a = new object[1];
        Console.WriteLine(a[GetInt()] ??= 1);
        Console.WriteLine(a[GetInt()] ??= 2);
    }
    static int GetInt()
    {
        Console.WriteLine(""In GetInt"");
        return 0;
    }
}
", expectedOutput: @"
In GetInt
1
In GetInt
1
In GetInt
1
In GetInt
1
");
            verifier.VerifyIL("C.TestInt()", @"
{
  // Code size      106 (0x6a)
  .maxstack  3
  .locals init (int?& V_0,
                int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int?""
  IL_0006:  dup
  IL_0007:  call       ""int C.GetInt()""
  IL_000c:  ldelema    ""int?""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  call       ""int int?.GetValueOrDefault()""
  IL_0018:  stloc.1
  IL_0019:  ldloc.0
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brtrue.s   IL_0032
  IL_0021:  ldc.i4.1
  IL_0022:  stloc.1
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  newobj     ""int?..ctor(int)""
  IL_002a:  stobj      ""int?""
  IL_002f:  ldloc.1
  IL_0030:  br.s       IL_0033
  IL_0032:  ldloc.1
  IL_0033:  call       ""void System.Console.WriteLine(int)""
  IL_0038:  call       ""int C.GetInt()""
  IL_003d:  ldelema    ""int?""
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  call       ""int int?.GetValueOrDefault()""
  IL_0049:  stloc.1
  IL_004a:  ldloc.0
  IL_004b:  call       ""bool int?.HasValue.get""
  IL_0050:  brtrue.s   IL_0063
  IL_0052:  ldc.i4.2
  IL_0053:  stloc.1
  IL_0054:  ldloc.0
  IL_0055:  ldloc.1
  IL_0056:  newobj     ""int?..ctor(int)""
  IL_005b:  stobj      ""int?""
  IL_0060:  ldloc.1
  IL_0061:  br.s       IL_0064
  IL_0063:  ldloc.1
  IL_0064:  call       ""void System.Console.WriteLine(int)""
  IL_0069:  ret
}
");

            verifier.VerifyIL("C.TestObject()", @"
{
  // Code size       70 (0x46)
  .maxstack  5
  .locals init (object[] V_0,
                int V_1,
                object V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""object""
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  call       ""int C.GetInt()""
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  ldelem.ref
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_0021
  IL_0014:  pop
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.1
  IL_0018:  box        ""int""
  IL_001d:  dup
  IL_001e:  stloc.2
  IL_001f:  stelem.ref
  IL_0020:  ldloc.2
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  stloc.0
  IL_0027:  call       ""int C.GetInt()""
  IL_002c:  stloc.1
  IL_002d:  ldloc.0
  IL_002e:  ldloc.1
  IL_002f:  ldelem.ref
  IL_0030:  dup
  IL_0031:  brtrue.s   IL_0040
  IL_0033:  pop
  IL_0034:  ldloc.0
  IL_0035:  ldloc.1
  IL_0036:  ldc.i4.2
  IL_0037:  box        ""int""
  IL_003c:  dup
  IL_003d:  stloc.2
  IL_003e:  stelem.ref
  IL_003f:  ldloc.2
  IL_0040:  call       ""void System.Console.WriteLine(object)""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void RefIndexerLvalue()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        var d = new D();
        Console.WriteLine(d[0] ??= ""Test String"");
    }
}
class D
{
    object field = null;
    public ref object this[int i]
    {
        get => ref field;
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"
Test String
");

            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (object& V_0,
                object V_1)
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  ldc.i4.0
  IL_0006:  callvirt   ""ref object D.this[int].get""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldind.ref
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      ""Test String""
  IL_0018:  dup
  IL_0019:  stloc.1
  IL_001a:  stind.ref
  IL_001b:  ldloc.1
  IL_001c:  call       ""void System.Console.WriteLine(object)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void PropertyLvalue()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;
    int? P1
    {
        get
        {
            Console.WriteLine(""In Get P1"");
            return f1;
        }
        set
        {
            Console.WriteLine(""In Set P1"");
            f1 = value;
        }
    }

    object f2 = null;
    object P2
    {
        get
        {
            Console.WriteLine(""In Get P2"");
            return f2;
        }
        set
        {
            Console.WriteLine(""In Set P2"");
            f2 = value;
        }
    }

    public static int GetInt(int i)
    {
        Console.WriteLine(""In GetInt"");
        return i;
    }

    public static void Main()
    {
        TestInt();
        TestObject();
    }

    public static void TestInt()
    {
        var c = new C();
        Console.WriteLine(c.P1 ??= GetInt(1));
        Console.WriteLine(c.P1 ??= GetInt(2));
    }

    public static void TestObject()
    {
        var c = new C();
        Console.WriteLine(c.P2 ??= GetInt(3));
        Console.WriteLine(c.P2 ??= GetInt(4));
    }
}
", expectedOutput: @"
In Get P1
In GetInt
In Set P1
1
In Get P1
1
In Get P2
In GetInt
In Set P2
3
In Get P2
3");

            verifier.VerifyIL("C.TestInt()", @"
{
  // Code size      119 (0x77)
  .maxstack  4
  .locals init (C V_0,
                int? V_1,
                int V_2,
                int? V_3)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""int? C.P1.get""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""int int?.GetValueOrDefault()""
  IL_0015:  stloc.2
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       ""bool int?.HasValue.get""
  IL_001d:  brtrue.s   IL_0038
  IL_001f:  ldc.i4.1
  IL_0020:  call       ""int C.GetInt(int)""
  IL_0025:  stloc.2
  IL_0026:  ldloc.0
  IL_0027:  ldloca.s   V_3
  IL_0029:  ldloc.2
  IL_002a:  call       ""int?..ctor(int)""
  IL_002f:  ldloc.3
  IL_0030:  callvirt   ""void C.P1.set""
  IL_0035:  ldloc.2
  IL_0036:  br.s       IL_0039
  IL_0038:  ldloc.2
  IL_0039:  call       ""void System.Console.WriteLine(int)""
  IL_003e:  stloc.0
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""int? C.P1.get""
  IL_0045:  stloc.1
  IL_0046:  ldloca.s   V_1
  IL_0048:  call       ""int int?.GetValueOrDefault()""
  IL_004d:  stloc.2
  IL_004e:  ldloca.s   V_1
  IL_0050:  call       ""bool int?.HasValue.get""
  IL_0055:  brtrue.s   IL_0070
  IL_0057:  ldc.i4.2
  IL_0058:  call       ""int C.GetInt(int)""
  IL_005d:  stloc.2
  IL_005e:  ldloc.0
  IL_005f:  ldloca.s   V_3
  IL_0061:  ldloc.2
  IL_0062:  call       ""int?..ctor(int)""
  IL_0067:  ldloc.3
  IL_0068:  callvirt   ""void C.P1.set""
  IL_006d:  ldloc.2
  IL_006e:  br.s       IL_0071
  IL_0070:  ldloc.2
  IL_0071:  call       ""void System.Console.WriteLine(int)""
  IL_0076:  ret
}");

            verifier.VerifyIL("C.TestObject()", @"
    {
      // Code size       79 (0x4f)
      .maxstack  4
      .locals init (C V_0,
                    object V_1)
      IL_0000:  newobj     ""C..ctor()""
      IL_0005:  dup
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  callvirt   ""object C.P2.get""
      IL_000d:  dup
      IL_000e:  brtrue.s   IL_0025
      IL_0010:  pop
      IL_0011:  ldloc.0
      IL_0012:  ldc.i4.3
      IL_0013:  call       ""int C.GetInt(int)""
      IL_0018:  box        ""int""
      IL_001d:  dup
      IL_001e:  stloc.1
      IL_001f:  callvirt   ""void C.P2.set""
      IL_0024:  ldloc.1
      IL_0025:  call       ""void System.Console.WriteLine(object)""
      IL_002a:  stloc.0
      IL_002b:  ldloc.0
      IL_002c:  callvirt   ""object C.P2.get""
      IL_0031:  dup
      IL_0032:  brtrue.s   IL_0049
      IL_0034:  pop
      IL_0035:  ldloc.0
      IL_0036:  ldc.i4.4
      IL_0037:  call       ""int C.GetInt(int)""
      IL_003c:  box        ""int""
      IL_0041:  dup
      IL_0042:  stloc.1
      IL_0043:  callvirt   ""void C.P2.set""
      IL_0048:  ldloc.1
      IL_0049:  call       ""void System.Console.WriteLine(object)""
      IL_004e:  ret
    }
    ");
        }

        [Fact]
        public void EventLvalue()
        {
            CompileAndVerify(@"
using System;
class C
{
    static event EventHandler E;
    public static void Main()
    {
        E ??= (sender, args) => {};
    }
}").VerifyIL("C.Main()", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.EventHandler C.E""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldsfld     ""System.EventHandler C.<>c.<>9__3_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""void C.<>c.<Main>b__3_0(object, System.EventArgs)""
  IL_001b:  newobj     ""System.EventHandler..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.EventHandler C.<>c.<>9__3_0""
  IL_0026:  stsfld     ""System.EventHandler C.E""
  IL_002b:  ret
}
");
        }

        [Fact]
        public void InvalidLHS()
        {
            CreateCompilation(@"
public class C
{
    int? WriteOnlyProperty { set {} }
    int? ReadOnlyProperty { get; }

    public static void Main()
    {
        var c = new C();
        c.WriteOnlyProperty ??= 1; // Non rvalue write only
        c.ReadOnlyProperty ??= 1; // Non lvalue readonly
        GetInt() ??= 1; // Non lvalue method invocation
        GetInt ??= null; // Non lvalue method group
        () => {} ??= null; // Non lvalue lambda
    }

    static int? GetInt() => null;
}
").VerifyDiagnostics(
                // (10,9): error CS0154: The property or indexer 'C.WriteOnlyProperty' cannot be used in this context because it lacks the get accessor
                //         c.WriteOnlyProperty ??= 1; // Non rvalue write only
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "c.WriteOnlyProperty").WithArguments("C.WriteOnlyProperty").WithLocation(10, 9),
                // (11,9): error CS0200: Property or indexer 'C.ReadOnlyProperty' cannot be assigned to -- it is read only
                //         c.ReadOnlyProperty ??= 1; // Non lvalue readonly
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "c.ReadOnlyProperty").WithArguments("C.ReadOnlyProperty").WithLocation(11, 9),
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         GetInt() ??= 1; // Non lvalue method invocation
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "GetInt()").WithLocation(12, 9),
                // (13,9): error CS1656: Cannot assign to 'GetInt' because it is a 'method group'
                //         GetInt ??= null; // Non lvalue method group
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "GetInt").WithArguments("GetInt", "method group").WithLocation(13, 9),
                // (14,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         () => {} ??= null; // Non lvalue lambda
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "() => {}").WithLocation(14, 9)
            );
        }

        [Fact]
        public void ValidRHS()
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
", parseOptions: TestOptions.Regular10, expectedOutput: @"
In TestMethod
In TestMethod
").VerifyIL("C.Main()", @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Action V_0) //a
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0015
  IL_0006:  pop
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.TestMethod()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  dup
  IL_0014:  stloc.0
  IL_0015:  callvirt   ""void System.Action.Invoke()""
  IL_001a:  ldloc.0
  IL_001b:  dup
  IL_001c:  brtrue.s   IL_0040
  IL_001e:  pop
  IL_001f:  ldsfld     ""System.Action C.<>c.<>9__0_0""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003e
  IL_0027:  pop
  IL_0028:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_002d:  ldftn      ""void C.<>c.<Main>b__0_0()""
  IL_0033:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0038:  dup
  IL_0039:  stsfld     ""System.Action C.<>c.<>9__0_0""
  IL_003e:  dup
  IL_003f:  stloc.0
  IL_0040:  callvirt   ""void System.Action.Invoke()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void InvalidRHS()
        {
            CreateCompilation(@"
public class C
{
    static int P1 { set {} }

    public static void Main()
    {
        var c = new C();
        c ??= P1;
    }
}
").VerifyDiagnostics(
                // (9,15): error CS0154: The property or indexer 'C.P1' cannot be used in this context because it lacks the get accessor
                //         c ??= P1;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P1").WithArguments("C.P1").WithLocation(9, 15)
            );
        }

        [Fact]
        public void RefReturnLvalue()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;
    ref int? P1
    {
        get
        {
            Console.WriteLine(""In Get P1"");
            return ref f1;
        }
    }

    ref int? GetF1()
    {
        Console.WriteLine(""In GetF1"");
        return ref f1;
    }

    object f2 = null;
    ref object P2
    {
        get
        {
            Console.WriteLine(""In Get P2"");
            return ref f2;
        }
    }

    ref object GetF2()
    {
        Console.WriteLine(""In GetF2"");
        return ref f2;
    }

    public static void TestInt()
    {
        var c = new C();
        Console.WriteLine(c.P1 ??= 1);
        c.f1 = null;
        Console.WriteLine(c.GetF1() ??= 2);
    }

    public static void TestObject()
    {
        var c = new C();
        Console.WriteLine(c.P2 ??= 1);
        c.f2 = null;
        Console.WriteLine(c.GetF2() ??= 2);
    }

    public static void Main()
    {
        TestInt();
        TestObject();
    }
}
", expectedOutput: @"
In Get P1
1
In GetF1
2
In Get P2
1
In GetF2
2");

            verifier.VerifyIL("C.TestInt()", @"
{
  // Code size      107 (0x6b)
  .maxstack  3
  .locals init (int?& V_0,
                int V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref int? C.P1.get""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""int int?.GetValueOrDefault()""
  IL_0012:  stloc.1
  IL_0013:  ldloc.0
  IL_0014:  call       ""bool int?.HasValue.get""
  IL_0019:  brtrue.s   IL_002c
  IL_001b:  ldc.i4.1
  IL_001c:  stloc.1
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  newobj     ""int?..ctor(int)""
  IL_0024:  stobj      ""int?""
  IL_0029:  ldloc.1
  IL_002a:  br.s       IL_002d
  IL_002c:  ldloc.1
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  dup
  IL_0033:  ldflda     ""int? C.f1""
  IL_0038:  initobj    ""int?""
  IL_003e:  callvirt   ""ref int? C.GetF1()""
  IL_0043:  stloc.0
  IL_0044:  ldloc.0
  IL_0045:  call       ""int int?.GetValueOrDefault()""
  IL_004a:  stloc.1
  IL_004b:  ldloc.0
  IL_004c:  call       ""bool int?.HasValue.get""
  IL_0051:  brtrue.s   IL_0064
  IL_0053:  ldc.i4.2
  IL_0054:  stloc.1
  IL_0055:  ldloc.0
  IL_0056:  ldloc.1
  IL_0057:  newobj     ""int?..ctor(int)""
  IL_005c:  stobj      ""int?""
  IL_0061:  ldloc.1
  IL_0062:  br.s       IL_0065
  IL_0064:  ldloc.1
  IL_0065:  call       ""void System.Console.WriteLine(int)""
  IL_006a:  ret
}");

            verifier.VerifyIL("C.TestObject()", @"
{
  // Code size       70 (0x46)
  .maxstack  4
  .locals init (object& V_0,
                object V_1)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref object C.P2.get""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldind.ref
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_001d
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  box        ""int""
  IL_0019:  dup
  IL_001a:  stloc.1
  IL_001b:  stind.ref
  IL_001c:  ldloc.1
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  dup
  IL_0023:  ldnull
  IL_0024:  stfld      ""object C.f2""
  IL_0029:  callvirt   ""ref object C.GetF2()""
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldind.ref
  IL_0031:  dup
  IL_0032:  brtrue.s   IL_0040
  IL_0034:  pop
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4.2
  IL_0037:  box        ""int""
  IL_003c:  dup
  IL_003d:  stloc.1
  IL_003e:  stind.ref
  IL_003f:  ldloc.1
  IL_0040:  call       ""void System.Console.WriteLine(object)""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void RefReadonlyReturnLvalue()
        {
            CreateCompilation(@"
using System;
public class C
{
    int? f1 = null;
    ref readonly int? P1
    {
        get
        {
            Console.WriteLine(""In Get P1"");
            return ref f1;
        }
    }

    ref readonly int? GetF1()
    {
        Console.WriteLine(""In GetF1"");
        return ref f1;
    }

    public static void Main()
    {
        var c = new C();
        Console.WriteLine(c.P1 ??= 1);
        c.f1 = null;
        Console.WriteLine(c.GetF1() ??= 2);
    }
}").VerifyDiagnostics(
                // (24,27): error CS8331: Cannot assign to property 'P1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         Console.WriteLine(c.P1 ??= 1);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.P1").WithArguments("property", "P1").WithLocation(24, 27),
                // (26,27): error CS8331: Cannot assign to method 'GetF1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         Console.WriteLine(c.GetF1() ??= 2);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.GetF1()").WithArguments("method", "GetF1").WithLocation(26, 27)
            );
        }

        [Fact]
        public void NonNullableLHS()
        {
            CreateCompilation(@"
public class C
{
    public static void Main()
    {
        int i1 = 0;
        i1 ??= 0;
    }
}").VerifyDiagnostics(
                // (8,9): error CS0019: Operator '??=' cannot be applied to operands of type 'int' and 'int'
                //         i1 ??= 0;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= 0").WithArguments("??=", "int", "int").WithLocation(7, 9)
            );
        }

        [Fact]
        public void ReferenceTypeLHS()
        {
            CompileAndVerify(@"
public class C
{
    static C P1 { get; set; }
    public static void Main()
    {
        Test(P1 ??= new C());
    }
    static void Test(C test) {}
}").VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  call       ""C C.P1.get""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0014
  IL_0008:  pop
  IL_0009:  newobj     ""C..ctor()""
  IL_000e:  dup
  IL_000f:  call       ""void C.P1.set""
  IL_0014:  call       ""void C.Test(C)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void RefVariable()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    static int? f1 = null;
    static object f2 = null;
    public static void TestInt()
    {
        ref int? i1 = ref f1;
        Console.WriteLine(i1 ??= 1);
        MInt(ref f1);
        Console.WriteLine(f1);
    }
    public static void MInt(ref int? i)
    {
        Console.WriteLine(i ??= 2);
        i = null;
        Console.WriteLine(i ??= 3);
    }

    public static void TestObject()
    {
        ref object o1 = ref f2;
        Console.WriteLine(o1 ??= 1);
        MObject(ref f2);
        Console.WriteLine(f2);
    }
    public static void MObject(ref object o)
    {
        Console.WriteLine(o ??= 2);
        o = null;
        Console.WriteLine(o ??= 3);
    }
    public static void Main()
    {
        TestInt();
        TestObject();
    }
}", expectedOutput: @"
1
1
3
3
1
1
3
3
");

            verifier.VerifyIL("C.TestInt()", expectedIL: @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (int?& V_0, //i1
                int V_1)
  IL_0000:  ldsflda    ""int? C.f1""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""int int?.GetValueOrDefault()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  brtrue.s   IL_0026
  IL_0015:  ldc.i4.1
  IL_0016:  stloc.1
  IL_0017:  ldloc.0
  IL_0018:  ldloc.1
  IL_0019:  newobj     ""int?..ctor(int)""
  IL_001e:  stobj      ""int?""
  IL_0023:  ldloc.1
  IL_0024:  br.s       IL_0027
  IL_0026:  ldloc.1
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  ldsflda    ""int? C.f1""
  IL_0031:  call       ""void C.MInt(ref int?)""
  IL_0036:  ldsfld     ""int? C.f1""
  IL_003b:  box        ""int?""
  IL_0040:  call       ""void System.Console.WriteLine(object)""
  IL_0045:  ret
}");

            verifier.VerifyIL("C.MInt(ref int?)", expectedIL: @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int int?.GetValueOrDefault()""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  call       ""bool int?.HasValue.get""
  IL_000d:  brtrue.s   IL_0020
  IL_000f:  ldc.i4.2
  IL_0010:  stloc.0
  IL_0011:  ldarg.0
  IL_0012:  ldloc.0
  IL_0013:  newobj     ""int?..ctor(int)""
  IL_0018:  stobj      ""int?""
  IL_001d:  ldloc.0
  IL_001e:  br.s       IL_0021
  IL_0020:  ldloc.0
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  ldarg.0
  IL_0027:  initobj    ""int?""
  IL_002d:  ldarg.0
  IL_002e:  call       ""int int?.GetValueOrDefault()""
  IL_0033:  stloc.0
  IL_0034:  ldarg.0
  IL_0035:  call       ""bool int?.HasValue.get""
  IL_003a:  brtrue.s   IL_004d
  IL_003c:  ldc.i4.3
  IL_003d:  stloc.0
  IL_003e:  ldarg.0
  IL_003f:  ldloc.0
  IL_0040:  newobj     ""int?..ctor(int)""
  IL_0045:  stobj      ""int?""
  IL_004a:  ldloc.0
  IL_004b:  br.s       IL_004e
  IL_004d:  ldloc.0
  IL_004e:  call       ""void System.Console.WriteLine(int)""
  IL_0053:  ret
}
");

            verifier.VerifyIL("C.TestObject()", expectedIL: @"
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (object& V_0, //o1
                object V_1)
  IL_0000:  ldsflda    ""object C.f2""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldind.ref
  IL_0008:  dup
  IL_0009:  brtrue.s   IL_0017
  IL_000b:  pop
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  box        ""int""
  IL_0013:  dup
  IL_0014:  stloc.1
  IL_0015:  stind.ref
  IL_0016:  ldloc.1
  IL_0017:  call       ""void System.Console.WriteLine(object)""
  IL_001c:  ldsflda    ""object C.f2""
  IL_0021:  call       ""void C.MObject(ref object)""
  IL_0026:  ldsfld     ""object C.f2""
  IL_002b:  call       ""void System.Console.WriteLine(object)""
  IL_0030:  ret
}
");

            verifier.VerifyIL("C.MObject(ref object)", expectedIL: @"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  dup
  IL_0003:  brtrue.s   IL_0011
  IL_0005:  pop
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.2
  IL_0008:  box        ""int""
  IL_000d:  dup
  IL_000e:  stloc.0
  IL_000f:  stind.ref
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  ldarg.0
  IL_0017:  ldnull
  IL_0018:  stind.ref
  IL_0019:  ldarg.0
  IL_001a:  ldind.ref
  IL_001b:  dup
  IL_001c:  brtrue.s   IL_002a
  IL_001e:  pop
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.3
  IL_0021:  box        ""int""
  IL_0026:  dup
  IL_0027:  stloc.0
  IL_0028:  stind.ref
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.WriteLine(object)""
  IL_002f:  ret
}
");
        }

        [Fact]
        public void RefAssignment()
        {
            var source1 = @"
using System;
class C
{
    static object f1 = null;
    static object f2 = ""F2"";
    static void Main()
    {
        ref object o1 = ref f1;
        ref object o2 = ref f2;
        Console.WriteLine(o1 ??= o2);
    }
}";

            CompileAndVerify(source1, expectedOutput: "F2").VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (object& V_0, //o1
                object& V_1, //o2
                object V_2)
  IL_0000:  ldsflda    ""object C.f1""
  IL_0005:  stloc.0
  IL_0006:  ldsflda    ""object C.f2""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  ldind.ref
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0019
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  ldind.ref
  IL_0015:  dup
  IL_0016:  stloc.2
  IL_0017:  stind.ref
  IL_0018:  ldloc.2
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}
");

            var source2 = @"
class C
{
    object f1;
    void M()
    {
        ref object o1 = ref f1;
        ref object o2 = ref f1;
        o1 ??= ref o2;
        ref o1 ??= ref o2;
    }
}";

            CreateCompilation(source2).VerifyDiagnostics(
                // (9,16): error CS1525: Invalid expression term 'ref'
                //         o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref o2").WithArguments("ref").WithLocation(9, 16),
                // (9,16): error CS1073: Unexpected token 'ref'
                //         o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 16),
                // (10,13): error CS0118: 'o1' is a variable but is used like a type
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_BadSKknown, "o1").WithArguments("o1", "variable", "type").WithLocation(10, 13),
                // (10,16): error CS1001: Identifier expected
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "??=").WithLocation(10, 16),
                // (10,16): error CS1002: ; expected
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "??=").WithLocation(10, 16),
                // (10,16): error CS1525: Invalid expression term '??='
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "??=").WithArguments("??=").WithLocation(10, 16),
                // (10,16): error CS8174: A declaration of a by-reference variable must have an initializer
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "").WithLocation(10, 16),
                // (10,20): error CS1525: Invalid expression term 'ref'
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref o2").WithArguments("ref").WithLocation(10, 20),
                // (10,20): error CS1073: Unexpected token 'ref'
                //         ref o1 ??= ref o2;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(10, 20));
        }

        [Fact]
        public void InParameter()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        M1(null);
    }

    static void M1(object o)
    {
        M2(o ??= ""Test String"");
    }

    static void M2(in object o) => Console.WriteLine(o);
}";

            var verifier = CompileAndVerify(source, expectedOutput: "Test String");
            verifier.VerifyIL("C.M1(object)", expectedIL: @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000d
  IL_0004:  pop
  IL_0005:  ldstr      ""Test String""
  IL_000a:  dup
  IL_000b:  starg.s    V_0
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""void C.M2(in object)""
  IL_0015:  ret
}
");

            var source2 = @"
class C
{
    static void Main()
    {
        object o = null;
        M(in (o ??= ""Test String""));
    }

    static void M(in object o) {}
}";

            CreateCompilation(source2).VerifyDiagnostics(
                // (7,15): error CS1510: A ref or out value must be an assignable variable
                //         M(in (o ??= "Test String"));
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, @"o ??= ""Test String""").WithLocation(7, 15)
            );
        }

        [Fact]
        public void AsyncLHSAndRHS()
        {
            CompileAndVerify(@"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    {
        Console.WriteLine((await Run()).P ??= 1);
        int? i = null;
        Console.WriteLine(i ??= await GetIntAsync(2));
        Console.WriteLine(i ??= await GetIntAsync(3));
    }

    int? f1 = null;
    int? P {
        get
        {
            Console.WriteLine(""In Get"");
            return f1;
        }
        // Purposefully don't set field to ensure property isn't read twice (wrong value will print if it is)
        set => Console.WriteLine(""In Set"");
    }
    static Task<C> Run()
    {
        return Task.Run(() => new C());
    }
    static Task<int> GetIntAsync(int i)
    {
        Console.WriteLine(""In GetInt"");
        return Task.Run(() => i);
    }
}", expectedOutput: @"
In Get
In Set
1
In GetInt
2
2");
        }

        [Fact]
        public void TupleLHS()
        {
            var verifier = CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        TestInt();
        TestObject();
    }
    public static void TestInt()
    {
        (int, int)? a = null;
        Console.WriteLine(a ??= (1, 2));
    }
    public static void TestObject()
    {
        (object f1, object f2)? b = null;
        Console.WriteLine(b ??= (f3: null, f4: null));
    }
}", expectedOutput: @"
(1, 2)
(, )
");
            verifier.VerifyIL("C.TestInt()", expectedIL: @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (System.ValueTuple<int, int>? V_0, //a
                System.ValueTuple<int, int> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.ValueTuple<int, int>?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""System.ValueTuple<int, int> System.ValueTuple<int, int>?.GetValueOrDefault()""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool System.ValueTuple<int, int>?.HasValue.get""
  IL_0017:  brtrue.s   IL_002d
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.1
  IL_001c:  ldc.i4.2
  IL_001d:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0022:  ldloca.s   V_0
  IL_0024:  ldloc.1
  IL_0025:  call       ""System.ValueTuple<int, int>?..ctor(System.ValueTuple<int, int>)""
  IL_002a:  ldloc.1
  IL_002b:  br.s       IL_002e
  IL_002d:  ldloc.1
  IL_002e:  box        ""System.ValueTuple<int, int>""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  ret
}");

            verifier.VerifyIL("C.TestObject()", expectedIL: @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (System.ValueTuple<object, object>? V_0, //b
                System.ValueTuple<object, object> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.ValueTuple<object, object>?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""System.ValueTuple<object, object> System.ValueTuple<object, object>?.GetValueOrDefault()""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""bool System.ValueTuple<object, object>?.HasValue.get""
  IL_0017:  brtrue.s   IL_002d
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldnull
  IL_001c:  ldnull
  IL_001d:  call       ""System.ValueTuple<object, object>..ctor(object, object)""
  IL_0022:  ldloca.s   V_0
  IL_0024:  ldloc.1
  IL_0025:  call       ""System.ValueTuple<object, object>?..ctor(System.ValueTuple<object, object>)""
  IL_002a:  ldloc.1
  IL_002b:  br.s       IL_002e
  IL_002d:  ldloc.1
  IL_002e:  box        ""System.ValueTuple<object, object>""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  ret
}");
        }

        [Fact]
        public void ValidRHSConversions()
        {
            var compilation = CreateCompilation(@"
class C
{
    public static void Main()
    {
        // Implicit reference conversion
        C c1 = null;
        UseParam(c1 ??= new D());

        // Implicit user-defined conversion
        C c2 = null;
        UseParam(c2 ??= new E());

        // Implicit user-defined conversion on the rhs type
        C c3 = null;
        UseParam(c3 ??= new F());

        // Implicit user-defined conversion to the underlying non-nullable type of lhs
        int? i1 = null;
        UseParam(i1 ??= new C());

        // Implicit conversion from null to reference type
        C c4 = null;
        UseParam(c4 ??= null);

        // Implicit conversion from default literal to reference type
        C c5 = null;
        UseParam(c5 ??= default);

        int? i2 = null;
        UseParamAsInt(i2 ??= default);
    }

    static void UseParam(object o) {}
    static void UseParamAsInt(int i) {}

    static public implicit operator C(E e) => null;
    static public implicit operator int(C c) => 0;
}
class D : C {}
class E {}
class F
{
    static public implicit operator C(F e) => null;
}
");

            compilation.VerifyDiagnostics();

            CompileAndVerify(compilation).VerifyIL("C.Main()", @"
{
  // Code size      180 (0xb4)
  .maxstack  2
  .locals init (int? V_0, //i1
                int? V_1, //i2
                int V_2)
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000a
  IL_0004:  pop
  IL_0005:  newobj     ""D..ctor()""
  IL_000a:  call       ""void C.UseParam(object)""
  IL_000f:  ldnull
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  pop
  IL_0014:  newobj     ""E..ctor()""
  IL_0019:  call       ""C C.op_Implicit(E)""
  IL_001e:  call       ""void C.UseParam(object)""
  IL_0023:  ldnull
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_0032
  IL_0027:  pop
  IL_0028:  newobj     ""F..ctor()""
  IL_002d:  call       ""C F.op_Implicit(F)""
  IL_0032:  call       ""void C.UseParam(object)""
  IL_0037:  ldloca.s   V_0
  IL_0039:  initobj    ""int?""
  IL_003f:  ldloca.s   V_0
  IL_0041:  call       ""int int?.GetValueOrDefault()""
  IL_0046:  stloc.2
  IL_0047:  ldloca.s   V_0
  IL_0049:  call       ""bool int?.HasValue.get""
  IL_004e:  brtrue.s   IL_0066
  IL_0050:  newobj     ""C..ctor()""
  IL_0055:  call       ""int C.op_Implicit(C)""
  IL_005a:  stloc.2
  IL_005b:  ldloca.s   V_0
  IL_005d:  ldloc.2
  IL_005e:  call       ""int?..ctor(int)""
  IL_0063:  ldloc.2
  IL_0064:  br.s       IL_0067
  IL_0066:  ldloc.2
  IL_0067:  box        ""int""
  IL_006c:  call       ""void C.UseParam(object)""
  IL_0071:  ldnull
  IL_0072:  dup
  IL_0073:  brtrue.s   IL_0077
  IL_0075:  pop
  IL_0076:  ldnull
  IL_0077:  call       ""void C.UseParam(object)""
  IL_007c:  ldnull
  IL_007d:  dup
  IL_007e:  brtrue.s   IL_0082
  IL_0080:  pop
  IL_0081:  ldnull
  IL_0082:  call       ""void C.UseParam(object)""
  IL_0087:  ldloca.s   V_1
  IL_0089:  initobj    ""int?""
  IL_008f:  ldloca.s   V_1
  IL_0091:  call       ""int int?.GetValueOrDefault()""
  IL_0096:  stloc.2
  IL_0097:  ldloca.s   V_1
  IL_0099:  call       ""bool int?.HasValue.get""
  IL_009e:  brtrue.s   IL_00ad
  IL_00a0:  ldc.i4.0
  IL_00a1:  stloc.2
  IL_00a2:  ldloca.s   V_1
  IL_00a4:  ldloc.2
  IL_00a5:  call       ""int?..ctor(int)""
  IL_00aa:  ldloc.2
  IL_00ab:  br.s       IL_00ae
  IL_00ad:  ldloc.2
  IL_00ae:  call       ""void C.UseParamAsInt(int)""
  IL_00b3:  ret
}");
        }

        [Fact]
        public void InvalidRHSConversions()
        {
            CreateCompilation(@"
class C
{
    public void M()
    {
        // Explicit numeric conversion
        int? i1 = null;
        i1 ??= 1.0;

        // Explicit reference conversion
        D d1 = null;
        d1 ??= new C();

        // Explicit user-defined conversion
        C c1 = null;
        c1 ??= new E();

        // No conversion between lhs and rhs
        C c2 = null;
        c2 ??= new F();
    }

    static public explicit operator C(E e) => null;
}
class D : C {}
class E {}
class F {}
").VerifyDiagnostics(
                // (8,9): error CS0019: Operator '??=' cannot be applied to operands of type 'int?' and 'double'
                //         i1 ??= 1.0;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= 1.0").WithArguments("??=", "int?", "double").WithLocation(8, 9),
                // (12,9): error CS0019: Operator '??=' cannot be applied to operands of type 'D' and 'C'
                //         d1 ??= new C();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "d1 ??= new C()").WithArguments("??=", "D", "C").WithLocation(12, 9),
                // (16,9): error CS0019: Operator '??=' cannot be applied to operands of type 'C' and 'E'
                //         c1 ??= new E();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 ??= new E()").WithArguments("??=", "C", "E").WithLocation(16, 9),
                // (20,9): error CS0019: Operator '??=' cannot be applied to operands of type 'C' and 'F'
                //         c2 ??= new F();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c2 ??= new F()").WithArguments("??=", "C", "F").WithLocation(20, 9)
            );
        }

        [Fact]
        public void AsInvalidSubexpression()
        {
            CreateCompilation(@"
class C
{
    public void M()
    {
        double? d = 1.0;
        M3(d ??= 3.0);
        object o = null;
        M3(o ??= null);
        (o ??= null) = null;
    }

    public void M3(C c) {}
}
").VerifyDiagnostics(
                // (7,12): error CS1503: Argument 1: cannot convert from 'double' to 'C'
                //         M3(d ??= 3.0);
                Diagnostic(ErrorCode.ERR_BadArgType, "d ??= 3.0").WithArguments("1", "double", "C").WithLocation(7, 12),
                // (9,12): error CS1503: Argument 1: cannot convert from 'object' to 'C'
                //         M3(o ??= null);
                Diagnostic(ErrorCode.ERR_BadArgType, "o ??= null").WithArguments("1", "object", "C").WithLocation(9, 12),
                // (10,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (o ??= null) = null;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "o ??= null").WithLocation(10, 10)
            );
        }

        [Fact]
        public void DynamicLHS()
        {
            var verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        Console.WriteLine(VerifyField());
        Console.WriteLine(VerifyProperty());
        Console.WriteLine(VerifyIndexer());
    }

    public static int? VerifyField()
    {
        return GetDynamicClass().F1 ??= 0;
    }
    public static int? VerifyProperty()
    {
        return GetDynamicClass().P1 ??= 1;
    }
    public static int? VerifyIndexer()
    {
        return GetDynamicClass()[0] ??= 2;
    }

    public static dynamic GetDynamicClass() => new DynamicClass();
}
public class DynamicClass
{
#pragma warning disable CS0169
    public int? F1;
#pragma warning restore CS0169
    private int? f2;
    public int? P1
    {
        get
        {
            Console.WriteLine(""In P1 Getter"");
            return f2;
        }
        set
        {
            Console.WriteLine(""In P1 Setter"");
            f2 = value;
        }
    }
    private Dictionary<int, int?> dictionary;
    public int? this[int i]
    {
        get
        {
            Console.WriteLine(""In Indexer Getter"");
            if (dictionary == null)
            {
                dictionary = new Dictionary<int, int?>();
                dictionary[i] = null;
            }
            return dictionary[i];
        }
        set
        {
            Console.WriteLine(""In Indexer Setter"");
            dictionary[i] = value;
        }
    }
}", new[] { CSharpRef }, expectedOutput: @"
0
In P1 Getter
In P1 Setter
1
In Indexer Getter
In Indexer Setter
2");

            verifier.VerifyIL("C.VerifyField()", expectedIL: @"
{
  // Code size      240 (0xf0)
  .maxstack  10
  .locals init (object V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__1.<>p__2""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""int?""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__1.<>p__2""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__1.<>p__2""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__1.<>p__2""
  IL_003a:  call       ""dynamic C.GetDynamicClass()""
  IL_003f:  stloc.0
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_0045:  brtrue.s   IL_0076
  IL_0047:  ldc.i4.0
  IL_0048:  ldstr      ""F1""
  IL_004d:  ldtoken    ""C""
  IL_0052:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0057:  ldc.i4.1
  IL_0058:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005d:  dup
  IL_005e:  ldc.i4.0
  IL_005f:  ldc.i4.0
  IL_0060:  ldnull
  IL_0061:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0066:  stelem.ref
  IL_0067:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0071:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_0076:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_007b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0080:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_0085:  ldloc.0
  IL_0086:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_008b:  dup
  IL_008c:  brtrue.s   IL_00ea
  IL_008e:  pop
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_0094:  brtrue.s   IL_00cf
  IL_0096:  ldc.i4.0
  IL_0097:  ldstr      ""F1""
  IL_009c:  ldtoken    ""C""
  IL_00a1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a6:  ldc.i4.2
  IL_00a7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00ac:  dup
  IL_00ad:  ldc.i4.0
  IL_00ae:  ldc.i4.0
  IL_00af:  ldnull
  IL_00b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b5:  stelem.ref
  IL_00b6:  dup
  IL_00b7:  ldc.i4.1
  IL_00b8:  ldc.i4.0
  IL_00b9:  ldnull
  IL_00ba:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bf:  stelem.ref
  IL_00c0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00ca:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00d4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_00d9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00de:  ldloc.0
  IL_00df:  ldc.i4.0
  IL_00e0:  box        ""int""
  IL_00e5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_00ea:  callvirt   ""int? System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00ef:  ret
}
");

            verifier.VerifyIL("C.VerifyProperty()", expectedIL: @"
{
  // Code size      240 (0xf0)
  .maxstack  10
  .locals init (object V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__2.<>p__2""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""int?""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__2.<>p__2""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__2.<>p__2""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__2.<>p__2""
  IL_003a:  call       ""dynamic C.GetDynamicClass()""
  IL_003f:  stloc.0
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_0045:  brtrue.s   IL_0076
  IL_0047:  ldc.i4.0
  IL_0048:  ldstr      ""P1""
  IL_004d:  ldtoken    ""C""
  IL_0052:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0057:  ldc.i4.1
  IL_0058:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_005d:  dup
  IL_005e:  ldc.i4.0
  IL_005f:  ldc.i4.0
  IL_0060:  ldnull
  IL_0061:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0066:  stelem.ref
  IL_0067:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_006c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0071:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_0076:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_007b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0080:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_0085:  ldloc.0
  IL_0086:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_008b:  dup
  IL_008c:  brtrue.s   IL_00ea
  IL_008e:  pop
  IL_008f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_0094:  brtrue.s   IL_00cf
  IL_0096:  ldc.i4.0
  IL_0097:  ldstr      ""P1""
  IL_009c:  ldtoken    ""C""
  IL_00a1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a6:  ldc.i4.2
  IL_00a7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00ac:  dup
  IL_00ad:  ldc.i4.0
  IL_00ae:  ldc.i4.0
  IL_00af:  ldnull
  IL_00b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b5:  stelem.ref
  IL_00b6:  dup
  IL_00b7:  ldc.i4.1
  IL_00b8:  ldc.i4.0
  IL_00b9:  ldnull
  IL_00ba:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00bf:  stelem.ref
  IL_00c0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00c5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00ca:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00d4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_00d9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00de:  ldloc.0
  IL_00df:  ldc.i4.1
  IL_00e0:  box        ""int""
  IL_00e5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_00ea:  callvirt   ""int? System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00ef:  ret
}
");

            verifier.VerifyIL("C.VerifyIndexer()", expectedIL: @"
{
  // Code size      252 (0xfc)
  .maxstack  9
  .locals init (object V_0)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__3.<>p__2""
  IL_0005:  brtrue.s   IL_002b
  IL_0007:  ldc.i4.0
  IL_0008:  ldtoken    ""int?""
  IL_000d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0021:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0026:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__3.<>p__2""
  IL_002b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__3.<>p__2""
  IL_0030:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>>.Target""
  IL_0035:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>> C.<>o__3.<>p__2""
  IL_003a:  call       ""dynamic C.GetDynamicClass()""
  IL_003f:  stloc.0
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_0045:  brtrue.s   IL_007b
  IL_0047:  ldc.i4.0
  IL_0048:  ldtoken    ""C""
  IL_004d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0052:  ldc.i4.2
  IL_0053:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldc.i4.0
  IL_005b:  ldnull
  IL_005c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0061:  stelem.ref
  IL_0062:  dup
  IL_0063:  ldc.i4.1
  IL_0064:  ldc.i4.3
  IL_0065:  ldnull
  IL_0066:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_006b:  stelem.ref
  IL_006c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0071:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0076:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_007b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_0080:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>>.Target""
  IL_0085:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_008a:  ldloc.0
  IL_008b:  ldc.i4.0
  IL_008c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, int)""
  IL_0091:  dup
  IL_0092:  brtrue.s   IL_00f6
  IL_0094:  pop
  IL_0095:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_009a:  brtrue.s   IL_00da
  IL_009c:  ldc.i4.0
  IL_009d:  ldtoken    ""C""
  IL_00a2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a7:  ldc.i4.3
  IL_00a8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00ad:  dup
  IL_00ae:  ldc.i4.0
  IL_00af:  ldc.i4.0
  IL_00b0:  ldnull
  IL_00b1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00b6:  stelem.ref
  IL_00b7:  dup
  IL_00b8:  ldc.i4.1
  IL_00b9:  ldc.i4.3
  IL_00ba:  ldnull
  IL_00bb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00c0:  stelem.ref
  IL_00c1:  dup
  IL_00c2:  ldc.i4.2
  IL_00c3:  ldc.i4.0
  IL_00c4:  ldnull
  IL_00c5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00ca:  stelem.ref
  IL_00cb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00d0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00d5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_00da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_00df:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>>.Target""
  IL_00e4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_00e9:  ldloc.0
  IL_00ea:  ldc.i4.0
  IL_00eb:  ldc.i4.2
  IL_00ec:  box        ""int""
  IL_00f1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic)""
  IL_00f6:  callvirt   ""int? System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int?>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00fb:  ret
}
");
        }

        [Fact]
        public void DynamicRHS()
        {
            CompileAndVerify(@"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        int? d = null;
        Console.WriteLine(d ??= GetDynamicClass().Property1);
        Console.WriteLine(d ??= GetDynamicClass().Property2);
    }

    public static dynamic GetDynamicClass() => new DynamicClass();
}
public class DynamicClass
{
    public int Property1
    {
        get
        {
            Console.WriteLine(""In get_Property1"");
            return 1;
        }
    }
    public int Property2
    {
        get
        {
            Console.WriteLine(""In get_Property2"");
            return 2;
        }
    }
}
", new[] { CSharpRef }, expectedOutput: @"
In get_Property1
1
1
");
        }

        [Fact]
        public void UseBeforeAssignment()
        {
            CreateCompilation(@"
public class C
{
    public static void Main()
    {
        C c1, c2;
        c1 ??= new C(); // LHS unassigned

        c1 = null;
        c1 ??= c2; // RHS unassigned

        object x1;
        object y1;
        GetC(x1 = 1).Prop ??= (y1 = 2);
        x1.ToString();
        y1.ToString();

        object x2;
        object y2;
        GetC(x2 = 1).Field ??= (y2 = 2);
        x2.ToString();
        y2.ToString();
    }
    static C GetC(object i) => null;
    object Prop { get; set; }
    object Field;
}").VerifyDiagnostics(
                // (7,9): error CS0165: Use of unassigned local variable 'c1'
                //         c1 ??= new C(); // LHS unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c1").WithArguments("c1").WithLocation(7, 9),
                // (10,16): error CS0165: Use of unassigned local variable 'c2'
                //         c1 ??= c2; // RHS unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "c2").WithArguments("c2").WithLocation(10, 16),
                // (16,9): error CS0165: Use of unassigned local variable 'y1'
                //         y1.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(16, 9),
                // (22,9): error CS0165: Use of unassigned local variable 'y2'
                //         y2.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(22, 9)
            );
        }

        [Fact]
        public void NonStaticInStaticContext()
        {
            CreateCompilation(@"
public class C
{
    C P { get; set; }
    public static void Main()
    {
        P ??= new C();
    }
}").VerifyDiagnostics(
                // (7,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.P'
                //         P ??= new C();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P").WithArguments("C.P").WithLocation(7, 9)
            );
        }

        [Fact]
        public void ThrowExpressionRHS()
        {
            CreateCompilation(@"
using System;
public class C
{
    public static void Main()
    {
        object o = null;
        o ??= throw new Exception();
    }
}
").VerifyDiagnostics(
                // (8,15): error CS8115: A throw expression is not allowed in this context.
                //         o ??= throw new Exception();
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(8, 15)
            );
        }

        [Fact]
        public void TypeParameterLHS()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main() => M(""Non Null Input"");
    static void M<T>(T t)
    {
        t ??= default;
        Console.WriteLine(t);
    }
}
", expectedOutput: "Non Null Input");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M<T>(T)", expectedIL: @"
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        ""T""
  IL_0008:  brtrue.s   IL_0012
  IL_000a:  ldarga.s   V_0
  IL_000c:  initobj    ""T""
  IL_0012:  ldarg.0
  IL_0013:  box        ""T""
  IL_0018:  call       ""void System.Console.WriteLine(object)""
  IL_001d:  ret
}
");

            CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        Verify<object>(null, ""Assignment Evaluated"");
        Verify<int>(default, 10);
        Verify<int?>(null, 1);
        Verify<int?>(2, 10);
    }
    static void Verify<T>(T t1, T t2)
    {
        Console.WriteLine(t1 ??= t2);
        Console.WriteLine(t1);
    }
}", expectedOutput: @"
Assignment Evaluated
Assignment Evaluated
0
0
1
1
2
2
");
        }

        [Fact]
        public void ConstrainedTypeParameter()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        M1(null, ""Test String"");
        M2((int?)null, 1);
    }
    static void M1<T>(T t1, T t2) where T : class
    {
        t1 ??= t2;
        Console.WriteLine(t1);
    }
    static void M2<T>(T? t1, T t2) where T : struct
    {
        t1 ??= t2;
        Console.WriteLine(t1);
    }
}", expectedOutput: @"
Test String
1
");

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1<T>(T, T)", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brtrue.s   IL_000b
  IL_0008:  ldarg.1
  IL_0009:  starg.s    V_0
  IL_000b:  ldarg.0
  IL_000c:  box        ""T""
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  ret
}
");
            verifier.VerifyIL("C.M2<T>(T?, T)", expectedIL: @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""T T?.GetValueOrDefault()""
  IL_0007:  stloc.0
  IL_0008:  ldarga.s   V_0
  IL_000a:  call       ""bool T?.HasValue.get""
  IL_000f:  brtrue.s   IL_001b
  IL_0011:  ldarg.1
  IL_0012:  stloc.0
  IL_0013:  ldarga.s   V_0
  IL_0015:  ldloc.0
  IL_0016:  call       ""T?..ctor(T)""
  IL_001b:  ldarg.0
  IL_001c:  box        ""T?""
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}
");

            CreateCompilation(@"
class C
{
    void M<T>(T t1, T t2) where T : struct
    {
        t1 ??= t2;
    }
}").VerifyDiagnostics(
                // (6,9): error CS0019: Operator '??=' cannot be applied to operands of type 'T' and 'T'
                //         t1 ??= t2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 ??= t2").WithArguments("??=", "T", "T").WithLocation(6, 9)
            );
        }

        [Fact]
        public void DynamicRuntimeCastFailure()
        {
            var source = @"
using System;
class C
{
    byte? B { get; set; } = null;
    static void Main() => M(new C(), int.MaxValue);
    static void M(dynamic d1, dynamic d2)
    {
        try
        {
            d1.B ??= d2;
            Console.WriteLine(""Should have thrown!"");
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) {}

        d1.B = (byte)1;

        // Should not throw, as B is non-null
        d1.B ??= d2;
    }
}";

            CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "");
        }

        [Fact]
        public void AwaitLHS()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    static async Task M(Task<Task> t1, Task t2)
    {
        await t1 ??= t2;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         await t1 ??= t2;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "await t1").WithLocation(7, 9)
            );
        }

        [Fact]
        public void OperatorEqualsIgnored()
        {
            var source = @"
using System;
class C
{
    public static bool operator==(C c1, C c2)
    {
        Console.WriteLine(""In operator=="");
        return false;
    }
    public static bool operator!=(C c1, C c2)
    {
        Console.WriteLine(""In operator!="");
        return false;
    }
    public override bool Equals(object o)
    {
        Console.WriteLine(""In C.Equals"");
        return false;
    }
    public override int GetHashCode() => 1;

    public static void Main()
    {
        C c1 = null, c2 = new C();
        c1 ??= c2;
        Console.WriteLine(c1.GetHashCode());
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (C V_0, //c1
                C V_1) //c2
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  newobj     ""C..ctor()""
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  brtrue.s   IL_000d
  IL_000b:  ldloc.1
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""int object.GetHashCode()""
  IL_0013:  call       ""void System.Console.WriteLine(int)""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void Associativity()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        int? a1 = null, b1 = null, c1 = 1;
        Console.WriteLine(a1 ??= b1 ??= c1);
        Console.WriteLine(b1);
        int? a2 = null, b2 = null, c2 = 1;
        Console.WriteLine(a2 ??= (b2 ??= c2));
        Console.WriteLine(b2);
    }
}";

            CompileAndVerify(source, expectedOutput: @"
1
1
1
1
");
        }

        [Fact]
        public void ThisDisallowed()
        {
            var source = @"
class C
{
    public void M1()
    {
        this ??= new C();
    }
}
struct S
{
    public void M2()
    {
        this ??= new S();
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this ??= new C();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(6, 9),
                // (13,9): error CS0019: Operator '??=' cannot be applied to operands of type 'S' and 'S'
                //         this ??= new S();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "this ??= new S()").WithArguments("??=", "S", "S").WithLocation(13, 9)
            );
        }

        [Fact]
        public void DefiniteAssignment()
        {
            var source = @"
class C
{
    void M()
    {
        object o1, o2;
        C c1 = new C();

        GetRef(o1 = """") ??= GetObject(o1);
        o1.ToString();


        GetRef() ??= GetObject(o2 = """", o2);
        o2.ToString();

        GetRef() ??= OutVar(out var o3).GetObject(o3);
        o3.ToString();
    }

    object o;
    ref object GetRef(object param = null) => ref o;
    object GetObject(object param1, object param2 = null) => null;
    C OutVar(out object o3)
    {
        o3 = null;
        return this;
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (14,9): error CS0165: Use of unassigned local variable 'o2'
                //         o2.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "o2").WithArguments("o2").WithLocation(14, 9),
                // (17,9): error CS0165: Use of unassigned local variable 'o3'
                //         o3.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "o3").WithArguments("o3").WithLocation(17, 9)
            );
        }

        [Fact]
        public void COMIndexedProperties()
        {
            var source1 =
@"Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class COMProp
    Private _p As New Dictionary(Of Integer, Object)
    Property P(index As Integer) As Object
        Get
            Return _p(index)
        End Get
        Set
            _p(index) = Value
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);

            var testSource = @"
using System;
class C
{
    static void Main()
    {
        var com = new COMProp();
        Console.WriteLine(com.P[1] ??= 2);
    }
}";

            var verifier = CompileAndVerify(testSource, references: new[] { reference1 });
            verifier.VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (COMProp V_0,
                object V_1)
  IL_0000:  newobj     ""COMProp..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  callvirt   ""object COMProp.P[int].get""
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_0021
  IL_0010:  pop
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.2
  IL_0014:  box        ""int""
  IL_0019:  dup
  IL_001a:  stloc.1
  IL_001b:  callvirt   ""void COMProp.P[int].set""
  IL_0020:  ldloc.1
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void NullableValueDefaultLiteralRHS()
        {
            var source = @"
class C
{
    void M()
    {
        int? a = null;
        int b = (a ??= default);
        b = (a ??= default(int));
        b = (a ??= default(int?)); // 1
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,14): error CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         b = (a ??= default(int?)); // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a ??= default(int?)").WithArguments("int?", "int").WithLocation(9, 14));
        }

        [Fact]
        public void InOutParamAssignment()
        {
            var source = @"
class C
{
    void M(in object o1, out object o2)
    {
        o1 ??= null;
        o2 ??= null;
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS8331: Cannot assign to variable 'o1' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         o1 ??= null;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "o1").WithArguments("variable", "o1").WithLocation(6, 9),
                // (7,9): error CS0269: Use of unassigned out parameter 'o2'
                //         o2 ??= null;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "o2").WithArguments("o2").WithLocation(7, 9)
            );
        }

        [Fact]
        public void ObsoleteConversion()
        {
            var source = @"
using System;
class C
{
    void M()
    {
        C c = null;
        c ??= new D();
        c ??= new E();
    }
}
class D
{
    [Obsolete(""D is obsolete"", true)]
    public static implicit operator C(D d) => null;
}
class E
{
    [Obsolete(""E is obsolete"", false)]
    public static implicit operator C(E e) => null;
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,15): error CS0619: 'D.implicit operator C(D)' is obsolete: 'D is obsolete'
                //         c ??= new D();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new D()").WithArguments("D.implicit operator C(D)", "D is obsolete").WithLocation(8, 15),
                // (9,15): warning CS0618: 'E.implicit operator C(E)' is obsolete: 'E is obsolete'
                //         c ??= new E();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "new E()").WithArguments("E.implicit operator C(E)", "E is obsolete").WithLocation(9, 15)
            );
        }

        [Fact]
        public void DestructuringSyntaxDoesNotWork()
        {
            var source = @"
class C
{
    void M()
    {
        int? x = null, y = null;
        (x, y) ??= (1, 2);
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (x, y) ??= (1, 2);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(x, y)").WithLocation(7, 9)
            );
        }

        [Fact]
        public void UseSiteDiagnostics()
        {
            var aSource = "public class A {}";
            var aRef = CreateCompilation(aSource).EmitToImageReference();
            var bSource = "public class B : A {}";
            var bRef = CreateCompilation(bSource, new[] { aRef }).EmitToImageReference();

            var testSource = @"
class C
{
    void M()
    {
        var c = new C();
        c ??= new B();
    }
}";

            var compilation = CreateCompilation(testSource, new[] { bRef });
            compilation.VerifyDiagnostics(
                // (7,9): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'c3ba0fb2-4f4f-4916-afc5-50566473adcc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         c ??= new B();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c ??= new B()").WithArguments("A", $"{aRef.Display}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 9),
                // (7,9): error CS0019: Operator '??=' cannot be applied to operands of type 'C' and 'B'
                //         c ??= new B();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c ??= new B()").WithArguments("??=", "C", "B").WithLocation(7, 9));
        }

        [Fact]
        [WorkItem(30024, "https://github.com/dotnet/roslyn/issues/30024")]
        public void SpanNotAllowedLHS()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M()
    {
        Span<byte> s1 = new Span<byte>();
        s1 ??= new Span<byte>();
        Span<byte>? s2 = null;
        s2 ??= new Span<byte>();
    }
}").VerifyDiagnostics(
                // (8,9): error CS0019: Operator '??=' cannot be applied to operands of type 'Span<byte>' and 'Span<byte>'
                //         s1 ??= new Span<byte>();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 ??= new Span<byte>()").WithArguments("??=", "System.Span<byte>", "System.Span<byte>").WithLocation(8, 9),
                // (9,9): error CS0306: The type 'Span<byte>' may not be used as a type argument
                //         Span<byte>? s2 = null;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "Span<byte>?").WithArguments("System.Span<byte>").WithLocation(9, 9));
        }

        [Fact]
        [WorkItem(32138, "https://github.com/dotnet/roslyn/issues/31238")]
        public void ExpressionTreeNotAllowed()
        {
            CreateCompilation(@"
using System;
using System.Linq.Expressions;
public class C {
    public object o = null;
    public void M() {
        String x = null;
        Expression<Func<string>> e0 = () => x ??= null;
        Expression<Func<object>> e1 = () => o ??= null;
    }
}").VerifyDiagnostics(
                    // (8,45): error CS8642: An expression tree may not contain a null coalescing assignment
                    //         Expression<Func<string>> e0 = () => x ??= null;
                    Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainNullCoalescingAssignment, "x ??= null").WithLocation(8, 45),
                    // (9,45): error CS8642: An expression tree may not contain a null coalescing assignment
                    //         Expression<Func<object>> e1 = () => o ??= null;
                    Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainNullCoalescingAssignment, "o ??= null").WithLocation(9, 45));
        }

        [Fact]
        public void PointersDisallowed()
        {
            CreateCompilation(@"
class C
{
    unsafe void M(int* i1, int* i2)
    {
        i1 ??= i2;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS0019: Operator '??=' cannot be applied to operands of type 'int*' and 'int*'
                //         i1 ??= i2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= i2").WithArguments("??=", "int*", "int*").WithLocation(6, 9));
        }

        [Fact]
        public void MissingMembers()
        {
            var source = @"
class C
{
    void M()
    {
        int? i = null;
        i ??= 1;
    }
}
";

            var c1 = CreateCompilation(source);
            c1.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);
            c1.VerifyEmitDiagnostics(
                // (7,9): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //         i ??= 1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(7, 9));

            var c2 = CreateCompilation(source);
            c2.MakeMemberMissing(SpecialMember.System_Nullable_T_get_HasValue);
            c2.VerifyEmitDiagnostics(
                // (7,9): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //         i ??= 1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(7, 9));
        }

        [Fact]
        public void DefaultConvertedToNonNullableValueType()
        {
            CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        int? a = null;
        Console.WriteLine(a ??= default);
        Console.WriteLine(a ??= 1);
    }
}
", expectedOutput: @"
0
0");
        }

        [Fact]
        public void ArrayAccessLHS()
        {
            var verifier = CompileAndVerify(@"
using System;
class C
{
    static void Main()
    {
        TestValueTypes();
        TestReferenceTypes();
    }
    static void TestValueTypes()
    {
        int?[] arr = new[] { (int?)0 };
        Console.WriteLine(arr[0] ??= 1);
    }
    static void TestReferenceTypes()
    {
        string[] arr = new string[] { null };
        Console.WriteLine(arr[0] ??= ""2"");
    }
}", expectedOutput: @"
0
2");

            verifier.VerifyIL("C.TestValueTypes()", @"
{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (int?& V_0,
                int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int?""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.0
  IL_0009:  newobj     ""int?..ctor(int)""
  IL_000e:  stelem     ""int?""
  IL_0013:  ldc.i4.0
  IL_0014:  ldelema    ""int?""
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  call       ""int int?.GetValueOrDefault()""
  IL_0020:  stloc.1
  IL_0021:  ldloc.0
  IL_0022:  call       ""bool int?.HasValue.get""
  IL_0027:  brtrue.s   IL_003a
  IL_0029:  ldc.i4.1
  IL_002a:  stloc.1
  IL_002b:  ldloc.0
  IL_002c:  ldloc.1
  IL_002d:  newobj     ""int?..ctor(int)""
  IL_0032:  stobj      ""int?""
  IL_0037:  ldloc.1
  IL_0038:  br.s       IL_003b
  IL_003a:  ldloc.1
  IL_003b:  call       ""void System.Console.WriteLine(int)""
  IL_0040:  ret
}");

            verifier.VerifyIL("C.TestReferenceTypes()", @"
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (string& V_0,
                string V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""string""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""string""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldind.ref
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_001d
  IL_0012:  pop
  IL_0013:  ldloc.0
  IL_0014:  ldstr      ""2""
  IL_0019:  dup
  IL_001a:  stloc.1
  IL_001b:  stind.ref
  IL_001c:  ldloc.1
  IL_001d:  call       ""void System.Console.WriteLine(string)""
  IL_0022:  ret
}");
        }

        [Fact]
        [WorkItem(36443, "https://github.com/dotnet/roslyn/issues/36443")]
        public void StructLHSAcrossAwait()
        {
            // This only works when compiling for debug mode currently.
            // https://github.com/dotnet/roslyn/issues/36443

            CompileAndVerify(@"
using System;
using System.Threading.Tasks;
struct S
{
    int? i;

    static async Task Main()
    {

        S s = default;
        Console.WriteLine(s.i ??= await GetInt());
        Console.WriteLine(s.i ??= await GetInt());
    }

    static Task<int?> GetInt() => Task.FromResult((int?)1);
}", expectedOutput: @"
1
1", options: TestOptions.DebugExe);
        }
    }
}
