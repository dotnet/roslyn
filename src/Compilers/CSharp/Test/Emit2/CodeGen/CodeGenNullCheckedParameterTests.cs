// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
    public void M(string input!!) { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""input""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""input""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ret
}", sequencePoints: "C.M");

            compilation.VerifyIL("ThrowIfNull", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldarg.1
  IL_0004:  call       ""Throw""
  IL_0009:  ret
}");
            compilation.VerifyIL("Throw", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_0006:  throw
}");
            if (ExecutionConditionUtil.IsCoreClr)
            {
                compilation.VerifyTypeIL("<PrivateImplementationDetails>", @"
.class private auto ansi sealed '<PrivateImplementationDetails>'
	extends [netstandard]System.Object
{
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method assembly hidebysig static 
		void Throw (
			string paramName
		) cil managed 
	{
		// Method begins at RVA 0x206b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: newobj instance void [netstandard]System.ArgumentNullException::.ctor(string)
		IL_0006: throw
	} // end of method '<PrivateImplementationDetails>'::Throw
	.method assembly hidebysig static 
		void ThrowIfNull (
			object argument,
			string paramName
		) cil managed 
	{
		// Method begins at RVA 0x2073
		// Code size 10 (0xa)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: brtrue.s IL_0009
		IL_0003: ldarg.1
		IL_0004: call void '<PrivateImplementationDetails>'::Throw(string)
		IL_0009: ret
	} // end of method '<PrivateImplementationDetails>'::ThrowIfNull
} // end of class <PrivateImplementationDetails>
");
            }
        }

        [Fact]
        public void TestNetModule()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string input!!) { }
}";
            var compilation = CompileAndVerify(
                source,
                options: TestOptions.DebugModule.WithModuleName("Module"),
                parseOptions: TestOptions.RegularPreview,
                // PeVerify under net472 gives the error: "The module  was expected to contain an assembly manifest."
                verify: Verification.Skipped);
            compilation.VerifyIL("C.M", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""input""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ret
}", sequencePoints: "C.M");

            compilation.VerifyIL("ThrowIfNull", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldarg.1
  IL_0004:  call       ""Throw""
  IL_0009:  ret
}");
            compilation.VerifyIL("Throw", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_0006:  throw
}");
            if (ExecutionConditionUtil.IsCoreClr)
            {
                compilation.VerifyTypeIL("<PrivateImplementationDetails><Module>", @"
.class private auto ansi sealed '<PrivateImplementationDetails><Module>'
	extends [netstandard]System.Object
{
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method assembly hidebysig static 
		void Throw (
			string paramName
		) cil managed 
	{
		// Method begins at RVA 0x206b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: newobj instance void [netstandard]System.ArgumentNullException::.ctor(string)
		IL_0006: throw
	} // end of method '<PrivateImplementationDetails><Module>'::Throw
	.method assembly hidebysig static 
		void ThrowIfNull (
			object argument,
			string paramName
		) cil managed 
	{
		// Method begins at RVA 0x2073
		// Code size 10 (0xa)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: brtrue.s IL_0009
		IL_0003: ldarg.1
		IL_0004: call void '<PrivateImplementationDetails><Module>'::Throw(string)
		IL_0009: ret
	} // end of method '<PrivateImplementationDetails><Module>'::ThrowIfNull
} // end of class <PrivateImplementationDetails><Module>
");
            }
        }

        [Fact]
        public void TestManyParamsOneNullChecked()
        {
            var source = @"
using System;
public class C
{
    public static void Main() { }
    public void M(string x, string y!!) { }
}
";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  2
 ~IL_0000:  ldarg.2
  IL_0001:  ldstr      ""y""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.2
  IL_0001:  ldstr      ""y""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalStringParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name!! = ""rose"") { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       12 (0xc)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""name""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ret
}", sequencePoints: "C.M");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""name""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ret
}", sequencePoints: "C.M");
        }

        [Fact]
        public void TestNullCheckedOperator()
        {
            var source = @"
class Box 
{
    public static void Main() { }
    public static int operator+ (Box b!!, Box c)  
    { 
        return 2;
    }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
 ~IL_0000:  ldarg.0
  IL_0001:  ldstr      ""b""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ldc.i4.2
  IL_000c:  ret
}", sequencePoints: "Box.op_Addition");
            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("int Box.op_Addition(Box, Box)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0)
 ~IL_0000:  ldarg.0
  IL_0001:  ldstr      ""b""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ldc.i4.2
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_0011
 -IL_0011:  ldloc.0
  IL_0012:  ret
}", sequencePoints: "Box.op_Addition");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/58322")]
        public void TestNullCheckedArgListImplementation()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!!, 'M'));
    }
    void M2(__arglist)
    {
    }
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
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
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
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
        public void TestNullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[string index!!] => null;
    public static void Main() { }
}";

            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[string].get", @"
{
  // Code size       13 (0xd)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""index""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ldnull
  IL_000c:  ret
}", sequencePoints: "C.get_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[string].get", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""index""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  ldnull
  IL_000d:  ret
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
    public string this[object item!!]
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
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[object].set", @"
{
  // Code size       21 (0x15)
  .maxstack  3
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""item""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ldarg.0
  IL_000c:  ldfld      ""object[] C.items""
  IL_0011:  ldc.i4.0
  IL_0012:  ldarg.2
  IL_0013:  stelem.ref
 -IL_0014:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[object].set", @"
{
  // Code size       23 (0x17)
  .maxstack  3
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""item""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ldarg.0
  IL_000e:  ldfld      ""object[] C.items""
  IL_0013:  ldc.i4.0
  IL_0014:  ldarg.2
  IL_0015:  stelem.ref
 -IL_0016:  ret
}", sequencePoints: "C.set_Item");
        }

        [Fact]
        public void TestNullCheckedIndexedSetter()
        {
            var source = @"
class C
{
    object this[object index!!] { set { } }
    public static void Main() { }
}";
            // Release
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[object].set", @"
{
  // Code size       12 (0xc)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""index""
  IL_0006:  call       ""ThrowIfNull""
 -IL_000b:  ret
}", sequencePoints: "C.set_Item");

            // Debug
            compilation = CompileAndVerify(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.this[object].set", @"
{
  // Code size       14 (0xe)
  .maxstack  2
 ~IL_0000:  ldarg.1
  IL_0001:  ldstr      ""index""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  nop
 -IL_000c:  nop
 -IL_000d:  ret
}", sequencePoints: "C.set_Item");
        }

        [Fact]
        public void TestNullCheckedSubstitution1()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B1<T> : A<T> where T : class
{
    internal override void M<U>(U u!!) { }
}
";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("B1<T>.M<U>(U)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""U""
  IL_0006:  ldstr      ""u""
  IL_000b:  call       ""ThrowIfNull""
  IL_0010:  ret
}");
            compilation.VerifyIL("A<T>.M<U>(U)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""U""
  IL_0006:  ldstr      ""u""
  IL_000b:  call       ""ThrowIfNull""
  IL_0010:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution2()
        {
            var source = @"
using System;

class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B3<T> : A<T?> where T : struct
{
    internal override void M<U>(U u!!) { }
}

class Program
{
    static void Main()
    {
        var b3 = new B3<int>();
        try
        {
            b3.M<int?>(1);
            Console.Write(1);
            b3.M<int?>(null);
        }
        catch
        {
            Console.Write(2);
        }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "12");
            compilation.VerifyIL("B3<T>.M<U>(U)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        ""U""
  IL_0006:  brtrue.s   IL_0013
  IL_0008:  ldstr      ""u""
  IL_000d:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_0012:  throw
  IL_0013:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution3()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B5 : A<object>
{
    internal override void M<U>(U u!!) { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("B5.M<U>(U)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""U""
  IL_0006:  ldstr      ""u""
  IL_000b:  call       ""ThrowIfNull""
  IL_0010:  ret
}");
        }

        [Fact]
        public void TestNullCheckedSubstitution4()
        {
            var source = @"
class C
{
    void M<T>(T value!!) where T : notnull { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.M<T>(T)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  ldstr      ""value""
  IL_000b:  call       ""ThrowIfNull""
  IL_0010:  ret
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
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void TestNullCheckingExpectedOutput2()
        {
            var source = @"
using System;
class Program
{
    static void M(int? i!!)
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
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void TestNullCheckingExpectedOutput3()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B1<T> : A<T> where T : class
{
    internal override void M<U>(U u!!) { }
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
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void TestNullCheckingExpectedOutput4()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B3<T> : A<T?> where T : struct
{
    internal override void M<U>(U u!!) { }
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
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void TestNullCheckingExpectedOutput5()
        {
            var source = @"
using System;
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B5 : A<object>
{
    internal override void M<U>(U u!!) { }
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
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
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
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
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
        Func<string, string> func1 = x!! => x;
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ret
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
        Func<string, string, string> func1 = (x!!, y) => x;
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string, string)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ret
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
        Func<string, string, string> func1 = (x!!, y!!) => x;
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string, string)", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.2
  IL_000c:  ldstr      ""y""
  IL_0011:  call       ""ThrowIfNull""
  IL_0016:  ldarg.1
  IL_0017:  ret
}");
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
        Func<string, string> func1 = _!! => null;
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c.<M>b__0_0(string)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""_""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldnull
  IL_000c:  ret
}");
        }

        [Fact]
        public void NullCheckedLambdaInsideFieldTest()
        {
            var source = @"
using System;
class C
{
    Func<string, string> func1 = x!! => x;
    public void M()
    {
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c.<.ctor>b__2_0(string)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ret
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
        void InnerM(string x!!) { }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
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
        void InnerM(string x!!, string y) { }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string, string)", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
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
        void InnerM(string x!!, string y!!) { }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string, string)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ldstr      ""y""
  IL_0011:  call       ""ThrowIfNull""
  IL_0016:  ret
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
        void InnerM(string x!!) { }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
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
    public void M(string x!!)
    {
        InnerM(""hello"");
        void InnerM(string x) { }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<M>g__InnerM|0_0(string)", @"
{
    // Code size        1 (0x1)
    .maxstack  0
    IL_0000:  ret
}");
            compilation.VerifyIL("C.M(string)", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldstr      ""hello""
  IL_0010:  call       ""void C.<M>g__InnerM|0_0(string)""
  IL_0015:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructors()
        {
            var source = @"
class C
{
    public C(string x!!) { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C..ctor(string)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.0
  IL_000c:  call       ""object..ctor()""
  IL_0011:  ret
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
    public C(string x!!) : base(x) { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C..ctor(string)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.0
  IL_000c:  ldarg.1
  IL_000d:  call       ""B..ctor(string)""
  IL_0012:  ret
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
    public C(string x!!) : this() { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C..ctor(string)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.0
  IL_000c:  call       ""C..ctor()""
  IL_0011:  ret
}");
        }

        [Fact]
        public void TestNullCheckedConstructorWithFieldInitializers()
        {
            var source = @"
class C
{
    int y = 5;
    public C(string x!!) { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C..ctor(string)", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.5
  IL_000d:  stfld      ""int C.y""
  IL_0012:  ldarg.0
  IL_0013:  call       ""object..ctor()""
  IL_0018:  ret
}");
        }

        [Fact]
        public void TestNullCheckedExpressionBodyMethod()
        {
            var source = @"
class C
{
    object Local(object arg!!) => arg;
    public static void Main() { }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.Local(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""arg""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ret
}");
        }

        [Fact]
        public void TestNullChecked2ExpressionBodyLambdas()
        {
            var source = @"
using System;
class C
{
    public Func<string, string> M(string s1!!) => s2!! => s2 + s1;
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.<>c__DisplayClass0_0.<M>b__0(string)", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s2""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.1
  IL_000c:  ldarg.0
  IL_000d:  ldfld      ""string C.<>c__DisplayClass0_0.s1""
  IL_0012:  call       ""string string.Concat(string, string)""
  IL_0017:  ret
}");
            compilation.VerifyIL("C.M(string)", @"
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s1""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0010:  dup
  IL_0011:  ldarg.1
  IL_0012:  stfld      ""string C.<>c__DisplayClass0_0.s1""
  IL_0017:  ldftn      ""string C.<>c__DisplayClass0_0.<M>b__0(string)""
  IL_001d:  newobj     ""System.Func<string, string>..ctor(object, System.IntPtr)""
  IL_0022:  ret
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
class C
{
    IEnumerable<char> GetChars(string s!!)
    {
        foreach (var c in s)
        {
            yield return c;
        }
    }
    public static void Main()
    {
        C c = new C();
        IEnumerable<char> e = c.GetChars(""hello"");

    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("C.GetChars(string)", @"
{
  // Code size       26 (0x1a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.s   -2
  IL_000d:  newobj     ""C.<GetChars>d__0..ctor(int)""
  IL_0012:  dup
  IL_0013:  ldarg.1
  IL_0014:  stfld      ""string C.<GetChars>d__0.<>3__s""
  IL_0019:  ret
}");
        }

        [Fact]
        public void TestNullCheckedAsyncIterator()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<char> GetChars(string s!!, Task t)
    {
        foreach (var c in s)
        {
            await t;
            yield return c;
        }
    }
}";
            var compilation = CompileAndVerify(CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, parseOptions: TestOptions.RegularPreview));
            compilation.VerifyIL("C.GetChars", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.s   -2
  IL_000d:  newobj     ""C.<GetChars>d__0..ctor(int)""
  IL_0012:  dup
  IL_0013:  ldarg.1
  IL_0014:  stfld      ""string C.<GetChars>d__0.<>3__s""
  IL_0019:  dup
  IL_001a:  ldarg.2
  IL_001b:  stfld      ""System.Threading.Tasks.Task C.<GetChars>d__0.<>3__t""
  IL_0020:  ret
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
        IEnumerable<char> GetChars(string s!!)
        {
            foreach (var c in s)
            {
                yield return c;
            }
        }
    }
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyIL("Iterators.<Use>g__GetChars|0_0(string)", @"
{
  // Code size       26 (0x1a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.s   -2
  IL_000d:  newobj     ""Iterators.<<Use>g__GetChars|0_0>d..ctor(int)""
  IL_0012:  dup
  IL_0013:  ldarg.0
  IL_0014:  stfld      ""string Iterators.<<Use>g__GetChars|0_0>d.<>3__s""
  IL_0019:  ret
}");
        }

        [Fact]
        public void TestNullCheckedEnumeratorInLocalFunction()
        {
            var source = @"
using System;
using System.Collections.Generic;

Iterators.Use();

class Iterators
{
    public static void Use()
    {
        IEnumerator<char> e = GetChars(""hello"");
        Console.Write(1);

        try
        {
            GetChars(null!);
        }
        catch (ArgumentNullException)
        {
            Console.Write(2);
        }
        IEnumerator<char> GetChars(string s!!)
        {
            foreach (var c in s)
            {
                yield return c;
            }
        }
    }

}";
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "12");
            verifier.VerifyIL("Iterators.<Use>g__GetChars|0_0(string)", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.0
  IL_000c:  newobj     ""Iterators.<<Use>g__GetChars|0_0>d..ctor(int)""
  IL_0011:  dup
  IL_0012:  ldarg.0
  IL_0013:  stfld      ""string Iterators.<<Use>g__GetChars|0_0>d.s""
  IL_0018:  ret
}");
        }

        [Fact]
        public void TestNullCheckedIteratorExecution()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Program
{
    IEnumerable<char> GetChars(string s!!)
    {
        foreach (var c in s)
        {
            yield return c;
        }
    }
    static void Main()
    {
        Program p = new Program();
        var c = Invoke(() => p.GetChars(string.Empty));
        var e = Invoke(() => c.GetEnumerator());
        Invoke(() => e.MoveNext());
        c = Invoke(() => p.GetChars(null));
    }
    static T Invoke<T>(Func<T> f)
    {
        T t = default;
        try
        {
            t = f();
            Console.WriteLine(""ok"");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType());
        }
        return t;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
ok
ok
ok
System.ArgumentNullException", parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void TestNullCheckedLambdaWithMissingType()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Func<string, string> func = x!! => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (7,37): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //         Func<string, string> func = x!! => x;
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(7, 37));
        }

        [Fact]
        public void TestNullCheckedLocalFunctionWithMissingType()
        {
            var source =
@"
class Program
{
    public static void Main()
    {
        M(""ok"");
        void M(string x!!) { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (7,23): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //         void M(string x!!) { }
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(7, 23));
        }

        [Fact]
        public void TestEmptyNullCheckedIterator1()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    public static void Main() { }
    static IEnumerable<char> GetChars(string s!!)
    {
        yield break;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview).VerifyIL("C.GetChars(string)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.s   -2
  IL_000d:  newobj     ""C.<GetChars>d__1..ctor(int)""
  IL_0012:  ret
}");
        }

        [Fact]
        public void TestEmptyNullCheckedIterator2()
        {
            var source = @"
using System.Collections.Generic;
class C
{
    public static void Main() { }
    static IEnumerator<char> GetChars(string s!!)
    {
        yield break;
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview).VerifyIL("C.GetChars(string)", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldc.i4.0
  IL_000c:  newobj     ""C.<GetChars>d__1..ctor(int)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void TestNullCheckedParams()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(params int[] number!!) {}
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview).VerifyIL("C.M(params int[])", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""number""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("where T : class")]
        [InlineData("where T : notnull")]
        public void TestTypeParameter(string constraint)
        {
            var source = @"
class C
{
    public void M<T>(T t!!) " + constraint + @"
    {
    }
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M<T>", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  ldstr      ""t""
  IL_000b:  call       ""ThrowIfNull""
  IL_0010:  ret
}");
        }

        [Fact]
        public void TestTypeParameter_StructConstrained()
        {
            var source = @"
class C
{
    public void M<T>(T? t!!) where T : struct
    {
    }
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics(
                // (4,25): warning CS8995: Nullable type 'T?' is null-checked and will throw if null.
                //     public void M<T>(T? t!!) where T : struct
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t").WithArguments("T?").WithLocation(4, 25));
            verifier.VerifyIL("C.M<T>", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       ""bool T?.HasValue.get""
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldstr      ""t""
  IL_000e:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_0013:  throw
  IL_0014:  ret
}");
            verifier.VerifyMissing("ThrowIfNull");
            verifier.VerifyMissing("Throw");
        }

        [Fact]
        public void TestNullableValueType()
        {
            var source = @"
class C
{
    public void M(int? i!!) // 1
    {
    }
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics(
                // (4,24): warning CS8995: Nullable type 'int?' is null-checked and will throw if null.
                //     public void M(int? i!!) // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "i").WithArguments("int?").WithLocation(4, 24));
            verifier.VerifyIL("C.M(int?)", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarga.s   V_1
  IL_0002:  call       ""bool int?.HasValue.get""
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  ldstr      ""i""
  IL_000e:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_0013:  throw
  IL_0014:  ret
}");
            verifier.VerifyMissing("ThrowIfNull");
            verifier.VerifyMissing("Throw");
        }

        [Fact]
        public void NullCheckedPointer()
        {
            var source = @"
using System;


class C
{
    static unsafe void M1(void* ptr!!) { }
    static unsafe void M2(int* ptr!!) { }
    static unsafe void M3(delegate*<int, void> funcPtr!!) { }

    public static void M0(int x) { }

    public static unsafe void Main()
    {
        int x = 1;
        try
        {
            M1(&x);
            Console.Write(1);
            M1(null);
        }
        catch
        {
            Console.Write(2);
        }

        try
        {
            M2(&x);
            Console.Write(3);
            M2(null);
        }
        catch
        {
            Console.Write(4);
        }

        try
        {
            M3(&M0);
            Console.Write(5);
            M3(null);
        }
        catch
        {
            Console.Write(6);
        }
    }
}";
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeDebugExe, verify: Verification.Skipped, expectedOutput: "123456");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M1", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldstr      ""ptr""
  IL_0008:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_000d:  throw
  IL_000e:  nop
  IL_000f:  ret
}");
            verifier.VerifyIL("C.M2", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldstr      ""ptr""
  IL_0008:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_000d:  throw
  IL_000e:  nop
  IL_000f:  ret
}");
            verifier.VerifyIL("C.M3", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000e
  IL_0003:  ldstr      ""funcPtr""
  IL_0008:  newobj     ""System.ArgumentNullException..ctor(string)""
  IL_000d:  throw
  IL_000e:  nop
  IL_000f:  ret
}");
            verifier.VerifyMissing("ThrowIfNull");
            verifier.VerifyMissing("Throw");
        }

        [Fact]
        public void UserDefinedConversion()
        {
            var source = @"
class C
{
    public static bool operator ==(C c1, C c2) => false;
    public static bool operator !=(C c1, C c2) => false;
    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;

    public static void M(C c!!)
    {
        if (c == null) { System.Console.Write(1); }
    }
}
";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""c""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.0
  IL_000c:  ldnull
  IL_000d:  call       ""bool C.op_Equality(C, C)""
  IL_0012:  brfalse.s  IL_001a
  IL_0014:  ldc.i4.1
  IL_0015:  call       ""void System.Console.Write(int)""
  IL_001a:  ret
}");
        }

        [Fact]
        public void Override_NullCheckedDifference()
        {
            var source =
@"
#nullable enable
class Base
{
    public virtual void M1(string x!!) { }
    public virtual void M2(string x) { }
}

class Derived : Base
{
    public override void M1(string x) { }
    public override void M2(string x!!) { }
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Base.M1", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
            verifier.VerifyIL("Base.M2", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("Derived.M1", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("Derived.M2", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Implementation_NullCheckedDifference1()
        {
            var source =
@"
#nullable enable
interface Base
{
    public void M1(string x!!) { }
    public void M2(string x) { }
}

class Derived : Base
{
    public void M1(string x) { }
    public void M2(string x!!) { }
}";
            var verifier = CompileAndVerify(source, verify: Verification.Skipped, targetFramework: TargetFramework.NetCoreApp);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Base.M1", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
            verifier.VerifyIL("Base.M2", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("Derived.M1", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("Derived.M2", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Implementation_NullCheckedDifference2()
        {
            var source =
@"
#nullable enable
interface Base
{
    void M1(string x);
    void M2(string x);
}

class Derived : Base
{
    public void M1(string x!!) { }
    void Base.M2(string x!!) { }
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Derived.M1", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
            verifier.VerifyIL("Derived.Base.M2", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""x""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ret
}");
        }

        [Fact]
        public void RecordPrimaryConstructor()
        {
            var source =
@"
using System;

Console.Write(new Record(""1"", ""0"").Prop1);
try
{
    new Record(null, ""a"");
}
catch (ArgumentNullException)
{
    Console.Write(2);
}

Console.Write(new RecordStruct(""3"", ""0"").Prop1);
try
{
    new RecordStruct(""b"", null);
}
catch (ArgumentNullException)
{
    Console.Write(4);
}

record Record(string Prop1!!, string Prop2!!);
record struct RecordStruct(string Prop1!!, string Prop2!!);
";
            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, verify: Verification.Skipped, expectedOutput: "1234");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Record..ctor(string, string)", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""Prop1""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.2
  IL_000c:  ldstr      ""Prop2""
  IL_0011:  call       ""ThrowIfNull""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  stfld      ""string Record.<Prop1>k__BackingField""
  IL_001d:  ldarg.0
  IL_001e:  ldarg.2
  IL_001f:  stfld      ""string Record.<Prop2>k__BackingField""
  IL_0024:  ldarg.0
  IL_0025:  call       ""object..ctor()""
  IL_002a:  ret
}");

            verifier.VerifyIL("RecordStruct..ctor(string, string)", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""Prop1""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldarg.2
  IL_000c:  ldstr      ""Prop2""
  IL_0011:  call       ""ThrowIfNull""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  stfld      ""string RecordStruct.<Prop1>k__BackingField""
  IL_001d:  ldarg.0
  IL_001e:  ldarg.2
  IL_001f:  stfld      ""string RecordStruct.<Prop2>k__BackingField""
  IL_0024:  ret
}");
        }

        [Fact, WorkItem(58824, "https://github.com/dotnet/roslyn/issues/58824")]
        public void TestWithEmbeddedReference()
        {
            var source = @"
using System;

C.M(""a"");
Console.Write(1);
try
{
    C.M(null);
    Console.Write(0);
}
catch
{
    Console.Write(2);
}

class C
{
    public static void M(string s!!)
    {
    }
}
";
            var verifier = CompileAndVerify(source, references: new[] { TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(true) }, expectedOutput: "12");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", @"
{
    // Code size       12 (0xc)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldstr      ""s""
    IL_0006:  call       ""ThrowIfNull""
    IL_000b:  ret
}
");
        }

        [Fact]
        public void AsyncMethod_1()
        {
            var source = @"
using System;
using System.Threading.Tasks;

var c = new C();
try
{
    await c.M(""a"");
    Console.Write(1);
}
catch (ArgumentNullException)
{
    Console.Write(0);
}

try
{
    _ = c.M(null!);
    Console.Write(0);
}
catch (ArgumentNullException)
{
    Console.Write(2);
}

class C
{
    public async Task M(string s!!)
    {
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics(
                // (28,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public async Task M(string s!!)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(28, 23));
            verifier.VerifyIL("C.M", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_0012:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0026:  ldloca.s   V_0
  IL_0028:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<C.<M>d__0>(ref C.<M>d__0)""
  IL_002d:  ldloca.s   V_0
  IL_002f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0034:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_0039:  ret
}
");
        }

        [Fact]
        public void AsyncLocalFunction()
        {
            var source = @"
using System;
using System.Threading.Tasks;

await C.M();

class C
{
    public static async Task M()
    {
        try
        {
            await local(""a"");
            Console.Write(1);
        }
        catch (ArgumentNullException)
        {
            Console.Write(0);
        }

        try
        {
            _ = local(null!);
            Console.Write(0);
        }
        catch (ArgumentNullException)
        {
            Console.Write(2);
        }

        async Task local(string s!!) { }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics(
                // (31,20): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async Task local(string s!!) { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "local").WithLocation(31, 20));
            verifier.VerifyIL("C.<M>g__local|0_0", @"
{
    // Code size       58 (0x3a)
    .maxstack  2
    .locals init (C.<<M>g__local|0_0>d V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldstr      ""s""
    IL_0006:  call       ""ThrowIfNull""
    IL_000b:  ldloca.s   V_0
    IL_000d:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
    IL_0012:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
    IL_0017:  ldloca.s   V_0
    IL_0019:  ldc.i4.m1
    IL_001a:  stfld      ""int C.<<M>g__local|0_0>d.<>1__state""
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
    IL_0026:  ldloca.s   V_0
    IL_0028:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<C.<<M>g__local|0_0>d>(ref C.<<M>g__local|0_0>d)""
    IL_002d:  ldloca.s   V_0
    IL_002f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<<M>g__local|0_0>d.<>t__builder""
    IL_0034:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
    IL_0039:  ret
}
");
        }

        [Fact]
        public void AsyncLambda()
        {
            var source = @"
using System;
using System.Threading.Tasks;

await C.M();

class C
{
    public static async Task M()
    {
        var lambda = async Task (string s!!) => { };
        try
        {
            await lambda(""a"");
            Console.Write(1);
        }
        catch (ArgumentNullException)
        {
            Console.Write(0);
        }

        try
        {
            _ = lambda(null!);
            Console.Write(0);
        }
        catch (ArgumentNullException)
        {
            Console.Write(2);
        }
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics(
                // (11,46): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         var lambda = async Task (string s!!) => { };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(11, 46));
            verifier.VerifyIL("C.<>c.<M>b__0_0", @"
{
    // Code size       58 (0x3a)
    .maxstack  2
    .locals init (C.<>c.<<M>b__0_0>d V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldstr      ""s""
    IL_0006:  call       ""ThrowIfNull""
    IL_000b:  ldloca.s   V_0
    IL_000d:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
    IL_0012:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<M>b__0_0>d.<>t__builder""
    IL_0017:  ldloca.s   V_0
    IL_0019:  ldc.i4.m1
    IL_001a:  stfld      ""int C.<>c.<<M>b__0_0>d.<>1__state""
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<M>b__0_0>d.<>t__builder""
    IL_0026:  ldloca.s   V_0
    IL_0028:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<C.<>c.<<M>b__0_0>d>(ref C.<>c.<<M>b__0_0>d)""
    IL_002d:  ldloca.s   V_0
    IL_002f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<M>b__0_0>d.<>t__builder""
    IL_0034:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
    IL_0039:  ret
}
");
        }

        [Fact]
        public void AsyncMethod_2()
        {
            var source = @"
using System;
using System.Threading.Tasks;

var c = new C();
try
{
    await c.M(""a"");
    Console.Write(1);
}
catch (ArgumentNullException)
{
    Console.Write(0);
}

try
{
    _ = c.M(null!);
    Console.Write(0);
}
catch (ArgumentNullException)
{
    Console.Write(2);
}

class C
{
    public async Task M(string s!!)
    {
        if (s == """")
            return;
        await Task.CompletedTask;
        return;
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""s""
  IL_0006:  call       ""ThrowIfNull""
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_0012:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldarg.1
  IL_001a:  stfld      ""string C.<M>d__0.s""
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldc.i4.m1
  IL_0022:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0027:  ldloca.s   V_0
  IL_0029:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_002e:  ldloca.s   V_0
  IL_0030:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<C.<M>d__0>(ref C.<M>d__0)""
  IL_0035:  ldloca.s   V_0
  IL_0037:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_003c:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_0041:  ret
}
");
        }

        [Fact]
        public void TestNullCheckedRefString()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        var s = ""1"";
        Console.Write(s);
        M(ref s);
        Console.Write(s);
        s = null;
        try
        {
            M(ref s!);
            Console.Write(0);
        }
        catch (ArgumentNullException)
        {
            Console.Write(3);
        }
    }

    public static void M(ref string x!!)
    {
        x = ""2"";
    }
}";
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  ldstr      ""x""
  IL_0007:  call       ""ThrowIfNull""
  IL_000c:  ldarg.0
  IL_000d:  ldstr      ""2""
  IL_0012:  stind.ref
  IL_0013:  ret
}");
        }

        [Fact]
        public void TestNullCheckedInString()
        {
            var source = @"
using System;

class C
{
    public static void Main()
    {
        var s = ""1"";
        Console.Write(s);
        M(in s);
        s = null;
        try
        {
            M(in s!);
            Console.Write(0);
        }
        catch (ArgumentNullException)
        {
            Console.Write(2);
        }
    }

    public static void M(in string x!!)
    {
    }
}";
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  ldstr      ""x""
  IL_0007:  call       ""ThrowIfNull""
  IL_000c:  ret
}");
        }
    }
}
