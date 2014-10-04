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
            
            compilation.VerifyPdb(@"
<symbols>
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
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Run&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""TestCase"" name=""&lt;Run&gt;b__0"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""40"" name=""&lt;&lt;Run&gt;b__0&gt;d__0"" />
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
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""28"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""0"" offset=""26"" />
          <slot kind=""0"" offset=""139"" />
          <slot kind=""28"" offset=""146"" />
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
        <entry il_offset=""0x15"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x37"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xae"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xb4"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xb8"" start_row=""18"" start_column=""24"" end_row=""18"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xc8"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0xd4"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0xe1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xfb"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x103"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""dc2"" il_index=""1"" il_start=""0xe"" il_end=""0xe1"" attributes=""0"" />
        <local name=""rez2"" il_index=""2"" il_start=""0xe"" il_end=""0xe1"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <scope startOffset=""0xe"" endOffset=""0xe1"">
          <local name=""dc2"" il_index=""1"" il_start=""0xe"" il_end=""0xe1"" attributes=""0"" />
          <local name=""rez2"" il_index=""2"" il_start=""0xe"" il_end=""0xe1"" attributes=""0"" />
        </scope>
      </scope>
      <async-info catch-IL-offset=""0xe3"">
        <kickoff-method declaringType=""TestCase"" methodName=""Run"" parameterNames="""" />
        <await yield=""0x5b"" resume=""0x7b"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" name=""MoveNext"" parameterNames="""">
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
        <kickoff-method declaringType=""TestCase"" methodName=""&lt;Run&gt;b__0"" parameterNames="""" />
        <await yield=""0x31"" resume=""0x4c"" declaringType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" methodName=""MoveNext"" parameterNames="""" />
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
            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""ConsoleApplication1.Program"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""53"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""QBar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""32"" name=""&lt;QBar&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""ZBar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""32"" name=""&lt;ZBar&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program"" name=""GetNextInt"" parameterNames=""random"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""31"" start_column=""13"" end_row=""31"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;QBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
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
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x11"" endOffset=""0x10d"" />
        </iteratorLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""24"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""0"" offset=""61"" />
          <slot kind=""0"" offset=""132"" />
          <slot kind=""28"" offset=""141"" />
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
        <entry il_offset=""0x4f"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x50"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0xce"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xdb"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xdc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xea"" start_row=""22"" start_column=""28"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x104"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x10d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x128"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x130"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""z"" il_index=""2"" il_start=""0x41"" il_end=""0xdc"" attributes=""0"" />
        <local name=""newInt"" il_index=""3"" il_start=""0x4f"" il_end=""0xdc"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x13e"">
        <scope startOffset=""0x41"" endOffset=""0xdc"">
          <local name=""z"" il_index=""2"" il_start=""0x41"" il_end=""0xdc"" attributes=""0"" />
          <scope startOffset=""0x4f"" endOffset=""0xdc"">
            <local name=""newInt"" il_index=""3"" il_start=""0x4f"" il_end=""0xdc"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" parameterNames="""" />
        <await yield=""0x79"" resume=""0x99"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""28"" offset=""21"" />
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
        <entry il_offset=""0x222"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x23c"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x244"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""rez"" il_index=""1"" il_start=""0x11"" il_end=""0x222"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x251"">
        <scope startOffset=""0x11"" endOffset=""0x222"">
          <local name=""rez"" il_index=""1"" il_start=""0x11"" il_end=""0x222"" attributes=""0"" />
        </scope>
      </scope>
      <async-info catch-IL-offset=""0x224"">
        <kickoff-method declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x148"" resume=""0x193"" declaringType=""TestCase+&lt;Await&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void LambdaDisplayClassLocalHoistedInAsyncMethod()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(byte b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await M(x1);
        await M(x2);
        await M(x3);
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);

            // One iterator local entry for the lambda local.
            comp.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x35"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x42"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x4e"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x5a"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x71"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0xe3"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x155"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1c6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1df"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1e7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x99"" resume=""0xb7"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x10b"" resume=""0x129"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x17d"" resume=""0x198"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var userType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__1");
                var fieldDisplayStrings = stateMachineType.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString());
                AssertEx.SetEqual(fieldDisplayStrings,
                    "System.Int32 C.<M>d__1.<>1__state",
                    "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__1.<>t__builder",
                    "C.<>c__DisplayClass0 C.<M>d__1.CS$<>8__locals1", // Name follows lambda local pattern.
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__1.<>u__$awaiter2");
            });
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void LambdaDisplayClassLocalNotHoistedInAsyncMethod()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(byte b)
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        await Console.Out.WriteLineAsync('a');
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);

            // No iterator local entries.
            comp.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""30"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0xa9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc4"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xcc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xe"" il_end=""0xa9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd9"">
        <scope startOffset=""0xe"" endOffset=""0xa9"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xe"" il_end=""0xa9"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x60"" resume=""0x7b"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var userType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__1");
                var fieldDisplayStrings = stateMachineType.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString());
                AssertEx.SetEqual(fieldDisplayStrings,
                    "System.Int32 C.<M>d__1.<>1__state",
                    "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__1.<>t__builder",
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__1.<>u__$awaiter2");
            });
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocalHoistedInAsyncMethod()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(object o)
    {
        dynamic d = 1;
        await M(d);
        d.ToString();
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            comp.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x11"" endOffset=""0x27b"" />
        </iteratorLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x221"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x27b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x296"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x29e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2ab"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x16e"" resume=""0x1b5"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocalNotHoistedInAsyncMethod()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(object o)
    {
        dynamic d = 1;
        await M(d);
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);

            // One dynamic local entry for "d".
            comp.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""0"" offset=""19"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x21a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x235"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x23d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""1"" il_start=""0x11"" il_end=""0x21a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x24a"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <scope startOffset=""0x11"" endOffset=""0x21a"">
          <local name=""d"" il_index=""1"" il_start=""0x11"" il_end=""0x21a"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x164"" resume=""0x1ac"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
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
}";
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);

            var v = CompileAndVerify(comp);
            
            v.VerifyIL("C.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      267 (0x10b)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2, //x
                object V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
                int V_6,
                C.<G>d__1 V_7,
                System.Exception V_8)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_006d
   -IL_000e:  nop
   -IL_000f:  ldc.i4.s   42
    IL_0011:  stloc.2
   ~IL_0012:  ldarg.0
    IL_0013:  ldnull
    IL_0014:  stfld      ""object C.<G>d__1.<>7__wrap1""
    IL_0019:  ldarg.0
    IL_001a:  ldc.i4.0
    IL_001b:  stfld      ""int C.<G>d__1.<>7__wrap2""
    .try
    {
     -IL_0020:  nop
     -IL_0021:  nop
     ~IL_0022:  leave.s    IL_002e
    }
    catch object
    {
     ~IL_0024:  stloc.3
      IL_0025:  ldarg.0
      IL_0026:  ldloc.3
      IL_0027:  stfld      ""object C.<G>d__1.<>7__wrap1""
      IL_002c:  leave.s    IL_002e
    }
   -IL_002e:  nop
   -IL_002f:  call       ""System.Threading.Tasks.Task<int> C.G()""
    IL_0034:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0039:  stloc.s    V_5
    IL_003b:  ldloca.s   V_5
    IL_003d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0042:  brtrue.s   IL_008a
    IL_0044:  ldarg.0
    IL_0045:  ldc.i4.0
    IL_0046:  dup
    IL_0047:  stloc.0
    IL_0048:  stfld      ""int C.<G>d__1.<>1__state""
    IL_004d:  ldarg.0
    IL_004e:  ldloc.s    V_5
    IL_0050:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_0055:  ldarg.0
    IL_0056:  stloc.s    V_7
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_005e:  ldloca.s   V_5
    IL_0060:  ldloca.s   V_7
    IL_0062:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__1)""
    IL_0067:  nop
    IL_0068:  leave      IL_010a
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_0073:  stloc.s    V_5
    IL_0075:  ldarg.0
    IL_0076:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_007b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0081:  ldarg.0
    IL_0082:  ldc.i4.m1
    IL_0083:  dup
    IL_0084:  stloc.0
    IL_0085:  stfld      ""int C.<G>d__1.<>1__state""
    IL_008a:  ldloca.s   V_5
    IL_008c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0091:  stloc.s    V_6
    IL_0093:  ldloca.s   V_5
    IL_0095:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_009b:  ldloc.s    V_6
    IL_009d:  stloc.s    V_4
    IL_009f:  ldloc.s    V_4
    IL_00a1:  stloc.2
   -IL_00a2:  nop
   ~IL_00a3:  ldarg.0
    IL_00a4:  ldfld      ""object C.<G>d__1.<>7__wrap1""
    IL_00a9:  stloc.3
    IL_00aa:  ldloc.3
    IL_00ab:  brfalse.s  IL_00c8
    IL_00ad:  ldloc.3
    IL_00ae:  isinst     ""System.Exception""
    IL_00b3:  stloc.s    V_8
    IL_00b5:  ldloc.s    V_8
    IL_00b7:  brtrue.s   IL_00bb
    IL_00b9:  ldloc.3
    IL_00ba:  throw
    IL_00bb:  ldloc.s    V_8
    IL_00bd:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00c2:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00c7:  nop
    IL_00c8:  ldarg.0
    IL_00c9:  ldfld      ""int C.<G>d__1.<>7__wrap2""
    IL_00ce:  pop
    IL_00cf:  ldarg.0
    IL_00d0:  ldnull
    IL_00d1:  stfld      ""object C.<G>d__1.<>7__wrap1""
   -IL_00d6:  ldloc.2
    IL_00d7:  stloc.1
    IL_00d8:  leave.s    IL_00f5
  }
  catch System.Exception
  {
   ~IL_00da:  stloc.s    V_8
    IL_00dc:  nop
    IL_00dd:  ldarg.0
    IL_00de:  ldc.i4.s   -2
    IL_00e0:  stfld      ""int C.<G>d__1.<>1__state""
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_00eb:  ldloc.s    V_8
    IL_00ed:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f2:  nop
    IL_00f3:  leave.s    IL_010a
  }
 -IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.s   -2
  IL_00f8:  stfld      ""int C.<G>d__1.<>1__state""
 ~IL_00fd:  ldarg.0
  IL_00fe:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_0103:  ldloc.1
  IL_0104:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0109:  nop
  IL_010a:  ret
}", sequencePoints: "C+<G>d__1.MoveNext");

            comp.VerifyPdb("C+<G>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;G&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""24"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""temp"" />
          <slot kind=""28"" offset=""105"" />
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
        <entry il_offset=""0x12"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x22"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x24"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2f"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0xa2"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xa3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd6"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0xda"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf5"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xfd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""2"" il_start=""0xe"" il_end=""0xda"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x10b"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <scope startOffset=""0xe"" endOffset=""0xda"">
          <local name=""x"" il_index=""2"" il_start=""0xe"" il_end=""0xda"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""G"" parameterNames="""" />
        <await yield=""0x4d"" resume=""0x6d"" declaringType=""C+&lt;G&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>");
        }
    }
}
