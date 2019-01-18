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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
        [WorkItem(631350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631350")]
        [WorkItem(643501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/643501")]
        [WorkItem(689616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689616")]
        public void TestAsyncDebug()
        {
            var text = @"
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
}";
            var compilation = CreateCompilationWithMscorlib45(text, options: TestOptions.DebugDll).VerifyDiagnostics();
            var v = CompileAndVerify(compilation);

            v.VerifyIL("TestCase.<Run>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      287 (0x11f)
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
    IL_0008:  brfalse.s  IL_0089
    IL_000a:  br.s       IL_000c
   -IL_000c:  nop
   -IL_000d:  ldarg.0
    IL_000e:  newobj     ""DynamicMembers..ctor()""
    IL_0013:  stfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
   -IL_0018:  ldarg.0
    IL_0019:  ldfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
    IL_001e:  ldsfld     ""System.Func<System.Threading.Tasks.Task<int>> TestCase.<>c.<>9__1_0""
    IL_0023:  dup
    IL_0024:  brtrue.s   IL_003d
    IL_0026:  pop
    IL_0027:  ldsfld     ""TestCase.<>c TestCase.<>c.<>9""
    IL_002c:  ldftn      ""System.Threading.Tasks.Task<int> TestCase.<>c.<Run>b__1_0()""
    IL_0032:  newobj     ""System.Func<System.Threading.Tasks.Task<int>>..ctor(object, System.IntPtr)""
    IL_0037:  dup
    IL_0038:  stsfld     ""System.Func<System.Threading.Tasks.Task<int>> TestCase.<>c.<>9__1_0""
    IL_003d:  callvirt   ""void DynamicMembers.Prop.set""
    IL_0042:  nop
   -IL_0043:  ldarg.0
    IL_0044:  ldfld      ""DynamicMembers TestCase.<Run>d__1.<dc2>5__1""
    IL_0049:  callvirt   ""System.Func<System.Threading.Tasks.Task<int>> DynamicMembers.Prop.get""
    IL_004e:  callvirt   ""System.Threading.Tasks.Task<int> System.Func<System.Threading.Tasks.Task<int>>.Invoke()""
    IL_0053:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0058:  stloc.1
   ~IL_0059:  ldloca.s   V_1
    IL_005b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0060:  brtrue.s   IL_00a5
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.0
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int TestCase.<Run>d__1.<>1__state""
   <IL_006b:  ldarg.0
    IL_006c:  ldloc.1
    IL_006d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_0072:  ldarg.0
    IL_0073:  stloc.2
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
    IL_007a:  ldloca.s   V_1
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, TestCase.<Run>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref TestCase.<Run>d__1)""
    IL_0083:  nop
    IL_0084:  leave      IL_011e
   >IL_0089:  ldarg.0
    IL_008a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_008f:  stloc.1
    IL_0090:  ldarg.0
    IL_0091:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> TestCase.<Run>d__1.<>u__1""
    IL_0096:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009c:  ldarg.0
    IL_009d:  ldc.i4.m1
    IL_009e:  dup
    IL_009f:  stloc.0
    IL_00a0:  stfld      ""int TestCase.<Run>d__1.<>1__state""
    IL_00a5:  ldarg.0
    IL_00a6:  ldloca.s   V_1
    IL_00a8:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ad:  stfld      ""int TestCase.<Run>d__1.<>s__3""
    IL_00b2:  ldarg.0
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      ""int TestCase.<Run>d__1.<>s__3""
    IL_00b9:  stfld      ""int TestCase.<Run>d__1.<rez2>5__2""
   -IL_00be:  ldarg.0
    IL_00bf:  ldfld      ""int TestCase.<Run>d__1.<rez2>5__2""
    IL_00c4:  ldc.i4.3
    IL_00c5:  ceq
    IL_00c7:  stloc.3
   ~IL_00c8:  ldloc.3
    IL_00c9:  brfalse.s  IL_00d7
   -IL_00cb:  ldsfld     ""int TestCase.Count""
    IL_00d0:  ldc.i4.1
    IL_00d1:  add
    IL_00d2:  stsfld     ""int TestCase.Count""
   -IL_00d7:  ldsfld     ""int TestCase.Count""
    IL_00dc:  ldc.i4.1
    IL_00dd:  sub
    IL_00de:  stsfld     ""int Driver.Result""
   -IL_00e3:  ldsfld     ""System.Threading.AutoResetEvent Driver.CompletedSignal""
    IL_00e8:  callvirt   ""bool System.Threading.EventWaitHandle.Set()""
    IL_00ed:  pop
    IL_00ee:  leave.s    IL_010a
  }
  catch System.Exception
  {
  ~$IL_00f0:  stloc.s    V_4
    IL_00f2:  ldarg.0
    IL_00f3:  ldc.i4.s   -2
    IL_00f5:  stfld      ""int TestCase.<Run>d__1.<>1__state""
    IL_00fa:  ldarg.0
    IL_00fb:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
    IL_0100:  ldloc.s    V_4
    IL_0102:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0107:  nop
    IL_0108:  leave.s    IL_011e
  }
 -IL_010a:  ldarg.0
  IL_010b:  ldc.i4.s   -2
  IL_010d:  stfld      ""int TestCase.<Run>d__1.<>1__state""
 ~IL_0112:  ldarg.0
  IL_0113:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder TestCase.<Run>d__1.<>t__builder""
  IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_011d:  nop
  IL_011e:  ret
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
          <slot startOffset=""0x0"" endOffset=""0x11f"" />
          <slot startOffset=""0x0"" endOffset=""0x11f"" />
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
        <entry offset=""0xc"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""51"" document=""1"" />
        <entry offset=""0x18"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""71"" document=""1"" />
        <entry offset=""0x43"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""37"" document=""1"" />
        <entry offset=""0x59"" hidden=""true"" document=""1"" />
        <entry offset=""0xbe"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""23"" document=""1"" />
        <entry offset=""0xc8"" hidden=""true"" document=""1"" />
        <entry offset=""0xcb"" startLine=""18"" startColumn=""24"" endLine=""18"" endColumn=""32"" document=""1"" />
        <entry offset=""0xd7"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""44"" document=""1"" />
        <entry offset=""0xe3"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""38"" document=""1"" />
        <entry offset=""0xf0"" hidden=""true"" document=""1"" />
        <entry offset=""0x10a"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x112"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0xf0"" />
        <kickoffMethod declaringType=""TestCase"" methodName=""Run"" />
        <await yield=""0x6b"" resume=""0x89"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" />
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
        <entry offset=""0xc"" startLine=""16"" startColumn=""32"" endLine=""16"" endColumn=""33"" document=""1"" />
        <entry offset=""0xd"" startLine=""16"" startColumn=""34"" endLine=""16"" endColumn=""58"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x6e"" startLine=""16"" startColumn=""59"" endLine=""16"" endColumn=""68"" document=""1"" />
        <entry offset=""0x72"" hidden=""true"" document=""1"" />
        <entry offset=""0x8c"" startLine=""16"" startColumn=""69"" endLine=""16"" endColumn=""70"" document=""1"" />
        <entry offset=""0x94"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""TestCase+&lt;&gt;c"" methodName=""&lt;Run&gt;b__1_0"" />
        <await yield=""0x2f"" resume=""0x4a"" declaringType=""TestCase+&lt;&gt;c+&lt;&lt;Run&gt;b__1_0&gt;d"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(734596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734596")]
        public void TestAsyncDebug2()
        {
            var text = @"
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
}";
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
        <entry offset=""0xc"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""26"" document=""1"" />
        <entry offset=""0x1e"" hidden=""true"" document=""1"" />
        <entry offset=""0x71"" hidden=""true"" document=""1"" />
        <entry offset=""0x89"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""1"" />
        <entry offset=""0x91"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x71"" />
        <kickoffMethod declaringType=""ConsoleApplication1.Program"" methodName=""QBar"" />
        <await yield=""0x30"" resume=""0x4b"" declaringType=""ConsoleApplication1.Program+&lt;QBar&gt;d__2"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__3"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x142"" />
          <slot />
          <slot />
          <slot startOffset=""0x3f"" endOffset=""0xe1"" />
          <slot startOffset=""0x52"" endOffset=""0xe1"" />
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
        <entry offset=""0xf"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0x10"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""45"" document=""1"" />
        <entry offset=""0x1b"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""20"" document=""1"" />
        <entry offset=""0x1c"" startLine=""22"" startColumn=""31"" endLine=""22"" endColumn=""46"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x3f"" startLine=""22"" startColumn=""22"" endLine=""22"" endColumn=""27"" document=""1"" />
        <entry offset=""0x52"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""14"" document=""1"" />
        <entry offset=""0x53"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""55"" document=""1"" />
        <entry offset=""0x69"" hidden=""true"" document=""1"" />
        <entry offset=""0xce"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""39"" document=""1"" />
        <entry offset=""0xe0"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" document=""1"" />
        <entry offset=""0xe1"" hidden=""true"" document=""1"" />
        <entry offset=""0xef"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""30"" document=""1"" />
        <entry offset=""0x109"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""30"" document=""1"" />
        <entry offset=""0x112"" hidden=""true"" document=""1"" />
        <entry offset=""0x12c"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
        <entry offset=""0x134"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" />
        <await yield=""0x7b"" resume=""0x99"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__3"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
        [WorkItem(690180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690180")]
        public void TestAsyncDebug3()
        {
            var text = @"
class TestCase
{
    static async void Await(dynamic d)
    {
        int rez = await d;
    }
}";
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
          <slot startOffset=""0x0"" endOffset=""0x25b"" />
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
        <entry offset=""0xf"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x10"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""27"" document=""1"" />
        <entry offset=""0xac"" hidden=""true"" document=""1"" />
        <entry offset=""0x22c"" hidden=""true"" document=""1"" />
        <entry offset=""0x246"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x24e"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x22c"" />
        <kickoffMethod declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x146"" resume=""0x18d"" declaringType=""TestCase+&lt;Await&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
  // Code size      158 (0x9e)
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
    IL_0008:  brfalse.s  IL_0046
    IL_000a:  br.s       IL_000c
   -IL_000c:  nop
   -IL_000d:  ldc.i4.1
    IL_000e:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0013:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0018:  stloc.2
   ~IL_0019:  ldloca.s   V_2
    IL_001b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0020:  brtrue.s   IL_0062
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4.0
    IL_0024:  dup
    IL_0025:  stloc.0
    IL_0026:  stfld      ""int C.<F>d__0.<>1__state""
   <IL_002b:  ldarg.0
    IL_002c:  ldloc.2
    IL_002d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0032:  ldarg.0
    IL_0033:  stloc.3
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003a:  ldloca.s   V_2
    IL_003c:  ldloca.s   V_3
    IL_003e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0043:  nop
    IL_0044:  leave.s    IL_009d
   >IL_0046:  ldarg.0
    IL_0047:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_004c:  stloc.2
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0053:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.m1
    IL_005b:  dup
    IL_005c:  stloc.0
    IL_005d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0069:  nop
   -IL_006a:  ldc.i4.1
    IL_006b:  stloc.1
    IL_006c:  leave.s    IL_0088
  }
  catch System.Exception
  {
   ~IL_006e:  stloc.s    V_4
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.s   -2
    IL_0073:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0078:  ldarg.0
    IL_0079:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_007e:  ldloc.s    V_4
    IL_0080:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0085:  nop
    IL_0086:  leave.s    IL_009d
  }
 -IL_0088:  ldarg.0
  IL_0089:  ldc.i4.s   -2
  IL_008b:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_0090:  ldarg.0
  IL_0091:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_0096:  ldloc.1
  IL_0097:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_009c:  nop
  IL_009d:  ret
}", sequencePoints: "C+<F>d__0.MoveNext");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DisplayClass_InBetweenSuspensionPoints_Debug()
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

        // possible EnC edit:
        // Console.WriteLine(x1);
    }
}
";
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
          <slot startOffset=""0x0"" endOffset=""0x104"" />
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
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1b"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x27"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x33"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3f"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x56"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""31"" document=""1"" />
        <entry offset=""0x84"" hidden=""true"" document=""1"" />
        <entry offset=""0xd7"" hidden=""true"" document=""1"" />
        <entry offset=""0xef"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf7"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x96"" resume=""0xb1"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
          <slot startOffset=""0x0"" endOffset=""0xe4"" />
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
        <entry offset=""0xd0"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd8"" hidden=""true"" document=""1"" />
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DisplayClass_AcrossSuspensionPoints_Debug()
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
          <slot startOffset=""0x0"" endOffset=""0xf3"" />
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
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1b"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x27"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x33"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3f"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x56"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""29"" document=""1"" />
        <entry offset=""0x62"" hidden=""true"" document=""1"" />
        <entry offset=""0xb3"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""31"" document=""1"" />
        <entry offset=""0xc6"" hidden=""true"" document=""1"" />
        <entry offset=""0xde"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe6"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x74"" resume=""0x8f"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void LocalReuse_Debug()
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
          <slot startOffset=""0x26"" endOffset=""0xb0"" />
          <slot startOffset=""0x26"" endOffset=""0xb0"" />
          <slot startOffset=""0xc2"" endOffset=""0x160"" />
          <slot startOffset=""0xc2"" endOffset=""0x160"" />
          <slot startOffset=""0xc2"" endOffset=""0x160"" />
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
        <entry offset=""0x15"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x16"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
        <entry offset=""0x20"" hidden=""true"" document=""1"" />
        <entry offset=""0x26"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x27"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2f"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x37"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""33"" document=""1"" />
        <entry offset=""0x43"" hidden=""true"" document=""1"" />
        <entry offset=""0x97"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""34"" document=""1"" />
        <entry offset=""0xa3"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""34"" document=""1"" />
        <entry offset=""0xaf"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb0"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""19"" document=""1"" />
        <entry offset=""0xbb"" hidden=""true"" document=""1"" />
        <entry offset=""0xc2"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc3"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0xcb"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""25"" document=""1"" />
        <entry offset=""0xd3"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""24"" document=""1"" />
        <entry offset=""0xdb"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""33"" document=""1"" />
        <entry offset=""0xe8"" hidden=""true"" document=""1"" />
        <entry offset=""0x13b"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""34"" document=""1"" />
        <entry offset=""0x147"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0x153"" startLine=""28"" startColumn=""13"" endLine=""28"" endColumn=""34"" document=""1"" />
        <entry offset=""0x15f"" startLine=""29"" startColumn=""9"" endLine=""29"" endColumn=""10"" document=""1"" />
        <entry offset=""0x160"" hidden=""true"" document=""1"" />
        <entry offset=""0x162"" hidden=""true"" document=""1"" />
        <entry offset=""0x17c"" startLine=""30"" startColumn=""5"" endLine=""30"" endColumn=""6"" document=""1"" />
        <entry offset=""0x184"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x191"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x55"" resume=""0x73"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
        <await yield=""0xfa"" resume=""0x116"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DynamicLocal_AcrossSuspensionPoints_Debug()
        {
            string source = @"
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
";
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
          <slot startOffset=""0x0"" endOffset=""0x100"" />
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
        <entry offset=""0xc"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x19"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x79"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0xd3"" hidden=""true"" document=""1"" />
        <entry offset=""0xeb"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf3"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x100"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x37"" resume=""0x55"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DynamicLocal_InBetweenSuspensionPoints_Debug()
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

        // Possible EnC edit:
        // System.Console.WriteLine(d);
    }
}
";
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
          <slot startOffset=""0x0"" endOffset=""0x100"" />
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
        <entry offset=""0xf"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x10"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""22"" document=""1"" />
        <entry offset=""0x74"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""1"" />
        <entry offset=""0x80"" hidden=""true"" document=""1"" />
        <entry offset=""0xd3"" hidden=""true"" document=""1"" />
        <entry offset=""0xeb"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf3"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x100"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x92"" resume=""0xad"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void AwaitInFinally()
        {
            string source = @"
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
}";
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
  // Code size      272 (0x110)
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
    IL_0008:  brfalse.s  IL_006e
    IL_000a:  br.s       IL_000c
   -IL_000c:  nop
   -IL_000d:  ldarg.0
    IL_000e:  ldc.i4.s   42
    IL_0010:  stfld      ""int C.<G>d__0.<x>5__1""
   ~IL_0015:  ldarg.0
    IL_0016:  ldnull
    IL_0017:  stfld      ""object C.<G>d__0.<>s__2""
    IL_001c:  ldarg.0
    IL_001d:  ldc.i4.0
    IL_001e:  stfld      ""int C.<G>d__0.<>s__3""
    .try
    {
     -IL_0023:  nop
     -IL_0024:  nop
     ~IL_0025:  leave.s    IL_0031
    }
    catch object
    {
     ~IL_0027:  stloc.2
      IL_0028:  ldarg.0
      IL_0029:  ldloc.2
      IL_002a:  stfld      ""object C.<G>d__0.<>s__2""
      IL_002f:  leave.s    IL_0031
    }
   -IL_0031:  nop
   -IL_0032:  call       ""System.Threading.Tasks.Task<int> C.G()""
    IL_0037:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003c:  stloc.3
   ~IL_003d:  ldloca.s   V_3
    IL_003f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0044:  brtrue.s   IL_008a
    IL_0046:  ldarg.0
    IL_0047:  ldc.i4.0
    IL_0048:  dup
    IL_0049:  stloc.0
    IL_004a:  stfld      ""int C.<G>d__0.<>1__state""
   <IL_004f:  ldarg.0
    IL_0050:  ldloc.3
    IL_0051:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_0056:  ldarg.0
    IL_0057:  stloc.s    V_4
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
    IL_005f:  ldloca.s   V_3
    IL_0061:  ldloca.s   V_4
    IL_0063:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__0)""
    IL_0068:  nop
    IL_0069:  leave      IL_010f
   >IL_006e:  ldarg.0
    IL_006f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_0074:  stloc.3
    IL_0075:  ldarg.0
    IL_0076:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__0.<>u__1""
    IL_007b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0081:  ldarg.0
    IL_0082:  ldc.i4.m1
    IL_0083:  dup
    IL_0084:  stloc.0
    IL_0085:  stfld      ""int C.<G>d__0.<>1__state""
    IL_008a:  ldarg.0
    IL_008b:  ldloca.s   V_3
    IL_008d:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0092:  stfld      ""int C.<G>d__0.<>s__4""
    IL_0097:  ldarg.0
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""int C.<G>d__0.<>s__4""
    IL_009e:  stfld      ""int C.<G>d__0.<x>5__1""
   -IL_00a3:  nop
   ~IL_00a4:  ldarg.0
    IL_00a5:  ldfld      ""object C.<G>d__0.<>s__2""
    IL_00aa:  stloc.2
    IL_00ab:  ldloc.2
    IL_00ac:  brfalse.s  IL_00c9
    IL_00ae:  ldloc.2
    IL_00af:  isinst     ""System.Exception""
    IL_00b4:  stloc.s    V_5
    IL_00b6:  ldloc.s    V_5
    IL_00b8:  brtrue.s   IL_00bc
    IL_00ba:  ldloc.2
    IL_00bb:  throw
    IL_00bc:  ldloc.s    V_5
    IL_00be:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00c3:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00c8:  nop
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      ""int C.<G>d__0.<>s__3""
    IL_00cf:  pop
    IL_00d0:  ldarg.0
    IL_00d1:  ldnull
    IL_00d2:  stfld      ""object C.<G>d__0.<>s__2""
   -IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""int C.<G>d__0.<x>5__1""
    IL_00dd:  stloc.1
    IL_00de:  leave.s    IL_00fa
  }
  catch System.Exception
  {
   ~IL_00e0:  stloc.s    V_5
    IL_00e2:  ldarg.0
    IL_00e3:  ldc.i4.s   -2
    IL_00e5:  stfld      ""int C.<G>d__0.<>1__state""
    IL_00ea:  ldarg.0
    IL_00eb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
    IL_00f0:  ldloc.s    V_5
    IL_00f2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f7:  nop
    IL_00f8:  leave.s    IL_010f
  }
 -IL_00fa:  ldarg.0
  IL_00fb:  ldc.i4.s   -2
  IL_00fd:  stfld      ""int C.<G>d__0.<>1__state""
 ~IL_0102:  ldarg.0
  IL_0103:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__0.<>t__builder""
  IL_0108:  ldloc.1
  IL_0109:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_010e:  nop
  IL_010f:  ret
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
          <slot startOffset=""0x0"" endOffset=""0x110"" />
          <slot startOffset=""0x27"" endOffset=""0x31"" />
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
        <entry offset=""0xc"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x23"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x24"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x31"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x32"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""27"" document=""1"" />
        <entry offset=""0x3d"" hidden=""true"" document=""1"" />
        <entry offset=""0xa3"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa4"" hidden=""true"" document=""1"" />
        <entry offset=""0xd7"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""18"" document=""1"" />
        <entry offset=""0xe0"" hidden=""true"" document=""1"" />
        <entry offset=""0xfa"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
        <entry offset=""0x102"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""G"" />
        <await yield=""0x4f"" resume=""0x6e"" declaringType=""C+&lt;G&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void HoistedSpilledVariables()
        {
            string source = @"
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
}";
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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void CatchInAsyncStateMachine()
        {
            string src = @"
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
}";
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
          <slot startOffset=""0x0"" endOffset=""0x5a"" />
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
        <entry offset=""0x45"" hidden=""true"" document=""1"" />
        <entry offset=""0x4d"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x5a"">
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
