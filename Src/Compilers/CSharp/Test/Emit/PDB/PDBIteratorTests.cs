// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBIteratorTests : CSharpPDBTestBase
    {
        [WorkItem(543376, "DevDiv")]
        [Fact]
        public void SimpleIterator1()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Foo()
    {
        yield break;
    }
}
";
            // NOTE: as in dev10, the custom debug info for Foo is lost.
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program+&lt;Foo&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [WorkItem(543376, "DevDiv")]
        [Fact]
        public void SimpleIterator2()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Foo()
    {
        yield break;
    }

    void Bar() { }
}
";

            // NOTE: as in dev10, the presence of Bar has prevented Foo's debug info from being dropped.
            // NOTE: as in dev10, Foo has no using info (and is, thus, never forwarded to).
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Foo&gt;d__0"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""Bar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""16"" end_row=""9"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""18"" end_row=""9"" end_column=""19"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;Foo&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Bar"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [WorkItem(543490, "DevDiv")]
        [Fact]
        public void SimpleIterator3()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Foo()
    {
        yield return 1; //hidden sequence point after this.
    }

    void Bar() { }
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Foo&gt;d__0"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""Bar"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""16"" end_row=""9"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""18"" end_row=""9"" end_column=""19"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;Foo&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Bar"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x3f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x3f"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x3f"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void IteratorWithLocals_ReleasePdb()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> IEI<T>(int i0, int i1)
    {
        int x = i0;
        yield return x;
        yield return x;
        {
            int y = i1;
            yield return y;
            yield return y;
        }
        yield break;
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program+&lt;IEI&gt;d__0`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""28"" bucketCount=""2"">
          <bucket startOffset=""0x3b"" endOffset=""0xd8"" />
          <bucket startOffset=""0x84"" endOffset=""0xd1"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x48"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x5f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x66"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x84"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x91"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xa8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xaf"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xc9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd0"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xd1"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xd8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd8"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xd8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void IteratorWithLocals_DebugPdb()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> IEI<T>(int i0, int i1)
    {
        int x = i0;
        yield return x;
        yield return x;
        {
            int y = i1;
            yield return y;
            yield return y;
        }
        yield break;
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program+&lt;IEI&gt;d__0`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""28"" bucketCount=""2"">
          <bucket startOffset=""0x3b"" endOffset=""0xd8"" />
          <bucket startOffset=""0x84"" endOffset=""0xd1"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x48"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x5f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x66"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x84"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x91"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xa8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xaf"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xc9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd0"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xd1"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xd8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd8"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xd8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void IteratorWithCapturedSyntheticVariables()
        {
            // this iterator captures the synthetic variable generated from the expansion of the foreach loop
            var text = @"// Based on LegacyTest csharp\Source\Conformance\iterators\blocks\using001.cs
using System;
using System.Collections.Generic;

class Test<T>
{
    public static IEnumerator<T> M(IEnumerable<T> items)
    {
        T val = default(T);

        foreach (T item in items)
        {
            val = item;
            yield return val;
        }
        yield return val;
    }
}";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Test`1+&lt;M&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x32"" endOffset=""0xcc"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""17"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""11"" start_column=""28"" end_row=""11"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x59"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5b"" start_row=""11"" start_column=""18"" end_row=""11"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x68"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x6f"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x86"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8e"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8f"" start_row=""11"" start_column=""25"" end_row=""11"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0xaa"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xc1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc8"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xcc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""1"" il_start=""0x0"" il_end=""0xcc"" attributes=""0"" />
        <local name=""item"" il_index=""2"" il_start=""0x5b"" il_end=""0x8f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd8"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x0"" endOffset=""0xcc"">
          <local name=""CS$524$0000"" il_index=""1"" il_start=""0x0"" il_end=""0xcc"" attributes=""0"" />
          <scope startOffset=""0x5b"" endOffset=""0x8f"">
            <local name=""item"" il_index=""2"" il_start=""0x5b"" il_end=""0x8f"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [WorkItem(542705, "DevDiv"), WorkItem(528790, "DevDiv"), WorkItem(543490, "DevDiv")]
        [Fact()]
        public void IteratorBackToNextStatementAfterYieldReturn()
        {
            var text = @"
using System.Collections.Generic;
class C
{
    IEnumerable<decimal> M()
    {
        const decimal d1 = 0.1M;
        yield return d1;

        const decimal dx = 1.23m;
        yield return dx;
        {
            const decimal d2 = 0.2M;
            yield return d2;
        }
        yield break;
    }

    static void Main()
    {
        foreach (var i in new C().M())
        {
            System.Console.WriteLine(i);
        }
    }
}
";

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""21"" start_column=""27"" end_row=""21"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x10"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""21"" start_column=""18"" end_row=""21"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""21"" start_column=""24"" end_row=""21"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x27"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x32"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x3f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x60"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x80"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x87"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <constant name=""d1"" value=""0.1"" type=""Decimal"" />
        <constant name=""dx"" value=""1.23"" type=""Decimal"" />
        <constant name=""d2"" value=""0.2"" type=""Decimal"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x89"">
        <scope startOffset=""0x26"" endOffset=""0x89"">
          <constant name=""d1"" value=""0.1"" type=""Decimal"" />
          <constant name=""dx"" value=""1.23"" type=""Decimal"" />
          <scope startOffset=""0x67"" endOffset=""0x87"">
            <constant name=""d2"" value=""0.2"" type=""Decimal"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            using (new CultureContext("en-US"))
            {
                string actual = GetPdbXml(text, TestOptions.ReleaseExe);
                AssertXmlEqual(expected, actual);
            }
        }

        [WorkItem(543490, "DevDiv")]
        [Fact()]
        public void IteratorMultipleEnumerables()
        {
            var text = @"
using System;
using System.Collections;
using System.Collections.Generic;

public class Test<T> : IEnumerable<T> where T : class
{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var v in this.IterProp)
        {
            yield return v;
        }
        foreach (var v in IterMethod())
        {
            yield return v;
        }
    }

    public IEnumerable<T> IterProp
    {
        get 
        { 
            yield return null;
            yield return null; 
        }
    }

    public IEnumerable<T> IterMethod()
    {
        yield return default(T);
        yield return null;
        yield break;
    }
}

public class Test
{
    public static void Main()
    {
        foreach (var v in new Test<string>()) { } 
    }
}
";
            string expected = @"
<symbols>
  <entryPoint declaringType=""Test"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""Test`1"" name=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0xc"">
        <namespace name=""System"" />
        <namespace name=""System.Collections"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""Test`1"" name=""GetEnumerator"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""48"" name=""&lt;GetEnumerator&gt;d__0"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Test`1"" name=""get_IterProp"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""48"" name=""&lt;get_IterProp&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Test`1"" name=""IterMethod"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""44"" name=""&lt;IterMethod&gt;d__2"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""45"" start_column=""5"" end_row=""45"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""46"" start_column=""9"" end_row=""46"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""46"" start_column=""27"" end_row=""46"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0xd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""46"" start_column=""18"" end_row=""46"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""46"" start_column=""47"" end_row=""46"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""46"" start_column=""49"" end_row=""46"" end_column=""50"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""46"" start_column=""24"" end_row=""46"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x22"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""47"" start_column=""5"" end_row=""47"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$5$0000"" il_index=""0"" il_start=""0x2"" il_end=""0x2d"" attributes=""1"" />
        <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2e"">
        <scope startOffset=""0x2"" endOffset=""0x2d"">
          <local name=""CS$5$0000"" il_index=""0"" il_start=""0x2"" il_end=""0x2d"" attributes=""1"" />
          <scope startOffset=""0xf"" endOffset=""0x18"">
            <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""Test`1+&lt;GetEnumerator&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""22"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""15"" start_column=""27"" end_row=""15"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x52"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x54"" start_row=""15"" start_column=""18"" end_row=""15"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x60"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x61"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x76"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7e"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x7f"" start_row=""15"" start_column=""24"" end_row=""15"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x9a"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x9b"" start_row=""19"" start_column=""27"" end_row=""19"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xb9"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xbb"" start_row=""19"" start_column=""18"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xc7"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xc8"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xda"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe2"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe3"" start_row=""19"" start_column=""24"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xfe"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x102"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""1"" il_start=""0x0"" il_end=""0x102"" attributes=""0"" />
        <local name=""v"" il_index=""2"" il_start=""0x54"" il_end=""0x7f"" attributes=""0"" />
        <local name=""v"" il_index=""3"" il_start=""0xbb"" il_end=""0xe3"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <scope startOffset=""0x0"" endOffset=""0x102"">
          <local name=""CS$524$0000"" il_index=""1"" il_start=""0x0"" il_end=""0x102"" attributes=""0"" />
          <scope startOffset=""0x54"" endOffset=""0x7f"">
            <local name=""v"" il_index=""2"" il_start=""0x54"" il_end=""0x7f"" attributes=""0"" />
          </scope>
          <scope startOffset=""0xbb"" endOffset=""0xe3"">
            <local name=""v"" il_index=""3"" il_start=""0xbb"" il_end=""0xe3"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""Test`1+&lt;get_IterProp&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""29"" start_column=""13"" end_row=""29"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x44"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4b"" start_row=""30"" start_column=""13"" end_row=""30"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x62"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x69"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6d"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6d"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test`1+&lt;IterMethod&gt;d__2"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""35"" start_column=""5"" end_row=""35"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""36"" start_column=""9"" end_row=""36"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x44"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4b"" start_row=""37"" start_column=""9"" end_row=""37"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x62"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x69"" start_row=""38"" start_column=""9"" end_row=""38"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6d"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6d"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";

            string actual = GetPdbXml(text, TestOptions.DebugExe);
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void VariablesWithSubstitutedType1()
        {
            var text = @"
using System.Collections.Generic;
class C
{
    static IEnumerable<T> F<T>(T[] o)
    {
        for (int i = 0; i < o.Length; i++)
        {
            T t = default(T);
            yield return t;
            yield return o[i];
        }
    }
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll, "C+<F>d__0`1.MoveNext");

            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x2d"" endOffset=""0xa8"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""7"" start_column=""14"" end_row=""7"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x37"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x51"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x7a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x82"" start_row=""7"" start_column=""39"" end_row=""7"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x92"" start_row=""7"" start_column=""25"" end_row=""7"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xa4"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa8"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xaf"" attributes=""0"" />
        <local name=""t"" il_index=""2"" il_start=""0x36"" il_end=""0x82"" attributes=""0"" />
        <local name=""CS$4$0001"" il_index=""4"" il_start=""0x92"" il_end=""0xa8"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xaf"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xaf"" attributes=""0"" />
        <scope startOffset=""0x36"" endOffset=""0x82"">
          <local name=""t"" il_index=""2"" il_start=""0x36"" il_end=""0x82"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x92"" endOffset=""0xa8"">
          <local name=""CS$4$0001"" il_index=""4"" il_start=""0x92"" il_end=""0xa8"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }


        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void LambdaDisplayClassLocalHoistedInIterator()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        yield return x1 + x2 + x3;
        yield return x1 + x2 + x3;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__2.MoveNext");

            // One iterator local entry for the lambda local.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__2"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x3e"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x4a"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x56"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x62"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0xb0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xb7"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0xee"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf5"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xfc"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xfc"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xfc"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var userType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__2");
                var fieldDisplayStrings = stateMachineType.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString());
                AssertEx.SetEqual(fieldDisplayStrings, "C.<>c__DisplayClass0 C.<M>d__2.CS$<>8__locals1"); // Name follows lambda local pattern.
            });
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void LambdaDisplayClassLocalNotHoistedInIterator()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
        byte x1 = 1;
        byte x2 = 1;
        byte x3 = 1;

        ((Action)(() => { x1 = x2 = x3; }))();

        yield return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__2.MoveNext");

            // No iterator local entries.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__2"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2f"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x4f"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x61"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x68"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6c"" attributes=""0"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""2"" il_start=""0x21"" il_end=""0x6c"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6c"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x6c"" attributes=""0"" />
        <scope startOffset=""0x21"" endOffset=""0x6c"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""2"" il_start=""0x21"" il_end=""0x6c"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var userType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__2");
                var fieldDisplayStrings = stateMachineType.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString());
                AssertEx.SetEqual(fieldDisplayStrings); // No fields for hoisted locals.
            });
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocalHoistedInIterator()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
        dynamic d = 1;
        yield return d;
        d.ToString();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__3.MoveNext");

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__3"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x21"" endOffset=""0xec"" />
        </iteratorLocals>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x86"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8d"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xe5"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xec"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xec"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xec"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocalNotHoistedInIterator()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
        dynamic d = 1;
        yield return d;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
            string actual = GetPdbXml(comp, "C+<M>d__2.MoveNext");

            // One dynamic local entry for "d".
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__2"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x83"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
        <local name=""d"" il_index=""2"" il_start=""0x21"" il_end=""0x87"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x87"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
        <scope startOffset=""0x21"" endOffset=""0x87"">
          <local name=""d"" il_index=""2"" il_start=""0x21"" il_end=""0x87"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(667579, "DevDiv")]
        public void DebuggerHiddenIterator()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    static void Main(string[] args)
    {
        foreach (var x in F()) ;
    }

    [DebuggerHidden]
    static IEnumerable<int> F()
    {
        throw new Exception();
        yield break;
    }
}";
            string actual = GetPdbXml(text, TestOptions.DebugDll, "C+<F>d__0.MoveNext");
            string expected =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x20"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x20"">
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x20"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }
    }
}
