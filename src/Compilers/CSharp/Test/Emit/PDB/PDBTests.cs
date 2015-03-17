﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
      <customDebugInfo>
        <forward declaringType=""Program"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""35"" document=""0"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Method"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""44"" />
          <slot kind=""0"" offset=""246"" />
          <slot kind=""0"" offset=""402"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""28"" document=""0"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""58"" document=""0"" />
        <entry offset=""0x14"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""0"" />
        <entry offset=""0x15"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""14"" document=""0"" />
        <entry offset=""0x16"" startLine=""12"" startColumn=""17"" endLine=""12"" endColumn=""33"" document=""0"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""60"" document=""0"" />
        <entry offset=""0x29"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" document=""0"" />
        <entry offset=""0x2a"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""14"" document=""0"" />
        <entry offset=""0x2b"" startLine=""17"" startColumn=""17"" endLine=""17"" endColumn=""31"" document=""0"" />
        <entry offset=""0x2d"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""62"" document=""0"" />
        <entry offset=""0x3e"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" document=""0"" />
        <entry offset=""0x3f"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""45"" document=""0"" />
        <entry offset=""0x4a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""0"" />
        <entry offset=""0x4b"" startLine=""22"" startColumn=""5"" endLine=""22"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""Program"" name=""Hidden"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""19"" document=""0"" />
        <entry offset=""0x3"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""30"" document=""0"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <namespace name=""System"" />
        <namespace name=""System.Diagnostics"" />
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""StepThrough"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Hidden"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""19"" document=""0"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""30"" document=""0"" />
        <entry offset=""0xa"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""y"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""y"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""NonUserCode"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Hidden"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""19"" document=""0"" />
        <entry offset=""0x3"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""30"" document=""0"" />
        <entry offset=""0xa"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".cctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""-45"" />
          <lambda offset=""-147"" />
          <lambda offset=""-109"" />
          <lambda offset=""-45"" />
          <lambda offset=""-40"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""9"" endColumn=""7"" document=""0"" />
      </sequencePoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass2_0"" name=""&lt;.cctor&gt;b__3"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""66"" endLine=""7"" endColumn=""70"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""93"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""38"" document=""0"" />
        <entry offset=""0x21"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""71"" document=""0"" />
        <entry offset=""0x41"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""27"" document=""0"" />
        <entry offset=""0x51"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_1"" parameterNames=""o"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""36"" endLine=""6"" endColumn=""37"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_2"" parameterNames=""h"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""7"" startColumn=""61"" endLine=""7"" endColumn=""70"" document=""0"" />
      </sequencePoints>
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

        /// <summary>
        /// Leading trivia is not included in the syntax offset.
        /// </summary>
        [Fact]
        public void SyntaxOffsetInPresenceOfTrivia_Methods()
        {
            string source = @"
class C
{
    public static void Main1() /*Comment1*/{/*Comment2*/int a = 1;/*Comment3*/}/*Comment4*/
    public static void Main2() {/*Comment2*/int a = 2;/*Comment3*/}/*Comment4*/
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // verify that both syntax offsets are the same
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main1"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""17"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""44"" endLine=""4"" endColumn=""45"" document=""0"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""57"" endLine=""4"" endColumn=""67"" document=""0"" />
        <entry offset=""0x3"" startLine=""4"" startColumn=""79"" endLine=""4"" endColumn=""80"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C"" name=""Main2"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""17"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""32"" endLine=""5"" endColumn=""33"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""45"" endLine=""5"" endColumn=""55"" document=""0"" />
        <entry offset=""0x3"" startLine=""5"" startColumn=""67"" endLine=""5"" endColumn=""68"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        /// <summary>
        /// Leading and trailing trivia are not included in the syntax offset.
        /// </summary>
        [Fact]
        public void SyntaxOffsetInPresenceOfTrivia_Initializers()
        {
            string source = @"
using System;
class C1
{
    public static Func<int> e=() => 0;
    public static Func<int> f/*Comment0*/=/*Comment1*/() => 1;/*Comment2*/
    public static Func<int> g=() => 2;
}
class C2
{
    public static Func<int> e=() => 0;
    public static Func<int> f=/*Comment1*/() => 1;
    public static Func<int> g=() => 2;
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // verify that syntax offsets of both .cctor's are the same
            c.VerifyPdb("C1..cctor", @"
<symbols>
  <methods>
    <method containingType=""C1"" name="".cctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>4</methodOrdinal>
          <lambda offset=""-29"" />
          <lambda offset=""-9"" />
          <lambda offset=""-1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""39"" document=""0"" />
        <entry offset=""0x15"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""63"" document=""0"" />
        <entry offset=""0x2a"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""39"" document=""0"" />
      </sequencePoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");

            c.VerifyPdb("C2..cctor", @"
<symbols>
  <methods>
    <method containingType=""C2"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".cctor"" />
        <encLambdaMap>
          <methodOrdinal>4</methodOrdinal>
          <lambda offset=""-29"" />
          <lambda offset=""-9"" />
          <lambda offset=""-1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""39"" document=""0"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""51"" document=""0"" />
        <entry offset=""0x2a"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""39"" document=""0"" />
      </sequencePoints>
      <locals />
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
    <method containingType=""C"" name=""Method"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""0"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""15"" document=""0"" />
        <entry offset=""0x5"" hidden=""true"" document=""0"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""0"" />
        <entry offset=""0x9"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""31"" document=""0"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""0"" />
        <entry offset=""0x16"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0x19"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1a"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""32"" document=""0"" />
        <entry offset=""0x20"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""23"" document=""0"" />
        <entry offset=""0x23"" hidden=""true"" document=""0"" />
        <entry offset=""0x25"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""14"" document=""0"" />
        <entry offset=""0x26"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""26"" document=""0"" />
        <entry offset=""0x2a"" startLine=""19"" startColumn=""28"" endLine=""19"" endColumn=""33"" document=""0"" />
        <entry offset=""0x2d"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""45"" document=""0"" />
        <entry offset=""0x35"" startLine=""21"" startColumn=""17"" endLine=""21"" endColumn=""27"" document=""0"" />
        <entry offset=""0x3c"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""14"" document=""0"" />
        <entry offset=""0x3d"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""28"" document=""0"" />
        <entry offset=""0x45"" hidden=""true"" document=""0"" />
        <entry offset=""0x49"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""23"" document=""0"" />
        <entry offset=""0x4f"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" document=""0"" />
        <entry offset=""0x50"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
            //  <entry offset=""0x1"" hidden=""true"" document=""0"" />
            // Move original offset 0x01 to 0x33
            //  <entry offset=""0x33"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" document=""0"" />
            // 
            // Note: 16707566 == 0x00FEEFEE
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" document=""0"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""0"" />
        <entry offset=""0xf"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x10"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointForWhile"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" document=""0"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""0"" />
        <entry offset=""0xc"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" document=""0"" />
        <entry offset=""0x11"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" document=""0"" />
        <entry offset=""0x13"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""27"" document=""0"" />
        <entry offset=""0x1a"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" document=""0"" />
        <entry offset=""0x1c"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""27"" document=""0"" />
        <entry offset=""0x1d"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""38"" document=""0"" />
        <entry offset=""0x22"" startLine=""31"" startColumn=""17"" endLine=""31"" endColumn=""23"" document=""0"" />
        <entry offset=""0x24"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" document=""0"" />
        <entry offset=""0x28"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""20"" document=""0"" />
        <entry offset=""0x2f"" startLine=""35"" startColumn=""5"" endLine=""35"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" parameterNames=""i"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""20"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" document=""0"" />
        <entry offset=""0x3"" hidden=""true"" document=""0"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""0"" />
        <entry offset=""0x6"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""41"" document=""0"" />
        <entry offset=""0xd"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""0"" />
        <entry offset=""0xe"" startLine=""9"" startColumn=""31"" endLine=""9"" endColumn=""35"" document=""0"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""29"" document=""0"" />
        <entry offset=""0x1c"" hidden=""true"" document=""0"" />
        <entry offset=""0x1f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" hidden=""true"" document=""0"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""0"" />
        <entry offset=""0x4"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""0"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
        <entry offset=""0xc"" hidden=""true"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""19"" document=""0"" />
        <entry offset=""0x3"" hidden=""true"" document=""0"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""0"" />
        <entry offset=""0x6"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""41"" document=""0"" />
        <entry offset=""0xd"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""0"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""16"" endLine=""7"" endColumn=""19"" document=""0"" />
        <entry offset=""0x14"" hidden=""true"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""34"" document=""0"" />
        <entry offset=""0x8"" hidden=""true"" document=""0"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" document=""0"" />
        <entry offset=""0x11"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""0"" />
        <entry offset=""0x16"" hidden=""true"" document=""0"" />
        <entry offset=""0x1a"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" document=""0"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""16"" document=""0"" />
        <entry offset=""0x2"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""37"" document=""0"" />
        <entry offset=""0xb"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" document=""0"" />
        <entry offset=""0x11"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""0"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""0"" />
        <entry offset=""0x19"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1a"" hidden=""true"" document=""0"" />
        <entry offset=""0x1e"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" document=""0"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithModuleName("MODULE"));

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
  IL_000a:  ldtoken    ""<PrivateImplementationDetails><MODULE>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails><MODULE>.EB196F988F4F427D318CA25B68671CF3A4510012""
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
            c.VerifyPdb("Program.Main", @"<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""5"" offset=""15"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""83"" />
          <slot kind=""1"" offset=""237"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""20"" document=""0"" />
        <entry offset=""0x2"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""47"" document=""0"" />
        <entry offset=""0x12"" hidden=""true"" document=""0"" />
        <entry offset=""0x14"" startLine=""12"" startColumn=""22"" endLine=""12"" endColumn=""27"" document=""0"" />
        <entry offset=""0x1b"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""14"" document=""0"" />
        <entry offset=""0x1c"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""33"" document=""0"" />
        <entry offset=""0x23"" hidden=""true"" document=""0"" />
        <entry offset=""0x29"" startLine=""17"" startColumn=""25"" endLine=""17"" endColumn=""31"" document=""0"" />
        <entry offset=""0x2b"" startLine=""20"" startColumn=""25"" endLine=""20"" endColumn=""42"" document=""0"" />
        <entry offset=""0x35"" hidden=""true"" document=""0"" />
        <entry offset=""0x38"" startLine=""21"" startColumn=""25"" endLine=""21"" endColumn=""26"" document=""0"" />
        <entry offset=""0x39"" startLine=""22"" startColumn=""25"" endLine=""22"" endColumn=""26"" document=""0"" />
        <entry offset=""0x3a"" startLine=""24"" startColumn=""25"" endLine=""24"" endColumn=""31"" document=""0"" />
        <entry offset=""0x3c"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" document=""0"" />
        <entry offset=""0x3d"" startLine=""12"" startColumn=""28"" endLine=""12"" endColumn=""30"" document=""0"" />
        <entry offset=""0x47"" hidden=""true"" document=""0"" />
        <entry offset=""0x52"" startLine=""27"" startColumn=""9"" endLine=""27"" endColumn=""10"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x3d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <scope startOffset=""0x14"" endOffset=""0x3d"">
          <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x3d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>"
);
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
    <method containingType=""SeqPointForWhile"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""28"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" document=""0"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""0"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <namespace name=""System"" />
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointForWhile"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""71"" />
          <slot kind=""1"" offset=""159"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" document=""0"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""0"" />
        <entry offset=""0xd"" hidden=""true"" document=""0"" />
        <entry offset=""0x10"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" document=""0"" />
        <entry offset=""0x11"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""26"" document=""0"" />
        <entry offset=""0x13"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" document=""0"" />
        <entry offset=""0x19"" hidden=""true"" document=""0"" />
        <entry offset=""0x1c"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""14"" document=""0"" />
        <entry offset=""0x1d"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" document=""0"" />
        <entry offset=""0x24"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""14"" document=""0"" />
        <entry offset=""0x27"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""14"" document=""0"" />
        <entry offset=""0x28"" startLine=""28"" startColumn=""17"" endLine=""28"" endColumn=""23"" document=""0"" />
        <entry offset=""0x2a"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2b"" startLine=""30"" startColumn=""11"" endLine=""30"" endColumn=""25"" document=""0"" />
        <entry offset=""0x30"" hidden=""true"" document=""0"" />
        <entry offset=""0x33"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""20"" document=""0"" />
        <entry offset=""0x3a"" startLine=""33"" startColumn=""5"" endLine=""33"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""NS.MyClass"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""25"" document=""0"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""0"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""27"" document=""0"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""NS.MyClass"" name="".ctor"" parameterNames=""values"">
      <customDebugInfo>
        <forward declaringType=""NS.MyClass"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""44"" document=""0"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""0"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""57"" document=""0"" />
        <entry offset=""0x19"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""NS.MyClass"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""NS.MyClass"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""56"" />
          <slot kind=""0"" offset=""126"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""0"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""27"" endLine=""18"" endColumn=""35"" document=""0"" />
        <entry offset=""0x5"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""26"" document=""0"" />
        <entry offset=""0x7"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""40"" document=""0"" />
        <entry offset=""0xd"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""48"" document=""0"" />
        <entry offset=""0x25"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""36"" document=""0"" />
        <entry offset=""0x32"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""Base"" name=""Finalize"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""29"" document=""0"" />
        <entry offset=""0xa"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
        <entry offset=""0x12"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Derived"" name=""Finalize"">
      <customDebugInfo>
        <forward declaringType=""Base"" methodName=""Finalize"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""0"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""29"" document=""0"" />
        <entry offset=""0xa"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""0"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""2"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""1"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""26"" document=""2"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""25"" document=""2"" />
        <entry offset=""0x20"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""30"" document=""3"" />
        <entry offset=""0x34"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""4"" />
        <entry offset=""0x3b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""16"" document=""4"" />
        <entry offset=""0x42"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""27"" document=""5"" />
        <entry offset=""0x4f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""26"" document=""5"" />
        <entry offset=""0x5b"" startLine=""112"" startColumn=""5"" endLine=""112"" endColumn=""16"" document=""6"" />
        <entry offset=""0x62"" startLine=""113"" startColumn=""5"" endLine=""113"" endColumn=""16"" document=""6"" />
        <entry offset=""0x69"" startLine=""114"" startColumn=""5"" endLine=""114"" endColumn=""27"" document=""6"" />
        <entry offset=""0x76"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""26"" document=""1"" />
        <entry offset=""0x82"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""31"" document=""1"" />
        <entry offset=""0x96"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""8"" document=""1"" />
        <entry offset=""0x9d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x9e"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""33"" document=""1"" />
        <entry offset=""0xa9"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <lambda offset=""-6"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""50"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__1_0"" parameterNames=""z"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""43"" endLine=""4"" endColumn=""44"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""14"" document=""0"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""16"" endLine=""4"" endColumn=""21"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""get_AutoProp1"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""35"" endLine=""4"" endColumn=""39"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp1"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""40"" endLine=""4"" endColumn=""52"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_AutoProp2"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""33"" endLine=""5"" endColumn=""37"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp2"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""38"" endLine=""5"" endColumn=""42"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_AutoProp3"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""38"" endLine=""6"" endColumn=""51"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_AutoProp3"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""52"" endLine=""6"" endColumn=""56"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""16"" document=""0"" />
        <entry offset=""0x3"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
            //  <entry offset=""0x73"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""0"" />
            //  <entry offset=""0x22"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" document=""0"" />
            // 
            // Note: NOT include other differences between Roslyn and Dev10, as they are filed in separated bugs
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""SeqPointAfterReturn"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""204"" />
          <slot kind=""1"" offset=""59"" />
          <slot kind=""1"" offset=""138"" />
          <slot kind=""1"" offset=""238"" />
          <slot kind=""1"" offset=""330"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""21"" document=""0"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""25"" document=""0"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" document=""0"" />
        <entry offset=""0x1b"" hidden=""true"" document=""0"" />
        <entry offset=""0x1e"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""21"" document=""0"" />
        <entry offset=""0x20"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""24"" document=""0"" />
        <entry offset=""0x28"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""28"" document=""0"" />
        <entry offset=""0x38"" hidden=""true"" document=""0"" />
        <entry offset=""0x3b"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""27"" document=""0"" />
        <entry offset=""0x3f"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""40"" document=""0"" />
        <entry offset=""0x47"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""27"" document=""0"" />
        <entry offset=""0x54"" hidden=""true"" document=""0"" />
        <entry offset=""0x58"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""27"" document=""0"" />
        <entry offset=""0x5c"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""33"" document=""0"" />
        <entry offset=""0x64"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""28"" document=""0"" />
        <entry offset=""0x71"" hidden=""true"" document=""0"" />
        <entry offset=""0x75"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""27"" document=""0"" />
        <entry offset=""0x79"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""20"" document=""0"" />
        <entry offset=""0x7e"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
      <customDebugInfo>
        <forward declaringType=""SeqPointAfterReturn"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""29"" startColumn=""5"" endLine=""29"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""30"" document=""0"" />
        <entry offset=""0x5"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""20"" document=""0"" />
        <entry offset=""0xa"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" document=""0"" />
        <entry offset=""0xe"" startLine=""33"" startColumn=""13"" endLine=""33"" endColumn=""28"" document=""0"" />
        <entry offset=""0x18"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1b"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1c"" startLine=""37"" startColumn=""13"" endLine=""37"" endColumn=""27"" document=""0"" />
        <entry offset=""0x26"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""10"" document=""0"" />
        <entry offset=""0x27"" startLine=""39"" startColumn=""5"" endLine=""39"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnValue"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointAfterReturn"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""42"" startColumn=""5"" endLine=""42"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""9"" endLine=""43"" endColumn=""30"" document=""0"" />
        <entry offset=""0x5"" startLine=""44"" startColumn=""9"" endLine=""44"" endColumn=""20"" document=""0"" />
        <entry offset=""0xa"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""45"" startColumn=""9"" endLine=""45"" endColumn=""10"" document=""0"" />
        <entry offset=""0xe"" startLine=""46"" startColumn=""13"" endLine=""46"" endColumn=""27"" document=""0"" />
        <entry offset=""0x16"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""10"" document=""0"" />
        <entry offset=""0x17"" startLine=""50"" startColumn=""13"" endLine=""50"" endColumn=""26"" document=""0"" />
        <entry offset=""0x1f"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""147"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" document=""0"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""0"" />
        <entry offset=""0x4"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""42"" document=""0"" />
        <entry offset=""0xa"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""35"" document=""0"" />
        <entry offset=""0xb"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""0"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""21"" document=""0"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""0"" />
        <entry offset=""0x11"" hidden=""true"" document=""0"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""0"" />
        <entry offset=""0x13"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""42"" document=""0"" />
        <entry offset=""0x19"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" document=""0"" />
        <entry offset=""0x1a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1b"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""24"" document=""0"" />
        <entry offset=""0x1f"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
        catch (System.Exception e) when (e.Message != null)
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
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""95"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""42"" document=""0"" />
        <entry offset=""0x8"" hidden=""true"" document=""0"" />
        <entry offset=""0x15"" startLine=""10"" startColumn=""36"" endLine=""10"" endColumn=""60"" document=""0"" />
        <entry offset=""0x23"" hidden=""true"" document=""0"" />
        <entry offset=""0x24"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0x25"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""40"" document=""0"" />
        <entry offset=""0x2b"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2e"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
</symbols>");
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
        catch when (F())
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
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""42"" document=""0"" />
        <entry offset=""0x8"" hidden=""true"" document=""0"" />
        <entry offset=""0x15"" startLine=""10"" startColumn=""15"" endLine=""10"" endColumn=""25"" document=""0"" />
        <entry offset=""0x1f"" hidden=""true"" document=""0"" />
        <entry offset=""0x20"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0x21"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""40"" document=""0"" />
        <entry offset=""0x27"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2a"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
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
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
          <slot kind=""0"" offset=""44"" />
          <slot kind=""1"" offset=""177"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""27"" document=""0"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""50"" document=""0"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""22"" document=""0"" />
        <entry offset=""0xf"" hidden=""true"" document=""0"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x13"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""35"" document=""0"" />
        <entry offset=""0x19"" hidden=""true"" document=""0"" />
        <entry offset=""0x1a"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1b"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""33"" document=""0"" />
        <entry offset=""0x21"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""0"" />
        <entry offset=""0x24"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" document=""0"" />
        <entry offset=""0x25"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""0"" />
        <entry offset=""0x26"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""0"" />
        <entry offset=""0x29"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""name"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""176"" />
          <slot kind=""0"" offset=""206"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""22"" startColumn=""16"" endLine=""22"" endColumn=""60"" document=""0"" />
        <entry offset=""0xc"" startLine=""22"" startColumn=""62"" endLine=""22"" endColumn=""90"" document=""0"" />
        <entry offset=""0x17"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""47"" document=""0"" />
        <entry offset=""0x24"" hidden=""true"" document=""0"" />
        <entry offset=""0x2f"" hidden=""true"" document=""0"" />
        <entry offset=""0x31"" hidden=""true"" document=""0"" />
        <entry offset=""0x3c"" startLine=""25"" startColumn=""16"" endLine=""25"" endColumn=""60"" document=""0"" />
        <entry offset=""0x47"" startLine=""25"" startColumn=""62"" endLine=""25"" endColumn=""90"" document=""0"" />
        <entry offset=""0x52"" startLine=""26"" startColumn=""9"" endLine=""26"" endColumn=""10"" document=""0"" />
        <entry offset=""0x53"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""48"" document=""0"" />
        <entry offset=""0x5e"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""0"" />
        <entry offset=""0x61"" hidden=""true"" document=""0"" />
        <entry offset=""0x6c"" hidden=""true"" document=""0"" />
        <entry offset=""0x6e"" hidden=""true"" document=""0"" />
        <entry offset=""0x79"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""10"" document=""0"" />
        <entry offset=""0x7a"" startLine=""33"" startColumn=""9"" endLine=""33"" endColumn=""10"" document=""0"" />
        <entry offset=""0x7b"" startLine=""34"" startColumn=""5"" endLine=""34"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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

        // LockStatement tested in CodeGenLock

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
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""24"" document=""0"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""31"" document=""0"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""16"" endLine=""11"" endColumn=""29"" document=""0"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""0"" />
        <entry offset=""0xf"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""20"" document=""0"" />
        <entry offset=""0x13"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""0"" />
        <entry offset=""0x14"" hidden=""true"" document=""0"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""32"" document=""0"" />
        <entry offset=""0x23"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""24"" />
          <slot kind=""9"" offset=""24"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" document=""0"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
        <entry offset=""0x16"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""35"" document=""0"" />
        <entry offset=""0x1e"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1f"" hidden=""true"" document=""0"" />
        <entry offset=""0x21"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""79"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""31"" document=""0"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""28"" document=""0"" />
        <entry offset=""0x31"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x32"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" document=""0"" />
        <entry offset=""0x3f"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""0"" />
        <entry offset=""0x40"" hidden=""true"" document=""0"" />
        <entry offset=""0x43"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""31"" document=""0"" />
        <entry offset=""0x51"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
          <slot kind=""0"" offset=""57"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""29"" document=""0"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""39"" document=""0"" />
        <entry offset=""0x15"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x16"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" document=""0"" />
        <entry offset=""0x1a"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""20"" document=""0"" />
        <entry offset=""0x1e"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1f"" hidden=""true"" document=""0"" />
        <entry offset=""0x25"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""38"" document=""0"" />
        <entry offset=""0x38"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""24"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""9"" offset=""24"" />
          <slot kind=""9"" offset=""37"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" document=""0"" />
        <entry offset=""0x1b"" startLine=""8"" startColumn=""35"" endLine=""8"" endColumn=""48"" document=""0"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""31"" document=""0"" />
        <entry offset=""0x32"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""31"" document=""0"" />
        <entry offset=""0x3a"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""0"" />
        <entry offset=""0x3b"" hidden=""true"" document=""0"" />
        <entry offset=""0x3f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""111"" />
          <slot kind=""0"" offset=""120"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""31"" document=""0"" />
        <entry offset=""0x15"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""31"" document=""0"" />
        <entry offset=""0x23"" startLine=""14"" startColumn=""16"" endLine=""14"" endColumn=""28"" document=""0"" />
        <entry offset=""0x3f"" startLine=""14"" startColumn=""30"" endLine=""14"" endColumn=""37"" document=""0"" />
        <entry offset=""0x5e"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""0"" />
        <entry offset=""0x5f"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""20"" document=""0"" />
        <entry offset=""0x63"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""20"" document=""0"" />
        <entry offset=""0x67"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""0"" />
        <entry offset=""0x68"" hidden=""true"" document=""0"" />
        <entry offset=""0x6e"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""31"" document=""0"" />
        <entry offset=""0x7c"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""31"" document=""0"" />
        <entry offset=""0x8a"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""0"" />
        <entry offset=""0xc"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""26"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""48"" />
          <slot kind=""0"" offset=""58"" />
          <slot kind=""0"" offset=""67"" />
          <slot kind=""temp"" />
          <slot kind=""9"" offset=""67"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""30"" document=""0"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""32"" endLine=""12"" endColumn=""39"" document=""0"" />
        <entry offset=""0x34"" startLine=""12"" startColumn=""41"" endLine=""12"" endColumn=""52"" document=""0"" />
        <entry offset=""0x43"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""0"" />
        <entry offset=""0x44"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""36"" document=""0"" />
        <entry offset=""0x4d"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""36"" document=""0"" />
        <entry offset=""0x56"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""36"" document=""0"" />
        <entry offset=""0x5e"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""0"" />
        <entry offset=""0x5f"" hidden=""true"" document=""0"" />
        <entry offset=""0x68"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""18"" document=""0"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""28"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""56"" startColumn=""5"" endLine=""56"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""57"" startColumn=""9"" endLine=""57"" endColumn=""26"" document=""1"" />
        <entry offset=""0x8"" startLine=""58"" startColumn=""5"" endLine=""58"" endColumn=""6"" document=""1"" />
      </sequencePoints>
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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""26"" document=""0"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""Foo"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""16"" document=""0"" />
        <entry offset=""0x2"" startLine=""7"" startColumn=""27"" endLine=""7"" endColumn=""51"" document=""0"" />
        <entry offset=""0x16"" hidden=""true"" document=""0"" />
        <entry offset=""0x18"" startLine=""7"" startColumn=""18"" endLine=""7"" endColumn=""23"" document=""0"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""0"" />
        <entry offset=""0x1d"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""34"" document=""0"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""0"" />
        <entry offset=""0x25"" hidden=""true"" document=""0"" />
        <entry offset=""0x29"" startLine=""7"" startColumn=""24"" endLine=""7"" endColumn=""26"" document=""0"" />
        <entry offset=""0x2f"" hidden=""true"" document=""0"" />
        <entry offset=""0x30"" hidden=""true"" document=""0"" />
        <entry offset=""0x45"" hidden=""true"" document=""0"" />
        <entry offset=""0x47"" hidden=""true"" document=""0"" />
        <entry offset=""0x4d"" hidden=""true"" document=""0"" />
        <entry offset=""0x4e"" hidden=""true"" document=""0"" />
        <entry offset=""0x56"" hidden=""true"" document=""0"" />
        <entry offset=""0x57"" hidden=""true"" document=""0"" />
        <entry offset=""0x5d"" hidden=""true"" document=""0"" />
        <entry offset=""0x64"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""16"" document=""0"" />
        <entry offset=""0x65"" startLine=""19"" startColumn=""27"" endLine=""19"" endColumn=""51"" document=""0"" />
        <entry offset=""0x7b"" hidden=""true"" document=""0"" />
        <entry offset=""0x7d"" startLine=""19"" startColumn=""18"" endLine=""19"" endColumn=""23"" document=""0"" />
        <entry offset=""0x84"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""0"" />
        <entry offset=""0x85"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""34"" document=""0"" />
        <entry offset=""0x8d"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""0"" />
        <entry offset=""0x8e"" hidden=""true"" document=""0"" />
        <entry offset=""0x94"" startLine=""19"" startColumn=""24"" endLine=""19"" endColumn=""26"" document=""0"" />
        <entry offset=""0x9c"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""T"" methodName=""Main"" />
  <methods>
    <method containingType=""T"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
  <entryPoint declaringType=""T"" methodName=""Main"" />
  <methods>
    <method containingType=""T"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""6"" endLine=""14"" endColumn=""26"" document=""1"" />
        <entry offset=""0x5"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""get_P"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""21"" endLine=""4"" endColumn=""24"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""18"" document=""0"" />
      </sequencePoints>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""31"" endLine=""4"" endColumn=""34"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""18"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""get_P"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""21"" endLine=""4"" endColumn=""22"" document=""0"" />
      </sequencePoints>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""41"" endLine=""4"" endColumn=""42"" document=""0"" />
      </sequencePoints>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""49"" endLine=""4"" endColumn=""56"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""InitializeComponent"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""39"" endLine=""6"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""9"" endLine=""4"" endColumn=""31"" document=""2"" />
        <entry offset=""0x8"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
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
                    getHostDiagnostics: null,
                    testData: new CompilationTestData() { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The method or operation is not implemented.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(new NotImplementedException().Message));

                Assert.False(result.Success);
            }
        }

        [Fact]
        public void ExtendedCustomDebugInformation()
        {
            var source =
@"class C
{
    static void M()
    {
        dynamic o = 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll.WithExtendedCustomDebugInformation(extendedCustomDebugInformation: true));
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <dynamicLocals>
          <bucket flagCount=""1"" flags=""1"" slotId=""0"" localName=""o"" />
        </dynamicLocals>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""23"" document=""0"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
            comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll.WithExtendedCustomDebugInformation(extendedCustomDebugInformation: false));
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""23"" document=""0"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
            comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithExtendedCustomDebugInformation(extendedCustomDebugInformation: true));
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
            comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithExtendedCustomDebugInformation(extendedCustomDebugInformation: false));
            comp.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
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
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""0"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" document=""0"" />
        <entry offset=""0xa"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" document=""0"" />
        <entry offset=""0xb"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" document=""0"" />
        <entry offset=""0x1f"" hidden=""true"" document=""0"" />
        <entry offset=""0x24"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" document=""0"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""0"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" document=""0"" />
        <entry offset=""0xe6"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""0"" />
        <entry offset=""0xe7"" hidden=""true"" document=""0"" />
        <entry offset=""0xeb"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" document=""0"" />
        <entry offset=""0xf4"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""0"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" document=""0"" />
        <entry offset=""0x9"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" document=""0"" />
        <entry offset=""0x1d"" hidden=""true"" document=""0"" />
        <entry offset=""0x22"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" document=""0"" />
        <entry offset=""0x26"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" document=""0"" />
        <entry offset=""0xdd"" hidden=""true"" document=""0"" />
        <entry offset=""0xe1"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" document=""0"" />
        <entry offset=""0xea"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""0"" />
      </sequencePoints>
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
