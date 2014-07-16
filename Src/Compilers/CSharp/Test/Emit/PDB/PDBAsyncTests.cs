// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            string expected = @"
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""TestCase"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xb6"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xbc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc0"" start_row=""18"" start_column=""24"" end_row=""18"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xce"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0xda"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0xe5"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x101"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x109"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x116"" attributes=""1"" />
        <local name=""dc2"" il_index=""1"" il_start=""0x16"" il_end=""0xe7"" attributes=""0"" />
        <local name=""rez2"" il_index=""2"" il_start=""0x16"" il_end=""0xe7"" attributes=""0"" />
        <local name=""CS$4$0001"" il_index=""7"" il_start=""0xb6"" il_end=""0xc0"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x116"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x116"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xe7"">
          <local name=""dc2"" il_index=""1"" il_start=""0x16"" il_end=""0xe7"" attributes=""0"" />
          <local name=""rez2"" il_index=""2"" il_start=""0x16"" il_end=""0xe7"" attributes=""0"" />
          <scope startOffset=""0xb6"" endOffset=""0xc0"">
            <local name=""CS$4$0001"" il_index=""7"" il_start=""0xb6"" il_end=""0xc0"" attributes=""1"" />
          </scope>
        </scope>
      </scope>
      <async-info catch-IL-offset=""0xe9"">
        <kickoff-method declaringType=""TestCase"" methodName=""Run"" parameterNames="""" />
        <await yield=""0x63"" resume=""0x83"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""16"" start_column=""32"" end_row=""16"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""16"" start_column=""34"" end_row=""16"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x80"" start_row=""16"" start_column=""59"" end_row=""16"" end_column=""68"" file_ref=""0"" />
        <entry il_offset=""0x84"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x9f"" start_row=""16"" start_column=""69"" end_row=""16"" end_column=""70"" file_ref=""0"" />
        <entry il_offset=""0xa7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb5"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0xb5"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb5"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb5"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0xb5"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""TestCase"" methodName=""&lt;Run&gt;b__0"" parameterNames="""" />
        <await yield=""0x39"" resume=""0x54"" declaringType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";

            var compilation = CreateCompilationWithMscorlib45(text, compOptions: TestOptions.DebugDll).VerifyDiagnostics();
            string actual = GetPdbXml(compilation);
            AssertXmlEqual(expected, actual);
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

            string expected = @"
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x83"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x9b"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xa3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb0"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb0"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb0"" attributes=""1"" />
      </scope>
      <async-info catch-IL-offset=""0x84"">
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""QBar"" parameterNames="""" />
        <await yield=""0x3a"" resume=""0x55"" declaringType=""ConsoleApplication1.Program+&lt;QBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x19"" endOffset=""0x115"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""18"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""22"" start_column=""31"" end_row=""22"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x44"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x49"" start_row=""22"" start_column=""22"" end_row=""22"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x57"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0xd6"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xe3"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xe4"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf2"" start_row=""22"" start_column=""28"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x10c"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x115"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x130"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x138"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x146"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x146"" attributes=""1"" />
        <local name=""z"" il_index=""2"" il_start=""0x49"" il_end=""0xe4"" attributes=""0"" />
        <local name=""newInt"" il_index=""3"" il_start=""0x57"" il_end=""0xe4"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x146"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x146"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x146"" attributes=""1"" />
        <scope startOffset=""0x49"" endOffset=""0xe4"">
          <local name=""z"" il_index=""2"" il_start=""0x49"" il_end=""0xe4"" attributes=""0"" />
          <scope startOffset=""0x57"" endOffset=""0xe4"">
            <local name=""newInt"" il_index=""3"" il_start=""0x57"" il_end=""0xe4"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" parameterNames="""" />
        <await yield=""0x81"" resume=""0xa1"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";

            var compilation = CreateCompilationWithMscorlib45(text, compOptions: TestOptions.DebugDll).VerifyDiagnostics();
            string actual = GetPdbXml(compilation);
            AssertXmlEqual(expected, actual);
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

            string expected = @"
