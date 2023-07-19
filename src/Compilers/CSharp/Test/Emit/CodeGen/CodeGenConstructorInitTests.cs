// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
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
  IL_000a:  call       ""readonly int S.X.get""
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
  IL_000a:  call       ""readonly int C.I.get""
  IL_000f:  call       ""void C.J.set""
  IL_0014:  ldarg.0
  IL_0015:  ldarg.0
  IL_0016:  call       ""readonly int C.J.get""
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

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void DecimalConstInit002()
        {
            var source1 = @"
class C
{
    const decimal d1 = 0.1m;
}
";
            var source2 = @"
class C
{
    static readonly decimal d1 = 0.1m;
}
";
            var expectedIL = @"
{
  // Code size       16 (0x10)
  .maxstack  5
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.1
  IL_0005:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000a:  stsfld     ""decimal C.d1""
  IL_000f:  ret
}
";
            CompileAndVerify(source1).VerifyIL("C..cctor", expectedIL);
            CompileAndVerify(source2).VerifyIL("C..cctor", expectedIL);
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void DecimalConstInit003()
        {
            var source1 = @"
class C
{
    const decimal d1 = 0.0m;
}
";

            var source2 = @"
class C
{
    static readonly decimal d1 = 0.0m;
}
";

            var expectedIL = @"
{
  // Code size       16 (0x10)
  .maxstack  5
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.1
  IL_0005:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
  IL_000a:  stsfld     ""decimal C.d1""
  IL_000f:  ret
}
";
            CompileAndVerify(source1).VerifyIL("C..cctor", expectedIL);
            CompileAndVerify(source2).VerifyIL("C..cctor", expectedIL);
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void DecimalConstInit004()
        {
            var source1 = @"
class C
{
    const decimal d1 = default;
    const decimal d2 = 0;
    const decimal d3 = 0m;
}
";

            var source2 = @"
class C
{
    static readonly decimal d1 = default;
    static readonly decimal d2 = 0;
    static readonly decimal d3 = 0m;
}
";
            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(source1, symbolValidator: validator, options: options);
            CompileAndVerify(source2, symbolValidator: validator, options: options);

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Null(type.GetMember(".cctor"));
            }
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void StaticLambdaConstructorAlwaysEmitted()
        {
            var source = @"
class C
{
    void M()
    {
        System.Action a1 = () => { };
    }
}
";
            CompileAndVerify(source).
                VerifyIL("C.<>c..cctor", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""C.<>c..ctor()""
  IL_0005:  stsfld     ""C.<>c C.<>c.<>9""
  IL_000a:  ret
}
");
        }

        [WorkItem(217748, "https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=217748")]
        [Fact]
        public void BadExpressionConstructor()
        {
            string source =
@"class C
{
    static dynamic F() => 0;
    dynamic d = F() * 2;
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyEmitDiagnostics(
                // (4,17): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
                //     dynamic d = F() * 2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F()").WithArguments("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo", "Create").WithLocation(4, 17));
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_01()
        {
            string source = @"
#nullable enable
class C
{
    static int i = 0;
    static bool b = false;
}";
            CompileAndVerify(
                source,
                symbolValidator: validator,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Null(type.GetMember(".cctor"));
            }
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_02()
        {
            string source = @"
#nullable enable
class C
{
    static string s = null!;
}";
            CompileAndVerify(
                source,
                symbolValidator: validator,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Null(type.GetMember(".cctor"));
            }
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_03()
        {
            string source = @"
#nullable enable
class C
{
    static (int, object) pair = (0, null!);
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldnull
  IL_0002:  newobj     ""System.ValueTuple<int, object>..ctor(int, object)""
  IL_0007:  stsfld     ""System.ValueTuple<int, object> C.pair""
  IL_000c:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_04()
        {
            string source = @"
#nullable enable
class C
{
    static (int, object) pair1 = default;
    static (int, object) pair2 = default((int, object));
    static (int, object) pair3 = default!;
    static (int, object) pair4 = default((int, object))!;
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for these initializers.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_05()
        {
            string source = @"
#nullable enable
class C
{
    static C instance = default!;
}";
            CompileAndVerify(
                source,
                symbolValidator: validator,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Null(type.GetMember(".cctor"));
            }
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_06()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
    public int y;
}

class C
{
    static S field1 = default;
    static S field2 = default(S);
    static S field3 = new S();
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for these initializers.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_08()
        {
            string source = @"
#nullable enable
class C
{
    static int x = 1;
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  stsfld     ""int C.x""
  IL_0006:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_09()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static S? s1 = null;
    static S? s2 = default(S?);
}";
            CompileAndVerify(
                source,
                symbolValidator: validator,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Null(type.GetMember(".cctor"));
            }
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_10()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static S? s1 = default;
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for these initializers.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_11()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static S? s1 = new S?();
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for these initializers.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_12()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static S? s1 = default(S);
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  newobj     ""S?..ctor(S)""
  IL_000e:  stsfld     ""S? C.s1""
  IL_0013:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_13()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static S? s1 = new S();
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  newobj     ""S?..ctor(S)""
  IL_000e:  stsfld     ""S? C.s1""
  IL_0013:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_14()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static object s1 = default(S);
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  box        ""S""
  IL_000e:  stsfld     ""object C.s1""
  IL_0013:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_15()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static object s1 = new S();
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  box        ""S""
  IL_000e:  stsfld     ""object C.s1""
  IL_0013:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_16()
        {
            string source = @"
#nullable enable

struct S
{
    public int x;
}

class C
{
    static object s1 = default(S?);
    static object s2 = (S?)null;
    static object s3 = new S?();
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for these initializers.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SkipSynthesizedStaticConstructor_17()
        {
            string source = @"
unsafe class C
{
    static System.IntPtr s1 = (System.IntPtr)0;
    static System.UIntPtr s2 = (System.UIntPtr)0;
    static void* s3 = (void*)0;
}";
            // note: we could make the synthesized constructor smarter and realize that
            // nothing needs to be emitted for the `(void*)0` initializer.
            // but it doesn't serve any realistic scenarios at this time.
            CompileAndVerify(source, options: TestOptions.UnsafeDebugDll, verify: Verification.Skipped).VerifyIL("C..cctor()", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0006:  stsfld     ""System.IntPtr C.s1""
  IL_000b:  ldc.i4.0
  IL_000c:  conv.i8
  IL_000d:  call       ""System.UIntPtr System.UIntPtr.op_Explicit(ulong)""
  IL_0012:  stsfld     ""System.UIntPtr C.s2""
  IL_0017:  ldc.i4.0
  IL_0018:  conv.i
  IL_0019:  stsfld     ""void* C.s3""
  IL_001e:  ret
}");
        }

        [WorkItem(543606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543606")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void StaticNullInitializerHasNoEffectOnTypeIL()
        {
            var source1 = @"
#nullable enable
class C
{
    static string s1;
}";

            var source2 = @"
#nullable enable
class C
{
    static string s1 = null!;
}";

            var expectedIL = @"
.class private auto ansi beforefieldinit C
        extends [mscorlib]System.Object
{
        // Fields
        .field private static string s1
        .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 01 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname
                instance void .ctor () cil managed
        {
                // Method begins at RVA 0x207f
                // Code size 7 (0x7)
                .maxstack 8
                IL_0000: ldarg.0
                IL_0001: call instance void [mscorlib]System.Object::.ctor()
                IL_0006: ret
        } // end of method C::.ctor
} // end of class C
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CompileAndVerify(source1, parseOptions: parseOptions).VerifyTypeIL("C", expectedIL);
            CompileAndVerify(source2, parseOptions: parseOptions).VerifyTypeIL("C", expectedIL);
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void ExplicitStaticConstructor_01()
        {
            string source = @"
#nullable enable
class C
{
    static string x = null!;

    static C()
    {
    }
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void ExplicitStaticConstructor_02()
        {
            string source = @"
#nullable enable
class C
{
    static string x;

    static C()
    {
        x = null!;
    }
}";
            CompileAndVerify(source).VerifyIL("C..cctor()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  stsfld     ""string C.x""
  IL_0006:  ret
}");
        }

        [Fact, WorkItem(55797, "https://github.com/dotnet/roslyn/issues/55797")]
        public void TwoParameterlessConstructors()
        {
            string source = @"
public class C
{
    public C() : Garbage()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,12): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                //     public C() : Garbage()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(4, 12),
                // (4,18): error CS1018: Keyword 'this' or 'base' expected
                //     public C() : Garbage()
                Diagnostic(ErrorCode.ERR_ThisOrBaseExpected, "Garbage").WithLocation(4, 18),
                // (4,18): error CS1002: ; expected
                //     public C() : Garbage()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Garbage").WithLocation(4, 18),
                // (4,18): error CS1520: Method must have a return type
                //     public C() : Garbage()
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "Garbage").WithLocation(4, 18),
                // (4,18): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
                //     public C() : Garbage()
                Diagnostic(ErrorCode.ERR_AmbigCall, "").WithArguments("C.C()", "C.C()").WithLocation(4, 18)
                );
        }

        [Fact, WorkItem(55797, "https://github.com/dotnet/roslyn/issues/55797")]
        public void TwoParameterlessConstructors_2()
        {
            string source = @"
public class C
{
    public C() : this()
    {
    }
    public C()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,18): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
                //     public C() : this()
                Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("C.C()", "C.C()").WithLocation(4, 18),
                // (7,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C()
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(7, 12)
                );
        }

        [Fact, WorkItem(55797, "https://github.com/dotnet/roslyn/issues/55797")]
        public void TwoParameterlessConstructors_3()
        {
            string source = @"
public class C
{
    public C() : this()
    {
    }
    public C2()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,18): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
                //     public C() : this()
                Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("C.C()", "C.C()").WithLocation(4, 18),
                // (7,12): error CS1520: Method must have a return type
                //     public C2()
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "C2").WithLocation(7, 12)
                );
        }

        [Fact, WorkItem(55797, "https://github.com/dotnet/roslyn/issues/55797")]
        public void TwoParameterlessConstructors_Struct()
        {
            string source = @"
public struct C
{
    public C() : this()
    {
    }
    public C2()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,18): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
                //     public C() : this()
                Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("C.C()", "C.C()").WithLocation(4, 18),
                // (7,12): error CS1520: Method must have a return type
                //     public C2()
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "C2").WithLocation(7, 12)
                );
        }
    }
}
