// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBAsyncTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
        [WorkItem(631350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631350")]
        [WorkItem(643501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/643501")]
        [WorkItem(689616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689616")]
        public void TestAsyncDebug()
        {
            var text = WithWindowsLineBreaks(@"
using System;
using System.Threading;
using System.Threading.Tasks;
 
class DynamicMembers
{
    public Func<Task<int>> Prop { get; set; }
}
class TestCase
{
    public static int Count = 0;
    public async void Run()
    {
        DynamicMembers dc2 = new DynamicMembers();
        dc2.Prop = async () => { await Task.Delay(10000); return 3; };
        var rez2 = await dc2.Prop();
        if (rez2 == 3) Count++;
 
        Driver.Result = TestCase.Count - 1;
        //When test complete, set the flag.
        Driver.CompletedSignal.Set();
    }
}
class Driver
{
    public static int Result = -1;
    public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
    static int Main()
    {
        var t = new TestCase();
        t.Run();
 
        CompletedSignal.WaitOne();
        return Driver.Result;
    }
}");
            var compilation = CreateCompilationWithMscorlib45(text, options: TestOptions.DebugDll).VerifyDiagnostics();
            var v = CompileAndVerify(compilation);

            v.VerifyIL("TestCase.<Run>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      303 (0x12f)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                TestCase.<Run>d__1 V_2,
                bool V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int TestCase.<Run>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_008b
   -IL_000e:  nop
   -IL_000f:  ldarg.0
    IL_0010:  newobj     ""DynamicMembers..ctor()""
    IL_0015:  stfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
   -IL_001a:  ldarg.0
    IL_001b:  ldfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
    IL_0020:  ldsfld     ""System.Func<System.Threading.Tasks.Task<int>> TestCase.<>c.<>9__1_0""
    IL_0025:  dup
    IL_0026:  brtrue.s   IL_003f
    IL_0028:  pop
    IL_0029:  ldsfld     ""TestCase.<>c TestCase.<>c.<>9""
    IL_002e:  ldftn      ""System.Threading.Tasks.Task<int> TestCase.<>c.<Run>b__1_0()""
    IL_0034:  newobj     ""System.Func<System.Threading.Tasks.Task<int>>..ctor(object, System.IntPtr)""
    IL_0039:  dup
    IL_003a:  stsfld     ""System.Func<System.Threading.Tasks.Task<int>> TestCase.<>c.<>9__1_0""
    IL_003f:  callvirt   ""void DynamicMembers.Prop.set""
    IL_0044:  nop
   -IL_0045:  ldarg.0
    IL_0046:  ldfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
    IL_004b:  callvirt   ""System.Func<System.Threading.Tasks.Task<int>> DynamicMembers.Prop.get""
    IL_0050:  callvirt   ""System.Threading.Tasks.Task<int> System.Func<System.Threading.Tasks.Task<int>>.Invoke()""
    IL_0055:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_005a:  stloc.1
   ~IL_005b:  ldloca.s   V_1
    IL_005d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0062:  brtrue.s   IL_00a7
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.0
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int TestCase.<Run>d__1.<>1__state""
   <IL_006d:  ldarg.0
    IL_006e:  ldloc.1
    IL_006f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_0074:  ldarg.0
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
    IL_007c:  ldloca.s   V_1
    IL_007e:  ldloca.s   V_2
    IL_0080:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, TestCase.<Run>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref TestCase.<Run>d__1)""
    IL_0085:  nop
    IL_0086:  leave      IL_012e
   >IL_008b:  ldarg.0
    IL_008c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_0091:  stloc.1
    IL_0092:  ldarg.0
    IL_0093:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_0098:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.m1
    IL_00a0:  dup
    IL_00a1:  stloc.0
    IL_00a2:  stfld      ""int TestCase.<Run>d__1.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldloca.s   V_1
    IL_00aa:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00af:  stfld      ""int TestCase.<Run>d__1.<>s__3""
    IL_00b4:  ldarg.0
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      ""int TestCase.<Run>d__1.<>s__3""
    IL_00bb:  stfld      ""int TestCase.<Run>d__1.<rez2>5__2""
   -IL_00c0:  ldarg.0
    IL_00c1:  ldfld      ""int TestCase.<Run>d__1.<rez2>5__2""
    IL_00c6:  ldc.i4.3
    IL_00c7:  ceq
    IL_00c9:  stloc.3
   ~IL_00ca:  ldloc.3
    IL_00cb:  brfalse.s  IL_00d9
   -IL_00cd:  ldsfld     ""int TestCase.Count""
    IL_00d2:  ldc.i4.1
    IL_00d3:  add
    IL_00d4:  stsfld     ""int TestCase.Count""
   -IL_00d9:  ldsfld     ""int TestCase.Count""
    IL_00de:  ldc.i4.1
    IL_00df:  sub
    IL_00e0:  stsfld     ""int Driver.Result""
   -IL_00e5:  ldsfld     ""System.Threading.AutoResetEvent Driver.CompletedSignal""
    IL_00ea:  callvirt   ""bool System.Threading.EventWaitHandle.Set()""
    IL_00ef:  pop
    IL_00f0:  leave.s    IL_0113
  }
  catch System.Exception
  {
  ~$IL_00f2:  stloc.s    V_4
    IL_00f4:  ldarg.0
    IL_00f5:  ldc.i4.s   -2
    IL_00f7:  stfld      ""int TestCase.<Run>d__1.<>1__state""
    IL_00fc:  ldarg.0
    IL_00fd:  ldnull
    IL_00fe:  stfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
    IL_0103:  ldarg.0
    IL_0104:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
    IL_0109:  ldloc.s    V_4
    IL_010b:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0110:  nop
    IL_0111:  leave.s    IL_012e
  }
 -IL_0113:  ldarg.0
  IL_0114:  ldc.i4.s   -2
  IL_0116:  stfld      ""int TestCase.<Run>d__1.<>1__state""
 ~IL_011b:  ldarg.0
  IL_011c:  ldnull
  IL_011d:  stfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
  IL_0122:  ldarg.0
  IL_0123:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
  IL_0128:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_012d:  nop
  IL_012e:  ret
}",
sequencePoints: "TestCase+<Run>d__1.MoveNext");

            v.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""DynamicMembers"" name=""get_Prop"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""35"" endLine=""8"" endColumn=""39"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""DynamicMembers"" name=""set_Prop"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""40"" endLine=""8"" endColumn=""44"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""TestCase"" name=""Run"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Run&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""26"" />
          <slot kind=""0"" offset=""139"" />
          <slot kind=""28"" offset=""146"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <lambda offset=""86"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
    <method containingType=""TestCase"" name="".cctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""33"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
        <namespace name=""System.Threading"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""Driver"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""TestCase"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""30"" startColumn=""5"" endLine=""30"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""32"" document=""1"" />
        <entry offset=""0x7"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""17"" document=""1"" />
        <entry offset=""0xe"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""35"" document=""1"" />
        <entry offset=""0x19"" startLine=""35"" startColumn=""9"" endLine=""35"" endColumn=""30"" document=""1"" />
        <entry offset=""0x21"" startLine=""36"" startColumn=""5"" endLine=""36"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""t"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Driver"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""TestCase"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""27"" startColumn=""5"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0x6"" startLine=""28"" startColumn=""5"" endLine=""28"" endColumn=""78"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""TestCase+&lt;&gt;c"" name=""&lt;Run&gt;b__1_0"">
      <customDebugInfo>
        <forwardIterator name=""&lt;&lt;Run&gt;b__1_0&gt;d"" />
      </customDebugInfo>
    </method>
    <method containingType=""TestCase+&lt;Run&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""TestCase"" methodName="".cctor"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x12f"" />
          <slot startOffset=""0x0"" endOffset=""0x12f"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""146"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""173"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""51"" document=""1"" />
        <entry offset=""0x1a"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""71"" document=""1"" />
        <entry offset=""0x45"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""37"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0xc0"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""23"" document=""1"" />
        <entry offset=""0xca"" hidden=""true"" document=""1"" />
        <entry offset=""0xcd"" startLine=""18"" startColumn=""24"" endLine=""18"" endColumn=""32"" document=""1"" />
        <entry offset=""0xd9"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""44"" document=""1"" />
        <entry offset=""0xe5"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""38"" document=""1"" />
        <entry offset=""0xf2"" hidden=""true"" document=""1"" />
        <entry offset=""0x113"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x11b"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0xf2"" />
        <kickoffMethod declaringType=""TestCase"" methodName=""Run"" />
        <await yield=""0x6d"" resume=""0x8b"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
    <method containingType=""TestCase+&lt;&gt;c+&lt;&lt;Run&gt;b__1_0&gt;d"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""TestCase"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""86"" />
          <slot kind=""20"" offset=""86"" />
          <slot kind=""33"" offset=""88"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""16"" startColumn=""32"" endLine=""16"" endColumn=""33"" document=""1"" />
        <entry offset=""0xf"" startLine=""16"" startColumn=""34"" endLine=""16"" endColumn=""58"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x70"" startLine=""16"" startColumn=""59"" endLine=""16"" endColumn=""68"" document=""1"" />
        <entry offset=""0x74"" hidden=""true"" document=""1"" />
        <entry offset=""0x8e"" startLine=""16"" startColumn=""69"" endLine=""16"" endColumn=""70"" document=""1"" />
        <entry offset=""0x96"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""TestCase+&lt;&gt;c"" methodName=""&lt;Run&gt;b__1_0"" />
        <await yield=""0x31"" resume=""0x4c"" declaringType=""TestCase+&lt;&gt;c+&lt;&lt;Run&gt;b__1_0&gt;d"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(734596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734596")]
        public void TestAsyncDebug2()
        {
            var text = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        private static Random random = new Random();
        static void Main(string[] args)
        {
            new Program().QBar();
        }
        async void QBar()
        {
            await ZBar();
        }
        async Task<List<int>> ZBar()
        {
            var addedInts = new List<int>();
            foreach (var z in new[] {1, 2, 3})
            {
                var newInt = await GetNextInt(random);
                addedInts.Add(newInt);
            }
            return addedInts;
        }
        private Task<int> GetNextInt(Random random)
        {
            return Task.FromResult(random.Next());
        }
    }
}");
            var compilation = CreateCompilationWithMscorlib45(text, options: TestOptions.DebugDll).VerifyDiagnostics();
            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""ConsoleApplication1.Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""34"" document=""1"" />
        <entry offset=""0xc"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""QBar"">
      <customDebugInfo>
        <forwardIterator name=""&lt;QBar&gt;d__2"" />
      </customDebugInfo>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""ZBar"">
      <customDebugInfo>
        <forwardIterator name=""&lt;ZBar&gt;d__3"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""6"" offset=""61"" />
          <slot kind=""8"" offset=""61"" />
          <slot kind=""0"" offset=""61"" />
          <slot kind=""0"" offset=""132"" />
          <slot kind=""28"" offset=""141"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""GetNextInt"" parameterNames=""random"">
      <customDebugInfo>
        <forward declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""31"" startColumn=""13"" endLine=""31"" endColumn=""51"" document=""1"" />
        <entry offset=""0xf"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""53"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;QBar&gt;d__2"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""15"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0xf"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""26"" document=""1"" />
        <entry offset=""0x20"" hidden=""true"" document=""1"" />
        <entry offset=""0x73"" hidden=""true"" document=""1"" />
        <entry offset=""0x8b"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""1"" />
        <entry offset=""0x93"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x73"" />
        <kickoffMethod declaringType=""ConsoleApplication1.Program"" methodName=""QBar"" />
        <await yield=""0x32"" resume=""0x4d"" declaringType=""ConsoleApplication1.Program+&lt;QBar&gt;d__2"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__3"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x152"" />
          <slot />
          <slot />
          <slot startOffset=""0x41"" endOffset=""0xe3"" />
          <slot startOffset=""0x54"" endOffset=""0xe3"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""33"" offset=""141"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0x12"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""45"" document=""1"" />
        <entry offset=""0x1d"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""20"" document=""1"" />
        <entry offset=""0x1e"" startLine=""22"" startColumn=""31"" endLine=""22"" endColumn=""46"" document=""1"" />
        <entry offset=""0x3c"" hidden=""true"" document=""1"" />
        <entry offset=""0x41"" startLine=""22"" startColumn=""22"" endLine=""22"" endColumn=""27"" document=""1"" />
        <entry offset=""0x54"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""14"" document=""1"" />
        <entry offset=""0x55"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""55"" document=""1"" />
        <entry offset=""0x6b"" hidden=""true"" document=""1"" />
        <entry offset=""0xd0"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""39"" document=""1"" />
        <entry offset=""0xe2"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" document=""1"" />
        <entry offset=""0xe3"" hidden=""true"" document=""1"" />
        <entry offset=""0xf1"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""30"" document=""1"" />
        <entry offset=""0x10b"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""30"" document=""1"" />
        <entry offset=""0x114"" hidden=""true"" document=""1"" />
        <entry offset=""0x135"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
        <entry offset=""0x13d"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" />
        <await yield=""0x7d"" resume=""0x9b"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__3"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
        [WorkItem(690180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690180")]
        public void TestAsyncDebug3()
        {
            var text = WithWindowsLineBreaks(@"
class TestCase
{
    static async void Await(dynamic d)
    {
        int rez = await d;
    }
}");
            var compilation = CreateCompilationWithMscorlib45(
                    text,
                    options: TestOptions.DebugDll,
                    references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef })
                .VerifyDiagnostics();

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""TestCase"" name=""Await"" parameterNames=""d"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Await&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""28"" offset=""21"" />
          <slot kind=""28"" offset=""21"" ordinal=""1"" />
          <slot kind=""28"" offset=""21"" ordinal=""2"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""TestCase+&lt;Await&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x25d"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""21"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x12"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""27"" document=""1"" />
        <entry offset=""0xae"" hidden=""true"" document=""1"" />
        <entry offset=""0x22e"" hidden=""true"" document=""1"" />
        <entry offset=""0x248"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x250"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x22e"" />
        <kickoffMethod declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x148"" resume=""0x18f"" declaringType=""TestCase+&lt;Await&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestAsyncDebug4()
        {
            var text = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<int> F()
    {
        await Task.Delay(1);
        return 1;
    }
}";
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(text, options: TestOptions.DebugDll));

            v.VerifyIL("C.F", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (C.<F>d__0 V_0,
                System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> V_1)
  IL_0000:  newobj     ""C.<F>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0018:  ldloc.0
  IL_0019:  ldfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Start<C.<F>d__0>(ref C.<F>d__0)""
  IL_0028:  ldloc.0
  IL_0029:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_002e:  call       ""System.Threading.Tasks.Task<int> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.Task.get""
  IL_0033:  ret
}",
sequencePoints: "C.F");

            v.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      160 (0xa0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0048
   -IL_000e:  nop
   -IL_000f:  ldc.i4.1
    IL_0010:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0015:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_001a:  stloc.2
   ~IL_001b:  ldloca.s   V_2
    IL_001d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0022:  brtrue.s   IL_0064
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.0
    IL_0028:  stfld      ""int C.<F>d__0.<>1__state""
   <IL_002d:  ldarg.0
    IL_002e:  ldloc.2
    IL_002f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0034:  ldarg.0
    IL_0035:  stloc.3
    IL_0036:  ldarg.0
    IL_0037:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003c:  ldloca.s   V_2
    IL_003e:  ldloca.s   V_3
    IL_0040:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0045:  nop
    IL_0046:  leave.s    IL_009f
   >IL_0048:  ldarg.0
    IL_0049:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_004e:  stloc.2
    IL_004f:  ldarg.0
    IL_0050:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0055:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.0
    IL_005f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0064:  ldloca.s   V_2
    IL_0066:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_006b:  nop
   -IL_006c:  ldc.i4.1
    IL_006d:  stloc.1
    IL_006e:  leave.s    IL_008a
  }
  catch System.Exception
  {
   ~IL_0070:  stloc.s    V_4
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.s   -2
    IL_0075:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007a:  ldarg.0
    IL_007b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0080:  ldloc.s    V_4
    IL_0082:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0087:  nop
    IL_0088:  leave.s    IL_009f
  }
 -IL_008a:  ldarg.0
  IL_008b:  ldc.i4.s   -2
  IL_008d:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_0092:  ldarg.0
  IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_0098:  ldloc.1
  IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_009e:  nop
  IL_009f:  ret
}", sequencePoints: "C+<F>d__0.MoveNext");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [Fact]
        public void DisplayClass_InBetweenSuspensionPoints_Release()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await M(x1 + x2 + x3);
    }
}
";
            // TODO: Currently we don't have means necessary to pass information about the display 
            // class being pushed on evaluation stack, so that EE could find the locals.
            // Thus the locals are not available in EE.
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x17"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x1e"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x25"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x36"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""31"" document=""1"" />
        <entry offset=""0x55"" hidden=""true"" document=""1"" />
        <entry offset=""0xa3"" hidden=""true"" document=""1"" />
        <entry offset=""0xba"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
        <entry offset=""0xc2"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xce"">
        <scope startOffset=""0xa"" endOffset=""0xa3"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xa"" il_end=""0xa3"" attributes=""0"" />
        </scope>
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x67"" resume=""0x7e"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [Fact]
        public void DisplayClass_InBetweenSuspensionPoints_Debug()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await M(x1 + x2 + x3);

        // possible EnC edit:
        // Console.WriteLine(x1);
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "b",
                    "<>8__1",  // display class
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x114"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""129"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1d"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x29"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x35"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x41"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x58"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""31"" document=""1"" />
        <entry offset=""0x86"" hidden=""true"" document=""1"" />
        <entry offset=""0xd9"" hidden=""true"" document=""1"" />
        <entry offset=""0xf8"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0x100"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x98"" resume=""0xb3"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""95"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [Fact]
        public void DisplayClass_AcrossSuspensionPoints_Release()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await Task.Delay(0);

        Console.WriteLine(x1);
    }
}
";
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>8__1",  // display class
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0xf2"" />
        </hoistedLocalScopes>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x21"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x2d"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x39"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x4f"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""29"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0xa7"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""31"" document=""1"" />
        <entry offset=""0xb9"" hidden=""true"" document=""1"" />
        <entry offset=""0xd7"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0xdf"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x6d"" resume=""0x84"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [Fact]
        public void DisplayClass_AcrossSuspensionPoints_Debug()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await Task.Delay(0);

        Console.WriteLine(x1);
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "b",
                    "<>8__1",  // display class
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x103"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""129"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1d"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x29"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x35"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x41"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x58"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""29"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0xb5"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""31"" document=""1"" />
        <entry offset=""0xc8"" hidden=""true"" document=""1"" />
        <entry offset=""0xe7"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0xef"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x76"" resume=""0x91"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""95"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void LocalReuse_Release()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        if (b > 0)
        {
            int x = 42;   
            int y = 42;   
            await Task.Delay(0);

            Console.WriteLine(x);
            Console.WriteLine(x);
        }

        if (b > 0)
        {
            int x = 42;   
            int y1 = 42;   
            int z = 42;   
            await Task.Delay(0);

            Console.WriteLine(x);
            Console.WriteLine(y1);
            Console.WriteLine(z);
        }
    }
}
";
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "b",
                    "<x>5__2",
                    "<>u__1",  // awaiter
                    "<y1>5__3",
                    "<z>5__4",
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <hoistedLocalScopes>
          <slot />
          <slot startOffset=""0x1a"" endOffset=""0x93"" />
          <slot startOffset=""0x9f"" endOffset=""0x130"" />
          <slot startOffset=""0x9f"" endOffset=""0x130"" />
        </hoistedLocalScopes>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
        <entry offset=""0x1a"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""24"" document=""1"" />
        <entry offset=""0x22"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""33"" document=""1"" />
        <entry offset=""0x2e"" hidden=""true"" document=""1"" />
        <entry offset=""0x7d"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""34"" document=""1"" />
        <entry offset=""0x88"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""34"" document=""1"" />
        <entry offset=""0x93"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""19"" document=""1"" />
        <entry offset=""0x9f"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0xa7"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""25"" document=""1"" />
        <entry offset=""0xaf"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""24"" document=""1"" />
        <entry offset=""0xb7"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""33"" document=""1"" />
        <entry offset=""0xc3"" hidden=""true"" document=""1"" />
        <entry offset=""0x10f"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""34"" document=""1"" />
        <entry offset=""0x11a"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0x125"" startLine=""28"" startColumn=""13"" endLine=""28"" endColumn=""34"" document=""1"" />
        <entry offset=""0x130"" hidden=""true"" document=""1"" />
        <entry offset=""0x132"" hidden=""true"" document=""1"" />
        <entry offset=""0x149"" startLine=""30"" startColumn=""5"" endLine=""30"" endColumn=""6"" document=""1"" />
        <entry offset=""0x151"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x15d"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x40"" resume=""0x5a"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
        <await yield=""0xd5"" resume=""0xec"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void LocalReuse_Debug()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int b)
    {
        if (b > 0)
        {
            int x = 42;   
            int y = 42;   
            await Task.Delay(0);

            Console.WriteLine(x);
            Console.WriteLine(x);
        }

        if (b > 0)
        {
            int x = 42;   
            int y1 = 42;   
            int z = 42;   
            await Task.Delay(0);

            Console.WriteLine(x);
            Console.WriteLine(y1);
            Console.WriteLine(z);
        }
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "b",
                    "<x>5__1",
                    "<y>5__2",
                    "<x>5__3",
                    "<y1>5__4",
                    "<z>5__5",
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x2a"" endOffset=""0xb4"" />
          <slot startOffset=""0x2a"" endOffset=""0xb4"" />
          <slot startOffset=""0xc6"" endOffset=""0x164"" />
          <slot startOffset=""0xc6"" endOffset=""0x164"" />
          <slot startOffset=""0xc6"" endOffset=""0x164"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""33"" offset=""102"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""217"" />
          <slot kind=""33"" offset=""337"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x19"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1a"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2b"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""24"" document=""1"" />
        <entry offset=""0x33"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x3b"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""33"" document=""1"" />
        <entry offset=""0x47"" hidden=""true"" document=""1"" />
        <entry offset=""0x9b"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""34"" document=""1"" />
        <entry offset=""0xa7"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""34"" document=""1"" />
        <entry offset=""0xb3"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb4"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""19"" document=""1"" />
        <entry offset=""0xbf"" hidden=""true"" document=""1"" />
        <entry offset=""0xc6"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc7"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0xcf"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""25"" document=""1"" />
        <entry offset=""0xd7"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""24"" document=""1"" />
        <entry offset=""0xdf"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""33"" document=""1"" />
        <entry offset=""0xec"" hidden=""true"" document=""1"" />
        <entry offset=""0x13f"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""34"" document=""1"" />
        <entry offset=""0x14b"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0x157"" startLine=""28"" startColumn=""13"" endLine=""28"" endColumn=""34"" document=""1"" />
        <entry offset=""0x163"" startLine=""29"" startColumn=""9"" endLine=""29"" endColumn=""10"" document=""1"" />
        <entry offset=""0x164"" hidden=""true"" document=""1"" />
        <entry offset=""0x166"" hidden=""true"" document=""1"" />
        <entry offset=""0x180"" startLine=""30"" startColumn=""5"" endLine=""30"" endColumn=""6"" document=""1"" />
        <entry offset=""0x188"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x195"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x59"" resume=""0x77"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
        <await yield=""0xfe"" resume=""0x11a"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [Fact]
        public void DynamicLocal_AcrossSuspensionPoints_Debug()
        {
            string source = WithWindowsLineBreaks(@"
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        dynamic d = 1;
        await Task.Delay(0);
        d.ToString();
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<d>5__1",
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x110"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""35"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1b"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x7b"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0xd5"" hidden=""true"" document=""1"" />
        <entry offset=""0xf4"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0xfc"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x39"" resume=""0x57"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [WorkItem(1070519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070519")]
        [Fact]
        public void DynamicLocal_InBetweenSuspensionPoints_Release()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        dynamic d = 1;
        d.ToString();
        await Task.Delay(0);
    }
}
";
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>u__1", // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x14"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""22"" document=""1"" />
        <entry offset=""0x64"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""1"" />
        <entry offset=""0x70"" hidden=""true"" document=""1"" />
        <entry offset=""0xbe"" hidden=""true"" document=""1"" />
        <entry offset=""0xd5"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0xdd"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe9"">
        <namespace name=""System.Threading.Tasks"" />
        <scope startOffset=""0xd"" endOffset=""0xbe"">
          <local name=""d"" il_index=""1"" il_start=""0xd"" il_end=""0xbe"" attributes=""0"" />
        </scope>
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x82"" resume=""0x99"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(1070519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070519")]
        [Fact]
        public void DynamicLocal_InBetweenSuspensionPoints_Debug()
        {
            string source = WithWindowsLineBreaks(@"
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        dynamic d = 1;
        d.ToString();
        await Task.Delay(0);

        // Possible EnC edit:
        // System.Console.WriteLine(d);
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<d>5__1",
                    "<>u__1", // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x110"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""58"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1e"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""22"" document=""1"" />
        <entry offset=""0x76"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""1"" />
        <entry offset=""0x82"" hidden=""true"" document=""1"" />
        <entry offset=""0xd5"" hidden=""true"" document=""1"" />
        <entry offset=""0xf4"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0xfc"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x94"" resume=""0xaf"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void VariableScopeNotContainingSuspensionPoint()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        {
            int x = 1;
            Console.WriteLine(x);
        }
        {
            await Task.Delay(0);
        }
    }
}
";
            // We need to hoist x even though its scope doesn't contain await.
            // The scopes may be merged by an EnC edit:
            // 
            // {
            //     int x = 1;
            //     Console.WriteLine(x);
            //     await Task.Delay(0);
            //     Console.WriteLine(x);
            // }

            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<x>5__1",
                    "<>u__1", // awaiter
                }, module.GetFieldNames("C.<M>d__0"));
            });
        }

        [Fact]
        public void AwaitInFinally()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<int> G()
    {
        int x = 42;

        try
        {
        }
        finally
        {
            x = await G();
        }

        return x;
    }
}");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<x>5__1",
                    "<>s__2",
                    "<>s__3",
                    "<>s__4",
                    "<>u__1", // awaiter
                }, module.GetFieldNames("C.<G>d__0"));
            });

            v.VerifyPdb("C.G", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""22"" offset=""34"" />
          <slot kind=""23"" offset=""34"" />
          <slot kind=""28"" offset=""105"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            v.VerifyIL("C.<G>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      274 (0x112)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                C.<G>d__0 V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<G>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0070
   -IL_000e:  nop
   -IL_000f:  ldarg.0
    IL_0010:  ldc.i4.s   42
    IL_0012:  stfld      ""int C.<G>d__0.<x>5__1""
   ~IL_0017:  ldarg.0
    IL_0018:  ldnull
    IL_0019:  stfld      ""object C.<G>d__0.<>s__2""
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  stfld      ""int C.<G>d__0.<>s__3""
    .try
    {
     -IL_0025:  nop
     -IL_0026:  nop
     ~IL_0027:  leave.s    IL_0033
    }
    catch object
    {
     ~IL_0029:  stloc.2
      IL_002a:  ldarg.0
      IL_002b:  ldloc.2
      IL_002c:  stfld      ""object C.<G>d__0.<>s__2""
      IL_0031:  leave.s    IL_0033
    }
   -IL_0033:  nop
   -IL_0034:  call       ""System.Threading.Tasks.Task<int> C.G()""
    IL_0039:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003e:  stloc.3
   ~IL_003f:  ldloca.s   V_3
    IL_0041:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0046:  brtrue.s   IL_008c
    IL_0048:  ldarg.0
    IL_0049:  ldc.i4.0
    IL_004a:  dup
    IL_004b:  stloc.0
    IL_004c:  stfld      ""int C.<G>d__0.<>1__state""
   <IL_0051:  ldarg.0
    IL_0052:  ldloc.3
    IL_0053:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_0058:  ldarg.0
    IL_0059:  stloc.s    V_4
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
    IL_0061:  ldloca.s   V_3
    IL_0063:  ldloca.s   V_4
    IL_0065:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__0)""
    IL_006a:  nop
    IL_006b:  leave      IL_0111
   >IL_0070:  ldarg.0
    IL_0071:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_0076:  stloc.3
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      ""int C.<G>d__0.<>1__state""
    IL_008c:  ldarg.0
    IL_008d:  ldloca.s   V_3
    IL_008f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0094:  stfld      ""int C.<G>d__0.<>s__4""
    IL_0099:  ldarg.0
    IL_009a:  ldarg.0
    IL_009b:  ldfld      ""int C.<G>d__0.<>s__4""
    IL_00a0:  stfld      ""int C.<G>d__0.<x>5__1""
   -IL_00a5:  nop
   ~IL_00a6:  ldarg.0
    IL_00a7:  ldfld      ""object C.<G>d__0.<>s__2""
    IL_00ac:  stloc.2
    IL_00ad:  ldloc.2
    IL_00ae:  brfalse.s  IL_00cb
    IL_00b0:  ldloc.2
    IL_00b1:  isinst     ""System.Exception""
    IL_00b6:  stloc.s    V_5
    IL_00b8:  ldloc.s    V_5
    IL_00ba:  brtrue.s   IL_00be
    IL_00bc:  ldloc.2
    IL_00bd:  throw
    IL_00be:  ldloc.s    V_5
    IL_00c0:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00c5:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00ca:  nop
    IL_00cb:  ldarg.0
    IL_00cc:  ldfld      ""int C.<G>d__0.<>s__3""
    IL_00d1:  pop
    IL_00d2:  ldarg.0
    IL_00d3:  ldnull
    IL_00d4:  stfld      ""object C.<G>d__0.<>s__2""
   -IL_00d9:  ldarg.0
    IL_00da:  ldfld      ""int C.<G>d__0.<x>5__1""
    IL_00df:  stloc.1
    IL_00e0:  leave.s    IL_00fc
  }
  catch System.Exception
  {
   ~IL_00e2:  stloc.s    V_5
    IL_00e4:  ldarg.0
    IL_00e5:  ldc.i4.s   -2
    IL_00e7:  stfld      ""int C.<G>d__0.<>1__state""
    IL_00ec:  ldarg.0
    IL_00ed:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
    IL_00f2:  ldloc.s    V_5
    IL_00f4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f9:  nop
    IL_00fa:  leave.s    IL_0111
  }
 -IL_00fc:  ldarg.0
  IL_00fd:  ldc.i4.s   -2
  IL_00ff:  stfld      ""int C.<G>d__0.<>1__state""
 ~IL_0104:  ldarg.0
  IL_0105:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
  IL_010a:  ldloc.1
  IL_010b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0110:  nop
  IL_0111:  ret
}", sequencePoints: "C+<G>d__0.MoveNext");

            v.VerifyPdb("C+<G>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;G&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x112"" />
          <slot startOffset=""0x29"" endOffset=""0x33"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""105"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
        <entry offset=""0x17"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x26"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x34"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""27"" document=""1"" />
        <entry offset=""0x3f"" hidden=""true"" document=""1"" />
        <entry offset=""0xa5"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa6"" hidden=""true"" document=""1"" />
        <entry offset=""0xd9"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""18"" document=""1"" />
        <entry offset=""0xe2"" hidden=""true"" document=""1"" />
        <entry offset=""0xfc"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
        <entry offset=""0x104"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x112"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""G"" />
        <await yield=""0x51"" resume=""0x70"" declaringType=""C+&lt;G&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void HoistedSpilledVariables()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

class C
{
    int[] a = new int[] { 1, 2 }; 
    
    static async Task<int> G()
    {
        int z0 = H(ref new C().a[F(1)], F(2), ref new C().a[F(3)], await G());
        int z1 = H(ref new C().a[F(1)], F(2), ref new C().a[F(3)], await G());

        return z0 + z1;
    }

    static int H(ref int a, int b, ref int c, int d) => 1;
    static int F(int a) => a;
}");
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<z0>5__1",
                    "<z1>5__2",
                    "<>s__3",
                    "<>s__4",
                    "<>s__5",
                    "<>s__6",
                    "<>s__7",
                    "<>s__8",
                    "<>s__9",
                    "<>s__10",
                    "<>u__1",  // awaiter
                    "<>s__11", // ref-spills
                    "<>s__12",
                    "<>s__13",
                    "<>s__14",
                }, module.GetFieldNames("C.<G>d__1"));
            });

            v.VerifyPdb("C.G", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""95"" />
          <slot kind=""28"" offset=""70"" />
          <slot kind=""28"" offset=""70"" ordinal=""1"" />
          <slot kind=""28"" offset=""150"" />
          <slot kind=""28"" offset=""150"" ordinal=""1"" />
          <slot kind=""29"" offset=""70"" />
          <slot kind=""29"" offset=""70"" ordinal=""1"" />
          <slot kind=""29"" offset=""70"" ordinal=""2"" />
          <slot kind=""29"" offset=""70"" ordinal=""3"" />
          <slot kind=""29"" offset=""150"" />
          <slot kind=""29"" offset=""150"" ordinal=""1"" />
          <slot kind=""29"" offset=""150"" ordinal=""2"" />
          <slot kind=""29"" offset=""150"" ordinal=""3"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17934, "https://github.com/dotnet/roslyn/issues/17934")]
        [Fact]
        public void PartialKickoffMethod()
        {
            string src = @"
public partial class C
{
    partial void M();
    async partial void M() {}
}";
            var compilation = CreateCompilationWithMscorlib45(src, options: TestOptions.DebugDll);
            var v = CompileAndVerify(compilation);
            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
            var peStream = new MemoryStream();
            var pdbStream = new MemoryStream();

            var result = compilation.Emit(
               peStream,
               pdbStream,
               options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));


            pdbStream.Position = 0;
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
            {
                var mdReader = provider.GetMetadataReader();
                var writer = new StringWriter();
                var visualizer = new MetadataVisualizer(mdReader, writer);
                visualizer.WriteMethodDebugInformation();

                AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
MethodDebugInformation (index: 0x31, size: 20): 
==================================================
1: nil
2: nil
3: nil
4: #4
{
  Kickoff Method: 0x06000001 (MethodDef)
  Locals: 0x11000002 (StandAloneSig)
  Document: #1
  IL_0000: <hidden>
  IL_0007: (5, 28) - (5, 29)
  IL_000A: <hidden>
  IL_0022: (5, 29) - (5, 30)
  IL_002A: <hidden>
}
5: nil",
                    writer.ToString());
            }
        }

        [Fact]
        public void CatchInAsyncStateMachine()
        {
            string src = WithWindowsLineBreaks(@"
using System;
using System.Threading.Tasks;

#line hidden

class C
{
    static async Task M()
    {
        object o;
        try
        {
            o = null;
        }
        catch (Exception e)
        {
#line 999 ""test""
            o = e;
#line hidden
        }
    }
}");
            var v = CreateEmptyCompilation(src, LatestVbReferences, options: TestOptions.DebugDll);

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name=""test"" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x68"" />
          <slot startOffset=""0x13"" endOffset=""0x2b"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0x10"" hidden=""true"" document=""1"" />
        <entry offset=""0x13"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""999"" startColumn=""13"" endLine=""999"" endColumn=""19"" document=""1"" />
        <entry offset=""0x28"" hidden=""true"" document=""1"" />
        <entry offset=""0x2b"" hidden=""true"" document=""1"" />
        <entry offset=""0x2d"" hidden=""true"" document=""1"" />
        <entry offset=""0x4c"" hidden=""true"" document=""1"" />
        <entry offset=""0x54"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x68"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }
    }
}
