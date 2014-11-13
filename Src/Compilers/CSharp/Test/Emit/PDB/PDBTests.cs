// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTests : CSharpPDBTestBase
    {
        #region Method Bodies

        [Fact]
        public void TestBasic()
        {
            var source = @"
class Program
{
    Program() { }

    static void Main(string[] args)
    {
        Program p = new Program();
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName="".ctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""p"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <local name=""p"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestSimpleLocals()
        {
            var source = @"
class C 
{ 
    void Method()
    {   //local at method scope
        object version = 6;
        System.Console.WriteLine(""version {0}"", version);
        {
            //a scope that defines no locals
            {
                //a nested local
                object foob = 1;
                System.Console.WriteLine(""foob {0}"", foob);
            }
            {
                //a nested local
                int foob1 = 1;
                System.Console.WriteLine(""foob1 {0}"", foob1);
            }
            System.Console.WriteLine(""Eva"");
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Method"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""44"" />
          <slot kind=""0"" offset=""246"" />
          <slot kind=""0"" offset=""402"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""12"" start_column=""17"" end_row=""12"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""13"" start_column=""17"" end_row=""13"" end_column=""60"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""17"" start_column=""17"" end_row=""17"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""18"" start_column=""17"" end_row=""18"" end_column=""62"" file_ref=""0"" />
        <entry il_offset=""0x3e"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""20"" start_column=""13"" end_row=""20"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x4a"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4b"" start_row=""22"" start_column=""5"" end_row=""22"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""version"" il_index=""0"" il_start=""0x0"" il_end=""0x4c"" attributes=""0"" />
        <local name=""foob"" il_index=""1"" il_start=""0x15"" il_end=""0x2a"" attributes=""0"" />
        <local name=""foob1"" il_index=""2"" il_start=""0x2a"" il_end=""0x3f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4c"">
        <local name=""version"" il_index=""0"" il_start=""0x0"" il_end=""0x4c"" attributes=""0"" />
        <scope startOffset=""0x15"" endOffset=""0x2a"">
          <local name=""foob"" il_index=""1"" il_start=""0x15"" il_end=""0x2a"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x2a"" endOffset=""0x3f"">
          <local name=""foob1"" il_index=""2"" il_start=""0x2a"" il_end=""0x3f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        /// <summary>
        /// Although the debugging info attached to DebuggerHidden method is not used by the debugger 
        /// (the debugger doesn't ever stop in the method) Dev11 emits the info and so do we.
        /// 
        /// StepThrough method needs the information if JustMyCode is disabled and a breakpoint is set within the method.
        /// NonUserCode method needs the information if JustMyCode is disabled.
        /// 
        /// It's up to the tool that consumes the debugging information, not the compiler to decide whether to ignore the info or not.
        /// BTW, the information can actually be retrieved at runtime from the PDB file via Reflection StackTrace.
        /// </summary>
        [Fact]
        public void MethodsWithDebuggerAttributes()
        {
            var source = @"
using System;
using System.Diagnostics;

class Program
{
    [DebuggerHidden]
    static void Hidden()
    {
        int x = 1;
        Console.WriteLine(x);
    }

    [DebuggerStepThrough]
    static void StepThrough()
    {
        int y = 1;
        Console.WriteLine(y);
    }

    [DebuggerNonUserCode]
    static void NonUserCode()
    {
        int z = 1;
        Console.WriteLine(z);
    }
}
";     
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Hidden"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <namespace name=""System"" />
        <namespace name=""System.Diagnostics"" />
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""StepThrough"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Hidden"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""19"" start_column=""5"" end_row=""19"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""y"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""y"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""NonUserCode"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Hidden"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""24"" start_column=""9"" end_row=""24"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""25"" start_column=""9"" end_row=""25"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""26"" start_column=""5"" end_row=""26"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""z"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""z"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        /// <summary>
        /// If a synthesized method contains any user code,
        /// the method must have a sequence point at
        /// offset 0 for correct stepping behavior.
        /// </summary>
        [WorkItem(804681, "DevDiv")]
        [Fact]
        public void SequencePointAtOffset0()
        {
            string source =
@"using System;
class C
{
    static Func<object, int> F = x =>
    {
        Func<object, int> f = o => 1;
        Func<Func<object, int>, Func<object, int>> g = h => y => h(y);
        return g(f)(null);
    };
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <methods>
    <method containingType=""C"" name="".cctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""9"" end_column=""7"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0"" name=""&lt;_cctor&gt;b__6"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""66"" end_row=""7"" end_column=""70"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1"" name=""&lt;_cctor&gt;b__2"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""93"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x51"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1"" name=""&lt;_cctor&gt;b__3"" parameterNames=""o"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""36"" end_row=""6"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1"" name=""&lt;_cctor&gt;b__5"" parameterNames=""h"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""7"" start_column=""61"" end_row=""7"" end_column=""70"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region IfStatement

        [Fact]
        public void IfStatement()
        {
            var source = @"
class C 
{ 
    void Method()
    {   
        bool b = true;
        if (b)
        {
            string s = ""true"";
            System.Console.WriteLine(s);
        } 
        else 
        {
            string s = ""false"";
            int i = 1;

            while (i < 100)
            {
                int j = i, k = 1;
                System.Console.WriteLine(j);  
                i = j + k;                
            }         
            
            i = i + 1;
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Method"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""32"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""1"" offset=""38"" />
          <slot kind=""0"" offset=""76"" />
          <slot kind=""0"" offset=""188"" />
          <slot kind=""0"" offset=""218"" />
          <slot kind=""0"" offset=""292"" />
          <slot kind=""0"" offset=""299"" />
          <slot kind=""1"" offset=""240"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""23"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""15"" file_ref=""0"" />
        <entry il_offset=""0x5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x23"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""19"" start_column=""17"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""19"" start_column=""28"" end_row=""19"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""20"" start_column=""17"" end_row=""20"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x35"" start_row=""21"" start_column=""17"" end_row=""21"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x45"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x49"" start_row=""24"" start_column=""13"" end_row=""24"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x4f"" start_row=""25"" start_column=""9"" end_row=""25"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x50"" start_row=""26"" start_column=""5"" end_row=""26"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""b"" il_index=""0"" il_start=""0x0"" il_end=""0x51"" attributes=""0"" />
        <local name=""s"" il_index=""2"" il_start=""0x8"" il_end=""0x17"" attributes=""0"" />
        <local name=""s"" il_index=""3"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
        <local name=""i"" il_index=""4"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
        <local name=""j"" il_index=""5"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
        <local name=""k"" il_index=""6"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x51"">
        <local name=""b"" il_index=""0"" il_start=""0x0"" il_end=""0x51"" attributes=""0"" />
        <scope startOffset=""0x8"" endOffset=""0x17"">
          <local name=""s"" il_index=""2"" il_start=""0x8"" il_end=""0x17"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x19"" endOffset=""0x50"">
          <local name=""s"" il_index=""3"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
          <local name=""i"" il_index=""4"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
          <scope startOffset=""0x25"" endOffset=""0x3d"">
            <local name=""j"" il_index=""5"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
            <local name=""k"" il_index=""6"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region WhileStatement

        [WorkItem(538299, "DevDiv")]
        [Fact]
        public void WhileStatement()
        {
            var source = @"using System;

public class SeqPointForWhile
{
    public static void Main()
    {
        SeqPointForWhile obj = new SeqPointForWhile();
        obj.While(234);
    }

    int field;
    public void While(int p)
    {
        while (p > 0) // SeqPt should be generated at the end of loop
        {
            p = (int)(p / 2);

            if (p > 100)
            {
                continue;
            }
            else if (p > 10)
            {
                int x = p;
                field = x;
            }
            else
            {
                int x = p;
                Console.WriteLine(x);
                break;
            }
        }
        field = -1;
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);

            // Offset 0x01 should be:
            //  <entry il_offset=""0x1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
            // Move original offset 0x01 to 0x33
            //  <entry il_offset=""0x33"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""22"" file_ref=""0"" />
            // 
            // Note: 16707566 == 0x00FEEFEE
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x10"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""13"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""22"" start_column=""18"" end_row=""22"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""29"" start_column=""17"" end_row=""29"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""30"" start_column=""17"" end_row=""30"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""31"" start_column=""17"" end_row=""31"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x2f"" start_row=""35"" start_column=""5"" end_row=""35"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x11"" il_end=""0x1a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x30"">
        <scope startOffset=""0x11"" endOffset=""0x1a"">
          <local name=""x"" il_index=""0"" il_start=""0x11"" il_end=""0x1a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region ForStatement

        [Fact]
        public void ForStatement1()
        {
            var source = @"
class C
{
    static bool F(int i) { return true; }
    static void G(int i) { }
    
    static void M()
    {
        for (int i = 1; F(i); G(i))
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames=""i"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""20"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""14"" end_row=""9"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""9"" start_column=""31"" end_row=""9"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""9"" start_column=""25"" end_row=""9"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""i"" il_index=""0"" il_start=""0x1"" il_end=""0x1f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x20"">
        <scope startOffset=""0x1"" endOffset=""0x1f"">
          <local name=""i"" il_index=""0"" il_start=""0x1"" il_end=""0x1f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForStatement2()
        {
            var source = @"
class C
{
    static void M()
    {
        for (;;)
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForStatement3()
        {
            var source = @"
class C
{
    static void M()
    {
        int i = 0;
        for (;;i++)
        {
            System.Console.WriteLine(i);
        }
    }
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""7"" start_column=""16"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x14"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""i"" il_index=""0"" il_start=""0x0"" il_end=""0x16"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <local name=""i"" il_index=""0"" il_start=""0x0"" il_end=""0x16"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        #endregion

        #region ForEachStatement

        [Fact]
        public void ForEachStatement_String()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var c in ""hello"")
        {
            System.Console.WriteLine(c);
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) '"hello"'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) Hidden index increment.
            // 10) 'in'
            // 11) Close brace at end of method

            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""27"" end_row=""6"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x16"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForEachStatement_Array()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var x in new int[2])
        {
            System.Console.WriteLine(x);
        }
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new int[2]'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) Hidden index increment.
            // 10) 'in'
            // 11) Close brace at end of method

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""6"" start_column=""27"" end_row=""6"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xb"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""2"" il_start=""0xd"" il_end=""0x1a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x25"">
        <scope startOffset=""0xd"" endOffset=""0x1a"">
          <local name=""x"" il_index=""2"" il_start=""0xd"" il_end=""0x1a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(544937, "DevDiv")]
        [Fact]
        public void ForEachStatement_MultiDimensionalArray()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var x in new int[2, 3])
        {
            System.Console.WriteLine(x);
        }
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new int[2, 3]'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) 'in'
            // 10) Close brace at end of method

            v.VerifyIL("C.Main", @"
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (int[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldc.i4.2
  IL_0003:  ldc.i4.3
  IL_0004:  newobj     ""int[*,*]..ctor""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0011:  stloc.1
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0019:  stloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0021:  stloc.3
 ~IL_0022:  br.s       IL_0053
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.1
  IL_0026:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002b:  stloc.s    V_4
 ~IL_002d:  br.s       IL_004a
 -IL_002f:  ldloc.0
  IL_0030:  ldloc.3
  IL_0031:  ldloc.s    V_4
  IL_0033:  call       ""int[*,*].Get""
  IL_0038:  stloc.s    V_5
 -IL_003a:  nop
 -IL_003b:  ldloc.s    V_5
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  nop
 -IL_0043:  nop
 ~IL_0044:  ldloc.s    V_4
  IL_0046:  ldc.i4.1
  IL_0047:  add
  IL_0048:  stloc.s    V_4
 -IL_004a:  ldloc.s    V_4
  IL_004c:  ldloc.2
  IL_004d:  ble.s      IL_002f
 ~IL_004f:  ldloc.3
  IL_0050:  ldc.i4.1
  IL_0051:  add
  IL_0052:  stloc.3
 -IL_0053:  ldloc.3
  IL_0054:  ldloc.1
  IL_0055:  ble.s      IL_0024
 -IL_0057:  ret
}
", sequencePoints: "C.Main");
        }

        [WorkItem(544937, "DevDiv")]
        [Fact]
        public void ForEachStatement_MultiDimensionalArrayBreakAndContinue()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        int[, ,] array = new[,,]
        {
            { {1, 2}, {3, 4} },
            { {5, 6}, {7, 8} },
        };

        foreach (int i in array)
        {
            if (i % 2 == 1) continue;
            if (i > 4) break;
            Console.WriteLine(i);
        }
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // Stepping:
            //   After "continue", step to "in".
            //   After "break", step to first sequence point following loop body (in this case, method close brace).
            v.VerifyIL("C.Main", @"
{
  // Code size      169 (0xa9)
  .maxstack  4
  .locals init (int[,,] V_0, //array
                int[,,] V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5,
                int V_6,
                int V_7,
                int V_8, //i
                bool V_9,
                bool V_10)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.2
  IL_0002:  ldc.i4.2
  IL_0003:  ldc.i4.2
  IL_0004:  newobj     ""int[*,*,*]..ctor""
  IL_0009:  dup
  IL_000a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.$$method0x6000001-EB196F988F4F427D318CA25B68671CF3A4510012""
  IL_000f:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0014:  stloc.0
 -IL_0015:  nop
 -IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.0
  IL_001a:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_001f:  stloc.2
  IL_0020:  ldloc.1
  IL_0021:  ldc.i4.1
  IL_0022:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0027:  stloc.3
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4.2
  IL_002a:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_002f:  stloc.s    V_4
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.0
  IL_0033:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0038:  stloc.s    V_5
 ~IL_003a:  br.s       IL_00a3
  IL_003c:  ldloc.1
  IL_003d:  ldc.i4.1
  IL_003e:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0043:  stloc.s    V_6
 ~IL_0045:  br.s       IL_0098
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.2
  IL_0049:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_004e:  stloc.s    V_7
 ~IL_0050:  br.s       IL_008c
 -IL_0052:  ldloc.1
  IL_0053:  ldloc.s    V_5
  IL_0055:  ldloc.s    V_6
  IL_0057:  ldloc.s    V_7
  IL_0059:  call       ""int[*,*,*].Get""
  IL_005e:  stloc.s    V_8
 -IL_0060:  nop
 -IL_0061:  ldloc.s    V_8
  IL_0063:  ldc.i4.2
  IL_0064:  rem
  IL_0065:  ldc.i4.1
  IL_0066:  ceq
  IL_0068:  stloc.s    V_9
 ~IL_006a:  ldloc.s    V_9
  IL_006c:  brfalse.s  IL_0070
 -IL_006e:  br.s       IL_0086
 -IL_0070:  ldloc.s    V_8
  IL_0072:  ldc.i4.4
  IL_0073:  cgt
  IL_0075:  stloc.s    V_10
 ~IL_0077:  ldloc.s    V_10
  IL_0079:  brfalse.s  IL_007d
 -IL_007b:  br.s       IL_00a8
 -IL_007d:  ldloc.s    V_8
  IL_007f:  call       ""void System.Console.WriteLine(int)""
  IL_0084:  nop
 -IL_0085:  nop
 ~IL_0086:  ldloc.s    V_7
  IL_0088:  ldc.i4.1
  IL_0089:  add
  IL_008a:  stloc.s    V_7
 -IL_008c:  ldloc.s    V_7
  IL_008e:  ldloc.s    V_4
  IL_0090:  ble.s      IL_0052
 ~IL_0092:  ldloc.s    V_6
  IL_0094:  ldc.i4.1
  IL_0095:  add
  IL_0096:  stloc.s    V_6
 -IL_0098:  ldloc.s    V_6
  IL_009a:  ldloc.3
  IL_009b:  ble.s      IL_0047
 ~IL_009d:  ldloc.s    V_5
  IL_009f:  ldc.i4.1
  IL_00a0:  add
  IL_00a1:  stloc.s    V_5
 -IL_00a3:  ldloc.s    V_5
  IL_00a5:  ldloc.2
  IL_00a6:  ble.s      IL_003c
 -IL_00a8:  ret
}
", sequencePoints: "C.Main");
        }

        [Fact]
        public void ForEachStatement_Enumerator()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var x in new System.Collections.Generic.List<int>())
        {
            System.Console.WriteLine(x);
        }
    }
}
";

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new System.Collections.Generic.List<int>()'
            // 4) Hidden initial jump (of while loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) 'in'
            // 10) hidden point in Finally
            // 11) Close brace at end of method

            v.VerifyIL("C.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (System.Collections.Generic.List<int>.Enumerator V_0,
                int V_1) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0007:  call       ""System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()""
  IL_000c:  stloc.0
  .try
  {
   ~IL_000d:  br.s       IL_0020
   -IL_000f:  ldloca.s   V_0
    IL_0011:  call       ""int System.Collections.Generic.List<int>.Enumerator.Current.get""
    IL_0016:  stloc.1
   -IL_0017:  nop
   -IL_0018:  ldloc.1
    IL_0019:  call       ""void System.Console.WriteLine(int)""
    IL_001e:  nop
   -IL_001f:  nop
   -IL_0020:  ldloca.s   V_0
    IL_0022:  call       ""bool System.Collections.Generic.List<int>.Enumerator.MoveNext()""
    IL_0027:  brtrue.s   IL_000f
    IL_0029:  leave.s    IL_003a
  }
  finally
  {
   ~IL_002b:  ldloca.s   V_0
    IL_002d:  constrained. ""System.Collections.Generic.List<int>.Enumerator""
    IL_0033:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0038:  nop
    IL_0039:  endfinally
  }
 -IL_003a:  ret
}
", sequencePoints: "C.Main");
        }

        [WorkItem(718501, "DevDiv")]
        [Fact]
        public void ForEachNops()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static List<List<int>> l = new List<List<int>>();

    static void Main(string[] args)
        {
            foreach (var i in l.AsEnumerable())
            {
                switch (i.Count)
                {
                    case 1:
                        break;

                    default:
                        if (i.Count != 0)
                        {
                        }

                        break;
                }
            }
        }
}
";
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName="".cctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""5"" offset=""15"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""83"" />
          <slot kind=""1"" offset=""237"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""18"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""12"" start_column=""31"" end_row=""12"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x12"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""12"" start_column=""22"" end_row=""12"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""14"" start_column=""17"" end_row=""14"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x23"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""17"" start_column=""25"" end_row=""17"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""20"" start_column=""25"" end_row=""20"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x35"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x38"" start_row=""21"" start_column=""25"" end_row=""21"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""22"" start_column=""25"" end_row=""22"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x3a"" start_row=""24"" start_column=""25"" end_row=""24"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""12"" start_column=""28"" end_row=""12"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x47"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x52"" start_row=""27"" start_column=""9"" end_row=""27"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x3d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <scope startOffset=""0x14"" endOffset=""0x3d"">
          <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x3d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region DoStatement

        [Fact]
        public void DoStatement()
        {
            var source = @"using System;

public class SeqPointForWhile
{
    public static void Main()
    {
        SeqPointForWhile obj = new SeqPointForWhile();
        obj.While(234);
    }

    int field;
    public void While(int p)
    {
        do
        {
            p = (int)(p / 2);

            if (p > 100)
            {
                continue;
            }
            else if (p > 10)
            {
                field = 1;
            }
            else
            {
                break;
            }
        } while (p > 0); // SeqPt should be generated for [while (p > 0);]

        field = -1;
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""28"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""55"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <namespace name=""System"" />
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""1"" offset=""71"" />
          <slot kind=""1"" offset=""159"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""19"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x11"" start_row=""20"" start_column=""17"" end_row=""20"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""22"" start_column=""18"" end_row=""22"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x19"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""25"" start_column=""13"" end_row=""25"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""28"" start_column=""17"" end_row=""28"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""30"" start_column=""11"" end_row=""30"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x30"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x3a"" start_row=""33"" start_column=""5"" end_row=""33"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Constructor

        [WorkItem(538317, "DevDiv")]
        [Fact]
        public void ConstructorSequencePoints1()
        {
            var source = @"namespace NS
{
    public class MyClass
    {
        int intTest;
        public MyClass()
        {
            intTest = 123;
        }

        public MyClass(params int[] values)
        {
            intTest = values[0] + values[1] + values[2];
        }

        public static int Main()
        {
            int intI = 1, intJ = 8;
            int intK = 3;

            // Can't step into Ctor
            MyClass mc = new MyClass();

            // Can't step into Ctor
            mc = new MyClass(intI, intJ, intK);

            return mc.intTest - 12;
        }
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // Dev10 vs. Roslyn
            // 
            // Default Ctor (no param)
            //    Dev10                                                 Roslyn
            // ======================================================================================
            //  Code size       18 (0x12)                               // Code size       16 (0x10)
            //  .maxstack  8                                            .maxstack  8
            //* IL_0000:  ldarg.0                                      *IL_0000:  ldarg.0
            //  IL_0001:  call                                          IL_0001:  callvirt
            //      instance void [mscorlib]System.Object::.ctor()         instance void [mscorlib]System.Object::.ctor()
            //  IL_0006:  nop                                          *IL_0006:  nop
            //* IL_0007:  nop
            //* IL_0008:  ldarg.0                                      *IL_0007:  ldarg.0
            //  IL_0009:  ldc.i4.s   123                                IL_0008:  ldc.i4.s   123
            //  IL_000b:  stfld      int32 NS.MyClass::intTest          IL_000a:  stfld      int32 NS.MyClass::intTest
            //  IL_0010:  nop                                           
            //* IL_0011:  ret                                          *IL_000f:  ret
            //  -----------------------------------------------------------------------------------------
            //  SeqPoint: 0, 7 ,8, 0x10                                 0, 6, 7, 0xf

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""NS.MyClass"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""NS.MyClass"" name="".ctor"" parameterNames=""values"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""NS.MyClass"" methodName="".ctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""44"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""57"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""NS.MyClass"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""NS.MyClass"" methodName="".ctor"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""56"" />
          <slot kind=""0"" offset=""126"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""18"" start_column=""27"" end_row=""18"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""25"" start_column=""13"" end_row=""25"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""intI"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intJ"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intK"" il_index=""2"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""mc"" il_index=""3"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""intI"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intJ"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intK"" il_index=""2"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""mc"" il_index=""3"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ConstructorSequencePoints2()
        {
            TestSequencePoints(
@"using System;

class D
{
    public D() : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

class D
{
    static D()
    [|{|]
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;
class A : Attribute {}
class D
{
    [A]
    public D() : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;
class A : Attribute {}
class D
{
    [A]
    public D() 
        : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

class A : Attribute {}
class C { }
class D
{
    [A]
    [|public D()|]
    {
    }
}", TestOptions.DebugDll);
        }

        #endregion

        #region Destructor

        [Fact]
        public void Destructors()
        {
            var source = @"
using System;

public class Base
{
    ~Base()
    {
        Console.WriteLine();
    }
}

public class Derived : Base
{
    ~Derived()
    {
        Console.WriteLine();
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Base"" name=""Finalize"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Derived"" name=""Finalize"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Base"" methodName=""Finalize"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Lambdas

        [WorkItem(539898, "DevDiv")]
        [Fact]
        public void LambdaSequencePoints_Body()
        {
            var source = @"using System;
delegate void D();
class C
{
    public static void Main()
    {
        D d = () => Console.Write(1);
        d();
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""13"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""13"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x29"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x29"">
        <namespace name=""System"" />
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x29"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0"" name=""&lt;Main&gt;b__1"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""21"" end_row=""7"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void Lambdas_Nested()
        {
            var source = @"using System;
class Test
{
    public static int Main()
    {
         if (M(1) != 10) 
            return 1;
        return 0;
    }

    static public int M(int p)
    {
        Func<int, int> f1 = delegate(int x)
        {
            int q = 2;
            Func<int, int> f2 = (y) => 
            {
                return p + q + x + y;
            };
            return f2(3);
        };
        return f1(4);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""Test"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""1"" offset=""12"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""10"" end_row=""6"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""7"" start_column=""13"" end_row=""7"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""M"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""26"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""13"" start_column=""9"" end_row=""21"" end_column=""11"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;M&gt;b__2"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""54"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""16"" start_column=""13"" end_row=""19"" end_column=""15"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""20"" start_column=""13"" end_row=""20"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1"" name=""&lt;M&gt;b__3"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""18"" start_column=""17"" end_row=""18"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""14"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void Lambdas_InitialSequencePoints()
        {
            var source = @"
class Test
{
    void Foo(int p)
    {
        System.Func<int> f1 = () => p;
        f1();
    }
}
";
            // Specifically note the sequence points at 0x0 in Test.Main, Test.M, and the lambda bodies.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Foo"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""28"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;Foo&gt;b__1"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Foo"" parameterNames=""p"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""37"" end_row=""6"" end_column=""38"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void Lambdas_Nested_InitialSequencePoints()
        {
            var source = @"
using System;
class Test
{
    public static int Main()
    {
        if (M(1) != 10) // can't step into M() at all
            return 1;
        return 0;
    }

    static public int M(int p)
    {
        Func<int, int> f1 = delegate(int x)
        {
            int q = 2;
            Func<int, int> f2 = (y) => { return p + q + x + y; };
            return f2(3);
        };
        return f1(4);
    }
}
";
            // Specifically note the sequence points at 0x0 in Test.Main, Test.M, and the lambda bodies.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""1"" offset=""11"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""M"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""26"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""14"" start_column=""9"" end_row=""19"" end_column=""11"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;M&gt;b__2"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""54"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""66"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1"" name=""&lt;M&gt;b__3"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""40"" end_row=""17"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""42"" end_row=""17"" end_column=""63"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""17"" start_column=""64"" end_row=""17"" end_column=""65"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Field and Property Initializers

        [Fact]
        public void TestPartialClassFieldInitializers()
        {
            var text1 = @"
public partial class C
{
    int x = 1;
}
";

            var text2 = @"
public partial class C
{
    int y = 1;

    static void Main()
    {
        C c = new C();
    }
}
";
            //Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            //loads the assembly into the ReflectionOnlyLoadFrom context.
            //So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateCompilationWithMscorlib(new SyntaxTree[] { Parse(text1, "a.cs"), Parse(text2, "b.cs") });

            compilation.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""BB, 7A, A6, D2, B2, 32, 59, 43, 8C, 98, 7F, E1, 98, 8D, F0, 94, 68, E9, EB, 80, "" />
    <file id=""2"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""B4, EA, 18, 73, D2,  E, 7F, 15, 51, 4C, 68, 86, 40, DF, E3, C3, 97, 9D, F6, B7, "" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""15"" file_ref=""2"" />
        <entry il_offset=""0x7"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""15"" file_ref=""1"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestPartialClassFieldInitializersWithLineDirectives()
        {
            var text1 = @"
using System;
public partial class C
{
    int x = 1;
#line 12 ""foo.cs""
    int z = Math.Abs(-3);
    int w = Math.Abs(4);
#line 17 ""bar.cs""
    double zed = Math.Sin(5);
}

#pragma checksum ""mah.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

";

            var text2 = @"
using System;
public partial class C
{
    int y = 1;
    int x2 = 1;
#line 12 ""foo2.cs""
    int z2 = Math.Abs(-3);
    int w2 = Math.Abs(4);
}
";

            var text3 = @"
using System;
public partial class C
{
#line 112 ""mah.cs""
    int y3 = 1;
    int x3 = 1;
    int z3 = Math.Abs(-3);
#line default
    int w3 = Math.Abs(4);
    double zed3 = Math.Sin(5);

    C() {
        Console.WriteLine(""hi"");
    } 

    static void Main()
    {
        C c = new C();
    }
}
";
            
            //Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            //loads the assembly into the ReflectionOnlyLoadFrom context.
            //So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateCompilationWithMscorlib(new[] { Parse(text1, "a.cs"), Parse(text2, "b.cs"), Parse(text3, "a.cs") }, options: TestOptions.DebugDll);

            compilation.VerifyPdb("C..ctor", @"
<symbols>
<files>
  <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""E2, 3B, 47,  2, DC, E4, 8D, B4, FF,  0, 67, 90, 31, 68, 74, C0,  6, D7, 39,  E, "" />
  <file id=""2"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""3"" name=""bar.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""4"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, CE, E5, E9, CB, 53, E5, EF, C1, 7F, 2C, 53, EC,  2, FE, 5C, 34, 2C, EF, 94, "" />
  <file id=""5"" name=""foo2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""6"" name=""mah.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""406ea660-64cf-4c82-b6f0-42d48172a799"" checkSum=""AB,  0, 7F, 1D, 23, D9, "" />
</files>
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""17"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""15"" file_ref=""1"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""26"" file_ref=""2"" />
        <entry il_offset=""0x14"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""25"" file_ref=""2"" />
        <entry il_offset=""0x20"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""30"" file_ref=""3"" />
        <entry il_offset=""0x34"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""15"" file_ref=""4"" />
        <entry il_offset=""0x3b"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""16"" file_ref=""4"" />
        <entry il_offset=""0x42"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""27"" file_ref=""5"" />
        <entry il_offset=""0x4f"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""26"" file_ref=""5"" />
        <entry il_offset=""0x5b"" start_row=""112"" start_column=""5"" end_row=""112"" end_column=""16"" file_ref=""6"" />
        <entry il_offset=""0x62"" start_row=""113"" start_column=""5"" end_row=""113"" end_column=""16"" file_ref=""6"" />
        <entry il_offset=""0x69"" start_row=""114"" start_column=""5"" end_row=""114"" end_column=""27"" file_ref=""6"" />
        <entry il_offset=""0x76"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""26"" file_ref=""1"" />
        <entry il_offset=""0x82"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""31"" file_ref=""1"" />
        <entry il_offset=""0x96"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""8"" file_ref=""1"" />
        <entry il_offset=""0x9d"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""1"" />
        <entry il_offset=""0x9e"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""33"" file_ref=""1"" />
        <entry il_offset=""0xa9"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0xaa"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543313, "DevDiv")]
        [Fact]
        public void TestFieldInitializerExpressionLambda()
        {
            var source = @"
class C
{
    int x = ((System.Func<int, int>)(z => z))(1);
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""50"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0"" name=""&lt;_ctor&gt;b__1"" parameterNames=""z"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".ctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""43"" end_row=""4"" end_column=""44"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FieldInitializerSequencePointSpans()
        {
            var source = @"
class C
{
    int x = 1, y = 2;
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""4"" start_column=""16"" end_row=""4"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Auto-Property

        [Fact, WorkItem(820806, "DevDiv")]
        public void BreakpointForAutoImplementedProperty()
        {
            var source = @"
public class C
{
    public static int AutoProp1 { get; private set; }
    internal string AutoProp2 { get; set; }
    internal protected C AutoProp3 { internal get; set;  }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_AutoProp1"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""35"" end_row=""4"" end_column=""39"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp1"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""40"" end_row=""4"" end_column=""52"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_AutoProp2"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""33"" end_row=""5"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp2"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""38"" end_row=""5"" end_column=""42"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_AutoProp3"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""38"" end_row=""6"" end_column=""51"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp3"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""52"" end_row=""6"" end_column=""56"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void PropertyDeclaration()
        {
            TestSequencePoints(
@"using System;

public class C
{
    int P { [|get;|] set; }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get; [|set;|] }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get [|{|] return 0; } }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get; } = [|int.Parse(""42"")|];
}", TestOptions.DebugDll, TestOptions.ExperimentalParseOptions);
        }

        #endregion

        #region ReturnStatement

        [Fact]
        public void Return_Implicit()
        {
            var source = @"class C
{
    static void Main()
    {
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Return_Explicit()
        {
            var source = @"class C
{
    static void Main()
    {
        return;
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""5"" start_column=""9"" end_row=""5"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(538298, "DevDiv")]
        [Fact]
        public void RegressSeqPtEndOfMethodAfterReturn()
        {
            var source = @"using System;

public class SeqPointAfterReturn
{
    public static int Main()
    {
        int ret = 0;
        ReturnVoid(100);
        if (field != ""Even"")
            ret = 1;

        ReturnVoid(99);
        if (field != ""Odd"")
            ret = ret + 1;

        string rets = ReturnValue(101);
        if (rets != ""Odd"")
            ret = ret + 1;

        rets = ReturnValue(102);
        if (rets != ""Even"")
            ret = ret + 1;

        return ret;
    }

    static string field;
    public static void ReturnVoid(int p)
    {
        int x = (int)(p % 2);
        if (x == 0)
        {
            field = ""Even"";
        }
        else
        {
            field = ""Odd"";
        }
    }

    public static string ReturnValue(int p)
    {
        int x = (int)(p % 2);
        if (x == 0)
        {
            return ""Even"";
        }
        else
        {
            return ""Odd"";
        }
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // Expected are current actual output plus Two extra expected SeqPt:
            //  <entry il_offset=""0x73"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
            //  <entry il_offset=""0x22"" start_row=""52"" start_column=""5"" end_row=""52"" end_column=""6"" file_ref=""0"" />
            // 
            // Note: NOT include other differences between Roslyn and Dev10, as they are filed in separated bugs
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""SeqPointAfterReturn"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""28"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""204"" />
          <slot kind=""1"" offset=""59"" />
          <slot kind=""1"" offset=""138"" />
          <slot kind=""1"" offset=""238"" />
          <slot kind=""1"" offset=""330"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""20"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x38"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x47"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x54"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x5c"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x64"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x71"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x75"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""24"" start_column=""9"" end_row=""24"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x7e"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
        <local name=""rets"" il_index=""1"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x81"">
        <namespace name=""System"" />
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
        <local name=""rets"" il_index=""1"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnVoid"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointAfterReturn"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""29"" start_column=""5"" end_row=""29"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0xa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""33"" start_column=""13"" end_row=""33"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""36"" start_column=""9"" end_row=""36"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""37"" start_column=""13"" end_row=""37"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""38"" start_column=""9"" end_row=""38"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""39"" start_column=""5"" end_row=""39"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnValue"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointAfterReturn"" methodName=""Main"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""42"" start_column=""5"" end_row=""42"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""43"" start_column=""9"" end_row=""43"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""44"" start_column=""9"" end_row=""44"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0xa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""45"" start_column=""9"" end_row=""45"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""46"" start_column=""13"" end_row=""46"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""49"" start_column=""9"" end_row=""49"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""50"" start_column=""13"" end_row=""50"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""52"" start_column=""5"" end_row=""52"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x21"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Exception Handling

        [WorkItem(542064, "DevDiv")]
        [Fact]
        public void ExceptionHandling()
        {
            var source = @"
class Test
{
    static int Main()
    {
        int ret = 0; // stop 1
        try
        { // stop 2
            throw new System.Exception(); // stop 3
        }
        catch (System.Exception e) // stop 4
        { // stop 5
            ret = 1; // stop 6
        }

        try
        { // stop 7
            throw new System.Exception(); // stop 8
        }
        catch // stop 9
        { // stop 10
            return ret; // stop 11
        }

    }
}
";
            // Dev12 inserts an additional sequence point on catch clause, just before 
            // the exception object is assigned to the variable. We don't place that sequence point.
            // Also the scope of he exception variable is different.

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Test.Main", @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""147"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x11"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <local name=""e"" il_index=""1"" il_start=""0xa"" il_end=""0x11"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x21"">
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <scope startOffset=""0xa"" endOffset=""0x11"">
          <local name=""e"" il_index=""1"" il_start=""0xa"" il_end=""0x11"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExceptionHandling_Filter1()
        {
            var source = @"
class Test
{
    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch (System.Exception e) if (e.Message != null)
        { 
            System.Console.WriteLine();
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Test.Main", @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""95"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""10"" start_column=""36"" end_row=""10"" end_column=""58"" file_ref=""0"" />
        <entry il_offset=""0x23"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""e"" il_index=""0"" il_start=""0x8"" il_end=""0x2e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2f"">
        <scope startOffset=""0x8"" endOffset=""0x2e"">
          <local name=""e"" il_index=""0"" il_start=""0x8"" il_end=""0x2e"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ExceptionHandling_Filter2()
        {
            var source = @"
class Test
{
    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch if (F())
        { 
            System.Console.WriteLine();
        }
    }

    private static bool F()
    {
        return true;
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Test.Main", @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""10"" start_column=""15"" end_row=""10"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(778655, "DevDiv")]
        [Fact]
        public void BranchToStartOfTry()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string str = null;
        bool isEmpty = string.IsNullOrEmpty(str);
        // isEmpty is always true here, so it should never go thru this if statement.
        if (!isEmpty)
        {
            throw new Exception();
        }
        try
        {
            Console.WriteLine();
        }
        catch
        {
        }
    }
}
";
            // Note the hidden sequence point @IL_0019.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""18"" />
          <slot kind=""0"" offset=""44"" />
          <slot kind=""1"" offset=""177"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""15"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""50"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x19"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""str"" il_index=""0"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
        <local name=""isEmpty"" il_index=""1"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2a"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""str"" il_index=""0"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
        <local name=""isEmpty"" il_index=""1"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region UsingStatement

        [Fact]
        public void UsingStatement()
        {
            var source = @"
public class DisposableClass : System.IDisposable
{
    private readonly string name;

    public DisposableClass(string name) 
    {
        this.name = name;
        System.Console.WriteLine(""Creating "" + name);
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing "" + name);
    }
}

class C
{
    static void Main()
    {
        using (DisposableClass a = new DisposableClass(""A""), b = new DisposableClass(""B""))
            System.Console.WriteLine(""First"");

        using (DisposableClass c = new DisposableClass(""C""), d = new DisposableClass(""D""))
        {
            System.Console.WriteLine(""Second"");
        }

        using (null)
        {

        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""name"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""176"" />
          <slot kind=""0"" offset=""206"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""18"">
        <entry il_offset=""0x0"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""22"" start_column=""16"" end_row=""22"" end_column=""60"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""22"" start_column=""62"" end_row=""22"" end_column=""90"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x24"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x31"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""25"" start_column=""16"" end_row=""25"" end_column=""60"" file_ref=""0"" />
        <entry il_offset=""0x47"" start_row=""25"" start_column=""62"" end_row=""25"" end_column=""90"" file_ref=""0"" />
        <entry il_offset=""0x52"" start_row=""26"" start_column=""9"" end_row=""26"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x53"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x61"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x6c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x6e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x7a"" start_row=""33"" start_column=""9"" end_row=""33"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x7b"" start_row=""34"" start_column=""5"" end_row=""34"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
        <local name=""c"" il_index=""2"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
        <local name=""d"" il_index=""3"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x7c"">
        <scope startOffset=""0x1"" endOffset=""0x3c"">
          <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
          <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x3c"" endOffset=""0x79"">
          <local name=""c"" il_index=""2"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
          <local name=""d"" il_index=""3"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region LockStatement

        [Fact]
        public void LockStatement()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        object o;
        Console.WriteLine(""Before"");
        lock (o = new object())
        {
            Console.WriteLine(""In"");
        }
        Console.WriteLine(""After"");
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""18"" />
          <slot kind=""3"" offset=""68"" />
          <slot kind=""2"" offset=""68"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0xc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x44"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x45"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x45"">
        <namespace name=""System"" />
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x45"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Anonymous Type

        [Fact]
        public void AnonymousType_Empty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new {};
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void AnonymousType_NonEmpty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new { a = 1 };
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region FixedStatement

        [Fact]
        public void FixedStatementSingleAddress()
        {
            var source = @"
using System;

unsafe class C
{
    int x;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x)
        {
            *p = 1;
        }
        Console.WriteLine(c.x);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""11"" start_column=""16"" end_row=""11"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x14"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x17"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x24"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x17"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x17"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementSingleString()
        {
            var source = @"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (char* p = ""hello"")
        {
            Console.WriteLine(*p);
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""24"" />
          <slot kind=""9"" offset=""24"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""16"" end_row=""8"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x21"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x22"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x21"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x21"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementSingleArray()
        {
            var source = @"
using System;

unsafe class C
{
    int[] a = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        fixed (int* p = c.a)
        {
            (*p)++;
        }
        Console.Write(c.a[0]);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""79"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""12"" start_column=""16"" end_row=""12"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x40"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x43"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x51"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x52"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x15"" il_end=""0x43"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x52"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x52"" attributes=""0"" />
        <scope startOffset=""0x15"" endOffset=""0x43"">
          <local name=""p"" il_index=""1"" il_start=""0x15"" il_end=""0x43"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""26"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleAddresses()
        {
            var source = @"
using System;

unsafe class C
{
    int x;
    int y;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x, q = &c.y)
        {
            *p = 1;
            *q = 2;
        }
        Console.WriteLine(c.x + c.y);
    }
}
";
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
          <slot kind=""0"" offset=""57"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""16"" end_row=""12"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""12"" start_column=""31"" end_row=""12"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x38"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x39"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x25"" attributes=""0"" />
        <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x25"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x39"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x39"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x25"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x25"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x25"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleStrings()
        {
            var source = @"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (char* p = ""hello"", q = ""goodbye"")
        {
            Console.Write(*p);
            Console.Write(*q);
        }
    }
}
";
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""24"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""9"" offset=""24"" />
          <slot kind=""9"" offset=""37"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""16"" end_row=""8"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""8"" start_column=""35"" end_row=""8"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x3a"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x3b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
        <local name=""q"" il_index=""1"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x3f"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
          <local name=""q"" il_index=""1"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleArrays()
        {
            var source = @"
using System;

unsafe class C
{
    int[] a = new int[1];
    int[] b = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        Console.Write(c.b[0]);
        fixed (int* p = c.a, q = c.b)
        {
            *p = 1;
            *q = 2;
        }
        Console.Write(c.a[0]);
        Console.Write(c.b[0]);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""111"" />
          <slot kind=""0"" offset=""120"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""14"" start_column=""16"" end_row=""14"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""14"" start_column=""30"" end_row=""14"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x5f"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x63"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x68"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x6e"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x7c"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x8a"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x8b"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x23"" il_end=""0x6e"" attributes=""0"" />
        <local name=""q"" il_index=""2"" il_start=""0x23"" il_end=""0x6e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x8b"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x8b"" attributes=""0"" />
        <scope startOffset=""0x23"" endOffset=""0x6e"">
          <local name=""p"" il_index=""1"" il_start=""0x23"" il_end=""0x6e"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x23"" il_end=""0x6e"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""26"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleMixed()
        {
            var source = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
            Console.Write((int)*p);
            Console.Write((int)*q);
            Console.Write((int)*r);
        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""20"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""48"" />
          <slot kind=""0"" offset=""58"" />
          <slot kind=""0"" offset=""67"" />
          <slot kind=""temp"" />
          <slot kind=""9"" offset=""67"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""16"" end_row=""12"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""12"" start_column=""32"" end_row=""12"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""12"" start_column=""41"" end_row=""12"" end_column=""52"" file_ref=""0"" />
        <entry il_offset=""0x43"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x44"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x4d"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x56"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x5f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x68"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x69"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
        <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
        <local name=""r"" il_index=""3"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x69"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x69"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x68"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
          <local name=""r"" il_index=""3"" il_start=""0x7"" il_end=""0x68"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""28"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Line Directives

        [Fact]
        public void LineDirective()
        {
            var source = @"
#line 50 ""foo.cs""

using System;

unsafe class C
{
    static void Main()
    {
        Console.Write(1);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""56"" start_column=""5"" end_row=""56"" end_column=""6"" file_ref=""1"" />
        <entry il_offset=""0x1"" start_row=""57"" start_column=""9"" end_row=""57"" end_column=""26"" file_ref=""1"" />
        <entry il_offset=""0x8"" start_row=""58"" start_column=""5"" end_row=""58"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(544917, "DevDiv")]
        [Fact]
        public void DisabledLineDirective()
        {
            var source = @"
#if false
#line 50 ""foo.cs""
#endif

using System;

unsafe class C
{
    static void Main()
    {
        Console.Write(1);
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestLineDirectivesHidden()
        {
            var text1 = @"
using System;
public class C
{
    public void Foo()
    {
        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }

#line hidden
        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }
#line default

        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text1, options: TestOptions.DebugDll);
            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""32"">
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""6"" offset=""137"" />
          <slot kind=""8"" offset=""137"" />
          <slot kind=""0"" offset=""137"" />
          <slot kind=""6"" offset=""264"" />
          <slot kind=""8"" offset=""264"" />
          <slot kind=""0"" offset=""264"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""29"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""7"" start_column=""27"" end_row=""7"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x16"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""7"" start_column=""18"" end_row=""7"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x25"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""7"" start_column=""24"" end_row=""7"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x2f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x30"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x45"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x47"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x56"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x57"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x64"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x65"" start_row=""19"" start_column=""27"" end_row=""19"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x7b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7d"" start_row=""19"" start_column=""18"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x84"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x8d"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x94"" start_row=""19"" start_column=""24"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x9c"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""2"" il_start=""0x18"" il_end=""0x25"" attributes=""0"" />
        <local name=""x"" il_index=""5"" il_start=""0x47"" il_end=""0x57"" attributes=""0"" />
        <local name=""x"" il_index=""8"" il_start=""0x7d"" il_end=""0x8e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9d"">
        <namespace name=""System"" />
        <scope startOffset=""0x18"" endOffset=""0x25"">
          <local name=""x"" il_index=""2"" il_start=""0x18"" il_end=""0x25"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x47"" endOffset=""0x57"">
          <local name=""x"" il_index=""5"" il_start=""0x47"" il_end=""0x57"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x7d"" endOffset=""0x8e"">
          <local name=""x"" il_index=""8"" il_start=""0x7d"" il_end=""0x8e"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Constants

        [Fact]
        public void Constant_StringsWithSurrogateChar()
        {
            var source = @"
using System;
public class T
{
    public static void Main()
    {
        const string HighSurrogateCharacter = ""\uD800"";
        const string LowSurrogateCharacter = ""\uDC00"";
        const string MatchedSurrogateCharacters = ""\uD800\uDC00"";
    }
}";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unparied surrogates.
            c.VerifyPdb(string.Format(@"
<symbols>
  <entryPoint declaringType=""T"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""T"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <constant name=""HighSurrogateCharacter"" value=""{0}"" type=""String"" />
        <constant name=""LowSurrogateCharacter"" value=""{0}"" type=""String"" />
        <constant name=""MatchedSurrogateCharacters"" value=""{1}"" type=""String"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <constant name=""HighSurrogateCharacter"" value=""{0}"" type=""String"" />
        <constant name=""LowSurrogateCharacter"" value=""{0}"" type=""String"" />
        <constant name=""MatchedSurrogateCharacters"" value=""{1}"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", "\uFFFD", "\uD800\uDC00"));
        }

        [Fact, WorkItem(546862, "DevDiv")]
        public void Constant_InvalidUnicodeString()
        {
            var source = @"
using System;
public class T
{
    public static void Main()
    {
        const string invalidUnicodeString = ""\uD800\0\uDC00"";
    }
}";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unparied surrogates.
            c.VerifyPdb(string.Format(@"
<symbols>
  <entryPoint declaringType=""T"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""T"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <constant name=""invalidUnicodeString"" value=""{0}"" type=""String"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <constant name=""invalidUnicodeString"" value=""{0}"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", "\uFFFDU+0000\uFFFD"));
        }

        [Fact]
        public void WRN_PDBConstantStringValueTooLong()
        {
            var longStringValue = new string('a', 2049);
            var source = @"
using System;

class C
{
    static void Main()
    {
        const string foo = """ + longStringValue + @""";
        Console.Write(foo);
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);

            var exebits = new MemoryStream();
            var pdbbits = new MemoryStream();
            var result = compilation.Emit(exebits, pdbbits);
            result.Diagnostics.Verify();

            /*
             * old behavior. This new warning was abandoned
            result.Diagnostics.Verify(// warning CS7063: Constant string value of 'foo' is too long to be used in a PDB file. Only the debug experience may be affected.
                                      Diagnostic(ErrorCode.WRN_PDBConstantStringValueTooLong).WithArguments("foo", longStringValue.Substring(0, 20) + "..."));

            //make sure that this warning is suppressable
            compilation = CreateCompilationWithMscorlib(text, compOptions: Options.Exe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(false).
                WithSpecificDiagnosticOptions(new Dictionary<int, ReportWarning>(){ {(int)ErrorCode.WRN_PDBConstantStringValueTooLong, ReportWarning.Suppress} }));

            result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);
            result.Diagnostics.Verify();

            //make sure that this warning can be turned into an error.
            compilation = CreateCompilationWithMscorlib(text, compOptions: Options.Exe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(false).
                WithSpecificDiagnosticOptions(new Dictionary<int, ReportWarning>() { { (int)ErrorCode.WRN_PDBConstantStringValueTooLong, ReportWarning.Error } }));

            result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);
            Assert.False(result.Success);
            result.Diagnostics.Verify(
                                      Diagnostic(ErrorCode.WRN_PDBConstantStringValueTooLong).WithArguments("foo", longStringValue.Substring(0, 20) + "...").WithWarningAsError(true));
             * */
        }

        #endregion

        #region Nested Types

        [Fact]
        public void NestedTypes()
        {
            string source = @"
using System;

namespace N
{
	public class C
	{
		public class D<T>
		{
			public class E 
			{
				public static void f(int a) 
				{
					Console.WriteLine();
				}
			}
		}
	}
}
";
            var c = CreateCompilationWithMscorlib(Parse(source, filename: "file.cs"));
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""file.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""F7,  3, 46, 2C, 11, 16, DE, 85, F9, DD, 5C, 76, F6, 55, D9, 13, E0, 95, DE, 14, "" />
  </files>
  <methods>
    <method containingType=""N.C+D`1+E"" name=""f"" parameterNames=""a"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""14"" start_column=""6"" end_row=""14"" end_column=""26"" file_ref=""1"" />
        <entry il_offset=""0x5"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Expression Bodied Members

        [Fact]
        public void ExpressionBodiedProperty()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P => M();
    public int M()
    {
        return 2;
    }
}");
            comp.VerifyDiagnostics();
            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_P"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""21"" end_row=""4"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"" >
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""18"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedIndexer()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int this[int i] => M();
    public int M()
    {
        return 2;
    }
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_Item"" parameterNames=""i"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""31"" end_row=""4"" end_column=""34"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""18"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedMethod()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P => 2;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_P"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""21"" end_row=""4"" end_column=""22"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedOperator()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public static C operator ++(C c) => c;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""op_Increment"" parameterNames=""c"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""41"" end_row=""4"" end_column=""42"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedConversion()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public static explicit operator C(int i) => new C();
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""op_Explicit"" parameterNames=""i"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""49"" end_row=""4"" end_column=""56"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        #endregion

        [Fact, WorkItem(846584, "DevDiv")]
        public void RelativePathForExternalSource_Sha1()
        {
            var text1 = @"
#pragma checksum ""..\Test2.cs"" ""{406ea660-64cf-4c82-b6f0-42d48172a799}"" ""BA8CBEA9C2EFABD90D53B616FB80A081""

public class C
{
    public void InitializeComponent() {
        #line 4 ""..\Test2.cs""
        InitializeComponent();
        #line default
    }
}
";

            var compilation = CreateCompilationWithMscorlib(
                new[] { Parse(text1, @"C:\Folder1\Folder2\Test1.cs") },
                options: TestOptions.DebugDll.WithSourceReferenceResolver(SourceFileResolver.Default));

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""C:\Folder1\Folder2\Test1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""40, A6, 20,  2, 2E, 60, 7D, 4F, 2D, A8, F4, A6, ED, 2E,  E, 49, 8D, 9F, D7, EB, "" />
    <file id=""2"" name=""C:\Folder1\Test2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""406ea660-64cf-4c82-b6f0-42d48172a799"" checkSum=""BA, 8C, BE, A9, C2, EF, AB, D9,  D, 53, B6, 16, FB, 80, A0, 81, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""InitializeComponent"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""39"" end_row=""6"" end_column=""40"" file_ref=""1"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""9"" end_row=""4"" end_column=""31"" file_ref=""2"" />
        <entry il_offset=""0x8"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SymWriterErrors()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);

            // Verify full metadata contains expected rows.
            using (MemoryStream peStream = new MemoryStream(), pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    peStream: peStream,
                    pdbStream: pdbStream,
                    xmlDocumentationStream: null,
                    cancellationToken: default(CancellationToken),
                    win32Resources: null,
                    manifestResources: null,
                    options: null,
                    testData: new CompilationTestData() { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The method or operation is not implemented.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("The method or operation is not implemented."));

                Assert.False(result.Success);
            }
        }

        [Fact, WorkItem(1067635)]
        public void SuppressDynamicAndEncCDIForWinRT()
        {
            var source = @"
public class C
{
    public static void F()
    {
        dynamic a = 1;
        int b = 2;
        foreach (var x in new[] { 1,2,3 })
        {
            System.Console.WriteLine(a * b);
        }
    }
}
";

            var debug = CreateCompilationWithMscorlib(source, new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugWinMD);
            debug.VerifyPdb(@"
<symbols>
    <methods>
    <method containingType=""C"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""13"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""8"" start_column=""27"" end_row=""8"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x1f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""8"" start_column=""18"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0xe6"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xeb"" start_row=""8"" start_column=""24"" end_row=""8"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xf4"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <local name=""x"" il_index=""4"" il_start=""0x24"" il_end=""0xe7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xf5"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <scope startOffset=""0x24"" endOffset=""0xe7"">
          <local name=""x"" il_index=""4"" il_start=""0x24"" il_end=""0xe7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");

            var release = CreateCompilationWithMscorlib(source, new[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseWinMD);
            release.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""8"" start_column=""27"" end_row=""8"" end_column=""42"" file_ref=""0"" />
        <entry il_offset=""0x1d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""18"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0xdd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe1"" start_row=""8"" start_column=""24"" end_row=""8"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xea"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xeb"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }
    }
}
