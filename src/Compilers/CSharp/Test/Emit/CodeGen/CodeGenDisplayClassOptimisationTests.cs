using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenDisplayClassOptimizationTests : CSharpTestBase
    {
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      185 (0xb9)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.0
  IL_003a:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003f:  br.s       IL_0081
  IL_0041:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0046:  stloc.2
  IL_0047:  ldloc.2
  IL_0048:  ldloc.0
  IL_0049:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_004e:  ldloc.2
  IL_004f:  ldloc.2
  IL_0050:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0055:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_005a:  stfld      ""int Program.<>c__DisplayClass0_1.x""
  IL_005f:  ldloc.1
  IL_0060:  ldloc.2
  IL_0061:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0067:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006c:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0071:  ldloc.0
  IL_0072:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0077:  stloc.3
  IL_0078:  ldloc.0
  IL_0079:  ldloc.3
  IL_007a:  ldc.i4.1
  IL_007b:  add
  IL_007c:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0081:  ldloc.0
  IL_0082:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0087:  ldloc.0
  IL_0088:  ldfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_008d:  callvirt   ""int System.Collections.Generic.List<string>.Count.get""
  IL_0092:  blt.s      IL_0041
  IL_0094:  ldloc.1
  IL_0095:  ldc.i4.0
  IL_0096:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_009b:  callvirt   ""void System.Action.Invoke()""
  IL_00a0:  ldloc.1
  IL_00a1:  ldc.i4.1
  IL_00a2:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00a7:  callvirt   ""void System.Action.Invoke()""
  IL_00ac:  ldloc.1
  IL_00ad:  ldc.i4.2
  IL_00ae:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00b3:  callvirt   ""void System.Action.Invoke()""
  IL_00b8:  ret
}");
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
            compilation.VerifyIL(
                "C.Main",
                @"{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1, //y
                C.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0045
  IL_0011:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  ldloc.0
  IL_0019:  stfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_001e:  ldloc.2
  IL_001f:  ldc.i4.0
  IL_0020:  stfld      ""int C.<>c__DisplayClass0_1.i""
  IL_0025:  br.s       IL_0037
  IL_0027:  ldloc.2
  IL_0028:  ldfld      ""int C.<>c__DisplayClass0_1.i""
  IL_002d:  stloc.3
  IL_002e:  ldloc.2
  IL_002f:  ldloc.3
  IL_0030:  ldc.i4.1
  IL_0031:  add
  IL_0032:  stfld      ""int C.<>c__DisplayClass0_1.i""
  IL_0037:  ldloc.2
  IL_0038:  ldfld      ""int C.<>c__DisplayClass0_1.i""
  IL_003d:  ldc.i4.s   10
  IL_003f:  blt.s      IL_0027
  IL_0041:  ldloc.1
  IL_0042:  ldc.i4.1
  IL_0043:  add
  IL_0044:  stloc.1
  IL_0045:  ldloc.1
  IL_0046:  ldc.i4.s   10
  IL_0048:  blt.s      IL_0011
  IL_004a:  ret
}");
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
            compilation.VerifyIL(
                "C.Main",
                @"{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1, //y
                C.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  IL_000f:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  ldloc.0
  IL_0017:  stfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4.0
  IL_001e:  stfld      ""int C.<>c__DisplayClass0_1.i""
  IL_0023:  br.s       IL_0035
  IL_0025:  ldloc.2
  IL_0026:  ldfld      ""int C.<>c__DisplayClass0_1.i""
  IL_002b:  stloc.3
  IL_002c:  ldloc.2
  IL_002d:  ldloc.3
  IL_002e:  ldc.i4.1
  IL_002f:  add
  IL_0030:  stfld      ""int C.<>c__DisplayClass0_1.i""
  IL_0035:  ldloc.2
  IL_0036:  ldfld      ""int C.<>c__DisplayClass0_1.i""
  IL_003b:  ldc.i4.s   10
  IL_003d:  blt.s      IL_0025
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4.1
  IL_0041:  add
  IL_0042:  stloc.1
  IL_0043:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      185 (0xb9)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.0
  IL_003a:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003f:  br.s       IL_0081
  IL_0041:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0046:  stloc.2
  IL_0047:  ldloc.2
  IL_0048:  ldloc.0
  IL_0049:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_004e:  ldloc.1
  IL_004f:  ldloc.2
  IL_0050:  ldloc.2
  IL_0051:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0056:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_005b:  stfld      ""int Program.<>c__DisplayClass0_1.x""
  IL_0060:  ldloc.2
  IL_0061:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0067:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006c:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0071:  ldloc.0
  IL_0072:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0077:  stloc.3
  IL_0078:  ldloc.0
  IL_0079:  ldloc.3
  IL_007a:  ldc.i4.1
  IL_007b:  add
  IL_007c:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0081:  ldloc.0
  IL_0082:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0087:  ldloc.0
  IL_0088:  ldfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_008d:  callvirt   ""int System.Collections.Generic.List<string>.Count.get""
  IL_0092:  blt.s      IL_0041
  IL_0094:  ldloc.1
  IL_0095:  ldc.i4.0
  IL_0096:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_009b:  callvirt   ""void System.Action.Invoke()""
  IL_00a0:  ldloc.1
  IL_00a1:  ldc.i4.1
  IL_00a2:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00a7:  callvirt   ""void System.Action.Invoke()""
  IL_00ac:  ldloc.1
  IL_00ad:  ldc.i4.2
  IL_00ae:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00b3:  callvirt   ""void System.Action.Invoke()""
  IL_00b8:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      183 (0xb7)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                System.Collections.Generic.IEnumerator<int> V_2,
                Program.<>c__DisplayClass0_1 V_3) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldc.i4.0
  IL_0039:  ldc.i4.3
  IL_003a:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Range(int, int)""
  IL_003f:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_0044:  stloc.2
  .try
  {
    IL_0045:  br.s       IL_007e
    IL_0047:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
    IL_004c:  stloc.3
    IL_004d:  ldloc.3
    IL_004e:  ldloc.0
    IL_004f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0054:  ldloc.3
    IL_0055:  ldloc.2
    IL_0056:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
    IL_005b:  stfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0060:  ldloc.3
    IL_0061:  ldloc.3
    IL_0062:  ldfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0067:  stfld      ""int Program.<>c__DisplayClass0_1.x""
    IL_006c:  ldloc.1
    IL_006d:  ldloc.3
    IL_006e:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
    IL_0074:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0079:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
    IL_007e:  ldloc.2
    IL_007f:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0084:  brtrue.s   IL_0047
    IL_0086:  leave.s    IL_0092
  }
  finally
  {
    IL_0088:  ldloc.2
    IL_0089:  brfalse.s  IL_0091
    IL_008b:  ldloc.2
    IL_008c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0091:  endfinally
  }
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4.0
  IL_0094:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0099:  callvirt   ""void System.Action.Invoke()""
  IL_009e:  ldloc.1
  IL_009f:  ldc.i4.1
  IL_00a0:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00a5:  callvirt   ""void System.Action.Invoke()""
  IL_00aa:  ldloc.1
  IL_00ab:  ldc.i4.2
  IL_00ac:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00b1:  callvirt   ""void System.Action.Invoke()""
  IL_00b6:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      183 (0xb7)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                System.Collections.Generic.IEnumerator<int> V_2,
                Program.<>c__DisplayClass0_1 V_3) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldc.i4.0
  IL_0039:  ldc.i4.3
  IL_003a:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Range(int, int)""
  IL_003f:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_0044:  stloc.2
  .try
  {
    IL_0045:  br.s       IL_007e
    IL_0047:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
    IL_004c:  stloc.3
    IL_004d:  ldloc.3
    IL_004e:  ldloc.0
    IL_004f:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0054:  ldloc.3
    IL_0055:  ldloc.2
    IL_0056:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
    IL_005b:  stfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0060:  ldloc.1
    IL_0061:  ldloc.3
    IL_0062:  ldloc.3
    IL_0063:  ldfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0068:  stfld      ""int Program.<>c__DisplayClass0_1.x""
    IL_006d:  ldloc.3
    IL_006e:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
    IL_0074:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0079:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
    IL_007e:  ldloc.2
    IL_007f:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0084:  brtrue.s   IL_0047
    IL_0086:  leave.s    IL_0092
  }
  finally
  {
    IL_0088:  ldloc.2
    IL_0089:  brfalse.s  IL_0091
    IL_008b:  ldloc.2
    IL_008c:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0091:  endfinally
  }
  IL_0092:  ldloc.1
  IL_0093:  ldc.i4.0
  IL_0094:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0099:  callvirt   ""void System.Action.Invoke()""
  IL_009e:  ldloc.1
  IL_009f:  ldc.i4.1
  IL_00a0:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00a5:  callvirt   ""void System.Action.Invoke()""
  IL_00aa:  ldloc.1
  IL_00ab:  ldc.i4.2
  IL_00ac:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00b1:  callvirt   ""void System.Action.Invoke()""
  IL_00b6:  ret
}");
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
    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token)
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
            compilation.VerifyIL(
                "Program.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
                @"{
  // Code size      388 (0x184)
  .maxstack  4
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals0
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_00f3
    IL_000d:  ldarg.0
    IL_000e:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_0013:  stfld      ""Program.<>c__DisplayClass0_0 Program.<M>d__0.<>8__1""
    IL_0018:  ldarg.0
    IL_0019:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
    IL_001e:  stfld      ""System.Collections.Generic.List<System.Action> Program.<M>d__0.<actions>5__2""
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<M>d__0.<>8__1""
    IL_0029:  newobj     ""System.Collections.Generic.List<string>..ctor()""
    IL_002e:  dup
    IL_002f:  ldstr      ""one""
    IL_0034:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
    IL_0039:  dup
    IL_003a:  ldstr      ""two""
    IL_003f:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
    IL_0044:  dup
    IL_0045:  ldstr      ""three""
    IL_004a:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
    IL_004f:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
    IL_0054:  ldarg.0
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""C Program.<M>d__0.enumerable""
    IL_005b:  ldloca.s   V_1
    IL_005d:  initobj    ""System.Threading.CancellationToken""
    IL_0063:  ldloc.1
    IL_0064:  callvirt   ""C.Enumerator C.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0069:  stfld      ""C.Enumerator Program.<M>d__0.<>7__wrap2""
    IL_006e:  br.s       IL_00b6
    IL_0070:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
    IL_0075:  stloc.2
    IL_0076:  ldloc.2
    IL_0077:  ldarg.0
    IL_0078:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<M>d__0.<>8__1""
    IL_007d:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0082:  ldloc.2
    IL_0083:  ldarg.0
    IL_0084:  ldfld      ""C.Enumerator Program.<M>d__0.<>7__wrap2""
    IL_0089:  callvirt   ""int C.Enumerator.Current.get""
    IL_008e:  stfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0093:  ldarg.0
    IL_0094:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<M>d__0.<actions>5__2""
    IL_0099:  ldloc.2
    IL_009a:  ldloc.2
    IL_009b:  ldfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_00a0:  stfld      ""int Program.<>c__DisplayClass0_1.x""
    IL_00a5:  ldloc.2
    IL_00a6:  ldftn      ""void Program.<>c__DisplayClass0_1.<M>b__0()""
    IL_00ac:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_00b1:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""C.Enumerator Program.<M>d__0.<>7__wrap2""
    IL_00bc:  callvirt   ""System.Threading.Tasks.Task<bool> C.Enumerator.MoveNextAsync()""
    IL_00c1:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_00c6:  stloc.3
    IL_00c7:  ldloca.s   V_3
    IL_00c9:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_00ce:  brtrue.s   IL_010f
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.0
    IL_00d2:  dup
    IL_00d3:  stloc.0
    IL_00d4:  stfld      ""int Program.<M>d__0.<>1__state""
    IL_00d9:  ldarg.0
    IL_00da:  ldloc.3
    IL_00db:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> Program.<M>d__0.<>u__1""
    IL_00e0:  ldarg.0
    IL_00e1:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<M>d__0.<>t__builder""
    IL_00e6:  ldloca.s   V_3
    IL_00e8:  ldarg.0
    IL_00e9:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, Program.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref Program.<M>d__0)""
    IL_00ee:  leave      IL_0183
    IL_00f3:  ldarg.0
    IL_00f4:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> Program.<M>d__0.<>u__1""
    IL_00f9:  stloc.3
    IL_00fa:  ldarg.0
    IL_00fb:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> Program.<M>d__0.<>u__1""
    IL_0100:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0106:  ldarg.0
    IL_0107:  ldc.i4.m1
    IL_0108:  dup
    IL_0109:  stloc.0
    IL_010a:  stfld      ""int Program.<M>d__0.<>1__state""
    IL_010f:  ldloca.s   V_3
    IL_0111:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0116:  brtrue     IL_0070
    IL_011b:  ldarg.0
    IL_011c:  ldnull
    IL_011d:  stfld      ""C.Enumerator Program.<M>d__0.<>7__wrap2""
    IL_0122:  ldarg.0
    IL_0123:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<M>d__0.<actions>5__2""
    IL_0128:  ldc.i4.0
    IL_0129:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
    IL_012e:  callvirt   ""void System.Action.Invoke()""
    IL_0133:  ldarg.0
    IL_0134:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<M>d__0.<actions>5__2""
    IL_0139:  ldc.i4.1
    IL_013a:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
    IL_013f:  callvirt   ""void System.Action.Invoke()""
    IL_0144:  ldarg.0
    IL_0145:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<M>d__0.<actions>5__2""
    IL_014a:  ldc.i4.2
    IL_014b:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
    IL_0150:  callvirt   ""void System.Action.Invoke()""
    IL_0155:  leave.s    IL_0170
  }
  catch System.Exception
  {
    IL_0157:  stloc.s    V_4
    IL_0159:  ldarg.0
    IL_015a:  ldc.i4.s   -2
    IL_015c:  stfld      ""int Program.<M>d__0.<>1__state""
    IL_0161:  ldarg.0
    IL_0162:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<M>d__0.<>t__builder""
    IL_0167:  ldloc.s    V_4
    IL_0169:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_016e:  leave.s    IL_0183
  }
  IL_0170:  ldarg.0
  IL_0171:  ldc.i4.s   -2
  IL_0173:  stfld      ""int Program.<M>d__0.<>1__state""
  IL_0178:  ldarg.0
  IL_0179:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<M>d__0.<>t__builder""
  IL_017e:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_0183:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       84 (0x54)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1) //actions
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.0
  IL_0024:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0029:  ldloc.0
  IL_002a:  ldloc.0
  IL_002b:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0030:  stfld      ""int Program.<>c__DisplayClass0_0.x""
  IL_0035:  ldloc.1
  IL_0036:  ldloc.0
  IL_0037:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_003d:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0042:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.0
  IL_0049:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_004e:  callvirt   ""void System.Action.Invoke()""
  IL_0053:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       84 (0x54)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1) //actions
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.0
  IL_0024:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0029:  ldloc.1
  IL_002a:  ldloc.0
  IL_002b:  ldloc.0
  IL_002c:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0031:  stfld      ""int Program.<>c__DisplayClass0_0.x""
  IL_0036:  ldloc.0
  IL_0037:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_003d:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0042:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.0
  IL_0049:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_004e:  callvirt   ""void System.Action.Invoke()""
  IL_0053:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      114 (0x72)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1) //actions
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.0
  IL_0024:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_002f:  ldc.i4.0
  IL_0030:  callvirt   ""string System.Collections.Generic.List<string>.this[int].get""
  IL_0035:  ldstr      ""one""
  IL_003a:  call       ""bool string.op_Inequality(string, string)""
  IL_003f:  brfalse.s  IL_0047
  IL_0041:  newobj     ""System.Exception..ctor()""
  IL_0046:  throw
  IL_0047:  ldloc.1
  IL_0048:  ldloc.0
  IL_0049:  ldloc.0
  IL_004a:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_004f:  stfld      ""int Program.<>c__DisplayClass0_0.x""
  IL_0054:  ldloc.0
  IL_0055:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_005b:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0060:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0065:  ldloc.1
  IL_0066:  ldc.i4.0
  IL_0067:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_006c:  callvirt   ""void System.Action.Invoke()""
  IL_0071:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      105 (0x69)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1) //actions
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0022:  ldloc.0
  IL_0023:  newobj     ""Program.Disposable..ctor()""
  IL_0028:  stfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
  .try
  {
    IL_002d:  ldloc.0
    IL_002e:  ldc.i4.0
    IL_002f:  stfld      ""int Program.<>c__DisplayClass0_0.i""
    IL_0034:  ldloc.1
    IL_0035:  ldloc.0
    IL_0036:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
    IL_003c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0041:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
    IL_0046:  leave.s    IL_005c
  }
  finally
  {
    IL_0048:  ldloc.0
    IL_0049:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
    IL_004e:  brfalse.s  IL_005b
    IL_0050:  ldloc.0
    IL_0051:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
    IL_0056:  callvirt   ""void System.IDisposable.Dispose()""
    IL_005b:  endfinally
  }
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4.0
  IL_005e:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0063:  callvirt   ""void System.Action.Invoke()""
  IL_0068:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      105 (0x69)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1) //actions
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0022:  ldloc.0
  IL_0023:  newobj     ""Program.Disposable..ctor()""
  IL_0028:  stfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
  .try
  {
    IL_002d:  ldloc.1
    IL_002e:  ldloc.0
    IL_002f:  ldc.i4.0
    IL_0030:  stfld      ""int Program.<>c__DisplayClass0_0.i""
    IL_0035:  ldloc.0
    IL_0036:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
    IL_003c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0041:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
    IL_0046:  leave.s    IL_005c
  }
  finally
  {
    IL_0048:  ldloc.0
    IL_0049:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
    IL_004e:  brfalse.s  IL_005b
    IL_0050:  ldloc.0
    IL_0051:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_0.disposable""
    IL_0056:  callvirt   ""void System.IDisposable.Dispose()""
    IL_005b:  endfinally
  }
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4.0
  IL_005e:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0063:  callvirt   ""void System.Action.Invoke()""
  IL_0068:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      310 (0x136)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                System.Collections.Generic.IEnumerator<int> V_2,
                Program.<>c__DisplayClass0_1 V_3, //CS$<>8__locals1
                Program.<>c__DisplayClass0_2 V_4, //CS$<>8__locals2
                int V_5)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldc.i4.0
  IL_0039:  ldc.i4.1
  IL_003a:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Range(int, int)""
  IL_003f:  callvirt   ""System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()""
  IL_0044:  stloc.2
  .try
  {
    IL_0045:  br         IL_00fa
    IL_004a:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
    IL_004f:  stloc.3
    IL_0050:  ldloc.3
    IL_0051:  ldloc.0
    IL_0052:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0057:  ldloc.3
    IL_0058:  ldloc.2
    IL_0059:  callvirt   ""int System.Collections.Generic.IEnumerator<int>.Current.get""
    IL_005e:  stfld      ""int Program.<>c__DisplayClass0_1.i""
    IL_0063:  ldloc.3
    IL_0064:  ldc.i4.0
    IL_0065:  stfld      ""int Program.<>c__DisplayClass0_1.j""
    IL_006a:  br.s       IL_00df
    IL_006c:  newobj     ""Program.<>c__DisplayClass0_2..ctor()""
    IL_0071:  stloc.s    V_4
    IL_0073:  ldloc.s    V_4
    IL_0075:  ldloc.3
    IL_0076:  stfld      ""Program.<>c__DisplayClass0_1 Program.<>c__DisplayClass0_2.CS$<>8__locals2""
    IL_007b:  ldloc.s    V_4
    IL_007d:  newobj     ""Program.Disposable..ctor()""
    IL_0082:  stfld      ""Program.Disposable Program.<>c__DisplayClass0_2.disposable""
    .try
    {
      IL_0087:  ldloc.s    V_4
      IL_0089:  ldloc.s    V_4
      IL_008b:  ldfld      ""Program.<>c__DisplayClass0_1 Program.<>c__DisplayClass0_2.CS$<>8__locals2""
      IL_0090:  ldfld      ""int Program.<>c__DisplayClass0_1.j""
      IL_0095:  stfld      ""int Program.<>c__DisplayClass0_2.x""
      IL_009a:  ldloc.1
      IL_009b:  ldloc.s    V_4
      IL_009d:  ldc.i4.0
      IL_009e:  stfld      ""int Program.<>c__DisplayClass0_2.y""
      IL_00a3:  ldloc.s    V_4
      IL_00a5:  ldftn      ""void Program.<>c__DisplayClass0_2.<Main>b__0()""
      IL_00ab:  newobj     ""System.Action..ctor(object, System.IntPtr)""
      IL_00b0:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
      IL_00b5:  leave.s    IL_00cd
    }
    finally
    {
      IL_00b7:  ldloc.s    V_4
      IL_00b9:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_2.disposable""
      IL_00be:  brfalse.s  IL_00cc
      IL_00c0:  ldloc.s    V_4
      IL_00c2:  ldfld      ""Program.Disposable Program.<>c__DisplayClass0_2.disposable""
      IL_00c7:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00cc:  endfinally
    }
    IL_00cd:  ldloc.3
    IL_00ce:  ldfld      ""int Program.<>c__DisplayClass0_1.j""
    IL_00d3:  stloc.s    V_5
    IL_00d5:  ldloc.3
    IL_00d6:  ldloc.s    V_5
    IL_00d8:  ldc.i4.1
    IL_00d9:  add
    IL_00da:  stfld      ""int Program.<>c__DisplayClass0_1.j""
    IL_00df:  ldloc.3
    IL_00e0:  ldfld      ""int Program.<>c__DisplayClass0_1.j""
    IL_00e5:  ldloc.3
    IL_00e6:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_00eb:  ldfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
    IL_00f0:  callvirt   ""int System.Collections.Generic.List<string>.Count.get""
    IL_00f5:  blt        IL_006c
    IL_00fa:  ldloc.2
    IL_00fb:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0100:  brtrue     IL_004a
    IL_0105:  leave.s    IL_0111
  }
  finally
  {
    IL_0107:  ldloc.2
    IL_0108:  brfalse.s  IL_0110
    IL_010a:  ldloc.2
    IL_010b:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0110:  endfinally
  }
  IL_0111:  ldloc.1
  IL_0112:  ldc.i4.0
  IL_0113:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0118:  callvirt   ""void System.Action.Invoke()""
  IL_011d:  ldloc.1
  IL_011e:  ldc.i4.1
  IL_011f:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0124:  callvirt   ""void System.Action.Invoke()""
  IL_0129:  ldloc.1
  IL_012a:  ldc.i4.2
  IL_012b:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0130:  callvirt   ""void System.Action.Invoke()""
  IL_0135:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      150 (0x96)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                int V_2, //i
                Program.<>c__DisplayClass0_1 V_3) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldc.i4.0
  IL_0039:  stloc.2
  IL_003a:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_003f:  stloc.3
  IL_0040:  ldloc.3
  IL_0041:  ldloc.0
  IL_0042:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0047:  ldloc.3
  IL_0048:  ldloc.2
  IL_0049:  stfld      ""int Program.<>c__DisplayClass0_1.j""
  IL_004e:  ldloc.2
  IL_004f:  dup
  IL_0050:  ldc.i4.1
  IL_0051:  add
  IL_0052:  stloc.2
  IL_0053:  ldc.i4.3
  IL_0054:  bge.s      IL_0071
  IL_0056:  ldloc.1
  IL_0057:  ldloc.3
  IL_0058:  ldc.i4.0
  IL_0059:  stfld      ""int Program.<>c__DisplayClass0_1.x""
  IL_005e:  ldloc.3
  IL_005f:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0065:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006a:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_006f:  br.s       IL_003a
  IL_0071:  ldloc.1
  IL_0072:  ldc.i4.0
  IL_0073:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0078:  callvirt   ""void System.Action.Invoke()""
  IL_007d:  ldloc.1
  IL_007e:  ldc.i4.1
  IL_007f:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0084:  callvirt   ""void System.Action.Invoke()""
  IL_0089:  ldloc.1
  IL_008a:  ldc.i4.2
  IL_008b:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0090:  callvirt   ""void System.Action.Invoke()""
  IL_0095:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      158 (0x9e)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.0
  IL_003a:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003f:  br.s       IL_0071
  IL_0041:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0046:  stloc.2
  IL_0047:  ldloc.2
  IL_0048:  ldloc.0
  IL_0049:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_004e:  ldloc.1
  IL_004f:  ldloc.2
  IL_0050:  ldloc.2
  IL_0051:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0056:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_005b:  stfld      ""int Program.<>c__DisplayClass0_1.j""
  IL_0060:  ldloc.2
  IL_0061:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0067:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006c:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_0071:  ldloc.0
  IL_0072:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0077:  ldc.i4.1
  IL_0078:  add
  IL_0079:  stloc.3
  IL_007a:  ldloc.0
  IL_007b:  ldloc.3
  IL_007c:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0081:  ldloc.3
  IL_0082:  ldc.i4.3
  IL_0083:  blt.s      IL_0041
  IL_0085:  ldloc.1
  IL_0086:  ldc.i4.0
  IL_0087:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_008c:  callvirt   ""void System.Action.Invoke()""
  IL_0091:  ldloc.1
  IL_0092:  ldc.i4.1
  IL_0093:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0098:  callvirt   ""void System.Action.Invoke()""
  IL_009d:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      131 (0x83)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //action
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_000c:  dup
  IL_000d:  ldstr      ""one""
  IL_0012:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0017:  dup
  IL_0018:  ldstr      ""two""
  IL_001d:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0022:  dup
  IL_0023:  ldstr      ""three""
  IL_0028:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_002d:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0032:  ldnull
  IL_0033:  stloc.1
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003b:  br.s       IL_0068
  IL_003d:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0042:  stloc.2
  IL_0043:  ldloc.2
  IL_0044:  ldloc.0
  IL_0045:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_004a:  ldloc.2
  IL_004b:  ldloc.2
  IL_004c:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0051:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0056:  stfld      ""int Program.<>c__DisplayClass0_1.j""
  IL_005b:  ldloc.2
  IL_005c:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0062:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0067:  stloc.1
  IL_0068:  ldloc.0
  IL_0069:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_006e:  ldc.i4.1
  IL_006f:  add
  IL_0070:  stloc.3
  IL_0071:  ldloc.0
  IL_0072:  ldloc.3
  IL_0073:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0078:  ldloc.3
  IL_0079:  ldc.i4.2
  IL_007a:  blt.s      IL_003d
  IL_007c:  ldloc.1
  IL_007d:  callvirt   ""void System.Action.Invoke()""
  IL_0082:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //action
                Program.<>c__DisplayClass0_1 V_2) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_000c:  dup
  IL_000d:  ldstr      ""one""
  IL_0012:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0017:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_001c:  ldnull
  IL_001d:  stloc.1
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.0
  IL_0020:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0025:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_002a:  stloc.2
  IL_002b:  ldloc.2
  IL_002c:  ldloc.0
  IL_002d:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0032:  ldloc.2
  IL_0033:  ldloc.2
  IL_0034:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0039:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003e:  stfld      ""int Program.<>c__DisplayClass0_1.j""
  IL_0043:  ldloc.2
  IL_0044:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_004a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_004f:  dup
  IL_0050:  stloc.1
  IL_0051:  pop
  IL_0052:  ldloc.1
  IL_0053:  callvirt   ""void System.Action.Invoke()""
  IL_0058:  ret
}");
        }

        [Fact]
        public void ForWithVariableDeclaredInConditionAndNoneInIntializerCorrectDisplayClassesAreCreated()
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Action V_1, //action
                Program.<>c__DisplayClass0_1 V_2) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_000c:  dup
  IL_000d:  ldstr      ""one""
  IL_0012:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0017:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_001c:  ldnull
  IL_001d:  stloc.1
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.0
  IL_0020:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0025:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_002a:  stloc.2
  IL_002b:  ldloc.2
  IL_002c:  ldloc.0
  IL_002d:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0032:  ldloc.2
  IL_0033:  ldloc.2
  IL_0034:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0039:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003e:  stfld      ""int Program.<>c__DisplayClass0_1.j""
  IL_0043:  ldloc.2
  IL_0044:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_004a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_004f:  dup
  IL_0050:  stloc.1
  IL_0051:  pop
  IL_0052:  ldloc.1
  IL_0053:  callvirt   ""void System.Action.Invoke()""
  IL_0058:  ret
}");
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
            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      168 (0xa8)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Collections.Generic.List<System.Action> V_1, //actions
                Program.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                int V_3)
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0012:  dup
  IL_0013:  ldstr      ""one""
  IL_0018:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_001d:  dup
  IL_001e:  ldstr      ""two""
  IL_0023:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0028:  dup
  IL_0029:  ldstr      ""three""
  IL_002e:  callvirt   ""void System.Collections.Generic.List<string>.Add(string)""
  IL_0033:  stfld      ""System.Collections.Generic.List<string> Program.<>c__DisplayClass0_0.strings""
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.0
  IL_003a:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_003f:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0044:  stloc.2
  IL_0045:  ldloc.2
  IL_0046:  ldloc.0
  IL_0047:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_004c:  ldloc.1
  IL_004d:  ldloc.2
  IL_004e:  ldloc.2
  IL_004f:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0054:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0059:  stfld      ""int Program.<>c__DisplayClass0_1.x""
  IL_005e:  ldloc.2
  IL_005f:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0065:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006a:  callvirt   ""void System.Collections.Generic.List<System.Action>.Add(System.Action)""
  IL_006f:  ldloc.0
  IL_0070:  ldfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_0075:  ldc.i4.1
  IL_0076:  add
  IL_0077:  stloc.3
  IL_0078:  ldloc.0
  IL_0079:  ldloc.3
  IL_007a:  stfld      ""int Program.<>c__DisplayClass0_0.i""
  IL_007f:  ldloc.3
  IL_0080:  ldc.i4.3
  IL_0081:  blt.s      IL_003f
  IL_0083:  ldloc.1
  IL_0084:  ldc.i4.0
  IL_0085:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_008a:  callvirt   ""void System.Action.Invoke()""
  IL_008f:  ldloc.1
  IL_0090:  ldc.i4.1
  IL_0091:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0096:  callvirt   ""void System.Action.Invoke()""
  IL_009b:  ldloc.1
  IL_009c:  ldc.i4.2
  IL_009d:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_00a2:  callvirt   ""void System.Action.Invoke()""
  IL_00a7:  ret
}");
        }

        [Fact]
        public void ScopeContainsGoToDoNotMergeDisplayClasses()
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


		goto target;
		target: ;
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0012:  dup
  IL_0013:  ldloc.0
  IL_0014:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0019:  dup
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      ""int Program.<>c__DisplayClass0_1.b""
  IL_0020:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0026:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002b:  callvirt   ""void System.Action.Invoke()""
  IL_0030:  ret
}");
        }

        [Fact]
        public void ScopeContainsScopeContainingGoToDoNotMergeDisplayClasses()
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


        {
		    goto target;
		    target: ;
        }
	}
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"1");

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0012:  dup
  IL_0013:  ldloc.0
  IL_0014:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0019:  dup
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      ""int Program.<>c__DisplayClass0_1.b""
  IL_0020:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0026:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002b:  callvirt   ""void System.Action.Invoke()""
  IL_0030:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0012:  dup
  IL_0013:  ldloc.0
  IL_0014:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0019:  dup
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      ""int Program.<>c__DisplayClass0_1.b""
  IL_0020:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__1()""
  IL_0026:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002b:  callvirt   ""void System.Action.Invoke()""
  IL_0030:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      ""int Program.<>c__DisplayClass0_0.b""
  IL_0013:  dup
  IL_0014:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_001a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void System.Action.Invoke()""
  IL_0024:  pop
  IL_0025:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      ""int Program.<>c__DisplayClass0_0.b""
  IL_0013:  dup
  IL_0014:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_001a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001f:  callvirt   ""void System.Action.Invoke()""
  IL_0024:  pop
  IL_0025:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0012:  dup
  IL_0013:  ldloc.0
  IL_0014:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0019:  dup
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      ""int Program.<>c__DisplayClass0_1.b""
  IL_0020:  dup
  IL_0021:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0027:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002c:  callvirt   ""void System.Action.Invoke()""
  IL_0031:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0036:  pop
  IL_0037:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.1
  IL_000f:  stfld      ""int Program.<>c__DisplayClass0_0.b""
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  stfld      ""int Program.<>c__DisplayClass0_0.c""
  IL_001b:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0020:  dup
  IL_0021:  ldloc.0
  IL_0022:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0027:  dup
  IL_0028:  ldc.i4.3
  IL_0029:  stfld      ""int Program.<>c__DisplayClass0_1.d""
  IL_002e:  dup
  IL_002f:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__0()""
  IL_0035:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003a:  callvirt   ""void System.Action.Invoke()""
  IL_003f:  ldfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0044:  pop
  IL_0045:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       58 (0x3a)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.1
  IL_000f:  stfld      ""int Program.<>c__DisplayClass0_0.b""
  IL_0014:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0019:  dup
  IL_001a:  ldloc.0
  IL_001b:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0020:  dup
  IL_0021:  ldc.i4.2
  IL_0022:  stfld      ""int Program.<>c__DisplayClass0_1.c""
  IL_0027:  dup
  IL_0028:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__1()""
  IL_002e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0033:  callvirt   ""void System.Action.Invoke()""
  IL_0038:  pop
  IL_0039:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.1
  IL_000f:  stfld      ""int Program.<>c__DisplayClass0_0.b""
  IL_0014:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0019:  dup
  IL_001a:  ldloc.0
  IL_001b:  stfld      ""Program.<>c__DisplayClass0_0 Program.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0020:  dup
  IL_0021:  ldc.i4.2
  IL_0022:  stfld      ""int Program.<>c__DisplayClass0_1.c""
  IL_0027:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__1()""
  IL_002d:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0032:  callvirt   ""void System.Action.Invoke()""
  IL_0037:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size       49 (0x31)
  .maxstack  4
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_000c:  newobj     ""Program.<>c__DisplayClass0_1..ctor()""
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  stfld      ""int Program.<>c__DisplayClass0_1.b""
  IL_0018:  dup
  IL_0019:  ldc.i4.2
  IL_001a:  stfld      ""int Program.<>c__DisplayClass0_1.c""
  IL_001f:  ldftn      ""void Program.<>c__DisplayClass0_1.<Main>b__1()""
  IL_0025:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002a:  callvirt   ""void System.Action.Invoke()""
  IL_002f:  pop
  IL_0030:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      103 (0x67)
  .maxstack  3
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0010:  dup
  IL_0011:  ldc.i4.1
  IL_0012:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_0017:  dup
  IL_0018:  callvirt   ""void Program.<>c__DisplayClass0_0.<Main>g__M|0()""
  IL_001d:  dup
  IL_001e:  callvirt   ""void Program.<>c__DisplayClass0_0.<Main>g__M|0()""
  IL_0023:  dup
  IL_0024:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0029:  ldc.i4.0
  IL_002a:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_002f:  callvirt   ""void System.Action.Invoke()""
  IL_0034:  dup
  IL_0035:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_003a:  ldc.i4.2
  IL_003b:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0040:  callvirt   ""void System.Action.Invoke()""
  IL_0045:  dup
  IL_0046:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_004b:  ldc.i4.1
  IL_004c:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0051:  callvirt   ""void System.Action.Invoke()""
  IL_0056:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_005b:  ldc.i4.3
  IL_005c:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_0061:  callvirt   ""void System.Action.Invoke()""
  IL_0066:  ret
}");
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

            compilation.VerifyIL(
                "Program.Main",
                @"{
  // Code size      114 (0x72)
  .maxstack  3
  IL_0000:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""System.Collections.Generic.List<System.Action>..ctor()""
  IL_000b:  stfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0010:  dup
  IL_0011:  ldc.i4.1
  IL_0012:  stfld      ""int Program.<>c__DisplayClass0_0.a""
  IL_0017:  dup
  IL_0018:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
  IL_001e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0023:  dup
  IL_0024:  callvirt   ""void System.Action.Invoke()""
  IL_0029:  callvirt   ""void System.Action.Invoke()""
  IL_002e:  dup
  IL_002f:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0034:  ldc.i4.0
  IL_0035:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_003a:  callvirt   ""void System.Action.Invoke()""
  IL_003f:  dup
  IL_0040:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0045:  ldc.i4.2
  IL_0046:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_004b:  callvirt   ""void System.Action.Invoke()""
  IL_0050:  dup
  IL_0051:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0056:  ldc.i4.1
  IL_0057:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_005c:  callvirt   ""void System.Action.Invoke()""
  IL_0061:  ldfld      ""System.Collections.Generic.List<System.Action> Program.<>c__DisplayClass0_0.actions""
  IL_0066:  ldc.i4.3
  IL_0067:  callvirt   ""System.Action System.Collections.Generic.List<System.Action>.this[int].get""
  IL_006c:  callvirt   ""void System.Action.Invoke()""
  IL_0071:  ret
}");
        }
    }
}
