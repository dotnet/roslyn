using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.NullCoalescingAssignment)]
    public class NullCoalescingAssignmentTests : CompilingTestBase
    {
        [Fact]
        public void LocalLvalue()
        {

            CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        int? i1 = null;
        Console.WriteLine(i1 ??= 0);
    }
}
", expectedOutput: "0").VerifyIL("C.Main()", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool int?.HasValue.get""
  IL_0011:  brtrue.s   IL_001b
  IL_0013:  ldc.i4.0
  IL_0014:  newobj     ""int?..ctor(int)""
  IL_0019:  br.s       IL_001c
  IL_001b:  ldloc.0
  IL_001c:  box        ""int?""
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void FieldLvalue()
        {

            CompileAndVerify(@"
public class C
{
    int? f1 = null;

    public static void Main()
    {
        var c = new C();
        Test(c.f1 ??= 0);
    }
    public static void Test(int? test) {}
}
").VerifyIL("C.Main()", @"
{
  // Code size       46 (0x2e)
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
  IL_0014:  brtrue.s   IL_0027
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  newobj     ""int?..ctor(int)""
  IL_001d:  dup
  IL_001e:  stloc.2
  IL_001f:  stfld      ""int? C.f1""
  IL_0024:  ldloc.2
  IL_0025:  br.s       IL_0028
  IL_0027:  ldloc.1
  IL_0028:  call       ""void C.Test(int?)""
  IL_002d:  ret
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
        Console.WriteLine(a[0] ??= 1);
    }
}
", expectedOutput: "1").VerifyIL("C.Main()", @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (int?& V_0,
                int?& V_1,
                int? V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int?""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int?""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  ldloc.1
  IL_0010:  call       ""bool int?.HasValue.get""
  IL_0015:  brtrue.s   IL_0028
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4.1
  IL_0019:  newobj     ""int?..ctor(int)""
  IL_001e:  dup
  IL_001f:  stloc.2
  IL_0020:  stobj      ""int?""
  IL_0025:  ldloc.2
  IL_0026:  br.s       IL_002e
  IL_0028:  ldloc.1
  IL_0029:  ldobj      ""int?""
  IL_002e:  box        ""int?""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  ret
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
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (System.EventHandler V_0)
  IL_0000:  ldsfld     ""System.EventHandler C.E""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brtrue.s   IL_002d
  IL_0009:  ldsfld     ""System.EventHandler C.<>c.<>9__3_0""
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0028
  IL_0011:  pop
  IL_0012:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0017:  ldftn      ""void C.<>c.<Main>b__3_0(object, System.EventArgs)""
  IL_001d:  newobj     ""System.EventHandler..ctor(object, System.IntPtr)""
  IL_0022:  dup
  IL_0023:  stsfld     ""System.EventHandler C.<>c.<>9__3_0""
  IL_0028:  stsfld     ""System.EventHandler C.E""
  IL_002d:  ret
}
");
        }

        [Fact]
        public void InvalidLHS()
        {

            CreateCompilation(@"
public class C
{
    int? P1 { get; }

    public static void Main()
    {
        var c = new C();
        c.P1 ??= 1; // Non rvalue
        GetInt() ??= 1; // Non lvalue
    }

    static int? GetInt() => null;
}
").VerifyDiagnostics(new DiagnosticDescription[]{
                // (9,9): error CS0200: Property or indexer 'C.P1' cannot be assigned to -- it is read only
                //         c.P1 ??= 1; // Non rvalue
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "c.P1").WithArguments("C.P1").WithLocation(9, 9),
                // (10,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         GetInt() ??= 1; // Non lvalue
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "GetInt()").WithLocation(10, 9)
            });
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
").VerifyDiagnostics(new DiagnosticDescription[]{
                // (9,15): error CS0154: The property or indexer 'C.P1' cannot be used in this context because it lacks the get accessor
                //         c ??= P1;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P1").WithArguments("C.P1").WithLocation(9, 15)
            });
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
  // Code size      117 (0x75)
  .maxstack  4
  .locals init (int?& V_0,
                int?& V_1,
                int? V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref int? C.P1.get""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  call       ""bool int?.HasValue.get""
  IL_0014:  brtrue.s   IL_0027
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  newobj     ""int?..ctor(int)""
  IL_001d:  dup
  IL_001e:  stloc.2
  IL_001f:  stobj      ""int?""
  IL_0024:  ldloc.2
  IL_0025:  br.s       IL_002d
  IL_0027:  ldloc.1
  IL_0028:  ldobj      ""int?""
  IL_002d:  box        ""int?""
  IL_0032:  call       ""void System.Console.WriteLine(object)""
  IL_0037:  dup
  IL_0038:  ldflda     ""int? C.f1""
  IL_003d:  initobj    ""int?""
  IL_0043:  callvirt   ""ref int? C.GetF1()""
  IL_0048:  stloc.1
  IL_0049:  ldloc.1
  IL_004a:  stloc.0
  IL_004b:  ldloc.0
  IL_004c:  call       ""bool int?.HasValue.get""
  IL_0051:  brtrue.s   IL_0064
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4.2
  IL_0055:  newobj     ""int?..ctor(int)""
  IL_005a:  dup
  IL_005b:  stloc.2
  IL_005c:  stobj      ""int?""
  IL_0061:  ldloc.2
  IL_0062:  br.s       IL_006a
  IL_0064:  ldloc.0
  IL_0065:  ldobj      ""int?""
  IL_006a:  box        ""int?""
  IL_006f:  call       ""void System.Console.WriteLine(object)""
  IL_0074:  ret
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
}").VerifyDiagnostics(new DiagnosticDescription[] {
                // (24,27): error CS8331: Cannot assign to property 'C.P1' because it is a readonly variable
                //         Console.WriteLine(c.P1 ??= 1);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.P1").WithArguments("property", "C.P1").WithLocation(24, 27),
                // (26,27): error CS8331: Cannot assign to method 'C.GetF1()' because it is a readonly variable
                //         Console.WriteLine(c.GetF1() ??= 2);
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.GetF1()").WithArguments("method", "C.GetF1()").WithLocation(26, 27)
});
        }

        [Fact]
        public void NonNullableLhs()
        {
            CreateCompilation(@"
using System;
public class C
{
    public static void Main()
    {
        int i1 = 0;
        i1 ??= 0;
    }
}").VerifyDiagnostics(new DiagnosticDescription[] {
                // (8,9): error CS0019: Operator '??=' cannot be applied to operands of type 'int' and 'int'
                //         i1 ??= 0;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= 0").WithArguments("??=", "int", "int").WithLocation(8, 9),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1)
            });
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
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (C V_0)
  IL_0000:  call       ""C C.P1.get""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brtrue.s   IL_0016
  IL_0009:  newobj     ""C..ctor()""
  IL_000e:  dup
  IL_000f:  call       ""void C.P1.set""
  IL_0014:  br.s       IL_0017
  IL_0016:  ldloc.0
  IL_0017:  call       ""void C.Test(C)""
  IL_001c:  ret
}");
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
  // Code size       65 (0x41)
  .maxstack  3
  .locals init (int?& V_0, //i1
                int?& V_1,
                int? V_2)
  IL_0000:  ldsflda    ""int? C.f1""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  call       ""bool int?.HasValue.get""
  IL_000e:  brtrue.s   IL_0021
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.1
  IL_0012:  newobj     ""int?..ctor(int)""
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  stobj      ""int?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_0027
  IL_0021:  ldloc.1
  IL_0022:  ldobj      ""int?""
  IL_0027:  box        ""int?""
  IL_002c:  call       ""void System.Console.WriteLine(object)""
  IL_0031:  ldsfld     ""int? C.f1""
  IL_0036:  box        ""int?""
  IL_003b:  call       ""void System.Console.WriteLine(object)""
  IL_0040:  ret
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
        public void UseBeforeAssignment()
        {
            CreateCompilation(@"
public class C
{
    public static void Main()
    {
        C c;
        c ??= new C();
    }
}").VerifyDiagnostics(new DiagnosticDescription[]
{
    // (7,9): error CS0165: Use of unassigned local variable 'c'
    //         c ??= new C();
    Diagnostic(ErrorCode.ERR_UseDefViolation, "c").WithArguments("c").WithLocation(7, 9)
});
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
}").VerifyDiagnostics(new DiagnosticDescription[]
{
    // (7,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.P'
    //         P ??= new C();
    Diagnostic(ErrorCode.ERR_ObjectRequired, "P").WithArguments("C.P").WithLocation(7, 9)
});
        }

        [Fact]
        public void ValidRhsConversions()
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
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (C V_0,
                int? V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_000b
  IL_0005:  newobj     ""D..ctor()""
  IL_000a:  pop
  IL_000b:  ldnull
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  newobj     ""E..ctor()""
  IL_0015:  call       ""C C.op_Implicit(E)""
  IL_001a:  pop
  IL_001b:  ldloca.s   V_1
  IL_001d:  initobj    ""int?""
  IL_0023:  ldloc.1
  IL_0024:  stloc.1
  IL_0025:  ldloca.s   V_1
  IL_0027:  call       ""bool int?.HasValue.get""
  IL_002c:  brtrue.s   IL_0039
  IL_002e:  newobj     ""C..ctor()""
  IL_0033:  call       ""int C.op_Implicit(C)""
  IL_0038:  pop
  IL_0039:  ret
}
");
        }

        [Fact]
        public void InvalidRhsConversions()
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
").VerifyDiagnostics(new DiagnosticDescription[] {
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
            });
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
").VerifyDiagnostics(new DiagnosticDescription[] {
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
            });
        }

        [Fact]
        public void DynamicLHS()
        {
            CompileAndVerify(@"
using System;
using System.Collections.Generic;
public class C
{
    public static void Main()
    {
        VerifyField();
        VerifyProperty();
        VerifyIndexer();
    }

    public static void VerifyField()
    {
        Console.WriteLine(GetDynamicClass().F1 ??= 0);
    }
    public static void VerifyProperty()
    {
        Console.WriteLine(GetDynamicClass().P1 ??= 1);
    }
    public static void VerifyIndexer()
    {
        Console.WriteLine(GetDynamicClass()[0] ??= 2);
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
2").VerifyIL("C.VerifyField()", expectedIL: @"
{
  // Code size      284 (0x11c)
  .maxstack  11
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__2""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""WriteLine""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__2""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__2""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__1.<>p__2""
  IL_0055:  ldtoken    ""System.Console""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  call       ""dynamic C.GetDynamicClass()""
  IL_0064:  stloc.0
  IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_006a:  brtrue.s   IL_009b
  IL_006c:  ldc.i4.0
  IL_006d:  ldstr      ""F1""
  IL_0072:  ldtoken    ""C""
  IL_0077:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007c:  ldc.i4.1
  IL_007d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0082:  dup
  IL_0083:  ldc.i4.0
  IL_0084:  ldc.i4.0
  IL_0085:  ldnull
  IL_0086:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008b:  stelem.ref
  IL_008c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0091:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0096:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00a0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__1.<>p__0""
  IL_00aa:  ldloc.0
  IL_00ab:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00b0:  stloc.1
  IL_00b1:  ldloc.1
  IL_00b2:  brtrue.s   IL_0115
  IL_00b4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00b9:  brtrue.s   IL_00f8
  IL_00bb:  ldc.i4     0x80
  IL_00c0:  ldstr      ""F1""
  IL_00c5:  ldtoken    ""C""
  IL_00ca:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00cf:  ldc.i4.2
  IL_00d0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d5:  dup
  IL_00d6:  ldc.i4.0
  IL_00d7:  ldc.i4.0
  IL_00d8:  ldnull
  IL_00d9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00de:  stelem.ref
  IL_00df:  dup
  IL_00e0:  ldc.i4.1
  IL_00e1:  ldc.i4.0
  IL_00e2:  ldnull
  IL_00e3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e8:  stelem.ref
  IL_00e9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00ee:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00f3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00f8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_00fd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0102:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__1.<>p__1""
  IL_0107:  ldloc.0
  IL_0108:  ldc.i4.0
  IL_0109:  box        ""int""
  IL_010e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0113:  br.s       IL_0116
  IL_0115:  ldloc.1
  IL_0116:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_011b:  ret
}
").VerifyIL("C.VerifyProperty()", expectedIL: @"
{
  // Code size      284 (0x11c)
  .maxstack  11
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__2.<>p__2""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""WriteLine""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__2.<>p__2""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__2.<>p__2""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__2.<>p__2""
  IL_0055:  ldtoken    ""System.Console""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  call       ""dynamic C.GetDynamicClass()""
  IL_0064:  stloc.0
  IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_006a:  brtrue.s   IL_009b
  IL_006c:  ldc.i4.0
  IL_006d:  ldstr      ""P1""
  IL_0072:  ldtoken    ""C""
  IL_0077:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_007c:  ldc.i4.1
  IL_007d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0082:  dup
  IL_0083:  ldc.i4.0
  IL_0084:  ldc.i4.0
  IL_0085:  ldnull
  IL_0086:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_008b:  stelem.ref
  IL_008c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0091:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0096:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_009b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_00a0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_00a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__2.<>p__0""
  IL_00aa:  ldloc.0
  IL_00ab:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_00b0:  stloc.1
  IL_00b1:  ldloc.1
  IL_00b2:  brtrue.s   IL_0115
  IL_00b4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00b9:  brtrue.s   IL_00f8
  IL_00bb:  ldc.i4     0x80
  IL_00c0:  ldstr      ""P1""
  IL_00c5:  ldtoken    ""C""
  IL_00ca:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00cf:  ldc.i4.2
  IL_00d0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d5:  dup
  IL_00d6:  ldc.i4.0
  IL_00d7:  ldc.i4.0
  IL_00d8:  ldnull
  IL_00d9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00de:  stelem.ref
  IL_00df:  dup
  IL_00e0:  ldc.i4.1
  IL_00e1:  ldc.i4.0
  IL_00e2:  ldnull
  IL_00e3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e8:  stelem.ref
  IL_00e9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00ee:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00f3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00f8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_00fd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0102:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__2.<>p__1""
  IL_0107:  ldloc.0
  IL_0108:  ldc.i4.1
  IL_0109:  box        ""int""
  IL_010e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0113:  br.s       IL_0116
  IL_0115:  ldloc.1
  IL_0116:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_011b:  ret
}").VerifyIL("C.VerifyIndexer()", expectedIL: @"
{
  // Code size      296 (0x128)
  .maxstack  10
  .locals init (object V_0,
                object V_1)
  IL_0000:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__3.<>p__2""
  IL_0005:  brtrue.s   IL_0046
  IL_0007:  ldc.i4     0x100
  IL_000c:  ldstr      ""WriteLine""
  IL_0011:  ldnull
  IL_0012:  ldtoken    ""C""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  ldc.i4.2
  IL_001d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldc.i4.s   33
  IL_0026:  ldnull
  IL_0027:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002c:  stelem.ref
  IL_002d:  dup
  IL_002e:  ldc.i4.1
  IL_002f:  ldc.i4.0
  IL_0030:  ldnull
  IL_0031:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0036:  stelem.ref
  IL_0037:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003c:  call       ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0041:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__3.<>p__2""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__3.<>p__2""
  IL_004b:  ldfld      ""System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic> System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>>.Target""
  IL_0050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>> C.<>o__3.<>p__2""
  IL_0055:  ldtoken    ""System.Console""
  IL_005a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005f:  call       ""dynamic C.GetDynamicClass()""
  IL_0064:  stloc.0
  IL_0065:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_006a:  brtrue.s   IL_00a0
  IL_006c:  ldc.i4.0
  IL_006d:  ldtoken    ""C""
  IL_0072:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0077:  ldc.i4.2
  IL_0078:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_007d:  dup
  IL_007e:  ldc.i4.0
  IL_007f:  ldc.i4.0
  IL_0080:  ldnull
  IL_0081:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0086:  stelem.ref
  IL_0087:  dup
  IL_0088:  ldc.i4.1
  IL_0089:  ldc.i4.3
  IL_008a:  ldnull
  IL_008b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0090:  stelem.ref
  IL_0091:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0096:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_009b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_00a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_00a5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>>.Target""
  IL_00aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>> C.<>o__3.<>p__0""
  IL_00af:  ldloc.0
  IL_00b0:  ldc.i4.0
  IL_00b1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, int)""
  IL_00b6:  stloc.1
  IL_00b7:  ldloc.1
  IL_00b8:  brtrue.s   IL_0121
  IL_00ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_00bf:  brtrue.s   IL_0103
  IL_00c1:  ldc.i4     0x80
  IL_00c6:  ldtoken    ""C""
  IL_00cb:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00d0:  ldc.i4.3
  IL_00d1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_00d6:  dup
  IL_00d7:  ldc.i4.0
  IL_00d8:  ldc.i4.0
  IL_00d9:  ldnull
  IL_00da:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00df:  stelem.ref
  IL_00e0:  dup
  IL_00e1:  ldc.i4.1
  IL_00e2:  ldc.i4.3
  IL_00e3:  ldnull
  IL_00e4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00e9:  stelem.ref
  IL_00ea:  dup
  IL_00eb:  ldc.i4.2
  IL_00ec:  ldc.i4.0
  IL_00ed:  ldnull
  IL_00ee:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_00f3:  stelem.ref
  IL_00f4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetIndex(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_00f9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_00fe:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_0103:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_0108:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>>.Target""
  IL_010d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>> C.<>o__3.<>p__1""
  IL_0112:  ldloc.0
  IL_0113:  ldc.i4.0
  IL_0114:  ldc.i4.2
  IL_0115:  box        ""int""
  IL_011a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, int, dynamic)""
  IL_011f:  br.s       IL_0122
  IL_0121:  ldloc.1
  IL_0122:  callvirt   ""void System.Action<System.Runtime.CompilerServices.CallSite, System.Type, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, dynamic)""
  IL_0127:  ret
}");
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
    }
}
