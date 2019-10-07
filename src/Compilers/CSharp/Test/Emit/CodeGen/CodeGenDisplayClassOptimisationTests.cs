using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenDisplayClassOptimizationTests : CSharpTestBase
    {
        private static void VerifyTypeIL(CompilationVerifier compilation, string typeName, string expected)
        {
            // .Net Core has different assemblies for the same standard library types as .Net Framework, meaning that that the emitted output will be different to the expected if we run them .Net Core
            // Since we do not expect there to be any meaningful differences between output for .Net Core and .Net Framework, we will skip these tests on .Net Core
            if (ExecutionConditionUtil.IsDesktop)
            {
                compilation.VerifyTypeIL(typeName, expected);
            }
        }

        [Fact]
        public void WhenOptimisationsAreEnabled_MergeDisplayClasses()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main()
    {
        int a = 1;
        {
            int b = 2;
            Func<int> c = () => a + b;
            Console.WriteLine(c());
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Release));

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x207a
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance int32 '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2082
			// Code size 14 (0xe)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 41 (0x29)
		.maxstack 8
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.1
		IL_0007: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000c: dup
		IL_000d: ldc.i4.2
		IL_000e: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0013: ldftn instance int32 Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_0019: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_001e: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_0023: call void [mscorlib]System.Console::WriteLine(int32)
		IL_0028: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void WhenOptimisationsAreDisabled_DoNotMergeDisplayClasses()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main()
    {
        int a = 1;
        {
            int b = 2;
            Func<int> c = () => a + b;
            Console.WriteLine(c());
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Debug));

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209a
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209a
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance int32 '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20a3
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 62 (0x3e)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class Program/'<>c__DisplayClass0_1',
			[2] class [mscorlib]System.Func`1<int32>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: nop
		IL_0007: ldloc.0
		IL_0008: ldc.i4.1
		IL_0009: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000e: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: ldloc.0
		IL_0016: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_001b: nop
		IL_001c: ldloc.1
		IL_001d: ldc.i4.2
		IL_001e: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0023: ldloc.1
		IL_0024: ldftn instance int32 Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_002a: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_002f: stloc.2
		IL_0030: ldloc.2
		IL_0031: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
		IL_0036: call void [mscorlib]System.Console::WriteLine(int32)
		IL_003b: nop
		IL_003c: nop
		IL_003d: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForWithBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main()
    {
        var actions = new List<Action>();
        var strings = new List<string>() { ""one"", ""two"", ""three"" };

        for (var i = 0; i < strings.Count; i++)
        {
            int x = i;
            actions.Add(() => { Console.WriteLine(strings[i - x - 1]); });
        }

        actions[0]();
        actions[1]();
        actions[2]();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"three
two
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2115
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2115
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x211d
			// Code size 42 (0x2a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_001c: sub
			IL_001d: ldc.i4.1
			IL_001e: sub
			IL_001f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0024: call void [mscorlib]System.Console::WriteLine(string)
			IL_0029: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 185 (0xb9)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class Program/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldloc.0
		IL_0039: ldc.i4.0
		IL_003a: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003f: br.s IL_0081
		// loop start (head: IL_0081)
			IL_0041: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0046: stloc.2
			IL_0047: ldloc.2
			IL_0048: ldloc.0
			IL_0049: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_004e: ldloc.2
			IL_004f: ldloc.2
			IL_0050: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0055: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_005a: stfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_005f: ldloc.1
			IL_0060: ldloc.2
			IL_0061: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0067: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_006c: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0071: ldloc.0
			IL_0072: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0077: stloc.3
			IL_0078: ldloc.0
			IL_0079: ldloc.3
			IL_007a: ldc.i4.1
			IL_007b: add
			IL_007c: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0081: ldloc.0
			IL_0082: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0087: ldloc.0
			IL_0088: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_008d: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
			IL_0092: blt.s IL_0041
		// end loop
		IL_0094: ldloc.1
		IL_0095: ldc.i4.0
		IL_0096: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_009b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00a0: ldloc.1
		IL_00a1: ldc.i4.1
		IL_00a2: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00a7: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00ac: ldloc.1
		IL_00ad: ldc.i4.2
		IL_00ae: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00b3: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00b8: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForInsideWhileCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
class C
{
    public static void Main()
    {
        int x = 0;
        int y = 0;
        while (y < 10)
        {
            for (int i = 0; i < 10; i++)
            {
                Func<int> f = () => i + x;
            }
            y++;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "C", @"
.class private auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a7
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public class C/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a7
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance int32 '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20af
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<>c__DisplayClass0_1'::i
			IL_0006: ldarg.0
			IL_0007: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000c: ldfld int32 C/'<>c__DisplayClass0_0'::x
			IL_0011: add
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 75 (0x4b)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0',
			[1] int32,
			[2] class C/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::x
		IL_000d: ldc.i4.0
		IL_000e: stloc.1
		IL_000f: br.s IL_0045
		// loop start (head: IL_0045)
			IL_0011: newobj instance void C/'<>c__DisplayClass0_1'::.ctor()
			IL_0016: stloc.2
			IL_0017: ldloc.2
			IL_0018: ldloc.0
			IL_0019: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_001e: ldloc.2
			IL_001f: ldc.i4.0
			IL_0020: stfld int32 C/'<>c__DisplayClass0_1'::i
			IL_0025: br.s IL_0037
			// loop start (head: IL_0037)
				IL_0027: ldloc.2
				IL_0028: ldfld int32 C/'<>c__DisplayClass0_1'::i
				IL_002d: stloc.3
				IL_002e: ldloc.2
				IL_002f: ldloc.3
				IL_0030: ldc.i4.1
				IL_0031: add
				IL_0032: stfld int32 C/'<>c__DisplayClass0_1'::i
				IL_0037: ldloc.2
				IL_0038: ldfld int32 C/'<>c__DisplayClass0_1'::i
				IL_003d: ldc.i4.s 10
				IL_003f: blt.s IL_0027
			// end loop
			IL_0041: ldloc.1
			IL_0042: ldc.i4.1
			IL_0043: add
			IL_0044: stloc.1
			IL_0045: ldloc.1
			IL_0046: ldc.i4.s 10
			IL_0048: blt.s IL_0011
		// end loop
		IL_004a: ret
	} // end of method C::Main
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a7
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
} // end of class C");
        }

        [Fact]
        public void ForInsideEmptyForCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
class C
{
    public static void Main()
    {
        int x = 0;
        int y = 0;
        for(;;)
        {
            for (int i = 0; i < 10; i++)
            {
                Func<int> f = () => i + x;
            }
            y++;
            break;
        }
    }
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "C", @"
.class private auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public class C/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance int32 '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20a8
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<>c__DisplayClass0_1'::i
			IL_0006: ldarg.0
			IL_0007: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000c: ldfld int32 C/'<>c__DisplayClass0_0'::x
			IL_0011: add
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 68 (0x44)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0',
			[1] int32,
			[2] class C/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::x
		IL_000d: ldc.i4.0
		IL_000e: stloc.1
		IL_000f: newobj instance void C/'<>c__DisplayClass0_1'::.ctor()
		IL_0014: stloc.2
		IL_0015: ldloc.2
		IL_0016: ldloc.0
		IL_0017: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_001c: ldloc.2
		IL_001d: ldc.i4.0
		IL_001e: stfld int32 C/'<>c__DisplayClass0_1'::i
		IL_0023: br.s IL_0035
		// loop start (head: IL_0035)
			IL_0025: ldloc.2
			IL_0026: ldfld int32 C/'<>c__DisplayClass0_1'::i
			IL_002b: stloc.3
			IL_002c: ldloc.2
			IL_002d: ldloc.3
			IL_002e: ldc.i4.1
			IL_002f: add
			IL_0030: stfld int32 C/'<>c__DisplayClass0_1'::i
			IL_0035: ldloc.2
			IL_0036: ldfld int32 C/'<>c__DisplayClass0_1'::i
			IL_003b: ldc.i4.s 10
			IL_003d: blt.s IL_0025
		// end loop
		IL_003f: ldloc.1
		IL_0040: ldc.i4.1
		IL_0041: add
		IL_0042: stloc.1
		IL_0043: ret
	} // end of method C::Main
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a0
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
} // end of class C");
        }

        [Fact]
        public void ForWithoutBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		for (var i = 0; i < strings.Count; i++)
			actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i - x - 1])) : () => {});

		actions[0]();
		actions[1]();
		actions[2]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"three
two
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2115
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2115
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x211d
			// Code size 42 (0x2a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_001c: sub
			IL_001d: ldc.i4.1
			IL_001e: sub
			IL_001f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0024: call void [mscorlib]System.Console::WriteLine(string)
			IL_0029: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x2148
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2115
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x2154
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 185 (0xb9)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class Program/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldloc.0
		IL_0039: ldc.i4.0
		IL_003a: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003f: br.s IL_0081
		// loop start (head: IL_0081)
			IL_0041: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0046: stloc.2
			IL_0047: ldloc.2
			IL_0048: ldloc.0
			IL_0049: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_004e: ldloc.1
			IL_004f: ldloc.2
			IL_0050: ldloc.2
			IL_0051: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0056: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_005b: stfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_0060: ldloc.2
			IL_0061: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0067: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_006c: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0071: ldloc.0
			IL_0072: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0077: stloc.3
			IL_0078: ldloc.0
			IL_0079: ldloc.3
			IL_007a: ldc.i4.1
			IL_007b: add
			IL_007c: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0081: ldloc.0
			IL_0082: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0087: ldloc.0
			IL_0088: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_008d: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
			IL_0092: blt.s IL_0041
		// end loop
		IL_0094: ldloc.1
		IL_0095: ldc.i4.0
		IL_0096: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_009b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00a0: ldloc.1
		IL_00a1: ldc.i4.1
		IL_00a2: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00a7: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00ac: ldloc.1
		IL_00ad: ldc.i4.2
		IL_00ae: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00b3: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00b8: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForeachWithBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		foreach (var i in Enumerable.Range(0,3))
		{
			int x = i;
			actions.Add(() => { Console.WriteLine(strings[i - x]); });
		}

		actions[0]();
		actions[1]();
		actions[2]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"one
one
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2124
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2124
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x212c
			// Code size 35 (0x23)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::i
			IL_0011: ldarg.0
			IL_0012: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_0017: sub
			IL_0018: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_001d: call void [mscorlib]System.Console::WriteLine(string)
			IL_0022: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 183 (0xb7)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>,
			[3] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldc.i4.0
		IL_0039: ldc.i4.3
		IL_003a: call class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> [System.Core]System.Linq.Enumerable::Range(int32, int32)
		IL_003f: callvirt instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
		IL_0044: stloc.2
		.try
		{
			IL_0045: br.s IL_007e
			// loop start (head: IL_007e)
				IL_0047: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
				IL_004c: stloc.3
				IL_004d: ldloc.3
				IL_004e: ldloc.0
				IL_004f: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
				IL_0054: ldloc.3
				IL_0055: ldloc.2
				IL_0056: callvirt instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
				IL_005b: stfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0060: ldloc.3
				IL_0061: ldloc.3
				IL_0062: ldfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0067: stfld int32 Program/'<>c__DisplayClass0_1'::x
				IL_006c: ldloc.1
				IL_006d: ldloc.3
				IL_006e: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
				IL_0074: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
				IL_0079: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
				IL_007e: ldloc.2
				IL_007f: callvirt instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
				IL_0084: brtrue.s IL_0047
			// end loop
			IL_0086: leave.s IL_0092
		} // end .try
		finally
		{
			IL_0088: ldloc.2
			IL_0089: brfalse.s IL_0091
			IL_008b: ldloc.2
			IL_008c: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0091: endfinally
		} // end handler
		IL_0092: ldloc.1
		IL_0093: ldc.i4.0
		IL_0094: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0099: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_009e: ldloc.1
		IL_009f: ldc.i4.1
		IL_00a0: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00a5: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00aa: ldloc.1
		IL_00ab: ldc.i4.2
		IL_00ac: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00b1: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00b6: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForeachWithoutBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		foreach (var i in Enumerable.Range(0,3))
            actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i - x])) : () => {});

		actions[0]();
		actions[1]();
		actions[2]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"one
one
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2124
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2124
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x212c
			// Code size 35 (0x23)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::i
			IL_0011: ldarg.0
			IL_0012: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_0017: sub
			IL_0018: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_001d: call void [mscorlib]System.Console::WriteLine(string)
			IL_0022: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x2150
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2124
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x215c
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 183 (0xb7)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>,
			[3] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldc.i4.0
		IL_0039: ldc.i4.3
		IL_003a: call class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> [System.Core]System.Linq.Enumerable::Range(int32, int32)
		IL_003f: callvirt instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
		IL_0044: stloc.2
		.try
		{
			IL_0045: br.s IL_007e
			// loop start (head: IL_007e)
				IL_0047: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
				IL_004c: stloc.3
				IL_004d: ldloc.3
				IL_004e: ldloc.0
				IL_004f: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
				IL_0054: ldloc.3
				IL_0055: ldloc.2
				IL_0056: callvirt instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
				IL_005b: stfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0060: ldloc.1
				IL_0061: ldloc.3
				IL_0062: ldloc.3
				IL_0063: ldfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0068: stfld int32 Program/'<>c__DisplayClass0_1'::x
				IL_006d: ldloc.3
				IL_006e: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
				IL_0074: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
				IL_0079: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
				IL_007e: ldloc.2
				IL_007f: callvirt instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
				IL_0084: brtrue.s IL_0047
			// end loop
			IL_0086: leave.s IL_0092
		} // end .try
		finally
		{
			IL_0088: ldloc.2
			IL_0089: brfalse.s IL_0091
			IL_008b: ldloc.2
			IL_008c: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0091: endfinally
		} // end handler
		IL_0092: ldloc.1
		IL_0093: ldc.i4.0
		IL_0094: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0099: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_009e: ldloc.1
		IL_009f: ldc.i4.1
		IL_00a0: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00a5: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00aa: ldloc.1
		IL_00ab: ldc.i4.2
		IL_00ac: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00b1: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00b6: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [CompilerTrait(CompilerFeature.AsyncStreams)]
        [Fact]
        public void AwaitForeachCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;
using System.Linq;

public class C
{
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }

    public sealed class Enumerator
    {
        public int Current { get; }

        public System.Threading.Tasks.Task<bool> MoveNextAsync() => null;
    }
}

public class Program
{
	public static async void M(C enumerable)
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		await foreach (var i in enumerable)
            actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i - x])) : () => {});

		actions[0]();
		actions[1]();
		actions[2]();
	}
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2053
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2053
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<M>b__0' () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 35 (0x23)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::i
			IL_0011: ldarg.0
			IL_0012: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_0017: sub
			IL_0018: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_001d: call void [mscorlib]System.Console::WriteLine(string)
			IL_0022: ret
		} // end of method '<>c__DisplayClass0_1'::'<M>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x20c4
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2053
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<M>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x20d0
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<M>b__0_1'
	} // end of class <>c
	.class nested private auto ansi sealed beforefieldinit '<M>d__0'
		extends [mscorlib]System.ValueType
		implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 '<>1__state'
		.field public valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder '<>t__builder'
		.field public class C enumerable
		.field private class Program/'<>c__DisplayClass0_0' '<>8__1'
		.field private class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> '<actions>5__2'
		.field private class C/Enumerator '<>7__wrap2'
		.field private valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool> '<>u__1'
		// Methods
		.method private final hidebysig newslot virtual 
			instance void MoveNext () cil managed 
		{
			.override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
			// Method begins at RVA 0x20d4
			// Code size 388 (0x184)
			.maxstack 4
			.locals init (
				[0] int32,
				[1] valuetype [mscorlib]System.Threading.CancellationToken,
				[2] class Program/'<>c__DisplayClass0_1',
				[3] valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>,
				[4] class [mscorlib]System.Exception
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<M>d__0'::'<>1__state'
			IL_0006: stloc.0
			.try
			{
				IL_0007: ldloc.0
				IL_0008: brfalse IL_00f3
				IL_000d: ldarg.0
				IL_000e: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
				IL_0013: stfld class Program/'<>c__DisplayClass0_0' Program/'<M>d__0'::'<>8__1'
				IL_0018: ldarg.0
				IL_0019: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
				IL_001e: stfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<M>d__0'::'<actions>5__2'
				IL_0023: ldarg.0
				IL_0024: ldfld class Program/'<>c__DisplayClass0_0' Program/'<M>d__0'::'<>8__1'
				IL_0029: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
				IL_002e: dup
				IL_002f: ldstr ""one""
				IL_0034: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
				IL_0039: dup
				IL_003a: ldstr ""two""
				IL_003f: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
				IL_0044: dup
				IL_0045: ldstr ""three""
				IL_004a: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
				IL_004f: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
				IL_0054: ldarg.0
				IL_0055: ldarg.0
				IL_0056: ldfld class C Program/'<M>d__0'::enumerable
				IL_005b: ldloca.s 1
				IL_005d: initobj [mscorlib]System.Threading.CancellationToken
				IL_0063: ldloc.1
				IL_0064: callvirt instance class C/Enumerator C::GetAsyncEnumerator(valuetype [mscorlib]System.Threading.CancellationToken)
				IL_0069: stfld class C/Enumerator Program/'<M>d__0'::'<>7__wrap2'
				IL_006e: br.s IL_00b6
				IL_0070: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
				IL_0075: stloc.2
				IL_0076: ldloc.2
				IL_0077: ldarg.0
				IL_0078: ldfld class Program/'<>c__DisplayClass0_0' Program/'<M>d__0'::'<>8__1'
				IL_007d: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
				IL_0082: ldloc.2
				IL_0083: ldarg.0
				IL_0084: ldfld class C/Enumerator Program/'<M>d__0'::'<>7__wrap2'
				IL_0089: callvirt instance int32 C/Enumerator::get_Current()
				IL_008e: stfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0093: ldarg.0
				IL_0094: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<M>d__0'::'<actions>5__2'
				IL_0099: ldloc.2
				IL_009a: ldloc.2
				IL_009b: ldfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_00a0: stfld int32 Program/'<>c__DisplayClass0_1'::x
				IL_00a5: ldloc.2
				IL_00a6: ldftn instance void Program/'<>c__DisplayClass0_1'::'<M>b__0'()
				IL_00ac: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
				IL_00b1: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
				IL_00b6: ldarg.0
				IL_00b7: ldfld class C/Enumerator Program/'<M>d__0'::'<>7__wrap2'
				IL_00bc: callvirt instance class [mscorlib]System.Threading.Tasks.Task`1<bool> C/Enumerator::MoveNextAsync()
				IL_00c1: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [mscorlib]System.Threading.Tasks.Task`1<bool>::GetAwaiter()
				IL_00c6: stloc.3
				IL_00c7: ldloca.s 3
				IL_00c9: call instance bool valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>::get_IsCompleted()
				IL_00ce: brtrue.s IL_010f
				IL_00d0: ldarg.0
				IL_00d1: ldc.i4.0
				IL_00d2: dup
				IL_00d3: stloc.0
				IL_00d4: stfld int32 Program/'<M>d__0'::'<>1__state'
				IL_00d9: ldarg.0
				IL_00da: ldloc.3
				IL_00db: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool> Program/'<M>d__0'::'<>u__1'
				IL_00e0: ldarg.0
				IL_00e1: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
				IL_00e6: ldloca.s 3
				IL_00e8: ldarg.0
				IL_00e9: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>, valuetype Program/'<M>d__0'>(!!0&, !!1&)
				IL_00ee: leave IL_0183
				IL_00f3: ldarg.0
				IL_00f4: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool> Program/'<M>d__0'::'<>u__1'
				IL_00f9: stloc.3
				IL_00fa: ldarg.0
				IL_00fb: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool> Program/'<M>d__0'::'<>u__1'
				IL_0100: initobj valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>
				IL_0106: ldarg.0
				IL_0107: ldc.i4.m1
				IL_0108: dup
				IL_0109: stloc.0
				IL_010a: stfld int32 Program/'<M>d__0'::'<>1__state'
				IL_010f: ldloca.s 3
				IL_0111: call instance !0 valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<bool>::GetResult()
				IL_0116: brtrue IL_0070
				IL_011b: ldarg.0
				IL_011c: ldnull
				IL_011d: stfld class C/Enumerator Program/'<M>d__0'::'<>7__wrap2'
				IL_0122: ldarg.0
				IL_0123: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<M>d__0'::'<actions>5__2'
				IL_0128: ldc.i4.0
				IL_0129: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
				IL_012e: callvirt instance void [mscorlib]System.Action::Invoke()
				IL_0133: ldarg.0
				IL_0134: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<M>d__0'::'<actions>5__2'
				IL_0139: ldc.i4.1
				IL_013a: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
				IL_013f: callvirt instance void [mscorlib]System.Action::Invoke()
				IL_0144: ldarg.0
				IL_0145: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<M>d__0'::'<actions>5__2'
				IL_014a: ldc.i4.2
				IL_014b: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
				IL_0150: callvirt instance void [mscorlib]System.Action::Invoke()
				IL_0155: leave.s IL_0170
			} // end .try
			catch [mscorlib]System.Exception
			{
				IL_0157: stloc.s 4
				IL_0159: ldarg.0
				IL_015a: ldc.i4.s -2
				IL_015c: stfld int32 Program/'<M>d__0'::'<>1__state'
				IL_0161: ldarg.0
				IL_0162: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
				IL_0167: ldloc.s 4
				IL_0169: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::SetException(class [mscorlib]System.Exception)
				IL_016e: leave.s IL_0183
			} // end handler
			IL_0170: ldarg.0
			IL_0171: ldc.i4.s -2
			IL_0173: stfld int32 Program/'<M>d__0'::'<>1__state'
			IL_0178: ldarg.0
			IL_0179: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
			IL_017e: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::SetResult()
			IL_0183: ret
		} // end of method '<M>d__0'::MoveNext
		.method private final hidebysig newslot virtual 
			instance void SetStateMachine (
				class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
			) cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
			// Method begins at RVA 0x2280
			// Code size 13 (0xd)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
			IL_0006: ldarg.1
			IL_0007: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
			IL_000c: ret
		} // end of method '<M>d__0'::SetStateMachine
	} // end of class <M>d__0
	// Methods
	.method public hidebysig static 
		void M (
			class C enumerable
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
			01 00 0f 50 72 6f 67 72 61 6d 2b 3c 4d 3e 64 5f
			5f 30 00 00
		)
		// Method begins at RVA 0x205c
		// Code size 45 (0x2d)
		.maxstack 2
		.locals init (
			[0] valuetype Program/'<M>d__0',
			[1] valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder
		)
		IL_0000: ldloca.s 0
		IL_0002: ldarg.0
		IL_0003: stfld class C Program/'<M>d__0'::enumerable
		IL_0008: ldloca.s 0
		IL_000a: call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::Create()
		IL_000f: stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
		IL_0014: ldloca.s 0
		IL_0016: ldc.i4.m1
		IL_0017: stfld int32 Program/'<M>d__0'::'<>1__state'
		IL_001c: ldloc.0
		IL_001d: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program/'<M>d__0'::'<>t__builder'
		IL_0022: stloc.1
		IL_0023: ldloca.s 1
		IL_0025: ldloca.s 0
		IL_0027: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncVoidMethodBuilder::Start<valuetype Program/'<M>d__0'>(!!0&)
		IL_002c: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2053
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void IfWithBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one""};

		if (0 is int i)
		{
			var x = i;
			actions.Add(() => { Console.WriteLine(strings[i + x]); });
		}

		actions[0]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: "one");


            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20b8
			// Code size 30 (0x1e)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_000c: ldarg.0
			IL_000d: ldfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0012: add
			IL_0013: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0018: call void [mscorlib]System.Console::WriteLine(string)
			IL_001d: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 84 (0x54)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0022: ldloc.0
		IL_0023: ldc.i4.0
		IL_0024: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0029: ldloc.0
		IL_002a: ldloc.0
		IL_002b: ldfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0030: stfld int32 Program/'<>c__DisplayClass0_0'::x
		IL_0035: ldloc.1
		IL_0036: ldloc.0
		IL_0037: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_003d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0042: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
		IL_0047: ldloc.1
		IL_0048: ldc.i4.0
		IL_0049: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_004e: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0053: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void IfWithoutBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one""};

		if (0 is int i)
			actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i + x])) : () => {});

		actions[0]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: "one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20b8
			// Code size 30 (0x1e)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_000c: ldarg.0
			IL_000d: ldfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0012: add
			IL_0013: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0018: call void [mscorlib]System.Console::WriteLine(string)
			IL_001d: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x20d7
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x20e3
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 84 (0x54)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0022: ldloc.0
		IL_0023: ldc.i4.0
		IL_0024: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0029: ldloc.1
		IL_002a: ldloc.0
		IL_002b: ldloc.0
		IL_002c: ldfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0031: stfld int32 Program/'<>c__DisplayClass0_0'::x
		IL_0036: ldloc.0
		IL_0037: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_003d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0042: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
		IL_0047: ldloc.1
		IL_0048: ldc.i4.0
		IL_0049: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_004e: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0053: ret
	} // end of method Program::Main
} // end of class Program
");
        }

        [Fact]
        public void ElseWithoutBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one""};

        if(true)
    		if (!(0 is int i) || strings[0] != ""one"")
                throw new Exception();
            else
			    actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i + x])) : () => {});

		actions[0]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: "one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20ce
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20d6
			// Code size 30 (0x1e)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_000c: ldarg.0
			IL_000d: ldfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0012: add
			IL_0013: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0018: call void [mscorlib]System.Console::WriteLine(string)
			IL_001d: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x20f5
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20ce
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x2101
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 114 (0x72)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0022: ldloc.0
		IL_0023: ldc.i4.0
		IL_0024: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0029: ldloc.0
		IL_002a: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_002f: ldc.i4.0
		IL_0030: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
		IL_0035: ldstr ""one""
		IL_003a: call bool [mscorlib]System.String::op_Inequality(string, string)
		IL_003f: brfalse.s IL_0047
		IL_0041: newobj instance void [mscorlib]System.Exception::.ctor()
		IL_0046: throw
		IL_0047: ldloc.1
		IL_0048: ldloc.0
		IL_0049: ldloc.0
		IL_004a: ldfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_004f: stfld int32 Program/'<>c__DisplayClass0_0'::x
		IL_0054: ldloc.0
		IL_0055: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_005b: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0060: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
		IL_0065: ldloc.1
		IL_0066: ldc.i4.0
		IL_0067: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_006c: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0071: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void UsingWithBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"" };

		using (var disposable = new Disposable())
		{
			var i = 0;
			actions.Add(() => { Console.WriteLine(disposable.ToString()); Console.WriteLine(strings[i]); });
		}

		actions[0]();
	}

	public class Disposable : IDisposable
	{
		public void Dispose(){}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"Program+Disposable
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested public auto ansi beforefieldinit Disposable
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		// Methods
		.method public final hidebysig newslot virtual 
			instance void Dispose () cil managed 
		{
			// Method begins at RVA 0x20d8
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method Disposable::Dispose
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20da
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method Disposable::.ctor
	} // end of class Disposable
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public class Program/Disposable disposable
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20da
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20e2
			// Code size 39 (0x27)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_0006: callvirt instance string [mscorlib]System.Object::ToString()
			IL_000b: call void [mscorlib]System.Console::WriteLine(string)
			IL_0010: ldarg.0
			IL_0011: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_001c: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0021: call void [mscorlib]System.Console::WriteLine(string)
			IL_0026: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 105 (0x69)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0022: ldloc.0
		IL_0023: newobj instance void Program/Disposable::.ctor()
		IL_0028: stfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
		.try
		{
			IL_002d: ldloc.0
			IL_002e: ldc.i4.0
			IL_002f: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0034: ldloc.1
			IL_0035: ldloc.0
			IL_0036: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
			IL_003c: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_0041: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0046: leave.s IL_005c
		} // end .try
		finally
		{
			IL_0048: ldloc.0
			IL_0049: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_004e: brfalse.s IL_005b
			IL_0050: ldloc.0
			IL_0051: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_0056: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_005b: endfinally
		} // end handler
		IL_005c: ldloc.1
		IL_005d: ldc.i4.0
		IL_005e: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0063: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0068: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void UsingWithoutBlockCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"" };

		using (var disposable = new Disposable())
			actions.Add(0 is int i ? (Action)(() => { Console.WriteLine(disposable.ToString()); Console.WriteLine(strings[i]); }) : () => {});

		actions[0]();
	}


	public class Disposable : IDisposable
	{
		public void Dispose(){}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"Program+Disposable
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested public auto ansi beforefieldinit Disposable
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		// Methods
		.method public final hidebysig newslot virtual 
			instance void Dispose () cil managed 
		{
			// Method begins at RVA 0x20d8
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method Disposable::Dispose
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20da
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method Disposable::.ctor
	} // end of class Disposable
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public class Program/Disposable disposable
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20da
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20e2
			// Code size 39 (0x27)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_0006: callvirt instance string [mscorlib]System.Object::ToString()
			IL_000b: call void [mscorlib]System.Console::WriteLine(string)
			IL_0010: ldarg.0
			IL_0011: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_001c: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0021: call void [mscorlib]System.Console::WriteLine(string)
			IL_0026: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x210a
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20da
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x20d8
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 105 (0x69)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0022: ldloc.0
		IL_0023: newobj instance void Program/Disposable::.ctor()
		IL_0028: stfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
		.try
		{
			IL_002d: ldloc.1
			IL_002e: ldloc.0
			IL_002f: ldc.i4.0
			IL_0030: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0035: ldloc.0
			IL_0036: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
			IL_003c: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_0041: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0046: leave.s IL_005c
		} // end .try
		finally
		{
			IL_0048: ldloc.0
			IL_0049: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_004e: brfalse.s IL_005b
			IL_0050: ldloc.0
			IL_0051: ldfld class Program/Disposable Program/'<>c__DisplayClass0_0'::disposable
			IL_0056: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_005b: endfinally
		} // end handler
		IL_005c: ldloc.1
		IL_005d: ldc.i4.0
		IL_005e: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0063: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0068: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void IfInUsingInForeachInForCorrectDisplayClassesAreCreated()
        {
            var source =
    @"using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		
	    foreach(var i in Enumerable.Range(0,1))
            for (var j = 0; j < strings.Count; j++)
				using (var disposable = new Disposable())
					if(j is int x)
						actions.Add(0 is int y ? (Action)(() =>
						{
							Console.WriteLine(disposable.ToString());
							Console.WriteLine(strings[j - x - 1 + i + y]);
						}) : () => { });

		actions[0]();
		actions[1]();
		actions[2]();
	}

	public class Disposable : IDisposable
	{
		public void Dispose() { }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"Program+Disposable
three
Program+Disposable
two
Program+Disposable
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested public auto ansi beforefieldinit Disposable
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		// Methods
		.method public final hidebysig newslot virtual 
			instance void Dispose () cil managed 
		{
			// Method begins at RVA 0x21b0
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method Disposable::Dispose
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x21b2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method Disposable::.ctor
	} // end of class Disposable
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x21b2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 i
		.field public int32 j
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x21b2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_2'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class Program/Disposable disposable
		.field public int32 x
		.field public int32 y
		.field public class Program/'<>c__DisplayClass0_1' 'CS$<>8__locals2'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x21b2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_2'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x21bc
			// Code size 82 (0x52)
			.maxstack 3
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/Disposable Program/'<>c__DisplayClass0_2'::disposable
			IL_0006: callvirt instance string [mscorlib]System.Object::ToString()
			IL_000b: call void [mscorlib]System.Console::WriteLine(string)
			IL_0010: ldarg.0
			IL_0011: ldfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0016: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_001b: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_0020: ldarg.0
			IL_0021: ldfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0026: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_002b: ldarg.0
			IL_002c: ldfld int32 Program/'<>c__DisplayClass0_2'::x
			IL_0031: sub
			IL_0032: ldc.i4.1
			IL_0033: sub
			IL_0034: ldarg.0
			IL_0035: ldfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_003a: ldfld int32 Program/'<>c__DisplayClass0_1'::i
			IL_003f: add
			IL_0040: ldarg.0
			IL_0041: ldfld int32 Program/'<>c__DisplayClass0_2'::y
			IL_0046: add
			IL_0047: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_004c: call void [mscorlib]System.Console::WriteLine(string)
			IL_0051: ret
		} // end of method '<>c__DisplayClass0_2'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_2
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x221a
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x21b2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x21b0
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 310 (0x136)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>,
			[3] class Program/'<>c__DisplayClass0_1',
			[4] class Program/'<>c__DisplayClass0_2',
			[5] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldc.i4.0
		IL_0039: ldc.i4.1
		IL_003a: call class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> [System.Core]System.Linq.Enumerable::Range(int32, int32)
		IL_003f: callvirt instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
		IL_0044: stloc.2
		.try
		{
			IL_0045: br IL_00fa
			// loop start (head: IL_00fa)
				IL_004a: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
				IL_004f: stloc.3
				IL_0050: ldloc.3
				IL_0051: ldloc.0
				IL_0052: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
				IL_0057: ldloc.3
				IL_0058: ldloc.2
				IL_0059: callvirt instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
				IL_005e: stfld int32 Program/'<>c__DisplayClass0_1'::i
				IL_0063: ldloc.3
				IL_0064: ldc.i4.0
				IL_0065: stfld int32 Program/'<>c__DisplayClass0_1'::j
				IL_006a: br.s IL_00df
				// loop start (head: IL_00df)
					IL_006c: newobj instance void Program/'<>c__DisplayClass0_2'::.ctor()
					IL_0071: stloc.s 4
					IL_0073: ldloc.s 4
					IL_0075: ldloc.3
					IL_0076: stfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
					IL_007b: ldloc.s 4
					IL_007d: newobj instance void Program/Disposable::.ctor()
					IL_0082: stfld class Program/Disposable Program/'<>c__DisplayClass0_2'::disposable
					.try
					{
						IL_0087: ldloc.s 4
						IL_0089: ldloc.s 4
						IL_008b: ldfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
						IL_0090: ldfld int32 Program/'<>c__DisplayClass0_1'::j
						IL_0095: stfld int32 Program/'<>c__DisplayClass0_2'::x
						IL_009a: ldloc.1
						IL_009b: ldloc.s 4
						IL_009d: ldc.i4.0
						IL_009e: stfld int32 Program/'<>c__DisplayClass0_2'::y
						IL_00a3: ldloc.s 4
						IL_00a5: ldftn instance void Program/'<>c__DisplayClass0_2'::'<Main>b__0'()
						IL_00ab: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
						IL_00b0: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
						IL_00b5: leave.s IL_00cd
					} // end .try
					finally
					{
						IL_00b7: ldloc.s 4
						IL_00b9: ldfld class Program/Disposable Program/'<>c__DisplayClass0_2'::disposable
						IL_00be: brfalse.s IL_00cc
						IL_00c0: ldloc.s 4
						IL_00c2: ldfld class Program/Disposable Program/'<>c__DisplayClass0_2'::disposable
						IL_00c7: callvirt instance void [mscorlib]System.IDisposable::Dispose()
						IL_00cc: endfinally
					} // end handler
					IL_00cd: ldloc.3
					IL_00ce: ldfld int32 Program/'<>c__DisplayClass0_1'::j
					IL_00d3: stloc.s 5
					IL_00d5: ldloc.3
					IL_00d6: ldloc.s 5
					IL_00d8: ldc.i4.1
					IL_00d9: add
					IL_00da: stfld int32 Program/'<>c__DisplayClass0_1'::j
					IL_00df: ldloc.3
					IL_00e0: ldfld int32 Program/'<>c__DisplayClass0_1'::j
					IL_00e5: ldloc.3
					IL_00e6: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
					IL_00eb: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
					IL_00f0: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
					IL_00f5: blt IL_006c
				// end loop
				IL_00fa: ldloc.2
				IL_00fb: callvirt instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
				IL_0100: brtrue IL_004a
			// end loop
			IL_0105: leave.s IL_0111
		} // end .try
		finally
		{
			IL_0107: ldloc.2
			IL_0108: brfalse.s IL_0110
			IL_010a: ldloc.2
			IL_010b: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0110: endfinally
		} // end handler
		IL_0111: ldloc.1
		IL_0112: ldc.i4.0
		IL_0113: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0118: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_011d: ldloc.1
		IL_011e: ldc.i4.1
		IL_011f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0124: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0129: ldloc.1
		IL_012a: ldc.i4.2
		IL_012b: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0130: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0135: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void WhileCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };

		int i = 0;
		while(i is int j && i++ < 3)
			actions.Add(0 is int x ? (Action)(() => Console.WriteLine(strings[j + x])) : () => { });

		actions[0]();
		actions[1]();
		actions[2]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"one
two
three");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20f2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 j
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20f2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20fa
			// Code size 35 (0x23)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_0011: ldarg.0
			IL_0012: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_0017: add
			IL_0018: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_001d: call void [mscorlib]System.Console::WriteLine(string)
			IL_0022: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x211e
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20f2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x212a
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 150 (0x96)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] int32,
			[3] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldc.i4.0
		IL_0039: stloc.2
		// loop start (head: IL_003a)
			IL_003a: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_003f: stloc.3
			IL_0040: ldloc.3
			IL_0041: ldloc.0
			IL_0042: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0047: ldloc.3
			IL_0048: ldloc.2
			IL_0049: stfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_004e: ldloc.2
			IL_004f: dup
			IL_0050: ldc.i4.1
			IL_0051: add
			IL_0052: stloc.2
			IL_0053: ldc.i4.3
			IL_0054: bge.s IL_0071
			IL_0056: ldloc.1
			IL_0057: ldloc.3
			IL_0058: ldc.i4.0
			IL_0059: stfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_005e: ldloc.3
			IL_005f: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0065: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_006a: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_006f: br.s IL_003a
		// end loop
		IL_0071: ldloc.1
		IL_0072: ldc.i4.0
		IL_0073: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0078: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_007d: ldloc.1
		IL_007e: ldc.i4.1
		IL_007f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0084: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0089: ldloc.1
		IL_008a: ldc.i4.2
		IL_008b: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0090: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0095: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForWithVariableDeclaredInInvocationExpressionInIteratorCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
        var actions = new List<Action>();
        var strings = new List<string>() { ""one"", ""two"", ""three"" };

		for (int i = 0; ++i < 3; actions.Add(i is int j ? (Action)(() => { Console.WriteLine(strings[i - j - 1]); }) : () => { })) ;

		actions[0]();
		actions[1]();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"two
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20fa
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 j
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20fa
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2102
			// Code size 42 (0x2a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_001c: sub
			IL_001d: ldc.i4.1
			IL_001e: sub
			IL_001f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0024: call void [mscorlib]System.Console::WriteLine(string)
			IL_0029: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x212d
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20fa
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x2139
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 158 (0x9e)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class Program/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldloc.0
		IL_0039: ldc.i4.0
		IL_003a: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003f: br.s IL_0071
		// loop start (head: IL_0071)
			IL_0041: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0046: stloc.2
			IL_0047: ldloc.2
			IL_0048: ldloc.0
			IL_0049: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_004e: ldloc.1
			IL_004f: ldloc.2
			IL_0050: ldloc.2
			IL_0051: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0056: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_005b: stfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_0060: ldloc.2
			IL_0061: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0067: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_006c: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0071: ldloc.0
			IL_0072: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0077: ldc.i4.1
			IL_0078: add
			IL_0079: stloc.3
			IL_007a: ldloc.0
			IL_007b: ldloc.3
			IL_007c: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0081: ldloc.3
			IL_0082: ldc.i4.3
			IL_0083: blt.s IL_0041
		// end loop
		IL_0085: ldloc.1
		IL_0086: ldc.i4.0
		IL_0087: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_008c: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0091: ldloc.1
		IL_0092: ldc.i4.1
		IL_0093: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0098: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_009d: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForWithVariableDeclaredInSimpleAssignmentExpressionInIteratorCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
        var strings = new List<string>() { ""one"", ""two"", ""three"" };

        Action action = null;
		for (int i = 0; ++i < 2; action = i is int j ? (Action)(() => { Console.WriteLine(strings[i - j - 1]); }) : () => { }) ;
        action();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20df
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 j
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20df
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20e7
			// Code size 42 (0x2a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_001c: sub
			IL_001d: ldc.i4.1
			IL_001e: sub
			IL_001f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0024: call void [mscorlib]System.Console::WriteLine(string)
			IL_0029: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x2112
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20df
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x211e
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 131 (0x83)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Action,
			[2] class Program/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_000c: dup
		IL_000d: ldstr ""one""
		IL_0012: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0017: dup
		IL_0018: ldstr ""two""
		IL_001d: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0022: dup
		IL_0023: ldstr ""three""
		IL_0028: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_002d: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0032: ldnull
		IL_0033: stloc.1
		IL_0034: ldloc.0
		IL_0035: ldc.i4.0
		IL_0036: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003b: br.s IL_0068
		// loop start (head: IL_0068)
			IL_003d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0042: stloc.2
			IL_0043: ldloc.2
			IL_0044: ldloc.0
			IL_0045: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_004a: ldloc.2
			IL_004b: ldloc.2
			IL_004c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0051: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0056: stfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_005b: ldloc.2
			IL_005c: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0062: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_0067: stloc.1
			IL_0068: ldloc.0
			IL_0069: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_006e: ldc.i4.1
			IL_006f: add
			IL_0070: stloc.3
			IL_0071: ldloc.0
			IL_0072: ldloc.3
			IL_0073: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0078: ldloc.3
			IL_0079: ldc.i4.2
			IL_007a: blt.s IL_003d
		// end loop
		IL_007c: ldloc.1
		IL_007d: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0082: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForWithVariableDeclaredInConditionCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var strings = new List<string>() { ""one"" };


            Action action = null;
            for (int i = 0; i is int j && null == (action = () => { Console.WriteLine(strings[i + j]); });) break; ;
            action();
        }
    }";
            var compilation = CompileAndVerify(source, expectedOutput: @"one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b5
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 j
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b5
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20bd
			// Code size 40 (0x28)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_001c: add
			IL_001d: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0022: call void [mscorlib]System.Console::WriteLine(string)
			IL_0027: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 89 (0x59)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Action,
			[2] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_000c: dup
		IL_000d: ldstr ""one""
		IL_0012: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0017: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_001c: ldnull
		IL_001d: stloc.1
		IL_001e: ldloc.0
		IL_001f: ldc.i4.0
		IL_0020: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0025: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_002a: stloc.2
		IL_002b: ldloc.2
		IL_002c: ldloc.0
		IL_002d: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0032: ldloc.2
		IL_0033: ldloc.2
		IL_0034: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0039: ldfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003e: stfld int32 Program/'<>c__DisplayClass0_1'::j
		IL_0043: ldloc.2
		IL_0044: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_004a: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_004f: dup
		IL_0050: stloc.1
		IL_0051: pop
		IL_0052: ldloc.1
		IL_0053: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0058: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ForWithVariableDeclaredInConditionAndNoneInInitializerCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var strings = new List<string>() { ""one"" };


            Action action = null;
            int i = 0;
            for (; i is int j && null == (action = () => { Console.WriteLine(strings[i + j]); });) break; ;
            action();
        }
    }";
            var compilation = CompileAndVerify(source, expectedOutput: @"one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b5
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 j
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20b5
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20bd
			// Code size 40 (0x28)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::j
			IL_001c: add
			IL_001d: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0022: call void [mscorlib]System.Console::WriteLine(string)
			IL_0027: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 89 (0x59)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Action,
			[2] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_000c: dup
		IL_000d: ldstr ""one""
		IL_0012: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0017: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_001c: ldnull
		IL_001d: stloc.1
		IL_001e: ldloc.0
		IL_001f: ldc.i4.0
		IL_0020: stfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_0025: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_002a: stloc.2
		IL_002b: ldloc.2
		IL_002c: ldloc.0
		IL_002d: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0032: ldloc.2
		IL_0033: ldloc.2
		IL_0034: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0039: ldfld int32 Program/'<>c__DisplayClass0_0'::i
		IL_003e: stfld int32 Program/'<>c__DisplayClass0_1'::j
		IL_0043: ldloc.2
		IL_0044: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_004a: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_004f: dup
		IL_0050: stloc.1
		IL_0051: pop
		IL_0052: ldloc.1
		IL_0053: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0058: ret
	} // end of method Program::Main
} // end of class Program
");
        }

        [Fact]
        public void DoWhileCorrectDisplayClassesAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		var strings = new List<string>() { ""one"", ""two"", ""three"" };


            int i = 0;
            do
                actions.Add(i is int x ? (Action)(() => Console.WriteLine(strings[i - x - 1])) : () => { });
            while (++i < 3);


            actions[0]();
            actions[1]();
            actions[2]();
        }
    }";
            var compilation = CompileAndVerify(source, expectedOutput: @"three
two
one");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<string> strings
		.field public int32 i
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2104
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2104
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x210c
			// Code size 42 (0x2a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0016: ldarg.0
			IL_0017: ldfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_001c: sub
			IL_001d: ldc.i4.1
			IL_001e: sub
			IL_001f: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
			IL_0024: call void [mscorlib]System.Console::WriteLine(string)
			IL_0029: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed serializable beforefieldinit '<>c'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public static initonly class Program/'<>c' '<>9'
		.field public static class [mscorlib]System.Action '<>9__0_1'
		// Methods
		.method private hidebysig specialname rtspecialname static 
			void .cctor () cil managed 
		{
			// Method begins at RVA 0x2137
			// Code size 11 (0xb)
			.maxstack 8
			IL_0000: newobj instance void Program/'<>c'::.ctor()
			IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
			IL_000a: ret
		} // end of method '<>c'::.cctor
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2104
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0_1' () cil managed 
		{
			// Method begins at RVA 0x2143
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<>c'::'<Main>b__0_1'
	} // end of class <>c
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 168 (0xa8)
		.maxstack 4
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>,
			[2] class Program/'<>c__DisplayClass0_1',
			[3] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stloc.1
		IL_000c: ldloc.0
		IL_000d: newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
		IL_0012: dup
		IL_0013: ldstr ""one""
		IL_0018: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_001d: dup
		IL_001e: ldstr ""two""
		IL_0023: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0028: dup
		IL_0029: ldstr ""three""
		IL_002e: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
		IL_0033: stfld class [mscorlib]System.Collections.Generic.List`1<string> Program/'<>c__DisplayClass0_0'::strings
		IL_0038: ldloc.0
		IL_0039: ldc.i4.0
		IL_003a: stfld int32 Program/'<>c__DisplayClass0_0'::i
		// loop start (head: IL_003f)
			IL_003f: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0044: stloc.2
			IL_0045: ldloc.2
			IL_0046: ldloc.0
			IL_0047: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_004c: ldloc.1
			IL_004d: ldloc.2
			IL_004e: ldloc.2
			IL_004f: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0054: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0059: stfld int32 Program/'<>c__DisplayClass0_1'::x
			IL_005e: ldloc.2
			IL_005f: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
			IL_0065: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_006a: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_006f: ldloc.0
			IL_0070: ldfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_0075: ldc.i4.1
			IL_0076: add
			IL_0077: stloc.3
			IL_0078: ldloc.0
			IL_0079: ldloc.3
			IL_007a: stfld int32 Program/'<>c__DisplayClass0_0'::i
			IL_007f: ldloc.3
			IL_0080: ldc.i4.3
			IL_0081: blt.s IL_003f
		// end loop
		IL_0083: ldloc.1
		IL_0084: ldc.i4.0
		IL_0085: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_008a: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_008f: ldloc.1
		IL_0090: ldc.i4.1
		IL_0091: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0096: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_009b: ldloc.1
		IL_009c: ldc.i4.2
		IL_009d: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_00a2: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_00a7: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsBackwardsGoToMergeDisplayClasses()
        {
            var source =
               @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
		    target: ;
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
            return;
            goto target;
		}
        
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2082
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x208a
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 38 (0x26)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.1
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0014: ldloc.0
		IL_0015: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_001b: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0020: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0025: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsForwardsGoToMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
		    goto target;
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
		    target: ;
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x206a
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2072
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 14 (0xe)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsBackwardsGoToCaseMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            case 1:
            default:
			    int b = 1;
			    Action action = () => Console.WriteLine(a + b);
			    action();
                break;
            case 0:
                goto case 1;

        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2090
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2098
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 52 (0x34)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: brfalse.s IL_001b
		IL_0017: ldloc.1
		IL_0018: ldc.i4.1
		IL_0019: pop
		IL_001a: pop
		IL_001b: ldloc.0
		IL_001c: ldc.i4.1
		IL_001d: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0022: ldloc.0
		IL_0023: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_0029: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002e: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0033: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsForwardsGoToCaseMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            case 0:
                goto case 1;
            case 1:
            default:
			    int b = 1;
			    Action action = () => Console.WriteLine(a + b);
			    action();
                break;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2090
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2098
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 52 (0x34)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: brfalse.s IL_001b
		IL_0017: ldloc.1
		IL_0018: ldc.i4.1
		IL_0019: pop
		IL_001a: pop
		IL_001b: ldloc.0
		IL_001c: ldc.i4.1
		IL_001d: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0022: ldloc.0
		IL_0023: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_0029: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002e: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0033: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsBackwardsGoToDefaultMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            default:
			    int b = 1;
			    Action action = () => Console.WriteLine(a + b);
			    action();
                break;
            case 0:
                goto default;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2089
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2091
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 45 (0x2d)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: pop
		IL_0014: ldloc.0
		IL_0015: ldc.i4.1
		IL_0016: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_001b: ldloc.0
		IL_001c: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_0022: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0027: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_002c: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsForwardsGoToDefaultMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            case 0:
			    int b = 1;
			    Action action = () => Console.WriteLine(a + b);
			    action();
                goto default;
            default:
                break;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208a
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2092
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 46 (0x2e)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: brtrue.s IL_002d
		IL_0015: ldloc.0
		IL_0016: ldc.i4.1
		IL_0017: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_001c: ldloc.0
		IL_001d: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_0023: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0028: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_002d: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void ScopeContainsScopeContainingBackwardsGoToMergeDisplayClasses()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
            target: ;
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();

            return;

            {
		        goto target;
            }
		}


	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2082
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x208a
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 38 (0x26)
		.maxstack 2
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.1
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0014: ldloc.0
		IL_0015: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_001b: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0020: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0025: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToToPointInBetweenScopeAndParentPreventsMerging01()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        target: ;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
		}

        return;
		    
		goto target;
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0026: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToToPointInBetweenScopeAndParentPreventsMerging02()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        target: ;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
		}

        return;
		{  
		    goto target;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0026: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToToPointInBetweenScopeAndParentPreventsMerging03()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        target: ;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();

            return;

		    goto target;
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0026: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToToPointInBetweenScopeAndParentPreventsMerging04()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        target: ;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();

            return;

		    {  
		        goto target;
            }
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0026: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToCaseToPointInBetweenScopeAndParentPreventsMerging()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            case 1:
            default:
                {
			        int b = 1;
			        Action action = () => Console.WriteLine(a + b);
			        action();
                    break;
                }
            case 0:
                goto case 1;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20a3
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 63 (0x3f)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] int32
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: stloc.1
		IL_0014: ldloc.1
		IL_0015: brfalse.s IL_001b
		IL_0017: ldloc.1
		IL_0018: ldc.i4.1
		IL_0019: pop
		IL_001a: pop
		IL_001b: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0020: dup
		IL_0021: ldloc.0
		IL_0022: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0027: dup
		IL_0028: ldc.i4.1
		IL_0029: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_002e: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0034: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0039: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_003e: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void BackwardsGoToDefaultToPointInBetweenScopeAndParentPreventsMerging()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
        switch(a)
        {
            default:
                {
			        int b = 1;
			        Action action = () => Console.WriteLine(a + b);
			        action();
                    break;
                }
            case 0:
                goto default;

        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x209c
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 56 (0x38)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0013: pop
		IL_0014: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0019: dup
		IL_001a: ldloc.0
		IL_001b: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0020: dup
		IL_0021: ldc.i4.1
		IL_0022: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0027: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_002d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0032: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0037: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable01()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
		}

		Action _ = () => Console.WriteLine(a);
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x20a2
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
		IL_0026: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable02()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			{
				Action action = () => Console.WriteLine(a + b);
				action();
			}

			{
				Action _ = () => Console.WriteLine(a + b);
			}
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2077
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x207f
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x207f
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 38 (0x26)
		.maxstack 8
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.0
		IL_0007: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000c: dup
		IL_000d: ldc.i4.1
		IL_000e: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0013: dup
		IL_0014: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_001a: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_001f: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0024: pop
		IL_0025: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable03()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
			Action _ = () => Console.WriteLine(b);
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2077
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x207f
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x2093
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 38 (0x26)
		.maxstack 8
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.0
		IL_0007: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000c: dup
		IL_000d: ldc.i4.1
		IL_000e: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0013: dup
		IL_0014: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_001a: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_001f: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0024: pop
		IL_0025: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable04()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			Action action = () => Console.WriteLine(a + b);
			action();
			Action _ = () => Console.WriteLine(a);
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x209c
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20a9
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 56 (0x38)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: dup
		IL_0013: ldloc.0
		IL_0014: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0019: dup
		IL_001a: ldc.i4.1
		IL_001b: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0020: dup
		IL_0021: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0027: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002c: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0031: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0036: pop
		IL_0037: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable05()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			{
				int c = 2;
				{
					int d = 3;

					Action action = () => Console.WriteLine(b + d);
					action();
					Action _ = () => Console.WriteLine(a + c);
				}
			}
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"4");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		.field public int32 c
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x20aa
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::c
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 d
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a2
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20be
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::d
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 70 (0x46)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.1
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0014: ldloc.0
		IL_0015: ldc.i4.2
		IL_0016: stfld int32 Program/'<>c__DisplayClass0_0'::c
		IL_001b: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0020: dup
		IL_0021: ldloc.0
		IL_0022: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0027: dup
		IL_0028: ldc.i4.3
		IL_0029: stfld int32 Program/'<>c__DisplayClass0_1'::d
		IL_002e: dup
		IL_002f: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__0'()
		IL_0035: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_003a: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_003f: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0044: pop
		IL_0045: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable06()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			{
				int c = 2;

				Action action = () => Console.WriteLine(a + c);
				action();
				Action x = () => Console.WriteLine(a + b + c);
			}
            Action y = () => Console.WriteLine(b);
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"2");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2096
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x209e
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 c
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2096
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x20ab
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::c
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
		.method assembly hidebysig 
			instance void '<Main>b__2' () cil managed 
		{
			// Method begins at RVA 0x20c4
			// Code size 36 (0x24)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0011: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_0016: add
			IL_0017: ldarg.0
			IL_0018: ldfld int32 Program/'<>c__DisplayClass0_1'::c
			IL_001d: add
			IL_001e: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0023: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__2'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 58 (0x3a)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.1
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0014: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0019: dup
		IL_001a: ldloc.0
		IL_001b: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0020: dup
		IL_0021: ldc.i4.2
		IL_0022: stfld int32 Program/'<>c__DisplayClass0_1'::c
		IL_0027: dup
		IL_0028: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
		IL_002e: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0033: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0038: pop
		IL_0039: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable07()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			{
				int c = 2;

				Action action = () => Console.WriteLine(b + c);
				action();
			}
            Action y = () => Console.WriteLine(a + b);
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x209c
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 c
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2094
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass0_0'::b
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass0_1'::c
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 56 (0x38)
		.maxstack 3
		.entrypoint
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.1
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_0'::b
		IL_0014: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0019: dup
		IL_001a: ldloc.0
		IL_001b: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
		IL_0020: dup
		IL_0021: ldc.i4.2
		IL_0022: stfld int32 Program/'<>c__DisplayClass0_1'::c
		IL_0027: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
		IL_002d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0032: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0037: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void OptimizationDoesNotIncreaseClosuresReferencingVariable08()
        {
            var source =
                @"using System;

public static class Program
{
	public static void Main()
	{
		int a = 0;
		{
			int b = 1;
			{
				int c = 2;

				Action action = () => Console.WriteLine(b + c);
				action();
			}
            Action y = () => Console.WriteLine(a);
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi abstract sealed beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2082
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x208a
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public int32 c
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2082
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x2097
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass0_1'::c
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 8
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.0
		IL_0007: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_000c: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0011: dup
		IL_0012: ldc.i4.1
		IL_0013: stfld int32 Program/'<>c__DisplayClass0_1'::b
		IL_0018: dup
		IL_0019: ldc.i4.2
		IL_001a: stfld int32 Program/'<>c__DisplayClass0_1'::c
		IL_001f: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
		IL_0025: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_002a: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_002f: pop
		IL_0030: ret
	} // end of method Program::Main
} // end of class Program");
        }

        [Fact]
        public void DoNotMergeEnvironmentsInsideLocalFunctionToOutside()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public class Program
{
	public static void Main()
	{
		var actions = new List<Action>();
		int a = 1;

		void M()
		{
			int b = 0;

			actions.Add(() => b += a);

			actions.Add(() => Console.WriteLine(b));
		}

		M();
		M();
		actions[0]();
		actions[2]();
		actions[1]();
		actions[3]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1
1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> actions
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20c3
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>g__M|0' () cil managed 
		{
			// Method begins at RVA 0x20cc
			// Code size 67 (0x43)
			.maxstack 3
			.locals init (
				[0] class Program/'<>c__DisplayClass0_1'
			)
			IL_0000: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0005: stloc.0
			IL_0006: ldloc.0
			IL_0007: ldarg.0
			IL_0008: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000d: ldloc.0
			IL_000e: ldc.i4.0
			IL_000f: stfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0014: ldarg.0
			IL_0015: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
			IL_001a: ldloc.0
			IL_001b: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
			IL_0021: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_0026: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_002b: ldarg.0
			IL_002c: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
			IL_0031: ldloc.0
			IL_0032: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__2'()
			IL_0038: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_003d: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0042: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>g__M|0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20c3
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x211b
			// Code size 25 (0x19)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldarg.0
			IL_0002: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0007: ldarg.0
			IL_0008: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000d: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0012: add
			IL_0013: stfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0018: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
		.method assembly hidebysig 
			instance void '<Main>b__2' () cil managed 
		{
			// Method begins at RVA 0x2135
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__2'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 103 (0x67)
		.maxstack 3
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0010: dup
		IL_0011: ldc.i4.1
		IL_0012: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0017: dup
		IL_0018: callvirt instance void Program/'<>c__DisplayClass0_0'::'<Main>g__M|0'()
		IL_001d: dup
		IL_001e: callvirt instance void Program/'<>c__DisplayClass0_0'::'<Main>g__M|0'()
		IL_0023: dup
		IL_0024: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0029: ldc.i4.0
		IL_002a: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_002f: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0034: dup
		IL_0035: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_003a: ldc.i4.2
		IL_003b: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0040: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0045: dup
		IL_0046: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_004b: ldc.i4.1
		IL_004c: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0051: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0056: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_005b: ldc.i4.3
		IL_005c: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_0061: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0066: ret
	} // end of method Program::Main
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20c3
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void DoNotMergeEnvironmentsInsideLambdaToOutside()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public class Program
{
	public static void Main()
	{
        var actions = new List<Action>();
        int a  = 1;
        
        Action M = () =>
        {
            int b = 0;
            
            actions.Add(() => b += a);
            
            actions.Add(() => Console.WriteLine(b));
        };
        
        M();
        M();
        actions[0]();
        actions[2]();
        actions[1]();
        actions[3]();
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1
1");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> actions
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20ce
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__0' () cil managed 
		{
			// Method begins at RVA 0x20d8
			// Code size 67 (0x43)
			.maxstack 3
			.locals init (
				[0] class Program/'<>c__DisplayClass0_1'
			)
			IL_0000: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
			IL_0005: stloc.0
			IL_0006: ldloc.0
			IL_0007: ldarg.0
			IL_0008: stfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000d: ldloc.0
			IL_000e: ldc.i4.0
			IL_000f: stfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0014: ldarg.0
			IL_0015: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
			IL_001a: ldloc.0
			IL_001b: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__1'()
			IL_0021: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_0026: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_002b: ldarg.0
			IL_002c: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
			IL_0031: ldloc.0
			IL_0032: ldftn instance void Program/'<>c__DisplayClass0_1'::'<Main>b__2'()
			IL_0038: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
			IL_003d: callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::Add(!0)
			IL_0042: ret
		} // end of method '<>c__DisplayClass0_0'::'<Main>b__0'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20ce
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance void '<Main>b__1' () cil managed 
		{
			// Method begins at RVA 0x2127
			// Code size 25 (0x19)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldarg.0
			IL_0002: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0007: ldarg.0
			IL_0008: ldfld class Program/'<>c__DisplayClass0_0' Program/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000d: ldfld int32 Program/'<>c__DisplayClass0_0'::a
			IL_0012: add
			IL_0013: stfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0018: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__1'
		.method assembly hidebysig 
			instance void '<Main>b__2' () cil managed 
		{
			// Method begins at RVA 0x2141
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_1'::b
			IL_0006: call void [mscorlib]System.Console::WriteLine(int32)
			IL_000b: ret
		} // end of method '<>c__DisplayClass0_1'::'<Main>b__2'
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 114 (0x72)
		.maxstack 3
		.entrypoint
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::.ctor()
		IL_000b: stfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0010: dup
		IL_0011: ldc.i4.1
		IL_0012: stfld int32 Program/'<>c__DisplayClass0_0'::a
		IL_0017: dup
		IL_0018: ldftn instance void Program/'<>c__DisplayClass0_0'::'<Main>b__0'()
		IL_001e: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0023: dup
		IL_0024: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0029: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_002e: dup
		IL_002f: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0034: ldc.i4.0
		IL_0035: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_003a: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_003f: dup
		IL_0040: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0045: ldc.i4.2
		IL_0046: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_004b: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0050: dup
		IL_0051: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0056: ldc.i4.1
		IL_0057: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_005c: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0061: ldfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action> Program/'<>c__DisplayClass0_0'::actions
		IL_0066: ldc.i4.3
		IL_0067: callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Action>::get_Item(int32)
		IL_006c: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_0071: ret
	} // end of method Program::Main
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20ce
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void LocalMethod01()
        {
            var source =
                @"using System;
public class Program 
{
    public static void Main()
    {
        M()();
    }
	public static Action M() 
	{
		int a = 1; 
		{
			int b = 2;

			void M1() 
			{
				Console.WriteLine(a + b);
			}

			{
				int c = 3;

				void M2() 
				{
					M1();
					Console.WriteLine(c);
				}
				return M2;
			}
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3
3");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M1|0' () cil managed 
		{
			// Method begins at RVA 0x20a3
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass1_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass1_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass1_0'::'<M>g__M1|0'
	} // end of class <>c__DisplayClass1_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 c
		.field public class Program/'<>c__DisplayClass1_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_1'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M2|1' () cil managed 
		{
			// Method begins at RVA 0x20b7
			// Code size 23 (0x17)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
			IL_0006: callvirt instance void Program/'<>c__DisplayClass1_0'::'<M>g__M1|0'()
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass1_1'::c
			IL_0011: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0016: ret
		} // end of method '<>c__DisplayClass1_1'::'<M>g__M2|1'
	} // end of class <>c__DisplayClass1_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 11 (0xb)
		.maxstack 8
		.entrypoint
		IL_0000: call class [mscorlib]System.Action Program::M()
		IL_0005: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_000a: ret
	} // end of method Program::Main
	.method public hidebysig static 
		class [mscorlib]System.Action M () cil managed 
	{
		// Method begins at RVA 0x205c
		// Code size 51 (0x33)
		.maxstack 3
		.locals init (
			[0] class Program/'<>c__DisplayClass1_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass1_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.1
		IL_0008: stfld int32 Program/'<>c__DisplayClass1_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.2
		IL_000f: stfld int32 Program/'<>c__DisplayClass1_0'::b
		IL_0014: newobj instance void Program/'<>c__DisplayClass1_1'::.ctor()
		IL_0019: dup
		IL_001a: ldloc.0
		IL_001b: stfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
		IL_0020: dup
		IL_0021: ldc.i4.3
		IL_0022: stfld int32 Program/'<>c__DisplayClass1_1'::c
		IL_0027: ldftn instance void Program/'<>c__DisplayClass1_1'::'<M>g__M2|1'()
		IL_002d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0032: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x209b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void LocalMethod02()
        {
            var source =
                @"using System;
public class Program 
{
    public static void Main()
    {
        M()();
    }
	public static Action M() 
	{
		int a = 1; 
		{
target:
			int b = 2;

			void M1() 
			{
				Console.WriteLine(a + b);
			}

			{
				int c = 3;

				void M2() 
				{
					M1();
					Console.WriteLine(c);
				}
				return M2;
                goto target;
			}
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3
3");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		.field public int32 b
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M1|0' () cil managed 
		{
			// Method begins at RVA 0x20a3
			// Code size 19 (0x13)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass1_0'::a
			IL_0006: ldarg.0
			IL_0007: ldfld int32 Program/'<>c__DisplayClass1_0'::b
			IL_000c: add
			IL_000d: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0012: ret
		} // end of method '<>c__DisplayClass1_0'::'<M>g__M1|0'
	} // end of class <>c__DisplayClass1_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 c
		.field public class Program/'<>c__DisplayClass1_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x209b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_1'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M2|1' () cil managed 
		{
			// Method begins at RVA 0x20b7
			// Code size 23 (0x17)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
			IL_0006: callvirt instance void Program/'<>c__DisplayClass1_0'::'<M>g__M1|0'()
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass1_1'::c
			IL_0011: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0016: ret
		} // end of method '<>c__DisplayClass1_1'::'<M>g__M2|1'
	} // end of class <>c__DisplayClass1_1
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 11 (0xb)
		.maxstack 8
		.entrypoint
		IL_0000: call class [mscorlib]System.Action Program::M()
		IL_0005: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_000a: ret
	} // end of method Program::Main
	.method public hidebysig static 
		class [mscorlib]System.Action M () cil managed 
	{
		// Method begins at RVA 0x205c
		// Code size 51 (0x33)
		.maxstack 3
		.locals init (
			[0] class Program/'<>c__DisplayClass1_0'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass1_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.1
		IL_0008: stfld int32 Program/'<>c__DisplayClass1_0'::a
		IL_000d: ldloc.0
		IL_000e: ldc.i4.2
		IL_000f: stfld int32 Program/'<>c__DisplayClass1_0'::b
		IL_0014: newobj instance void Program/'<>c__DisplayClass1_1'::.ctor()
		IL_0019: dup
		IL_001a: ldloc.0
		IL_001b: stfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
		IL_0020: dup
		IL_0021: ldc.i4.3
		IL_0022: stfld int32 Program/'<>c__DisplayClass1_1'::c
		IL_0027: ldftn instance void Program/'<>c__DisplayClass1_1'::'<M>g__M2|1'()
		IL_002d: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0032: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x209b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void LocalMethod03()
        {
            var source =
                @"using System;
public class Program 
{
    public static void Main()
    {
        M()();
    }
	public static Action M() 
	{
		int a = 1; 
target:
		{
			int b = 2;

			void M1() 
			{
				Console.WriteLine(a + b);
			}

			{
				int c = 3;

				void M2() 
				{
					M1();
					Console.WriteLine(c);
				}
				return M2;
                goto target;
			}
		}
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"3
3");

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a8
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::.ctor
	} // end of class <>c__DisplayClass1_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public class Program/'<>c__DisplayClass1_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a8
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_1'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M1|0' () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
			IL_0006: ldfld int32 Program/'<>c__DisplayClass1_0'::a
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass1_1'::b
			IL_0011: add
			IL_0012: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0017: ret
		} // end of method '<>c__DisplayClass1_1'::'<M>g__M1|0'
	} // end of class <>c__DisplayClass1_1
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_2'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 c
		.field public class Program/'<>c__DisplayClass1_1' 'CS$<>8__locals2'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a8
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_2'::.ctor
		.method assembly hidebysig 
			instance void '<M>g__M2|1' () cil managed 
		{
			// Method begins at RVA 0x20c9
			// Code size 23 (0x17)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass1_1' Program/'<>c__DisplayClass1_2'::'CS$<>8__locals2'
			IL_0006: callvirt instance void Program/'<>c__DisplayClass1_1'::'<M>g__M1|0'()
			IL_000b: ldarg.0
			IL_000c: ldfld int32 Program/'<>c__DisplayClass1_2'::c
			IL_0011: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0016: ret
		} // end of method '<>c__DisplayClass1_2'::'<M>g__M2|1'
	} // end of class <>c__DisplayClass1_2
	// Methods
	.method public hidebysig static 
		void Main () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 11 (0xb)
		.maxstack 8
		.entrypoint
		IL_0000: call class [mscorlib]System.Action Program::M()
		IL_0005: callvirt instance void [mscorlib]System.Action::Invoke()
		IL_000a: ret
	} // end of method Program::Main
	.method public hidebysig static 
		class [mscorlib]System.Action M () cil managed 
	{
		// Method begins at RVA 0x205c
		// Code size 64 (0x40)
		.maxstack 3
		.locals init (
			[0] class Program/'<>c__DisplayClass1_0',
			[1] class Program/'<>c__DisplayClass1_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass1_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.1
		IL_0008: stfld int32 Program/'<>c__DisplayClass1_0'::a
		IL_000d: newobj instance void Program/'<>c__DisplayClass1_1'::.ctor()
		IL_0012: stloc.1
		IL_0013: ldloc.1
		IL_0014: ldloc.0
		IL_0015: stfld class Program/'<>c__DisplayClass1_0' Program/'<>c__DisplayClass1_1'::'CS$<>8__locals1'
		IL_001a: ldloc.1
		IL_001b: ldc.i4.2
		IL_001c: stfld int32 Program/'<>c__DisplayClass1_1'::b
		IL_0021: newobj instance void Program/'<>c__DisplayClass1_2'::.ctor()
		IL_0026: dup
		IL_0027: ldloc.1
		IL_0028: stfld class Program/'<>c__DisplayClass1_1' Program/'<>c__DisplayClass1_2'::'CS$<>8__locals2'
		IL_002d: dup
		IL_002e: ldc.i4.3
		IL_002f: stfld int32 Program/'<>c__DisplayClass1_2'::c
		IL_0034: ldftn instance void Program/'<>c__DisplayClass1_2'::'<M>g__M2|1'()
		IL_003a: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_003f: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a8
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void LocalMethod04()
        {
            var source =
                @"using System;
public class Program 
{
	public void M() 
	{
        int x = 0;
        {
            int y = 0;
            void M() => y++;
        
            {
                  Action a = () => x++;
            }
        }
	}
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2072
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance void '<M>b__1' () cil managed 
		{
			// Method begins at RVA 0x209c
			// Code size 17 (0x11)
			.maxstack 3
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0006: stloc.0
			IL_0007: ldarg.0
			IL_0008: ldloc.0
			IL_0009: ldc.i4.1
			IL_000a: add
			IL_000b: stfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0010: ret
		} // end of method '<>c__DisplayClass0_0'::'<M>b__1'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.ValueType
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 y
	} // end of class <>c__DisplayClass0_1
	// Methods
	.method public hidebysig 
		instance void M () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 22 (0x16)
		.maxstack 3
		.locals init (
			[0] valuetype Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: dup
		IL_0006: ldc.i4.0
		IL_0007: stfld int32 Program/'<>c__DisplayClass0_0'::x
		IL_000c: ldloca.s 0
		IL_000e: ldc.i4.0
		IL_000f: stfld int32 Program/'<>c__DisplayClass0_1'::y
		IL_0014: pop
		IL_0015: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2072
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
	.method assembly hidebysig static 
		void '<M>g__M|0_0' (
			valuetype Program/'<>c__DisplayClass0_1'& ''
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x207c
		// Code size 17 (0x11)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldfld int32 Program/'<>c__DisplayClass0_1'::y
		IL_0006: stloc.0
		IL_0007: ldarg.0
		IL_0008: ldloc.0
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 Program/'<>c__DisplayClass0_1'::y
		IL_0010: ret
	} // end of method Program::'<M>g__M|0_0'
} // end of class Program");
        }

        [Fact]
        public void LocalMethod05()
        {
            var source =
                @"using System;
public class Program 
{
	public Func<int> M() 
	{
        int x = 0;
        {
            int y = 0;
            int M() => y++;
        
            {
                 Func<int> a = () => x++;
                {
                    int M1() => M() + a();
                    return M1;
                }
            }
        }
    }
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "Program", @"
.class public auto ansi beforefieldinit Program
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 x
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance int32 '<M>b__1' () cil managed 
		{
			// Method begins at RVA 0x20a8
			// Code size 18 (0x12)
			.maxstack 3
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0006: stloc.0
			IL_0007: ldarg.0
			IL_0008: ldloc.0
			IL_0009: ldc.i4.1
			IL_000a: add
			IL_000b: stfld int32 Program/'<>c__DisplayClass0_0'::x
			IL_0010: ldloc.0
			IL_0011: ret
		} // end of method '<>c__DisplayClass0_0'::'<M>b__1'
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 y
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
		.method assembly hidebysig 
			instance int32 '<M>g__M|0' () cil managed 
		{
			// Method begins at RVA 0x20c8
			// Code size 18 (0x12)
			.maxstack 3
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 Program/'<>c__DisplayClass0_1'::y
			IL_0006: stloc.0
			IL_0007: ldarg.0
			IL_0008: ldloc.0
			IL_0009: ldc.i4.1
			IL_000a: add
			IL_000b: stfld int32 Program/'<>c__DisplayClass0_1'::y
			IL_0010: ldloc.0
			IL_0011: ret
		} // end of method '<>c__DisplayClass0_1'::'<M>g__M|0'
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_2'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public class [mscorlib]System.Func`1<int32> a
		.field public class Program/'<>c__DisplayClass0_1' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_2'::.ctor
		.method assembly hidebysig 
			instance int32 '<M>g__M1|2' () cil managed 
		{
			// Method begins at RVA 0x20e6
			// Code size 24 (0x18)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals1'
			IL_0006: callvirt instance int32 Program/'<>c__DisplayClass0_1'::'<M>g__M|0'()
			IL_000b: ldarg.0
			IL_000c: ldfld class [mscorlib]System.Func`1<int32> Program/'<>c__DisplayClass0_2'::a
			IL_0011: callvirt instance !0 class [mscorlib]System.Func`1<int32>::Invoke()
			IL_0016: add
			IL_0017: ret
		} // end of method '<>c__DisplayClass0_2'::'<M>g__M1|2'
	} // end of class <>c__DisplayClass0_2
	// Methods
	.method public hidebysig 
		instance class [mscorlib]System.Func`1<int32> M () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 68 (0x44)
		.maxstack 4
		.locals init (
			[0] class Program/'<>c__DisplayClass0_0',
			[1] class Program/'<>c__DisplayClass0_1'
		)
		IL_0000: newobj instance void Program/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldc.i4.0
		IL_0008: stfld int32 Program/'<>c__DisplayClass0_0'::x
		IL_000d: newobj instance void Program/'<>c__DisplayClass0_1'::.ctor()
		IL_0012: stloc.1
		IL_0013: ldloc.1
		IL_0014: ldc.i4.0
		IL_0015: stfld int32 Program/'<>c__DisplayClass0_1'::y
		IL_001a: newobj instance void Program/'<>c__DisplayClass0_2'::.ctor()
		IL_001f: dup
		IL_0020: ldloc.1
		IL_0021: stfld class Program/'<>c__DisplayClass0_1' Program/'<>c__DisplayClass0_2'::'CS$<>8__locals1'
		IL_0026: dup
		IL_0027: ldloc.0
		IL_0028: ldftn instance int32 Program/'<>c__DisplayClass0_0'::'<M>b__1'()
		IL_002e: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_0033: stfld class [mscorlib]System.Func`1<int32> Program/'<>c__DisplayClass0_2'::a
		IL_0038: ldftn instance int32 Program/'<>c__DisplayClass0_2'::'<M>g__M1|2'()
		IL_003e: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_0043: ret
	} // end of method Program::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a0
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method Program::.ctor
} // end of class Program");
        }

        [Fact]
        public void YieldReturnCorrectDisplayClasseAreCreated()
        {
            var source =
                @"using System;
using System.Collections.Generic;

public class C {
    public IEnumerable<int> M() {
        int a  = 1;
        yield return 1;
        while(true)
        {
            yield return 2;
        	int b = 2;
        	yield return 3;
            {
                yield return 4;
        		int c = 3;
                target:
        		yield return 5;
                {
                    yield return 6;
                    int d = 4;
                    goto target;
                    yield return 7;
                    Action e = () => Console.WriteLine(a + b + c + d);
                }
            }
        }
    }
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2059
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public int32 c
		.field public class C/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2059
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_2'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 d
		.field public class C/'<>c__DisplayClass0_1' 'CS$<>8__locals2'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2059
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_2'::.ctor
		.method assembly hidebysig 
			instance void '<M>b__0' () cil managed 
		{
			// Method begins at RVA 0x2061
			// Code size 53 (0x35)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0006: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000b: ldfld int32 C/'<>c__DisplayClass0_0'::a
			IL_0010: ldarg.0
			IL_0011: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0016: ldfld int32 C/'<>c__DisplayClass0_1'::b
			IL_001b: add
			IL_001c: ldarg.0
			IL_001d: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0022: ldfld int32 C/'<>c__DisplayClass0_1'::c
			IL_0027: add
			IL_0028: ldarg.0
			IL_0029: ldfld int32 C/'<>c__DisplayClass0_2'::d
			IL_002e: add
			IL_002f: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0034: ret
		} // end of method '<>c__DisplayClass0_2'::'<M>b__0'
	} // end of class <>c__DisplayClass0_2
	.class nested private auto ansi sealed beforefieldinit '<M>d__0'
		extends [mscorlib]System.Object
		implements class [mscorlib]System.Collections.Generic.IEnumerable`1<int32>,
		           [mscorlib]System.Collections.IEnumerable,
		           class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>,
		           [mscorlib]System.IDisposable,
		           [mscorlib]System.Collections.IEnumerator
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field private int32 '<>1__state'
		.field private int32 '<>2__current'
		.field private int32 '<>l__initialThreadId'
		.field private class C/'<>c__DisplayClass0_0' '<>8__1'
		.field private class C/'<>c__DisplayClass0_1' '<>8__2'
		.field private class C/'<>c__DisplayClass0_2' '<>8__3'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor (
				int32 '<>1__state'
			) cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			// Method begins at RVA 0x2097
			// Code size 25 (0x19)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ldarg.0
			IL_0007: ldarg.1
			IL_0008: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_000d: ldarg.0
			IL_000e: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
			IL_0013: stfld int32 C/'<M>d__0'::'<>l__initialThreadId'
			IL_0018: ret
		} // end of method '<M>d__0'::.ctor
		.method private final hidebysig newslot virtual 
			instance void System.IDisposable.Dispose () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [mscorlib]System.IDisposable::Dispose()
			// Method begins at RVA 0x20b1
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<M>d__0'::System.IDisposable.Dispose
		.method private final hidebysig newslot virtual 
			instance bool MoveNext () cil managed 
		{
			.override method instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
			// Method begins at RVA 0x20b4
			// Code size 342 (0x156)
			.maxstack 2
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
			IL_0006: stloc.0
			IL_0007: ldloc.0
			IL_0008: switch (IL_002f, IL_005d, IL_0090, IL_00b3, IL_00ca, IL_00ed, IL_0120, IL_0135)
			IL_002d: ldc.i4.0
			IL_002e: ret
			IL_002f: ldarg.0
			IL_0030: ldc.i4.m1
			IL_0031: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_0036: ldarg.0
			IL_0037: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
			IL_003c: stfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
			IL_0041: ldarg.0
			IL_0042: ldfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
			IL_0047: ldc.i4.1
			IL_0048: stfld int32 C/'<>c__DisplayClass0_0'::a
			IL_004d: ldarg.0
			IL_004e: ldc.i4.1
			IL_004f: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_0054: ldarg.0
			IL_0055: ldc.i4.1
			IL_0056: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_005b: ldc.i4.1
			IL_005c: ret
			IL_005d: ldarg.0
			IL_005e: ldc.i4.m1
			IL_005f: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_0064: ldarg.0
			IL_0065: newobj instance void C/'<>c__DisplayClass0_1'::.ctor()
			IL_006a: stfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_006f: ldarg.0
			IL_0070: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_0075: ldarg.0
			IL_0076: ldfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
			IL_007b: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_0080: ldarg.0
			IL_0081: ldc.i4.2
			IL_0082: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_0087: ldarg.0
			IL_0088: ldc.i4.2
			IL_0089: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_008e: ldc.i4.1
			IL_008f: ret
			IL_0090: ldarg.0
			IL_0091: ldc.i4.m1
			IL_0092: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_0097: ldarg.0
			IL_0098: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_009d: ldc.i4.2
			IL_009e: stfld int32 C/'<>c__DisplayClass0_1'::b
			IL_00a3: ldarg.0
			IL_00a4: ldc.i4.3
			IL_00a5: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_00aa: ldarg.0
			IL_00ab: ldc.i4.3
			IL_00ac: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00b1: ldc.i4.1
			IL_00b2: ret
			IL_00b3: ldarg.0
			IL_00b4: ldc.i4.m1
			IL_00b5: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00ba: ldarg.0
			IL_00bb: ldc.i4.4
			IL_00bc: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_00c1: ldarg.0
			IL_00c2: ldc.i4.4
			IL_00c3: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00c8: ldc.i4.1
			IL_00c9: ret
			IL_00ca: ldarg.0
			IL_00cb: ldc.i4.m1
			IL_00cc: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00d1: ldarg.0
			IL_00d2: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_00d7: ldc.i4.3
			IL_00d8: stfld int32 C/'<>c__DisplayClass0_1'::c
			IL_00dd: ldarg.0
			IL_00de: ldc.i4.5
			IL_00df: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_00e4: ldarg.0
			IL_00e5: ldc.i4.5
			IL_00e6: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00eb: ldc.i4.1
			IL_00ec: ret
			IL_00ed: ldarg.0
			IL_00ee: ldc.i4.m1
			IL_00ef: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_00f4: ldarg.0
			IL_00f5: newobj instance void C/'<>c__DisplayClass0_2'::.ctor()
			IL_00fa: stfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
			IL_00ff: ldarg.0
			IL_0100: ldfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
			IL_0105: ldarg.0
			IL_0106: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_010b: stfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0110: ldarg.0
			IL_0111: ldc.i4.6
			IL_0112: stfld int32 C/'<M>d__0'::'<>2__current'
			IL_0117: ldarg.0
			IL_0118: ldc.i4.6
			IL_0119: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_011e: ldc.i4.1
			IL_011f: ret
			IL_0120: ldarg.0
			IL_0121: ldc.i4.m1
			IL_0122: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_0127: ldarg.0
			IL_0128: ldfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
			IL_012d: ldc.i4.4
			IL_012e: stfld int32 C/'<>c__DisplayClass0_2'::d
			IL_0133: br.s IL_00dd
			IL_0135: ldarg.0
			IL_0136: ldc.i4.m1
			IL_0137: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_013c: ldarg.0
			IL_013d: ldfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
			IL_0142: pop
			IL_0143: ldarg.0
			IL_0144: ldnull
			IL_0145: stfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
			IL_014a: ldarg.0
			IL_014b: ldnull
			IL_014c: stfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
			IL_0151: br IL_0064
		} // end of method '<M>d__0'::MoveNext
		.method private final hidebysig specialname newslot virtual 
			instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.get_Current' () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
			// Method begins at RVA 0x2216
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__0'::'<>2__current'
			IL_0006: ret
		} // end of method '<M>d__0'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'
		.method private final hidebysig newslot virtual 
			instance void System.Collections.IEnumerator.Reset () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [mscorlib]System.Collections.IEnumerator::Reset()
			// Method begins at RVA 0x221e
			// Code size 6 (0x6)
			.maxstack 8
			IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
			IL_0005: throw
		} // end of method '<M>d__0'::System.Collections.IEnumerator.Reset
		.method private final hidebysig specialname newslot virtual 
			instance object System.Collections.IEnumerator.get_Current () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance object [mscorlib]System.Collections.IEnumerator::get_Current()
			// Method begins at RVA 0x2225
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__0'::'<>2__current'
			IL_0006: box [mscorlib]System.Int32
			IL_000b: ret
		} // end of method '<M>d__0'::System.Collections.IEnumerator.get_Current
		.method private final hidebysig newslot virtual 
			instance class [mscorlib]System.Collections.Generic.IEnumerator`1<int32> 'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator' () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
			// Method begins at RVA 0x2234
			// Code size 43 (0x2b)
			.maxstack 2
			.locals init (
				[0] class C/'<M>d__0'
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
			IL_0006: ldc.i4.s -2
			IL_0008: bne.un.s IL_0022
			IL_000a: ldarg.0
			IL_000b: ldfld int32 C/'<M>d__0'::'<>l__initialThreadId'
			IL_0010: call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
			IL_0015: bne.un.s IL_0022
			IL_0017: ldarg.0
			IL_0018: ldc.i4.0
			IL_0019: stfld int32 C/'<M>d__0'::'<>1__state'
			IL_001e: ldarg.0
			IL_001f: stloc.0
			IL_0020: br.s IL_0029
			IL_0022: ldc.i4.0
			IL_0023: newobj instance void C/'<M>d__0'::.ctor(int32)
			IL_0028: stloc.0
			IL_0029: ldloc.0
			IL_002a: ret
		} // end of method '<M>d__0'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'
		.method private final hidebysig newslot virtual 
			instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance class [mscorlib]System.Collections.IEnumerator [mscorlib]System.Collections.IEnumerable::GetEnumerator()
			// Method begins at RVA 0x226b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance class [mscorlib]System.Collections.Generic.IEnumerator`1<int32> C/'<M>d__0'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'()
			IL_0006: ret
		} // end of method '<M>d__0'::System.Collections.IEnumerable.GetEnumerator
		// Properties
		.property instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.Current'()
		{
			.get instance int32 C/'<M>d__0'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'()
		}
		.property instance object System.Collections.IEnumerator.Current()
		{
			.get instance object C/'<M>d__0'::System.Collections.IEnumerator.get_Current()
		}
	} // end of class <M>d__0
	// Methods
	.method public hidebysig 
		instance class [mscorlib]System.Collections.Generic.IEnumerable`1<int32> M () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
			01 00 09 43 2b 3c 4d 3e 64 5f 5f 30 00 00
		)
		// Method begins at RVA 0x2050
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldc.i4.s -2
		IL_0002: newobj instance void C/'<M>d__0'::.ctor(int32)
		IL_0007: ret
	} // end of method C::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2059
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
} // end of class C");
        }

        [Fact]
        public void AsyncAwaitCorrectDisplayClasseAreCreated()
        {
            var source =
                @"using System;
using System.Threading.Tasks;

public class C {
    public async Task M() {
        int a  = 1;
        await Task.Delay(0);
        while(true)
        {
            await Task.Delay(0);
        	int b = 2;
        	await Task.Delay(0);
            {
                await Task.Delay(0);
        		int c = 3;
                target:
        		await Task.Delay(0);
                {
                    await Task.Delay(0);
                    int d = 4;
                    goto target;
                    await Task.Delay(0);
                    Action e = () => Console.WriteLine(a + b + c + d);
                }
            }
        }
    }
}";
            var compilation = CompileAndVerify(source);

            VerifyTypeIL(compilation, "C", @"
.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 a
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
	} // end of class <>c__DisplayClass0_0
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_1'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 b
		.field public int32 c
		.field public class C/'<>c__DisplayClass0_0' 'CS$<>8__locals1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_1'::.ctor
	} // end of class <>c__DisplayClass0_1
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_2'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 d
		.field public class C/'<>c__DisplayClass0_1' 'CS$<>8__locals2'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x208d
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_2'::.ctor
		.method assembly hidebysig 
			instance void '<M>b__0' () cil managed 
		{
			// Method begins at RVA 0x2095
			// Code size 53 (0x35)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0006: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
			IL_000b: ldfld int32 C/'<>c__DisplayClass0_0'::a
			IL_0010: ldarg.0
			IL_0011: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0016: ldfld int32 C/'<>c__DisplayClass0_1'::b
			IL_001b: add
			IL_001c: ldarg.0
			IL_001d: ldfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
			IL_0022: ldfld int32 C/'<>c__DisplayClass0_1'::c
			IL_0027: add
			IL_0028: ldarg.0
			IL_0029: ldfld int32 C/'<>c__DisplayClass0_2'::d
			IL_002e: add
			IL_002f: call void [mscorlib]System.Console::WriteLine(int32)
			IL_0034: ret
		} // end of method '<>c__DisplayClass0_2'::'<M>b__0'
	} // end of class <>c__DisplayClass0_2
	.class nested private auto ansi sealed beforefieldinit '<M>d__0'
		extends [mscorlib]System.ValueType
		implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 '<>1__state'
		.field public valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder '<>t__builder'
		.field private class C/'<>c__DisplayClass0_0' '<>8__1'
		.field private class C/'<>c__DisplayClass0_1' '<>8__2'
		.field private class C/'<>c__DisplayClass0_2' '<>8__3'
		.field private valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter '<>u__1'
		// Methods
		.method private final hidebysig newslot virtual 
			instance void MoveNext () cil managed 
		{
			.override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
			// Method begins at RVA 0x20cc
			// Code size 785 (0x311)
			.maxstack 3
			.locals init (
				[0] int32,
				[1] valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter,
				[2] class [mscorlib]System.Exception
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__0'::'<>1__state'
			IL_0006: stloc.0
			.try
			{
				IL_0007: ldloc.0
				IL_0008: switch (IL_0078, IL_00ef, IL_0156, IL_01b1, IL_0218, IL_028f, IL_02c3)
				IL_0029: ldarg.0
				IL_002a: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
				IL_002f: stfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
				IL_0034: ldarg.0
				IL_0035: ldfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
				IL_003a: ldc.i4.1
				IL_003b: stfld int32 C/'<>c__DisplayClass0_0'::a
				IL_0040: ldc.i4.0
				IL_0041: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_0046: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_004b: stloc.1
				IL_004c: ldloca.s 1
				IL_004e: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_0053: brtrue.s IL_0094
				IL_0055: ldarg.0
				IL_0056: ldc.i4.0
				IL_0057: dup
				IL_0058: stloc.0
				IL_0059: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_005e: ldarg.0
				IL_005f: ldloc.1
				IL_0060: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0065: ldarg.0
				IL_0066: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_006b: ldloca.s 1
				IL_006d: ldarg.0
				IL_006e: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_0073: leave IL_0310
				IL_0078: ldarg.0
				IL_0079: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_007e: stloc.1
				IL_007f: ldarg.0
				IL_0080: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0085: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_008b: ldarg.0
				IL_008c: ldc.i4.m1
				IL_008d: dup
				IL_008e: stloc.0
				IL_008f: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0094: ldloca.s 1
				IL_0096: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_009b: ldarg.0
				IL_009c: newobj instance void C/'<>c__DisplayClass0_1'::.ctor()
				IL_00a1: stfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_00a6: ldarg.0
				IL_00a7: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_00ac: ldarg.0
				IL_00ad: ldfld class C/'<>c__DisplayClass0_0' C/'<M>d__0'::'<>8__1'
				IL_00b2: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_1'::'CS$<>8__locals1'
				IL_00b7: ldc.i4.0
				IL_00b8: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_00bd: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_00c2: stloc.1
				IL_00c3: ldloca.s 1
				IL_00c5: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_00ca: brtrue.s IL_010b
				IL_00cc: ldarg.0
				IL_00cd: ldc.i4.1
				IL_00ce: dup
				IL_00cf: stloc.0
				IL_00d0: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_00d5: ldarg.0
				IL_00d6: ldloc.1
				IL_00d7: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_00dc: ldarg.0
				IL_00dd: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_00e2: ldloca.s 1
				IL_00e4: ldarg.0
				IL_00e5: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_00ea: leave IL_0310
				IL_00ef: ldarg.0
				IL_00f0: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_00f5: stloc.1
				IL_00f6: ldarg.0
				IL_00f7: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_00fc: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_0102: ldarg.0
				IL_0103: ldc.i4.m1
				IL_0104: dup
				IL_0105: stloc.0
				IL_0106: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_010b: ldloca.s 1
				IL_010d: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_0112: ldarg.0
				IL_0113: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_0118: ldc.i4.2
				IL_0119: stfld int32 C/'<>c__DisplayClass0_1'::b
				IL_011e: ldc.i4.0
				IL_011f: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_0124: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_0129: stloc.1
				IL_012a: ldloca.s 1
				IL_012c: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_0131: brtrue.s IL_0172
				IL_0133: ldarg.0
				IL_0134: ldc.i4.2
				IL_0135: dup
				IL_0136: stloc.0
				IL_0137: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_013c: ldarg.0
				IL_013d: ldloc.1
				IL_013e: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0143: ldarg.0
				IL_0144: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_0149: ldloca.s 1
				IL_014b: ldarg.0
				IL_014c: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_0151: leave IL_0310
				IL_0156: ldarg.0
				IL_0157: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_015c: stloc.1
				IL_015d: ldarg.0
				IL_015e: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0163: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_0169: ldarg.0
				IL_016a: ldc.i4.m1
				IL_016b: dup
				IL_016c: stloc.0
				IL_016d: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0172: ldloca.s 1
				IL_0174: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_0179: ldc.i4.0
				IL_017a: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_017f: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_0184: stloc.1
				IL_0185: ldloca.s 1
				IL_0187: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_018c: brtrue.s IL_01cd
				IL_018e: ldarg.0
				IL_018f: ldc.i4.3
				IL_0190: dup
				IL_0191: stloc.0
				IL_0192: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0197: ldarg.0
				IL_0198: ldloc.1
				IL_0199: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_019e: ldarg.0
				IL_019f: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_01a4: ldloca.s 1
				IL_01a6: ldarg.0
				IL_01a7: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_01ac: leave IL_0310
				IL_01b1: ldarg.0
				IL_01b2: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_01b7: stloc.1
				IL_01b8: ldarg.0
				IL_01b9: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_01be: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_01c4: ldarg.0
				IL_01c5: ldc.i4.m1
				IL_01c6: dup
				IL_01c7: stloc.0
				IL_01c8: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_01cd: ldloca.s 1
				IL_01cf: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_01d4: ldarg.0
				IL_01d5: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_01da: ldc.i4.3
				IL_01db: stfld int32 C/'<>c__DisplayClass0_1'::c
				IL_01e0: ldc.i4.0
				IL_01e1: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_01e6: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_01eb: stloc.1
				IL_01ec: ldloca.s 1
				IL_01ee: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_01f3: brtrue.s IL_0234
				IL_01f5: ldarg.0
				IL_01f6: ldc.i4.4
				IL_01f7: dup
				IL_01f8: stloc.0
				IL_01f9: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_01fe: ldarg.0
				IL_01ff: ldloc.1
				IL_0200: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0205: ldarg.0
				IL_0206: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_020b: ldloca.s 1
				IL_020d: ldarg.0
				IL_020e: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_0213: leave IL_0310
				IL_0218: ldarg.0
				IL_0219: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_021e: stloc.1
				IL_021f: ldarg.0
				IL_0220: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0225: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_022b: ldarg.0
				IL_022c: ldc.i4.m1
				IL_022d: dup
				IL_022e: stloc.0
				IL_022f: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0234: ldloca.s 1
				IL_0236: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_023b: ldarg.0
				IL_023c: newobj instance void C/'<>c__DisplayClass0_2'::.ctor()
				IL_0241: stfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
				IL_0246: ldarg.0
				IL_0247: ldfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
				IL_024c: ldarg.0
				IL_024d: ldfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_0252: stfld class C/'<>c__DisplayClass0_1' C/'<>c__DisplayClass0_2'::'CS$<>8__locals2'
				IL_0257: ldc.i4.0
				IL_0258: call class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Threading.Tasks.Task::Delay(int32)
				IL_025d: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter [mscorlib]System.Threading.Tasks.Task::GetAwaiter()
				IL_0262: stloc.1
				IL_0263: ldloca.s 1
				IL_0265: call instance bool [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_026a: brtrue.s IL_02ab
				IL_026c: ldarg.0
				IL_026d: ldc.i4.5
				IL_026e: dup
				IL_026f: stloc.0
				IL_0270: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0275: ldarg.0
				IL_0276: ldloc.1
				IL_0277: stfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_027c: ldarg.0
				IL_027d: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_0282: ldloca.s 1
				IL_0284: ldarg.0
				IL_0285: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter, valuetype C/'<M>d__0'>(!!0&, !!1&)
				IL_028a: leave IL_0310
				IL_028f: ldarg.0
				IL_0290: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_0295: stloc.1
				IL_0296: ldarg.0
				IL_0297: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_029c: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_02a2: ldarg.0
				IL_02a3: ldc.i4.m1
				IL_02a4: dup
				IL_02a5: stloc.0
				IL_02a6: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_02ab: ldloca.s 1
				IL_02ad: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_02b2: ldarg.0
				IL_02b3: ldfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
				IL_02b8: ldc.i4.4
				IL_02b9: stfld int32 C/'<>c__DisplayClass0_2'::d
				IL_02be: br IL_01e0
				IL_02c3: ldarg.0
				IL_02c4: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_02c9: stloc.1
				IL_02ca: ldarg.0
				IL_02cb: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter C/'<M>d__0'::'<>u__1'
				IL_02d0: initobj [mscorlib]System.Runtime.CompilerServices.TaskAwaiter
				IL_02d6: ldarg.0
				IL_02d7: ldc.i4.m1
				IL_02d8: dup
				IL_02d9: stloc.0
				IL_02da: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_02df: ldloca.s 1
				IL_02e1: call instance void [mscorlib]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_02e6: ldarg.0
				IL_02e7: ldnull
				IL_02e8: stfld class C/'<>c__DisplayClass0_2' C/'<M>d__0'::'<>8__3'
				IL_02ed: ldarg.0
				IL_02ee: ldnull
				IL_02ef: stfld class C/'<>c__DisplayClass0_1' C/'<M>d__0'::'<>8__2'
				IL_02f4: br IL_009b
			} // end .try
			catch [mscorlib]System.Exception
			{
				IL_02f9: stloc.2
				IL_02fa: ldarg.0
				IL_02fb: ldc.i4.s -2
				IL_02fd: stfld int32 C/'<M>d__0'::'<>1__state'
				IL_0302: ldarg.0
				IL_0303: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
				IL_0308: ldloc.2
				IL_0309: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetException(class [mscorlib]System.Exception)
				IL_030e: leave.s IL_0310
			} // end handler
			IL_0310: ret
		} // end of method '<M>d__0'::MoveNext
		.method private final hidebysig newslot virtual 
			instance void SetStateMachine (
				class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
			) cil managed 
		{
			.custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
			// Method begins at RVA 0x2408
			// Code size 13 (0xd)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
			IL_0006: ldarg.1
			IL_0007: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
			IL_000c: ret
		} // end of method '<M>d__0'::SetStateMachine
	} // end of class <M>d__0
	// Methods
	.method public hidebysig 
		instance class [mscorlib]System.Threading.Tasks.Task M () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = (
			01 00 09 43 2b 3c 4d 3e 64 5f 5f 30 00 00
		)
		// Method begins at RVA 0x2050
		// Code size 49 (0x31)
		.maxstack 2
		.locals init (
			[0] valuetype C/'<M>d__0',
			[1] valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder
		)
		IL_0000: ldloca.s 0
		IL_0002: call valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Create()
		IL_0007: stfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
		IL_000c: ldloca.s 0
		IL_000e: ldc.i4.m1
		IL_000f: stfld int32 C/'<M>d__0'::'<>1__state'
		IL_0014: ldloc.0
		IL_0015: ldfld valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
		IL_001a: stloc.1
		IL_001b: ldloca.s 1
		IL_001d: ldloca.s 0
		IL_001f: call instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Start<valuetype C/'<M>d__0'>(!!0&)
		IL_0024: ldloca.s 0
		IL_0026: ldflda valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder C/'<M>d__0'::'<>t__builder'
		IL_002b: call instance class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::get_Task()
		IL_0030: ret
	} // end of method C::M
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x208d
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [mscorlib]System.Object::.ctor()
		IL_0006: ret
	} // end of method C::.ctor
} // end of class C");
        }
    }
}
