// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [WorkItem(540992, "DevDiv")]
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
        public void ParameterlessConstructorStruct001()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x;
        public S1()
        {
            x = 42;
        }
    }

    static void Main()
    {
        var s = new S1();
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "42").
                VerifyIL("C.S1..ctor", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.S1.x""
  IL_0008:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct001()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
        public S1()
        {
        }
    }

    static void Main()
    {
        var s = new S1();
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "42").
                VerifyIL("C.S1..ctor", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.S1.x""
  IL_0008:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct002()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
    }

    static void Main()
    {
        var s = new S1();
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "42").
                VerifyIL("C.S1..ctor", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""C.S1""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      ""int C.S1.x""
  IL_000f:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct003()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
        public S1(int x)
        {
        }
    }

    static void Main()
    {
        var s = new S1(1);
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "42").
                VerifyIL("C.S1..ctor(int)", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.S1.x""
  IL_0008:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct004()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
        public S1(int x)
            :this()
        {
            this.x += x;
        }
    }

    static void Main()
    {
        var s = new S1(1);
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "43").
                VerifyIL("C.S1..ctor(int)", @"
{
  // Code size       21 (0x15)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""C.S1..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int C.S1.x""
  IL_000d:  ldarg.1
  IL_000e:  add
  IL_000f:  stfld      ""int C.S1.x""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct005()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
        public S1(int x)
        {
            this.x += x;
        }
    }

    static void Main()
    {
        var s = new S1(1);
        System.Console.WriteLine(s.x);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "43").
                VerifyIL("C.S1..ctor(int)", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.S1.x""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.0
  IL_000a:  ldfld      ""int C.S1.x""
  IL_000f:  ldarg.1
  IL_0010:  add
  IL_0011:  stfld      ""int C.S1.x""
  IL_0016:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerStruct006()
        {
            var source = @"
class C
{
    struct S1
    {
        public readonly int x = 42;
        public string y;
    }

    static void Main()
    {
        var s = new S1();
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "").
                VerifyIL("C.S1..ctor()", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""C.S1""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      ""int C.S1.x""
  IL_000f:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct001()
        {
            var source = @"
class C
{
    struct S1()
    {
        public readonly int x = 42;
        public string y = null;
    }

    static void Main()
    {
        var s = new S1();
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.WriteLine(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "42").
                VerifyIL("C.S1..ctor()", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.S1.x""
  IL_0008:  ldarg.0
  IL_0009:  ldnull
  IL_000a:  stfld      ""string C.S1.y""
  IL_000f:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct002()
        {
            var source = @"
class C
{
    struct S1(int arg = 42)
    {
        public readonly int x = arg;
        public string y = null;
    }

    static void Main()
    {
        var s = new S1();
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct003()
        {
            var source = @"
class C
{
    struct S1(int arg, string s = ""hi"")
    {
        public int x = arg;
        public string y = s;
    }

    static void Main()
    {
        var s = new S1();
        s.x = 333;
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "0");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct004()
        {
            var source = @"
class C
{
    struct S1(int arg, string s = ""hi"", __arglist)
    {
        public int x = arg;
        public string y = s;
    }

    static void Main()
    {
        var s = new S1();
        s.x = 333;
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "0");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct004a()
        {
            var source = @"
class C
{
    struct S1(int arg, string s = ""hi"", __arglist)
    {
        public int x = arg;
        public string y = s;

        public S1(): this(0, ""hello"", __arglist())
        {
        }
    }

    static void Main()
    {
        var s = new S1();
        s.x = 333;
        s.y = ""JUNK"";

        s = new S1();        
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "0hello").
                VerifyIL("C.S1..ctor()", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldstr      ""hello""
  IL_0007:  call       ""C.S1..ctor(int, string, __arglist)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct005()
        {
            var source = @"
class C
{
    struct S1(decimal s = 123, int? i = 5, System.DateTime d = default(System.DateTime))
    {
        public decimal x = s;
        public int? y = i;
    }

    static void Main()
    {
        var s = new S1();
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "0");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct005a()
        {
            var source = @"
class C
{
    struct S1(decimal s = 123, int? i = 5, System.DateTime d = default(System.DateTime))
    {
        public decimal x = s;
        public int? y = i;

        public S1(): this(0)
        {
        }
    }

    static void Main()
    {
        var s = new S1();
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "05").
                VerifyIL("C.S1..ctor()", @"
{
  // Code size       27 (0x1b)
  .maxstack  4
  .locals init (System.DateTime V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""decimal decimal.Zero""
  IL_0006:  ldc.i4.5
  IL_0007:  newobj     ""int?..ctor(int)""
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    ""System.DateTime""
  IL_0014:  ldloc.0
  IL_0015:  call       ""C.S1..ctor(decimal, int?, System.DateTime)""
  IL_001a:  ret
}
");
        }

        [Fact]
        public void InstanceInitializerPrimaryStruct006()
        {
            var source = @"
class C
{
    struct S1(decimal s = 123, int? i = 5, System.DateTime d = default(System.DateTime))
    {
        public decimal x;
        public int? y;

        {
            x = s;
            y = i;
        }
    }

    static void Main()
    {
        var s = new S1();
        System.Console.Write(s.x);
        System.Console.WriteLine(s.y);
    }
}
";
            CompileAndVerifyExperimental(source, expectedOutput: "0");
        }


        [Fact]
        public void InstanceInitializerStructInExprTree()
        {
            var source = @"

using System;
using System.Linq.Expressions;

class C
{
    struct S1
    {
        public int x = 42;
    }

    static void Main()
    {
        Expression<Func<S1>> testExpr = () => new S1();
        System.Console.Write(testExpr.Compile()().x);
    }
}
";
            CompileAndVerifyExperimental(source, additionalRefs: new[] { ExpressionAssemblyRef }, expectedOutput: "42");
        }
    }
}
