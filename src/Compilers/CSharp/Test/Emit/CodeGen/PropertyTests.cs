// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class PropertyTests : EmitMetadataTestBase
    {
        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBodedProperty()
        {
            var source = @"
class C
{
    public int x;
    public int X
    {
        set => x = value;
        get => x;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("C.X.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.X.set", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.x""
  IL_0007:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82122")]
        public void PropertyIsReadOnly_WithTypeArgumentFromUnrelatedAssembly()
        {
            string source1 = """
                public class C0<T>
                {
                    public virtual int P { get; set; }
                }

                public class C1<T> : C0<T>
                {
                    public override int P { get; set; }
                }
                """;
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();

            string source2 = """
                internal class C2 { }

                internal class C3 : C1<C2>
                {
                    public override int P { get; set; }
                }
                """;

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics();

            // Get the property symbol and check IsReadOnly
            var c3 = comp2.GlobalNamespace.GetTypeMember("C3");
            var property = c3.BaseTypeNoUseSiteDiagnostics.GetMember<PropertySymbol>("P");

            // These should not throw an assertion
            Assert.False(property.IsReadOnly);
            Assert.False(property.IsWriteOnly);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86184")]
        public void ReadOnlyTarget_01()
        {
            var text1 = @"
public struct S1
{
    public int F1;
    public readonly int P 
    {
        get
        {
            System.Console.Write($""get:{F1} "");
            Program.F.F1++;
            return 0;
        }
        set
        {
            System.Console.Write($""set:{F1} "");
            Program.F.F1++;
        }
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 3 };
        Test2(ref F);
        System.Console.Write($""final:{F.F1}"");

        System.Console.WriteLine();

        F = new S1 { F1 = 3 };
        Test3(in F);
        System.Console.Write($""final:{F.F1}"");

        System.Console.WriteLine();

        F = new S1 { F1 = 3 };
        Test4(ref F);
        System.Console.Write($""final:{F.F1}"");
    }

    static void Test2(ref S1 f)
    {
        f.P += GetValue();
    }

    static void Test3(in S1 f)
    {
        f.P += GetValue();
    }

    static void Test4(ref readonly S1 f)
    {
        f.P += GetValue();
    }

    public static int GetValue() { System.Console.Write($""GetValue:{Program.F.F1} ""); Program.F.F1++; return 1; }
}
";
            var comp1 = CreateCompilation(text1, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp1, expectedOutput: @"
get:3 GetValue:4 set:5 final:6
get:3 GetValue:4 set:5 final:6
get:3 GetValue:4 set:5 final:6
").VerifyDiagnostics();

            var expectedIL = @"
{
  // Code size       21 (0x15)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  call       ""readonly int S1.P.get""
  IL_0008:  call       ""int Program.GetValue()""
  IL_000d:  add
  IL_000e:  call       ""readonly void S1.P.set""
  IL_0013:  nop
  IL_0014:  ret
}
";

            verifier.VerifyIL("Program.Test2", expectedIL);
            verifier.VerifyIL("Program.Test3", expectedIL);
            verifier.VerifyIL("Program.Test4", expectedIL);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86184")]
        public void ReadOnlyTarget_02()
        {
            var text0 = @"
public static class Helper
{
    public static unsafe void Assign(ref readonly int i, int value)
    {
        fixed (int* f2 = &i)
        {
            (*f2) = value;
        }
    }
}
";
            var comp0 = CreateCompilation(text0, options: TestOptions.DebugDll.WithAllowUnsafe(true));

            var text1 = @"
public struct S1
{
    public int F1;
    public int F2;
    public readonly int P 
    {
        get
        {
            System.Console.Write($""get:{F1} {F2} "");
            Program.F.F1++;
            Helper.Assign(in F2, 5);
            return 0;
        }
        set
        {
            System.Console.Write($""set:{F1} {F2} "");
            Program.F.F1++;
        }
    }
}

class Program
{
    public static S1 F;

    static void Main()
    {
        F = new S1 { F1 = 3 };
        Test2();
        System.Console.Write($""final:{F.F1}"");
    }

    static void Test2()
    {
        GetTarget().P += GetValue();
    }

    public static S1 GetTarget() { return F; }
    public static int GetValue() { System.Console.Write($""GetValue:{Program.F.F1} ""); Program.F.F1++; return 1; }
}
";
            var comp1 = CreateCompilation(text1, references: [comp0.EmitToImageReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp1, expectedOutput: @"get:3 0 GetValue:4 set:3 5 final:6").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (S1 V_0)
  IL_0000:  nop
  IL_0001:  call       ""S1 Program.GetTarget()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       ""readonly int S1.P.get""
  IL_0010:  call       ""int Program.GetValue()""
  IL_0015:  add
  IL_0016:  call       ""readonly void S1.P.set""
  IL_001b:  nop
  IL_001c:  ret
}
");
        }

        [Fact]
        public void ReadOnlyTarget_03()
        {
            var text1 = @"
interface I1
{
    int P { get; set; }
}

public class C0 : I1
{
    public int P 
    {
        get
        {
            System.Console.Write(""get "");
            return 0;
        }
        set
        {
            System.Console.Write(""set "");
        }
    }
}

public class C1 : C0;

class Program
{
    static void Main()
    {
        Test2<C0>(new C1[] {new C1()});
    }

    static void Test2<T>(T[] array) where T : I1
    {
        array[GetIndex()].P += GetValue();
    }

    public static int GetIndex() { System.Console.Write($""GetIndex ""); return 0; }
    public static int GetValue() { System.Console.Write($""GetValue ""); return 1; }
}
";
            var comp1 = CreateCompilation(text1, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp1, expectedOutput: @"GetIndex get GetValue set").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test2<T>", @"
{
  // Code size       78 (0x4e)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T& V_2,
                T V_3,
                T V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  call       ""int Program.GetIndex()""
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_4
  IL_000b:  initobj    ""T""
  IL_0011:  ldloc.s    V_4
  IL_0013:  box        ""T""
  IL_0018:  brtrue.s   IL_0026
  IL_001a:  ldloc.0
  IL_001b:  ldloc.1
  IL_001c:  ldelem     ""T""
  IL_0021:  stloc.3
  IL_0022:  ldloca.s   V_3
  IL_0024:  br.s       IL_002d
  IL_0026:  ldloc.0
  IL_0027:  ldloc.1
  IL_0028:  ldelema    ""T""
  IL_002d:  stloc.2
  IL_002e:  ldloc.2
  IL_002f:  ldloc.2
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""int I1.P.get""
  IL_003b:  call       ""int Program.GetValue()""
  IL_0040:  add
  IL_0041:  constrained. ""T""
  IL_0047:  callvirt   ""void I1.P.set""
  IL_004c:  nop
  IL_004d:  ret
}
");
        }
    }
}
