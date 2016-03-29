// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBConstantTests : CSharpTestBase
    {
        private CultureInfo _testCulture = new CultureInfo("en-US");

        [Fact]
        public void TestSimpleLocalConstants()
        {
            var text = @"
class C
{
    void M()
    {
        const int x = 1;
        {
            const int y = 2;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <constant name=""x"" value=""1"" type=""Int32"" />
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <constant name=""y"" value=""2"" type=""Int32"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestLambdaLocalConstants()
        {
            var text = @"
using System;

class C
{
    void M(Action a)
    {
        const int x = 1;
        M(() =>
        {
            const int y = 2;
            {
                const int z = 3;
            }
        });
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""a"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""54"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""15"" endColumn=""12"" />
        <entry offset=""0x27"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <namespace name=""System"" />
        <constant name=""x"" value=""1"" type=""Int32"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" parameterNames=""a"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""14"" />
        <entry offset=""0x2"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" />
        <entry offset=""0x3"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <constant name=""y"" value=""2"" type=""Int32"" />
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <constant name=""z"" value=""3"" type=""Int32"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543342")]
        [Fact]
        public void TestIteratorLocalConstants()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        const int x = 1;
        for (int i = 0; i < 10; i++)
        {
            const int y = 2;
            yield return x + y + i;
        }
    }
}
";
            // NOTE: Roslyn's output is somewhat different than Dev10's in this case, but
            // all of the changes look reasonable.  The main thing for this test is that 
            // Dev10 creates fields for the locals in the iterator class.  Roslyn doesn't
            // do that - the <constant> in the <scope> is sufficient.
            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<>4__this",
                    "<i>5__1"
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x20"" endOffset=""0x66"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""37"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x1f"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x20"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" />
        <entry offset=""0x27"" hidden=""true"" />
        <entry offset=""0x29"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0x2a"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""36"" />
        <entry offset=""0x41"" hidden=""true"" />
        <entry offset=""0x48"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x49"" startLine=""9"" startColumn=""33"" endLine=""9"" endColumn=""36"" />
        <entry offset=""0x59"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""31"" />
        <entry offset=""0x64"" hidden=""true"" />
        <entry offset=""0x67"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x69"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x1f"" endOffset=""0x69"">
          <constant name=""x"" value=""1"" type=""Int32"" />
          <scope startOffset=""0x29"" endOffset=""0x49"">
            <constant name=""y"" value=""2"" type=""Int32"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestLocalConstantsTypes()
        {
            var text = @"
class C
{
    void M()
    {
        const object o = null;
        const string s = ""hello"";
        const float f = float.MinValue;
        const double d = double.MaxValue;
    }
}
";
            using (new CultureContext("en-US"))
            {
                CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""o"" value=""null"" type=""Object"" />
        <constant name=""s"" value=""hello"" type=""String"" />
        <constant name=""f"" value=""-3.402823E+38"" type=""Single"" />
        <constant name=""d"" value=""1.79769313486232E+308"" type=""Double"" />
      </scope>
    </method>
  </methods>
</symbols>");
            }
        }

        [Fact]
        public void StringConstantTooLong()
        {
            var text = @"
class C
{
    void M()
    {
        const string text = @""
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB"";
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);

            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""5"" endLine=""43"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""5"" endLine=""43"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""text"" value=""\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [Fact]
        public void TestDecimalLocalConstants()
        {
            var text = @"
class C
{
    void M()
    {
        const decimal d = (decimal)1.5;
    }
}
";
            using (new CultureContext("en-US"))
            {
                CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""d"" value=""1.5"" type=""Decimal"" />
      </scope>
    </method>
  </methods>
</symbols>");
            }
        }
    }
}
