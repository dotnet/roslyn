// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenNullCheckedParameterTests : CSharpTestBase
    {
        [Fact]
        public void TestIsNullChecked()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string input!) { }
}
";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       10 (0xa)
      .maxstack  1
     ~IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
      IL_0008:  throw
     -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       11 (0xb)
      .maxstack  1
     ~IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
      IL_0008:  throw
     -IL_0009:  nop
     -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestManyParamsOneNullChecked()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string x, string y!) { }
}
";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
  // Code size       10 (0xa)
  .maxstack  1
 ~IL_0000:  ldarg.2
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
  IL_0008:  throw
 -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
      // Code size       11 (0xb)
      .maxstack  1
     ~IL_0000:  ldarg.2
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
      IL_0008:  throw
     -IL_0009:  nop
     -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalNullParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name! = null) { }
}";

            // Release
            CreateCompilation(source).VerifyEmitDiagnostics(
                    // (5,19): error CS8719: Parameter 'string' is null-checked but is null by default.
                    //     void M(string name! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("string").WithLocation(5, 19));
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalStringParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name! = ""rose"") { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
  // Code size       10 (0xa)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
  IL_0008:  throw
 -IL_0009:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
 {
    // Code size       11 (0xb)
    .maxstack  1
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    -IL_0009:  nop
    -IL_000a:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedOperator()
        {
            var source = @"
class Box 
{
    public static void Main() { }
    public static int operator+ (Box b!, Box c)  
    { 
        return 2;
    }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
   ~IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
   -IL_0009:  ldc.i4.2
    IL_000a:  ret
}", sequencePoints: "Box.op_Addition");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int V_0)
   ~IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
   -IL_0009:  nop
   -IL_000a:  ldc.i4.2
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e
   -IL_000e:  ldloc.0
    IL_000f:  ret
}", sequencePoints: "Box.op_Addition");
        }

        [Fact(Skip = "PROTOTYPE")]
        public void TestNullCheckedArgListImplementation()
        {
            // PROTOTYPE : Will address later - issues with post-fix & binding?
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!, 'M'));
    }
    void M2(__arglist)
    {
    }
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.M", @"
{
    // Code size       10 (0xa)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.s   77
    IL_0004:  call       ""void C.M2(__arglist) with __arglist( int, char)""
    IL_0009:  ret
}");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.M", @"
{
    // Code size       12 (0xc)
    .maxstack  3
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  ldc.i4.s   77
    IL_0005:  call       ""void C.M2(__arglist) with __arglist( int, char)""
    IL_000a:  nop
    IL_000b:  ret
}");
        }

        [Fact]
        public void TestManyNullCheckedArgs()
        {
            var source = @"
class C
{
    public void M(int x!, string y!) { }
    public static void Main() { }
}";

            CreateCompilation(source).VerifyEmitDiagnostics(
                    // (4,23): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //     public void M(int x!, string y!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(4, 23));
        }

        [Fact]
        public void TestNullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[string index!] => null;
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[string].get", @"
{
  // Code size       11 (0xb)
  .maxstack  1
 ~IL_0000:  ldarg.1
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
  IL_0008:  throw
 -IL_0009:  ldnull
  IL_000a:  ret
}", sequencePoints: "C.get_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[string].get", @"
{
   // Code size       11 (0xb)
   .maxstack  1
  ~IL_0000:  ldarg.1
   IL_0001:  brtrue.s   IL_0009
   IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
   IL_0008:  throw
  -IL_0009:  ldnull
   IL_000a:  ret
}", sequencePoints: "C.get_Item");
        }

        [Fact]
        public void TestNullCheckedIndexedGetterSetter()
        {
            var source = @"
using System;
class C
{
    private object[] items = {'h', ""hello""};
    public string this[object item!]
    {
        get
        {
            return items[0].ToString();
        }
        set
        {
            items[0] = value;
        }
    }
    public static void Main() 
    {
        C c = new C();
        Console.WriteLine((string)c[""world""] ?? ""didn't work"");
    }
}";
            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[object].set", @"
{
    // Code size       19 (0x13)
    .maxstack  3
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    -IL_0009:  ldarg.0
    IL_000a:  ldfld      ""object[] C.items""
    IL_000f:  ldc.i4.0
    IL_0010:  ldarg.2
    IL_0011:  stelem.ref
    -IL_0012:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[object].set", @"
{
      // Code size       20 (0x14)
      .maxstack  3
     ~IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0009
      IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
      IL_0008:  throw
     -IL_0009:  nop
     -IL_000a:  ldarg.0
      IL_000b:  ldfld      ""object[] C.items""
      IL_0010:  ldc.i4.0
      IL_0011:  ldarg.2
      IL_0012:  stelem.ref
     -IL_0013:  ret
}", sequencePoints: "C.set_Item");
        }

        [Fact]
        public void TestNullCheckedIndexedSetter()
        {
            var source = @"
class C
{
    object this[object index!] { set { } }
    public static void Main() { }
}";
            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe);
            compilation.VerifyIL("C.this[object].set", @"
{
    // Code size       10 (0xa)
    .maxstack  1
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    -IL_0009:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe);
            compilation.VerifyIL("C.this[object].set", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    ~IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    -IL_0009:  nop
    -IL_000a:  ret
}", sequencePoints: "C.set_Item");
        }

        [Fact]
        public void TestNullableInteraction()
        {
            // PROTOTYPE : Add warning about combining explicit null checking with a nullable
            var source = @"
class C
{
    static void M(int? i!) { }
    public static void Main() { }
}";
            // Release
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M(int?)", @"
{
    // Code size       16 (0x10)
    .maxstack  1
    IL_0000:  ldarga.s   V_0
    IL_0002:  call       ""bool int?.HasValue.get""
    IL_0007:  brtrue.s   IL_000f
    IL_0009:  newobj     ""System.ArgumentNullException..ctor()""
    IL_000e:  throw
    IL_000f:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution1()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B1<T> : A<T> where T : class
{
    internal override void M<U>(U u!) { }
}
";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("B1<T>.M<U>(U)", @"
{
      // Code size       15 (0xf)
      .maxstack  1
      IL_0000:  ldarg.1
      IL_0001:  box        ""U""
      IL_0006:  brtrue.s   IL_000e
      IL_0008:  newobj     ""System.ArgumentNullException..ctor()""
      IL_000d:  throw
      IL_000e:  ret
}");
            compilation.VerifyIL("A<T>.M<U>(U)", @"
{
    // Code size       15 (0xf)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  box        ""U""
    IL_0006:  brtrue.s   IL_000e
    IL_0008:  newobj     ""System.ArgumentNullException..ctor()""
    IL_000d:  throw
    IL_000e:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution2()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B2<T> : A<T> where T : struct
{
    internal override void M<U>(U u!) { }
}";

            CreateCompilation(source).VerifyEmitDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitution3()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B3<T> : A<T?> where T : struct
{
    internal override void M<U>(U u!) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("B3<T>.M<U>(U)", @"
{
    // Code size       15 (0xf)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  box        ""U""
    IL_0006:  brtrue.s   IL_000e
    IL_0008:  newobj     ""System.ArgumentNullException..ctor()""
    IL_000d:  throw
    IL_000e:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution4()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B4 : A<int>
{
    internal override void M<U>(U u!) { }
}";
            CreateCompilation(source).VerifyEmitDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitution5()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B5 : A<object>
{
    internal override void M<U>(U u!) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("B5.M<U>(U)", @"
{
    // Code size       15 (0xf)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  box        ""U""
    IL_0006:  brtrue.s   IL_000e
    IL_0008:  newobj     ""System.ArgumentNullException..ctor()""
    IL_000d:  throw
    IL_000e:  ret
}");
        }

        [Fact]
        public void TestNullCheckingExpectedOutput()
        {
            var source = @"
using System;
class Program
{
    static void M<T>(T t)
    {
        if (t is null) throw new ArgumentNullException();
    }
    static void Main()
    {
        Invoke(() => M(new object()));
        Invoke(() => M((object)null));
        Invoke(() => M((int?)1));
        Invoke(() => M((int?)null));
    }
    static void Invoke(Action a)
    {
        try
        {
            a();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
}
}";
            CompileAndVerify(source, expectedOutput: @"
ok
System.ArgumentNullException
ok
System.ArgumentNullException");
        }

        [Fact]
        public void TestNullCheckingExpectedOutput2()
        {
            var source = @"
using System;
class Program
{
    static void M(int? i!)
    {
    }
    static void Main()
    {
        Invoke(() => M((int?)1));
        Invoke(() => M((int?)null));
    }
    static void Invoke(Action a)
    {
        try
        {
            a();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
}
}";
            CompileAndVerify(source, expectedOutput: @"
ok
System.ArgumentNullException");
        }

        [Fact]
        public void TestNullCheckingExpectedOutput3()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B1<T> : A<T> where T : class
{
    internal override void M<U>(U u!) { }
}
class Program
{
    static void Main()
    {
        B1<string> b1 = new B1<string>();
        Invoke(() => b1.M<string>(""hello world""));
        Invoke(() => b1.M<string>(null));
    }
    static void Invoke(Action a)
    {
        try
        {
            a();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"
ok
System.ArgumentNullException");
        }

        [Fact]
        public void TestNullCheckingExpectedOutput4()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B3<T> : A<T?> where T : struct
{
    internal override void M<U>(U u!) { }
}
class Program
{
    static void Main()
    {
        B3<bool> b3 = new B3<bool>();
        Invoke(() => b3.M<bool?>(false));
        Invoke(() => b3.M<bool?>(null));
    }
    static void Invoke(Action a)
    {
        try
        {
            a();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"
ok
System.ArgumentNullException");
        }

        [Fact]
        public void TestNullCheckingExpectedOutput5()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B5 : A<object>
{
    internal override void M<U>(U u!) { }
}
class Program
{
    static void Main()
    {
        B5 b5 = new B5();
        Invoke(() => b5.M<bool?>(false));
        Invoke(() => b5.M<bool?>(null));
    }
    static void Invoke(Action a)
    {
        try
        {
            a();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"
ok
System.ArgumentNullException");
        }

        [Fact]
        public void NotNullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
      // Code size        2 (0x2)
      .maxstack  1
      IL_0000:  ldarg.1
      IL_0001:  ret
}");
        }

        [Fact]
        public void NotNullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
      // Code size        2 (0x2)
      .maxstack  1
      IL_0000:  ldarg.1
      IL_0001:  ret
}");
        }

        [Fact]
        public void NotNullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
      // Code size        2 (0x2)
      .maxstack  1
      IL_0000:  ldarg.1
      IL_0001:  ret
}");
        }

        [Fact]
        public void NullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x! => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  ret
}");
        }

        [Fact]
        public void NullCheckedLambdaWithMultipleParameters()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (x!, y) => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string, string)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  ret
}");
        }

        [Fact]
        public void ManyNullCheckedLambdasTest()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (x!, y!) => x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string, string)", @"
{
    // Code size       20 (0x14)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.2
    IL_000a:  brtrue.s   IL_0012
    IL_000c:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0011:  throw
    IL_0012:  ldarg.1
    IL_0013:  ret
}");
        }

        [Fact]
        public void NoGeneratedNullCheckIfNonNullableTest()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = x! => x;
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyEmitDiagnostics(
                    // (7,32): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //         Func<int, int> func1 = x! => x;
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(7, 32));
        }

        [Fact]
        public void NullCheckedDiscardTest()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = _! => null;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldnull
    IL_000a:  ret
}");
        }

        [Fact]
        public void NullCheckedLambdaInsideFieldTest()
        {
            var source = @"
using System;
class C
{
    Func<string, string> func1 = x! => x;
    public void M()
    {
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c.<.ctor>b__2_0(string)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  ret
}");
        }

        [Fact]
        public void NullCheckedLocalFunction()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        InnerM(""hello world"");
        void InnerM(string x!) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
    // Code size       10 (0xa)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ret
}");
        }

        [Fact]
        public void NullCheckedLocalFunctionWithManyParams()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        InnerM(""hello"",  ""world"");
        void InnerM(string x!, string y) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string, string)", @"
{
    // Code size       10 (0xa)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ret
}");
        }

        [Fact]
        public void TestLocalFunctionWithManyNullCheckedParams()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        InnerM(""hello"",  ""world"");
        void InnerM(string x!, string y!) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string, string)", @"
{
    // Code size       19 (0x13)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  brtrue.s   IL_0012
    IL_000c:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0011:  throw
    IL_0012:  ret
}");
        }

        [Fact]
        public void TestLocalFunctionWithShadowedNullCheckedInnerParam()
        {
            var source = @"
using System;
class C
{
    public void M(string x)
    {
        InnerM(""hello"");
        void InnerM(string x!) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
    // Code size       10 (0xa)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ret
}");
            compilation.VerifyIL("C.M(string)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""hello""
    IL_0005:  call       ""void C.<M>g__InnerM|0_0(string)""
    IL_000a:  ret
}");
        }

        [Fact]
        public void TestLocalFunctionWithShadowedNullCheckedOuterParam()
        {
            var source = @"
using System;
class C
{
    public void M(string x!)
    {
        InnerM(""hello"");
        void InnerM(string x) { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
    // Code size        1 (0x1)
    .maxstack  0
    IL_0000:  ret
}");
            compilation.VerifyIL("C.M(string)", @"
{
    // Code size       20 (0x14)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldstr      ""hello""
    IL_000e:  call       ""void C.<M>g__InnerM|0_0(string)""
    IL_0013:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructors()
        {
            var source = @"
class C
{
    public C(string x!) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C..ctor(string)", @"
{
    // Code size       16 (0x10)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.0
    IL_000a:  call       ""object..ctor()""
    IL_000f:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructorWithBaseChain()
        {
            var source = @"
class B
{
    public B(string y) { }
}
class C : B
{
    public C(string x!) : base(x) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C..ctor(string)", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  call       ""B..ctor(string)""
    IL_0010:  ret
}");
            compilation.VerifyIL("B..ctor(string)", @"
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""object..ctor()""
    IL_0006:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructorWithThisChain()
        {
            var source = @"
class C
{
    public C() { }
    public C(string x!) : this() { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C..ctor(string)", @"
{
    // Code size       16 (0x10)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.0
    IL_000a:  call       ""C..ctor()""
    IL_000f:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructorWithFieldInitializers()
        {
            var source = @"
class C
{
    int y = 5;
    public C(string x!) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C..ctor(string)", @"
{
    // Code size       23 (0x17)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.0
    IL_000a:  ldc.i4.5
    IL_000b:  stfld      ""int C.y""
    IL_0010:  ldarg.0
    IL_0011:  call       ""object..ctor()""
    IL_0016:  ret
}");
        }

        [Fact]
        public void TestNullCheckedExpressionBodyMethod()
        {
            var source = @"
class C
{
    object Local(object arg!) => arg;
    public static void Main() { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Local(object)", @"
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  ret
}");
        }

        [Fact]
        public void TestNullChecked2ExpressionBodyLambdas()
        {
            var source = @"
using System;
class C
{
    public Func<string, string> M(string s1!) => s2! => s2 + s1;
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.<>c__DisplayClass0_0.<M>b__0(string)", @"
{
    // Code size       22 (0x16)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""string C.<>c__DisplayClass0_0.s1""
    IL_0010:  call       ""string string.Concat(string, string)""
    IL_0015:  ret
}");
            compilation.VerifyIL("C.M(string)", @"
{
    // Code size       33 (0x21)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
    IL_000e:  dup
    IL_000f:  ldarg.1
    IL_0010:  stfld      ""string C.<>c__DisplayClass0_0.s1""
    IL_0015:  ldftn      ""string C.<>c__DisplayClass0_0.<M>b__0(string)""
    IL_001b:  newobj     ""System.Func<string, string>..ctor(object, System.IntPtr)""
    IL_0020:  ret
}");
            compilation.VerifyIL("C.<>c__DisplayClass0_0..ctor()", @"
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""object..ctor()""
    IL_0006:  ret
}");
        }

        [Fact]
        public void TestNullCheckedIterator()
        {
            var source = @"
using System.Collections.Generic;
class Iterators
{
    IEnumerable<char> GetChars(string s!)
    {
        foreach (var c in s)
        {
            yield return c;
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Iterators.GetChars(string)", @"
{
    // Code size       24 (0x18)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldc.i4.s   -2
    IL_000b:  newobj     ""Iterators.<GetChars>d__0..ctor(int)""
    IL_0010:  dup
    IL_0011:  ldarg.1
    IL_0012:  stfld      ""string Iterators.<GetChars>d__0.<>3__s""
    IL_0017:  ret
}");
        }

        [Fact]
        public void TestNullCheckedIteratorInLocalFunction()
        {
            var source = @"
using System.Collections.Generic;
class Iterators
{
    void Use()
    {
        IEnumerable<char> e = GetChars(""hello"");
        IEnumerable<char> GetChars(string s!)
        {
            foreach (var c in s)
            {
                yield return c;
            }
        }
    }

}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("Iterators.<Use>g__GetChars|0_0(string)", @"
{
    // Code size       24 (0x18)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.ArgumentNullException..ctor()""
    IL_0008:  throw
    IL_0009:  ldc.i4.s   -2
    IL_000b:  newobj     ""Iterators.<<Use>g__GetChars|0_0>d..ctor(int)""
    IL_0010:  dup
    IL_0011:  ldarg.0
    IL_0012:  stfld      ""string Iterators.<<Use>g__GetChars|0_0>d.<>3__s""
    IL_0017:  ret
}");
        }
    }
}
