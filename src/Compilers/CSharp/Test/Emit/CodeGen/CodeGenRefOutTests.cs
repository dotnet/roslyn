// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerIndirection()
        {
            var code = @"
using System;

unsafe
{
    M(ref *(int*)0);
    void M(ref int i) => Console.WriteLine(""run"");
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_0007:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  conv.i
  IL_0003:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_0008:  nop
  IL_0009:  nop
  IL_000a:  nop
  IL_000b:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestOutOnPointerIndirection()
        {
            var code = @"
using System;

unsafe
{
    try
    {
        M(out *(int*)0);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }

    void M(out int i)
    {
        throw new Exception(""run"");
    }
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size       22 (0x16)
  .maxstack  1
  .try
  {
    IL_0000:  ldc.i4.0
    IL_0001:  conv.i
    IL_0002:  call       ""void Program.<<Main>$>g__M|0_0(out int)""
    IL_0007:  leave.s    IL_0015
  }
  catch System.Exception
  {
    IL_0009:  callvirt   ""string System.Exception.Message.get""
    IL_000e:  call       ""void System.Console.WriteLine(string)""
    IL_0013:  leave.s    IL_0015
  }
  IL_0015:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (System.Exception V_0) //e
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  ldc.i4.0
    IL_0003:  conv.i
    IL_0004:  call       ""void Program.<<Main>$>g__M|0_0(out int)""
    IL_0009:  nop
    IL_000a:  nop
    IL_000b:  leave.s    IL_001e
  }
  catch System.Exception
  {
    IL_000d:  stloc.0
    IL_000e:  nop
    IL_000f:  ldloc.0
    IL_0010:  callvirt   ""string System.Exception.Message.get""
    IL_0015:  call       ""void System.Console.WriteLine(string)""
    IL_001a:  nop
    IL_001b:  nop
    IL_001c:  leave.s    IL_001e
  }
  IL_001e:  nop
  IL_001f:  nop
  IL_0020:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerIndirection_ThroughTernary_01()
        {
            var code = @"
using System;

unsafe
{
    bool b = true;
    M(ref b ? ref *(int*)0 : ref *(int*)1);
    void M(ref int i) => Console.WriteLine(""run"");
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brtrue.s   IL_0007
  IL_0003:  ldc.i4.1
  IL_0004:  conv.i
  IL_0005:  br.s       IL_0009
  IL_0007:  ldc.i4.0
  IL_0008:  conv.i
  IL_0009:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_000e:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (bool V_0) //b
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  brtrue.s   IL_000a
  IL_0006:  ldc.i4.1
  IL_0007:  conv.i
  IL_0008:  br.s       IL_000c
  IL_000a:  ldc.i4.0
  IL_000b:  conv.i
  IL_000c:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  nop
  IL_0014:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerIndirection_ThroughTernary_02()
        {
            var code = @"
using System;

unsafe
{
    int i1 = 0;
    int* p1 = &i1;
    bool b = true;
    M2(ref b ? ref *M1(*p1) : ref i1);

    int* M1(int i)
    {
        Console.Write(i);
        return (int*)0;
    }

    void M2(ref int i) => Console.WriteLine(""run"");
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (int V_0, //i1
                int* V_1) //p1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  conv.u
  IL_0005:  stloc.1
  IL_0006:  ldc.i4.1
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  ldloca.s   V_0
  IL_000b:  br.s       IL_0014
  IL_000d:  ldloc.1
  IL_000e:  ldind.i4
  IL_000f:  call       ""int* Program.<<Main>$>g__M1|0_0(int)""
  IL_0014:  call       ""void Program.<<Main>$>g__M2|0_1(ref int)""
  IL_0019:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (int V_0, //i1
                int* V_1, //p1
                bool V_2) //b
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  conv.u
  IL_0006:  stloc.1
  IL_0007:  ldc.i4.1
  IL_0008:  stloc.2
  IL_0009:  ldloc.2
  IL_000a:  brtrue.s   IL_0010
  IL_000c:  ldloca.s   V_0
  IL_000e:  br.s       IL_0017
  IL_0010:  ldloc.1
  IL_0011:  ldind.i4
  IL_0012:  call       ""int* Program.<<Main>$>g__M1|0_0(int)""
  IL_0017:  call       ""void Program.<<Main>$>g__M2|0_1(ref int)""
  IL_001c:  nop
  IL_001d:  nop
  IL_001e:  nop
  IL_001f:  nop
  IL_0020:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "0run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem(53113, "https://github.com/dotnet/roslyn/issues/53113")]
        public void TestRefOnPointerArrayAccess()
        {
            var code = @"
using System;

unsafe
{
    M(ref ((int*)0)[1]);
    void M(ref int i) => Console.WriteLine(""run"");
}
";

            verify(TestOptions.UnsafeReleaseExe, @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i
  IL_0002:  ldc.i4.4
  IL_0003:  add
  IL_0004:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_0009:  ret
}
");

            verify(TestOptions.UnsafeDebugExe, @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  conv.i
  IL_0003:  ldc.i4.4
  IL_0004:  add
  IL_0005:  call       ""void Program.<<Main>$>g__M|0_0(ref int)""
  IL_000a:  nop
  IL_000b:  nop
  IL_000c:  nop
  IL_000d:  ret
}
");

            void verify(CSharpCompilationOptions options, string expectedIL)
            {
                var comp = CreateCompilation(code, options: options);
                var verifier = CompileAndVerify(comp, expectedOutput: "run", verify: Verification.Fails);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestExtensionMethodRefReadonlyModifierWithEnumType()
        {
            var source = """
                int i = 0;
                i.M1();
                
                E e = (E)0;
                e.M2();
                
                enum E;
                
                static class Extensions
                {
                    public static void M1(this ref readonly int e)
                    {
                        System.Console.WriteLine("int");
                    }
                
                    public static void M2(this ref readonly E e)
                    {
                        System.Console.WriteLine("enum");
                    }
                }
                """;

            var comp = CompileAndVerify(source, symbolValidator: validate, expectedOutput: """
                int
                enum
                """);

            static void validate(ModuleSymbol m)
            {
                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("Extensions.M1").Parameters.Single().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("Extensions.M2").Parameters.Single().RefKind);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestExtensionBlockRefReadonlyModifierWithEnumType()
        {
            var source = """
                int i = 0;
                i.M1();
                _ = i.P1;
                i.P1 = 0;
                _ = i + new object();
                
                E e = (E)0;
                e.M1();
                _ = e.P1;
                e.P1 = 0;
                _ = e + new object();
                
                enum E;
                
                static class IntExtensions
                {
                    extension(ref readonly int i)
                    {
                        public void M1()
                        {
                            System.Console.WriteLine("method_int");
                        }

                        public int P1
                        {
                            get
                            {
                                System.Console.WriteLine("property_get_int");
                                return 0;
                            }
                
                            set
                            {
                                System.Console.WriteLine("property_set_int");
                            }
                        }

                        public static int operator +(in int a, object b)
                        {
                            System.Console.WriteLine("operator_int");
                            return 0;
                        }
                    }
                }
                
                static class EnumExtensions
                {
                    extension(ref readonly E e)
                    {
                        public void M1()
                        {
                            System.Console.WriteLine("method_enum");
                        }

                        public int P1
                        {
                            get
                            {
                                System.Console.WriteLine("property_get_enum");
                                return 0;
                            }

                            set
                            {
                                System.Console.WriteLine("property_set_enum");
                            }
                        }

                        public static int operator +(in E a, object b)
                        {
                            System.Console.WriteLine("operator_enum");
                            return 0;
                        }
                    }   
                }
                """;

            var comp = CompileAndVerify(source, symbolValidator: validate, expectedOutput: """
                method_int
                property_get_int
                property_set_int
                operator_int
                method_enum
                property_get_enum
                property_set_enum
                operator_enum
                """);

            static void validate(ModuleSymbol m)
            {
                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetTypeMember("IntExtensions").GetTypeMembers().Single().ExtensionParameter.RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("IntExtensions.M1").Parameters.Single().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("IntExtensions.get_P1").Parameters.Single().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("IntExtensions.set_P1").Parameters.First().RefKind);

                AssertEx.Equal(
                    RefKind.In,
                    m.GlobalNamespace.GetMember<MethodSymbol>("IntExtensions.op_Addition").Parameters.First().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetTypeMember("EnumExtensions").GetTypeMembers().Single().ExtensionParameter.RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("EnumExtensions.M1").Parameters.Single().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("EnumExtensions.get_P1").Parameters.Single().RefKind);

                AssertEx.Equal(
                    RefKind.RefReadOnlyParameter,
                    m.GlobalNamespace.GetMember<MethodSymbol>("EnumExtensions.set_P1").Parameters.First().RefKind);

                AssertEx.Equal(
                    RefKind.In,
                    m.GlobalNamespace.GetMember<MethodSymbol>("EnumExtensions.op_Addition").Parameters.First().RefKind);
            }
        }
    }
}
