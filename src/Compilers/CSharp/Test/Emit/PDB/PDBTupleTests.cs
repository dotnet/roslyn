// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTupleTests : CSharpPDBTestBase
    {
        [Fact]
        public void Local()
        {
            var source =
@"class C
{
    static void F()
    {
        (int A, int B, (int C, int), int, int, int G, int H, int I) t = (1, 2, (3, 4), 5, 6, 7, 8, 9);
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <tupleElementNames>
          <local elementNames=""|A|B||||G|H|I|C||"" slotIndex=""0"" localName=""t"" scopeStart=""0x0"" scopeEnd=""0x0"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""71"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""103"" />
        <entry offset=""0x1b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <local name=""t"" il_index=""0"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Constant()
        {
            var source =
@"class C<T>
{
    static (int, int) F;
}
class C
{
    static void F()
    {
        const C<(int A, int B)> c = null;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <tupleElementNames>
          <local elementNames=""|A|B"" slotIndex=""-1"" localName=""c"" scopeStart=""0x0"" scopeEnd=""0x2"" />
        </tupleElementNames>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""c"" value=""null"" signature=""C`1{System.ValueTuple`2{Int32, Int32}}"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TuplesAndDynamic()
        {
            var source =
@"class C<T>
{
}
class C
{
    static void F()
    {
        {
            (dynamic A, object B, object)[] x;
            const C<(object, dynamic, object C)> y = null;
        }
        {
            const C<(object A, object)> x = null;
        }
        {
            const C<(object, dynamic)> x = null;
            const C<(object, dynamic B)> y = null;
        }
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flagCount=""5"" flags=""00100"" slotId=""0"" localName=""x"" />
          <bucket flagCount=""5"" flags=""00010"" slotId=""0"" localName=""y"" />
          <bucket flagCount=""4"" flags=""0001"" slotId=""0"" localName=""x"" />
          <bucket flagCount=""4"" flags=""0001"" slotId=""0"" localName=""y"" />
        </dynamicLocals>
        <tupleElementNames>
          <local elementNames=""|A|B|"" slotIndex=""0"" localName=""x"" scopeStart=""0x0"" scopeEnd=""0x0"" />
          <local elementNames=""|||C"" slotIndex=""-1"" localName=""y"" scopeStart=""0x1"" scopeEnd=""0x3"" />
          <local elementNames=""|A|"" slotIndex=""-1"" localName=""x"" scopeStart=""0x3"" scopeEnd=""0x5"" />
          <local elementNames=""||B"" slotIndex=""-1"" localName=""y"" scopeStart=""0x5"" scopeEnd=""0x7"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""58"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0x4"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
        <entry offset=""0x5"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x6"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" />
        <entry offset=""0x7"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <local name=""x"" il_index=""0"" il_start=""0x1"" il_end=""0x3"" attributes=""0"" />
          <constant name=""y"" value=""null"" signature=""C`1{System.ValueTuple`3{Object, Object, Object}}"" />
        </scope>
        <scope startOffset=""0x3"" endOffset=""0x5"">
          <constant name=""x"" value=""null"" signature=""C`1{System.ValueTuple`2{Object, Object}}"" />
        </scope>
        <scope startOffset=""0x5"" endOffset=""0x7"">
          <constant name=""x"" value=""null"" signature=""C`1{System.ValueTuple`2{Object, Object}}"" />
          <constant name=""y"" value=""null"" signature=""C`1{System.ValueTuple`2{Object, Object}}"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void MultiByteCharacters()
        {
            var source =
@"class C
{
    static void F()
    {
        (int \u1234, int, int \u005f\u1200\u005f) \u1200 = (1, 2, 3);
    }
}";
            var comp = CreateCompilationWithMscorlib(source, new[] { ValueTupleRef }, options: TestOptions.DebugDll);
            comp.VerifyPdb(
string.Format(@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <tupleElementNames>
          <local elementNames=""|{0}||{1}"" slotIndex=""0"" localName=""{2}"" scopeStart=""0x0"" scopeEnd=""0x0"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""53"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""70"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""{2}"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>",
    "\u1234",
    "_\u1200_",
    "\u1200"));
        }
    }
}
