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
    public class CodeGenStructsAndEnum : EmitMetadataTestBase
    {
        #region "Struct"

        [Fact]
        public void ValInstanceField()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public static Boo Inst;
    }

    public static void Main()
    {
        Boo val = Boo.Inst;

        int i = val.I1;
        System.Console.Write(i);
        val.I1 = 42;
        System.Console.Write(val.I1);
        val.I1 = 7;
        System.Console.Write(val.I1);
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "0427");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (D.Boo V_0) //val
  IL_0000:  ldsfld     ""D.Boo D.Boo.Inst""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""int D.Boo.I1""
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4.s   42
  IL_0015:  stfld      ""int D.Boo.I1""
  IL_001a:  ldloc.0
  IL_001b:  ldfld      ""int D.Boo.I1""
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldc.i4.7
  IL_0028:  stfld      ""int D.Boo.I1""
  IL_002d:  ldloc.0
  IL_002e:  ldfld      ""int D.Boo.I1""
  IL_0033:  call       ""void System.Console.Write(int)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void ValStaticField()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public static Boo Inst;
    }

    public static void Main()
    {
        Boo val = Boo.Inst;

        System.Console.Write(Boo.Inst.I1);

        val.I1 = 42;
        Boo.Inst = val;

        System.Console.Write(Boo.Inst.I1);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "042");

            compilation.VerifyIL("D.Main",
@"{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (D.Boo V_0) //val
  IL_0000:  ldsfld     ""D.Boo D.Boo.Inst""
  IL_0005:  stloc.0   
  IL_0006:  ldsflda    ""D.Boo D.Boo.Inst""
  IL_000b:  ldfld      ""int D.Boo.I1""
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldc.i4.s   42
  IL_0019:  stfld      ""int D.Boo.I1""
  IL_001e:  ldloc.0   
  IL_001f:  stsfld     ""D.Boo D.Boo.Inst""
  IL_0024:  ldsflda    ""D.Boo D.Boo.Inst""
  IL_0029:  ldfld      ""int D.Boo.I1""
  IL_002e:  call       ""void System.Console.Write(int)""
  IL_0033:  ret       
}
");
        }

        [Fact]
        public void StructCtor()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        System.Decimal val = 0m;
        System.Console.Write(val);

        val = new System.Decimal(7);
        System.Console.Write(val);

        val = new System.Decimal();
        System.Console.Write(val);

        val = ((decimal)int.MaxValue + 1) * 4; // use the ctor that takes a long
        System.Console.Write(val);

    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0708589934592");

            // expect just two locals (temp is reused)
            compilation.VerifyIL("D.Main",
@"
{
  // Code size       51 (0x33)
  .maxstack  1
  IL_0000:  ldsfld     ""decimal decimal.Zero""
  IL_0005:  call       ""void System.Console.Write(decimal)""
  IL_000a:  ldc.i4.7
  IL_000b:  newobj     ""decimal..ctor(int)""
  IL_0010:  call       ""void System.Console.Write(decimal)""
  IL_0015:  ldsfld     ""decimal decimal.Zero""
  IL_001a:  call       ""void System.Console.Write(decimal)""
  IL_001f:  ldc.i8     0x200000000
  IL_0028:  newobj     ""decimal..ctor(long)""
  IL_002d:  call       ""void System.Console.Write(decimal)""
  IL_0032:  ret
}
");
        }

        [Fact]
        public void AddressUnbox()
        {
            string source = @"
using System;

class Program
{
    public struct S1
    {
        public int x;

        public void Goo()
        {
            x = 123;
        }
    }

    public static S1 goo()
    {
        return new S1();
    }

    static void Main()
    {
        goo().ToString();
        Console.Write(goo().x);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"0");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (Program.S1 V_0)
  IL_0000:  call       ""Program.S1 Program.goo()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  constrained. ""Program.S1""
  IL_000e:  callvirt   ""string object.ToString()""
  IL_0013:  pop
  IL_0014:  call       ""Program.S1 Program.goo()""
  IL_0019:  ldfld      ""int Program.S1.x""
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ret
}
");
        }

        [Fact]
        public void EqualsHashcode()
        {
            string source = @"
using System;

struct S1
{
    public int field;

    public override bool Equals(object obj)
    {
        return obj is S1 && field == ((S1)obj).field;
    }

    public override int GetHashCode()
    {
        return field;
    }

    public static bool operator ==(S1 value1, S1 value2)
    {
        return value1.field == value2.field;
    }

    public static bool operator !=(S1 value1, S1 value2)
    {
        return value1.field != value2.field;
    }
}

class Program
{
    static void Main(string[] args)
    {
    }
}
";
            // ILVerify: Unexpected type on the stack. { Offset = 20, Found = readonly address of '[...]S1', Expected = address of '[...]S1' }
            var compilation = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"");

            compilation.VerifyIL("S1.Equals(object)",
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""S1""
  IL_0006:  brfalse.s  IL_001c
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""int S1.field""
  IL_000e:  ldarg.1
  IL_000f:  unbox      ""S1""
  IL_0014:  ldfld      ""int S1.field""
  IL_0019:  ceq
  IL_001b:  ret
  IL_001c:  ldc.i4.0
  IL_001d:  ret
}
").VerifyIL("S1.GetHashCode()",
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S1.field""
  IL_0006:  ret
}
").VerifyIL("bool S1.op_Equality(S1, S1)",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S1.field""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int S1.field""
  IL_000c:  ceq
  IL_000e:  ret
}
").VerifyIL("bool S1.op_Inequality(S1, S1)",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S1.field""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int S1.field""
  IL_000c:  ceq
  IL_000e:  ldc.i4.0
  IL_000f:  ceq
  IL_0011:  ret
}
");
        }

        [Fact]
        public void EmitObjectGetTypeCallOnStruct()
        {
            string source = @"
using System;

class Program
{
    public struct S1
    {
    }

    static void Main()
    {
        Console.Write((new S1()).GetType());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"Program+S1");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (Program.S1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.S1""
  IL_0008:  ldloc.0
  IL_0009:  box        ""Program.S1""
  IL_000e:  call       ""System.Type object.GetType()""
  IL_0013:  call       ""void System.Console.Write(object)""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void EmitInterfaceMethodOnStruct()
        {
            string source = @"
using System;

class Program
{
    interface I
    {
        void M();
    }

    struct S : I
    {
        public void M()
        {
            Console.WriteLine(""S::M"");
        }
    }

    static void Main(string[] args)
    {
        S s = new S();
        s.M();
        ((I)s).M();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"S::M
S::M");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (Program.S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""void Program.S.M()""
  IL_000f:  ldloc.0
  IL_0010:  box        ""Program.S""
  IL_0015:  callvirt   ""void Program.I.M()""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void ValueTypeWithGeneric()
        {
            string source = @"
namespace NS
{
    using System;
    using N2;

    namespace N2
    {
        public interface IGoo<T>
        {
            void M(T t);
        }

        struct S<T, V> : IGoo<T>
        {
            public S(V v)
            {
                field = v;
            }

            public V field;
            public void M(T t) {  Console.WriteLine(t); }
        }
    }

    public class Test
    {
        static S<N2, char>[] ary;

        class N1 { }
        class N2 : N1 { }
        
        static void Main()
        {
            IGoo<string> goo = new S<string, byte>(255);
            goo.M(""Abc"");
            Console.WriteLine(((S<string, byte>)goo).field);

            ary = new S<N2, char>[3];
            ary[0] = ary[1] = ary[2] = new S<N2, char>('q');
            Console.WriteLine(ary[1].field);
        }
    }
}
";
            // ILVerify: Unexpected type on the stack. { Offset = 31, Found = readonly address of '[...]NS.N2.S`2<string,uint8>', Expected = address of '[...]NS.N2.S`2<string,uint8>' }
            var compilation = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: @"
Abc
255
q");

            compilation.VerifyIL("NS.Test.Main",
@"{
  // Code size      120 (0x78)
  .maxstack  8
  .locals init (NS.N2.S<NS.Test.N2, char> V_0)
  IL_0000:  ldc.i4     0xff
  IL_0005:  newobj     ""NS.N2.S<string, byte>..ctor(byte)""
  IL_000a:  box        ""NS.N2.S<string, byte>""
  IL_000f:  dup
  IL_0010:  ldstr      ""Abc""
  IL_0015:  callvirt   ""void NS.N2.IGoo<string>.M(string)""
  IL_001a:  unbox      ""NS.N2.S<string, byte>""
  IL_001f:  ldfld      ""byte NS.N2.S<string, byte>.field""
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ldc.i4.3
  IL_002a:  newarr     ""NS.N2.S<NS.Test.N2, char>""
  IL_002f:  stsfld     ""NS.N2.S<NS.Test.N2, char>[] NS.Test.ary""
  IL_0034:  ldsfld     ""NS.N2.S<NS.Test.N2, char>[] NS.Test.ary""
  IL_0039:  ldc.i4.0
  IL_003a:  ldsfld     ""NS.N2.S<NS.Test.N2, char>[] NS.Test.ary""
  IL_003f:  ldc.i4.1
  IL_0040:  ldsfld     ""NS.N2.S<NS.Test.N2, char>[] NS.Test.ary""
  IL_0045:  ldc.i4.2
  IL_0046:  ldc.i4.s   113
  IL_0048:  newobj     ""NS.N2.S<NS.Test.N2, char>..ctor(char)""
  IL_004d:  dup
  IL_004e:  stloc.0
  IL_004f:  stelem     ""NS.N2.S<NS.Test.N2, char>""
  IL_0054:  ldloc.0
  IL_0055:  dup
  IL_0056:  stloc.0
  IL_0057:  stelem     ""NS.N2.S<NS.Test.N2, char>""
  IL_005c:  ldloc.0
  IL_005d:  stelem     ""NS.N2.S<NS.Test.N2, char>""
  IL_0062:  ldsfld     ""NS.N2.S<NS.Test.N2, char>[] NS.Test.ary""
  IL_0067:  ldc.i4.1
  IL_0068:  ldelema    ""NS.N2.S<NS.Test.N2, char>""
  IL_006d:  ldfld      ""char NS.N2.S<NS.Test.N2, char>.field""
  IL_0072:  call       ""void System.Console.WriteLine(char)""
  IL_0077:  ret
}
");
        }

        [WorkItem(540954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540954")]
        [Fact]
        public void StructInit()
        {
            var text =
@"
struct Struct
{
    public static void Main()
    {
        Struct s = new Struct();
    }
}
";
            string expectedIL = @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Struct V_0) //s
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""Struct""
  IL_0009:  ret
}
";
            CompileAndVerify(text, options: TestOptions.DebugExe).VerifyIL("Struct.Main()", expectedIL);
        }

        [WorkItem(541845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541845")]
        [Fact]
        public void ConstructEnum()
        {
            var text =
@"
using System;
 
class A
{
    enum E1
    {
        AA
    }

    static void Main()
    {
        var v = new DayOfWeek();
        Console.Write(v.ToString());
        var e = new E1();
        Console.WriteLine(e.ToString());
    }
}
";
            string expectedIL = @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (System.DayOfWeek V_0, //v
  A.E1 V_1) //e
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  constrained. ""System.DayOfWeek""
  IL_000a:  callvirt   ""string object.ToString()""
  IL_000f:  call       ""void System.Console.Write(string)""
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_1
  IL_0018:  constrained. ""A.E1""
  IL_001e:  callvirt   ""string object.ToString()""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ret
}";
            CompileAndVerify(text, expectedOutput: "SundayAA").VerifyIL("A.Main()", expectedIL);
        }

        [WorkItem(541599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541599")]
        [Fact]
        public void TestStructWithStaticField01()
        {
            var source = @"
using System;

public struct S
{
    public static int _verify = 123;
    static void Main()
    {
        Console.Write(_verify);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"123");
        }

        [Fact, WorkItem(543088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543088")]
        public void UseStructLocal()
        {
            var text = @"
using System;

struct GetProperty
{
    int i;
    GetProperty(int x)
    {
        i = x;
    }

    static void Main()
    {
        GetProperty t = new GetProperty(123);
        Console.Write(t.i);
    }
}
";

            CompileAndVerify(text, expectedOutput: "123");
        }

        [Fact]
        public void InplaceInit001()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        v1 = default(Boo);
        DummyUse(v1);

        Boo v2 = default(Boo);
        DummyUse(v2);

        rArg = default(Boo);
        DummyUse(rArg);

        vArg = default(Boo);
        DummyUse(rArg);
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size       73 (0x49)
  .maxstack  1
  .locals init (D.Boo V_0)
  IL_0000:  ldsflda    ""D.Boo D.v1""
  IL_0005:  initobj    ""D.Boo""
  IL_000b:  ldsfld     ""D.Boo D.v1""
  IL_0010:  call       ""void D.DummyUse(D.Boo)""
  IL_0015:  ldloca.s   V_0
  IL_0017:  initobj    ""D.Boo""
  IL_001d:  ldloc.0
  IL_001e:  call       ""void D.DummyUse(D.Boo)""
  IL_0023:  ldarg.0
  IL_0024:  initobj    ""D.Boo""
  IL_002a:  ldarg.0
  IL_002b:  ldobj      ""D.Boo""
  IL_0030:  call       ""void D.DummyUse(D.Boo)""
  IL_0035:  ldarga.s   V_1
  IL_0037:  initobj    ""D.Boo""
  IL_003d:  ldarg.0
  IL_003e:  ldobj      ""D.Boo""
  IL_0043:  call       ""void D.DummyUse(D.Boo)""
  IL_0048:  ret
}
");
        }

        [Fact]
        public void InplaceInit002()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        try
        {
            v1 = default(Boo);
            DummyUse(v1);

            Boo v2 = default(Boo);
            DummyUse(v2);

            rArg = default(Boo);
            DummyUse(rArg);

            vArg = default(Boo);
            DummyUse(rArg);
        }
        catch (System.Exception) 
        {
            rArg = default(Boo);
            DummyUse(rArg);
        }
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size       96 (0x60)
  .maxstack  1
  .locals init (D.Boo V_0)
  .try
{
  IL_0000:  ldsflda    ""D.Boo D.v1""
  IL_0005:  initobj    ""D.Boo""
  IL_000b:  ldsfld     ""D.Boo D.v1""
  IL_0010:  call       ""void D.DummyUse(D.Boo)""
  IL_0015:  ldloca.s   V_0
  IL_0017:  initobj    ""D.Boo""
  IL_001d:  ldloc.0
  IL_001e:  call       ""void D.DummyUse(D.Boo)""
  IL_0023:  ldarg.0
  IL_0024:  initobj    ""D.Boo""
  IL_002a:  ldarg.0
  IL_002b:  ldobj      ""D.Boo""
  IL_0030:  call       ""void D.DummyUse(D.Boo)""
  IL_0035:  ldarga.s   V_1
  IL_0037:  initobj    ""D.Boo""
  IL_003d:  ldarg.0
  IL_003e:  ldobj      ""D.Boo""
  IL_0043:  call       ""void D.DummyUse(D.Boo)""
  IL_0048:  leave.s    IL_005f
}
  catch System.Exception
{
  IL_004a:  pop
  IL_004b:  ldarg.0
  IL_004c:  initobj    ""D.Boo""
  IL_0052:  ldarg.0
  IL_0053:  ldobj      ""D.Boo""
  IL_0058:  call       ""void D.DummyUse(D.Boo)""
  IL_005d:  leave.s    IL_005f
}
  IL_005f:  ret
}
");
        }

        [Fact]
        public void InplaceCtor001()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(int i1)
        {
            this.I1 = i1;
        }
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        //try
        //{
            v1 = new Boo(42);
            DummyUse(v1);

            Boo v2 = new Boo(42);
            DummyUse(v2);

            rArg = new Boo(42);
            DummyUse(rArg);

            vArg = new Boo(42);
            DummyUse(rArg);
        //}
        //catch (System.Exception) 
        //
        //    rArg = new Boo(42);
        //    DummyUse(rArg);
        //}
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size       79 (0x4f)
  .maxstack  2
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     ""D.Boo..ctor(int)""
  IL_0007:  stsfld     ""D.Boo D.v1""
  IL_000c:  ldsfld     ""D.Boo D.v1""
  IL_0011:  call       ""void D.DummyUse(D.Boo)""
  IL_0016:  ldc.i4.s   42
  IL_0018:  newobj     ""D.Boo..ctor(int)""
  IL_001d:  call       ""void D.DummyUse(D.Boo)""
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.s   42
  IL_0025:  newobj     ""D.Boo..ctor(int)""
  IL_002a:  stobj      ""D.Boo""
  IL_002f:  ldarg.0
  IL_0030:  ldobj      ""D.Boo""
  IL_0035:  call       ""void D.DummyUse(D.Boo)""
  IL_003a:  ldarga.s   V_1
  IL_003c:  ldc.i4.s   42
  IL_003e:  call       ""D.Boo..ctor(int)""
  IL_0043:  ldarg.0
  IL_0044:  ldobj      ""D.Boo""
  IL_0049:  call       ""void D.DummyUse(D.Boo)""
  IL_004e:  ret
}
");
        }

        [Fact]
        public void InplaceCtor002()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(int i1)
        {
            this.I1 = i1;
        }
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        Boo v3;
        try{
            Boo v3a;
            try
            {
                v1 = new Boo(42);
                DummyUse(v1);

                Boo v2 = new Boo(43);
                DummyUse(v2);

                v3 = new Boo(44);
                v3.ToString();

                // should be a call, since v4 is defined in the same try scope
                Boo v4 = new Boo(45);
                v4.ToString();

                rArg = new Boo(46);
                DummyUse(rArg);

                vArg = new Boo(47);
                DummyUse(rArg);
            }
            catch (System.Exception) 
            {
                v3 = new Boo(44);
                v3.ToString();

                // should be a call, since v3a is defined in the same try scope
                v3a = new Boo(44);
                v3a.ToString();

                // should be a call, since v4 is defined in the same try scope
                Boo v4 = new Boo(45);
                v4.ToString();

                rArg = new Boo(48);
                DummyUse(rArg);
            }
        }
        finally
        {
        }
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size      222 (0xde)
  .maxstack  2
  .locals init (D.Boo V_0, //v3
                D.Boo V_1, //v3a
                D.Boo V_2, //v4
                D.Boo V_3) //v4
  .try
  {
    IL_0000:  ldc.i4.s   42
    IL_0002:  newobj     ""D.Boo..ctor(int)""
    IL_0007:  stsfld     ""D.Boo D.v1""
    IL_000c:  ldsfld     ""D.Boo D.v1""
    IL_0011:  call       ""void D.DummyUse(D.Boo)""
    IL_0016:  ldc.i4.s   43
    IL_0018:  newobj     ""D.Boo..ctor(int)""
    IL_001d:  call       ""void D.DummyUse(D.Boo)""
    IL_0022:  ldc.i4.s   44
    IL_0024:  newobj     ""D.Boo..ctor(int)""
    IL_0029:  stloc.0
    IL_002a:  ldloca.s   V_0
    IL_002c:  constrained. ""D.Boo""
    IL_0032:  callvirt   ""string object.ToString()""
    IL_0037:  pop
    IL_0038:  ldloca.s   V_2
    IL_003a:  ldc.i4.s   45
    IL_003c:  call       ""D.Boo..ctor(int)""
    IL_0041:  ldloca.s   V_2
    IL_0043:  constrained. ""D.Boo""
    IL_0049:  callvirt   ""string object.ToString()""
    IL_004e:  pop
    IL_004f:  ldarg.0
    IL_0050:  ldc.i4.s   46
    IL_0052:  newobj     ""D.Boo..ctor(int)""
    IL_0057:  stobj      ""D.Boo""
    IL_005c:  ldarg.0
    IL_005d:  ldobj      ""D.Boo""
    IL_0062:  call       ""void D.DummyUse(D.Boo)""
    IL_0067:  ldc.i4.s   47
    IL_0069:  newobj     ""D.Boo..ctor(int)""
    IL_006e:  starg.s    V_1
    IL_0070:  ldarg.0
    IL_0071:  ldobj      ""D.Boo""
    IL_0076:  call       ""void D.DummyUse(D.Boo)""
    IL_007b:  leave.s    IL_00dd
  }
  catch System.Exception
  {
    IL_007d:  pop
    IL_007e:  ldloca.s   V_0
    IL_0080:  ldc.i4.s   44
    IL_0082:  call       ""D.Boo..ctor(int)""
    IL_0087:  ldloca.s   V_0
    IL_0089:  constrained. ""D.Boo""
    IL_008f:  callvirt   ""string object.ToString()""
    IL_0094:  pop
    IL_0095:  ldloca.s   V_1
    IL_0097:  ldc.i4.s   44
    IL_0099:  call       ""D.Boo..ctor(int)""
    IL_009e:  ldloca.s   V_1
    IL_00a0:  constrained. ""D.Boo""
    IL_00a6:  callvirt   ""string object.ToString()""
    IL_00ab:  pop
    IL_00ac:  ldloca.s   V_3
    IL_00ae:  ldc.i4.s   45
    IL_00b0:  call       ""D.Boo..ctor(int)""
    IL_00b5:  ldloca.s   V_3
    IL_00b7:  constrained. ""D.Boo""
    IL_00bd:  callvirt   ""string object.ToString()""
    IL_00c2:  pop
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.s   48
    IL_00c6:  newobj     ""D.Boo..ctor(int)""
    IL_00cb:  stobj      ""D.Boo""
    IL_00d0:  ldarg.0
    IL_00d1:  ldobj      ""D.Boo""
    IL_00d6:  call       ""void D.DummyUse(D.Boo)""
    IL_00db:  leave.s    IL_00dd
  }
  IL_00dd:  ret
}
");
        }

        [WorkItem(16364, "https://github.com/dotnet/roslyn/issues/16364")]
        [Fact]
        public void InplaceCtor003()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(ref int i1)
        {            
            this.I1 = 1;
            this.I1 += i1;
        }

        public Boo(ref Boo b1)
        {            
            this.I1 = 1;
            this.I1 += b1.I1;
        }
    }

    public static Boo v1;

    public static void Main()
    {
        Boo a1 = default(Boo);

        a1 = new Boo(ref a1);
        System.Console.Write(a1.I1);

        v1 = new Boo(ref v1);
        System.Console.Write(v1.I1);

        a1 = default(Boo);
        v1 = default(Boo);

        a1 = new Boo(ref a1.I1);
        System.Console.Write(a1.I1);

        v1 = new Boo(ref v1.I1);
        System.Console.Write(v1.I1);

    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "1111");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      136 (0x88)
  .maxstack  1
  .locals init (D.Boo V_0) //a1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""D.Boo""
  IL_0008:  ldloca.s   V_0
  IL_000a:  newobj     ""D.Boo..ctor(ref D.Boo)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldfld      ""int D.Boo.I1""
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  ldsflda    ""D.Boo D.v1""
  IL_0020:  newobj     ""D.Boo..ctor(ref D.Boo)""
  IL_0025:  stsfld     ""D.Boo D.v1""
  IL_002a:  ldsflda    ""D.Boo D.v1""
  IL_002f:  ldfld      ""int D.Boo.I1""
  IL_0034:  call       ""void System.Console.Write(int)""
  IL_0039:  ldloca.s   V_0
  IL_003b:  initobj    ""D.Boo""
  IL_0041:  ldsflda    ""D.Boo D.v1""
  IL_0046:  initobj    ""D.Boo""
  IL_004c:  ldloca.s   V_0
  IL_004e:  ldflda     ""int D.Boo.I1""
  IL_0053:  newobj     ""D.Boo..ctor(ref int)""
  IL_0058:  stloc.0
  IL_0059:  ldloc.0
  IL_005a:  ldfld      ""int D.Boo.I1""
  IL_005f:  call       ""void System.Console.Write(int)""
  IL_0064:  ldsflda    ""D.Boo D.v1""
  IL_0069:  ldflda     ""int D.Boo.I1""
  IL_006e:  newobj     ""D.Boo..ctor(ref int)""
  IL_0073:  stsfld     ""D.Boo D.v1""
  IL_0078:  ldsflda    ""D.Boo D.v1""
  IL_007d:  ldfld      ""int D.Boo.I1""
  IL_0082:  call       ""void System.Console.Write(int)""
  IL_0087:  ret
}
");
        }

        [WorkItem(16364, "https://github.com/dotnet/roslyn/issues/16364")]
        [Fact]
        public void InplaceCtor004()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(ref int i1)
        {            
            this.I1 = 1;
            this.I1 += i1;
        }

        public Boo(ref Boo b1)
        {            
            this.I1 = 1;
            this.I1 += b1.I1;
        }
    }

    public static Boo v1;

    public static void Main()
    {
        Boo a1 = default(Boo);

        ref var r1 = ref a1;
        a1 = new Boo(ref r1);
        System.Console.Write(a1.I1);

        ref var r2 = ref v1;
        v1 = new Boo(ref r2);
        System.Console.Write(v1.I1);

        a1 = default(Boo);
        v1 = default(Boo);

        ref var r3 = ref a1.I1;
        a1 = new Boo(ref r3);
        System.Console.Write(a1.I1);

        ref var r4 = ref v1.I1;
        v1 = new Boo(ref r4);
        System.Console.Write(v1.I1);

    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "1111");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size      146 (0x92)
  .maxstack  1
  .locals init (D.Boo V_0, //a1
                D.Boo& V_1, //r1
                D.Boo& V_2, //r2
                int& V_3, //r3
                int& V_4) //r4
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""D.Boo""
  IL_0008:  ldloca.s   V_0
  IL_000a:  stloc.1
  IL_000b:  ldloc.1
  IL_000c:  newobj     ""D.Boo..ctor(ref D.Boo)""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  ldfld      ""int D.Boo.I1""
  IL_0018:  call       ""void System.Console.Write(int)""
  IL_001d:  ldsflda    ""D.Boo D.v1""
  IL_0022:  stloc.2
  IL_0023:  ldloc.2
  IL_0024:  newobj     ""D.Boo..ctor(ref D.Boo)""
  IL_0029:  stsfld     ""D.Boo D.v1""
  IL_002e:  ldsflda    ""D.Boo D.v1""
  IL_0033:  ldfld      ""int D.Boo.I1""
  IL_0038:  call       ""void System.Console.Write(int)""
  IL_003d:  ldloca.s   V_0
  IL_003f:  initobj    ""D.Boo""
  IL_0045:  ldsflda    ""D.Boo D.v1""
  IL_004a:  initobj    ""D.Boo""
  IL_0050:  ldloca.s   V_0
  IL_0052:  ldflda     ""int D.Boo.I1""
  IL_0057:  stloc.3
  IL_0058:  ldloc.3
  IL_0059:  newobj     ""D.Boo..ctor(ref int)""
  IL_005e:  stloc.0
  IL_005f:  ldloc.0
  IL_0060:  ldfld      ""int D.Boo.I1""
  IL_0065:  call       ""void System.Console.Write(int)""
  IL_006a:  ldsflda    ""D.Boo D.v1""
  IL_006f:  ldflda     ""int D.Boo.I1""
  IL_0074:  stloc.s    V_4
  IL_0076:  ldloc.s    V_4
  IL_0078:  newobj     ""D.Boo..ctor(ref int)""
  IL_007d:  stsfld     ""D.Boo D.v1""
  IL_0082:  ldsflda    ""D.Boo D.v1""
  IL_0087:  ldfld      ""int D.Boo.I1""
  IL_008c:  call       ""void System.Console.Write(int)""
  IL_0091:  ret
}
");
        }

        [WorkItem(16364, "https://github.com/dotnet/roslyn/issues/16364")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void InplaceCtor005()
        {
            string source = @"
using System;

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(int x, __arglist)
        {
            this.I1 = 1;
            this.I1 += __refvalue(new ArgIterator(__arglist).GetNextArg(), Boo).I1;
        }
    }

    public static Boo v1;

    public static void Main()
    {
        Boo a1 = default(Boo);

        a1 = new Boo(1, __arglist(ref a1));
        System.Console.Write(a1.I1);

        v1 = new Boo(1, __arglist(ref v1));
        System.Console.Write(v1.I1);
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "11");

            compilation.VerifyIL("D.Main",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (D.Boo V_0) //a1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""D.Boo""
  IL_0008:  ldc.i4.1
  IL_0009:  ldloca.s   V_0
  IL_000b:  newobj     ""D.Boo..ctor(int, __arglist) with __arglist( ref D.Boo)""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldfld      ""int D.Boo.I1""
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ldc.i4.1
  IL_001d:  ldsflda    ""D.Boo D.v1""
  IL_0022:  newobj     ""D.Boo..ctor(int, __arglist) with __arglist( ref D.Boo)""
  IL_0027:  stsfld     ""D.Boo D.v1""
  IL_002c:  ldsflda    ""D.Boo D.v1""
  IL_0031:  ldfld      ""int D.Boo.I1""
  IL_0036:  call       ""void System.Console.Write(int)""
  IL_003b:  ret
}
");
        }

        [Fact]
        public void InitUsed001()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(int i1)
        {
            this.I1 = i1;
        }
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void DummyUse1(ref Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        DummyUse(v1 = default(Boo));
        DummyUse(v1);

        Boo v2;
        DummyUse(v2 = default(Boo));
        DummyUse(v2);

        // TODO: no need for a temp
        Boo v2a;
        DummyUse(v2a = default(Boo));
        DummyUse1(ref v2a);

        DummyUse(rArg = default(Boo));
        DummyUse(rArg);

        // TODO: no need for a temp
        DummyUse(vArg = default(Boo));
        DummyUse(vArg);
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size      126 (0x7e)
  .maxstack  3
  .locals init (D.Boo V_0, //v2a
  D.Boo V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    ""D.Boo""
  IL_0008:  ldloc.1
  IL_0009:  dup
  IL_000a:  stsfld     ""D.Boo D.v1""
  IL_000f:  call       ""void D.DummyUse(D.Boo)""
  IL_0014:  ldsfld     ""D.Boo D.v1""
  IL_0019:  call       ""void D.DummyUse(D.Boo)""
  IL_001e:  ldloca.s   V_1
  IL_0020:  initobj    ""D.Boo""
  IL_0026:  ldloc.1
  IL_0027:  dup
  IL_0028:  call       ""void D.DummyUse(D.Boo)""
  IL_002d:  call       ""void D.DummyUse(D.Boo)""
  IL_0032:  ldloca.s   V_0
  IL_0034:  initobj    ""D.Boo""
  IL_003a:  ldloc.0
  IL_003b:  call       ""void D.DummyUse(D.Boo)""
  IL_0040:  ldloca.s   V_0
  IL_0042:  call       ""void D.DummyUse1(ref D.Boo)""
  IL_0047:  ldarg.0
  IL_0048:  ldloca.s   V_1
  IL_004a:  initobj    ""D.Boo""
  IL_0050:  ldloc.1
  IL_0051:  dup
  IL_0052:  stloc.1
  IL_0053:  stobj      ""D.Boo""
  IL_0058:  ldloc.1
  IL_0059:  call       ""void D.DummyUse(D.Boo)""
  IL_005e:  ldarg.0
  IL_005f:  ldobj      ""D.Boo""
  IL_0064:  call       ""void D.DummyUse(D.Boo)""
  IL_0069:  ldarga.s   V_1
  IL_006b:  initobj    ""D.Boo""
  IL_0071:  ldarg.1
  IL_0072:  call       ""void D.DummyUse(D.Boo)""
  IL_0077:  ldarg.1
  IL_0078:  call       ""void D.DummyUse(D.Boo)""
  IL_007d:  ret
}
");
        }

        [Fact]
        public void CtorUsed001()
        {
            string source = @"

public class D
{
    public struct Boo
    {
        public int I1;

        public Boo(int i1)
        {
            this.I1 = i1;
        }
    }

    public static Boo v1;

    public static void DummyUse(Boo arg)
    {
    }

    public static void DummyUse1(ref Boo arg)
    {
    }

    public static void Main()
    {
        Boo a1 = default(Boo);
        Boo a2 = default(Boo);
        TestInit(out a1, a2);
    }

    private static void TestInit(out Boo rArg, Boo vArg)
    {
        DummyUse(v1 = new Boo(42));
        DummyUse(v1);

        Boo v2;
        DummyUse(v2 = new Boo(42));
        DummyUse(v2);

        Boo v2a;
        DummyUse(v2a = new Boo(42));
        DummyUse1(ref v2a);

        DummyUse(rArg = new Boo(42));
        DummyUse(rArg);

        DummyUse(vArg = new Boo(42));
        DummyUse(vArg);
    }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "");

            compilation.VerifyIL("D.TestInit",
@"
{
  // Code size      122 (0x7a)
  .maxstack  3
  .locals init (D.Boo V_0, //v2a
  D.Boo V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     ""D.Boo..ctor(int)""
  IL_0007:  dup
  IL_0008:  stsfld     ""D.Boo D.v1""
  IL_000d:  call       ""void D.DummyUse(D.Boo)""
  IL_0012:  ldsfld     ""D.Boo D.v1""
  IL_0017:  call       ""void D.DummyUse(D.Boo)""
  IL_001c:  ldc.i4.s   42
  IL_001e:  newobj     ""D.Boo..ctor(int)""
  IL_0023:  dup
  IL_0024:  call       ""void D.DummyUse(D.Boo)""
  IL_0029:  call       ""void D.DummyUse(D.Boo)""
  IL_002e:  ldloca.s   V_0
  IL_0030:  ldc.i4.s   42
  IL_0032:  call       ""D.Boo..ctor(int)""
  IL_0037:  ldloc.0
  IL_0038:  call       ""void D.DummyUse(D.Boo)""
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""void D.DummyUse1(ref D.Boo)""
  IL_0044:  ldarg.0
  IL_0045:  ldc.i4.s   42
  IL_0047:  newobj     ""D.Boo..ctor(int)""
  IL_004c:  dup
  IL_004d:  stloc.1
  IL_004e:  stobj      ""D.Boo""
  IL_0053:  ldloc.1
  IL_0054:  call       ""void D.DummyUse(D.Boo)""
  IL_0059:  ldarg.0
  IL_005a:  ldobj      ""D.Boo""
  IL_005f:  call       ""void D.DummyUse(D.Boo)""
  IL_0064:  ldarga.s   V_1
  IL_0066:  ldc.i4.s   42
  IL_0068:  call       ""D.Boo..ctor(int)""
  IL_006d:  ldarg.1
  IL_006e:  call       ""void D.DummyUse(D.Boo)""
  IL_0073:  ldarg.1
  IL_0074:  call       ""void D.DummyUse(D.Boo)""
  IL_0079:  ret
}
");
        }

        [Fact]
        public void InheritedCallOnReadOnly()
        {
            string source = @"
    class Program
    {
        static void Main()
        {
            var obj = new C1();
            System.Console.WriteLine(obj.field.ToString());
        }
    }

    class C1
    {
        public readonly S1 field;
    }

    struct S1
    {
    }
";

            var compilation = CompileAndVerify(source, expectedOutput: "S1", verify: Verification.Skipped);

            compilation.VerifyIL("Program.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  ldfld      ""S1 C1.field""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  constrained. ""S1""
  IL_0013:  callvirt   ""string object.ToString()""
  IL_0018:  call       ""void System.Console.WriteLine(string)""
  IL_001d:  ret
}
");
            compilation = CompileAndVerify(source, expectedOutput: "S1", parseOptions: TestOptions.Regular.WithPEVerifyCompatFeature());

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  newobj     ""C1..ctor()""
  IL_0005:  ldfld      ""S1 C1.field""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  constrained. ""S1""
  IL_0013:  callvirt   ""string object.ToString()""
  IL_0018:  call       ""void System.Console.WriteLine(string)""
  IL_001d:  ret
}
");
        }

        [Fact]
        [WorkItem(27049, "https://github.com/dotnet/roslyn/issues/27049")]
        public void BoxingRefStructForBaseCall()
        {
            CreateCompilation(@"
ref struct S
{
    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => base.ToString();
}").VerifyDiagnostics(
                // (4,48): error CS0029: Cannot implicitly convert type 'S' to 'System.ValueType'
                //     public override bool Equals(object obj) => base.Equals(obj);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "base").WithArguments("S", "System.ValueType").WithLocation(4, 48),
                // (6,42): error CS0029: Cannot implicitly convert type 'S' to 'System.ValueType'
                //     public override int GetHashCode() => base.GetHashCode();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "base").WithArguments("S", "System.ValueType").WithLocation(6, 42),
                // (8,42): error CS0029: Cannot implicitly convert type 'S' to 'System.ValueType'
                //     public override string ToString() => base.ToString();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "base").WithArguments("S", "System.ValueType").WithLocation(8, 42));
        }

        #endregion
        #region "Enum"

        [Fact]
        public void TestEnum()
        {
            string source =
@"enum E { A, B }
class C
{
    static void Main()
    {
        E e = E.A;
        e = e + 1;
    }
}
";
            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("C.Main",
@"{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  pop
  IL_0002:  ret
}
");
        }

        [Fact]
        public void BoxEnum()
        {
            string source =
@"enum E { A, B }
class C
{
    static void Main()
    {
        E e = E.B;
        object o = e;
        e = (E)o;
        System.Console.Write(e);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "B");

            compilation.VerifyIL("C.Main",
@"{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""E""
  IL_0006:  unbox.any  ""E""
  IL_000b:  box        ""E""
  IL_0010:  call       ""void System.Console.Write(object)""
  IL_0015:  ret
}
");
        }

        [Fact]
        public void MBRO_StructField()
        {
            string source =
@"
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            var v = new cls1();
            v.Test();
        }
    }

    struct S1
    {
        public Guid g;
    }

    class cls1 : MarshalByRefObject
    {
        public Guid g1;
        public S1 s;

        public void Test()
        {
            g1 = new Guid();
            TestRef(ref g1);
            g1 = new Guid(g1.ToString());
            System.Console.WriteLine(g1);
            System.Console.WriteLine(s.g.ToString());

            var other = new cls1();
            other.g1 = new Guid();
            System.Console.WriteLine(other.g1);
            TestRef(ref other.g1);
            other.g1 = new Guid(other.g1.ToString());
            System.Console.WriteLine(other.g1);
            System.Console.WriteLine(other.s.g.ToString());

            var gg = other.s.g;
            System.Console.WriteLine(gg);
        }

        public void TestRef(ref Guid arg)
        {
            arg = new Guid(""ca761232ed4211cebacd00aa0057b223"");
        }
    }

";
            var compilation = CompileAndVerify(source, expectedOutput: @"ca761232-ed42-11ce-bacd-00aa0057b223
00000000-0000-0000-0000-000000000000
00000000-0000-0000-0000-000000000000
ca761232-ed42-11ce-bacd-00aa0057b223
00000000-0000-0000-0000-000000000000
00000000-0000-0000-0000-000000000000");

            compilation.VerifyIL("cls1.Test",
@"
{
  // Code size      237 (0xed)
  .maxstack  2
  .locals init (cls1 V_0, //other
  System.Guid V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Guid cls1.g1""
  IL_0006:  initobj    ""System.Guid""
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""System.Guid cls1.g1""
  IL_0013:  call       ""void cls1.TestRef(ref System.Guid)""
  IL_0018:  ldarg.0
  IL_0019:  ldarg.0
  IL_001a:  ldflda     ""System.Guid cls1.g1""
  IL_001f:  constrained. ""System.Guid""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  newobj     ""System.Guid..ctor(string)""
  IL_002f:  stfld      ""System.Guid cls1.g1""
  IL_0034:  ldarg.0
  IL_0035:  ldfld      ""System.Guid cls1.g1""
  IL_003a:  box        ""System.Guid""
  IL_003f:  call       ""void System.Console.WriteLine(object)""
  IL_0044:  ldarg.0
  IL_0045:  ldflda     ""S1 cls1.s""
  IL_004a:  ldflda     ""System.Guid S1.g""
  IL_004f:  constrained. ""System.Guid""
  IL_0055:  callvirt   ""string object.ToString()""
  IL_005a:  call       ""void System.Console.WriteLine(string)""
  IL_005f:  newobj     ""cls1..ctor()""
  IL_0064:  stloc.0
  IL_0065:  ldloc.0
  IL_0066:  ldloca.s   V_1
  IL_0068:  initobj    ""System.Guid""
  IL_006e:  ldloc.1
  IL_006f:  stfld      ""System.Guid cls1.g1""
  IL_0074:  ldloc.0
  IL_0075:  ldfld      ""System.Guid cls1.g1""
  IL_007a:  box        ""System.Guid""
  IL_007f:  call       ""void System.Console.WriteLine(object)""
  IL_0084:  ldarg.0
  IL_0085:  ldloc.0
  IL_0086:  ldflda     ""System.Guid cls1.g1""
  IL_008b:  call       ""void cls1.TestRef(ref System.Guid)""
  IL_0090:  ldloc.0
  IL_0091:  ldloc.0
  IL_0092:  ldflda     ""System.Guid cls1.g1""
  IL_0097:  constrained. ""System.Guid""
  IL_009d:  callvirt   ""string object.ToString()""
  IL_00a2:  newobj     ""System.Guid..ctor(string)""
  IL_00a7:  stfld      ""System.Guid cls1.g1""
  IL_00ac:  ldloc.0
  IL_00ad:  ldfld      ""System.Guid cls1.g1""
  IL_00b2:  box        ""System.Guid""
  IL_00b7:  call       ""void System.Console.WriteLine(object)""
  IL_00bc:  ldloc.0
  IL_00bd:  ldflda     ""S1 cls1.s""
  IL_00c2:  ldflda     ""System.Guid S1.g""
  IL_00c7:  constrained. ""System.Guid""
  IL_00cd:  callvirt   ""string object.ToString()""
  IL_00d2:  call       ""void System.Console.WriteLine(string)""
  IL_00d7:  ldloc.0
  IL_00d8:  ldfld      ""S1 cls1.s""
  IL_00dd:  ldfld      ""System.Guid S1.g""
  IL_00e2:  box        ""System.Guid""
  IL_00e7:  call       ""void System.Console.WriteLine(object)""
  IL_00ec:  ret
}
");
        }

        [Fact]
        public void InitTemp001()
        {
            string source = @"

using System;
 
struct S
{
    int x;
 
    static void Main()
    {
        Console.WriteLine(new S { x = 0 }.Equals(new S { x = 1 }));
    }
}

";

            var compilation = CompileAndVerify(source, expectedOutput: "False");

            compilation.VerifyIL("S.Main",
@"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (S V_0,
  S V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      ""int S.x""
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    ""S""
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.1
  IL_001d:  stfld      ""int S.x""
  IL_0022:  ldloc.1
  IL_0023:  box        ""S""
  IL_0028:  constrained. ""S""
  IL_002e:  callvirt   ""bool object.Equals(object)""
  IL_0033:  call       ""void System.Console.WriteLine(bool)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void InitTemp001a()
        {
            string source = @"

using System;
 
struct S1
{
    public int x;
}

struct S
{
    public S1 x;

    
 
    static void Main()
    {
        Console.WriteLine(new S { x = new S1{x=0} }.x.Equals(new S { x = new S1{x=1} }));
    }
}

";

            var compilation = CompileAndVerify(source, expectedOutput: "False");

            compilation.VerifyIL("S.Main",
@"
{
  // Code size       94 (0x5e)
  .maxstack  4
  .locals init (S V_0,
                S1 V_1,
                S V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldloca.s   V_1
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloca.s   V_1
  IL_0014:  ldc.i4.0
  IL_0015:  stfld      ""int S1.x""
  IL_001a:  ldloc.1
  IL_001b:  stfld      ""S1 S.x""
  IL_0020:  ldloca.s   V_0
  IL_0022:  ldflda     ""S1 S.x""
  IL_0027:  ldloca.s   V_2
  IL_0029:  initobj    ""S""
  IL_002f:  ldloca.s   V_2
  IL_0031:  ldloca.s   V_1
  IL_0033:  initobj    ""S1""
  IL_0039:  ldloca.s   V_1
  IL_003b:  ldc.i4.1
  IL_003c:  stfld      ""int S1.x""
  IL_0041:  ldloc.1
  IL_0042:  stfld      ""S1 S.x""
  IL_0047:  ldloc.2
  IL_0048:  box        ""S""
  IL_004d:  constrained. ""S1""
  IL_0053:  callvirt   ""bool object.Equals(object)""
  IL_0058:  call       ""void System.Console.WriteLine(bool)""
  IL_005d:  ret
}
");
        }

        [Fact]
        public void InitTemp001b()
        {
            string source = @"

using System;
 
class S1
{
    public int x;
}

struct S
{
    public S1 x;

    
 
    static void Main()
    {
        Console.WriteLine(new S { x = new S1{x=0} }.x.Equals(new S { x = new S1{x=1} }));
    }
}

";

            var compilation = CompileAndVerify(source, expectedOutput: "False");

            compilation.VerifyIL("S.Main",
@"
{
  // Code size       77 (0x4d)
  .maxstack  5
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  newobj     ""S1..ctor()""
  IL_000f:  dup
  IL_0010:  ldc.i4.0
  IL_0011:  stfld      ""int S1.x""
  IL_0016:  stfld      ""S1 S.x""
  IL_001b:  ldloc.0
  IL_001c:  ldfld      ""S1 S.x""
  IL_0021:  ldloca.s   V_0
  IL_0023:  initobj    ""S""
  IL_0029:  ldloca.s   V_0
  IL_002b:  newobj     ""S1..ctor()""
  IL_0030:  dup
  IL_0031:  ldc.i4.1
  IL_0032:  stfld      ""int S1.x""
  IL_0037:  stfld      ""S1 S.x""
  IL_003c:  ldloc.0
  IL_003d:  box        ""S""
  IL_0042:  callvirt   ""bool object.Equals(object)""
  IL_0047:  call       ""void System.Console.WriteLine(bool)""
  IL_004c:  ret
}
");
        }

        [Fact]
        public void InitTemp002()
        {
            string source = @"
using System;

struct S
{
    int x;

    static void Main()
    {
        Console.WriteLine(new S { x = 0 }.Equals(new S { x = 1 }).Equals(
                          new S { x = 1 }.Equals(new S { x = 1 })));
    }
}

";

            var compilation = CompileAndVerify(source, expectedOutput: "False");

            compilation.VerifyIL("S.Main",
@"
{
  // Code size      116 (0x74)
  .maxstack  4
  .locals init (S V_0,
                S V_1,
                bool V_2,
                S V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      ""int S.x""
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    ""S""
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.1
  IL_001d:  stfld      ""int S.x""
  IL_0022:  ldloc.1
  IL_0023:  box        ""S""
  IL_0028:  constrained. ""S""
  IL_002e:  callvirt   ""bool object.Equals(object)""
  IL_0033:  stloc.2
  IL_0034:  ldloca.s   V_2
  IL_0036:  ldloca.s   V_1
  IL_0038:  initobj    ""S""
  IL_003e:  ldloca.s   V_1
  IL_0040:  ldc.i4.1
  IL_0041:  stfld      ""int S.x""
  IL_0046:  ldloca.s   V_1
  IL_0048:  ldloca.s   V_3
  IL_004a:  initobj    ""S""
  IL_0050:  ldloca.s   V_3
  IL_0052:  ldc.i4.1
  IL_0053:  stfld      ""int S.x""
  IL_0058:  ldloc.3
  IL_0059:  box        ""S""
  IL_005e:  constrained. ""S""
  IL_0064:  callvirt   ""bool object.Equals(object)""
  IL_0069:  call       ""bool bool.Equals(bool)""
  IL_006e:  call       ""void System.Console.WriteLine(bool)""
  IL_0073:  ret
}
");
        }

        [Fact]
        public void InitTemp003()
        {
            string source = @"
using System;

readonly struct S
{
    readonly int x;

    public S(int x)
    {
        this.x = x;
    }

    static void Main()
    {
        // named argument reordering introduces a sequence with temps
        // and we cannot know whether RefMethod returns a ref to a sequence local
        // so we must assume that it can, and therefore must keep all the sequence the locals in use 
        // for the duration of the most-encompassing expression.
        Console.WriteLine(RefMethod(arg2: I(5), arg1: I(3)).GreaterThan(
                          RefMethod(arg2: I(0), arg1: I(0))));
    }

    public static ref readonly S RefMethod(in S arg1, in S arg2)
    {
        return ref arg2;
    }

    public bool GreaterThan(in S arg)
    {
        return this.x > arg.x;
    }

    public static S I(int arg)
    {
        return new S(arg);
    }
}

";

            var compilation = CompileAndVerify(source, verify: Verification.Fails, expectedOutput: "True");

            compilation.VerifyIL("S.Main",
@"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (S V_0,
                S V_1,
                S V_2,
                S V_3)
  IL_0000:  ldc.i4.5
  IL_0001:  call       ""S S.I(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  call       ""S S.I(int)""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""ref readonly S S.RefMethod(in S, in S)""
  IL_0017:  ldc.i4.0
  IL_0018:  call       ""S S.I(int)""
  IL_001d:  stloc.2
  IL_001e:  ldc.i4.0
  IL_001f:  call       ""S S.I(int)""
  IL_0024:  stloc.3
  IL_0025:  ldloca.s   V_3
  IL_0027:  ldloca.s   V_2
  IL_0029:  call       ""ref readonly S S.RefMethod(in S, in S)""
  IL_002e:  call       ""bool S.GreaterThan(in S)""
  IL_0033:  call       ""void System.Console.WriteLine(bool)""
  IL_0038:  ret
}
");
        }

        [Fact]
        public void InitTemp004()
        {
            string source = @"
using System;

readonly struct S
{
    public readonly int x;

    public S(int x)
    {
        this.x = x;
    }

    static void Main()
    {
        System.Console.Write(TestRO().x);
        System.Console.WriteLine();
        System.Console.Write(Test().x);
    }

    static ref readonly S TestRO()
    {
        try
        {
            // both args are refs
            return ref RefMethodRO(arg2: I(5), arg1: I(3));
        }
        finally
        {
            // first arg is a value!!
            RefMethodRO(arg2: I_Val(5), arg1: I(3));
        }
    }

    public static ref readonly S RefMethodRO(in S arg1, in S arg2)
    {
        System.Console.Write(arg2.x);
        return ref arg2;
    }

    // similar as above, but with regular (not readonly) refs for comparison
    static ref S Test()
    {
        try
        {
            return ref RefMethod(arg2: ref I(5), arg1: ref I(3));
        }
        finally
        {
            var temp = I(5);
            RefMethod(arg2: ref temp, arg1: ref I(3));
        }
    }

    public static ref S RefMethod(ref S arg1, ref S arg2)
    {
        System.Console.Write(arg2.x);
        return ref arg2;
    }

    private static S[] arr = new S[] { new S() };

    public static ref S I(int arg)
    {
        arr[0] = new S(arg);
        return ref arr[0];
    }

    public static S I_Val(int arg)
    {
        arr[0] = new S(arg);
        return arr[0];
    }
}

";

            var compilation = CompileAndVerify(source, verify: Verification.Fails, expectedOutput: @"353
353");

            compilation.VerifyIL("S.TestRO",
@"
{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init (S& V_0,
                S& V_1,
                S V_2)
  .try
  {
    IL_0000:  ldc.i4.5
    IL_0001:  call       ""ref S S.I(int)""
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.3
    IL_0008:  call       ""ref S S.I(int)""
    IL_000d:  ldloc.0
    IL_000e:  call       ""ref readonly S S.RefMethodRO(in S, in S)""
    IL_0013:  stloc.1
    IL_0014:  leave.s    IL_002c
  }
  finally
  {
    IL_0016:  ldc.i4.5
    IL_0017:  call       ""S S.I_Val(int)""
    IL_001c:  stloc.2
    IL_001d:  ldc.i4.3
    IL_001e:  call       ""ref S S.I(int)""
    IL_0023:  ldloca.s   V_2
    IL_0025:  call       ""ref readonly S S.RefMethodRO(in S, in S)""
    IL_002a:  pop
    IL_002b:  endfinally
  }
  IL_002c:  ldloc.1
  IL_002d:  ret
}
");
        }

        [WorkItem(842477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/842477")]
        [Fact]
        public void DecimalConst()
        {
            string source = @"

#pragma warning disable 458, 169, 414
using System;

public class NullableTest
{
	static decimal? NULL = null;

	public static void EqualEqual()
	{
		Test.Eval((decimal?)1m == null, false);
		Test.Eval((decimal?)1m == NULL, false);
		Test.Eval((decimal?)0 == NULL, false);		
	}
}

public class Test
{
    public static void Eval(object obj1, object obj2)
    {
    }
}


";

            var compilation = CompileAndVerify(source);

            compilation.VerifyIL("NullableTest.EqualEqual",
@"
{
  // Code size      112 (0x70)
  .maxstack  2
  .locals init (decimal? V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  ldc.i4.0
  IL_0007:  box        ""bool""
  IL_000c:  call       ""void Test.Eval(object, object)""
  IL_0011:  ldsfld     ""decimal decimal.One""
  IL_0016:  ldsfld     ""decimal? NullableTest.NULL""
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_0023:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""bool decimal?.HasValue.get""
  IL_002f:  and
  IL_0030:  box        ""bool""
  IL_0035:  ldc.i4.0
  IL_0036:  box        ""bool""
  IL_003b:  call       ""void Test.Eval(object, object)""
  IL_0040:  ldsfld     ""decimal decimal.Zero""
  IL_0045:  ldsfld     ""decimal? NullableTest.NULL""
  IL_004a:  stloc.0
  IL_004b:  ldloca.s   V_0
  IL_004d:  call       ""decimal decimal?.GetValueOrDefault()""
  IL_0052:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0057:  ldloca.s   V_0
  IL_0059:  call       ""bool decimal?.HasValue.get""
  IL_005e:  and
  IL_005f:  box        ""bool""
  IL_0064:  ldc.i4.0
  IL_0065:  box        ""bool""
  IL_006a:  call       ""void Test.Eval(object, object)""
  IL_006f:  ret
}
");
        }

        [Fact]
        public void FieldLoad001()
        {
            string source = @"
    using System;

    struct Point
    {
        public int x;
        public int y;
    }

    class Rectangle
    {
        public Point topLeft;
        public Point bottomRight;
    }

    struct C1
    {
        public C1(int i)
        {
            r = new Rectangle();
        }

        public Rectangle r;
    }

    class Program
    {
        static object p = new C1(1);

        static void Main(string[] args)
        {
            System.Console.WriteLine(((C1)p).r.topLeft.x);
        }
    }

";
            // ILVerify: Unexpected type on the stack. { Offset = 10, Found = readonly address of '[...]C1', Expected = address of '[...]C1' }
            var compilation = CompileAndVerify(source, verify: Verification.FailsILVerify, expectedOutput: "0");

            compilation.VerifyIL("Program.Main",
@"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldsfld     ""object Program.p""
  IL_0005:  unbox      ""C1""
  IL_000a:  ldfld      ""Rectangle C1.r""
  IL_000f:  ldflda     ""Point Rectangle.topLeft""
  IL_0014:  ldfld      ""int Point.x""
  IL_0019:  call       ""void System.Console.WriteLine(int)""
  IL_001e:  ret
}
");
        }

        #endregion
    }
}
