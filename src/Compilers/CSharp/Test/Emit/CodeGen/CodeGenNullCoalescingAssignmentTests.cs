// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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

            // PROTOTYPE(null-operator-enhancements): lines 8 and 9 appear to be entirely redundant.
            verifier.VerifyIL("C.TestNullable()", @"
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool int?.HasValue.get""
  IL_0011:  brtrue.s   IL_001f
  IL_0013:  call       ""int C.GetInt()""
  IL_0018:  newobj     ""int?..ctor(int)""
  IL_001d:  br.s       IL_0020
  IL_001f:  ldloc.0
  IL_0020:  box        ""int?""
  IL_0025:  call       ""void System.Console.WriteLine(object)""
  IL_002a:  ret
}
");

            // When the optimizer is on, the local is entirely elided as the result of the assignment isn't used again
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
            CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;

    public static void Main()
    {
        var c = new C();
        Console.WriteLine(c.f1 ??= GetInt());
    }
    static int GetInt()
    {
        Console.WriteLine(""In GetInt"");
        return 0;
    }
}
", expectedOutput: @"
In GetInt
0
").VerifyIL("C.Main()", @"
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (C V_0,
                int? V_1,
                int? V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""int? C.f1""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool int?.HasValue.get""
  IL_0014:  brtrue.s   IL_002b
  IL_0016:  ldloc.0
  IL_0017:  call       ""int C.GetInt()""
  IL_001c:  newobj     ""int?..ctor(int)""
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  stfld      ""int? C.f1""
  IL_0028:  ldloc.2
  IL_0029:  br.s       IL_002c
  IL_002b:  ldloc.1
  IL_002c:  box        ""int?""
  IL_0031:  call       ""void System.Console.WriteLine(object)""
  IL_0036:  ret
}
");
        }

        [Fact]
        public void IndexerLvalue()
        {
            CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;

    public static void Main()
    {
        int?[] a = new int?[1];
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
").VerifyIL("C.Main()", @"
{
  // Code size      118 (0x76)
  .maxstack  4
  .locals init (int?& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int?""
  IL_0006:  dup
  IL_0007:  call       ""int C.GetInt()""
  IL_000c:  ldelema    ""int?""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  ldobj      ""int?""
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  call       ""bool int?.HasValue.get""
  IL_0020:  brtrue.s   IL_0033
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.1
  IL_0024:  newobj     ""int?..ctor(int)""
  IL_0029:  dup
  IL_002a:  stloc.2
  IL_002b:  stobj      ""int?""
  IL_0030:  ldloc.2
  IL_0031:  br.s       IL_0034
  IL_0033:  ldloc.1
  IL_0034:  box        ""int?""
  IL_0039:  call       ""void System.Console.WriteLine(object)""
  IL_003e:  call       ""int C.GetInt()""
  IL_0043:  ldelema    ""int?""
  IL_0048:  stloc.0
  IL_0049:  ldloc.0
  IL_004a:  ldobj      ""int?""
  IL_004f:  stloc.1
  IL_0050:  ldloca.s   V_1
  IL_0052:  call       ""bool int?.HasValue.get""
  IL_0057:  brtrue.s   IL_006a
  IL_0059:  ldloc.0
  IL_005a:  ldc.i4.2
  IL_005b:  newobj     ""int?..ctor(int)""
  IL_0060:  dup
  IL_0061:  stloc.2
  IL_0062:  stobj      ""int?""
  IL_0067:  ldloc.2
  IL_0068:  br.s       IL_006b
  IL_006a:  ldloc.1
  IL_006b:  box        ""int?""
  IL_0070:  call       ""void System.Console.WriteLine(object)""
  IL_0075:  ret
}
");
        }

        [Fact]
        public void PropertyLvalue()
        {
            CompileAndVerify(@"
using System;
public class C
{
    int? f1 = null;
    int? P1
    {
        get
        {
            Console.WriteLine(""In Get"");
            return f1;
        }
        set
        {
            Console.WriteLine(""In Set"");
            f1 = value;
        }
    }

    public static int GetInt(int i)
    {
        Console.WriteLine(""In GetInt"");
        return i;
    }

    public static void Main()
    {
        var c = new C();
        Console.WriteLine(c.P1 ??= GetInt(1));
        Console.WriteLine(c.P1 ??= GetInt(2));
    }
}
", expectedOutput: @"
In Get
In GetInt
In Set
1
In Get
1").VerifyIL("C.Main()", @"
{
  // Code size      109 (0x6d)
  .maxstack  4
  .locals init (C V_0,
                int? V_1,
                int? V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""int? C.P1.get""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       ""bool int?.HasValue.get""
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  ldloc.0
  IL_0018:  ldloca.s   V_2
  IL_001a:  ldc.i4.1
  IL_001b:  call       ""int C.GetInt(int)""
  IL_0020:  call       ""int?..ctor(int)""
  IL_0025:  ldloc.2
  IL_0026:  callvirt   ""void C.P1.set""
  IL_002b:  ldloc.2
  IL_002c:  br.s       IL_002f
  IL_002e:  ldloc.1
  IL_002f:  box        ""int?""
  IL_0034:  call       ""void System.Console.WriteLine(object)""
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  callvirt   ""int? C.P1.get""
  IL_0040:  stloc.1
  IL_0041:  ldloca.s   V_1
  IL_0043:  call       ""bool int?.HasValue.get""
  IL_0048:  brtrue.s   IL_0061
  IL_004a:  ldloc.0
  IL_004b:  ldloca.s   V_2
  IL_004d:  ldc.i4.2
  IL_004e:  call       ""int C.GetInt(int)""
  IL_0053:  call       ""int?..ctor(int)""
  IL_0058:  ldloc.2
  IL_0059:  callvirt   ""void C.P1.set""
  IL_005e:  ldloc.2
  IL_005f:  br.s       IL_0062
  IL_0061:  ldloc.1
  IL_0062:  box        ""int?""
  IL_0067:  call       ""void System.Console.WriteLine(object)""
  IL_006c:  ret
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
", expectedOutput: @"
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
            CompileAndVerify(@"
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

    public static void Main()
    {
        var c = new C();
        Console.WriteLine(c.P1 ??= 1);
        c.f1 = null;
        Console.WriteLine(c.GetF1() ??= 2);
    }
}
", expectedOutput: @"
In Get P1
1
In GetF1
2").VerifyIL("C.Main()", @"
{
  // Code size      119 (0x77)
  .maxstack  4
  .locals init (int?& V_0,
                int? V_1,
                int? V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref int? C.P1.get""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldobj      ""int?""
  IL_0012:  stloc.1
  IL_0013:  ldloca.s   V_1
  IL_0015:  call       ""bool int?.HasValue.get""
  IL_001a:  brtrue.s   IL_002d
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.1
  IL_001e:  newobj     ""int?..ctor(int)""
  IL_0023:  dup
  IL_0024:  stloc.2
  IL_0025:  stobj      ""int?""
  IL_002a:  ldloc.2
  IL_002b:  br.s       IL_002e
  IL_002d:  ldloc.1
  IL_002e:  box        ""int?""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  dup
  IL_0039:  ldflda     ""int? C.f1""
  IL_003e:  initobj    ""int?""
  IL_0044:  callvirt   ""ref int? C.GetF1()""
  IL_0049:  stloc.0
  IL_004a:  ldloc.0
  IL_004b:  ldobj      ""int?""
  IL_0050:  stloc.1
  IL_0051:  ldloca.s   V_1
  IL_0053:  call       ""bool int?.HasValue.get""
  IL_0058:  brtrue.s   IL_006b
  IL_005a:  ldloc.0
  IL_005b:  ldc.i4.2
  IL_005c:  newobj     ""int?..ctor(int)""
  IL_0061:  dup
  IL_0062:  stloc.2
  IL_0063:  stobj      ""int?""
  IL_0068:  ldloc.2
  IL_0069:  br.s       IL_006c
  IL_006b:  ldloc.1
  IL_006c:  box        ""int?""
  IL_0071:  call       ""void System.Console.WriteLine(object)""
  IL_0076:  ret
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
                // (24,27): error CS8331: Cannot assign to property 'C.P1' because it is a readonly variable
                //         Console.WriteLine(c.P1 ??= 1);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.P1").WithArguments("property", "C.P1").WithLocation(24, 27),
                // (26,27): error CS8331: Cannot assign to method 'C.GetF1()' because it is a readonly variable
                //         Console.WriteLine(c.GetF1() ??= 2);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.GetF1()").WithArguments("method", "C.GetF1()").WithLocation(26, 27)
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
            CompileAndVerify(@"
using System;
public class C
{
    static int? f1 = null;
    public static void Main()
    {
        ref int? i1 = ref f1;
        Console.WriteLine(i1 ??= 1);
        Console.WriteLine(f1);
    }
}", expectedOutput: @"1
1").VerifyIL("C.Main()", expectedIL: @"
{
  // Code size       66 (0x42)
  .maxstack  3
  .locals init (int?& V_0, //i1
                int? V_1,
                int? V_2)
  IL_0000:  ldsflda    ""int? C.f1""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldobj      ""int?""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool int?.HasValue.get""
  IL_0014:  brtrue.s   IL_0027
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  newobj     ""int?..ctor(int)""
  IL_001d:  dup
  IL_001e:  stloc.2
  IL_001f:  stobj      ""int?""
  IL_0024:  ldloc.2
  IL_0025:  br.s       IL_0028
  IL_0027:  ldloc.1
  IL_0028:  box        ""int?""
  IL_002d:  call       ""void System.Console.WriteLine(object)""
  IL_0032:  ldsfld     ""int? C.f1""
  IL_0037:  box        ""int?""
  IL_003c:  call       ""void System.Console.WriteLine(object)""
  IL_0041:  ret
}
");
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
            CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        (int, int)? a = null;
        Console.WriteLine(a ??= (1, 2));
    }
}", expectedOutput: "(1, 2)").VerifyIL("C.Main()", expectedIL:
@"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init ((int, int)? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""(int, int)?""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool (int, int)?.HasValue.get""
  IL_0011:  brtrue.s   IL_0021
  IL_0013:  ldc.i4.1
  IL_0014:  ldc.i4.2
  IL_0015:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_001a:  newobj     ""(int, int)?..ctor((int, int))""
  IL_001f:  br.s       IL_0022
  IL_0021:  ldloc.0
  IL_0022:  box        ""(int, int)?""
  IL_0027:  call       ""void System.Console.WriteLine(object)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void ValidRHSConversions()
        {
            CompileAndVerify(@"
class C
{
    public static void Main()
    {
        // Implicit reference conversion
        C c1 = null;
        c1 ??= new D();

        // Implicit user-defined conversion
        C c2 = null;
        c2 ??= new E();

        // Implicit user-defined conversion to the underlying non-nullable type of lhs
        int? i1 = null;
        i1 ??= new C();
    }

    static public implicit operator C(E e) => null;
    static public implicit operator int(C c) => 0;
}
class D : C {}
class E {}
").VerifyIL("C.Main()", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldnull
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""D..ctor()""
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  brtrue.s   IL_0017
  IL_000c:  newobj     ""E..ctor()""
  IL_0011:  call       ""C C.op_Implicit(E)""
  IL_0016:  pop
  IL_0017:  ldloca.s   V_0
  IL_0019:  initobj    ""int?""
  IL_001f:  ldloc.0
  IL_0020:  stloc.0
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""bool int?.HasValue.get""
  IL_0028:  brtrue.s   IL_0035
  IL_002a:  newobj     ""C..ctor()""
  IL_002f:  call       ""int C.op_Implicit(C)""
  IL_0034:  pop
  IL_0035:  ret
}
");
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
        M2(d ??= 2.0);
        M3(d ??= 3.0);
        object o = null;
        M3(o ??= null);
        (o ??= null) = null;
    }

    public void M2(double d) {}
    public void M3(C c) {}
}
").VerifyDiagnostics(
                // (7,12): error CS1503: Argument 1: cannot convert from 'double?' to 'double'
                //         M2(d ??= 2.0);
                Diagnostic(ErrorCode.ERR_BadArgType, "d ??= 2.0").WithArguments("1", "double?", "double").WithLocation(7, 12),
                // (8,12): error CS1503: Argument 1: cannot convert from 'double?' to 'C'
                //         M3(d ??= 3.0);
                Diagnostic(ErrorCode.ERR_BadArgType, "d ??= 3.0").WithArguments("1", "double?", "C").WithLocation(8, 12),
                // (10,12): error CS1503: Argument 1: cannot convert from 'object' to 'C'
                //         M3(o ??= null);
                Diagnostic(ErrorCode.ERR_BadArgType, "o ??= null").WithArguments("1", "object", "C").WithLocation(10, 12),
                // (11,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (o ??= null) = null;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "o ??= null").WithLocation(11, 10)
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
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (T? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool T?.HasValue.get""
  IL_0009:  brtrue.s   IL_0013
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldarg.1
  IL_000e:  call       ""T?..ctor(T)""
  IL_0013:  ldarg.0
  IL_0014:  box        ""T?""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}
");

            CreateCompilation(@"
class C
{
    void M<T>(T t1, T t2) where T : struct
    {
        t1 ??= t2;
    }
}").VerifyDiagnostics(new DiagnosticDescription[] {
                // (6,9): error CS0019: Operator '??=' cannot be applied to operands of type 'T' and 'T'
                //         t1 ??= t2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "t1 ??= t2").WithArguments("??=", "T", "T").WithLocation(6, 9)
            });
        }

        [Fact]
        public void CoalescingAssignment_DynamicRuntimeCastFailure()
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

            var verifier = CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: "");
        }
    }
}
