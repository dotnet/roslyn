// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBAsyncTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(631350, "DevDiv")]
        [WorkItem(643501, "DevDiv")]
        [WorkItem(689616, "DevDiv")]
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

            compilation.VerifyPdb(@"<symbols>
  <methods>
    <method containingType=""DynamicMembers"" name=""get_Prop"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""35"" end_row=""8"" end_column=""39"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""DynamicMembers"" name=""set_Prop"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""40"" end_row=""8"" end_column=""44"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""TestCase"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""33"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
        <namespace name=""System.Threading"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""TestCase"" name=""Run"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Run&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""26"" />
          <slot kind=""0"" offset=""139"" />
          <slot kind=""28"" offset=""146"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Driver"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""27"" start_column=""5"" end_row=""27"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""28"" start_column=""5"" end_row=""28"" end_column=""78"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Driver"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""30"" start_column=""5"" end_row=""30"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""35"" start_column=""9"" end_row=""35"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""36"" start_column=""5"" end_row=""36"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""t"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""t"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""TestCase+&lt;Run&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""28"" count=""2"">
          <slot startOffset=""0xe"" endOffset=""0xff"" />
          <slot startOffset=""0xe"" endOffset=""0xff"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""173"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x45"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xca"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xd5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd9"" start_row=""18"" start_column=""24"" end_row=""18"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xe7"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0xf3"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x100"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11a"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x122"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info catch-IL-offset=""0x102"">
        <kickoff-method declaringType=""TestCase"" methodName=""Run"" parameterNames="""" />
        <await yield=""0x6d"" resume=""0x8b"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""TestCase+&lt;&gt;c__DisplayClass0+&lt;&lt;Run&gt;b__1&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""16"" start_column=""32"" end_row=""16"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""16"" start_column=""34"" end_row=""16"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x78"" start_row=""16"" start_column=""59"" end_row=""16"" end_column=""68"" file_ref=""0"" />
        <entry il_offset=""0x7c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x97"" start_row=""16"" start_column=""69"" end_row=""16"" end_column=""70"" file_ref=""0"" />
        <entry il_offset=""0x9f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""TestCase+&lt;&gt;c__DisplayClass0"" methodName=""&lt;Run&gt;b__1"" parameterNames="""" />
        <await yield=""0x31"" resume=""0x4c"" declaringType=""TestCase+&lt;&gt;c__DisplayClass0+&lt;&lt;Run&gt;b__1&gt;d__0"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(734596, "DevDiv")]
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
            compilation.VerifyPdb(@"<symbols>
  <methods>
    <method containingType=""ConsoleApplication1.Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""QBar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""32"" name=""&lt;QBar&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""ZBar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""32"" name=""&lt;ZBar&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""24"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""6"" offset=""61"" />
          <slot kind=""8"" offset=""61"" />
          <slot kind=""0"" offset=""61"" />
          <slot kind=""0"" offset=""132"" />
          <slot kind=""28"" offset=""141"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""GetNextInt"" parameterNames=""random"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""31"" start_column=""13"" end_row=""31"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""53"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;QBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x7b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x93"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x9b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info catch-IL-offset=""0x7c"">
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""QBar"" parameterNames="""" />
        <await yield=""0x32"" resume=""0x4d"" declaringType=""ConsoleApplication1.Program+&lt;QBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName=""Main"" parameterNames=""args"" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""52"" count=""5"">
          <slot startOffset=""0x11"" endOffset=""0x11e"" />
          <slot startOffset=""0x0"" endOffset=""0x0"" />
          <slot startOffset=""0x0"" endOffset=""0x0"" />
          <slot startOffset=""0x41"" endOffset=""0xed"" />
          <slot startOffset=""0x54"" endOffset=""0xed"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""18"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""22"" start_column=""31"" end_row=""22"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x3c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""22"" start_column=""22"" end_row=""22"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x54"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x55"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0xdb"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xed"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xee"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xfc"" start_row=""22"" start_column=""28"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x116"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x11f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x13a"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x142"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" parameterNames="""" />
        <await yield=""0x7d"" resume=""0x9c"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(690180, "DevDiv")]
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
  <methods>
    <method containingType=""TestCase+&lt;Await&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x11"" endOffset=""0x232"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x233"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x24d"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x255"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info catch-IL-offset=""0x235"">
        <kickoff-method declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x148"" resume=""0x190"" declaringType=""TestCase+&lt;Await&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            // TODO: Currently we don't have means neccessary to pass information about the display 
            // class being pushed on evaluation stack, so that EE could find the locals.
            // Thus the locals are not available in EE.
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>u__1",  // awaiter
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0xab"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc2"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xca"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xa"" il_end=""0xab"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd6"">
        <scope startOffset=""0xa"" endOffset=""0xab"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xa"" il_end=""0xab"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x67"" resume=""0x7e"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x11"" endOffset=""0xe0"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x35"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0xe1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xfa"" start_row=""19"" start_column=""5"" end_row=""19"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x102"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x98"" resume=""0xb3"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DisplayClass_AccrossSuspensionPoints_Release()
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0xa"" endOffset=""0xc0"" />
        </hoistedLocalScopes>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x4f"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xaf"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0xc1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd8"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x6d"" resume=""0x84"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DisplayClass_AccrossSuspensionPoints_Debug()
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x11"" endOffset=""0xcf"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""13"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x35"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xbd"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0xd0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe9"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xf1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x76"" resume=""0x91"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""b"">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocal_AccrossSuspensionPoints_Debug()
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0xe"" endOffset=""0xdc"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x83"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xdd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf6"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xfe"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames="""" />
        <await yield=""0x39"" resume=""0x57"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [WorkItem(1070519, "DevDiv")]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var v = CompileAndVerify(CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All)), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>t__builder",
                    "<>u__1", // awaiter
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x64"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xc6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xdd"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""1"" il_start=""0xd"" il_end=""0xc6"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xf1"">
        <scope startOffset=""0xd"" endOffset=""0xc6"">
          <local name=""d"" il_index=""1"" il_start=""0xd"" il_end=""0xc6"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames="""" />
        <await yield=""0x82"" resume=""0x99"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(1070519, "DevDiv")]
        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x11"" endOffset=""0xdc"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x76"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xdd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf6"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xfe"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames="""" />
        <await yield=""0x94"" resume=""0xaf"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });
        }

        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<G>d__1"));
            });

            v.VerifyPdb("C.G", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""G"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;G&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""22"" offset=""34"" />
          <slot kind=""23"" offset=""34"" />
          <slot kind=""28"" offset=""105"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");

            v.VerifyIL("C.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      287 (0x11f)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                object V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                int V_4,
                C.<G>d__1 V_5,
                System.Exception V_6)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<G>d__1.<>1__state""
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
    IL_0012:  stfld      ""int C.<G>d__1.<x>5__1""
   ~IL_0017:  ldarg.0
    IL_0018:  ldnull
    IL_0019:  stfld      ""object C.<G>d__1.<>s__2""
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  stfld      ""int C.<G>d__1.<>s__3""
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
      IL_002c:  stfld      ""object C.<G>d__1.<>s__2""
      IL_0031:  leave.s    IL_0033
    }
   -IL_0033:  nop
   -IL_0034:  call       ""System.Threading.Tasks.Task<int> C.G()""
    IL_0039:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003e:  stloc.3
    IL_003f:  ldloca.s   V_3
    IL_0041:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0046:  brtrue.s   IL_008c
    IL_0048:  ldarg.0
    IL_0049:  ldc.i4.0
    IL_004a:  dup
    IL_004b:  stloc.0
    IL_004c:  stfld      ""int C.<G>d__1.<>1__state""
    IL_0051:  ldarg.0
    IL_0052:  ldloc.3
    IL_0053:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__1""
    IL_0058:  ldarg.0
    IL_0059:  stloc.s    V_5
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_0061:  ldloca.s   V_3
    IL_0063:  ldloca.s   V_5
    IL_0065:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__1)""
    IL_006a:  nop
    IL_006b:  leave      IL_011e
    IL_0070:  ldarg.0
    IL_0071:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__1""
    IL_0076:  stloc.3
    IL_0077:  ldarg.0
    IL_0078:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__1""
    IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      ""int C.<G>d__1.<>1__state""
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0093:  stloc.s    V_4
    IL_0095:  ldloca.s   V_3
    IL_0097:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009d:  ldarg.0
    IL_009e:  ldloc.s    V_4
    IL_00a0:  stfld      ""int C.<G>d__1.<>s__4""
    IL_00a5:  ldarg.0
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      ""int C.<G>d__1.<>s__4""
    IL_00ac:  stfld      ""int C.<G>d__1.<x>5__1""
   -IL_00b1:  nop
   ~IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""object C.<G>d__1.<>s__2""
    IL_00b8:  stloc.2
    IL_00b9:  ldloc.2
    IL_00ba:  brfalse.s  IL_00d7
    IL_00bc:  ldloc.2
    IL_00bd:  isinst     ""System.Exception""
    IL_00c2:  stloc.s    V_6
    IL_00c4:  ldloc.s    V_6
    IL_00c6:  brtrue.s   IL_00ca
    IL_00c8:  ldloc.2
    IL_00c9:  throw
    IL_00ca:  ldloc.s    V_6
    IL_00cc:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00d1:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00d6:  nop
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""int C.<G>d__1.<>s__3""
    IL_00dd:  pop
    IL_00de:  ldarg.0
    IL_00df:  ldnull
    IL_00e0:  stfld      ""object C.<G>d__1.<>s__2""
   -IL_00e5:  ldarg.0
    IL_00e6:  ldfld      ""int C.<G>d__1.<x>5__1""
    IL_00eb:  stloc.1
    IL_00ec:  leave.s    IL_0109
  }
  catch System.Exception
  {
   ~IL_00ee:  stloc.s    V_6
    IL_00f0:  nop
    IL_00f1:  ldarg.0
    IL_00f2:  ldc.i4.s   -2
    IL_00f4:  stfld      ""int C.<G>d__1.<>1__state""
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_00ff:  ldloc.s    V_6
    IL_0101:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0106:  nop
    IL_0107:  leave.s    IL_011e
  }
 -IL_0109:  ldarg.0
  IL_010a:  ldc.i4.s   -2
  IL_010c:  stfld      ""int C.<G>d__1.<>1__state""
 ~IL_0111:  ldarg.0
  IL_0112:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_0117:  ldloc.1
  IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_011d:  nop
  IL_011e:  ret
}", sequencePoints: "C+<G>d__1.MoveNext");

            v.VerifyPdb("C+<G>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;G&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0xe"" endOffset=""0xed"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""17"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x17"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x27"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x29"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0xb1"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xb2"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe5"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0xee"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x109"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x111"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""G"" parameterNames="""" />
        <await yield=""0x51"" resume=""0x70"" declaringType=""C+&lt;G&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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
  <methods>
    <method containingType=""C"" name=""G"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;G&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""52"">
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
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }
    }
}
