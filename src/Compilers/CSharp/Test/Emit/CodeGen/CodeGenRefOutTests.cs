// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenRefOutTests : CSharpTestBase
    {
        [Fact]
        public void TestOutParamSignature()
        {
            var source = @"
class C
{
    void M(out int x)
    {
        x = 0;
    }
}";
            CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("C", "M", ".method private hidebysig instance System.Void M([out] System.Int32& x) cil managed")
            });
        }

        [Fact]
        public void TestRefParamSignature()
        {
            var source = @"
class C
{
    void M(ref int x)
    {
    }
}";
            CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("C", "M", ".method private hidebysig instance System.Void M(System.Int32& x) cil managed")
            });
        }

        [Fact]
        public void TestOneReferenceMultipleParameters()
        {
            var source = @"
class C
{
    static void Main()
    {
        int z = 0;
        Test(ref z, out z);
        System.Console.WriteLine(z);
    }

    static void Test(ref int x, out int y)
    {
        x = 1;
        y = 2;
    }
}";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void TestReferenceParameterOrder()
        {
            var source = @"
public class Test
{
    static int[] array = new int[1];

    public static void Main(string[] args)
    {
        // Named parameters are in reversed order
        // Arguments have side effects
        // Arguments refer to the same array element
        Goo(y: out GetArray(""A"")[GetIndex(""B"")], x: ref GetArray(""C"")[GetIndex(""D"")]);
        System.Console.WriteLine(array[0]);
    }

    static void Goo(ref int x, out int y)
    {
        x = 1;
        y = 2;
    }

    static int GetIndex(string msg)
    {
        System.Console.WriteLine(""Index {0}"", msg);
        return 0;
    }

    static int[] GetArray(string msg)
    {
        System.Console.WriteLine(""Array {0}"", msg);
        return array;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
Array A
Index B
Array C
Index D
2");
        }

        [Fact]
        public void TestPassMutableStructByReference()
        {
            var source = @"
class C
{
    static void Main()
    {
        MutableStruct s1 = new MutableStruct();
        s1.Dump();
        ByRef(ref s1, 2);
        s1.Dump();

        System.Console.WriteLine();

        MutableStruct s2 = new MutableStruct();
        s2.Dump();
        ByVal(s2, 2);
        s2.Dump();
    }

    static void ByRef(ref MutableStruct s, int depth)
    {
        if (depth <= 0)
        {
            s.Flag();
        }
        else
        {
            s.Dump();
            ByRef(ref s, depth - 1);
            s.Dump();
        }
    }

    static void ByVal(MutableStruct s, int depth)
    {
        if (depth <= 0)
        {
            s.Flag();
        }
        else
        {
            s.Dump();
            ByVal(s, depth - 1);
            s.Dump();
        }
    }
}

struct MutableStruct
{
    private bool flagged;

    public void Flag()
    {
        this.flagged = true;
    }

    public void Dump()
    {
        System.Console.WriteLine(flagged ? ""Flagged"" : ""Unflagged"");
    }
}";
            CompileAndVerify(source, expectedOutput: @"
Unflagged
Unflagged
Unflagged
Flagged
Flagged
Flagged

Unflagged
Unflagged
Unflagged
Unflagged
Unflagged
Unflagged");
        }

        [Fact]
        public void TestPassFieldByReference()
        {
            var source = @"
class C
{
    int field;
    int[] arrayField = new int[1];

    static int staticField;
    static int[] staticArrayField = new int[1];

    static void Main()
    {
        C c = new C();

        System.Console.WriteLine(c.field);
        TestRef(ref c.field);
        System.Console.WriteLine(c.field);

        System.Console.WriteLine(c.arrayField[0]);
        TestRef(ref c.arrayField[0]);
        System.Console.WriteLine(c.arrayField[0]);

        System.Console.WriteLine(C.staticField);
        TestRef(ref C.staticField);
        System.Console.WriteLine(C.staticField);

        System.Console.WriteLine(C.staticArrayField[0]);
        TestRef(ref C.staticArrayField[0]);
        System.Console.WriteLine(C.staticArrayField[0]);
    }

    static void TestRef(ref int x)
    {
        x++;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
0
1
0
1
0
1
0
1");
        }

        [Fact]
        public void TestSetFieldViaOutParameter()
        {
            var source = @"
class C
{
    int field;
    int[] arrayField = new int[1];

    static int staticField;
    static int[] staticArrayField = new int[1];

    static void Main()
    {
        C c = new C();

        System.Console.WriteLine(c.field);
        TestOut(out c.field);
        System.Console.WriteLine(c.field);

        System.Console.WriteLine(c.arrayField[0]);
        TestOut(out c.arrayField[0]);
        System.Console.WriteLine(c.arrayField[0]);

        System.Console.WriteLine(C.staticField);
        TestOut(out C.staticField);
        System.Console.WriteLine(C.staticField);

        System.Console.WriteLine(C.staticArrayField[0]);
        TestOut(out C.staticArrayField[0]);
        System.Console.WriteLine(C.staticArrayField[0]);
    }

    static void TestOut(out int x)
    {
        x = 1;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
0
1
0
1
0
1
0
1");
        }

        [WorkItem(543521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543521")]
        [Fact()]
        public void TestConstructorWithOutParameter()
        {
            CompileAndVerify(@"
class Class1
{
	Class1(out bool outParam)
	{
		outParam = true;
	}
	static void Main()
	{
		var b = false;
		var c1 = new Class1(out b);
	}
}");
        }

        [WorkItem(24014, "https://github.com/dotnet/roslyn/issues/24014")]
        [Fact]
        public void RefExtensionMethods_OutParam()
        {
            var code = @"
using System;
public class C
{
    public static void Main()
    {

        var inst = new S1();

        int orig;

        var result = inst.Mutate(out orig);

        System.Console.Write(orig);
        System.Console.Write(inst.x);
    }
}

public static class S1_Ex
{
    public static bool Mutate(ref this S1 instance, out int orig)
    {
        orig = instance.x;
        instance.x = 42;

        return true;
    }
}

public struct S1
{
    public int x;
}
";

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "042");

            verifier.VerifyIL("C.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (S1 V_0, //inst
                int V_1) //orig
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S1""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""bool S1_Ex.Mutate(ref S1, out int)""
  IL_0011:  pop
  IL_0012:  ldloc.1
  IL_0013:  call       ""void System.Console.Write(int)""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""int S1.x""
  IL_001e:  call       ""void System.Console.Write(int)""
  IL_0023:  ret
}");

        }

        [WorkItem(24014, "https://github.com/dotnet/roslyn/issues/24014")]
        [Fact]
        public void OutParamAndOptional()
        {
            var code = @"
using System;
public class C
{
    public static C cc => new C();
    readonly int x;
    readonly int y;

    public static void Main()
    {
        var v = new C(1);
        System.Console.WriteLine('Q');
    }

    private C()
    {
    }

    private C(int x)
    {
        var c = C.cc.Test(1, this, out x, out y);
    }

    public C Test(object arg1, C arg2, out int i1, out int i2, object opt = null)
    {
        i1 = 1;
        i2 = 2;

        return arg2;
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "Q");

            verifier.VerifyIL("C..ctor(int)", @"
{
  // Code size       34 (0x22)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  call       ""C C.cc.get""
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldarg.0
  IL_0012:  ldarga.s   V_1
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""int C.y""
  IL_001a:  ldnull
  IL_001b:  callvirt   ""C C.Test(object, C, out int, out int, object)""
  IL_0020:  pop
  IL_0021:  ret
}");
        }

        [WorkItem(24014, "https://github.com/dotnet/roslyn/issues/24014")]
        [Fact]
        public void OutParamAndOptionalNested()
        {
            var code = @"
using System;
public class C
{
    public static C cc => new C();

    readonly int y;

    public static void Main()
    {
        var v = new C(1);
        System.Console.WriteLine('Q');
    }

    private C()
    {
    }

    private C(int x)
    {
        var captured = 2;

        C Test(object arg1, C arg2, out int i1, out int i2, object opt = null)
        {
            i1 = 1;
            i2 = captured++;

            return arg2;
        }

        var c = Test(1, this, out x, out y);
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: "Q");

            verifier.VerifyIL("C..ctor(int)", @"
{
  // Code size       39 (0x27)
  .maxstack  6
  .locals init (C.<>c__DisplayClass5_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.2
  IL_0009:  stfld      ""int C.<>c__DisplayClass5_0.captured""
  IL_000e:  ldc.i4.1
  IL_000f:  box        ""int""
  IL_0014:  ldarg.0
  IL_0015:  ldarga.s   V_1
  IL_0017:  ldarg.0
  IL_0018:  ldflda     ""int C.y""
  IL_001d:  ldnull
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       ""C C.<.ctor>g__Test|5_0(object, C, out int, out int, object, ref C.<>c__DisplayClass5_0)""
  IL_0025:  pop
  IL_0026:  ret
}");
        }
    }
}
