// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            string source = @"
class Helper
{
	int x;
	public void foo(int y){}
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
		D d4 = new D(d1.foo); 
		Helper d5 = new Helper(d1); 
		
  }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, references: new[] { CSharpRef }, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Helper"" name=""foo"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""24"" end_row=""5"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""5"" start_column=""25"" end_row=""5"" end_column=""26"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""2"" end_row=""6"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""6"" start_column=""17"" end_row=""6"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""19"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""2"" end_row=""7"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""22"" end_row=""7"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""23"" end_row=""7"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d1"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""43"" />
          <slot kind=""0"" offset=""67"" />
          <slot kind=""0"" offset=""98"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""18"" start_column=""3"" end_row=""18"" end_column=""4"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""19"" start_column=""3"" end_row=""19"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""20"" start_column=""3"" end_row=""20"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""21"" start_column=""3"" end_row=""21"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xb1"" start_row=""22"" start_column=""3"" end_row=""22"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x10f"" start_row=""24"" start_column=""3"" end_row=""24"" end_column=""4"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d2"" il_index=""1"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d4"" il_index=""2"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
        <local name=""d5"" il_index=""3"" il_start=""0x0"" il_end=""0x110"" attributes=""0"" />
      </locals>
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
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""2"" flags=""01"" slotId=""0"" localName=""arrDynamic"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""3"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""6"" offset=""58"" />
          <slot kind=""8"" offset=""58"" />
          <slot kind=""0"" offset=""58"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""3"" end_row=""7"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""8"" start_column=""31"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x15"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""8"" start_column=""18"" end_row=""8"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""8"" start_column=""28"" end_row=""8"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""arrDynamic"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
        <local name=""d"" il_index=""3"" il_start=""0x17"" il_end=""0x1d"" attributes=""0"" />
      </locals>
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
        const dynamic d = null;
	}
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
	    <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""2"" end_row=""5"" end_column=""3"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""2"" end_row=""7"" end_column=""3"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <constant name=""d"" value=""0"" type=""Int32"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""d"" value=""0"" type=""Int32"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void EmitPDBDynamicArrayVariable()
        {
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""612"" bucketCount=""3"">
          <bucket flagCount=""2"" flags=""01"" slotId=""0"" localName=""arr"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""1"" localName=""arrdim"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""2"" localName=""arrobj"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""52"" />
          <slot kind=""0"" offset=""91"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""3"" end_row=""9"" end_column=""4"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""3"" end_row=""10"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""11"" start_column=""3"" end_row=""11"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""12"" start_column=""3"" end_row=""12"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""13"" start_column=""3"" end_row=""13"" end_column=""4"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""arr"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <local name=""arrdim"" il_index=""1"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <local name=""arrobj"" il_index=""2"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <local name=""arr"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <local name=""arrdim"" il_index=""1"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <local name=""arrobj"" il_index=""2"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBDynamicCollectionVariable()
        {
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""1012"" bucketCount=""5"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""l1"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""1"" localName=""l2"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""l3"" />
          <bucket flagCount=""3"" flags=""011"" slotId=""3"" localName=""d1"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""4"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""52"" />
          <slot kind=""0"" offset=""89"" />
          <slot kind=""0"" offset=""146"" />
          <slot kind=""0"" offset=""197"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""3"" end_row=""6"" end_column=""4"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""3"" end_row=""7"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""3"" end_row=""8"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""9"" start_column=""3"" end_row=""9"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""10"" start_column=""3"" end_row=""10"" end_column=""70"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""11"" start_column=""3"" end_row=""11"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""12"" start_column=""3"" end_row=""12"" end_column=""4"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""l1"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""l2"" il_index=""1"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""l3"" il_index=""2"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""d1"" il_index=""3"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""d2"" il_index=""4"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
      </locals>
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
            string source = @"
class Helper
{
	int x;
	public void foo(int y){}
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
		dynamic d3 = new D(staticObj.foo);
  }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Helper"" name=""foo"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""24"" end_row=""5"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""5"" start_column=""25"" end_row=""5"" end_column=""26"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""2"" end_row=""6"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""6"" start_column=""17"" end_row=""6"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""19"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Helper"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""2"" end_row=""7"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""22"" end_row=""7"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""23"" end_row=""7"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Helper"" methodName=""foo"" parameterNames=""y"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d1"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""d3"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""12"" />
          <slot kind=""0"" offset=""49"" />
          <slot kind=""0"" offset=""79"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""18"" start_column=""3"" end_row=""18"" end_column=""4"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""19"" start_column=""3"" end_row=""19"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""20"" start_column=""3"" end_row=""20"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""21"" start_column=""3"" end_row=""21"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""22"" start_column=""3"" end_row=""22"" end_column=""4"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""staticObj"" il_index=""0"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
        <local name=""d1"" il_index=""1"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
        <local name=""d3"" il_index=""2"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
      </locals>
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
            string source = @"
class Test
{
	public Test()
	{
		dynamic d;
	}
	public static void Main(string[] args)
	{
	}
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""13"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""2"" end_row=""4"" end_column=""15"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""5"" start_column=""2"" end_row=""5"" end_column=""3"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""2"" end_row=""7"" end_column=""3"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x7"" il_end=""0x8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <scope startOffset=""0x7"" endOffset=""0x8"">
          <local name=""d"" il_index=""0"" il_start=""0x7"" il_end=""0x8"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName="".ctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""2"" end_row=""9"" end_column=""3"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""2"" end_row=""10"" end_column=""3"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassPropertyDynamicLocals()
        {
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""get_Field"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_Field"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""get_Field"" parameterNames="""" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""get_Field"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""19"" start_column=""5"" end_row=""19"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassOverloadedOperatorDynamicLocals()
        {
            string source = @"
class Complex
{
    int real;
    int imaginery;
    public Complex(int real, int imaginery)
    {
        this.real = real;
        this.imaginery = imaginery;
    }
    public static dynamic operator +(Complex c1, Complex c2)
    {
        dynamic d = new Complex(c1.real + c2.real, c1.imaginery + c2.imaginery);
        return d;
    }
}
class Test
{

    public static void Main(string[] args)
    {
    }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Complex"" name="".ctor"" parameterNames=""real, imaginery"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Complex"" name=""op_Addition"" parameterNames=""c1, c2"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Complex"" methodName="".ctor"" parameterNames=""real, imaginery"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""81"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Complex"" methodName="".ctor"" parameterNames=""real, imaginery"" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""22"" start_column=""5"" end_row=""22"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassIndexerDynamicLocal()
        {
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""get_Item"" parameterNames=""i"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x10"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x10"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x10"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_Item"" parameterNames=""i, value"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""get_Item"" parameterNames=""i"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xd"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xd"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""get_Item"" parameterNames=""i"" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBClassEventHandlerDynamicLocal()
        {
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Sample"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""26"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""76"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cki"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1a"">
        <namespace name=""System"" />
        <local name=""cki"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Sample"" name=""myHandler"" parameterNames=""sender, args"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Sample"" methodName=""Main"" parameterNames="""" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name="".ctor"" parameterNames=""d"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d1"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""get_D"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d2"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d2"" il_index=""0"" il_start=""0x0"" il_end=""0xc"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xc"">
        <local name=""d2"" il_index=""0"" il_start=""0x0"" il_end=""0xc"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""set_D"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d3"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d3"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""d3"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""op_Addition"" parameterNames=""t1, t2"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d4"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""26"" start_column=""5"" end_row=""26"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""29"" start_column=""5"" end_row=""29"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d4"" il_index=""0"" il_start=""0x0"" il_end=""0x18"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x18"">
        <local name=""d4"" il_index=""0"" il_start=""0x0"" il_end=""0x18"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName="".ctor"" parameterNames=""d"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d5"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""31"" start_column=""5"" end_row=""31"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""33"" start_column=""5"" end_row=""33"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d5"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""14"" />
          <slot kind=""0"" offset=""43"" />
          <slot kind=""0"" offset=""102"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""50"" file_ref=""0"" />
        <entry il_offset=""0x55"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""obj1"" il_index=""0"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
        <local name=""obj2"" il_index=""1"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
        <local name=""obj3"" il_index=""2"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x56"">
        <namespace name=""System"" />
        <local name=""obj1"" il_index=""0"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
        <local name=""obj2"" il_index=""1"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
        <local name=""obj3"" il_index=""2"" il_start=""0x0"" il_end=""0x56"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""&lt;Main&gt;b__0"" parameterNames=""d3"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""25"" end_row=""9"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Test"" name=""&lt;Main&gt;b__2"" parameterNames=""d4"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d5"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""10"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""32"" end_row=""10"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""46"" end_row=""10"" end_column=""54"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""10"" start_column=""55"" end_row=""10"" end_column=""56"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d5"" il_index=""0"" il_start=""0x0"" il_end=""0x6"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <local name=""d5"" il_index=""0"" il_start=""0x0"" il_end=""0x6"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""&lt;Main&gt;b__4"" parameterNames=""d6"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""35"" end_row=""11"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""37"" end_row=""11"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""11"" start_column=""48"" end_row=""11"" end_column=""49"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBLangConstructsLocals2()
        {
            string source = @"
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
        for (dynamic d = ""1""; ;)
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""3"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""2812"" bucketCount=""14"">
          <bucket flagCount=""2"" flags=""01"" slotId=""2"" localName=""scores"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""3"" localName=""arrDynamic"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""4"" localName=""scoreQuery1"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""5"" localName=""scoreQuery2"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""6"" localName=""dInWhile"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""9"" localName=""dInDoWhile"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""14"" localName=""dInForEach"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""16"" localName=""dInFor"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""18"" localName=""d"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""20"" localName=""dInIf"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""21"" localName=""dInElse"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""22"" localName=""dInTry"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""23"" localName=""dInCatch"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""24"" localName=""dInFinally"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""80"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""38"" />
          <slot kind=""0"" offset=""89"" />
          <slot kind=""0"" offset=""159"" />
          <slot kind=""0"" offset=""1071"" />
          <slot kind=""0"" offset=""1163"" />
          <slot kind=""0"" offset=""261"" />
          <slot kind=""temp"" />
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
          <slot kind=""1"" offset=""672"" />
          <slot kind=""0"" offset=""717"" />
          <slot kind=""0"" offset=""781"" />
          <slot kind=""0"" offset=""846"" />
          <slot kind=""0"" offset=""948"" />
          <slot kind=""0"" offset=""1018"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""36"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""69"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""64"" file_ref=""0"" />
        <entry il_offset=""0x5b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5d"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x66"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x6d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x71"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x72"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x7a"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x7b"" start_row=""22"" start_column=""11"" end_row=""22"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x81"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""23"" start_column=""9"" end_row=""23"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x86"" start_row=""23"" start_column=""27"" end_row=""23"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x8c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8e"" start_row=""23"" start_column=""18"" end_row=""23"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x95"" start_row=""24"" start_column=""9"" end_row=""24"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x96"" start_row=""26"" start_column=""9"" end_row=""26"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x97"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x9d"" start_row=""23"" start_column=""24"" end_row=""23"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xa5"" start_row=""27"" start_column=""14"" end_row=""27"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xa8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xaa"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xab"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xac"" start_row=""27"" start_column=""32"" end_row=""27"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0xb6"" start_row=""27"" start_column=""25"" end_row=""27"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xbd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc1"" start_row=""31"" start_column=""14"" end_row=""31"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xc8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xca"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xcb"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xcc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""arrInt"" il_index=""1"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scores"" il_index=""2"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""arrDynamic"" il_index=""3"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scoreQuery1"" il_index=""4"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scoreQuery2"" il_index=""5"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""dInWhile"" il_index=""6"" il_start=""0x5d"" il_end=""0x67"" attributes=""0"" />
        <local name=""dInDoWhile"" il_index=""9"" il_start=""0x71"" il_end=""0x7b"" attributes=""0"" />
        <local name=""d"" il_index=""13"" il_start=""0x8e"" il_end=""0x97"" attributes=""0"" />
        <local name=""dInForEach"" il_index=""14"" il_start=""0x95"" il_end=""0x97"" attributes=""0"" />
        <local name=""i"" il_index=""15"" il_start=""0xa5"" il_end=""0xc1"" attributes=""0"" />
        <local name=""dInFor"" il_index=""16"" il_start=""0xaa"" il_end=""0xac"" attributes=""0"" />
        <local name=""d"" il_index=""18"" il_start=""0xc1"" il_end=""0xce"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xce"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <local name=""d1"" il_index=""0"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""arrInt"" il_index=""1"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scores"" il_index=""2"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""arrDynamic"" il_index=""3"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scoreQuery1"" il_index=""4"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <local name=""scoreQuery2"" il_index=""5"" il_start=""0x0"" il_end=""0xce"" attributes=""0"" />
        <scope startOffset=""0x5d"" endOffset=""0x67"">
          <local name=""dInWhile"" il_index=""6"" il_start=""0x5d"" il_end=""0x67"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x71"" endOffset=""0x7b"">
          <local name=""dInDoWhile"" il_index=""9"" il_start=""0x71"" il_end=""0x7b"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x8e"" endOffset=""0x97"">
          <local name=""d"" il_index=""13"" il_start=""0x8e"" il_end=""0x97"" attributes=""0"" />
          <scope startOffset=""0x95"" endOffset=""0x97"">
            <local name=""dInForEach"" il_index=""14"" il_start=""0x95"" il_end=""0x97"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0xa5"" endOffset=""0xc1"">
          <local name=""i"" il_index=""15"" il_start=""0xa5"" il_end=""0xc1"" attributes=""0"" />
          <scope startOffset=""0xaa"" endOffset=""0xac"">
            <local name=""dInFor"" il_index=""16"" il_start=""0xaa"" il_end=""0xac"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0xc1"" endOffset=""0xce"">
          <local name=""d"" il_index=""18"" il_start=""0xc1"" il_end=""0xce"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test"" name=""&lt;Main&gt;b__0"" parameterNames=""score"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""58"" start_column=""20"" end_row=""58"" end_column=""25"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Test"" name=""&lt;Main&gt;b__2"" parameterNames=""score"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""61"" start_column=""20"" end_row=""61"" end_column=""25"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void EmitPDBVarVariableLocal()
        {
            string source = @"
using System;
class Test
{
	public static void Main(string[] args)
	{
		dynamic d = ""1"";
		var v = d;
	}
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""d"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""v"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""29"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""2"" end_row=""6"" end_column=""3"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""3"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""3"" end_row=""8"" end_column=""13"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""9"" start_column=""2"" end_row=""9"" end_column=""3"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <local name=""v"" il_index=""1"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </locals>
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
            string source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""2"" flags=""01"" slotId=""0"" localName=""obj"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""22"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""2"" end_row=""10"" end_column=""3"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""3"" end_row=""11"" end_column=""49"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""3"" end_row=""12"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""13"" start_column=""2"" end_row=""13"" end_column=""3"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
      </locals>
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
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""41"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        Foo<dynamic> zzz;
    }
}

class Foo<T>
{

}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flagCount=""2"" flags=""01"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""46"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        Foo<dynamic, Foo<dynamic,dynamic>> zzz;
    }
}

class Foo<T,V>
{

}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flagCount=""5"" flags=""01011"" slotId=""1"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""68"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""zzz"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        dynamic yyy;
        int dummy = 0;
        Foo<dynamic, Foo<dynamic,dynamic>> zzz;
    }
}

class Foo<T,V>
{

}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""yyy"" />
          <bucket flagCount=""5"" flags=""01011"" slotId=""2"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""0"" offset=""92"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""yyy"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
        <local name=""dummy"" il_index=""1"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
        <local name=""zzz"" il_index=""2"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </locals>
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
        public void EmitPDBForDynamicLocals_5_Just_Long()           //Dynamic local with dynamic attirbute of length 63 above which the flag is emitted empty
        {
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""63"" flags=""010101010101010101010101010101010101010101010101010101010101011"" slotId=""0"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""361"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""zzz"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""0"" flags="""" slotId=""0"" localName=""zzz"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""372"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""zzz"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
        public void EmitPDBForDynamicLocals_7()         //Cornercase dynamic locals with normal locals
        {
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""612"" bucketCount=""3"">
          <bucket flagCount=""0"" flags="""" slotId=""0"" localName=""z1"" />
          <bucket flagCount=""0"" flags="""" slotId=""2"" localName=""z2"" />
          <bucket flagCount=""0"" flags="""" slotId=""3"" localName=""z3"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""24"">
          <slot kind=""0"" offset=""372"" />
          <slot kind=""0"" offset=""389"" />
          <slot kind=""0"" offset=""771"" />
          <slot kind=""0"" offset=""1145"" />
          <slot kind=""0"" offset=""1162"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""z1"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""dummy1"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""z2"" il_index=""2"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""z3"" il_index=""3"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""dummy2"" il_index=""4"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
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
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""812"" bucketCount=""4"">
          <bucket flagCount=""0"" flags="""" slotId=""0"" localName=""z3"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""www"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""length63length63length63length63length63length63length63length6"" />
          <bucket flagCount=""0"" flags="""" slotId=""0"" localName="""" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""372"" />
          <slot kind=""0"" offset=""393"" />
          <slot kind=""0"" offset=""415"" />
          <slot kind=""0"" offset=""497"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""z3"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""www"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""length63length63length63length63length63length63length63length6"" il_index=""2"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""length64length64length64length64length64length64length64length64"" il_index=""3"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""612"" bucketCount=""3"">
          <bucket flagCount=""64"" flags=""0000000000000000000000000000000000000000000000000000000000000001"" slotId=""0"" localName=""yes"" />
          <bucket flagCount=""0"" flags="""" slotId=""1"" localName=""no"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""www"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""208"" />
          <slot kind=""0"" offset=""422"" />
          <slot kind=""0"" offset=""443"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""yes"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""no"" il_index=""1"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
        <local name=""www"" il_index=""2"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
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
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""412"" bucketCount=""2"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""simple"" />
          <bucket flagCount=""1"" flags=""1"" slotId=""2"" localName=""inner"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""44"" />
          <slot kind=""0"" offset=""84"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""36"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""9"" start_column=""33"" end_row=""9"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""9"" start_column=""24"" end_row=""9"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x14"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""simple"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <local name=""x"" il_index=""1"" il_start=""0x1"" il_end=""0x18"" attributes=""0"" />
        <local name=""inner"" il_index=""2"" il_start=""0x5"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""simple"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
        <scope startOffset=""0x1"" endOffset=""0x18"">
          <local name=""x"" il_index=""1"" il_start=""0x1"" il_end=""0x18"" attributes=""0"" />
          <scope startOffset=""0x5"" endOffset=""0x7"">
            <local name=""inner"" il_index=""2"" il_start=""0x5"" il_end=""0x7"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""Program"" name=""nothing"" parameterNames=""localArg"">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""localInner"" />
        </dynamicLocals>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""localInner"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <local name=""localInner"" il_index=""0"" il_start=""0x0"" il_end=""0x2"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(637465, "DevDiv")]
        [Fact]
        public void DynamicLocalOptimizedAway()
        {
            string source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""GetDynamic"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""20"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>
");
        }
    }

}