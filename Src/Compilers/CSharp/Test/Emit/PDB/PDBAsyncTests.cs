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
            #region "Source"
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
            #endregion

            #region "Expected Pdb Xml"
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
    <method containingType=""Driver"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""27"" start_column=""5"" end_row=""27"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""28"" start_column=""5"" end_row=""28"" end_column=""78"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x12"">
        <namespace name=""System"" />
        <namespace name=""System.Threading"" />
        <namespace name=""System.Threading.Tasks"" />
      </scope>
    </method>
    <method containingType=""Driver"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Driver"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""35"" start_column=""9"" end_row=""35"" end_column=""30"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""TestCase+&lt;Run&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Driver"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x98"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x9b"" start_row=""18"" start_column=""24"" end_row=""18"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xa7"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0xb3"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0xbe"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xc0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd7"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xdf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""2"" il_start=""0xc0"" il_end=""0xd7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xeb"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <scope startOffset=""0xc0"" endOffset=""0xd7"">
          <local name=""&lt;&gt;t__ex"" il_index=""2"" il_start=""0xc0"" il_end=""0xd7"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663301"" catch-IL-offset=""193"">
        <await yield=""86"" resume=""109"" method=""100663307"" />
      </async-info>
    </method>
    <method containingType=""TestCase+&lt;&lt;Run&gt;b__0&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""16"" start_column=""34"" end_row=""16"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x72"" start_row=""16"" start_column=""59"" end_row=""16"" end_column=""68"" file_ref=""0"" />
        <entry il_offset=""0x76"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x77"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8d"" start_row=""16"" start_column=""69"" end_row=""16"" end_column=""70"" file_ref=""0"" />
        <entry il_offset=""0x95"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xa2"" attributes=""0"" />
        <local name=""&lt;&gt;t__exprRetValue"" il_index=""1"" il_start=""0x0"" il_end=""0xa2"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""3"" il_start=""0x76"" il_end=""0x8d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xa2"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xa2"" attributes=""0"" />
        <local name=""&lt;&gt;t__exprRetValue"" il_index=""1"" il_start=""0x0"" il_end=""0xa2"" attributes=""0"" />
        <scope startOffset=""0x76"" endOffset=""0x8d"">
          <local name=""&lt;&gt;t__ex"" il_index=""3"" il_start=""0x76"" il_end=""0x8d"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663303"">
        <await yield=""48"" resume=""71"" method=""100663309"" />
      </async-info>
    </method>
  </methods>