<symbols>
  <methods>
    <method containingType=""TestCase+&lt;Await&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x228"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x244"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x24c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x259"" attributes=""1"" />
        <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x22a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x259"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x259"" attributes=""1"" />
        <scope startOffset=""0x19"" endOffset=""0x22a"">
          <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x22a"" attributes=""0"" />
        </scope>
      </scope>
      <async-info catch-IL-offset=""0x22c"">
        <kickoff-method declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x150"" resume=""0x19b"" declaringType=""TestCase+&lt;Await&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";

            var compilation = CreateCompilationWithMscorlib45(
                    text,
                    compOptions: TestOptions.DebugDll,
                    references: new[] { SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929, CSharpRef })
                .VerifyDiagnostics();

            string actual = GetPdbXml(compilation);
            AssertXmlEqual(expected, actual);
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
            var comp = CreateCompilationWithMscorlib45(source, compOptions: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // One iterator local entry for the lambda local.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x30"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x48"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x54"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x60"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x77"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0xe9"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x15b"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1ca"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1cc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1e5"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1ed"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1fa"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1fa"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1fa"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x9f"" resume=""0xbd"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x111"" resume=""0x12f"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x183"" resume=""0x19e"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);

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
            var comp = CreateCompilationWithMscorlib45(source, compOptions: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // No iterator local entries.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""13"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x44"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0xaf"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xb1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xcc"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xd4"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xe1"" attributes=""1"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb1"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xe1"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xe1"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xb1"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb1"" attributes=""1"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x68"" resume=""0x83"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);

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
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x19"" endOffset=""0x283"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x229"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x281"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x283"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x29e"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2a6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x2b3"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2b3"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x2b3"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x176"" resume=""0x1bd"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
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
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // One dynamic local entry for "d".
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x220"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x222"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x23d"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x245"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x252"" attributes=""1"" />
        <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x222"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x252"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x252"" attributes=""1"" />
        <scope startOffset=""0x19"" endOffset=""0x222"">
          <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x222"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x16c"" resume=""0x1b4"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
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
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.DebugDll);

            var v = CompileAndVerify(comp, emitPdb: true);
            
            v.VerifyIL("C.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      279 (0x117)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
           int V_1, //CS$523$0001
           int V_2, //x
           object V_3,
           int V_4,
           System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
           int V_6,
           C.<G>d__1 V_7,
           object V_8,
           System.Exception V_9)
 ~IL_0000:  ldarg.0   
  IL_0001:  ldfld      ""int C.<G>d__1.<>1__state""
  IL_0006:  stloc.0   
  .try
  {
   ~IL_0007:  ldloc.0   
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0   
    IL_000d:  ldc.i4.1  
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0016
    IL_0012:  br.s       IL_0016
    IL_0014:  br.s       IL_0075
   -IL_0016:  nop       
   -IL_0017:  ldc.i4.s   42
    IL_0019:  stloc.2   
   ~IL_001a:  ldarg.0   
    IL_001b:  ldnull    
    IL_001c:  stfld      ""object C.<G>d__1.<>7__wrap1""
    IL_0021:  ldarg.0   
    IL_0022:  ldc.i4.0  
    IL_0023:  stfld      ""int C.<G>d__1.<>7__wrap2""
    .try
    {
     -IL_0028:  nop       
     -IL_0029:  nop       
     ~IL_002a:  leave.s    IL_0036
    }
    catch object
    {
     ~IL_002c:  stloc.3   
      IL_002d:  ldarg.0   
      IL_002e:  ldloc.3   
      IL_002f:  stfld      ""object C.<G>d__1.<>7__wrap1""
      IL_0034:  leave.s    IL_0036
    }
   -IL_0036:  nop       
   -IL_0037:  call       ""System.Threading.Tasks.Task<int> C.G()""
    IL_003c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0041:  stloc.s    V_5
    IL_0043:  ldloca.s   V_5
    IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_004a:  brtrue.s   IL_0092
    IL_004c:  ldarg.0   
    IL_004d:  ldc.i4.1  
    IL_004e:  dup       
    IL_004f:  stloc.0   
    IL_0050:  stfld      ""int C.<G>d__1.<>1__state""
    IL_0055:  ldarg.0   
    IL_0056:  ldloc.s    V_5
    IL_0058:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_005d:  ldarg.0   
    IL_005e:  stloc.s    V_7
    IL_0060:  ldarg.0   
    IL_0061:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_0066:  ldloca.s   V_5
    IL_0068:  ldloca.s   V_7
    IL_006a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__1)""
    IL_006f:  nop       
    IL_0070:  leave      IL_0116
    IL_0075:  ldarg.0   
    IL_0076:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_007b:  stloc.s    V_5
    IL_007d:  ldarg.0   
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0089:  ldarg.0   
    IL_008a:  ldc.i4.m1 
    IL_008b:  dup       
    IL_008c:  stloc.0   
    IL_008d:  stfld      ""int C.<G>d__1.<>1__state""
    IL_0092:  ldloca.s   V_5
    IL_0094:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0099:  stloc.s    V_6
    IL_009b:  ldloca.s   V_5
    IL_009d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00a3:  ldloc.s    V_6
    IL_00a5:  stloc.s    V_4
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  stloc.2   
   -IL_00aa:  nop       
   ~IL_00ab:  ldarg.0   
    IL_00ac:  ldfld      ""object C.<G>d__1.<>7__wrap1""
    IL_00b1:  stloc.s    V_8
    IL_00b3:  ldloc.s    V_8
    IL_00b5:  brfalse.s  IL_00d4
    IL_00b7:  ldloc.s    V_8
    IL_00b9:  isinst     ""System.Exception""
    IL_00be:  stloc.s    V_9
    IL_00c0:  ldloc.s    V_9
    IL_00c2:  brtrue.s   IL_00c7
    IL_00c4:  ldloc.s    V_8
    IL_00c6:  throw     
    IL_00c7:  ldloc.s    V_9
    IL_00c9:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_00ce:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_00d3:  nop       
    IL_00d4:  ldarg.0   
    IL_00d5:  ldfld      ""int C.<G>d__1.<>7__wrap2""
    IL_00da:  pop       
    IL_00db:  ldarg.0   
    IL_00dc:  ldnull    
    IL_00dd:  stfld      ""object C.<G>d__1.<>7__wrap1""
   -IL_00e2:  ldloc.2   
    IL_00e3:  stloc.1   
    IL_00e4:  leave.s    IL_0101
  }
  catch System.Exception
  {
   ~IL_00e6:  stloc.s    V_9
    IL_00e8:  nop       
    IL_00e9:  ldarg.0   
    IL_00ea:  ldc.i4.s   -2
    IL_00ec:  stfld      ""int C.<G>d__1.<>1__state""
    IL_00f1:  ldarg.0   
    IL_00f2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
    IL_00f7:  ldloc.s    V_9
    IL_00f9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00fe:  nop       
    IL_00ff:  leave.s    IL_0116
  }
 -IL_0101:  ldarg.0   
  IL_0102:  ldc.i4.s   -2
  IL_0104:  stfld      ""int C.<G>d__1.<>1__state""
 ~IL_0109:  ldarg.0   
  IL_010a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_010f:  ldloc.1   
  IL_0110:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0115:  nop       
  IL_0116:  ret       
}", sequencePoints: "C+<G>d__1.MoveNext");

            string actual = GetPdbXml(comp, "C+<G>d__1.MoveNext");
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;G&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""17"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x37"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0xaa"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xab"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe2"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0xe6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x101"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x109"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x117"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x117"" attributes=""1"" />
        <local name=""x"" il_index=""2"" il_start=""0x16"" il_end=""0xe6"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x117"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x117"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x117"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xe6"">
          <local name=""x"" il_index=""2"" il_start=""0x16"" il_end=""0xe6"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""G"" parameterNames="""" />
        <await yield=""0x55"" resume=""0x75"" declaringType=""C+&lt;G&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }
    }
}
