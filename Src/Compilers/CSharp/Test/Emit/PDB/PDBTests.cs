// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTests : CSharpTestBase
    {
        private CultureInfo testCulture = new CultureInfo("en-US");

        [Fact]
        public void TestBasic()
        {
            var text = @"
class Program
{
    Program() { }

    static void Main(string[] args)
    {
        Program p = new Program();
    }
}
";

            string actual = GetPdbXml(text, TestOptions.Dll, "Program.Main");

            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName="".ctor"" parameterNames="""" />
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestSimpleLocals()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Dll, "C.Method");
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Method"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestSimpleBranches()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Dll, "C.Method");
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Method"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""21"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""15"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""9"" start_column=""13"" end_row=""9"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x26"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x28"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""19"" start_column=""17"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""19"" start_column=""28"" end_row=""19"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x30"" start_row=""20"" start_column=""17"" end_row=""20"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x38"" start_row=""21"" start_column=""17"" end_row=""21"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x4a"" start_row=""24"" start_column=""13"" end_row=""24"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x50"" start_row=""25"" start_column=""9"" end_row=""25"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x51"" start_row=""26"" start_column=""5"" end_row=""26"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""b"" il_index=""0"" il_start=""0x0"" il_end=""0x52"" attributes=""0"" />
        <local name=""s"" il_index=""2"" il_start=""0xb"" il_end=""0x1a"" attributes=""0"" />
        <local name=""s"" il_index=""3"" il_start=""0x1c"" il_end=""0x51"" attributes=""0"" />
        <local name=""i"" il_index=""4"" il_start=""0x1c"" il_end=""0x51"" attributes=""0"" />
        <local name=""j"" il_index=""5"" il_start=""0x28"" il_end=""0x40"" attributes=""0"" />
        <local name=""k"" il_index=""6"" il_start=""0x28"" il_end=""0x40"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x52"">
        <local name=""b"" il_index=""0"" il_start=""0x0"" il_end=""0x52"" attributes=""0"" />
        <scope startOffset=""0xb"" endOffset=""0x1a"">
          <local name=""s"" il_index=""2"" il_start=""0xb"" il_end=""0x1a"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1c"" endOffset=""0x51"">
          <local name=""s"" il_index=""3"" il_start=""0x1c"" il_end=""0x51"" attributes=""0"" />
          <local name=""i"" il_index=""4"" il_start=""0x1c"" il_end=""0x51"" attributes=""0"" />
          <scope startOffset=""0x28"" endOffset=""0x40"">
            <local name=""j"" il_index=""5"" il_start=""0x28"" il_end=""0x40"" attributes=""0"" />
            <local name=""k"" il_index=""6"" il_start=""0x28"" il_end=""0x40"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(538298, "DevDiv")]
        [Fact]
        public void RegressSeqPtEndOfMethodAfterReturn()
        {
            var text = @"using System;

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

            string actual = GetPdbXml(text, TestOptions.Dll);

            // Expected are current actual output plus Two extra expected SeqPt:
            //  <entry il_offset=""0x73"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
            //  <entry il_offset=""0x22"" start_row=""52"" start_column=""5"" end_row=""52"" end_column=""6"" file_ref=""0"" />
            // 
            // Note: NOT include other differences between Roslyn and Dev10, as they are filed in separated bugs
            string expected = @"
<symbols>
  <methods>
    <method containingType=""SeqPointAfterReturn"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""16"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x45"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x4d"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x5f"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x63"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x6b"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x7d"" start_row=""22"" start_column=""13"" end_row=""22"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""24"" start_column=""9"" end_row=""24"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x85"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
        <local name=""rets"" il_index=""1"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x87"">
        <namespace name=""System"" />
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
        <local name=""rets"" il_index=""1"" il_start=""0x0"" il_end=""0x87"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnVoid"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointAfterReturn"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" start_row=""29"" start_column=""5"" end_row=""29"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""20"" file_ref=""0"" />
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointAfterReturn"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" start_row=""42"" start_column=""5"" end_row=""42"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""43"" start_column=""9"" end_row=""43"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""44"" start_column=""9"" end_row=""44"" end_column=""20"" file_ref=""0"" />
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(538299, "DevDiv")]
        [Fact]
        public void WhileLoop()
        {
            var text = @"using System;

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

            string actual = GetPdbXml(text, TestOptions.Exe);

            // Offset 0x01 should be:
            //  <entry il_offset=""0x1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
            // Move original offset 0x01 to 0x33
            //  <entry il_offset=""0x33"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""22"" file_ref=""0"" />
            // 
            // Note: 16707566 == 0x00FEEFEE
            string expected = @"
<symbols>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""20"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""20"" start_column=""17"" end_row=""20"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""22"" start_column=""18"" end_row=""22"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x27"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""28"" start_column=""13"" end_row=""28"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""29"" start_column=""17"" end_row=""29"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""30"" start_column=""17"" end_row=""30"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""31"" start_column=""17"" end_row=""31"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""33"" start_column=""9"" end_row=""33"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x3e"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""34"" start_column=""9"" end_row=""34"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x4d"" start_row=""35"" start_column=""5"" end_row=""35"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""1"" il_start=""0x24"" il_end=""0x2f"" attributes=""0"" />
        <local name=""x"" il_index=""2"" il_start=""0x31"" il_end=""0x3d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4e"">
        <scope startOffset=""0x24"" endOffset=""0x2f"">
          <local name=""x"" il_index=""1"" il_start=""0x24"" il_end=""0x2f"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x31"" endOffset=""0x3d"">
          <local name=""x"" il_index=""2"" il_start=""0x31"" il_end=""0x3d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void ForEachLoopString()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Exe);

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

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""6"" start_column=""27"" end_row=""6"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0xa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""2"" il_start=""0xc"" il_end=""0x1d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2f"">
        <scope startOffset=""0xc"" endOffset=""0x1d"">
          <local name=""c"" il_index=""2"" il_start=""0xc"" il_end=""0x1d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void ForEachLoopArray()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Exe);

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

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
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
        <entry il_offset=""0x28"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""2"" il_start=""0xd"" il_end=""0x1a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x29"">
        <scope startOffset=""0xd"" endOffset=""0x1a"">
          <local name=""x"" il_index=""2"" il_start=""0xd"" il_end=""0x1a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(544937, "DevDiv")]
        [Fact]
        public void ForEachLoopMultiDimensionalArray()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Exe);

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

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""6"" start_column=""27"" end_row=""6"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x22"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2f"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x3a"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x43"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x44"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4a"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x58"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5c"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x69"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""5"" il_start=""0x2f"" il_end=""0x44"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6a"">
        <scope startOffset=""0x2f"" endOffset=""0x44"">
          <local name=""x"" il_index=""5"" il_start=""0x2f"" il_end=""0x44"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(544937, "DevDiv")]
        [Fact]
        public void ForEachLoopMultiDimensionalArrayBreakAndContinue()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Exe);

            // Stepping:
            //   After "continue", step to "in".
            //   After "break", step to first sequence point following loop body (in this case, method close brace).

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""22"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""9"" end_row=""12"" end_column=""11"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""14"" start_column=""27"" end_row=""14"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x3a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x45"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x50"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x52"" start_row=""14"" start_column=""18"" end_row=""14"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x60"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x61"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x71"" start_row=""16"" start_column=""29"" end_row=""16"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x73"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""17"" start_column=""24"" end_row=""17"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x83"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x8b"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x92"" start_row=""14"" start_column=""24"" end_row=""14"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xa1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa7"" start_row=""14"" start_column=""24"" end_row=""14"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xb5"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xbb"" start_row=""14"" start_column=""24"" end_row=""14"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xcc"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""array"" il_index=""0"" il_start=""0x0"" il_end=""0xcd"" attributes=""0"" />
        <local name=""i"" il_index=""8"" il_start=""0x52"" il_end=""0x8c"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xcd"">
        <namespace name=""System"" />
        <local name=""array"" il_index=""0"" il_start=""0x0"" il_end=""0xcd"" attributes=""0"" />
        <scope startOffset=""0x52"" endOffset=""0x8c"">
          <local name=""i"" il_index=""8"" il_start=""0x52"" il_end=""0x8c"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void ForEachLoopEnumerator()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Exe);

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

            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""6"" start_column=""27"" end_row=""6"" end_column=""69"" file_ref=""0"" />
        <entry il_offset=""0xd"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""6"" start_column=""18"" end_row=""6"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1f"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x20"" start_row=""6"" start_column=""24"" end_row=""6"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x2d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3c"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""1"" il_start=""0xf"" il_end=""0x20"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x3d"">
        <scope startOffset=""0xf"" endOffset=""0x20"">
          <local name=""x"" il_index=""1"" il_start=""0xf"" il_end=""0x20"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void DoLoop()
        {
            var text = @"using System;

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

            string actual = GetPdbXml(text, TestOptions.Exe);

            string expected = @"
<symbols>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""SeqPointForWhile"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""16"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""20"" start_column=""17"" end_row=""20"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""22"" start_column=""18"" end_row=""22"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""24"" start_column=""17"" end_row=""24"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""25"" start_column=""13"" end_row=""25"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""28"" start_column=""17"" end_row=""28"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x30"" start_row=""30"" start_column=""9"" end_row=""30"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""30"" start_column=""11"" end_row=""30"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""32"" start_column=""9"" end_row=""32"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""33"" start_column=""5"" end_row=""33"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(538317, "DevDiv")]
        [Fact]
        public void RegressSeqPtAtBeginOfCtor()
        {
            var text = @"namespace NS
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

            string actual = GetPdbXml(text, TestOptions.Dll);

            #region "Dev10 vs. Roslyn"
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
            #endregion

            string expected = @"
<symbols>
  <methods>
    <method containingType=""NS.MyClass"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""NS.MyClass"" methodName="".ctor"" parameterNames="""" />
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
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(539898, "DevDiv")]
        [Fact]
        public void RegressSeqPointsForLambdaBody()
        {
            var text = @"using System;
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""13"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x25"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x25"">
        <namespace name=""System"" />
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x25"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C"" name=""&lt;Main&gt;b__0"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""21"" end_row=""7"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

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

            string actual = GetPdbXml(compilation, "C..ctor");

            string expected = @"
<symbols>
  <files>
    <file id=""1"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""2"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
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
</symbols>";
            AssertXmlEqual(expected, actual);
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
            var compilation = CreateCompilationWithMscorlib(new[] { ParseWithChecksum(text1, "a.cs"), ParseWithChecksum(text2, "b.cs"), ParseWithChecksum(text3, "a.cs") }, compOptions: TestOptions.Dll);

            string actual = GetPdbXml(compilation, "C..ctor");

            string expected = @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""97, 63, 79, DA, 3D, 5D, 5B, 2B, CF, 44, E6,  D, AE,  B, 1B, F0, D8, 20, F4, F9, "" />
    <file id=""2"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""3"" name=""bar.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""4"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""A7, 4B, B7, C3, B2, 7D,  B, C9, A0, 4A, AD, 18, 74,  8, E9, 4E, DF, CC, 88, B9, "" />
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
</symbols>";
            AssertXmlEqual(expected, actual);
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

            var compilation = CreateCompilationWithMscorlib(text1, compOptions: TestOptions.Dll);

            string actual = GetPdbXml(compilation, "C.Foo");

            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
        <entry il_offset=""0x33"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x48"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x4f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x50"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x58"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x59"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x68"" start_row=""19"" start_column=""27"" end_row=""19"" end_column=""51"" file_ref=""0"" />
        <entry il_offset=""0x7c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7e"" start_row=""19"" start_column=""18"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x83"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x84"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x8c"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x8d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x91"" start_row=""19"" start_column=""24"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x9b"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""2"" il_start=""0x18"" il_end=""0x25"" attributes=""0"" />
        <local name=""x"" il_index=""4"" il_start=""0x4a"" il_end=""0x59"" attributes=""0"" />
        <local name=""x"" il_index=""5"" il_start=""0x7e"" il_end=""0x8d"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x9c"">
        <namespace name=""System"" />
        <scope startOffset=""0x18"" endOffset=""0x25"">
          <local name=""x"" il_index=""2"" il_start=""0x18"" il_end=""0x25"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x4a"" endOffset=""0x59"">
          <local name=""x"" il_index=""4"" il_start=""0x4a"" il_end=""0x59"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x7e"" endOffset=""0x8d"">
          <local name=""x"" il_index=""5"" il_start=""0x7e"" il_end=""0x8d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestImplicitReturn()
        {
            var text = @"class C
{
    static void Main()
    {
    }
}
";

            string actual = GetPdbXml(text, TestOptions.Dll, "C.Main");
            string expected = @"
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExplicitReturn()
        {
            var text = @"class C
{
    static void Main()
    {
        return;
    }
}
";

            string actual = GetPdbXml(text, TestOptions.Dll, "C.Main");
            string expected = @"
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(542064, "DevDiv")]
        [Fact]
        public void TestExceptionHandling()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Dll, "Test.Main");
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExceptionHandling_Filter1()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll, "Test.Main");
            string expected = @"
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
";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExceptionHandling_Filter2()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll, "Test.Main");
            string expected = @"
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
";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void Destructors()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void UsingStatement()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll, "C.Main");
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""name"" />
      </customDebugInfo>
      <sequencepoints total=""18"">
        <entry il_offset=""0x0"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""22"" start_column=""16"" end_row=""22"" end_column=""60"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""22"" start_column=""62"" end_row=""22"" end_column=""90"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x24"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x36"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""25"" start_column=""16"" end_row=""25"" end_column=""60"" file_ref=""0"" />
        <entry il_offset=""0x51"" start_row=""25"" start_column=""62"" end_row=""25"" end_column=""90"" file_ref=""0"" />
        <entry il_offset=""0x5d"" start_row=""26"" start_column=""9"" end_row=""26"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""27"" start_column=""13"" end_row=""27"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x69"" start_row=""28"" start_column=""9"" end_row=""28"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6c"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x7e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x80"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x90"" start_row=""31"" start_column=""9"" end_row=""31"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x91"" start_row=""33"" start_column=""9"" end_row=""33"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x92"" start_row=""34"" start_column=""5"" end_row=""34"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x46"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x46"" attributes=""0"" />
        <local name=""c"" il_index=""3"" il_start=""0x46"" il_end=""0x90"" attributes=""0"" />
        <local name=""d"" il_index=""4"" il_start=""0x46"" il_end=""0x90"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x93"">
        <scope startOffset=""0x1"" endOffset=""0x46"">
          <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x46"" attributes=""0"" />
          <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x46"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x46"" endOffset=""0x90"">
          <local name=""c"" il_index=""3"" il_start=""0x46"" il_end=""0x90"" attributes=""0"" />
          <local name=""d"" il_index=""4"" il_start=""0x46"" il_end=""0x90"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void LockStatement()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll, "C.Main");
            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
        <entry il_offset=""0x3e"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x49"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x4a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4a"">
        <namespace name=""System"" />
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x4a"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void NoDebugInfoOnAnonymousTypes_Empty()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new {};
    }
}
";
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void NoDebugInfoOnAnonymousTypes_NonEmpty()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new { a = 1 };
    }
}
";
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

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
            string actual = GetPdbXml(text, TestOptions.Dll);
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
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
            string actual = GetPdbXml(text, TestOptions.Dll);
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
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

            string actual = GetPdbXml(text, TestOptions.Dll);
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x3f"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void IteratorWithLocals()
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program+&lt;IEI&gt;d__0`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""28"" bucketCount=""2"">
          <bucket startOffset=""0x3B"" endOffset=""0xD8"" />
          <bucket startOffset=""0x84"" endOffset=""0xD1"" />
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xd8"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Test`1+&lt;M&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x32"" endOffset=""0xCE"" />
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
        <entry il_offset=""0xac"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xc3"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xca"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xce"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""item"" il_index=""2"" il_start=""0x5b"" il_end=""0x8f"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xda"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x5b"" endOffset=""0x8f"">
          <local name=""item"" il_index=""2"" il_start=""0x5b"" il_end=""0x8f"" attributes=""0"" />
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
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" start_row=""20"" start_column=""5"" end_row=""20"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""21"" start_column=""27"" end_row=""21"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x12"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""21"" start_column=""18"" end_row=""21"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""24"" start_column=""9"" end_row=""24"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x24"" start_row=""21"" start_column=""24"" end_row=""21"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x30"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x24"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x41"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x14"" endOffset=""0x24"">
          <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x24"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""11"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x4e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x55"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x71"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x78"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x94"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x9b"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x9c"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <constant name=""d1"" value=""0.1"" type=""Decimal"" />
        <constant name=""dx"" value=""1.23"" type=""Decimal"" />
        <constant name=""d2"" value=""0.2"" type=""Decimal"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xa0"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x32"" endOffset=""0xa0"">
          <constant name=""d1"" value=""0.1"" type=""Decimal"" />
          <constant name=""dx"" value=""1.23"" type=""Decimal"" />
          <scope startOffset=""0x78"" endOffset=""0x9c"">
            <constant name=""d2"" value=""0.2"" type=""Decimal"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            using (new CultureContext("en-US"))
            {
                string actual = GetPdbXml(text, TestOptions.Exe);
                AssertXmlEqual(expected, actual);
            }
        }

        [WorkItem(543490, "DevDiv")]
        [Fact()]
        public void IteratorMultipleEnumerables()
        {
            #region "Source"
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
            #endregion

            #region "Expected Pdb Xml"
            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
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
        <entry il_offset=""0x24"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""47"" start_column=""5"" end_row=""47"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <scope startOffset=""0xf"" endOffset=""0x18"">
          <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
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
        <entry il_offset=""0x9c"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x9d"" start_row=""19"" start_column=""27"" end_row=""19"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xbb"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xbd"" start_row=""19"" start_column=""18"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xca"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xcb"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xde"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe6"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe7"" start_row=""19"" start_column=""24"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x104"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x108"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""v"" il_index=""2"" il_start=""0x54"" il_end=""0x7f"" attributes=""0"" />
        <local name=""v"" il_index=""4"" il_start=""0xbd"" il_end=""0xe7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x114"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""1"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x54"" endOffset=""0x7f"">
          <local name=""v"" il_index=""2"" il_start=""0x54"" il_end=""0x7f"" attributes=""0"" />
        </scope>
        <scope startOffset=""0xbd"" endOffset=""0xe7"">
          <local name=""v"" il_index=""4"" il_start=""0xbd"" il_end=""0xe7"" attributes=""0"" />
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6d"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6d"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            #endregion

            string actual = GetPdbXml(text, TestOptions.Exe);
            AssertXmlEqual(expected, actual);
        }

        [WorkItem(543313, "DevDiv")]
        [Fact]
        public void TestFieldInitializerExpressionLambda()
        {
            var text = @"
class C
{
    int x = ((System.Func<int, int>)(z => z))(1);
}
";
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""&lt;.ctor&gt;b__0"" parameterNames=""z"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".ctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""43"" end_row=""4"" end_column=""44"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
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
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FieldInitializerSequencePointSpans()
        {
            var text = @"
class C
{
    int x = 1, y = 2;
}
";
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
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
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void TestStepIntoNestedLambdas()
        {
            var text = @"using System;
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
            string actual = GetPdbXml(text, TestOptions.Exe);

            string expected = @"
<symbols>
  <entryPoint declaringType=""Test"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""10"" end_row=""6"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""7"" start_column=""13"" end_row=""7"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""M"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""13"" start_column=""9"" end_row=""21"" end_column=""11"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals2"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals2"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;M&gt;b__3"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
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
        <local name=""CS$&lt;&gt;8__locals4"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals4"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1"" name=""&lt;M&gt;b__5"" parameterNames=""y"">
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void LambdaInitialSequencePoints()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Foo"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;Foo&gt;b__2"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Foo"" parameterNames=""p"" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""37"" end_row=""6"" end_column=""38"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void NestedLambdaInitialSequencePoints()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""8"" start_column=""13"" end_row=""8"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""M"" parameterNames=""p"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""14"" start_column=""9"" end_row=""19"" end_column=""11"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals2"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals2"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0"" name=""&lt;M&gt;b__3"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test"" methodName=""Main"" parameterNames="""" />
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
        <local name=""CS$&lt;&gt;8__locals4"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals4"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1"" name=""&lt;M&gt;b__5"" parameterNames=""y"">
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementSingleAddress()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementSingleString()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""16"" end_row=""8"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x27"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x29"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x29"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2a"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x29"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x29"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementSingleArray()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementMultipleAddresses()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementMultipleStrings()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""8"" start_column=""16"" end_row=""8"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""8"" start_column=""35"" end_row=""8"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x3d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x3e"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x4e"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x53"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x53"" attributes=""0"" />
        <local name=""q"" il_index=""1"" il_start=""0x1"" il_end=""0x53"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x54"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x53"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x53"" attributes=""0"" />
          <local name=""q"" il_index=""1"" il_start=""0x1"" il_end=""0x53"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementMultipleArrays()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void FixedStatementMultipleMixed()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""12"" start_column=""16"" end_row=""12"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""12"" start_column=""32"" end_row=""12"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""12"" start_column=""41"" end_row=""12"" end_column=""52"" file_ref=""0"" />
        <entry il_offset=""0x4e"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4f"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""15"" start_column=""13"" end_row=""15"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x61"" start_row=""16"" start_column=""13"" end_row=""16"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x69"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x73"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x74"" attributes=""0"" />
        <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
        <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
        <local name=""r"" il_index=""3"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x74"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x74"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x73"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
          <local name=""r"" il_index=""3"" il_start=""0x7"" il_end=""0x73"" attributes=""0"" />
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void LineDirective()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(544917, "DevDiv")]
        [Fact]
        public void DisabledLineDirective()
        {
            var text = @"
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
            string actual = GetPdbXml(text, TestOptions.UnsafeExe.WithOptimizations(false));
            string expected = @"
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [Fact()]
        public void WRN_PDBConstantStringValueTooLong()
        {
            var longStringValue = new string('a', 2049);
            var text = @"
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

            var compilation = CreateCompilationWithMscorlib(text, compOptions:TestOptions.Exe.WithDebugInformationKind(DebugInformationKind.Full).WithOptimizations(false));

            var exebits = new MemoryStream();
            var pdbbits = new MemoryStream();
            var result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);
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

        [Fact(), WorkItem(543615, "DevDiv")]
        public void WRN_DebugFullNameTooLong()
        {
            var text = @"
using System;

using DICT1 = System.Collections.Generic.Dictionary<int, int>;

namespace foo
{
    using ACT = System.Action<DICT1, DICT1, DICT1, DICT1, DICT1, DICT1, DICT1>;
    
    class C
    {
        static void Main()
        {
            ACT ac = null;
            Console.Write(ac);
        }
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe.WithDebugInformationKind(DebugInformationKind.Full).WithOptimizations(false));

            var exebits = new MemoryStream();
            var pdbbits = new MemoryStream();
            var result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);

            result.Diagnostics.Verify(
                Diagnostic(ErrorCode.WRN_DebugFullNameTooLong, "Main").WithArguments("AACT TSystem.Action`7[[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        }

        [WorkItem(546754, "DevDiv")]
        [Fact]
        public void TestArrayTypeInAlias()
        {
            var source1 = @"
using System;

public class W {}

public class Y<T>
{
  public class F {}
  public class Z<U> {}
}
";
            string compName = GetUniqueName();
            var comp = CreateCompilationWithMscorlib(source1, assemblyName: compName, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));

            var source2 = @"
using t1 = Y<W[]>;
using t2 = Y<W[,]>;
using t3 = Y<W[,][]>;
using t4 = Y<Y<W>[][,]>;
using t5 = Y<W[,][]>.Z<W[][,,]>;
using t6 = Y<Y<Y<int[]>.F[,][]>.Z<Y<W[,][]>.F[]>[][]>;

public class C1
{
    public static void Main()
    {
    }
}
";
            string actual = GetPdbXml(source2, TestOptions.Exe.WithDebugInformationKind(DebugInformationKind.Full), references: new[] { comp.ToMetadataReference() } );
            string expected = string.Format(@"
<symbols>
  <entryPoint declaringType=""C1"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C1"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""6"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""t1"" target=""Y`1[[W[], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t2"" target=""Y`1[[W[,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t3"" target=""Y`1[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t4"" target=""Y`1[[Y`1[[W, {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][,][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t5"" target=""Y`1+Z`1[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[W[,,][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t6"" target=""Y`1[[Y`1+Z`1[[Y`1+F[[System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[Y`1+F[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>", compName);

            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestStringsWithSurrogateChar()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Exe.WithDebugInformationKind(DebugInformationKind.Full));

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unparied surrogates.
            string expected = String.Format(@"
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
</symbols>", "\uFFFD", "\uD800\uDC00");

            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(546862, "DevDiv")]
        public void TestInvalidUnicodeString()
        {
            var text = @"
using System;
public class T
{
    public static void Main()
    {
        const string invalidUnicodeString = ""\uD800\0\uDC00"";
    }
}";

            string actual = GetPdbXml(text, TestOptions.Exe.WithDebugInformationKind(DebugInformationKind.Full));

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unparied surrogates.
            string expected = String.Format(@"
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
</symbols>", "\uFFFDU+0000\uFFFD");

            AssertXmlEqual(expected, actual);
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
        public void TestDebuggerAttributes()
        {
            var text = @"
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

            string actual = GetPdbXml(text, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Hidden"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Hidden"" parameterNames="""" />
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
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""Hidden"" parameterNames="""" />
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
";
            AssertXmlEqual(expected, actual);
        }

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

            string actual = GetPdbXml(c);

            string expected = @"
<symbols>
  <files>
    <file id=""1"" name=""file.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
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
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(17390, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void EmitPDBForWithoutDynamicLocals()
        {
            string source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        var x = 1;
    }
}
";
            string actual = GetPdbXml(source, TestOptions.Dll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <namespace name=""System"" />
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            AssertXmlEqual(expected, actual);
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
            string actual = GetPdbXml(source, TestOptions.Exe.WithOptimizations(true));
        }

        [Fact]
        public void CompilerGeneratedLocals()
        {
            string source = @"
using System;
using System.Collections.Generic;

namespace LocalsWindow
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            foreach (var arg in args) // ForEachArray, ForEachArrayIndex0, ForEachArrayLimit0
            {
                fixed (char* p = arg) // FixedString
                {
                    Console.Write(*p);
                }
            }

            lock(args) // Lock
            {
                IEnumerable<string> e = args;
                foreach (var x in e) // ForEachEnumerator
                {
                    Console.Write(x);
                }
            }

            IDisposable d = null;
            using (d) // Using
            {
                Console.WriteLine(d);
            }
        }
    }
}
";
            string actual = GetPdbXml(source, TestOptions.Dll.WithAllowUnsafe(true).WithOptimizations(false).WithDebugInformationKind(DebugInformationKind.Full));
            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <methods>
    <method containingType=""LocalsWindow.Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""37"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""11"" start_column=""33"" end_row=""11"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x6"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""11"" start_column=""22"" end_row=""11"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""13"" start_column=""24"" end_row=""13"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""14"" start_column=""17"" end_row=""14"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""15"" start_column=""21"" end_row=""15"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0x37"" start_row=""16"" start_column=""17"" end_row=""16"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x38"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3e"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x3f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x43"" start_row=""11"" start_column=""30"" end_row=""11"" end_column=""32"" file_ref=""0"" />
        <entry il_offset=""0x4f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x52"" start_row=""19"" start_column=""13"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x5f"" start_row=""20"" start_column=""13"" end_row=""20"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x60"" start_row=""21"" start_column=""17"" end_row=""21"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x63"" start_row=""22"" start_column=""17"" end_row=""22"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x64"" start_row=""22"" start_column=""35"" end_row=""22"" end_column=""36"" file_ref=""0"" />
        <entry il_offset=""0x6d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x6f"" start_row=""22"" start_column=""26"" end_row=""22"" end_column=""31"" file_ref=""0"" />
        <entry il_offset=""0x78"" start_row=""23"" start_column=""17"" end_row=""23"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""24"" start_column=""21"" end_row=""24"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""25"" start_column=""17"" end_row=""25"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0x82"" start_row=""22"" start_column=""32"" end_row=""22"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0x91"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa5"" start_row=""26"" start_column=""13"" end_row=""26"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xa8"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xbc"" start_row=""28"" start_column=""13"" end_row=""28"" end_column=""34"" file_ref=""0"" />
        <entry il_offset=""0xbe"" start_row=""29"" start_column=""13"" end_row=""29"" end_column=""18"" file_ref=""0"" />
        <entry il_offset=""0xc1"" start_row=""30"" start_column=""13"" end_row=""30"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xc2"" start_row=""31"" start_column=""17"" end_row=""31"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0xc9"" start_row=""32"" start_column=""13"" end_row=""32"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0xcc"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xe0"" start_row=""33"" start_column=""9"" end_row=""33"" end_column=""10"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xe1"" attributes=""0"" />
        <local name=""CS$6$0000"" il_index=""1"" il_start=""0x2"" il_end=""0x4f"" attributes=""1"" />
        <local name=""CS$7$0001"" il_index=""2"" il_start=""0x2"" il_end=""0x4f"" attributes=""1"" />
        <local name=""arg"" il_index=""3"" il_start=""0x8"" il_end=""0x3f"" attributes=""0"" />
        <local name=""p"" il_index=""4"" il_start=""0xd"" il_end=""0x3e"" attributes=""0"" />
        <local name=""CS$519$0002"" il_index=""5"" il_start=""0xd"" il_end=""0x3e"" attributes=""1"" />
        <local name=""CS$2$0003"" il_index=""7"" il_start=""0x4f"" il_end=""0xbc"" attributes=""1"" />
        <local name=""CS$520$0004"" il_index=""8"" il_start=""0x4f"" il_end=""0xbc"" attributes=""1"" />
        <local name=""e"" il_index=""9"" il_start=""0x5f"" il_end=""0xa6"" attributes=""0"" />
        <local name=""CS$5$0005"" il_index=""10"" il_start=""0x64"" il_end=""0xa5"" attributes=""1"" />
        <local name=""x"" il_index=""11"" il_start=""0x6f"" il_end=""0x82"" attributes=""0"" />
        <local name=""CS$3$0006"" il_index=""12"" il_start=""0xbe"" il_end=""0xe0"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xe1"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0xe1"" attributes=""0"" />
        <scope startOffset=""0x2"" endOffset=""0x4f"">
          <local name=""CS$6$0000"" il_index=""1"" il_start=""0x2"" il_end=""0x4f"" attributes=""1"" />
          <local name=""CS$7$0001"" il_index=""2"" il_start=""0x2"" il_end=""0x4f"" attributes=""1"" />
          <scope startOffset=""0x8"" endOffset=""0x3f"">
            <local name=""arg"" il_index=""3"" il_start=""0x8"" il_end=""0x3f"" attributes=""0"" />
            <scope startOffset=""0xd"" endOffset=""0x3e"">
              <local name=""p"" il_index=""4"" il_start=""0xd"" il_end=""0x3e"" attributes=""0"" />
              <local name=""CS$519$0002"" il_index=""5"" il_start=""0xd"" il_end=""0x3e"" attributes=""1"" />
            </scope>
          </scope>
        </scope>
        <scope startOffset=""0x4f"" endOffset=""0xbc"">
          <local name=""CS$2$0003"" il_index=""7"" il_start=""0x4f"" il_end=""0xbc"" attributes=""1"" />
          <local name=""CS$520$0004"" il_index=""8"" il_start=""0x4f"" il_end=""0xbc"" attributes=""1"" />
          <scope startOffset=""0x5f"" endOffset=""0xa6"">
            <local name=""e"" il_index=""9"" il_start=""0x5f"" il_end=""0xa6"" attributes=""0"" />
            <scope startOffset=""0x64"" endOffset=""0xa5"">
              <local name=""CS$5$0005"" il_index=""10"" il_start=""0x64"" il_end=""0xa5"" attributes=""1"" />
              <scope startOffset=""0x6f"" endOffset=""0x82"">
                <local name=""x"" il_index=""11"" il_start=""0x6f"" il_end=""0x82"" attributes=""0"" />
              </scope>
            </scope>
          </scope>
        </scope>
        <scope startOffset=""0xbe"" endOffset=""0xe0"">
          <local name=""CS$3$0006"" il_index=""12"" il_start=""0xbe"" il_end=""0xe0"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
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
            string actual = GetPdbXml(source, TestOptions.Dll.WithOptimizations(false).WithDebugInformationKind(DebugInformationKind.Full));
            string expected = @"
<symbols>
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
      <scope startOffset=""0x0"" endOffset=""0x12"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""&lt;.cctor&gt;b__1"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""71"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x49"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x4b"">
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C"" name=""&lt;.cctor&gt;b__2"" parameterNames=""o"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""36"" end_row=""6"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""&lt;.cctor&gt;b__4"" parameterNames=""h"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""7"" start_column=""61"" end_row=""7"" end_column=""70"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals5"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <local name=""CS$&lt;&gt;8__locals5"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0"" name=""&lt;.cctor&gt;b__6"" parameterNames=""y"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".cctor"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""66"" end_row=""7"" end_column=""70"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
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
            // Note the hidden sequence point.
            string actual = GetPdbXml(source, TestOptions.Dll.WithAllowUnsafe(true).WithOptimizations(false).WithDebugInformationKind(DebugInformationKind.Full));
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""14"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""50"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xf"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x10"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x16"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""18"" start_column=""13"" end_row=""18"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""21"" start_column=""9"" end_row=""21"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""str"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""isEmpty"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""str"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""isEmpty"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(846584, "DevDiv")]
        public void RelativePathForExternalSource()
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
                new[] { ParseWithChecksum(text1, @"C:\Folder1\Folder2\Test1.cs") }, 
                compOptions: TestOptions.Dll.WithSourceReferenceResolver(SourceFileResolver.Default));

            string actual = GetPdbXml(compilation);

            string expected = @"
<symbols>
  <files>
    <file id=""1"" name=""C:\Folder1\Folder2\Test1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""83, F7, 6A, C2, BC, 53, 7E, F3, D7, E4, 94, 59,  5, 74, E3, E5, B4, B3, B5, 5F, "" />
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
            var comp = CreateCompilationWithMscorlib(source, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp, "C+<M>d__3.MoveNext");

            // One iterator local entry for the lambda local.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__3"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__2"" parameterNames="""" />
        <iteratorLocals version=""4"" kind=""IteratorLocals"" size=""20"" bucketCount=""1"">
          <bucket startOffset=""0x32"" endOffset=""0xFC"" />
        </iteratorLocals>
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xfc"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var userType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__3");
                var fieldDisplayStrings = stateMachineType.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString());
                AssertEx.SetEqual(fieldDisplayStrings, "C.<>c__DisplayClass0 C.<M>d__3.CS$<>8__locals1"); // Name follows lambda local pattern.
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
            var comp = CreateCompilationWithMscorlib(source, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp, "C+<M>d__3.MoveNext");

            // No iterator local entries.
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__3"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__2"" parameterNames="""" />
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""2"" il_start=""0x21"" il_end=""0x6c"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6c"">
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
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
                var stateMachineType = userType.GetMember<NamedTypeSymbol>("<M>d__3");
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
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
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
          <bucket startOffset=""0x21"" endOffset=""0xEC"" />
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0xec"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
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
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
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
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        <local name=""d"" il_index=""2"" il_start=""0x21"" il_end=""0x87"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x87"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x21"" endOffset=""0x87"">
          <local name=""d"" il_index=""2"" il_start=""0x21"" il_end=""0x87"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

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

            var comp = CreateCompilationWithMscorlib(source, compOptions: TestOptions.Dll.WithDebugInformationKind(DebugInformationKind.Full));
            string actual = GetPdbXml(comp);

            string expected = @"
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
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void SymWriterErrors()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            using (MemoryStream peStream = new MemoryStream(), pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    executableStream: peStream,
                    outputName: null,
                    pdbFilePath: null,
                    pdbStream: pdbStream,
                    xmlDocStream: null,
                    cancellationToken: default(CancellationToken),
                    win32Resources: null,
                    manifestResources: null,
                    moduleVersionId: Guid.NewGuid(),
                    metadataOnly: false,
                    testData: new CompilationTestData() { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The method or operation is not implemented.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("The method or operation is not implemented."));
            }

        }
    }
}

