// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenConstructorInitTests : CSharpTestBase
    {
        [Fact]
        public void TestImplicitConstructor()
        {
            var source = @"
class C
{
    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret       
}
");
        }

        [Fact]
        public void TestImplicitConstructorInitializer()
        {
            var source = @"
class C
{
    C()
    {
    }

    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret       
}
");
        }

        [Fact]
        public void TestExplicitBaseConstructorInitializer()
        {
            var source = @"
class C
{
    C() : base()
    {
    }

    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret       
}
");
        }

        [Fact]
        public void TestExplicitThisConstructorInitializer()
        {
            var source = @"
class C
{
    C() : this(1)
    {
    }    

    C(int x)
    {
    }

    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4.1  
  IL_0002:  call       ""C..ctor(int)""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void TestExplicitOverloadedBaseConstructorInitializer()
        {
            var source = @"
class B
{
    public B(int x)
    {
    }

    public B(string x)
    {
    }
}

class C : B
{
    C() : base(1)
    {
    }

    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4.1  
  IL_0002:  call       ""B..ctor(int)""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void TestExplicitOverloadedThisConstructorInitializer()
        {
            var source = @"
class C
{
    C() : this(1)
    {
    }    

    C(int x)
    {
    }    

    C(string x)
    {
    }

    static void Main()
    {
        C c = new C();
    }
}
";
            CompileAndVerify(source, expectedOutput: string.Empty).
                VerifyIL("C..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4.1  
  IL_0002:  call       ""C..ctor(int)""
  IL_0007:  ret       
}
");
        }

        [Fact]
        public void TestComplexInitialization()
        {
            var source = @"
class B
{
    private int f = E.Init(3, ""B.f"");

    public B()
    {
        System.Console.WriteLine(""B()"");
    }    

    public B(int x) : this (x.ToString())
    {
        System.Console.WriteLine(""B(int)"");
    }    

    public B(string x) : this()
    {
        System.Console.WriteLine(""B(string)"");
    }
}

class C : B
{
    private int f = E.Init(4, ""C.f"");

    public C() : this(1)
    {
        System.Console.WriteLine(""C()"");
    }    

    public C(int x) : this(x.ToString())
    {
        System.Console.WriteLine(""C(int)"");
    }    

    public C(string x) : base(x.Length)
    {
        System.Console.WriteLine(""C(string)"");
    }
}

class E
{
    static void Main()
    {
        C c = new C();
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}
";
            //interested in execution order and number of field initializations
            CompileAndVerify(source, expectedOutput: @"
C.f
B.f
B()
B(string)
B(int)
C(string)
C(int)
C()
");
        }

        // Successive Operator On Class
        [WorkItem(540992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540992")]
        [Fact]
        public void TestSuccessiveOperatorOnClass()
        {
            var text = @"
using System;
class C
{
    public int num;
    public C(int i)
    {
        this.num = i;
    }
    static void Main(string[] args)
    {
        C c1 = new C(1);
        C c2 = new C(2);
        C c3 = new C(3);
        bool verify = c1.num == 1 && c2.num == 2 & c3.num == 3;
        Console.WriteLine(verify);
    }
}
";
            var expectedOutput = @"True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestInitializerInCtor001()
        {
            var source = @"
class C
{
    public int I{get;}

    public C()
    {
        I = 42;
    }

    static void Main()
    {
        C c = new C();
        System.Console.WriteLine(c.I);
    }
}
";
            CompileAndVerify(source, expectedOutput: "42").
                VerifyIL("C..ctor", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.s   42
  IL_0009:  stfld      ""int C.<I>k__BackingField""
  IL_000e:  ret
}
");
        }

        [Fact]
        public void TestInitializerInCtor002()
        {
            var source = @"
public struct S
{
    public int X{get;}
    public int Y{get;}

    public S(int dummy)
    {
        X = 42;
        Y = X;
    }

    public static void Main()
    {
        S s = new S(1);
        System.Console.WriteLine(s.Y);
    }
}
";
            CompileAndVerify(source, expectedOutput: "42").
                VerifyIL("S..ctor", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int S.<X>k__BackingField""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.0
  IL_000a:  call       ""int S.X.get""
  IL_000f:  stfld      ""int S.<Y>k__BackingField""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void TestInitializerInCtor003()
        {
            var source = @"
struct C
{
    public int I{get;}
    public int J{get; set;}

    public C(int arg)
    {
        I = 33;
        J = I;
        I = J;
        I = arg;
    }

    static void Main()
    {
        C c = new C(42);
        System.Console.WriteLine(c.I);
    }
}
";
            CompileAndVerify(source, expectedOutput: "42").
                VerifyIL("C..ctor(int)", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   33
  IL_0003:  stfld      ""int C.<I>k__BackingField""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.0
  IL_000a:  call       ""int C.I.get""
  IL_000f:  call       ""void C.J.set""
  IL_0014:  ldarg.0
  IL_0015:  ldarg.0
  IL_0016:  call       ""int C.J.get""
  IL_001b:  stfld      ""int C.<I>k__BackingField""
  IL_0020:  ldarg.0
  IL_0021:  ldarg.1
  IL_0022:  stfld      ""int C.<I>k__BackingField""
  IL_0027:  ret
}
");
        }


        [Fact]
        public void TestInitializerInCtor004()
        {
            var source = @"
struct C
{
    public static int I{get;}
    public static int J{get; set;}

    static C()
    {
        I = 33;
        J = I;
        I = J;
        I = 42;
    }

    static void Main()
    {
        System.Console.WriteLine(C.I);
    }
}
";
            CompileAndVerify(source, expectedOutput: "42").
                VerifyIL("C..cctor()", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldc.i4.s   33
  IL_0002:  stsfld     ""int C.<I>k__BackingField""
  IL_0007:  call       ""int C.I.get""
  IL_000c:  call       ""void C.J.set""
  IL_0011:  call       ""int C.J.get""
  IL_0016:  stsfld     ""int C.<I>k__BackingField""
  IL_001b:  ldc.i4.s   42
  IL_001d:  stsfld     ""int C.<I>k__BackingField""
  IL_0022:  ret
}
");
        }

        [Fact]
        public void TestInitializerInCtor005()
        {
            var source = @"
struct C
{
    static int P1 { get; }

    static int y = (P1 = 123);

    static void Main()
    {
        System.Console.WriteLine(y);
        System.Console.WriteLine(P1);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"123
123").
                VerifyIL("C..cctor()", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.i4.s   123
  IL_0002:  dup
  IL_0003:  stsfld     ""int C.<P1>k__BackingField""
  IL_0008:  stsfld     ""int C.y""
  IL_000d:  ret
}
");
        }

        [Fact]
        public void TestInitializerInCtor006()
        {
            var source = @"
struct C
{
    static int P1 { get; }

    static int y { get; } = (P1 = 123);

    static void Main()
    {
        System.Console.WriteLine(y);
        System.Console.WriteLine(P1);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"123
123").
                VerifyIL("C..cctor()", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldc.i4.s   123
  IL_0002:  dup
  IL_0003:  stsfld     ""int C.<P1>k__BackingField""
  IL_0008:  stsfld     ""int C.<y>k__BackingField""
  IL_000d:  ret
}
");
        }

        [WorkItem(4383, "https://github.com/dotnet/roslyn/issues/4383")]
        [Fact]
        public void DecimalConstInit001()
        {
            var source = @"
using System;
using System.Collections.Generic;

public static class Module1
{
    public static void Main()
    {
        Console.WriteLine(ClassWithStaticField.Dictionary[""String3""]);
    }
    }

    public class ClassWithStaticField
    {
        public const decimal DecimalConstant = 375;

        private static Dictionary<String, Single> DictionaryField = new Dictionary<String, Single> {
        {""String1"", 1.0F},
        {""String2"", 2.0F},
        {""String3"", 3.0F}
    };

        public static Dictionary<String, Single> Dictionary
        {
            get
            {
                return DictionaryField;
            }
        }
    }
";
            CompileAndVerify(source, expectedOutput: "3").
                VerifyIL("ClassWithStaticField..cctor", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldc.i4     0x177
  IL_0005:  newobj     ""decimal..ctor(int)""
  IL_000a:  stsfld     ""decimal ClassWithStaticField.DecimalConstant""
  IL_000f:  newobj     ""System.Collections.Generic.Dictionary<string, float>..ctor()""
  IL_0014:  dup
  IL_0015:  ldstr      ""String1""
  IL_001a:  ldc.r4     1
  IL_001f:  callvirt   ""void System.Collections.Generic.Dictionary<string, float>.Add(string, float)""
  IL_0024:  dup
  IL_0025:  ldstr      ""String2""
  IL_002a:  ldc.r4     2
  IL_002f:  callvirt   ""void System.Collections.Generic.Dictionary<string, float>.Add(string, float)""
  IL_0034:  dup
  IL_0035:  ldstr      ""String3""
  IL_003a:  ldc.r4     3
  IL_003f:  callvirt   ""void System.Collections.Generic.Dictionary<string, float>.Add(string, float)""
  IL_0044:  stsfld     ""System.Collections.Generic.Dictionary<string, float> ClassWithStaticField.DictionaryField""
  IL_0049:  ret
}
");
        }
    }
}
