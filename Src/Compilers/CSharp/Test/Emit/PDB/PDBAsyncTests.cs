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
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xbc"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xc9"" start_row=""18"" start_column=""24"" end_row=""18"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xd7"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0xe3"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0xee"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xf0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x10a"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x112"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x11f"" attributes=""1"" />
        <local name=""dc2"" il_index=""1"" il_start=""0x16"" il_end=""0xf0"" attributes=""0"" />
        <local name=""rez2"" il_index=""2"" il_start=""0x16"" il_end=""0xf0"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x11f"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x11f"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xf0"">
          <local name=""dc2"" il_index=""1"" il_start=""0x16"" il_end=""0xf0"" attributes=""0"" />
          <local name=""rez2"" il_index=""2"" il_start=""0x16"" il_end=""0xf0"" attributes=""0"" />
        </scope>
      </scope>
      <async-info catch-IL-offset=""0xf2"">
        <kickoff-method declaringType=""TestCase"" methodName=""Run"" parameterNames="""" />
        <await yield=""0x67"" resume=""0x87"" declaringType=""TestCase+&lt;Run&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""16"" start_column=""32"" end_row=""16"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""16"" start_column=""34"" end_row=""16"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""16"" start_column=""59"" end_row=""16"" end_column=""68"" file_ref=""0"" />
        <entry il_offset=""0x89"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa4"" start_row=""16"" start_column=""69"" end_row=""16"" end_column=""70"" file_ref=""0"" />
        <entry il_offset=""0xac"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xba"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0xba"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xba"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xba"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0xba"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""TestCase"" methodName=""&lt;Run&gt;b__0"" parameterNames="""" />
        <await yield=""0x3b"" resume=""0x57"" declaringType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" methodName=""MoveNext"" parameterNames="""" />
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
        <entry il_offset=""0x85"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x87"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa1"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xa9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb6"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb6"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xb6"" attributes=""1"" />
      </scope>
      <async-info catch-IL-offset=""0x89"">
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""QBar"" parameterNames="""" />
        <await yield=""0x3c"" resume=""0x57"" declaringType=""ConsoleApplication1.Program+&lt;QBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x19"" endOffset=""0x121"" />
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
        <entry il_offset=""0xdc"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xe9"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xea"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf8"" start_row=""22"" start_column=""28"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x118"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x121"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x13c"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x144"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x152"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x152"" attributes=""1"" />
        <local name=""z"" il_index=""2"" il_start=""0x49"" il_end=""0xea"" attributes=""0"" />
        <local name=""newInt"" il_index=""3"" il_start=""0x57"" il_end=""0xea"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x152"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x152"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x152"" attributes=""1"" />
        <scope startOffset=""0x49"" endOffset=""0xea"">
          <local name=""z"" il_index=""2"" il_start=""0x49"" il_end=""0xea"" attributes=""0"" />
          <scope startOffset=""0x57"" endOffset=""0xea"">
            <local name=""newInt"" il_index=""3"" il_start=""0x57"" il_end=""0xea"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""ConsoleApplication1.Program"" methodName=""ZBar"" parameterNames="""" />
        <await yield=""0x85"" resume=""0xa5"" declaringType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
        <entry il_offset=""0x235"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x237"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x251"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x259"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x266"" attributes=""1"" />
        <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x237"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x266"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x266"" attributes=""1"" />
        <scope startOffset=""0x19"" endOffset=""0x237"">
          <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x237"" attributes=""0"" />
        </scope>
      </scope>
      <async-info catch-IL-offset=""0x239"">
        <kickoff-method declaringType=""TestCase"" methodName=""Await"" parameterNames=""d"" />
        <await yield=""0x154"" resume=""0x1a6"" declaringType=""TestCase+&lt;Await&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
        <entry il_offset=""0xed"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x163"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1d6"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1d8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1f3"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1fb"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x208"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x208"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x208"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0xa1"" resume=""0xbf"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x117"" resume=""0x135"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
        <await yield=""0x18d"" resume=""0x1a8"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
        <entry il_offset=""0xb4"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xb6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd1"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xd9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xe6"" attributes=""1"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb6"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xe6"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xe6"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xb6"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb6"" attributes=""1"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""b"" />
        <await yield=""0x6a"" resume=""0x86"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
          <bucket startOffset=""0x19"" endOffset=""0x28d"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x233"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x28b"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x28d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2a8"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2b0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x2bd"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2bd"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x2bd"" attributes=""1"" />
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x178"" resume=""0x1c5"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
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
        <entry il_offset=""0x22c"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x249"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x251"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x25e"" attributes=""1"" />
        <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x22e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x25e"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x25e"" attributes=""1"" />
        <scope startOffset=""0x19"" endOffset=""0x22e"">
          <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x22e"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""M"" parameterNames=""o"" />
        <await yield=""0x16e"" resume=""0x1be"" declaringType=""C+&lt;M&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
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
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));

            CompileAndVerify(comp).VerifyIL("C.<G>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      292 (0x124)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
  int V_1, //CS$523$0001
  int V_2, //x
  object V_3,
  int V_4,
  System.Runtime.CompilerServices.TaskAwaiter<int> V_5,
  int V_6,
  bool V_7,
  C.<G>d__1 V_8,
  object V_9,
  System.Exception V_10)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<G>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0016
  IL_0014:  br.s       IL_0074
  IL_0016:  ldc.i4.s   42
  IL_0018:  stloc.2
  IL_0019:  ldarg.0
  IL_001a:  ldnull
  IL_001b:  stfld      ""object C.<G>d__1.<>7__wrap1""
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.0
  IL_0022:  stfld      ""int C.<G>d__1.<>7__wrap2""
  .try
{
  IL_0027:  leave.s    IL_0033
}
  catch object
{
  IL_0029:  stloc.3
  IL_002a:  ldarg.0
  IL_002b:  ldloc.3
  IL_002c:  stfld      ""object C.<G>d__1.<>7__wrap1""
  IL_0031:  leave.s    IL_0033
}
  IL_0033:  call       ""System.Threading.Tasks.Task<int> C.G()""
  IL_0038:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
  IL_003d:  stloc.s    V_5
  IL_003f:  ldloca.s   V_5
  IL_0041:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
  IL_0046:  stloc.s    V_7
  IL_0048:  ldloc.s    V_7
  IL_004a:  brtrue.s   IL_0093
  IL_004c:  ldarg.0
  IL_004d:  ldc.i4.1
  IL_004e:  dup
  IL_004f:  stloc.0
  IL_0050:  stfld      ""int C.<G>d__1.<>1__state""
  IL_0055:  ldarg.0
  IL_0056:  ldloc.s    V_5
  IL_0058:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
  IL_005d:  ldarg.0
  IL_005e:  stloc.s    V_8
  IL_0060:  ldarg.0
  IL_0061:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_0066:  ldloca.s   V_5
  IL_0068:  ldloca.s   V_8
  IL_006a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<G>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<G>d__1)""
  IL_006f:  leave      IL_0123
  IL_0074:  ldarg.0
  IL_0075:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
  IL_007a:  stloc.s    V_5
  IL_007c:  ldarg.0
  IL_007d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<G>d__1.<>u__$awaiter0""
  IL_0082:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_0088:  ldarg.0
  IL_0089:  ldc.i4.m1
  IL_008a:  dup
  IL_008b:  stloc.0
  IL_008c:  stfld      ""int C.<G>d__1.<>1__state""
  IL_0091:  br.s       IL_0093
  IL_0093:  ldloca.s   V_5
  IL_0095:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
  IL_009a:  stloc.s    V_6
  IL_009c:  ldloca.s   V_5
  IL_009e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
  IL_00a4:  ldloc.s    V_6
  IL_00a6:  stloc.s    V_4
  IL_00a8:  ldloc.s    V_4
  IL_00aa:  stloc.2
  IL_00ab:  ldarg.0
  IL_00ac:  ldfld      ""object C.<G>d__1.<>7__wrap1""
  IL_00b1:  stloc.s    V_9
  IL_00b3:  ldloc.s    V_9
  IL_00b5:  ldnull
  IL_00b6:  ceq
  IL_00b8:  stloc.s    V_7
  IL_00ba:  ldloc.s    V_7
  IL_00bc:  brtrue.s   IL_00e3
  IL_00be:  ldloc.s    V_9
  IL_00c0:  isinst     ""System.Exception""
  IL_00c5:  stloc.s    V_10
  IL_00c7:  ldloc.s    V_10
  IL_00c9:  ldnull
  IL_00ca:  cgt.un
  IL_00cc:  stloc.s    V_7
  IL_00ce:  ldloc.s    V_7
  IL_00d0:  brtrue.s   IL_00d5
  IL_00d2:  ldloc.s    V_9
  IL_00d4:  throw
  IL_00d5:  ldloc.s    V_10
  IL_00d7:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
  IL_00dc:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
  IL_00e1:  br.s       IL_00e3
  IL_00e3:  ldarg.0
  IL_00e4:  ldfld      ""int C.<G>d__1.<>7__wrap2""
  IL_00e9:  pop
  IL_00ea:  ldarg.0
  IL_00eb:  ldnull
  IL_00ec:  stfld      ""object C.<G>d__1.<>7__wrap1""
  IL_00f1:  ldloc.2
  IL_00f2:  stloc.1
  IL_00f3:  leave.s    IL_010f
}
  catch System.Exception
{
  IL_00f5:  stloc.s    V_10
  IL_00f7:  nop
  IL_00f8:  ldarg.0
  IL_00f9:  ldc.i4.s   -2
  IL_00fb:  stfld      ""int C.<G>d__1.<>1__state""
  IL_0100:  ldarg.0
  IL_0101:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_0106:  ldloc.s    V_10
  IL_0108:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
  IL_010d:  leave.s    IL_0123
}
  IL_010f:  ldarg.0
  IL_0110:  ldc.i4.s   -2
  IL_0112:  stfld      ""int C.<G>d__1.<>1__state""
  IL_0117:  ldarg.0
  IL_0118:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<G>d__1.<>t__builder""
  IL_011d:  ldloc.1
  IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0123:  ret
}         
");

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
        <entry il_offset=""0xb0"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xb1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf8"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0xfc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x117"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x11f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x12d"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x12d"" attributes=""1"" />
        <local name=""x"" il_index=""2"" il_start=""0x16"" il_end=""0xfc"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x12d"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x12d"" attributes=""1"" />
        <local name=""CS$523$0001"" il_index=""1"" il_start=""0x0"" il_end=""0x12d"" attributes=""1"" />
        <scope startOffset=""0x16"" endOffset=""0xfc"">
          <local name=""x"" il_index=""2"" il_start=""0x16"" il_end=""0xfc"" attributes=""0"" />
        </scope>
      </scope>
      <async-info>
        <kickoff-method declaringType=""C"" methodName=""G"" parameterNames="""" />
        <await yield=""0x59"" resume=""0x79"" declaringType=""C+&lt;G&gt;d__1"" methodName=""MoveNext"" parameterNames="""" />
      </async-info>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }
    }
}
