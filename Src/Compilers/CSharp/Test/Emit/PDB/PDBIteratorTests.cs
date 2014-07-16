// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBIteratorTests : CSharpPDBTestBase
    {
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
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xaf"" attributes=""1"" />
        <local name=""t"" il_index=""2"" il_start=""0x36"" il_end=""0x82"" attributes=""0"" />
        <local name=""CS$4$0001"" il_index=""4"" il_start=""0x92"" il_end=""0xa8"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xaf"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0xaf"" attributes=""1"" />
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
    }
}