</symbols>";
            #endregion

            var compilation = CreateCompilationWithMscorlib45(text).VerifyDiagnostics();
            string actual = GetPdbXml(compilation);
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        [WorkItem(734596, "DevDiv")]
        public void TestAsyncDebug2()
        {
            #region "Source"
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
            #endregion

            #region "Expected Pdb Xml"
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
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
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
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""31"" start_column=""13"" end_row=""31"" end_column=""51"" file_ref=""0"" />
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
        <entry il_offset=""0xe"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x74"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x76"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x77"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8d"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x95"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xa1"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""2"" il_start=""0x76"" il_end=""0x8d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xa1"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xa1"" attributes=""0"" />
        <scope startOffset=""0x76"" endOffset=""0x8d"">
          <local name=""&lt;&gt;t__ex"" il_index=""2"" il_start=""0x76"" il_end=""0x8d"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663299"" catch-IL-offset=""119"">
        <await yield=""49"" resume=""72"" method=""100663303"" />
      </async-info>
    </method>
    <method containingType=""ConsoleApplication1.Program+&lt;ZBar&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""ConsoleApplication1.Program"" methodName="".cctor"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0xE"" endOffset=""0xF5"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""22"" start_column=""31"" end_row=""22"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x37"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""22"" start_column=""22"" end_row=""22"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x4a"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0xb8"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xc4"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd2"" start_row=""22"" start_column=""28"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xec"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xf5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x10e"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x116"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x123"" attributes=""0"" />
        <local name=""&lt;&gt;t__exprRetValue"" il_index=""1"" il_start=""0x0"" il_end=""0x123"" attributes=""0"" />
        <local name=""newInt"" il_index=""2"" il_start=""0x4a"" il_end=""0xc4"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""4"" il_start=""0xf5"" il_end=""0x10e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x123"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x123"" attributes=""0"" />
        <local name=""&lt;&gt;t__exprRetValue"" il_index=""1"" il_start=""0x0"" il_end=""0x123"" attributes=""0"" />
        <scope startOffset=""0x4a"" endOffset=""0xc4"">
          <local name=""newInt"" il_index=""2"" il_start=""0x4a"" il_end=""0xc4"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xf5"" endOffset=""0x10e"">
          <local name=""&lt;&gt;t__ex"" il_index=""4"" il_start=""0xf5"" il_end=""0x10e"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663300"">
        <await yield=""114"" resume=""140"" method=""100663305"" />
      </async-info>
    </method>
  </methods>
</symbols>";
            #endregion

            var compilation = CreateCompilationWithMscorlib45(text).VerifyDiagnostics();
            string actual = GetPdbXml(compilation);
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        [WorkItem(690180, "DevDiv")]
        public void TestAsyncDebug3()
        {
            #region "Source"
            var text = @"
class TestCase
{
    static async void Await(dynamic d)
    {
        int rez = await d;
    }
}";
            #endregion

            #region "Expected Pdb Xml"
            string expected = @"
<symbols>
  <methods>
    <method containingType=""TestCase+&lt;Await&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x230"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x232"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x234"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x24c"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x254"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x261"" attributes=""0"" />
        <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x232"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""8"" il_start=""0x232"" il_end=""0x24c"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x261"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x261"" attributes=""0"" />
        <scope startOffset=""0x19"" endOffset=""0x232"">
          <local name=""rez"" il_index=""1"" il_start=""0x19"" il_end=""0x232"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x232"" endOffset=""0x24c"">
          <local name=""&lt;&gt;t__ex"" il_index=""8"" il_start=""0x232"" il_end=""0x24c"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663297"" catch-IL-offset=""564"">
        <await yield=""340"" resume=""417"" method=""100663299"" />
      </async-info>
    </method>
  </methods>
</symbols>";
            #endregion

            var compilation = CreateCompilationWithMscorlib45(
                    text,
                    compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full),
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
            var comp = CreateCompilationWithMscorlib45(source, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // One iterator local entry for the lambda local.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__2"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x30"" endOffset=""0x1CF"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""16"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x30"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x48"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x54"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x60"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x77"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0xea"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x15d"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1cd"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1cf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1d1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1e8"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1f0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x1fd"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""3"" il_start=""0x1cf"" il_end=""0x1e8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1fd"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x1fd"" attributes=""0"" />
        <scope startOffset=""0x1cf"" endOffset=""0x1e8"">
          <local name=""&lt;&gt;t__ex"" il_index=""3"" il_start=""0x1cf"" il_end=""0x1e8"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663297"">
        <await yield=""161"" resume=""188"" method=""100663301"" />
        <await yield=""276"" resume=""303"" method=""100663301"" />
        <await yield=""391"" resume=""415"" method=""100663301"" />
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
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__1.<>u__$awaiter3");
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
            var comp = CreateCompilationWithMscorlib45(source, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp, "C+<M>d__1.MoveNext");

            // No iterator local entries.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x16"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x44"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0xb0"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xb2"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xb5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xcd"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xd5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xe2"" attributes=""0"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb2"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""4"" il_start=""0xb2"" il_end=""0xcd"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xe2"">
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0xe2"" attributes=""0"" />
        <scope startOffset=""0x16"" endOffset=""0xb2"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x16"" il_end=""0xb2"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xb2"" endOffset=""0xcd"">
          <local name=""&lt;&gt;t__ex"" il_index=""4"" il_start=""0xb2"" il_end=""0xcd"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663297"">
        <await yield=""106"" resume=""130"" method=""100663301"" />
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
                    "System.Runtime.CompilerServices.TaskAwaiter C.<M>d__1.<>u__$awaiter3");
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
          <bucket startOffset=""0x19"" endOffset=""0x288"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x22e"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x286"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x288"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x28b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2a3"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2ab"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x2b8"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""5"" il_start=""0x288"" il_end=""0x2a3"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2b8"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x2b8"" attributes=""0"" />
        <scope startOffset=""0x288"" endOffset=""0x2a3"">
          <local name=""&lt;&gt;t__ex"" il_index=""5"" il_start=""0x288"" il_end=""0x2a3"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663297"">
        <await yield=""376"" resume=""448"" method=""100663299"" />
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
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x227"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x229"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x22c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x244"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x24c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x259"" attributes=""0"" />
        <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x229"" attributes=""0"" />
        <local name=""&lt;&gt;t__ex"" il_index=""6"" il_start=""0x229"" il_end=""0x244"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x259"">
        <namespace name=""System"" />
        <namespace name=""System.Threading.Tasks"" />
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x259"" attributes=""0"" />
        <scope startOffset=""0x19"" endOffset=""0x229"">
          <local name=""d"" il_index=""1"" il_start=""0x19"" il_end=""0x229"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x229"" endOffset=""0x244"">
          <local name=""&lt;&gt;t__ex"" il_index=""6"" il_start=""0x229"" il_end=""0x244"" attributes=""0"" />
        </scope>
      </scope>
      <async-info kickoff-method=""100663297"">
        <await yield=""366"" resume=""441"" method=""100663299"" />
      </async-info>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }
    }
}
