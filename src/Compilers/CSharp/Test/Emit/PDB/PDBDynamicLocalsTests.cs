// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBDynamicLocalsTests : CSharpTestBase
    {
        [Fact]
        public void EmitPDBDynamicObjectVariable1()
        {
            string source = WithWindowsLineBreaks(@"
class Helper
{
	int x;
	public void goo(int y){}
	public Helper(){}
	public Helper(int x){}
}
struct Point
{	
	int x;
	int y;
}
class Test
{
  delegate void D(int y);
  public static void Main(string[] args)
  {
		dynamic d1 = new Helper();
		dynamic d2 = new Point(); 
		D d4 = new D(d1.goo); 
		Helper d5 = new Helper(d1); 
		
  }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, references: new[] { CSharpRef }, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Helper"" name=""goo"" parameterNames=""y"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""24"" endLine=""5"" endColumn=""25"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""25"" endLine=""5"" endColumn=""26"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Helper"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""2"" endLine=""6"" endColumn=""17"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""17"" endLine=""6"" endColumn=""18"" document=""1"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""19"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""2"" endLine=""7"" endColumn=""22"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""22"" endLine=""7"" endColumn=""23"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""23"" endLine=""7"" endColumn=""24"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d1"" />
          <bucket flags=""1"" slotId=""1"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""43"" />
          <slot kind=""0"" offset=""67"" />
          <slot kind=""0"" offset=""98"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""3"" endLine=""18"" endColumn=""4"" document=""1"" />
        <entry offset=""0x1"" startLine=""19"" startColumn=""3"" endLine=""19"" endColumn=""29"" document=""1"" />
        <entry offset=""0x7"" startLine=""20"" startColumn=""3"" endLine=""20"" endColumn=""28"" document=""1"" />
        <entry offset=""0x17"" startLine=""21"" startColumn=""3"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0xb1"" startLine=""22"" startColumn=""3"" endLine=""22"" endColumn=""30"" document=""1"" />
        <entry offset=""0x10f"" startLine=""24"" startColumn=""3"" endLine=""24"" endColumn=""4"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x110"">
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d2"" il_index=""1"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d4"" il_index=""2"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d5"" il_index=""3"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBLangConstructsLocals1()
        {
            string source = WithWindowsLineBreaks(@"
using System;
class Test
{
    public static void Main(string[] args)
    {
		dynamic[] arrDynamic = new dynamic[] {""1""};
        foreach (dynamic d in arrDynamic)  
        {
            //do nothing
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""01"" slotId=""0"" localName=""arrDynamic"" />
          <bucket flags=""1"" slotId=""3"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""6"" offset=""58"" />
          <slot kind=""8"" offset=""58"" />
          <slot kind=""0"" offset=""58"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""3"" endLine=""7"" endColumn=""46"" document=""1"" />
        <entry offset=""0x10"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" document=""1"" />
        <entry offset=""0x11"" startLine=""8"" startColumn=""31"" endLine=""8"" endColumn=""41"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1b"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1c"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""8"" startColumn=""28"" endLine=""8"" endColumn=""30"" document=""1"" />
        <entry offset=""0x27"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <namespace name=""System"" />
        <local name=""arrDynamic"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
        <scope startOffset=""0x17"" endOffset=""0x1d"">
          <local name=""d"" il_index=""3"" il_start=""0x17"" il_end=""0x1d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicConstVariable()
        {
            string source = @"
class Test
{
	public static void Main(string[] args)
	{
        {
            const dynamic d = null;
            const string c = null;
        }
        {
            const dynamic c = null;
            const dynamic d = null;
        }
	}
}";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
          <bucket flags=""1"" slotId=""0"" localName=""c"" />
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""2"" endLine=""5"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x5"" startLine=""14"" startColumn=""2"" endLine=""14"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <constant name=""d"" value=""null"" type=""Object"" />
          <constant name=""c"" value=""null"" type=""String"" />
        </scope>
        <scope startOffset=""0x3"" endOffset=""0x5"">
          <constant name=""c"" value=""null"" type=""Object"" />
          <constant name=""d"" value=""null"" type=""Object"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicDuplicateName()
        {
            string source = WithWindowsLineBreaks(@"
class Test
{
	public static void Main(string[] args)
	{
        {
            dynamic a = null;
            object b = null;
        }
        {
            dynamic[] a = null;
            dynamic b = null;
        }
	}
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""a"" />
          <bucket flags=""01"" slotId=""2"" localName=""a"" />
          <bucket flags=""1"" slotId=""3"" localName=""b"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""119"" />
          <slot kind=""0"" offset=""150"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""2"" endLine=""5"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""7"" startColumn=""13"" endLine=""7"" endColumn=""30"" document=""1"" />
        <entry offset=""0x4"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""29"" document=""1"" />
        <entry offset=""0x6"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""32"" document=""1"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""30"" document=""1"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd"" startLine=""14"" startColumn=""2"" endLine=""14"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe"">
        <scope startOffset=""0x1"" endOffset=""0x7"">
          <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x7"" attributes=""0"" />
          <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x7"" endOffset=""0xd"">
          <local name=""a"" il_index=""2"" il_start=""0x7"" il_end=""0xd"" attributes=""0"" />
          <local name=""b"" il_index=""3"" il_start=""0x7"" il_end=""0xd"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicVariableNameTooLong()
        {
            string source = WithWindowsLineBreaks(@"
class Test
{
	public static void Main(string[] args)
	{
        const dynamic a123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        const dynamic b12345678901234567890123456789012345678901234567890123456789012 = null; // 63 chars
        dynamic c123456789012345678901234567890123456789012345678901234567890123 = null; // 64 chars
        dynamic d12345678901234567890123456789012345678901234567890123456789012 = null; // 63 chars
	}
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""1"" localName=""d12345678901234567890123456789012345678901234567890123456789012"" />
          <bucket flags=""1"" slotId=""0"" localName=""b12345678901234567890123456789012345678901234567890123456789012"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""234"" />
          <slot kind=""0"" offset=""336"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""2"" endLine=""5"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""89"" document=""1"" />
        <entry offset=""0x3"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""88"" document=""1"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""2"" endLine=""10"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <local name=""c123456789012345678901234567890123456789012345678901234567890123"" il_index=""0"" il_start=""0x0"" il_end=""0x6"" attributes=""0"" />
        <local name=""d12345678901234567890123456789012345678901234567890123456789012"" il_index=""1"" il_start=""0x0"" il_end=""0x6"" attributes=""0"" />
        <constant name=""a123456789012345678901234567890123456789012345678901234567890123"" value=""null"" type=""Object"" />
        <constant name=""b12345678901234567890123456789012345678901234567890123456789012"" value=""null"" type=""Object"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicArrayVariable()
        {
            string source = WithWindowsLineBreaks(@"
class ArrayTest
{
	int x;
}
class Test
{
  public static void Main(string[] args)
  {
		dynamic[] arr = new dynamic[10];
		dynamic[,] arrdim = new string[2,3];
		dynamic[] arrobj = new ArrayTest[2];
  }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""01"" slotId=""0"" localName=""arr"" />
          <bucket flags=""01"" slotId=""1"" localName=""arrdim"" />
          <bucket flags=""01"" slotId=""2"" localName=""arrobj"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""52"" />
          <slot kind=""0"" offset=""91"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""3"" endLine=""9"" endColumn=""4"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""3"" endLine=""10"" endColumn=""35"" document=""1"" />
        <entry offset=""0x9"" startLine=""11"" startColumn=""3"" endLine=""11"" endColumn=""39"" document=""1"" />
        <entry offset=""0x13"" startLine=""12"" startColumn=""3"" endLine=""12"" endColumn=""39"" document=""1"" />
        <entry offset=""0x1e"" startLine=""13"" startColumn=""3"" endLine=""13"" endColumn=""4"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1f"">
        <local name=""arr"" il_index=""0"" il_start=""0x0"" il_end=""0x1f"" attributes=""0"" />
        <local name=""arrdim"" il_index=""1"" il_start=""0x0"" il_end=""0x1f"" attributes=""0"" />
        <local name=""arrobj"" il_index=""2"" il_start=""0x0"" il_end=""0x1f"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicCollectionVariable()
        {
            string source = WithWindowsLineBreaks(@"
using System.Collections.Generic;
class Test
{
  public static void Main(string[] args)
  {
		dynamic l1 = new List<int>();
		List<dynamic> l2 = new List<dynamic>();
		dynamic l3 = new List<dynamic>();
		Dictionary<dynamic,dynamic> d1 = new Dictionary<dynamic,dynamic>();
		dynamic d2 = new Dictionary<int,int>();
  }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""l1"" />
          <bucket flags=""01"" slotId=""1"" localName=""l2"" />
          <bucket flags=""1"" slotId=""2"" localName=""l3"" />
          <bucket flags=""011"" slotId=""3"" localName=""d1"" />
          <bucket flags=""1"" slotId=""4"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""52"" />
          <slot kind=""0"" offset=""89"" />
          <slot kind=""0"" offset=""146"" />
          <slot kind=""0"" offset=""197"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""3"" endLine=""6"" endColumn=""4"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""3"" endLine=""7"" endColumn=""32"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""3"" endLine=""8"" endColumn=""42"" document=""1"" />
        <entry offset=""0xd"" startLine=""9"" startColumn=""3"" endLine=""9"" endColumn=""36"" document=""1"" />
        <entry offset=""0x13"" startLine=""10"" startColumn=""3"" endLine=""10"" endColumn=""70"" document=""1"" />
        <entry offset=""0x19"" startLine=""11"" startColumn=""3"" endLine=""11"" endColumn=""42"" document=""1"" />
        <entry offset=""0x20"" startLine=""12"" startColumn=""3"" endLine=""12"" endColumn=""4"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x21"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""l1"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""l2"" il_index=""1"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""l3"" il_index=""2"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""d1"" il_index=""3"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""d2"" il_index=""4"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void EmitPDBDynamicObjectVariable2()
        {
            string source = WithWindowsLineBreaks(@"
class Helper
{
	int x;
	public void goo(int y){}
	public Helper(){}
	public Helper(int x){}
}
struct Point
{	
	int x;
	int y;
}
class Test
{
  delegate void D(int y);
  public static void Main(string[] args)
  {
		Helper staticObj = new Helper();
		dynamic d1 = new Helper();
		dynamic d3 = new D(staticObj.goo);
  }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Helper"" name=""goo"" parameterNames=""y"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""24"" endLine=""5"" endColumn=""25"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""25"" endLine=""5"" endColumn=""26"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Helper"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""2"" endLine=""6"" endColumn=""17"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""17"" endLine=""6"" endColumn=""18"" document=""1"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""19"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""2"" endLine=""7"" endColumn=""22"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""22"" endLine=""7"" endColumn=""23"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""23"" endLine=""7"" endColumn=""24"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Helper"" methodName=""goo"" parameterNames=""y"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""1"" localName=""d1"" />
          <bucket flags=""1"" slotId=""2"" localName=""d3"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""12"" />
          <slot kind=""0"" offset=""49"" />
          <slot kind=""0"" offset=""79"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""3"" endLine=""18"" endColumn=""4"" document=""1"" />
        <entry offset=""0x1"" startLine=""19"" startColumn=""3"" endLine=""19"" endColumn=""35"" document=""1"" />
        <entry offset=""0x7"" startLine=""20"" startColumn=""3"" endLine=""20"" endColumn=""29"" document=""1"" />
        <entry offset=""0xd"" startLine=""21"" startColumn=""3"" endLine=""21"" endColumn=""37"" document=""1"" />
        <entry offset=""0x1a"" startLine=""22"" startColumn=""3"" endLine=""22"" endColumn=""4"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1b"">
        <local name=""staticObj"" il_index=""0"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
        <local name=""d1"" il_index=""1"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
        <local name=""d3"" il_index=""2"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassConstructorDynamicLocals()
        {
            string source = WithWindowsLineBreaks(@"
class Test
{
	public Test()
	{
		dynamic d;
	}
	public static void Main(string[] args)
	{
	}
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""2"" endLine=""4"" endColumn=""15"" document=""1"" />
        <entry offset=""0x7"" startLine=""5"" startColumn=""2"" endLine=""5"" endColumn=""3"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""2"" endLine=""7"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <scope startOffset=""0x7"" endOffset=""0x9"">
          <local name=""d"" il_index=""0"" il_start=""0x7"" il_end=""0x9"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""2"" endLine=""9"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""2"" endLine=""10"" endColumn=""3"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassPropertyDynamicLocals()
        {
            string source = WithWindowsLineBreaks(@"
class Test
{
    string field;
    public dynamic Field
    {
        get
        {
            dynamic d = field + field;
            return d;
        }
        set
        {
            dynamic d = null;
            //field = d; Not yet implemented in Roslyn
        }
    }
    public static void Main(string[] args)
    {
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""get_Field"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""39"" document=""1"" />
        <entry offset=""0x13"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0x17"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_Field"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""get_Field"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""30"" document=""1"" />
        <entry offset=""0x3"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""get_Field"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassOverloadedOperatorDynamicLocals()
        {
            string source = WithWindowsLineBreaks(@"
class Complex
{
    int real;
    int imaginary;
    public Complex(int real, int imaginary)
    {
        this.real = real;
        this.imaginary = imaginary;
    }
    public static dynamic operator +(Complex c1, Complex c2)
    {
        dynamic d = new Complex(c1.real + c2.real, c1.imaginary + c2.imaginary);
        return d;
    }
}
class Test
{

    public static void Main(string[] args)
    {
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Complex"" name="".ctor"" parameterNames=""real, imaginary"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""44"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0xf"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""36"" document=""1"" />
        <entry offset=""0x16"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Complex"" name=""op_Addition"" parameterNames=""c1, c2"">
      <customDebugInfo>
        <forward declaringType=""Complex"" methodName="".ctor"" parameterNames=""real, imaginary"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""81"" document=""1"" />
        <entry offset=""0x21"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""18"" document=""1"" />
        <entry offset=""0x25"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Complex"" methodName="".ctor"" parameterNames=""real, imaginary"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""22"" startColumn=""5"" endLine=""22"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassIndexerDynamicLocal()
        {
            string source = WithWindowsLineBreaks(@"
class Test
{
    dynamic[] arr;
    public dynamic this[int i]
    {
        get
        {
            dynamic d = arr[i];
            return d;
        }
        set
        {
            dynamic d = (dynamic) value;
            arr[i] = d;
        }
    }

    public static void Main(string[] args)
    {
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""get_Item"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""32"" document=""1"" />
        <entry offset=""0xa"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0xe"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x10"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x10"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_Item"" parameterNames=""i, value"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""get_Item"" parameterNames=""i"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""41"" document=""1"" />
        <entry offset=""0x3"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0xc"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xd"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""get_Item"" parameterNames=""i"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassEventHandlerDynamicLocal()
        {
            string source = WithWindowsLineBreaks(@"
using System;
class Sample
{
    public static void Main()
    {
        ConsoleKeyInfo cki;
        Console.Clear();
        Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
    }
    protected static void myHandler(object sender, ConsoleCancelEventArgs args)
    {
        dynamic d;
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Sample"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""26"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""25"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""76"" document=""1"" />
        <entry offset=""0x19"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1a"">
        <namespace name=""System"" />
        <local name=""cki"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Sample"" name=""myHandler"" parameterNames=""sender, args"">
      <customDebugInfo>
        <forward declaringType=""Sample"" methodName=""Main"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBStructDynamicLocals()
        {
            string source = WithWindowsLineBreaks(@"
using System;
struct Test
{
    int d;
    public Test(int d)
    {
        dynamic d1;
        this.d = d;
    }
    public int D
    {
        get
        {
            dynamic d2;
            return d;
        }
        set
        {
            dynamic d3;
            d = value;
        }

    }
    public static Test operator +(Test t1, Test t2)
    {
        dynamic d4;
        return new Test(t1.d + t2.d);
    }
    public static void Main()
    {
        dynamic d5;
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name="".ctor"" parameterNames=""d"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d1"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
        <entry offset=""0x8"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""get_D"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""22"" document=""1"" />
        <entry offset=""0xa"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xc"">
        <local name=""d2"" il_index=""0"" il_start=""0x0"" il_end=""0xc"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_D"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d3"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""23"" document=""1"" />
        <entry offset=""0x8"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""d3"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""op_Addition"" parameterNames=""t1, t2"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d4"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""38"" document=""1"" />
        <entry offset=""0x16"" startLine=""29"" startColumn=""5"" endLine=""29"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x18"">
        <local name=""d4"" il_index=""0"" il_start=""0x0"" il_end=""0x18"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d5"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""31"" startColumn=""5"" endLine=""31"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""33"" startColumn=""5"" endLine=""33"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <local name=""d5"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBAnonymousFunctionLocals()
        {
            string source = WithWindowsLineBreaks(@"
using System;
class Test
{
    public delegate dynamic D1(dynamic d1);
    public delegate void D2(dynamic d2);
    public static void Main(string[] args)
    {
        D1 obj1 = d3 => d3;
        D2 obj2 = new D2(d4 => { dynamic d5; d5 = d4; });
        D1 obj3 = (dynamic d6) => { return d6; };
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""14"" />
          <slot kind=""0"" offset=""43"" />
          <slot kind=""0"" offset=""102"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <lambda offset=""27"" />
          <lambda offset=""63"" />
          <lambda offset=""125"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""28"" document=""1"" />
        <entry offset=""0x21"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""58"" document=""1"" />
        <entry offset=""0x41"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""50"" document=""1"" />
        <entry offset=""0x61"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x62"">
        <namespace name=""System"" />
        <local name=""obj1"" il_index=""0"" il_start=""0x0"" il_end=""0x62"" attributes=""0"" />
        <local name=""obj2"" il_index=""1"" il_start=""0x0"" il_end=""0x62"" attributes=""0"" />
        <local name=""obj3"" il_index=""2"" il_start=""0x0"" il_end=""0x62"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c"" name=""&lt;Main&gt;b__2_0"" parameterNames=""d3"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""27"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test+&lt;&gt;c"" name=""&lt;Main&gt;b__2_1"" parameterNames=""d4"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d5"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""73"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""32"" endLine=""10"" endColumn=""33"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""46"" endLine=""10"" endColumn=""54"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""55"" endLine=""10"" endColumn=""56"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""d5"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c"" name=""&lt;Main&gt;b__2_2"" parameterNames=""d6"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""125"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""35"" endLine=""11"" endColumn=""36"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""37"" endLine=""11"" endColumn=""47"" document=""1"" />
        <entry offset=""0x5"" startLine=""11"" startColumn=""48"" endLine=""11"" endColumn=""49"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBLangConstructsLocalVariables()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;
class Test
{
    public static void Main(string[] args)
    {
        int d1 = 0;
        int[] arrInt = new int[] { 1, 2, 3 };
        dynamic[] scores = new dynamic[] { ""97"", ""92"", ""81"", ""60"" };
        dynamic[] arrDynamic = new dynamic[] { ""1"", ""2"", ""3"" };
        while (d1 < 1)
        {
            dynamic dInWhile;
            d1++;
        }
        do
        {
            dynamic dInDoWhile;
            d1++;
        } while (d1 < 1);
        foreach (int d in arrInt)
        {
            dynamic dInForEach;
        }
        for (int i = 0; i < 1; i++)
        {
            dynamic dInFor;
        }
        for (dynamic d = ""1""; d1 < 0;)
        {
            //do nothing
        }
        if (d1 == 0)
        {
            dynamic dInIf;
        }
        else
        {
            dynamic dInElse;
        }
        try
        {
            dynamic dInTry;
            throw new Exception();
        }
        catch
        {
            dynamic dInCatch;
        }
        finally
        {
            dynamic dInFinally;
        }
        IEnumerable<dynamic> scoreQuery1 =
            from score in scores
            select score;
        dynamic scoreQuery2 =
            from score in scores
            select score;
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <dynamicLocals>
          <bucket flags=""01"" slotId=""2"" localName=""scores"" />
          <bucket flags=""01"" slotId=""3"" localName=""arrDynamic"" />
          <bucket flags=""01"" slotId=""4"" localName=""scoreQuery1"" />
          <bucket flags=""1"" slotId=""5"" localName=""scoreQuery2"" />
          <bucket flags=""1"" slotId=""6"" localName=""dInWhile"" />
          <bucket flags=""1"" slotId=""8"" localName=""dInDoWhile"" />
          <bucket flags=""1"" slotId=""13"" localName=""dInForEach"" />
          <bucket flags=""1"" slotId=""15"" localName=""dInFor"" />
          <bucket flags=""1"" slotId=""17"" localName=""d"" />
          <bucket flags=""1"" slotId=""20"" localName=""dInIf"" />
          <bucket flags=""1"" slotId=""21"" localName=""dInElse"" />
          <bucket flags=""1"" slotId=""22"" localName=""dInTry"" />
          <bucket flags=""1"" slotId=""23"" localName=""dInCatch"" />
          <bucket flags=""1"" slotId=""24"" localName=""dInFinally"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""38"" />
          <slot kind=""0"" offset=""89"" />
          <slot kind=""0"" offset=""159"" />
          <slot kind=""0"" offset=""1077"" />
          <slot kind=""0"" offset=""1169"" />
          <slot kind=""0"" offset=""261"" />
          <slot kind=""1"" offset=""214"" />
          <slot kind=""0"" offset=""345"" />
          <slot kind=""1"" offset=""310"" />
          <slot kind=""6"" offset=""412"" />
          <slot kind=""8"" offset=""412"" />
          <slot kind=""0"" offset=""412"" />
          <slot kind=""0"" offset=""470"" />
          <slot kind=""0"" offset=""511"" />
          <slot kind=""0"" offset=""562"" />
          <slot kind=""1"" offset=""502"" />
          <slot kind=""0"" offset=""603"" />
          <slot kind=""1"" offset=""590"" />
          <slot kind=""1"" offset=""678"" />
          <slot kind=""0"" offset=""723"" />
          <slot kind=""0"" offset=""787"" />
          <slot kind=""0"" offset=""852"" />
          <slot kind=""0"" offset=""954"" />
          <slot kind=""0"" offset=""1024"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""1145"" />
          <lambda offset=""1237"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""46"" document=""1"" />
        <entry offset=""0x15"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""69"" document=""1"" />
        <entry offset=""0x3c"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""64"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0x5d"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x5e"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""18"" document=""1"" />
        <entry offset=""0x62"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x63"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""23"" document=""1"" />
        <entry offset=""0x69"" hidden=""true"" document=""1"" />
        <entry offset=""0x6d"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6e"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""18"" document=""1"" />
        <entry offset=""0x72"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x73"" startLine=""22"" startColumn=""11"" endLine=""22"" endColumn=""26"" document=""1"" />
        <entry offset=""0x79"" hidden=""true"" document=""1"" />
        <entry offset=""0x7d"" startLine=""23"" startColumn=""9"" endLine=""23"" endColumn=""16"" document=""1"" />
        <entry offset=""0x7e"" startLine=""23"" startColumn=""27"" endLine=""23"" endColumn=""33"" document=""1"" />
        <entry offset=""0x84"" hidden=""true"" document=""1"" />
        <entry offset=""0x86"" startLine=""23"" startColumn=""18"" endLine=""23"" endColumn=""23"" document=""1"" />
        <entry offset=""0x8d"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8e"" startLine=""26"" startColumn=""9"" endLine=""26"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8f"" hidden=""true"" document=""1"" />
        <entry offset=""0x95"" startLine=""23"" startColumn=""24"" endLine=""23"" endColumn=""26"" document=""1"" />
        <entry offset=""0x9d"" startLine=""27"" startColumn=""14"" endLine=""27"" endColumn=""23"" document=""1"" />
        <entry offset=""0xa0"" hidden=""true"" document=""1"" />
        <entry offset=""0xa2"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa3"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa4"" startLine=""27"" startColumn=""32"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0xaa"" startLine=""27"" startColumn=""25"" endLine=""27"" endColumn=""30"" document=""1"" />
        <entry offset=""0xb1"" hidden=""true"" document=""1"" />
        <entry offset=""0xb5"" startLine=""31"" startColumn=""14"" endLine=""31"" endColumn=""29"" document=""1"" />
        <entry offset=""0xbc"" hidden=""true"" document=""1"" />
        <entry offset=""0xbe"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" document=""1"" />
        <entry offset=""0xbf"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc0"" startLine=""31"" startColumn=""31"" endLine=""31"" endColumn=""37"" document=""1"" />
        <entry offset=""0xc6"" hidden=""true"" document=""1"" />
        <entry offset=""0xca"" startLine=""35"" startColumn=""9"" endLine=""35"" endColumn=""21"" document=""1"" />
        <entry offset=""0xd0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd4"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd5"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd6"" hidden=""true"" document=""1"" />
        <entry offset=""0xd8"" startLine=""40"" startColumn=""9"" endLine=""40"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd9"" startLine=""42"" startColumn=""9"" endLine=""42"" endColumn=""10"" document=""1"" />
        <entry offset=""0xda"" hidden=""true"" document=""1"" />
        <entry offset=""0xdb"" startLine=""44"" startColumn=""9"" endLine=""44"" endColumn=""10"" document=""1"" />
        <entry offset=""0xdc"" startLine=""46"" startColumn=""13"" endLine=""46"" endColumn=""35"" document=""1"" />
        <entry offset=""0xe2"" startLine=""48"" startColumn=""9"" endLine=""48"" endColumn=""14"" document=""1"" />
        <entry offset=""0xe3"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe4"" startLine=""51"" startColumn=""9"" endLine=""51"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe9"" startLine=""53"" startColumn=""9"" endLine=""53"" endColumn=""10"" document=""1"" />
        <entry offset=""0xea"" startLine=""55"" startColumn=""9"" endLine=""55"" endColumn=""10"" document=""1"" />
        <entry offset=""0xec"" startLine=""56"" startColumn=""9"" endLine=""58"" endColumn=""26"" document=""1"" />
        <entry offset=""0x113"" startLine=""59"" startColumn=""9"" endLine=""61"" endColumn=""26"" document=""1"" />
        <entry offset=""0x13a"" startLine=""62"" startColumn=""5"" endLine=""62"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x13b"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <local name=""arrInt"" il_index=""1"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <local name=""scores"" il_index=""2"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <local name=""arrDynamic"" il_index=""3"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <local name=""scoreQuery1"" il_index=""4"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <local name=""scoreQuery2"" il_index=""5"" il_start=""0x0"" il_end=""0x13b"" attributes=""0"" />
        <scope startOffset=""0x5d"" endOffset=""0x63"">
          <local name=""dInWhile"" il_index=""6"" il_start=""0x5d"" il_end=""0x63"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x6d"" endOffset=""0x73"">
          <local name=""dInDoWhile"" il_index=""8"" il_start=""0x6d"" il_end=""0x73"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x86"" endOffset=""0x8f"">
          <local name=""d"" il_index=""12"" il_start=""0x86"" il_end=""0x8f"" attributes=""0"" />
          <scope startOffset=""0x8d"" endOffset=""0x8f"">
            <local name=""dInForEach"" il_index=""13"" il_start=""0x8d"" il_end=""0x8f"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0x9d"" endOffset=""0xb5"">
          <local name=""i"" il_index=""14"" il_start=""0x9d"" il_end=""0xb5"" attributes=""0"" />
          <scope startOffset=""0xa2"" endOffset=""0xa4"">
            <local name=""dInFor"" il_index=""15"" il_start=""0xa2"" il_end=""0xa4"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0xb5"" endOffset=""0xca"">
          <local name=""d"" il_index=""17"" il_start=""0xb5"" il_end=""0xca"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xd4"" endOffset=""0xd6"">
          <local name=""dInIf"" il_index=""20"" il_start=""0xd4"" il_end=""0xd6"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xd8"" endOffset=""0xda"">
          <local name=""dInElse"" il_index=""21"" il_start=""0xd8"" il_end=""0xda"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xdb"" endOffset=""0xe2"">
          <local name=""dInTry"" il_index=""22"" il_start=""0xdb"" il_end=""0xe2"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xe3"" endOffset=""0xe5"">
          <local name=""dInCatch"" il_index=""23"" il_start=""0xe3"" il_end=""0xe5"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xe9"" endOffset=""0xeb"">
          <local name=""dInFinally"" il_index=""24"" il_start=""0xe9"" il_end=""0xeb"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c"" name=""&lt;Main&gt;b__0_0"" parameterNames=""score"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""58"" startColumn=""20"" endLine=""58"" endColumn=""25"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test+&lt;&gt;c"" name=""&lt;Main&gt;b__0_1"" parameterNames=""score"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""61"" startColumn=""20"" endLine=""61"" endColumn=""25"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBLangConstructsLocalConstants()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;
class Test
{
    public static void Main(string[] args)
    {
        int d1 = 0;
        int[] arrInt = new int[] { 1, 2, 3 };
        const dynamic scores = null;
        const dynamic arrDynamic = null;
        while (d1 < 1)
        {
            const dynamic dInWhile = null;
            d1++;
        }
        do
        {
            const dynamic dInDoWhile = null;
            d1++;
        } while (d1 < 1);
        foreach (int d in arrInt)
        {
            const dynamic dInForEach = null;
        }
        for (int i = 0; i < 1; i++)
        {
            const dynamic dInFor = null;
        }
        for (dynamic d = ""1""; d1 < 0;)
        {
            //do nothing
        }
        if (d1 == 0)
        {
            const dynamic dInIf = null;
        }
        else
        {
            const dynamic dInElse = null;
        }
        try
        {
            const dynamic dInTry = null;
            throw new Exception();
        }
        catch
        {
            const dynamic dInCatch = null;
        }
        finally
        {
            const dynamic dInFinally = null;
        }
        const IEnumerable<dynamic> scoreQuery1 = null;
        const dynamic scoreQuery2 = null;
    }
}");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""scores"" />
          <bucket flags=""1"" slotId=""0"" localName=""arrDynamic"" />
          <bucket flags=""01"" slotId=""0"" localName=""scoreQuery1"" />
          <bucket flags=""1"" slotId=""0"" localName=""scoreQuery2"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInWhile"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInDoWhile"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInForEach"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInFor"" />
          <bucket flags=""1"" slotId=""9"" localName=""d"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInIf"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInElse"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInTry"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInCatch"" />
          <bucket flags=""1"" slotId=""0"" localName=""dInFinally"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""38"" />
          <slot kind=""1"" offset=""159"" />
          <slot kind=""1"" offset=""268"" />
          <slot kind=""6"" offset=""383"" />
          <slot kind=""8"" offset=""383"" />
          <slot kind=""0"" offset=""383"" />
          <slot kind=""0"" offset=""495"" />
          <slot kind=""1"" offset=""486"" />
          <slot kind=""0"" offset=""600"" />
          <slot kind=""1"" offset=""587"" />
          <slot kind=""1"" offset=""675"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""46"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x18"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""23"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x26"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""18"" document=""1"" />
        <entry offset=""0x2a"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2b"" startLine=""22"" startColumn=""11"" endLine=""22"" endColumn=""26"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" startLine=""23"" startColumn=""9"" endLine=""23"" endColumn=""16"" document=""1"" />
        <entry offset=""0x34"" startLine=""23"" startColumn=""27"" endLine=""23"" endColumn=""33"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""23"" startColumn=""18"" endLine=""23"" endColumn=""23"" document=""1"" />
        <entry offset=""0x43"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""10"" document=""1"" />
        <entry offset=""0x44"" startLine=""26"" startColumn=""9"" endLine=""26"" endColumn=""10"" document=""1"" />
        <entry offset=""0x45"" hidden=""true"" document=""1"" />
        <entry offset=""0x4b"" startLine=""23"" startColumn=""24"" endLine=""23"" endColumn=""26"" document=""1"" />
        <entry offset=""0x53"" startLine=""27"" startColumn=""14"" endLine=""27"" endColumn=""23"" document=""1"" />
        <entry offset=""0x56"" hidden=""true"" document=""1"" />
        <entry offset=""0x58"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
        <entry offset=""0x59"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" document=""1"" />
        <entry offset=""0x5a"" startLine=""27"" startColumn=""32"" endLine=""27"" endColumn=""35"" document=""1"" />
        <entry offset=""0x60"" startLine=""27"" startColumn=""25"" endLine=""27"" endColumn=""30"" document=""1"" />
        <entry offset=""0x67"" hidden=""true"" document=""1"" />
        <entry offset=""0x6b"" startLine=""31"" startColumn=""14"" endLine=""31"" endColumn=""29"" document=""1"" />
        <entry offset=""0x72"" hidden=""true"" document=""1"" />
        <entry offset=""0x74"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" document=""1"" />
        <entry offset=""0x75"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""10"" document=""1"" />
        <entry offset=""0x76"" startLine=""31"" startColumn=""31"" endLine=""31"" endColumn=""37"" document=""1"" />
        <entry offset=""0x7c"" hidden=""true"" document=""1"" />
        <entry offset=""0x80"" startLine=""35"" startColumn=""9"" endLine=""35"" endColumn=""21"" document=""1"" />
        <entry offset=""0x86"" hidden=""true"" document=""1"" />
        <entry offset=""0x8a"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8b"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8c"" hidden=""true"" document=""1"" />
        <entry offset=""0x8e"" startLine=""40"" startColumn=""9"" endLine=""40"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8f"" startLine=""42"" startColumn=""9"" endLine=""42"" endColumn=""10"" document=""1"" />
        <entry offset=""0x90"" hidden=""true"" document=""1"" />
        <entry offset=""0x91"" startLine=""44"" startColumn=""9"" endLine=""44"" endColumn=""10"" document=""1"" />
        <entry offset=""0x92"" startLine=""46"" startColumn=""13"" endLine=""46"" endColumn=""35"" document=""1"" />
        <entry offset=""0x98"" startLine=""48"" startColumn=""9"" endLine=""48"" endColumn=""14"" document=""1"" />
        <entry offset=""0x99"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""10"" document=""1"" />
        <entry offset=""0x9a"" startLine=""51"" startColumn=""9"" endLine=""51"" endColumn=""10"" document=""1"" />
        <entry offset=""0x9d"" hidden=""true"" document=""1"" />
        <entry offset=""0x9f"" startLine=""53"" startColumn=""9"" endLine=""53"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa0"" startLine=""55"" startColumn=""9"" endLine=""55"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa2"" startLine=""58"" startColumn=""5"" endLine=""58"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa3"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0xa3"" attributes=""0"" />
        <local name=""arrInt"" il_index=""1"" il_start=""0x0"" il_end=""0xa3"" attributes=""0"" />
        <constant name=""scores"" value=""null"" type=""Object"" />
        <constant name=""arrDynamic"" value=""null"" type=""Object"" />
        <constant name=""scoreQuery1"" value=""null"" signature=""System.Collections.Generic.IEnumerable`1{Object}"" />
        <constant name=""scoreQuery2"" value=""null"" type=""Object"" />
        <scope startOffset=""0x17"" endOffset=""0x1d"">
          <constant name=""dInWhile"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x25"" endOffset=""0x2b"">
          <constant name=""dInDoWhile"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x3c"" endOffset=""0x45"">
          <local name=""d"" il_index=""6"" il_start=""0x3c"" il_end=""0x45"" attributes=""0"" />
          <scope startOffset=""0x43"" endOffset=""0x45"">
            <constant name=""dInForEach"" value=""null"" type=""Object"" />
          </scope>
        </scope>
        <scope startOffset=""0x53"" endOffset=""0x6b"">
          <local name=""i"" il_index=""7"" il_start=""0x53"" il_end=""0x6b"" attributes=""0"" />
          <scope startOffset=""0x58"" endOffset=""0x5a"">
            <constant name=""dInFor"" value=""null"" type=""Object"" />
          </scope>
        </scope>
        <scope startOffset=""0x6b"" endOffset=""0x80"">
          <local name=""d"" il_index=""9"" il_start=""0x6b"" il_end=""0x80"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x8a"" endOffset=""0x8c"">
          <constant name=""dInIf"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x8e"" endOffset=""0x90"">
          <constant name=""dInElse"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x91"" endOffset=""0x98"">
          <constant name=""dInTry"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x99"" endOffset=""0x9b"">
          <constant name=""dInCatch"" value=""null"" type=""Object"" />
        </scope>
        <scope startOffset=""0x9f"" endOffset=""0xa1"">
          <constant name=""dInFinally"" value=""null"" type=""Object"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17947, "https://github.com/dotnet/roslyn/issues/17947")]
        [Fact]
        public void VariablesAndConstantsInUnreachableCode()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    void F()
    {
        dynamic v1 = 1;
        const dynamic c1 = null;

        throw null;

        dynamic v2 = 1; 
        const dynamic c2 = null;

        { 
            dynamic v3 = 1; 
            const dynamic c3 = null;
        }
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (object V_0, //v1
                object V_1, //v2
                object V_2) //v3
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  stloc.0
  IL_0008:  ldnull
  IL_0009:  throw
}");

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
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""v1"" />
          <bucket flags=""1"" slotId=""1"" localName=""v2"" />
          <bucket flags=""1"" slotId=""0"" localName=""c1"" />
          <bucket flags=""1"" slotId=""0"" localName=""c2"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""103"" />
          <slot kind=""0"" offset=""181"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""24"" document=""1"" />
        <entry offset=""0x8"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""20"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa"">
        <local name=""v1"" il_index=""0"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <local name=""v2"" il_index=""1"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <constant name=""c1"" value=""null"" type=""Object"" />
        <constant name=""c2"" value=""null"" type=""Object"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void EmitPDBVarVariableLocal()
        {
            string source = WithWindowsLineBreaks(@"
using System;
class Test
{
	public static void Main(string[] args)
	{
		dynamic d = ""1"";
		var v = d;
	}
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""d"" />
          <bucket flags=""1"" slotId=""1"" localName=""v"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""29"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""2"" endLine=""6"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""3"" endLine=""7"" endColumn=""19"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""3"" endLine=""8"" endColumn=""13"" document=""1"" />
        <entry offset=""0x9"" startLine=""9"" startColumn=""2"" endLine=""9"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa"">
        <namespace name=""System"" />
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <local name=""v"" il_index=""1"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBGenericDynamicNonLocal()
        {
            string source = WithWindowsLineBreaks(@"
using System;
class dynamic<T>
{
	public T field;
}
class Test
{
	public static void Main(string[] args)
	{
		dynamic<dynamic> obj = new dynamic<dynamic>();
		obj.field = ""1"";
	}
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""01"" slotId=""0"" localName=""obj"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""22"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""2"" endLine=""10"" endColumn=""3"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""3"" endLine=""11"" endColumn=""49"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""3"" endLine=""12"" endColumn=""19"" document=""1"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""2"" endLine=""13"" endColumn=""3"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <namespace name=""System"" />
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_1()         //With 2 normal dynamic locals
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        dynamic zzz;
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flags=""1"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""41"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_2()         //With 1 normal dynamic local and 1 containing dynamic local
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        Goo<dynamic> zzz;
    }
}

class Goo<T>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flags=""01"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""46"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_3()         //With 1 normal dynamic local and 1 containing(more than one) dynamic local
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        Goo<dynamic, Goo<dynamic,dynamic>> zzz;
    }
}

class Goo<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flags=""01011"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""68"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_4()         //With 1 normal dynamic local, 1 containing dynamic local with a normal local variable
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        int dummy = 0;
        Goo<dynamic, Goo<dynamic,dynamic>> zzz;
    }
}

class Goo<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flags=""01011"" slotId=""2"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""0"" offset=""92"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x3"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
        <local name=""dummy"" il_index=""1"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
        <local name=""zzz"" il_index=""2"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_5_Just_Long()           //Dynamic local with dynamic attribute of length 63 above which the flag is emitted empty
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> zzz;
    }
}

class F<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""010101010101010101010101010101010101010101010101010101010101011"" slotId=""0"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""361"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""zzz"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_6_Too_Long()            //The limitation of the previous testcase with dynamic attribute length 64 and not emitted
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> zzz;
    }
}

class F<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""372"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""zzz"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_7()         //Corner case dynamic locals with normal locals
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> z1;
        int dummy1 = 0;
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> z2;
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> z3;
        int dummy2 = 0;
    }
}

class F<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""372"" />
          <slot kind=""0"" offset=""389"" />
          <slot kind=""0"" offset=""771"" />
          <slot kind=""0"" offset=""1145"" />
          <slot kind=""0"" offset=""1162"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""24"" document=""1"" />
        <entry offset=""0x3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x6"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""z1"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""dummy1"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""z2"" il_index=""2"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""z3"" il_index=""3"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""dummy2"" il_index=""4"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_8_Mixed_Corner_Cases()          //Mixed case with one more limitation. If identifier length is greater than 63 then the info is not emitted
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        F<dynamic, F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,F<dynamic,dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> z3;
        dynamic www;
        dynamic length63length63length63length63length63length63length63length6;
        dynamic length64length64length64length64length64length64length64length64;
    }
}

class F<T,V>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""1"" localName=""www"" />
          <bucket flags=""1"" slotId=""2"" localName=""length63length63length63length63length63length63length63length6"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""372"" />
          <slot kind=""0"" offset=""393"" />
          <slot kind=""0"" offset=""415"" />
          <slot kind=""0"" offset=""497"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""z3"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""www"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""length63length63length63length63length63length63length63length6"" il_index=""2"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""length64length64length64length64length64length64length64length64"" il_index=""3"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_9()            //Check corner case with only corner cases
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> yes;
        F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<F<dynamic>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> no;
        dynamic www;
    }
}

class F<T>
{

}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""0000000000000000000000000000000000000000000000000000000000000001"" slotId=""0"" localName=""yes"" />
          <bucket flags=""1"" slotId=""2"" localName=""www"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""208"" />
          <slot kind=""0"" offset=""422"" />
          <slot kind=""0"" offset=""443"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""yes"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""no"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""www"" il_index=""2"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForDynamicLocals_TwoScope()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic simple;
        for(int x =0 ; x < 10 ; ++x)
        { dynamic inner; }
    }

    static void nothing(dynamic localArg)
    {
        dynamic localInner;
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""simple"" />
          <bucket flags=""1"" slotId=""2"" localName=""inner"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""44"" />
          <slot kind=""0"" offset=""84"" />
          <slot kind=""1"" offset=""36"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3"" hidden=""true"" document=""1"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""33"" endLine=""9"" endColumn=""36"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""24"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x15"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""simple"" il_index=""0"" il_start=""0x0"" il_end=""0x15"" attributes=""0"" />
        <scope startOffset=""0x1"" endOffset=""0x14"">
          <local name=""x"" il_index=""1"" il_start=""0x1"" il_end=""0x14"" attributes=""0"" />
          <scope startOffset=""0x5"" endOffset=""0x7"">
            <local name=""inner"" il_index=""2"" il_start=""0x5"" il_end=""0x7"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""Program"" name=""nothing"" parameterNames=""localArg"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""localInner"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <local name=""localInner"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(637465, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/637465")]
        [Fact]
        public void DynamicLocalOptimizedAway()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    public static void Main()
    {
        dynamic d = GetDynamic();
    }

    static dynamic GetDynamic() 
    { 
        throw null; 
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""34"" document=""1"" />
        <entry offset=""0x6"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""GetDynamic"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""20"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }
    }
}
