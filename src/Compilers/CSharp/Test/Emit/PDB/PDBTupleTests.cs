// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTupleTests : CSharpPDBTestBase
    {
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""103"" document=""1"" />
        <entry offset=""0x1b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <local name=""t"" il_index=""0"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
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
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""c"" value=""null"" signature=""C`1{System.ValueTuple`2{Int32, Int32}}"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""00100"" slotId=""0"" localName=""x"" />
          <bucket flags=""00010"" slotId=""0"" localName=""y"" />
          <bucket flags=""0001"" slotId=""0"" localName=""x"" />
          <bucket flags=""0001"" slotId=""0"" localName=""y"" />
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x5"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""1"" />
        <entry offset=""0x7"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyPdb(
string.Format(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""70"" document=""1"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
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

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DeconstructionForeach()
        {
            var source =
@"class C
{
    static void F(System.Collections.Generic.IEnumerable<(int a, int b)> ie)
    { //4,5
        foreach (         //5,9
            var (a, b)    //6,13
            in            //7,13
            ie)           //8,13
        { //9,9
        } //10,9
    } //11,5
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyPdb(
string.Format(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""ie"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""5"" offset=""17"" />
          <slot kind=""0"" offset=""59"" />
          <slot kind=""0"" offset=""62"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""15"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0xb"" startLine=""6"" startColumn=""13"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1e"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1f"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x20"" startLine=""7"" startColumn=""13"" endLine=""7"" endColumn=""15"" document=""1"" />
        <entry offset=""0x2a"" hidden=""true"" document=""1"" />
        <entry offset=""0x34"" hidden=""true"" document=""1"" />
        <entry offset=""0x35"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x36"">
        <scope startOffset=""0xb"" endOffset=""0x20"">
          <local name=""a"" il_index=""1"" il_start=""0xb"" il_end=""0x20"" attributes=""0"" />
          <local name=""b"" il_index=""2"" il_start=""0xb"" il_end=""0x20"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>"));
        }

        [WorkItem(17947, "https://github.com/dotnet/roslyn/issues/17947")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void VariablesAndConstantsInUnreachableCode()
        {
            string source = @"
class C
{
    void F()
    {
        (int a, int b)[] v1 = null;
        const (int a, int b)[] c1 = null;

        throw null;

        (int a, int b)[] v2 = null; 
        const (int a, int b)[] c2 = null;

        { 
            (int a, int b)[] v3 = null; 
            const (int a, int b)[] c3 = null;
        }
    }
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size        5 (0x5)
  .maxstack  1
  .locals init ((int a, int b)[] V_0, //v1
                (int a, int b)[] V_1, //v2
                (int a, int b)[] V_2) //v3
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldnull
  IL_0004:  throw
}
");

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <tupleElementNames>
          <local elementNames=""|a|b"" slotIndex=""0"" localName=""v1"" scopeStart=""0x0"" scopeEnd=""0x0"" />
          <local elementNames=""|a|b"" slotIndex=""1"" localName=""v2"" scopeStart=""0x0"" scopeEnd=""0x0"" />
          <local elementNames=""|a|b"" slotIndex=""-1"" localName=""c1"" scopeStart=""0x0"" scopeEnd=""0x5"" />
          <local elementNames=""|a|b"" slotIndex=""-1"" localName=""c2"" scopeStart=""0x0"" scopeEnd=""0x5"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""28"" />
          <slot kind=""0"" offset=""133"" />
          <slot kind=""0"" offset=""232"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""36"" document=""1"" />
        <entry offset=""0x3"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x5"">
        <local name=""v1"" il_index=""0"" il_start=""0x0"" il_end=""0x5"" attributes=""0"" />
        <local name=""v2"" il_index=""1"" il_start=""0x0"" il_end=""0x5"" attributes=""0"" />
        <constant name=""c1"" value=""null"" signature=""System.ValueTuple`2{Int32, Int32}[]"" />
        <constant name=""c2"" value=""null"" signature=""System.ValueTuple`2{Int32, Int32}[]"" />
      </scope>
    </method>
  </methods>
</symbols>

");
        }
    }
}
