// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenOperatorTests : CSharpTestBase
    {
        [Fact]
        public void TestDelegateAndStringOperators()
        {
            var source = @"
using System;
class C 
{ 
    delegate void D();
    public static void M123()
    {
        Console.WriteLine(123);
    }

    public static void M456()
    {
        Console.WriteLine(456);
    }

    public static void Main() 
    { 
        D d123 = M123;
        D d456 = M456;
        D d123456 = d123 + d456;
        d123456();
        Console.WriteLine(d123456 - d123 == d456);
        Console.WriteLine(d123456 - d123 == d123);
        string s123 = 123.ToString();
        object s456 = 456.ToString();
        Console.WriteLine(s123 + s456);
        Console.WriteLine(s123 + s456 + s123);
    }
}
";
            string expectedOutput =
@"123
456
True
False
123456
123456123
";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestCompoundAssignmentOperators()
        {
            var source = @"
using System;
class C 
{ 
    delegate void D();
    public static void M123()
    {
        Console.Write(123);
    }

    public static void M456()
    {
        Console.Write(456);
    }

    static C c;
    static C GetC() { Console.Write(""GetC""); return c; }
    byte f;

    public static void Main() 
    { 
        C.c = new C();
        D d123 = M123;
        D d456 = M456;
        D d = null;
        d += d123;
        d();
        d += d456;
        d();
        d -= d123;
        d();
        Console.WriteLine();
        short b = 100;
        b -= 70;
        Console.Write(b);
        b *= 2;
        Console.Write(b);
        Console.WriteLine();
        string s = string.Empty;
        s += 789.ToString();
        Console.Write(s);
        s += 987.ToString();
        Console.Write(s);
        Console.WriteLine();
        GetC().f += 100;
        Console.Write(C.c.f);
        Console.WriteLine();
        int[] arr = { 10 };
        arr[0] += 1;
        Console.Write(arr[0]);
        Console.WriteLine();
    }
}
";
            string expectedOutput =
@"123123456456
3060
789789987
GetC100
11";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        // TODO: Add VerifyIL for is and as Codegen tests
        [Fact]
        public void TestIsOperator()
        {
            var source = @"
namespace TestIsOperator
{
    public interface MyInter { }
    public class TestType : MyInter { }
    public class AnotherType { }
    public class MyBase { }
    public class MyDerived : MyBase { }
    public class C
    {
        static void Main()
        {

            string myStr = ""foo"";

            object o = myStr;
            bool b = o is string;

            int myInt = 3;
            b = myInt is int;

            TestType tt = null;
            o = tt;
            b = o is TestType;

            tt = new TestType();
            o = tt;
            b = o is AnotherType;

            b = o is MyInter;

            MyInter mi = new TestType();
            o = mi;
            b = o is MyInter;

            MyBase mb = new MyBase();
            o = mb;
            b = o is MyDerived;

            MyDerived md = new MyDerived();
            o = md;
            b = o is MyBase;

            b = null is MyBase;
        }
    }
}
";
            var compilation = CompileAndVerify(source);
        }

        // TODO: Add VerifyIL for is and as Codegen tests
        [Fact]
        public void TestIsOperatorGeneric()
        {
            var source = @"
namespace TestIsOperatorGeneric
{
    public class C
    {
        public static void Main() { }
        public static void M<T, U, W>(T t, U u, W w)
            where T : class
            where U : class
            where W : class
        {
            bool test = t is int;
            test = u is object;
            test = t is U;
            test = t is T;
            test = u is U;
            test = u is T;
            test = w is int;
            test = w is object;
            test = w is U;
            test = u is W;
            test = t is W;
            test = w is W;
        }
    }
}
";
            var compilation = CompileAndVerify(source);
        }

        [Fact]
        public void CS0184WRN_IsAlwaysFalse()
        {
            var text = @"
class MyClass
{
    public static int Main()
    {
        int i = 0;
        bool b = i is string;   // CS0184
        System.Console.WriteLine(b);
        return 0;
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: "False");
            comp.VerifyDiagnostics(
                // (7,18): warning CS0184: The given expression is never of the provided ('string') type
                //         bool b = i is string;   // CS0184
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "i is string").WithArguments("string"));
            comp.VerifyIL("MyClass.Main", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.WriteLine(bool)""
  IL_0006:  ldc.i4.0
  IL_0007:  ret
}");
        }

        [WorkItem(542466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542466")]
        [Fact]
        public void CS0184WRN_IsAlwaysFalse_Enum()
        {
            var text = @"
class IsTest
{
    static void Main()
    {
        var b = 1 is color;
        System.Console.WriteLine(b);
    }
}
enum color
{ }
";

            var comp = CompileAndVerify(text, expectedOutput: "False");
            comp.VerifyDiagnostics(
            // (6,17): warning CS0184: The given expression is never of the provided ('color') type
            //         var b = 1 is color;
            Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is color").WithArguments("color"));
            comp.VerifyIL("IsTest.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.WriteLine(bool)""
  IL_0006:  ret
}");
        }

        [WorkItem(542466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542466")]
        [Fact]
        public void CS0184WRN_IsAlwaysFalse_ExplicitEnumeration()
        {
            var text = @"
class IsTest
{
    static void Main()
    {
        var b = default(color) is int;
        System.Console.WriteLine(b);
    }
}
enum color
{ }
";

            var comp = CompileAndVerify(text, expectedOutput: "False");
            comp.VerifyDiagnostics(
                // (6,17): warning CS0184: The given expression is never of the provided ('int') type
                //         var b = default(color) is int;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "default(color) is int").WithArguments("int"));
            comp.VerifyIL("IsTest.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.WriteLine(bool)""
  IL_0006:  ret
}");
        }

        [WorkItem(542466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542466")]
        [Fact]
        public void CS0184WRN_IsAlwaysFalse_ImplicitNumeric()
        {
            var text = @"
class IsTest
{
    static void Main()
    {
        var b = 1 is double;
        System.Console.WriteLine(b);
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: "False");
            comp.VerifyDiagnostics(
                // (6,17): warning CS0184: The given expression is never of the provided ('double') type
                //         var b = 1 is double;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is double").WithArguments("double"));
            comp.VerifyIL("IsTest.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.WriteLine(bool)""
  IL_0006:  ret
}");
        }

        [Fact, WorkItem(542466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542466")]
        public void CS0184WRN_IsAlwaysFalse_ExplicitNumeric()
        {
            var text = @"
class IsTest
{
    static void Main()
    {
        var b = 1.0 is int;
        System.Console.WriteLine(b);
        b = 1.0 is float;
        System.Console.WriteLine(b);
        b = 1 is byte;
        System.Console.WriteLine(b);
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: @"False
False
False");
            comp.VerifyDiagnostics(
                // (6,17): warning CS0184: The given expression is never of the provided ('int') type
                //         var b = 1.0 is int;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1.0 is int").WithArguments("int"),
                // (7,13): warning CS0184: The given expression is never of the provided ('float') type
                //         b = 1.0 is float;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1.0 is float").WithArguments("float"),
                // (8,13): warning CS0184: The given expression is never of the provided ('byte') type
                //         b = 1 is byte;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is byte").WithArguments("byte"));
            comp.VerifyIL("IsTest.Main", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""void System.Console.WriteLine(bool)""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""void System.Console.WriteLine(bool)""
  IL_000c:  ldc.i4.0
  IL_000d:  call       ""void System.Console.WriteLine(bool)""
  IL_0012:  ret
}");
        }

        [Fact, WorkItem(546371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546371")]
        public void CS0184WRN_IsAlwaysFalse_NumericIsNullable()
        {
            #region src
            var text = @"using System;

public class Test
    {
        const char v0 = default(char);
        public static sbyte v1 = default(sbyte);
        internal static byte v2 = 0;// default(byte);
        protected static short v3 = 0; // default(short);
        private static ushort v4 = default(ushort);

        static void Main()
        {
            const int v5 = 0; //  default(int);
            uint v6 = 0; // default(uint);
            long v7 = 0; // default(long);
            const ulong v8 = default(ulong);
            float v9 = default(float);
            // char
            if (v0 is ushort?)
                Console.Write(0);
            else
                Console.Write(1);
            
            // sbyte & byte
            if (v1 is short? || v2 is short?)
                Console.Write(0);
            else
                Console.Write(2);

            // short & ushort
            if (v3 is int? || v4 is int?)
                Console.Write(0);
            else
                Console.Write(3);

            // int & uint
            if (v5 is long? || v6 is long?)
                Console.Write(0);
            else
                Console.Write(4);

            // long & ulong
            if (v7 is float? || v8 is float?)
                Console.Write(0);
            else
                Console.Write(5);

            // float
            if (v9 is int?)
                Console.Write(0);
            else
                Console.Write(6);
        }
    }
";
            #endregion

            var comp = CompileAndVerify(text, expectedOutput: @"123456");
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v0 is ushort?").WithArguments("ushort?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v1 is short?").WithArguments("short?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v2 is short?").WithArguments("short?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v3 is int?").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v4 is int?").WithArguments("int?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v5 is long?").WithArguments("long?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v6 is long?").WithArguments("long?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v7 is float?").WithArguments("float?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v8 is float?").WithArguments("float?"),
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "v9 is int?").WithArguments("int?")
                );
            comp.VerifyIL("Test.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ldsfld     ""sbyte Test.v1""
  IL_000b:  pop
  IL_000c:  ldsfld     ""byte Test.v2""
  IL_0011:  pop
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""void System.Console.Write(int)""
  IL_0018:  ldsfld     ""short Test.v3""
  IL_001d:  pop
  IL_001e:  ldsfld     ""ushort Test.v4""
  IL_0023:  pop
  IL_0024:  ldc.i4.3
  IL_0025:  call       ""void System.Console.Write(int)""
  IL_002a:  ldc.i4.4
  IL_002b:  call       ""void System.Console.Write(int)""
  IL_0030:  ldc.i4.5
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  ldc.i4.6
  IL_0037:  call       ""void System.Console.Write(int)""
  IL_003c:  ret
}
");
        }

        // TODO: Add VerifyIL for is and as Codegen tests
        [Fact]
        public void TestAsOperator()
        {
            var source = @"
namespace TestAsOperator
{
    public interface MyInter {}
    public class TestType : MyInter {}
    public class AnotherType {}
    public class MyBase {}
    public class MyDerived : MyBase { }
    public class C
    {
        static void Main()
        {
            string myStr = ""foo"";
            object o = myStr;
            object b = o as string;            

            TestType tt = null;
            o = tt;
            b = o as TestType;

            tt = new TestType();
            o = tt;
            b = o as AnotherType;
        
            b = o as MyInter;

            MyInter mi = new TestType();
            o = mi;
            b = o as MyInter;

            MyBase mb = new MyBase();
            o = mb;
            b = o as MyDerived;      

            MyDerived md = new MyDerived();
            o = md;
            b = o as MyBase;

            b = null as MyBase;            
        }
    }
}
";
            var compilation = CompileAndVerify(source);
        }

        [Fact]
        public void TestAsOperatorGeneric()
        {
            var source = @"
public class TestAsOperatorGeneric
{
    public static void Main() { }
    public static void M<T, U, W>(T t, U u, W w)
        where T : class
        where U : class
        where W : class
    {
        object test2 = u as object;
        System.Console.WriteLine(test2);

        U test3 = t as U;
        System.Console.WriteLine(test3);

        T test4 = t as T;
        System.Console.WriteLine(test4);

        U test5 = u as U;
        System.Console.WriteLine(test5);

        T test12 = u as T;
        System.Console.WriteLine(test12);

        object test7 = w as object;
        System.Console.WriteLine(test7);

        U test8 = w as U;
        System.Console.WriteLine(test8);

        W test9 = u as W;
        System.Console.WriteLine(test9);

        W test10 = t as W;
        System.Console.WriteLine(test10);

        W test11 = w as W;
        System.Console.WriteLine(test11);
    }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("TestAsOperatorGeneric.M<T, U, W>(T, U, W)",
@"{
  // Code size      186 (0xba)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        ""U""
  IL_0006:  call       ""void System.Console.WriteLine(object)""
  IL_000b:  ldarg.0
  IL_000c:  box        ""T""
  IL_0011:  isinst     ""U""
  IL_0016:  unbox.any  ""U""
  IL_001b:  box        ""U""
  IL_0020:  call       ""void System.Console.WriteLine(object)""
  IL_0025:  ldarg.0
  IL_0026:  box        ""T""
  IL_002b:  call       ""void System.Console.WriteLine(object)""
  IL_0030:  ldarg.1
  IL_0031:  box        ""U""
  IL_0036:  call       ""void System.Console.WriteLine(object)""
  IL_003b:  ldarg.1
  IL_003c:  box        ""U""
  IL_0041:  isinst     ""T""
  IL_0046:  unbox.any  ""T""
  IL_004b:  box        ""T""
  IL_0050:  call       ""void System.Console.WriteLine(object)""
  IL_0055:  ldarg.2
  IL_0056:  box        ""W""
  IL_005b:  call       ""void System.Console.WriteLine(object)""
  IL_0060:  ldarg.2
  IL_0061:  box        ""W""
  IL_0066:  isinst     ""U""
  IL_006b:  unbox.any  ""U""
  IL_0070:  box        ""U""
  IL_0075:  call       ""void System.Console.WriteLine(object)""
  IL_007a:  ldarg.1
  IL_007b:  box        ""U""
  IL_0080:  isinst     ""W""
  IL_0085:  unbox.any  ""W""
  IL_008a:  box        ""W""
  IL_008f:  call       ""void System.Console.WriteLine(object)""
  IL_0094:  ldarg.0
  IL_0095:  box        ""T""
  IL_009a:  isinst     ""W""
  IL_009f:  unbox.any  ""W""
  IL_00a4:  box        ""W""
  IL_00a9:  call       ""void System.Console.WriteLine(object)""
  IL_00ae:  ldarg.2
  IL_00af:  box        ""W""
  IL_00b4:  call       ""void System.Console.WriteLine(object)""
  IL_00b9:  ret
}");
        }

        [Fact, WorkItem(754408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754408")]
        public void TestNullCoalesce_DynamicAndObject()
        {
            var source = @"using System;
public class A {}
public class C
{
    public static dynamic Get()
    {
        object a = new A();
        dynamic b = new A();
        return a ?? b;
    }

    public static void Main()
    {
        var c = Get();
    }
}";
            var comp = CompileAndVerify(source,
                additionalRefs: new[] { CSharpRef, SystemCoreRef_v4_0_30319_17929 },
                expectedOutput: string.Empty);
            comp.VerifyIL("C.Get",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (object V_0) //b
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  newobj     ""A..ctor()""
  IL_000a:  stloc.0
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ldloc.0
  IL_0010:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_Implicit_b_To_a_nullable()
        {
            var source = @"
using System;

// a ?? b

public class E : D { } 
public class D { }
public class C
{
    public static int Main()
    {
        Nullable_a_Implicit_b_to_a0_null_a('a');
        Nullable_a_Implicit_b_to_a0_constant_non_null_a('a');
        Nullable_a_Implicit_b_to_a0_not_null_a('a', 10);
        return 0;
    }

    public static int Nullable_a_Implicit_b_to_a0_null_a(char ch)
    {        
        char b = ch;
        int? a = null;
        int z = a ?? b;
        return z;
    }
    public static int Nullable_a_Implicit_b_to_a0_constant_non_null_a(char ch)
    {
        char b = ch;
        int? a = 10;
        int z = a ?? b;
        return z;
    }
    public static int Nullable_a_Implicit_b_to_a0_not_null_a(char ch, int? i)
    {
        char b = ch;
        int? a = i;
        int z = a ?? b;
        return z;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: string.Empty);
            compilation.VerifyIL("C.Nullable_a_Implicit_b_to_a0_null_a", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (char V_0, //b
  int? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  initobj    ""int?""
  IL_000a:  ldloc.1
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  brtrue.s   IL_0017
  IL_0015:  ldloc.0
  IL_0016:  ret
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  ret
}
");
            compilation.VerifyIL("C.Nullable_a_Implicit_b_to_a0_constant_non_null_a", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (char V_0, //b
  int? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.s   10
  IL_0004:  newobj     ""int?..ctor(int)""
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""bool int?.HasValue.get""
  IL_0011:  brtrue.s   IL_0015
  IL_0013:  ldloc.0
  IL_0014:  ret
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       ""int int?.GetValueOrDefault()""
  IL_001c:  ret
}");
            compilation.VerifyIL("C.Nullable_a_Implicit_b_to_a0_not_null_a", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (char V_0, //b
  int? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_1
  IL_0006:  call       ""bool int?.HasValue.get""
  IL_000b:  brtrue.s   IL_000f
  IL_000d:  ldloc.0
  IL_000e:  ret
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""int int?.GetValueOrDefault()""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void TestNullCoalesce_Nullable_a_Implicit_b_to_a0()
        {
            var source = @"
using System;

// a ?? b

public class E : D { } 
public class D { }
public class C
{
    public static int Main()
    {
        Nullable_a_Implicit_b_to_a0_null_a('a');
        Nullable_a_Implicit_b_to_a0_constant_non_null_a('a');
        Nullable_a_Implicit_b_to_a0_not_null_a('a', 10);
        Nullable_a_Implicit_b_to_a_null_a('a');
        Nullable_a_Implicit_b_to_a_constant_non_null_a('a');
        Nullable_a_Implicit_b_to_a_not_null_a('a', 10);
        return 0;
    }

    public static int Nullable_a_Implicit_b_to_a0_null_a(char ch)
    {        
        char b = ch;
        int? a = null;
        int z = a ?? b;
        return z;
    }
    public static int Nullable_a_Implicit_b_to_a0_constant_non_null_a(char ch)
    {
        char b = ch;
        int? a = 10;
        int z = a ?? b;
        return z;
    }
    public static int Nullable_a_Implicit_b_to_a0_not_null_a(char ch, int? i)
    {
        char b = ch;
        int? a = i;
        int z = a ?? b;
        return z;
    }

    public static int? Nullable_a_Implicit_b_to_a_null_a(char? ch)
    {
        char? b = ch;
        int? a = null;
        int? z = a ?? b;
        return z;
    }
    public static int? Nullable_a_Implicit_b_to_a_constant_non_null_a(char? ch)
    {
        char? b = ch;
        int? a = 10;
        int? z = a ?? b;
        return z;
    }
    public static int? Nullable_a_Implicit_b_to_a_not_null_a(char ch, int? i)
    {
        char? b = ch;
        int? a = i;
        int? z = a ?? b;
        return z;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: string.Empty);
            compilation.VerifyIL("C.Nullable_a_Implicit_b_to_a0_null_a", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (char V_0, //b
  int? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  initobj    ""int?""
  IL_000a:  ldloc.1
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""bool int?.HasValue.get""
  IL_0013:  brtrue.s   IL_0017
  IL_0015:  ldloc.0
  IL_0016:  ret
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""int int?.GetValueOrDefault()""
  IL_001e:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_Nullable_a_ImplicitReference_a_to_b()
        {
            var source = @"
using System;

// a ?? b

public class E : D { } 
public class D { }
public class C
{
    public static int Main()
    {
        ImplicitReference_a_to_b_null_a_nullable(10);
        ImplicitReference_a_to_b_constant_nonnull_a_nullable(10);
        ImplicitReference_a_to_b_not_null_a_nullable(10, 'a');
        Null_Literal_a('a');
        return 0;
    }

    public static int? ImplicitReference_a_to_b_null_a_nullable(int? c)
    {
        int? b = c;
        char? a = null;
        int? z = a ?? b;
        return z;
    }
    public static int? ImplicitReference_a_to_b_constant_nonnull_a_nullable(int? c)
    {
        int? b = c;
        char? a = 'a';
        int? z = a ?? b;
        return z;
    }
    public static int? ImplicitReference_a_to_b_not_null_a_nullable(int? c, char? d)
    {
        int? b = c;
        char? a = d;
        int? z = a ?? b;
        return z;
    }
    public static char? Null_Literal_a(char? ch)
    {
        char? b = ch;
        char? z = null ?? b;
        return z;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: string.Empty);
            compilation.VerifyIL("C.ImplicitReference_a_to_b_null_a_nullable", @"
{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (int? V_0, //b
  char? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  initobj    ""char?""
  IL_000a:  ldloc.1
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""bool char?.HasValue.get""
  IL_0013:  brtrue.s   IL_0017
  IL_0015:  ldloc.0
  IL_0016:  ret
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       ""char char?.GetValueOrDefault()""
  IL_001e:  newobj     ""int?..ctor(int)""
  IL_0023:  ret
}");

            compilation.VerifyIL("C.Null_Literal_a", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_ImplicitReference_a_to_b()
        {
            var source = @"
using System;

// a ?? b

public class E : D { } 
public class D { }
public class C
{
    public static int Main()
    {
        ImplicitReference_a_to_b_null_a();
        ImplicitReference_a_to_b_not_null_a();
        return 0;
    }
    public static D ImplicitReference_a_to_b_null_a()
    {
        D b = new D();
        E a = null;
        D z = a ?? b;
        return z;
    }
    public static D ImplicitReference_a_to_b_not_null_a()
    {
        D b = new D();
        E a = new E();
        D z = a ?? b;
        return z;
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: string.Empty);
            compilation.VerifyIL("C.ImplicitReference_a_to_b_null_a", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (D V_0) //b
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldnull
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000c
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  ret
}
");
            compilation.VerifyIL("C.ImplicitReference_a_to_b_not_null_a",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (D V_0) //b
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""E..ctor()""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ldloc.0
  IL_0010:  ret
}
");
        }

        [WorkItem(541337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541337")]
        [Fact]
        public void TestNullCoalesce_TypeParameter_Bug8008()
        {
            var source = @"
static class Program
{
    static void Main()
    {
    }
 
    static void Foo<T>(T x)
    {
        var y = default(T) ?? x;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,17): error CS0019: Operator '??' cannot be applied to operands of type 'T' and 'T'
                //         var y = default(T) ?? x;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "default(T) ?? x").WithArguments("??", "T", "T").WithLocation(10, 17)); ;
        }

        [Fact]
        public void TestNullCoalesce_NoDuplicateCallsToFoo()
        {
            var source = @"
// a ?? b

public class Test
{
    static void Main()
    {
        object o = Foo() ?? Bar();
    }

    static object Foo()
    {
        System.Console.Write(""Foo"");
        return new object();
    }

    static object Bar()
    {
        System.Console.Write(""Bar"");
        return new object();
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "Foo");
            compilation.VerifyIL("Test.Main", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  call       ""object Test.Foo()""
  IL_0005:  brtrue.s   IL_000d
  IL_0007:  call       ""object Test.Bar()""
  IL_000c:  pop
  IL_000d:  ret
}
");
        }

        [WorkItem(541232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541232")]
        [Fact]
        public void TestNullCoalesce_MethodGroups()
        {
            var source =
@"class C
{
    static void M()
    {
        System.Action a = null;
        a = a ?? M;
        a = M ?? a;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,13): error CS0019: Operator '??' cannot be applied to operands of type 'method group' and 'System.Action'
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "M ?? a").WithArguments("??", "method group", "System.Action").WithLocation(7, 13));
        }

        /// <summary>
        /// From orcas bug #42645.  PEVerify fails.
        /// </summary>
        [Fact]
        public void TestNullCoalesce_InterfaceRegression1()
        {
            var source = @"
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        List<int> b = new List<int>();

        IEnumerable<int> c = a ?? (IEnumerable<int>)b;
        Foo(c);
    }

    static void Foo<T>(T x)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            // Note the explicit casts, even though the conversions are
            // implicit reference conversions.
            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (System.Collections.Generic.List<int> V_0, //b
  System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000b:  stloc.0
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0013
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void TestNullCoalesce_InterfaceRegression1a()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b = new List<int>();

        IEnumerable<int> c = b ?? a;
        Foo(c);
    }

    static void Foo<T>(T x)
    {
        System.Console.WriteLine(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (int[] V_0, //a
  System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0013
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_0018:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_InterfaceRegression1b()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b;

        IEnumerable<int> c = (b = (IEnumerable<int>)new List<int>()) ?? a;
        Foo(c);
        Foo(b);
    }

    static void Foo<T>(T x)
    {
        System.Console.Write(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int[] V_0, //a
  System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  dup
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0014
  IL_0012:  pop
  IL_0013:  ldloc.0
  IL_0014:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_0019:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_InterfaceRegression1c()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b = new List<int>();

        Foo(b, b ?? a);
    }

    static void Foo<T, U>(T x, U y)
    {
        System.Console.Write(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  3
  .locals init (int[] V_0, //a
  System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  dup
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0014
  IL_0012:  pop
  IL_0013:  ldloc.0
  IL_0014:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)""
  IL_0019:  ret
}
");
        }

        /// <summary>
        /// From whidbey bug #49619.  PEVerify fails.
        /// </summary>
        /// <remarks>
        /// Dev10 does not produce verifiable code for this example.
        /// </remarks>
        [Fact]
        public void TestNullCoalesce_InterfaceRegression2()
        {
            var source = @"
public interface IA { }
public interface IB { int f(); }
public class AB1 : IA, IB { public int f() { return 42; } }
public class AB2 : IA, IB { public int f() { return 1; } }

class MainClass
{
    public static void g(AB1 ab1)
    {
        ((IB)ab1 ?? (IB)new AB2()).f();
    }
}";
            // Note the explicit casts, even though the conversions are
            // implicit reference conversions.
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("MainClass.g", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (IB V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_000c
  IL_0006:  pop
  IL_0007:  newobj     ""AB2..ctor()""
  IL_000c:  callvirt   ""int IB.f()""
  IL_0011:  pop
  IL_0012:  ret
}");
        }

        [Fact, WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void TestNullCoalesce_Nested()
        {
            var src =
@"interface I { void DoNothing(); }
class A : I { public void DoNothing() {} }
class B : I { public void DoNothing() {} }
class C : I
{
    public void DoNothing() {}

    static I Tester(A a, B b)
    {
        I i = a ?? (b ?? (I)new C());
        i.DoNothing();
        i = a ?? ((I)b ?? new C());
        i.DoNothing();
        i = ((I)a ?? b) ?? new C();
        i.DoNothing();
        return i;
    }

    static void Main()
    {
        System.Console.Write(Tester(null, null).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe, expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       86 (0x56)
  .maxstack  2
  .locals init (I V_0, //i
                I V_1,
                I V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_0014
  IL_0007:  pop
  IL_0008:  ldarg.1
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0014
  IL_000e:  pop
  IL_000f:  newobj     ""C..ctor()""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""void I.DoNothing()""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  stloc.1
  IL_001e:  ldloc.1
  IL_001f:  dup
  IL_0020:  brtrue.s   IL_002f
  IL_0022:  pop
  IL_0023:  ldarg.1
  IL_0024:  stloc.1
  IL_0025:  ldloc.1
  IL_0026:  dup
  IL_0027:  brtrue.s   IL_002f
  IL_0029:  pop
  IL_002a:  newobj     ""C..ctor()""
  IL_002f:  stloc.0
  IL_0030:  ldloc.0
  IL_0031:  callvirt   ""void I.DoNothing()""
  IL_0036:  nop
  IL_0037:  ldarg.0
  IL_0038:  stloc.1
  IL_0039:  ldloc.1
  IL_003a:  dup
  IL_003b:  brtrue.s   IL_003f
  IL_003d:  pop
  IL_003e:  ldarg.1
  IL_003f:  dup
  IL_0040:  brtrue.s   IL_0048
  IL_0042:  pop
  IL_0043:  newobj     ""C..ctor()""
  IL_0048:  stloc.0
  IL_0049:  ldloc.0
  IL_004a:  callvirt   ""void I.DoNothing()""
  IL_004f:  nop
  IL_0050:  ldloc.0
  IL_0051:  stloc.2
  IL_0052:  br.s       IL_0054
  IL_0054:  ldloc.2
  IL_0055:  ret
}");
            // Optimized
            verify = CompileAndVerify(src, expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0013
  IL_0006:  pop
  IL_0007:  ldarg.1
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  newobj     ""C..ctor()""
  IL_0013:  callvirt   ""void I.DoNothing()""
  IL_0018:  ldarg.0
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  dup
  IL_001c:  brtrue.s   IL_002b
  IL_001e:  pop
  IL_001f:  ldarg.1
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  dup
  IL_0023:  brtrue.s   IL_002b
  IL_0025:  pop
  IL_0026:  newobj     ""C..ctor()""
  IL_002b:  callvirt   ""void I.DoNothing()""
  IL_0030:  ldarg.0
  IL_0031:  stloc.0
  IL_0032:  ldloc.0
  IL_0033:  dup
  IL_0034:  brtrue.s   IL_0038
  IL_0036:  pop
  IL_0037:  ldarg.1
  IL_0038:  dup
  IL_0039:  brtrue.s   IL_0041
  IL_003b:  pop
  IL_003c:  newobj     ""C..ctor()""
  IL_0041:  dup
  IL_0042:  callvirt   ""void I.DoNothing()""
  IL_0047:  ret
}");
        }

        [Fact]
        public void TestNullCoalesce_FuncVariance()
        {
            var source = @"
using System;
using System.Collections.Generic;

    class Program
    {
        static void Main(string[] args)
        {
            Func<Exception[]> f1 = null;

            Func<IEnumerable<object>> f2 = null;

            var oo = f1 ?? f2;
            Console.WriteLine(oo);

            oo = f2 ?? f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (System.Func<System.Exception[]> V_0, //f1
  System.Func<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  System.Func<System.Collections.Generic.IEnumerable<object>> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000c
  IL_000a:  pop
  IL_000b:  ldloc.1
  IL_000c:  call       ""void System.Console.WriteLine(object)""
  IL_0011:  ldloc.1
  IL_0012:  dup
  IL_0013:  brtrue.s   IL_0019
  IL_0015:  pop
  IL_0016:  ldloc.0
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void TestNullCoalesce_FuncVariance01()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

    class Program
    {
        static void Main(string[] args)
        {
            Func<Exception[]> f1 = null;

            Func<IEnumerable<object>> f2 = null;

            var oo = (Func<IEnumerable>)f1 ?? (Func<IEnumerable>)f2;
            Console.WriteLine(oo);

            oo = (Func<IEnumerable>)f2 ?? (Func<IEnumerable>)f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (System.Func<System.Exception[]> V_0, //f1
  System.Func<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  System.Func<System.Collections.IEnumerable> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000e
  IL_000a:  pop
  IL_000b:  ldloc.1
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ldloc.1
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  pop
  IL_001a:  ldloc.0
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  ret
}
");
        }

        [Fact]
        public void TestNullCoalesce_InterfaceVariance()
        {
            var source = @"
using System;
using System.Collections.Generic;

    class Program
    {
        interface CoInter<out T>
        {
        }

        static void Main(string[] args)
        {
            CoInter<Exception[]> f1 = null;

            CoInter<IEnumerable<object>> f2 = null;

            var oo = f1 ?? f2;
            Console.WriteLine(oo);

            oo = f2 ?? f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Program.CoInter<System.Exception[]> V_0, //f1
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000c
  IL_000a:  pop
  IL_000b:  ldloc.1
  IL_000c:  call       ""void System.Console.WriteLine(object)""
  IL_0011:  ldloc.1
  IL_0012:  dup
  IL_0013:  brtrue.s   IL_0019
  IL_0015:  pop
  IL_0016:  ldloc.0
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void TestNullCoalesce_InterfaceVariance01()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

    class Program
    {
        interface CoInter<out T>
        {
        }

        static void Main(string[] args)
        {
            CoInter<Exception[]> f1 = null;

            CoInter<IEnumerable<object>> f2 = null;

            var oo = (CoInter<IEnumerable>)f1 ?? (CoInter<IEnumerable>)f2;
            Console.WriteLine(oo);

            oo = (CoInter<IEnumerable>)f2 ?? (CoInter<IEnumerable>)f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (Program.CoInter<System.Exception[]> V_0, //f1
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  Program.CoInter<System.Collections.IEnumerable> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_000e
  IL_000a:  pop
  IL_000b:  ldloc.1
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ldloc.1
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_001d
  IL_0019:  pop
  IL_001a:  ldloc.0
  IL_001b:  stloc.2
  IL_001c:  ldloc.2
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  ret
}
");
        }

        [WorkItem(543074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543074")]
        [Fact]
        public void TestEqualEqualOnNestedStructGuid()
        {
            var source = @"
using System;

public class Parent
{
    public System.Guid Foo(int d = 0, System.Guid g = default(System.Guid)) { return g; }
}

public class Test
{
    public static void Main()
    {
        var x = new Parent().Foo();
        var ret = x == default(System.Guid); 
        Console.Write(ret);
    }
}
";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(543092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543092")]
        [Fact]
        public void ShortCircuitConditionalOperator()
        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;

class X
{
    public int selectCount = 0;
    public bool Select<T>(Func<int, T> selector)
    {
        selectCount++;
        return true;
    }
}

class P
{
    static int Main()
    {
        int errCount = 0;
        var src = new X();
        // QE is not 'executed'
        var b = false && from x in src select x; // WRN CS0429
        if (src.selectCount == 1)
            errCount++;

        Console.Write(errCount);
        return (errCount > 0) ? 1 : 0;
    }
}";
            // the grammar does not allow a query on the right-hand-side of &&, but we allow it except in strict mode.
            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: TestOptions.Regular.WithFeature("strict", "true")).VerifyDiagnostics(
                // (23,26): error CS1525: Invalid expression term 'from'
                //         var b = false && from x in src select x; // WRN CS0429
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "from x in src").WithArguments("from").WithLocation(23, 26),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;").WithLocation(4, 1),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;").WithLocation(3, 1)
                );
            CompileAndVerify(source, additionalRefs: new[] { LinqAssemblyRef },
                expectedOutput: "0");
        }

        [WorkItem(543109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543109")]
        [Fact()]
        public void ShortCircuitConditionalOperator02()
        {
            var source = @"
using System;

class P
{
    static int Main()
    {
        int errCount = 0;
        var f = false;
        var b = f && (0 == errCount++);
        Console.Write(errCount);
        return (errCount > 0) ? 1 : 0;
    }
}";
            CompileAndVerify(source, additionalRefs: new[] { LinqAssemblyRef },
                expectedOutput: "0");
        }

        [WorkItem(543377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543377")]
        [Fact]
        public void DecimalComparison()
        {
            var source = @"
class Program
{
    static void Main()
    {
        decimal d1 = 1.0201m;
        if (d1 == 10201M)
        {
        }
    }
}";
            CompileAndVerify(source).
VerifyIL("Program.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  5
  IL_0000:  ldc.i4     0x27d9
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.4
  IL_0009:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000e:  ldc.i4     0x27d9
  IL_0013:  newobj     ""decimal..ctor(int)""
  IL_0018:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_001d:  pop
  IL_001e:  ret
}");
        }

        [WorkItem(543453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543453")]
        [Fact]
        public void TestIncrementDecrementOperator_Generic()
        {
            var source = @"
using System;
class BaseType<T> where T : BaseType<T>, new()
{
    public static T operator ++(BaseType<T> x)
    {
        return null;
    }
    public static implicit operator T(BaseType<T> x)
    {
        return null;
    }
}

class DrivedType : BaseType<DrivedType>
{
    public static void Main()
    {
        BaseType<DrivedType> test = new BaseType<DrivedType>();
        DrivedType test2 = test++;
    }
}
";
            CompileAndVerify(source);
        }

        [WorkItem(543474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543474")]
        [Fact]
        public void LogicalComplementOperator()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        checked
        {
            var b = false;
            if (!b) {System.Console.Write(""1""); }
        }
    }
}";
            CompileAndVerify(source,
                expectedOutput: "1");
        }

        [WorkItem(543500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543500")]
        [Fact]
        public void BuiltInLeftShiftOperators()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        uint ui = 1;
        long l = 1;
        ulong ul = 1;

        Console.WriteLine(i << 31);
        Console.WriteLine(i << 32);
        Console.WriteLine(i << 33);
        Console.WriteLine(i << -1);
        Console.WriteLine();

        Console.WriteLine(ui << 31);
        Console.WriteLine(ui << 32);
        Console.WriteLine(ui << 33);
        Console.WriteLine(ui << -1);
        Console.WriteLine();

        Console.WriteLine(l << 63);
        Console.WriteLine(l << 64);
        Console.WriteLine(l << 65);
        Console.WriteLine(l << -1);
        Console.WriteLine();

        Console.WriteLine(ul << 63);
        Console.WriteLine(ul << 64);
        Console.WriteLine(ul << 65);
        Console.WriteLine(ul << -1);
        Console.WriteLine();
    }
}";
            CompileAndVerify(source,
                expectedOutput: @"
-2147483648
1
2
-2147483648

2147483648
1
2
2147483648

-9223372036854775808
1
2
-9223372036854775808

9223372036854775808
1
2
9223372036854775808
");
        }

        [WorkItem(543500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543500")]
        [Fact]
        public void BuiltInRightShiftOperators()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int i = int.MaxValue;
        uint ui = uint.MaxValue;
        long l = long.MaxValue;
        ulong ul = ulong.MaxValue;

        Console.WriteLine(i >> 31);
        Console.WriteLine(i >> 32);
        Console.WriteLine(i >> 33);
        Console.WriteLine(i >> -1);
        Console.WriteLine();

        Console.WriteLine(ui >> 31);
        Console.WriteLine(ui >> 32);
        Console.WriteLine(ui >> 33);
        Console.WriteLine(ui >> -1);
        Console.WriteLine();

        Console.WriteLine(l >> 63);
        Console.WriteLine(l >> 64);
        Console.WriteLine(l >> 65);
        Console.WriteLine(l >> -1);
        Console.WriteLine();

        Console.WriteLine(ul >> 63);
        Console.WriteLine(ul >> 64);
        Console.WriteLine(ul >> 65);
        Console.WriteLine(ul >> -1);
        Console.WriteLine();
    }
}";
            CompileAndVerify(source,
                expectedOutput: @"
0
2147483647
1073741823
0

1
4294967295
2147483647
1

0
9223372036854775807
4611686018427387903
0

1
18446744073709551615
9223372036854775807
1
");
        }

        [WorkItem(543500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543500")]
        [Fact]
        public void BuiltInShiftOperators()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        long l = long.MaxValue;
        int amount = 1;

        Console.WriteLine(i << 31); // 31
        Console.WriteLine(i << 32); // no shift
        Console.WriteLine(i << 33); // 1
        Console.WriteLine(i << -1); // 31
        Console.WriteLine(i >> amount); // & 31
        Console.WriteLine();

        Console.WriteLine(l >> 63); // 63
        Console.WriteLine(l >> 64); // no shift
        Console.WriteLine(l >> 65); // 1
        Console.WriteLine(l >> -1); // 63
        Console.WriteLine(l >> amount); // & 63
        Console.WriteLine();
    }
}";
            var verifier = CompileAndVerify(source,
                expectedOutput: @"
-2147483648
1
2
-2147483648
0

0
9223372036854775807
4611686018427387903
0
4611686018427387903
");

            verifier.VerifyIL("Program.Main", @"
{
  // Code size      109 (0x6d)
  .maxstack  3
  .locals init (long V_0, //l
  int V_1) //amount
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i8     0x7fffffffffffffff
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  ldc.i4.s   31
  IL_0010:  shl
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  dup
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  dup
  IL_001d:  ldc.i4.1
  IL_001e:  shl
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  dup
  IL_0025:  ldc.i4.s   31
  IL_0027:  shl
  IL_0028:  call       ""void System.Console.WriteLine(int)""
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4.s   31
  IL_0030:  and
  IL_0031:  shr
  IL_0032:  call       ""void System.Console.WriteLine(int)""
  IL_0037:  call       ""void System.Console.WriteLine()""
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.s   63
  IL_003f:  shr
  IL_0040:  call       ""void System.Console.WriteLine(long)""
  IL_0045:  ldloc.0
  IL_0046:  call       ""void System.Console.WriteLine(long)""
  IL_004b:  ldloc.0
  IL_004c:  ldc.i4.1
  IL_004d:  shr
  IL_004e:  call       ""void System.Console.WriteLine(long)""
  IL_0053:  ldloc.0
  IL_0054:  ldc.i4.s   63
  IL_0056:  shr
  IL_0057:  call       ""void System.Console.WriteLine(long)""
  IL_005c:  ldloc.0
  IL_005d:  ldloc.1
  IL_005e:  ldc.i4.s   63
  IL_0060:  and
  IL_0061:  shr
  IL_0062:  call       ""void System.Console.WriteLine(long)""
  IL_0067:  call       ""void System.Console.WriteLine()""
  IL_006c:  ret
}
");
        }

        [Fact, WorkItem(543993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543993")]
        public void BuiltInShiftOperators01()
        {
            var source = @"
using System;

public class Test
{
    public static void Main()
    {
        int n = 2;
        int v1 = (1 << n) << 3;
        int v2 = 1 << n << 3;
        Console.Write(""{0}=={1} "", v1, v2);
        v1 = (1 >> n) >> 3;
        v2 = 1 >> n >> 3;
        Console.Write(""{0}=={1}"", v1, v2);
    }
}
";
            var verifier = CompileAndVerify(source,
                expectedOutput: @"32==32 0==0");

            verifier.VerifyIL("Test.Main", @"
{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (int V_0, //n
  int V_1, //v1
  int V_2) //v2
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.s   31
  IL_0006:  and
  IL_0007:  shl
  IL_0008:  ldc.i4.3
  IL_0009:  shl
  IL_000a:  stloc.1
  IL_000b:  ldc.i4.1
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.s   31
  IL_000f:  and
  IL_0010:  shl
  IL_0011:  ldc.i4.3
  IL_0012:  shl
  IL_0013:  stloc.2
  IL_0014:  ldstr      ""{0}=={1} ""
  IL_0019:  ldloc.1
  IL_001a:  box        ""int""
  IL_001f:  ldloc.2
  IL_0020:  box        ""int""
  IL_0025:  call       ""void System.Console.Write(string, object, object)""
  IL_002a:  ldc.i4.1
  IL_002b:  ldloc.0
  IL_002c:  ldc.i4.s   31
  IL_002e:  and
  IL_002f:  shr
  IL_0030:  ldc.i4.3
  IL_0031:  shr
  IL_0032:  stloc.1
  IL_0033:  ldc.i4.1
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.s   31
  IL_0037:  and
  IL_0038:  shr
  IL_0039:  ldc.i4.3
  IL_003a:  shr
  IL_003b:  stloc.2
  IL_003c:  ldstr      ""{0}=={1}""
  IL_0041:  ldloc.1
  IL_0042:  box        ""int""
  IL_0047:  ldloc.2
  IL_0048:  box        ""int""
  IL_004d:  call       ""void System.Console.Write(string, object, object)""
  IL_0052:  ret
}
");
        }

        [Fact, WorkItem(543993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543993")]
        public void BuiltInShiftOperators02()
        {
            var source = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(1 << 2 << 3);
        System.Console.WriteLine((1 << 2) << 3);
        System.Console.WriteLine(1 << (2 << 3));
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
32
32
65536");
        }

        [WorkItem(543568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543568")]
        [Fact]
        public void ImplicitConversionOperatorWithOptionalParam()
        {
            var source = @"
using System;

public class C
{
    static public implicit operator int(C d = null) // warning CS1066
    {
        if (d != null) return 0;
        return 1;
    }
}

class TestFunction
{
    public static void Main()
    {
        var tf = new C();
        int result = tf;
        Console.Write(result);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543569")]
        [Fact()]
        public void AssignmentInOperandOfIsAlwaysFalse()
        {
            var source = @"
using System;

public class Program
{
    public static void Main()
    {
        int[] a = null;
        bool result = (a = new[] { 4, 5, 6 }) is char[]; // warning CS0184
        if (a == null)
        {
            Console.WriteLine(""FAIL"");
        }
        else
        {
            Console.WriteLine(""PASS"");
        }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"PASS");
        }

        [WorkItem(543577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543577")]
        [Fact()]
        public void IsOperatorAlwaysFalseInLambda()
        {
            var source = @"
using System;

class C
{
    static int counter = 0;
    public static void Increment()
    {
        counter++;
    }

    public static void Main()
    {
        Func<bool> testExpr = () => Increment() is object; // warning CS0184
        Console.WriteLine(counter);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543446"), WorkItem(543446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543446")]
        [Fact]
        public void ThrowExceptionByConversion()
        {
            var source = @"using System;

namespace Test
{
    class DException : Exception
    {
        public static implicit operator DException(Action d)
        {
            return new DException();
        }
    }

    class Program
    {
        static void M()  {    }

        static void Main()
        {
            try
            {
                throw (DException) M;
            }
            catch (DException)
            {
                Console.Write(0);
            }
            Console.Write(1);
        }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"01");
        }

        [WorkItem(543586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543586")]
        [Fact]
        public void ImplicitVsExplicitOverloadOperators()
        {
            var source = @"using System;
class Test
{
    static void Main()
    {
        Str str = (Str)1;
        Console.WriteLine(str.num);
    }
}

struct Str
{
    public int num;
    public static explicit operator Str(int i)
    {
        Str temp;
        temp.num = 10;
        return temp;
    }

    public static implicit operator Str(double i)
    {
        Str temp;
        temp.num = 100;
        return temp;
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"10");
        }

        [WorkItem(543602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543602")]
        [WorkItem(543660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543660")]
        [Fact]
        public void SwitchExpressionWithImplicitConversion()
        {
            var source = @"using System;
public class Test
{
    public static implicit operator int(Test val)
    {
        return 1;
    }

    public static implicit operator float(Test val)
    {
        return 2.1f;
    }

    public static int Main()
    {
        Test t = new Test();
        switch (t)
        {
            case 1:
                Console.WriteLine(0);
                return 0;
            default:
                Console.WriteLine(1);
                return 1;
        }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"0");
        }

        [WorkItem(543498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543498")]
        [Fact]
        public void UserDefinedConversionAfterUserDefinedIncrement()
        {
            var source =
@"using System;

class A
{
    public static C operator ++(A x)
    {
        Console.Write('3');
        return new C();
    }
}

class C : A
{
    public static implicit operator B(C x)
    {
        Console.Write('4');
        return new B();
    }
}

class B : A
{
    static void Main()
    {
        Console.Write('1');
        B b = new B();
        Console.Write('2');
        b++;
        Console.Write('5');
    }
}";
            CompileAndVerify(source, expectedOutput: "12345");
        }

        [Fact]
        public void TestXor()
        {
            var source = @"
using System;

class Program
{
    static bool t() { return true; }
    static bool f() { return false; }
    static void write(bool b) { Console.WriteLine(b); }

    static void Main(string[] args)
    {
        write(t() ^ t());
        write(t() ^ f());
        write(f() ^ t());
        write(f() ^ f());
        Console.WriteLine(""---"");
        write(!(t() ^ t()));
        write(!(t() ^ f()));
        write(!(f() ^ t()));
        write(!(f() ^ f()));
        Console.WriteLine(""---"");
        write((t() ^ t()) || (t() ^ f()));
        write((t() ^ f()) || (t() ^ t()));
        write((f() ^ t()) || (f() ^ f()));
        write((f() ^ f()) || (t() ^ t()));
        Console.WriteLine(""---"");
        write((t() ^ t()) && (t() ^ f()));
        write((t() ^ f()) && (t() ^ t()));
        write((f() ^ t()) && (f() ^ f()));
        write((f() ^ f()) && (f() ^ t()));
        Console.WriteLine(""---"");
        write((t() ^ t()) || !(t() ^ f()));
        write((t() ^ f()) || !(t() ^ t()));
        write(!(f() ^ t()) || (f() ^ f()));
        write(!(f() ^ f()) || (f() ^ t()));
        Console.WriteLine(""---"");
        write((t() ^ t()) && !(t() ^ f()));
        write((t() ^ f()) && !(t() ^ t()));
        write(!(f() ^ t()) && (f() ^ f()));
        write(!(f() ^ f()) && (f() ^ t()));
    }
}
";
            string expectedOutput =
@"False
True
True
False
---
True
False
False
True
---
True
True
True
False
---
False
False
False
False
---
False
True
False
True
---
False
True
False
True
";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestXorInIf()
        {
            var source = @"
using System;

class Program
{
    static bool t() { return true; }
    static bool f() { return false; }
    static void Main(string[] args)
    {
        if (t() ^ t())
        {
            Console.WriteLine(""1"");
        }
        if (!(t() ^ f()))
        {
            Console.WriteLine(""2"");
        }
        if (f() ^ t())
        {
            Console.WriteLine(""3"");
        }
        if (f() ^ f())
        {
            Console.WriteLine(""4"");
        }
        if ((t() ^ f()) && (f() ^ t()))
        {
            Console.WriteLine(""5"");
        }
        if ((t() ^ t()) || (t() ^ f()))
        {
            Console.WriteLine(""6"");
        }
        if ((t() ^ t()) || (f() ^ f()))
        {
            Console.WriteLine(""7"");
        }
    }
}
";
            string expectedOutput =
@"3
5
6";
            var compilation = CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void XorIl()
        {
            var text = @"using System;
class MyClass
{
    public static bool f = false, t = true;
    public static bool r;
    public static void Main()
    {
        r = f ^ t;
        r = !(f ^ t);
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: "");

            comp.VerifyIL("MyClass.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""bool MyClass.f""
  IL_0005:  ldsfld     ""bool MyClass.t""
  IL_000a:  xor
  IL_000b:  stsfld     ""bool MyClass.r""
  IL_0010:  ldsfld     ""bool MyClass.f""
  IL_0015:  ldsfld     ""bool MyClass.t""
  IL_001a:  ceq
  IL_001c:  stsfld     ""bool MyClass.r""
  IL_0021:  ret
}");
        }

        [Fact, WorkItem(543446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543446")]
        public void UserDefinedConversionAfterUserDefinedConvert()
        {
            var source =
@"using System;
delegate void D(int p1);
class DException : Exception
{
    public D d;
    public static implicit operator DException(D d)
    {
        DException e = new DException();
        e.d = d;
        return e;
    }
}
class Program
{
    static void PM(int p1)
    {
    }
    static void Main()
    {
        throw (DException)PM;
    }
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void UserDefinedOperatorAfterUserDefinedConversion()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        var c = new C();
        var trash = c + c; // which +?
    }
}

class C
{
    public static string operator +(C c, string s)
    {
        Console.WriteLine(""+(C,string)"");
        return ""+s"";
    }
    public static string operator +(C c, object o)
    {
        Console.WriteLine(""+(C,object)"");
        return ""+o"";
    }
    public static implicit operator string(C c)
    {
        Console.WriteLine(""C->string"");
        return ""C->string"";
    }
    public override string ToString()
    {
        Console.WriteLine(""C.ToString()"");
        return ""C2"";
    }
}";
            CompileAndVerify(source, expectedOutput:
@"C->string
+(C,string)
");
        }

        [Fact, WorkItem(529248, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529248")]
        public void TestNullCoalescingOperatorWithNullableConversions()
        {
            // Native compiler violates the language specification while binding the type for the null coalescing operator (??).
            // The last bullet in section 7.13 of the specification for binding the type of ?? operator states that:

            // SPEC:    Otherwise, if b has a type B and an implicit conversion exists from a to B, the result type is B.
            // SPEC:    At run-time, a is first evaluated. If a is not null, a is unwrapped to type A0 (if A exists and is nullable)
            // SPEC:    and converted to type B, and this becomes the result. Otherwise, b is evaluated and becomes the result.

            // Note that for this test there is no implicit conversion from 's' -> int (SnapshotPoint? -> int), but there is an implicit conversion
            // from stripped type SnapshotPoint -> int.

            // Native compiler instead implements this part based on whether A is a nullable type or not. We maintain compatibility with the native compiler:

            // SPEC PROPOSAL:    Otherwise, if A exists and is a nullable type and if b has a type B and an implicit  conversion exists from A0 to B,
            // SPEC PROPOSAL:    the result type is B. At run-time, a is first evaluated. If a is not null, a is unwrapped to type A0 and converted to type B,
            // SPEC PROPOSAL:    and this becomes the result. Otherwise, b is evaluated and becomes the result.
            //
            // SPEC PROPOSAL:    Otherwise, if A does not exist or is a non-nullable type and if b has a type B and an implicit conversion exists from a to B,
            // SPEC PROPOSAL:    the result type is B. At run-time, a is first evaluated. If a is not null, a is converted to type B, and this becomes the result.
            // SPEC PROPOSAL:    Otherwise, b is evaluated and becomes the result.


            string source = @"
struct SnapshotPoint
{
    public static implicit operator int(SnapshotPoint snapshotPoint)
    {
        System.Console.WriteLine(""Pass"");
        return 0;
    }
}
class Program
{
    static void Main(string[] args)
    {
       SnapshotPoint? s = new SnapshotPoint();
       var r = s ?? -1;
       SnapshotPoint? s2 = null;
       r = s2 ?? -1;
    }
}
";
            var verifier = CompileAndVerify(source: source, expectedOutput: "Pass");

            verifier.VerifyIL("Program.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  1
  .locals init (SnapshotPoint V_0,
  SnapshotPoint? V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""SnapshotPoint""
  IL_0008:  ldloc.0
  IL_0009:  newobj     ""SnapshotPoint?..ctor(SnapshotPoint)""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""bool SnapshotPoint?.HasValue.get""
  IL_0016:  brfalse.s  IL_0025
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""SnapshotPoint SnapshotPoint?.GetValueOrDefault()""
  IL_001f:  call       ""int SnapshotPoint.op_Implicit(SnapshotPoint)""
  IL_0024:  pop
  IL_0025:  ldloca.s   V_1
  IL_0027:  initobj    ""SnapshotPoint?""
  IL_002d:  ldloc.1
  IL_002e:  stloc.1
  IL_002f:  ldloca.s   V_1
  IL_0031:  call       ""bool SnapshotPoint?.HasValue.get""
  IL_0036:  brfalse.s  IL_0045
  IL_0038:  ldloca.s   V_1
  IL_003a:  call       ""SnapshotPoint SnapshotPoint?.GetValueOrDefault()""
  IL_003f:  call       ""int SnapshotPoint.op_Implicit(SnapshotPoint)""
  IL_0044:  pop
  IL_0045:  ret
}");
        }

        [Fact, WorkItem(543980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543980")]
        public void IsOperatorOnEnumAndTypeParameterConstraintToStruct()
        {
            string source = @"using System;

public enum E {  One }

class Gen<T> where T : struct
{
    public static void TestIsOperatorEnum(T t)
    {
        Console.WriteLine(t is Enum);
        Console.WriteLine(t is E);
        Console.WriteLine(t as Enum);
    }
}

public class Test
{
    public static void Main()
    {
        Gen<E>.TestIsOperatorEnum(new E());
    }
}
";
            string expectedOutput = @"True
True
One";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(543982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543982")]
        public void OverloadAdditionOperatorOnGenericClass()
        {
            var source = @"
using System;

public class G<T>
{
    public static G<T> operator ~(G<T> g)
    {
        Console.WriteLine(""G<{0}> unary negation"", typeof(T));
        return new G<T>();
    }
    public static G<T> operator +(G<T> G1, G<T> G2)
    {
        Console.WriteLine(""G<{0}> binary addition"", typeof(T));
        return new G<T>();
    }
}

public class Gen<T, U> where T : G<U>
{
    public static void TestLookupOnT(T obj, U val)
    {
        G<U> t = obj + ~obj;
    }
}

public class Test
{
    public static void Main()
    {
        Gen<G<int>, int>.TestLookupOnT(new G<int>(), 1);
    }
}
";
            string expected = @"G<System.Int32> unary negation
G<System.Int32> binary addition";

            CompileAndVerify(
                source: source,
                expectedOutput: expected);
        }

        [Fact, WorkItem(544539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544539"), WorkItem(544540, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544540")]
        public void NonShortCircuitBoolean()
        {
            var source = @"using System;
struct Program
{
    public static bool P()
    {
        Console.WriteLine(""P"");
        return true;
    }
    public static void Main(string[] args)
    {
        bool x = true | P();
        Console.WriteLine(P() & false);
    }
}";
            string expected = @"P
P
False";

            CompileAndVerify(
                source: source,
                expectedOutput: expected);
        }

        [Fact]
        public void EqualZero()
        {
            var text = @"
using System;
class MyClass
{
    public enum E1
    {
        A,
        B
    }

    public static void Main()
    {
        Test1((object)null, 0);
    }

    public static void Test1<T>(T x, E1 e) where T : class
    {
        if (x == null)
        {
            Console.WriteLine(!(x == null));
        }
        
        if (e == E1.A)
        {
            Console.WriteLine(!(e == E1.A));
        }
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: @"False
False
");

            comp.VerifyIL("MyClass.Test1<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  brtrue.s   IL_0016
  IL_0008:  ldarg.0
  IL_0009:  box        ""T""
  IL_000e:  ldnull
  IL_000f:  cgt.un
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldarg.1
  IL_0017:  brtrue.s   IL_0022
  IL_0019:  ldarg.1
  IL_001a:  ldc.i4.0
  IL_001b:  cgt.un
  IL_001d:  call       ""void System.Console.WriteLine(bool)""
  IL_0022:  ret
}
");
        }

        [Fact]
        public void EqualZeroUnoptimized()
        {
            var text = @"
using System;
class MyClass
{
    public enum E1
    {
        A,
        B
    }

    public static void Main()
    {
        Test1((object)null, 0);
    }

    public static void Test1<T>(T x, E1 e) where T : class
    {
        if (x == null)
        {
            Console.WriteLine(x == null);
        }
        
        if (e == E1.A)
        {
            Console.WriteLine(e == E1.A);
        }
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugExe, expectedOutput: @"True
True
");

            comp.VerifyIL("MyClass.Test1<T>", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (bool V_0,
                bool V_1)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  ldnull
  IL_0008:  ceq
  IL_000a:  stloc.0
 ~IL_000b:  ldloc.0
  IL_000c:  brfalse.s  IL_001f
 -IL_000e:  nop
 -IL_000f:  ldarg.0
  IL_0010:  box        ""T""
  IL_0015:  ldnull
  IL_0016:  ceq
  IL_0018:  call       ""void System.Console.WriteLine(bool)""
  IL_001d:  nop
 -IL_001e:  nop
 -IL_001f:  ldarg.1
  IL_0020:  ldc.i4.0
  IL_0021:  ceq
  IL_0023:  stloc.1
 ~IL_0024:  ldloc.1
  IL_0025:  brfalse.s  IL_0033
 -IL_0027:  nop
 -IL_0028:  ldarg.1
  IL_0029:  ldc.i4.0
  IL_002a:  ceq
  IL_002c:  call       ""void System.Console.WriteLine(bool)""
  IL_0031:  nop
 -IL_0032:  nop
 -IL_0033:  ret
}
", sequencePoints: "MyClass.Test1");
        }

        [WorkItem(543893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543893")]
        [Fact]
        public void EnumBitwiseComplement()
        {
            var text = @"
public class A
{
    enum E : ushort { one = 1, two = 2, four = 4 }
    public static void Main()
    {
checked {
        E e = E.one;
        e &= ~E.two;
        System.Console.WriteLine(e);
}
    }
}
";

            var comp = CompileAndVerify(text, expectedOutput: @"one");

            // Can't actually see an unchecked cast here since only constant values are emitted.
            comp.VerifyIL("A.Main", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4     0xfffd
  IL_0006:  and
  IL_0007:  box        ""A.E""
  IL_000c:  call       ""void System.Console.WriteLine(object)""
  IL_0011:  ret
}
");
            text = @"
public class A
{
    enum E : ushort { one = 1, two = 2, four = 4 }
    public static void Main()
    {
        checked {
            E e = E.one;
            int i = 5 + (int)~e;
            System.Console.WriteLine(i);
        }
    }
}
";

            comp = CompileAndVerify(text, expectedOutput: @"65539");

            // Can't actually see an unchecked cast here since only constant values are emitted.
            comp.VerifyIL("A.Main", @"
{ 
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (A.E V_0) //e
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.5
  IL_0003:  ldloc.0
  IL_0004:  not
  IL_0005:  conv.u2
  IL_0006:  add.ovf
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void EnumXor()
        {
            var text = @"
using System;

enum e : sbyte
{
    x = sbyte.MinValue,
    y = sbyte.MaxValue,
    z = 1
}

enum e1 : byte
{
    x = byte.MinValue,
    y = byte.MaxValue,
    z = 1
}

public static class Test
{
    public static void Main()
    {
        TestE();
        TestE1();
    }

    private static void TestE()
    {
        var x = e.x;
        var y = e.y;

        var z = x ^ y;
        System.Console.WriteLine((int)z);

        x ^= e.z;
        y ^= unchecked((e)(-1));
        x ^= e.z;
        y ^= unchecked((e)(-1));

        z = x ^ y;

        System.Console.WriteLine((int)z);
    }

    private static void TestE1()
    {
        var x = e1.x;
        var y = e1.y;

        var z = x ^ y;
        System.Console.WriteLine((int)z);

        x ^= e1.z;
        y ^= unchecked((e1)(-1));
        x ^= e1.z;
        y ^= unchecked((e1)(-1));

        z = x ^ y;

        System.Console.WriteLine((int)z);
    }
}

";

            var comp = CompileAndVerify(text, expectedOutput: @"
-1
-1
255
255
");

            comp.VerifyIL("Test.TestE()", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (e V_0) //y
  IL_0000:  ldc.i4.s   -128
  IL_0002:  ldc.i4.s   127
  IL_0004:  stloc.0
  IL_0005:  dup
  IL_0006:  ldloc.0
  IL_0007:  xor
  IL_0008:  call       ""void System.Console.WriteLine(int)""
  IL_000d:  ldc.i4.1
  IL_000e:  xor
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.m1
  IL_0011:  xor
  IL_0012:  stloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  xor
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.m1
  IL_0017:  xor
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  xor
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ret
}
");

            comp.VerifyIL("Test.TestE1()", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (e1 V_0) //y
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4     0xff
  IL_0006:  stloc.0
  IL_0007:  dup
  IL_0008:  ldloc.0
  IL_0009:  xor
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ldc.i4.1
  IL_0010:  xor
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4     0xff
  IL_0017:  xor
  IL_0018:  stloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  xor
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4     0xff
  IL_0021:  xor
  IL_0022:  stloc.0
  IL_0023:  ldloc.0
  IL_0024:  xor
  IL_0025:  call       ""void System.Console.WriteLine(int)""
  IL_002a:  ret
}
");
        }

        [WorkItem(544452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544452")]
        [Fact]
        public void LiftedByteEnumAddition()
        {
            var text =
@"using System;
public class Program
{
    private static bool ThrowsException(Action action)
    {
        try 
        {  
            action(); 
            return false;
        } 
        catch(Exception)
        {
            return true;
        }
    }
    private static void Test(bool b)
    {
        Console.Write(b ? 't' : 'f');
    }

    enum Color : byte { Red, Green, Blue }
    static void Main()
    {
        Color? c = Color.Blue;
        byte? b = byte.MaxValue - 1;
        Color? r = 0;
        Test(ThrowsException(()=>{ r = checked(c + b); }));
        Test(ThrowsException(()=>{ r = checked(c.Value + b.Value); }));
        Test(ThrowsException(()=>{ r = unchecked(c + b); }));
        Test(ThrowsException(()=>{ r = unchecked(c.Value + b.Value); }));
    }

    static void M(Color c, byte b)
    {
        Console.WriteLine(checked(c + b));
        Console.WriteLine(unchecked(c + b));
    }
}";

            string il = @"{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  conv.ovf.u1
  IL_0004:  box        ""Program.Color""
  IL_0009:  call       ""void System.Console.WriteLine(object)""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  add
  IL_0011:  conv.u1
  IL_0012:  box        ""Program.Color""
  IL_0017:  call       ""void System.Console.WriteLine(object)""
  IL_001c:  ret
}";

            var comp = CompileAndVerify(text, expectedOutput: @"ttff");

            comp.VerifyIL("Program.M", il);
        }

        [WorkItem(7091, "https://github.com/dotnet/roslyn/issues/7091")]
        [Fact]
        public void LiftedBitwiseOr()
        {
            var text =
@"using System;
public class Program
{
    static void Main()
    {
        var res = XX() | YY();
    }

    static bool XX()
    {
        Console.WriteLine (""XX"");
        return true;
    }

    static bool? YY()
    {
        Console.WriteLine(""YY"");
        return true;
    }
}
";
            var expectedOutput =
@"XX
YY";
            var comp = CompileAndVerify(text, expectedOutput: expectedOutput);
            string il = @"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (bool? V_0)
  IL_0000:  call       ""bool Program.XX()""
  IL_0005:  call       ""bool? Program.YY()""
  IL_000a:  stloc.0
  IL_000b:  pop
  IL_000c:  ret
}
";
            comp.VerifyIL("Program.Main", il);
        }

        [WorkItem(544943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544943")]
        [Fact]
        public void OptimizedXor()
        {
            var text = @"
using System;

class C
{
    void M(bool b)
    {
        Console.WriteLine(b ^ true);
        Console.WriteLine(b ^ false);
        Console.WriteLine(true ^ b);
        Console.WriteLine(false ^ b);
    }
}";

            //NOTE: all xors optimized away
            var comp = CompileAndVerify(text).VerifyIL("C.M", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  ceq
  IL_0004:  call       ""void System.Console.WriteLine(bool)""
  IL_0009:  ldarg.1
  IL_000a:  call       ""void System.Console.WriteLine(bool)""
  IL_000f:  ldarg.1
  IL_0010:  ldc.i4.0
  IL_0011:  ceq
  IL_0013:  call       ""void System.Console.WriteLine(bool)""
  IL_0018:  ldarg.1
  IL_0019:  call       ""void System.Console.WriteLine(bool)""
  IL_001e:  ret
}");
        }

        [WorkItem(544943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544943")]
        [Fact]
        public void XorMalformedTrue()
        {
            var text = @"
using System;
 
class C
{
    static void Main()
    {
        byte[] x = { 0xFF };
        bool[] y = { true };
        Buffer.BlockCopy(x, 0, y, 0, 1);
 
        Console.WriteLine(y[0]);
        Console.WriteLine(y[0] ^ true);
    }
}";

            var comp = CompileAndVerify(text, expectedOutput: @"True
False");
        }

        [WorkItem(539398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539398")]
        [WorkItem(1043494, "DevDiv")]
        [Fact(Skip = "1043494")]
        public void TestFloatNegativeZero()
        {
            var text = @"
using System;
 
class C
{
    static void Main()
    {
        Console.WriteLine(+0f == -0f);
        Console.WriteLine(1f / 0f);
        Console.WriteLine(1f / -0f);
        Console.WriteLine(-1f / 0f);
        Console.WriteLine(-1f / -0f);
        Console.WriteLine(1f / (1f * 0f));
        Console.WriteLine(1f / (1f * -0f));
        Console.WriteLine(1f / (-1f * 0f));
        Console.WriteLine(1f / (-1f * -0f));
    }
}";

            var comp = CompileAndVerify(text, expectedOutput: @"
True
Infinity
-Infinity
-Infinity
Infinity
Infinity
-Infinity
-Infinity
Infinity");
        }

        [WorkItem(539398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539398")]
        [WorkItem(1043494, "DevDiv")]
        [Fact(Skip = "1043494")]
        public void TestDoubleNegativeZero()
        {
            var text = @"
using System;
 
class C
{
    static void Main()
    {
        Console.WriteLine(+0d == -0d);
        Console.WriteLine(1d / 0d);
        Console.WriteLine(1d / -0d);
        Console.WriteLine(-1d / 0d);
        Console.WriteLine(-1d / -0d);
        Console.WriteLine(1d / (1d * 0d));
        Console.WriteLine(1d / (1d * -0d));
        Console.WriteLine(1d / (-1d * 0d));
        Console.WriteLine(1d / (-1d * -0d));
    }
}";

            var comp = CompileAndVerify(text, expectedOutput: @"
True
Infinity
-Infinity
-Infinity
Infinity
Infinity
-Infinity
-Infinity
Infinity");
        }

        // NOTE: decimal doesn't have infinity, so we convert to double.
        [WorkItem(539398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539398")]
        [WorkItem(1043494, "DevDiv")]
        [Fact(Skip = "1043494")]
        public void TestDecimalNegativeZero()
        {
            var text = @"
using System;
 
class C
{
    static void Main()
    {
        Console.WriteLine(+0m == -0m);
        Console.WriteLine(1d / (double)(0m));
        Console.WriteLine(1d / (double)(-0m));
        Console.WriteLine(-1d / (double)(0m));
        Console.WriteLine(-1d / (double)(-0m));
        Console.WriteLine(1d / (double)(1m * 0m));
        Console.WriteLine(1d / (double)(1m * -0m));
        Console.WriteLine(1d / (double)(-1m * 0m));
        Console.WriteLine(1d / (double)(-1m * -0m));
    }
}";

            var comp = CompileAndVerify(text, expectedOutput: @"
True
Infinity
-Infinity
-Infinity
Infinity
Infinity
-Infinity
-Infinity
Infinity");
        }

        [WorkItem(545239, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545239")]
        [Fact()]
        public void IncrementPropertyOfTypeParameterReturnValue()
        {
            var text = @"
public interface I
{
    int IntPropI { get; set; }
}

struct S1 : I
{
    public int x;

    public int IntPropI
    {
        get
        {
            x ++;
            return x;
        }
        set
        {
            x ++;
            System.Console.WriteLine(x);
        }
    }
}

public class Test
{

    public static void Main()
    {
        S1 s = new S1();
        TestINop(s);
    }

    public static T Nop<T>(T t) { return t; }
    public static void TestINop<T>(T t) where T : I
    {
        Nop(t).IntPropI++;
        Nop(t).IntPropI++;
    }
}
";

            CompileAndVerify(text, expectedOutput: @"
2
2").VerifyIL("Test.TestINop<T>", @"
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (int V_0,
  T V_1,
  T V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T Test.Nop<T>(T)""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  dup
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""int I.IntPropI.get""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""void I.IntPropI.set""
  IL_0024:  ldarg.0
  IL_0025:  call       ""T Test.Nop<T>(T)""
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  dup
  IL_002e:  constrained. ""T""
  IL_0034:  callvirt   ""int I.IntPropI.get""
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  ldc.i4.1
  IL_003c:  add
  IL_003d:  constrained. ""T""
  IL_0043:  callvirt   ""void I.IntPropI.set""
  IL_0048:  ret
}
");
        }

        [WorkItem(546750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546750")]
        [Fact()]
        public void IncrementStructFieldWithReceiverThis()
        {
            var text = @"
struct S
{
    int x;

    void Test()
    {
        x++;
    }
}
";

            // NOTE: don't need a ref local in this case.
            CompileAndVerify(text).VerifyIL("S.Test", @"
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int S.x""
  IL_0007:  ldc.i4.1
  IL_0008:  add
  IL_0009:  stfld      ""int S.x""
  IL_000e:  ret
}
");
        }



        [Fact]
        public void TestTernary_InterfaceRegression1a()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    private static bool C() { return true;}

    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b = new List<int>();

        IEnumerable<int> c = C()? b : a;
        Foo(c);
    }

    static void Foo<T>(T x)
    {
        System.Console.WriteLine(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  1
  .locals init (int[] V_0, //a
  System.Collections.Generic.IEnumerable<int> V_1, //b
  System.Collections.Generic.IEnumerable<int> V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  stloc.1
  IL_000d:  call       ""bool Test.C()""
  IL_0012:  brtrue.s   IL_0019
  IL_0014:  ldloc.0
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  br.s       IL_001a
  IL_0019:  ldloc.1
  IL_001a:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void TestTernary_InterfaceRegression1b()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    private static bool C() { return true;}

    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b = null;

        IEnumerable<int> c = C()? (b = (IEnumerable<int>)new List<int>()) : a;
        Foo(c);
        Foo(b);
    }

    static void Foo<T>(T x)
    {
        System.Console.Write(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int[] V_0, //a
  System.Collections.Generic.IEnumerable<int> V_1, //b
  System.Collections.Generic.IEnumerable<int> V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  ldnull
  IL_0008:  stloc.1
  IL_0009:  call       ""bool Test.C()""
  IL_000e:  brtrue.s   IL_0015
  IL_0010:  ldloc.0
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  br.s       IL_001e
  IL_0015:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stloc.2
  IL_001d:  ldloc.2
  IL_001e:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_0023:  ldloc.1
  IL_0024:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void TestTernary_InterfaceRegression1c()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        IEnumerable<int> b = new List<int>();

        Foo(b, b != null ? b : a);
    }

    static void Foo<T, U>(T x, U y)
    {
        System.Console.Write(typeof(T));
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "System.Collections.Generic.IEnumerable`1[System.Int32]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int[] V_0, //a
                System.Collections.Generic.IEnumerable<int> V_1, //b
                System.Collections.Generic.IEnumerable<int> V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.1
  IL_000e:  ldloc.1
  IL_000f:  brtrue.s   IL_0016
  IL_0011:  ldloc.0
  IL_0012:  stloc.2
  IL_0013:  ldloc.2
  IL_0014:  br.s       IL_0017
  IL_0016:  ldloc.1
  IL_0017:  call       ""void Test.Foo<System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>>(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)""
  IL_001c:  ret
}
");
        }


        [Fact]
        public void TestTernary_InterfaceRegression2()
        {
            var source = @"
public interface IA { }
public interface IB { int f(); }
public class AB1 : IA, IB { public int f() { return 42; } }
public class AB2 : IA, IB { public int f() { return 1; } }

class MainClass
{
    private static bool C() { return true;}

    public static void g(AB1 ab1)
    {
        (C()? (IB)ab1 : (IB)new AB2()).f();
    }
}";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("MainClass.g", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (IB V_0)
  IL_0000:  call       ""bool MainClass.C()""
  IL_0005:  brtrue.s   IL_0010
  IL_0007:  newobj     ""AB2..ctor()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  br.s       IL_0013
  IL_0010:  ldarg.0
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  callvirt   ""int IB.f()""
  IL_0018:  pop
  IL_0019:  ret
}");
        }

        [Fact]
        public void TestTernary_FuncVariance()
        {
            var source = @"
using System;
using System.Collections.Generic;

    class Program
    {
        private static bool C() { return true;}

        static void Main(string[] args)
        {
            Func<Exception[]> f1 = null;

            Func<IEnumerable<object>> f2 = null;

            var oo = C()? f1 : f2;
            Console.WriteLine(oo);

            oo = C()? f2 : f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (System.Func<System.Exception[]> V_0, //f1
  System.Func<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  System.Func<System.Collections.Generic.IEnumerable<object>> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_000e
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0011
  IL_000e:  ldloc.0
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  call       ""bool Program.C()""
  IL_001b:  brtrue.s   IL_0022
  IL_001d:  ldloc.0
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.1
  IL_0023:  call       ""void System.Console.WriteLine(object)""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void TestTernary_FuncVariance01()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

    class Program
    {
        private static bool C() { return true;}

        static void Main(string[] args)
        {
            Func<Exception[]> f1 = null;

            Func<IEnumerable<object>> f2 = null;

            var oo = C()? (Func<IEnumerable>)f1 : (Func<IEnumerable>)f2;
            Console.WriteLine(oo);

            oo = C()? (Func<IEnumerable>)f2 : (Func<IEnumerable>)f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (System.Func<System.Exception[]> V_0, //f1
  System.Func<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  System.Func<System.Collections.IEnumerable> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_0010
  IL_000b:  ldloc.1
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  br.s       IL_0013
  IL_0010:  ldloc.0
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  call       ""void System.Console.WriteLine(object)""
  IL_0018:  call       ""bool Program.C()""
  IL_001d:  brtrue.s   IL_0024
  IL_001f:  ldloc.0
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0027
  IL_0024:  ldloc.1
  IL_0025:  stloc.2
  IL_0026:  ldloc.2
  IL_0027:  call       ""void System.Console.WriteLine(object)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void TestTernary_InterfaceVariance()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

    class Program
    {
        private static bool C() { return true;}

        interface CoInter<out T>
        {
        }

        static void Main(string[] args)
        {
            CoInter<Exception[]> f1 = null;

            CoInter<IEnumerable<object>> f2 = null;

            var oo = C()? f1 : f2;
            Console.WriteLine(oo);

            oo = C()? f2 : f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (Program.CoInter<System.Exception[]> V_0, //f1
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_000e
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0011
  IL_000e:  ldloc.0
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  call       ""bool Program.C()""
  IL_001b:  brtrue.s   IL_0022
  IL_001d:  ldloc.0
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.1
  IL_0023:  call       ""void System.Console.WriteLine(object)""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void TestTernary_InterfaceVarianceA()
        {
            var source = @"
using System;
using System.Security;

[assembly: SecurityTransparent()]

    class Program
    {
        private static bool C() { return true;}

        interface CoInter<out T>
        {
        }

        static void Main(string[] args)
        {
            CoInter<Exception> f1 = null;

            CoInter<object> f2 = null;

            var oo = C()? f1 : f2;
            Console.WriteLine(oo);

            oo = C()? f2 : f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(new string[] { source }, additionalRefs: new[] { SystemCoreRef }, expectedOutput: @"");
            //            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (Program.CoInter<System.Exception> V_0, //f1
  Program.CoInter<object> V_1, //f2
  Program.CoInter<object> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_000e
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_0011
  IL_000e:  ldloc.0
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  call       ""void System.Console.WriteLine(object)""
  IL_0016:  call       ""bool Program.C()""
  IL_001b:  brtrue.s   IL_0022
  IL_001d:  ldloc.0
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.1
  IL_0023:  call       ""void System.Console.WriteLine(object)""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void TestTernary_InterfaceVariance01()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

    class Program
    {
        private static bool C() { return true;}

        interface CoInter<out T>
        {
        }

        static void Main(string[] args)
        {
            CoInter<Exception[]> f1 = null;

            CoInter<IEnumerable<object>> f2 = null;

            var oo = C()? (CoInter<IEnumerable>)f1 : (CoInter<IEnumerable>)f2;
            Console.WriteLine(oo);

            oo = C()? (CoInter<IEnumerable>)f2 : (CoInter<IEnumerable>)f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Program.CoInter<System.Exception[]> V_0, //f1
  Program.CoInter<System.Collections.Generic.IEnumerable<object>> V_1, //f2
  Program.CoInter<System.Collections.IEnumerable> V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_0010
  IL_000b:  ldloc.1
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  br.s       IL_0013
  IL_0010:  ldloc.0
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  call       ""void System.Console.WriteLine(object)""
  IL_0018:  call       ""bool Program.C()""
  IL_001d:  brtrue.s   IL_0024
  IL_001f:  ldloc.0
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0027
  IL_0024:  ldloc.1
  IL_0025:  stloc.2
  IL_0026:  ldloc.2
  IL_0027:  call       ""void System.Console.WriteLine(object)""
  IL_002c:  ret
}
");
        }

        [Fact]
        public void TestTernary_ToBase()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

    class Program
    {
        private static bool C() { return true;}

        static void Main(string[] args)
        {
            Exception[] f1 = null;

            IEnumerable<object> f2 = null;

            var oo = C()? (object)f1 : (object)f2;
            Console.WriteLine(oo);

            oo = C()? (object)f2 : (object)f1;
            Console.WriteLine(oo);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (System.Exception[] V_0, //f1
  System.Collections.Generic.IEnumerable<object> V_1) //f2
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  call       ""bool Program.C()""
  IL_0009:  brtrue.s   IL_000e
  IL_000b:  ldloc.1
  IL_000c:  br.s       IL_000f
  IL_000e:  ldloc.0
  IL_000f:  call       ""void System.Console.WriteLine(object)""
  IL_0014:  call       ""bool Program.C()""
  IL_0019:  brtrue.s   IL_001e
  IL_001b:  ldloc.0
  IL_001c:  br.s       IL_001f
  IL_001e:  ldloc.1
  IL_001f:  call       ""void System.Console.WriteLine(object)""
  IL_0024:  ret
}
");
        }

        [WorkItem(634407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634407")]
        [Fact]
        public void TestTernary_Null()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Security;

[assembly: SecurityTransparent()]

    class Program
    {
        private static bool C() { return true;}

        static void Main(string[] args)
        {
            Exception[] f1 = null;

            var oo = C()? f1 : null as IEnumerable<object>;
            Console.WriteLine(oo);

            var oo1 = C()? null as IEnumerable<object> : f1 as IEnumerable<Exception>;
            Console.WriteLine(oo1);
        }
    }
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.Exception[] V_0, //f1
  System.Collections.Generic.IEnumerable<object> V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  call       ""bool Program.C()""
  IL_0007:  brtrue.s   IL_000c
  IL_0009:  ldnull
  IL_000a:  br.s       IL_000f
  IL_000c:  ldloc.0
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  call       ""void System.Console.WriteLine(object)""
  IL_0014:  call       ""bool Program.C()""
  IL_0019:  brtrue.s   IL_0020
  IL_001b:  ldloc.0
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  br.s       IL_0021
  IL_0020:  ldnull
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}");
        }

        [WorkItem(634406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634406")]
        [Fact]
        public void TestBinary_Implicit()
        {
            var source = @"
using System;
using System.Security;

[assembly: SecurityTransparent()]

class Program
{
    private static bool C() { return true; }

    class cls1
    {
        public static implicit operator int(cls1 from)
        {
            return 42;
        }
    }

    static void Main(string[] args)
    {
        cls1 f1 = null;

        var oo = f1 ?? 33;
        Console.WriteLine(oo);
    }
}
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Program.cls1 V_0)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_0009
  IL_0005:  ldc.i4.s   33
  IL_0007:  br.s       IL_000f
  IL_0009:  ldloc.0
  IL_000a:  call       ""int Program.cls1.op_Implicit(Program.cls1)""
  IL_000f:  call       ""void System.Console.WriteLine(int)""
  IL_0014:  ret
}
");
        }

        [WorkItem(656807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/656807")]
        [Fact]
        public void DelegateEqualsNull()
        {
            var source = @"
public delegate int D(int x);
public class Program
{
    public static D d1 = null;
    public static int r1;
    public static bool r2;
    public static void Main(string[] args)
    {
        if (d1 == null) { r1 = 1; }
        if (d1 != null) { r1 = 2; }
        r2 = (d1 == null);
        r2 = (d1 != null);
    }
}
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  IL_0000:  ldsfld     ""D Program.d1""
  IL_0005:  brtrue.s   IL_000d
  IL_0007:  ldc.i4.1
  IL_0008:  stsfld     ""int Program.r1""
  IL_000d:  ldsfld     ""D Program.d1""
  IL_0012:  brfalse.s  IL_001a
  IL_0014:  ldc.i4.2
  IL_0015:  stsfld     ""int Program.r1""
  IL_001a:  ldsfld     ""D Program.d1""
  IL_001f:  ldnull
  IL_0020:  ceq
  IL_0022:  stsfld     ""bool Program.r2""
  IL_0027:  ldsfld     ""D Program.d1""
  IL_002c:  ldnull
  IL_002d:  cgt.un
  IL_002f:  stsfld     ""bool Program.r2""
  IL_0034:  ret
}");
        }

        [WorkItem(717072, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/717072")]
        [Fact]
        public void DecimalOperators()
        {
            var source = @"
class Program
{
    static void Main()
    {
        decimal d1 = 1.0201m;
        if (d1 == 10201M)
        {
        }

        if (d1 != 10201M)
        {
        }

        decimal d2 = d1 + d1;
        decimal d3 = d1 - d1;
        decimal d4 = d1 * d1;
        decimal d5 = d1 / d1;
        decimal d6 = d1 % d1;
        decimal d7 = d1 ++;
        decimal d8 = d1--;
        decimal d9 = -d1;
        decimal d10 = +d1;
    }
}
";

            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size      116 (0x74)
  .maxstack  6
  .locals init (decimal V_0) //d1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4     0x27d9
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.4
  IL_000b:  call       ""decimal..ctor(int, int, int, bool, byte)""
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4     0x27d9
  IL_0016:  newobj     ""decimal..ctor(int)""
  IL_001b:  call       ""bool decimal.op_Equality(decimal, decimal)""
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4     0x27d9
  IL_0027:  newobj     ""decimal..ctor(int)""
  IL_002c:  call       ""bool decimal.op_Inequality(decimal, decimal)""
  IL_0031:  pop
  IL_0032:  ldloc.0
  IL_0033:  ldloc.0
  IL_0034:  call       ""decimal decimal.op_Addition(decimal, decimal)""
  IL_0039:  pop
  IL_003a:  ldloc.0
  IL_003b:  ldloc.0
  IL_003c:  call       ""decimal decimal.op_Subtraction(decimal, decimal)""
  IL_0041:  pop
  IL_0042:  ldloc.0
  IL_0043:  ldloc.0
  IL_0044:  call       ""decimal decimal.op_Multiply(decimal, decimal)""
  IL_0049:  pop
  IL_004a:  ldloc.0
  IL_004b:  ldloc.0
  IL_004c:  call       ""decimal decimal.op_Division(decimal, decimal)""
  IL_0051:  pop
  IL_0052:  ldloc.0
  IL_0053:  ldloc.0
  IL_0054:  call       ""decimal decimal.op_Modulus(decimal, decimal)""
  IL_0059:  pop
  IL_005a:  ldloc.0
  IL_005b:  dup
  IL_005c:  call       ""decimal decimal.op_Increment(decimal)""
  IL_0061:  stloc.0
  IL_0062:  pop
  IL_0063:  ldloc.0
  IL_0064:  dup
  IL_0065:  call       ""decimal decimal.op_Decrement(decimal)""
  IL_006a:  stloc.0
  IL_006b:  pop
  IL_006c:  ldloc.0
  IL_006d:  call       ""decimal decimal.op_UnaryNegation(decimal)""
  IL_0072:  pop
  IL_0073:  ret
}
");
        }

        [WorkItem(732269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/732269")]
        [Fact]
        public void NullCoalesce()
        {
            var source = @"
class Program
{
    static int Main(int? x, int y) { return x ?? y; }
}
";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool int?.HasValue.get""
  IL_0009:  brtrue.s   IL_000d
  IL_000b:  ldarg.1
  IL_000c:  ret
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""int int?.GetValueOrDefault()""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void TestCompoundOnAfieldOfGeneric()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var x = new c0();
        test<c0>.Repro1(x);
        System.Console.WriteLine(x.x);

        test<c0>.Repro2(x);
        System.Console.WriteLine(x.x);
    }
}

class c0
{
    public int x;

    public int P1
    {
        get { return x; }
        set { x = value; }
    }

    public int this[int i]
    {
        get { return x; }
        set { x = value; }
    }

    public static int Foo(c0 arg)
    {
        return 1;
    }

    public int Foo()
    {
        return 1;
    }
}

class test<T> where T : c0
{
    public static void Repro1(T arg)
    {
        arg.x += 1;
        arg.P1 += 1;
        arg[1] += 1;
    }

    public static void Repro2(T arg)
    {
        arg.x = c0.Foo(arg);
        arg.x = arg.Foo();
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: @"3
1");
            compilation.VerifyIL("test<T>.Repro1(T)", @"
{
  // Code size       80 (0x50)
  .maxstack  4
  .locals init (T& V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  dup
  IL_0007:  ldfld      ""int c0.x""
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  stfld      ""int c0.x""
  IL_0013:  ldarga.s   V_0
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""int c0.P1.get""
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  constrained. ""T""
  IL_002b:  callvirt   ""void c0.P1.set""
  IL_0030:  ldarga.s   V_0
  IL_0032:  stloc.0
  IL_0033:  ldloc.0
  IL_0034:  ldc.i4.1
  IL_0035:  ldloc.0
  IL_0036:  ldc.i4.1
  IL_0037:  constrained. ""T""
  IL_003d:  callvirt   ""int c0.this[int].get""
  IL_0042:  ldc.i4.1
  IL_0043:  add
  IL_0044:  constrained. ""T""
  IL_004a:  callvirt   ""void c0.this[int].set""
  IL_004f:  ret
}
").VerifyIL("test<T>.Repro2(T)", @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldarg.0
  IL_0007:  box        ""T""
  IL_000c:  call       ""int c0.Foo(c0)""
  IL_0011:  stfld      ""int c0.x""
  IL_0016:  ldarg.0
  IL_0017:  box        ""T""
  IL_001c:  ldarg.0
  IL_001d:  box        ""T""
  IL_0022:  callvirt   ""int c0.Foo()""
  IL_0027:  stfld      ""int c0.x""
  IL_002c:  ret
}
");
        }

        [Fact()]
        [WorkItem(4828, "https://github.com/dotnet/roslyn/issues/4828")]
        public void OptimizeOutLocals_01()
        {
            const string source = @"
    class Program
    {
        static void Main(string[] args)
        {
            int a = 0;
            int b = a + a / 1;
        }
    }";
            var result = CompileAndVerify(source, options: TestOptions.ReleaseExe);

            result.VerifyIL("Program.Main",
@"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  div
  IL_0003:  pop
  IL_0004:  ret
}
");
        }

        [Fact, WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_01()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
        var f = new long[4096];
        for (int i = 0; i < 4096 ; i++)
        {
            f[i] = 4096 - i;
        }

        System.Console.WriteLine((Calculate1(f) == Calculate2(f)) ? ""True"" : ""False"");
    }

    public static long Calculate1(long[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_01() };" + @"
    }

    public static long Calculate2(long[] f)
    {
        long result = 0;
        int i;

        for (i = 0; i < f.Length; i++)
        {
            result+=(i + 1)*f[i];
        }

        return result + (i + 1);
    }
}
";

            var result = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "True");
        }

        private static string BuildSequenceOfBinaryExpressions_01(int count = 4096)
        {
            var builder = new System.Text.StringBuilder();
            int i;
            for (i = 0; i < count ; i++)
            {
                builder.Append(i + 1);
                builder.Append(" * ");
                builder.Append("f[");
                builder.Append(i);
                builder.Append("] + ");
            }

            builder.Append(i + 1);

            return builder.ToString();
        }

        [Fact, WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_02()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
        var f = new long[4096];
        for (int i = 0; i < 4096 ; i++)
        {
            f[i] = 4096 - i;
        }

        System.Console.WriteLine(Calculate(f));
    }

    public static double Calculate(long[] f)
    {
" + $"        return checked({ BuildSequenceOfBinaryExpressions_01() });" + @"
    }
}
";

            var result = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "11461640193");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6077")]
        [WorkItem(6077, "https://github.com/dotnet/roslyn/issues/6077")]
        [WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_03()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
    }

    public static bool Calculate(bool[] a, bool[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_03() };" + @"
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
    // (10,16): error CS8078: An expression is too long or complex to compile
    //         return a[0] && f[0] || a[1] && f[1] || a[2] && f[2] || ...
    Diagnostic(ErrorCode.ERR_InsufficientStack, "a").WithLocation(10, 16)
                );
        }

        private static string BuildSequenceOfBinaryExpressions_03()
        {
            var builder = new System.Text.StringBuilder();
            int i;
            for (i = 0; i < 8192; i++)
            {
                builder.Append("a[");
                builder.Append(i);
                builder.Append("]");
                builder.Append(" && ");
                builder.Append("f[");
                builder.Append(i);
                builder.Append("] || ");
            }

            builder.Append("a[");
            builder.Append(i);
            builder.Append("]");

            return builder.ToString();
        }

        [Fact, WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_04()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
        var f = new float?[4096];
        for (int i = 0; i < 4096 ; i++)
        {
            f[i] = 4096 - i;
        }

        System.Console.WriteLine(Calculate(f));
    }

    public static double? Calculate(float?[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_01() };" + @"
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
    // (17,16): error CS8078: An expression is too long or complex to compile
    //         return 1 * f[0] + 2 * f[1] + 3 * f[2] + 4 * f[3] + ...
    Diagnostic(ErrorCode.ERR_InsufficientStack, "1").WithLocation(17, 16)
                );
        }

        [Fact, WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_05()
        {
            int count = 50;
            var source =
@"
class Test
{ 
    static void Main()
    {
        Test1();
        Test2();
    }

    static void Test1()
    {
        var f = new double?[" + $"{count}" + @"];
        for (int i = 0; i < " + $"{count}" + @" ; i++)
        {
            f[i] = 4096 - i;
        }

        System.Console.WriteLine(Calculate(f));
    }

    public static double? Calculate(double?[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_01(count) };" + @"
    }

    static void Test2()
    {
        var f = new double[" + $"{count}" + @"];
        for (int i = 0; i < " + $"{count}" + @" ; i++)
        {
            f[i] = 4096 - i;
        }

        System.Console.WriteLine(Calculate(f));
    }

    public static double Calculate(double[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_01(count) };" + @"
    }
}
";

            var result = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"5180801
5180801");
        }

        [Fact, WorkItem(5395, "https://github.com/dotnet/roslyn/issues/5395")]
        public void EmitSequenceOfBinaryExpressions_06()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
    }

    public static bool Calculate(S1[] a, S1[] f)
    {
" + $"        return { BuildSequenceOfBinaryExpressions_03() };" + @"
    }
}

struct S1
{
    public static S1 operator & (S1 x, S1 y)
    {
        return new S1();
    }

    public static S1 operator |(S1 x, S1 y)
    {
        return new S1();
    }

    public static bool operator true(S1 x)
    {
        return true;
    }

    public static bool operator false(S1 x)
    {
        return true;
    }

    public static implicit operator bool (S1 x)
    {
        return true;
    } 
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics(
    // (10,16): error CS8078: An expression is too long or complex to compile
    //         return a[0] && f[0] || a[1] && f[1] || a[2] && f[2] || ...
    Diagnostic(ErrorCode.ERR_InsufficientStack, "a").WithLocation(10, 16)
                );
        }

        [Fact, WorkItem(7262, "https://github.com/dotnet/roslyn/issues/7262")]
        public void TruncatePrecisionOnCast()
        {
            var source =
@"
class Test
{ 
    static void Main()
    {
        float temp1 = (float)(23334800f / 5.5f);
        System.Console.WriteLine((int)temp1);

        const float temp2 = (float)(23334800f / 5.5f);
        System.Console.WriteLine((int)temp2);

        System.Console.WriteLine((int)(23334800f / 5.5f));
}
}
";
            var expectedOutput =
@"4242691
4242691
4242691";
            var result = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: expectedOutput);
        }
    }
}
