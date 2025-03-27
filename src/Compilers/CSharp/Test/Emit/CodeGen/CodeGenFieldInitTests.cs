// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenFieldInitTests : CSharpTestBase
    {
        [Fact]
        public void TestInstanceFieldInitializersPartialClass()
        {
            var source = @"
class C
{
    static void Main()
    {
        Partial p;
        
        System.Console.WriteLine(""Start Partial()"");
        p = new Partial();
        System.Console.WriteLine(""p.a = {0}"", p.a);
        System.Console.WriteLine(""p.b = {0}"", p.b);
        System.Console.WriteLine(""p.c = {0}"", p.c);
        System.Console.WriteLine(""End Partial()"");

        System.Console.WriteLine(""Start Partial(int)"");
        p = new Partial(3);
        System.Console.WriteLine(""p.a = {0}"", p.a);
        System.Console.WriteLine(""p.b = {0}"", p.b);
        System.Console.WriteLine(""p.c = {0}"", p.c);
        System.Console.WriteLine(""End Partial(int)"");
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

partial class Partial
{
    public int a = C.Init(1, ""Partial.a"");

    public Partial()
    {
    }
}

partial class Partial
{
    public int c, b = C.Init(2, ""Partial.b"");

    public Partial(int garbage)
    {
        this.c = C.Init(3, ""Partial.c"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Start Partial()
Partial.a
Partial.b
p.a = 1
p.b = 2
p.c = 0
End Partial()
Start Partial(int)
Partial.a
Partial.b
Partial.c
p.a = 1
p.b = 2
p.c = 3
End Partial(int)
");
        }

        [Fact]
        public void TestInstanceFieldInitializersInheritance()
        {
            var source = @"
class C
{
    static void Main()
    {
        Derived2 d = new Derived2();
        System.Console.WriteLine(""d.a = {0}"", d.a);
        System.Console.WriteLine(""d.b = {0}"", d.b);
        System.Console.WriteLine(""d.c = {0}"", d.c);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class Base
{
    public int a = C.Init(1, ""Base.a"");

    public Base()
    {
        System.Console.WriteLine(""Base()"");
    }
}

class Derived : Base
{
    public int b = C.Init(2, ""Derived.b"");

    public Derived()
    {
        System.Console.WriteLine(""Derived()"");
    }
}

class Derived2 : Derived
{
    public int c = C.Init(3, ""Derived2.c"");

    public Derived2()
    {
        System.Console.WriteLine(""Derived2()"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Derived2.c
Derived.b
Base.a
Base()
Derived()
Derived2()
d.a = 1
d.b = 2
d.c = 3
");
        }

        [Fact]
        public void TestStaticFieldInitializersPartialClass()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(""Partial.a = {0}"", Partial.a);
        System.Console.WriteLine(""Partial.b = {0}"", Partial.b);
        System.Console.WriteLine(""Partial.c = {0}"", Partial.c);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

partial class Partial
{
    public static int a = C.Init(1, ""Partial.a"");
}

partial class Partial
{
    public static int c, b = C.Init(2, ""Partial.b"");

    static Partial()
    {
        c = C.Init(3, ""Partial.c"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Partial.a
Partial.b
Partial.c
Partial.a = 1
Partial.b = 2
Partial.c = 3
");
        }

        [Fact]
        public void TestStaticFieldInitializersInheritance1()
        {
            var source = @"
class C
{
    static void Main()
    {
        Base b = new Base();
        System.Console.WriteLine(""Base.a = {0}"", Base.a);
        System.Console.WriteLine(""Derived.b = {0}"", Derived.b);
        System.Console.WriteLine(""Derived2.c = {0}"", Derived2.c);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class Base
{
    public static int a = C.Init(1, ""Base.a"");
    static Base() { System.Console.WriteLine(""Base()""); }
}

class Derived : Base
{
    public static int b = C.Init(2, ""Derived.b"");
    static Derived() { System.Console.WriteLine(""Derived()""); }
}

class Derived2 : Derived
{
    public static int c = C.Init(3, ""Derived2.c"");
    static Derived2() { System.Console.WriteLine(""Derived2()""); }
}
";
            CompileAndVerify(source, expectedOutput: @"
Base.a
Base()
Base.a = 1
Derived.b
Derived()
Derived.b = 2
Derived2.c
Derived2()
Derived2.c = 3
");
        }

        [Fact]
        public void TestStaticFieldInitializersInheritance2()
        {
            var source = @"
class C
{
    static void Main()
    {
        Base b = new Derived();
        System.Console.WriteLine(""Base.a = {0}"", Base.a);
        System.Console.WriteLine(""Derived.b = {0}"", Derived.b);
        System.Console.WriteLine(""Derived2.c = {0}"", Derived2.c);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class Base
{
    public static int a = C.Init(1, ""Base.a"");
    static Base() { System.Console.WriteLine(""Base()""); }
}

class Derived : Base
{
    public static int b = C.Init(2, ""Derived.b"");
    static Derived() { System.Console.WriteLine(""Derived()""); }
}

class Derived2 : Derived
{
    public static int c = C.Init(3, ""Derived2.c"");
    static Derived2() { System.Console.WriteLine(""Derived2()""); }
}
";
            CompileAndVerify(source, expectedOutput: @"
Derived.b
Derived()
Base.a
Base()
Base.a = 1
Derived.b = 2
Derived2.c
Derived2()
Derived2.c = 3
");
        }

        [Fact]
        public void TestStaticFieldInitializersInheritance3()
        {
            var source = @"
class C
{
    static void Main()
    {
        Base b = new Derived2();
        System.Console.WriteLine(""Base.a = {0}"", Base.a);
        System.Console.WriteLine(""Derived.b = {0}"", Derived.b);
        System.Console.WriteLine(""Derived2.c = {0}"", Derived2.c);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class Base
{
    public static int a = C.Init(1, ""Base.a"");
    static Base() { System.Console.WriteLine(""Base()""); }
}

class Derived : Base
{
    public static int b = C.Init(2, ""Derived.b"");
    static Derived() { System.Console.WriteLine(""Derived()""); }
}

class Derived2 : Derived
{
    public static int c = C.Init(3, ""Derived2.c"");
    static Derived2() { System.Console.WriteLine(""Derived2()""); }
}
";
            CompileAndVerify(source, expectedOutput: @"
Derived2.c
Derived2()
Derived.b
Derived()
Base.a
Base()
Base.a = 1
Derived.b = 2
Derived2.c = 3
");
        }

        [Fact]
        public void TestFieldInitializersMixed()
        {
            var source = @"
class C
{
    static void Main()
    {
        Derived d = new Derived();
        System.Console.WriteLine(""Base.a = {0}"", Base.a);
        System.Console.WriteLine(""Derived.b = {0}"", Derived.b);
        System.Console.WriteLine(""d.x = {0}"", d.x);
        System.Console.WriteLine(""d.y = {0}"", d.y);

    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class Base
{
    public static int a = C.Init(1, ""Base.a"");
    public int x = C.Init(3, ""Base.x"");

    static Base() 
    { 
        System.Console.WriteLine(""static Base()""); 
    }

    public Base()
    {
        System.Console.WriteLine(""Base()"");
    }
}

class Derived : Base
{
    public static int b = C.Init(2, ""Derived.b"");
    public int y = C.Init(4, ""Derived.y"");

    static Derived() 
    {
        System.Console.WriteLine(""static Derived()""); 
    }

    public Derived()
    {
        System.Console.WriteLine(""Derived()"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
Derived.b
static Derived()
Derived.y
Base.a
static Base()
Base.x
Base()
Derived()
Base.a = 1
Derived.b = 2
d.x = 3
d.y = 4
");
        }

        [Fact]
        public void TestFieldInitializersInOptimizedMode1()
        {
            var source = @"
class C
{
    public string str1 = null;
    public string str2 = ""a"";
    public string str3 = (string)(null);
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation).VerifyIL("C..ctor", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""a""
  IL_0006:  stfld      ""string C.str2""
  IL_000b:  ldarg.0
  IL_000c:  call       ""object..ctor()""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TestFieldInitializersInOptimizedMode2()
        {
            var source = @"
class C
{
    public int f1 = 0;
    public short f2 = (short)0;
    public bool f3 = false;
    public char f4 = '\0';
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation).VerifyIL("C..ctor", @"
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
        public void TestFieldInitializersInOptimizedMode3()
        {
            var source = @"
class C<T>
{
    public T f1 = default(T);
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation).VerifyIL("C<T>..ctor", @"
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
        public void TestFieldInitializersInOptimizedMode4()
        {
            var source = @"
class C
{
    public static string str1 = null;
    public static string str2 = ""a"";
    public static string str3 = (string)(null);
    public static int f1 = 0;
    public static short f2 = (short)0;
    public static bool f3 = false;
    public static char f4 = '\0';
}

";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation).VerifyIL("C..cctor", @"
{
  // Code size       47 (0x2f)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  stsfld     ""string C.str1""
  IL_0006:  ldstr      ""a""
  IL_000b:  stsfld     ""string C.str2""
  IL_0010:  ldnull
  IL_0011:  stsfld     ""string C.str3""
  IL_0016:  ldc.i4.0
  IL_0017:  stsfld     ""int C.f1""
  IL_001c:  ldc.i4.0
  IL_001d:  stsfld     ""short C.f2""
  IL_0022:  ldc.i4.0
  IL_0023:  stsfld     ""bool C.f3""
  IL_0028:  ldc.i4.0
  IL_0029:  stsfld     ""char C.f4""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void TestFieldInitializersInOptimizedMode5()
        {
            var source = @"
    using System;    

    enum E1 : byte
    {
        a,
        b
    }

    class C<T> where T:Exception
    {
        public static string str1 = null;
        public static T tt = default(T);
        public static Exception tt1 = (T)null;
        public static Exception tt2 = null as T;
        public static T tt3 = (T)(Exception)(object)default(T);
        public static E1 ee = 0;
        public static E1 ee1 = (E1)(int)(E1)0;
        public static E1 ee2 = (E1)(int)E1.a;
        public static object str3 = (object)(string)(null);
        public static int f1 = 0;
        public static short f2 = (short)0;
        public static bool f3 = false;
        public static char f4 = '\0';
    }

";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(
                source,
                symbolValidator: validator,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validator(ModuleSymbol module)
            {
                // note: we could make the synthesized constructor smarter and realize that
                // nothing needs to be emitted for these initializers.
                // but it doesn't serve any realistic scenarios at this time.
                var type = module.ContainingAssembly.GetTypeByMetadataName("C`1");
                Assert.NotNull(type.GetMember(".cctor"));
            }
        }

        [WorkItem(530445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530445")]
        [Fact]
        public void TestFieldInitializersInOptimizedMode6()
        {
            var source = @"
class C
{
    private bool a = false;
}

";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(compilation).VerifyIL("C..ctor", @"
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
        public void TestFieldInitializersConstructorInitializers()
        {
            var source = @"
class C
{
    static void Main()
    {
        A a = new A();
        System.Console.WriteLine(""a.a = {0}"", a.a);
    }

    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

class A
{
    public int a = C.Init(1, ""A.a"");

    public A()
        : this(1)
    {
        System.Console.WriteLine(""A()"");
    }

    public A(int garbage)
    {
        System.Console.WriteLine(""A(int)"");
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
A.a
A(int)
A()
a.a = 1
");
        }

        [Fact]
        public void Ordering()
        {
            var trees = new List<SyntaxTree>();
            var expectedOutput = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                trees.Add(SyntaxFactory.ParseSyntaxTree("System.Console.WriteLine(" + i + ");", options: TestOptions.Script));
                expectedOutput.AppendLine(i.ToString());
            }

            var compilation = CreateCompilationWithMscorlib461(trees, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: expectedOutput.ToString());
        }

        [Fact]
        public void FieldInitializerWithBadConstantValueSameModule()
        {
            var source =
@"class A
{
    public int F = B.F1;
}
class B
{
    public const int F1 = F2;
    public static int F2 = 0;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,27): error CS0133: The expression being assigned to 'B.F1' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "F2").WithArguments("B.F1").WithLocation(7, 27));
        }

        [Fact]
        public void FieldInitializerWithBadConstantValueDifferentModule()
        {
            var source1 =
@"public class B
{
    public const int F1 = F2;
    public static int F2 = 0;
}";
            var compilation1 = CreateCompilation(source1, assemblyName: "1110a705-cc34-430b-9450-ca37031aa828");
            compilation1.VerifyDiagnostics(
                // (3,27): error CS0133: The expression being assigned to 'B.F1' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "F2").WithArguments("B.F1").WithLocation(3, 27));

            var source2 =
@"class A
{
    public object F = M(B.F1);
    private static object M(int i) { return null; }
}";
            CreateCompilation(source2, new[] { new CSharpCompilationReference(compilation1) }, assemblyName: "2110a705-cc34-430b-9450-ca37031aa828")
                .Emit(new System.IO.MemoryStream()).Diagnostics
                    .Verify(
                    // error CS7038: Failed to emit module '2110a705-cc34-430b-9450-ca37031aa828': Unable to determine specific cause of the failure.
                    Diagnostic(ErrorCode.ERR_ModuleEmitFailure).WithArguments("2110a705-cc34-430b-9450-ca37031aa828", "Unable to determine specific cause of the failure."));
        }
    }
}
