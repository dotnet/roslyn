// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests.EditAndContinueTestBase;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBLambdaTests : CSharpPDBTestBase
    {
        [WorkItem(539898, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539898")]
        [Fact]
        public void SequencePoints_Body()
        {
            var source = WithWindowsLineBreaks(@"
using System;
delegate void D();
class C
{
    public static void Main()
    {
        D d = () => Console.Write(1);
        d();
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
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""23"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""38"" document=""1"" />
        <entry offset=""0x21"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""13"" document=""1"" />
        <entry offset=""0x28"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x29"">
        <namespace name=""System"" />
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x29"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;Main&gt;b__0_0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""21"" endLine=""8"" endColumn=""37"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543479")]
        [Fact]
        public void Nested()
        {
            var source = MarkedSource(WithWindowsLineBreaks(@"
using System;
class Test
{
    public static int Main()
    {
        if (M(1) != 10) 
            return 1;
        return 0;
    }

    static public int M(int p)
    <M:0><C:0>{
        Func<int, int> f1 = delegate(int x)
        <C:1><L:0.0>{
            int q = 2;
            Func<int, int> f2 = (y) => 
            <L:1.1>{
                return p + q + x + y;
            };
            return f2(3);
        };
        return f1(4);
    }
}
"), removeTags: true); // We're validating offsets so need to remove tags entirely

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source.Tree, options: TestOptions.DebugDll);

            compilation.VerifyPdbLambdasAndClosures(source);
        }

        [WorkItem(543479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543479")]
        [Fact]
        public void InitialSequencePoints()
        {
            var source = WithWindowsLineBreaks(@"
class Test
{
    void Goo(int p)
    {
        System.Func<int> f1 = () => p;
        f1();
    }
}
");
            // Specifically note the sequence points at 0x0 in Test.Main, Test.M, and the lambda bodies.
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Goo"" parameterNames=""p"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""28"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""39"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""39"" document=""1"" />
        <entry offset=""0x1b"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""14"" document=""1"" />
        <entry offset=""0x22"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0_0"" name=""&lt;Goo&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Goo"" parameterNames=""p"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""37"" endLine=""6"" endColumn=""38"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543479")]
        [Fact]
        public void Nested_InitialSequencePoints()
        {
            var source = WithWindowsLineBreaks(@"
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
");
            // Specifically note the sequence points at 0x0 in Test.Main, Test.M, and the lambda bodies.
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""22"" document=""1"" />
        <entry offset=""0x16"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1a"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Test"" name=""M"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""26"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""56"" />
          <lambda offset=""56"" closure=""0"" />
          <lambda offset=""122"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""19"" endColumn=""11"" document=""1"" />
        <entry offset=""0x1b"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""22"" document=""1"" />
        <entry offset=""0x25"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_0"" name=""&lt;M&gt;b__0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""56"" />
          <slot kind=""0"" offset=""110"" />
          <slot kind=""21"" offset=""56"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x15"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""66"" document=""1"" />
        <entry offset=""0x29"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""26"" document=""1"" />
        <entry offset=""0x33"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_1"" name=""&lt;M&gt;b__1"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""122"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""40"" endLine=""17"" endColumn=""41"" document=""1"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""42"" endLine=""17"" endColumn=""63"" document=""1"" />
        <entry offset=""0x1f"" startLine=""17"" startColumn=""64"" endLine=""17"" endColumn=""65"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FieldAndPropertyInitializers()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class B
{
    public B(Func<int> f) { }
}

class C : B
{
    Func<int> FI = () => 1;
    static Func<int> FS = () => 2;
    Func<int> P { get; } = () => FS();
    public C() : base(() => 3) {}
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""B"" name="".ctor"" parameterNames=""f"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""28"" document=""1"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""29"" endLine=""6"" endColumn=""30"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""get_P"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""19"" endLine=""13"" endColumn=""23"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
        <encLambdaMap>
          <methodOrdinal>5</methodOrdinal>
          <lambda offset=""-2"" />
          <lambda offset=""-28"" />
          <lambda offset=""-19"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""28"" document=""1"" />
        <entry offset=""0x25"" startLine=""13"" startColumn=""28"" endLine=""13"" endColumn=""38"" document=""1"" />
        <entry offset=""0x4a"" startLine=""14"" startColumn=""18"" endLine=""14"" endColumn=""31"" document=""1"" />
        <entry offset=""0x70"" startLine=""14"" startColumn=""32"" endLine=""14"" endColumn=""33"" document=""1"" />
        <entry offset=""0x71"" startLine=""14"" startColumn=""33"" endLine=""14"" endColumn=""34"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
        <encLambdaMap>
          <methodOrdinal>6</methodOrdinal>
          <lambda offset=""-1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""35"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""29"" endLine=""14"" endColumn=""30"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_1"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""26"" endLine=""11"" endColumn=""27"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_2"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""34"" endLine=""13"" endColumn=""38"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__6_0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""33"" endLine=""12"" endColumn=""34"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ClosuresInCtor()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class B
{
    public B(Func<int> f) { }
}

class C : B
{
    Func<int> f, g, h;

    public C(int a, int b) : base(() => a) 
    {
        int c = 1;
        f = () => b;
        g = () => f();
        h = () => c;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""B"" name="".ctor"" parameterNames=""f"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""28"" document=""1"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""29"" endLine=""6"" endColumn=""30"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames=""a, b"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-1"" />
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>3</methodOrdinal>
          <closure offset=""-1"" />
          <closure offset=""0"" />
          <lambda offset=""-2"" closure=""0"" />
          <lambda offset=""41"" closure=""0"" />
          <lambda offset=""63"" closure=""0"" />
          <lambda offset=""87"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""30"" endLine=""13"" endColumn=""43"" document=""1"" />
        <entry offset=""0x2e"" hidden=""true"" document=""1"" />
        <entry offset=""0x34"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x35"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3c"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""21"" document=""1"" />
        <entry offset=""0x4e"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""23"" document=""1"" />
        <entry offset=""0x60"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""21"" document=""1"" />
        <entry offset=""0x72"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x73"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x73"" attributes=""0"" />
        <scope startOffset=""0x2e"" endOffset=""0x73"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x2e"" il_end=""0x73"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""41"" endLine=""13"" endColumn=""42"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_0"" name=""&lt;.ctor&gt;b__1"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""19"" endLine=""16"" endColumn=""20"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_0"" name=""&lt;.ctor&gt;b__2"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""19"" endLine=""17"" endColumn=""22"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_1"" name=""&lt;.ctor&gt;b__3"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""19"" endLine=""18"" endColumn=""20"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Queries1()
        {
            var source = WithWindowsLineBreaks(@"
using System.Linq;

class C
{
    public void M()
    {
        int c = 1;
        var x = from a in new[] { 1, 2, 3 }
                let b = a + c
                where b > 10
                select b * 10;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
 <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""35"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""92"" closure=""0"" />
          <lambda offset=""121"" />
          <lambda offset=""152"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x6"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" document=""1"" />
        <entry offset=""0xe"" startLine=""9"" startColumn=""9"" endLine=""12"" endColumn=""31"" document=""1"" />
        <entry offset=""0x79"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7a"">
        <namespace name=""System.Linq"" />
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
        <local name=""x"" il_index=""1"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_1"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""23"" endLine=""11"" endColumn=""29"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_2"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""24"" endLine=""12"" endColumn=""30"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;M&gt;b__0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""30"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Queries_GroupBy1()
        {
            var source = WithWindowsLineBreaks(@"
using System.Linq;

class C
{
    void F()
    {
        var result = from/*0*/ a in new[] { 1, 2, 3 }
                     join/*1*/ b in new[] { 5 } on a + 1 equals b - 1
                     group/*2*/ new { a, b = a + 5 } by new { c = a + 4 } into d
                     select/*3*/ d.Key;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""109"" />
          <lambda offset=""122"" />
          <lambda offset=""79"" />
          <lambda offset=""185"" />
          <lambda offset=""161"" />
          <lambda offset=""244"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""11"" endColumn=""40"" document=""1"" />
        <entry offset=""0xe6"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe7"">
        <namespace name=""System.Linq"" />
        <local name=""result"" il_index=""0"" il_start=""0x0"" il_end=""0xe7"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""52"" endLine=""9"" endColumn=""57"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_1"" parameterNames=""b"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""65"" endLine=""9"" endColumn=""70"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_2"" parameterNames=""a, b"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""70"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_3"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""57"" endLine=""10"" endColumn=""74"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_4"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""33"" endLine=""10"" endColumn=""53"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_5"" parameterNames=""d"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""34"" endLine=""11"" endColumn=""39"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForEachStatement_Array()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    void G(Func<int, int> f) {}

    void F()                       
    {                              
        foreach (int x0 in new[] { 1 })  // Group #0             
        {                                // Group #1
            int x1 = 0;                  
                                         
            G(a => x0);   
            G(a => x1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""41"" />
          <slot kind=""8"" offset=""41"" />
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""108"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""108"" />
          <lambda offset=""259"" closure=""0"" />
          <lambda offset=""287"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""28"" endLine=""10"" endColumn=""39"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""10"" startColumn=""18"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x20"" hidden=""true"" document=""1"" />
        <entry offset=""0x26"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x27"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2e"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""24"" document=""1"" />
        <entry offset=""0x41"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x54"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0x55"" hidden=""true"" document=""1"" />
        <entry offset=""0x59"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x5f"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x60"">
        <scope startOffset=""0x11"" endOffset=""0x55"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""2"" il_start=""0x11"" il_end=""0x55"" attributes=""0"" />
          <scope startOffset=""0x20"" endOffset=""0x55"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""3"" il_start=""0x20"" il_end=""0x55"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForEachStatement_MultidimensionalArray()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    void G(Func<int, int> f) {}

    void F()                       
    {                              
        foreach (int x0 in new[,] { { 1 } })  // Group #0             
        {                                     // Group #1
            int x1 = 0;                  
                                         
            G(a => x0);   
            G(a => x1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""41"" />
          <slot kind=""7"" offset=""41"" />
          <slot kind=""7"" offset=""41"" ordinal=""1"" />
          <slot kind=""8"" offset=""41"" />
          <slot kind=""8"" offset=""41"" ordinal=""1"" />
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""113"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""113"" />
          <lambda offset=""269"" closure=""0"" />
          <lambda offset=""297"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""28"" endLine=""10"" endColumn=""44"" document=""1"" />
        <entry offset=""0x2b"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" hidden=""true"" document=""1"" />
        <entry offset=""0x38"" hidden=""true"" document=""1"" />
        <entry offset=""0x3f"" startLine=""10"" startColumn=""18"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x4f"" hidden=""true"" document=""1"" />
        <entry offset=""0x56"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x57"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x5f"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""24"" document=""1"" />
        <entry offset=""0x73"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x87"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0x88"" hidden=""true"" document=""1"" />
        <entry offset=""0x8e"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x93"" hidden=""true"" document=""1"" />
        <entry offset=""0x97"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x9b"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9c"">
        <scope startOffset=""0x38"" endOffset=""0x88"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""5"" il_start=""0x38"" il_end=""0x88"" attributes=""0"" />
          <scope startOffset=""0x4f"" endOffset=""0x88"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""6"" il_start=""0x4f"" il_end=""0x88"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForEachStatement_String()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    void G(Func<int, int> f) {}

    void F()                       
    {                              
        foreach (int x0 in ""1"")  // Group #0             
        {                          // Group #1
            int x1 = 0;                  
                                         
            G(a => x0);   
            G(a => x1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""41"" />
          <slot kind=""8"" offset=""41"" />
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""100"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""100"" />
          <lambda offset=""245"" closure=""0"" />
          <lambda offset=""273"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""28"" endLine=""10"" endColumn=""31"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""10"" startColumn=""18"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x26"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2d"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""24"" document=""1"" />
        <entry offset=""0x40"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x53"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0x54"" hidden=""true"" document=""1"" />
        <entry offset=""0x58"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x61"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x62"">
        <scope startOffset=""0xc"" endOffset=""0x54"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""2"" il_start=""0xc"" il_end=""0x54"" attributes=""0"" />
          <scope startOffset=""0x1f"" endOffset=""0x54"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""3"" il_start=""0x1f"" il_end=""0x54"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForEachStatement_Enumerable()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;

class C
{
    void G(Func<int, int> f) {}

    void F()                       
    {                              
        foreach (int x0 in new List<int>())  // Group #0             
        {                                     // Group #1
            int x1 = 0;                  
                                         
            G(a => x0);   
            G(a => x1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""5"" offset=""41"" />
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""112"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""112"" />
          <lambda offset=""268"" closure=""0"" />
          <lambda offset=""296"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""11"" startColumn=""28"" endLine=""11"" endColumn=""43"" document=""1"" />
        <entry offset=""0xd"" hidden=""true"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" startLine=""11"" startColumn=""18"" endLine=""11"" endColumn=""24"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x28"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x29"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""24"" document=""1"" />
        <entry offset=""0x30"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x43"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""24"" document=""1"" />
        <entry offset=""0x56"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x57"" startLine=""11"" startColumn=""25"" endLine=""11"" endColumn=""27"" document=""1"" />
        <entry offset=""0x62"" hidden=""true"" document=""1"" />
        <entry offset=""0x71"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x72"">
        <scope startOffset=""0xf"" endOffset=""0x57"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""1"" il_start=""0xf"" il_end=""0x57"" attributes=""0"" />
          <scope startOffset=""0x22"" endOffset=""0x57"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""2"" il_start=""0x22"" il_end=""0x57"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForStatement1()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    bool G(Func<int, int> f) => true;

    void F()                       
    {                              
        for (int x0 = 0, x1 = 0; G(a => x0) && G(a => x1);)
        {
            int x2 = 0;
            G(a => x2); 
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""102"" />
          <slot kind=""1"" offset=""41"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""102"" />
          <lambda offset=""149"" closure=""1"" />
          <lambda offset=""73"" closure=""0"" />
          <lambda offset=""87"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""14"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0xe"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""32"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1e"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x25"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""24"" document=""1"" />
        <entry offset=""0x38"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x39"" startLine=""10"" startColumn=""34"" endLine=""10"" endColumn=""58"" document=""1"" />
        <entry offset=""0x63"" hidden=""true"" document=""1"" />
        <entry offset=""0x66"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x67"">
        <scope startOffset=""0x1"" endOffset=""0x66"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x66"" attributes=""0"" />
          <scope startOffset=""0x17"" endOffset=""0x39"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x17"" il_end=""0x39"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void SwitchStatement1()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class C
{
    bool G(Func<int> f) => true;

    int a = 1;

    void F()                       
    {        
        int x2 = 1;
        G(() => x2);
                      
        switch (a)
        {
            case 1:
                int x0 = 1;
                G(() => x0);
                break;

            case 2:
                int x1 = 1;
                G(() => x1);
                break;
        }
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""30"" offset=""86"" />
          <slot kind=""35"" offset=""86"" />
          <slot kind=""1"" offset=""86"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""86"" />
          <lambda offset=""48"" closure=""0"" />
          <lambda offset=""183"" closure=""1"" />
          <lambda offset=""289"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x6"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""20"" document=""1"" />
        <entry offset=""0xe"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""21"" document=""1"" />
        <entry offset=""0x21"" hidden=""true"" document=""1"" />
        <entry offset=""0x2e"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""28"" document=""1"" />
        <entry offset=""0x43"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""29"" document=""1"" />
        <entry offset=""0x56"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""23"" document=""1"" />
        <entry offset=""0x58"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""28"" document=""1"" />
        <entry offset=""0x5f"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""29"" document=""1"" />
        <entry offset=""0x72"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""23"" document=""1"" />
        <entry offset=""0x74"" startLine=""27"" startColumn=""5"" endLine=""27"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x75"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x75"" attributes=""0"" />
        <scope startOffset=""0x21"" endOffset=""0x74"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x21"" il_end=""0x74"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda1()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return o switch
        {
            int i => new Func<string>(() => $""Number: {i} + {o} == {i + (int)o}"")(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"" parameterNames=""o"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""0"" />
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""0"" />
              <closure offset=""11"" />
              <lambda offset=""83"" closure=""1"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0xe"" hidden=""true"" document=""1"" />
            <entry offset=""0x1e"" startLine=""8"" startColumn=""18"" endLine=""12"" endColumn=""10"" document=""1"" />
            <entry offset=""0x1f"" hidden=""true"" document=""1"" />
            <entry offset=""0x47"" hidden=""true"" document=""1"" />
            <entry offset=""0x49"" hidden=""true"" document=""1"" />
            <entry offset=""0x4b"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""84"" document=""1"" />
            <entry offset=""0x5f"" startLine=""11"" startColumn=""18"" endLine=""11"" endColumn=""30"" document=""1"" />
            <entry offset=""0x67"" hidden=""true"" document=""1"" />
            <entry offset=""0x6a"" startLine=""8"" startColumn=""9"" endLine=""12"" endColumn=""11"" document=""1"" />
            <entry offset=""0x6b"" hidden=""true"" document=""1"" />
            <entry offset=""0x6f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x71"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x71"" attributes=""0"" />
            <scope startOffset=""0xe"" endOffset=""0x6f"">
              <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0xe"" il_end=""0x6f"" attributes=""0"" />
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda2()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return o switch
        {
            int i when new Func<string>(() => $""Number: {i} + {o} == {i + (int)o}"")() != null => $""Definitely a number: {i}"",
            int i => new Func<string>(() => $""Number: {i} + {o} == {i + (int)o}"")(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"" parameterNames=""o"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""0"" />
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""35"" offset=""20"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""0"" />
              <closure offset=""11"" />
              <lambda offset=""85"" closure=""1"" />
              <lambda offset=""210"" closure=""1"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0xe"" hidden=""true"" document=""1"" />
            <entry offset=""0x2a"" startLine=""8"" startColumn=""18"" endLine=""13"" endColumn=""10"" document=""1"" />
            <entry offset=""0x2b"" hidden=""true"" document=""1"" />
            <entry offset=""0x3f"" hidden=""true"" document=""1"" />
            <entry offset=""0x41"" startLine=""10"" startColumn=""19"" endLine=""10"" endColumn=""94"" document=""1"" />
            <entry offset=""0x54"" hidden=""true"" document=""1"" />
            <entry offset=""0x56"" startLine=""10"" startColumn=""98"" endLine=""10"" endColumn=""125"" document=""1"" />
            <entry offset=""0x6e"" hidden=""true"" document=""1"" />
            <entry offset=""0x7c"" startLine=""11"" startColumn=""22"" endLine=""11"" endColumn=""84"" document=""1"" />
            <entry offset=""0x90"" startLine=""12"" startColumn=""18"" endLine=""12"" endColumn=""30"" document=""1"" />
            <entry offset=""0x98"" hidden=""true"" document=""1"" />
            <entry offset=""0x9b"" startLine=""8"" startColumn=""9"" endLine=""13"" endColumn=""11"" document=""1"" />
            <entry offset=""0x9c"" hidden=""true"" document=""1"" />
            <entry offset=""0xa1"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0xa4"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0xa4"" attributes=""0"" />
            <scope startOffset=""0xe"" endOffset=""0xa1"">
              <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0xe"" il_end=""0xa1"" attributes=""0"" />
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda3()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return new Func<string>(() => o.ToString())() switch
        {
            ""goo"" => o.ToString(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""35"" offset=""57"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""41"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe"" startLine=""8"" startColumn=""9"" endLine=""12"" endColumn=""11"" document=""1"" />
        <entry offset=""0x23"" startLine=""8"" startColumn=""55"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""34"" document=""1"" />
        <entry offset=""0x41"" startLine=""11"" startColumn=""18"" endLine=""11"" endColumn=""30"" document=""1"" />
        <entry offset=""0x49"" hidden=""true"" document=""1"" />
        <entry offset=""0x4c"" startLine=""8"" startColumn=""9"" endLine=""12"" endColumn=""11"" document=""1"" />
        <entry offset=""0x4d"" hidden=""true"" document=""1"" />
        <entry offset=""0x51"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <namespace name=""System"" />
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda4()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return o switch
        {
            string s => new Func<string>(() => string.Concat(""s"", s))(),
            int i => new Func<string>(() => i.ToString() + i switch
            {
                1 => new Func<string>(() => string.Concat(""One"", i))(),
                _ => ""Don't know""
            })(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"" parameterNames=""o"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""11"" />
              <lambda offset=""86"" closure=""0"" />
              <lambda offset=""157"" closure=""0"" />
              <lambda offset=""241"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1"" hidden=""true"" document=""1"" />
            <entry offset=""0xa"" startLine=""8"" startColumn=""18"" endLine=""17"" endColumn=""10"" document=""1"" />
            <entry offset=""0xb"" hidden=""true"" document=""1"" />
            <entry offset=""0x33"" hidden=""true"" document=""1"" />
            <entry offset=""0x35"" hidden=""true"" document=""1"" />
            <entry offset=""0x37"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""72"" document=""1"" />
            <entry offset=""0x4b"" hidden=""true"" document=""1"" />
            <entry offset=""0x4d"" startLine=""11"" startColumn=""22"" endLine=""15"" endColumn=""17"" document=""1"" />
            <entry offset=""0x61"" startLine=""16"" startColumn=""18"" endLine=""16"" endColumn=""30"" document=""1"" />
            <entry offset=""0x69"" hidden=""true"" document=""1"" />
            <entry offset=""0x6c"" startLine=""8"" startColumn=""9"" endLine=""17"" endColumn=""11"" document=""1"" />
            <entry offset=""0x6d"" hidden=""true"" document=""1"" />
            <entry offset=""0x71"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x73"">
            <namespace name=""System"" />
            <scope startOffset=""0x1"" endOffset=""0x71"">
              <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x71"" attributes=""0"" />
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda5()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return o switch
        {
            string s => new Func<string>(() => string.Concat(o, s))(),
            int i => new Func<string>(() => i.ToString() + i switch
            {
                1 => new Func<string>(() => string.Concat(""One"", i, o))(),
                _ => ""Don't know""
            })(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"" parameterNames=""o"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""0"" />
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""0"" />
              <closure offset=""11"" />
              <lambda offset=""86"" closure=""1"" />
              <lambda offset=""155"" closure=""1"" />
              <lambda offset=""239"" closure=""1"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0xe"" hidden=""true"" document=""1"" />
            <entry offset=""0x1e"" startLine=""8"" startColumn=""18"" endLine=""17"" endColumn=""10"" document=""1"" />
            <entry offset=""0x1f"" hidden=""true"" document=""1"" />
            <entry offset=""0x65"" hidden=""true"" document=""1"" />
            <entry offset=""0x67"" hidden=""true"" document=""1"" />
            <entry offset=""0x69"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""70"" document=""1"" />
            <entry offset=""0x7d"" hidden=""true"" document=""1"" />
            <entry offset=""0x7f"" startLine=""11"" startColumn=""22"" endLine=""15"" endColumn=""17"" document=""1"" />
            <entry offset=""0x93"" startLine=""16"" startColumn=""18"" endLine=""16"" endColumn=""30"" document=""1"" />
            <entry offset=""0x9b"" hidden=""true"" document=""1"" />
            <entry offset=""0x9e"" startLine=""8"" startColumn=""9"" endLine=""17"" endColumn=""11"" document=""1"" />
            <entry offset=""0x9f"" hidden=""true"" document=""1"" />
            <entry offset=""0xa3"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0xa5"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0xa5"" attributes=""0"" />
            <scope startOffset=""0xe"" endOffset=""0xa3"">
              <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0xe"" il_end=""0xa3"" attributes=""0"" />
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>");
        }

        [Fact]
        public void SwitchExpressionWithLambda6()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static string M(object o)
    {
        return o switch
        {
            string s => new Func<string>(() => string.Concat(""s"", s))(),
            int i => new Func<string>(() => i.ToString() + new Func<int>(() => i + 1)())(),
            _ => ""Don't know""
        };
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"" parameterNames=""o"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""11"" />
              <lambda offset=""86"" closure=""0"" />
              <lambda offset=""157"" closure=""0"" />
              <lambda offset=""192"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1"" hidden=""true"" document=""1"" />
            <entry offset=""0xa"" startLine=""8"" startColumn=""18"" endLine=""13"" endColumn=""10"" document=""1"" />
            <entry offset=""0xb"" hidden=""true"" document=""1"" />
            <entry offset=""0x33"" hidden=""true"" document=""1"" />
            <entry offset=""0x35"" hidden=""true"" document=""1"" />
            <entry offset=""0x37"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""72"" document=""1"" />
            <entry offset=""0x4b"" hidden=""true"" document=""1"" />
            <entry offset=""0x4d"" startLine=""11"" startColumn=""22"" endLine=""11"" endColumn=""91"" document=""1"" />
            <entry offset=""0x61"" startLine=""12"" startColumn=""18"" endLine=""12"" endColumn=""30"" document=""1"" />
            <entry offset=""0x69"" hidden=""true"" document=""1"" />
            <entry offset=""0x6c"" startLine=""8"" startColumn=""9"" endLine=""13"" endColumn=""11"" document=""1"" />
            <entry offset=""0x6d"" hidden=""true"" document=""1"" />
            <entry offset=""0x71"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x73"">
            <namespace name=""System"" />
            <scope startOffset=""0x1"" endOffset=""0x71"">
              <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x71"" attributes=""0"" />
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>");
        }

        [Fact]
        public void UsingStatement1()
        {
            string source = WithWindowsLineBreaks(@"
using System;

class C
{
    static bool G<T>(Func<T> f) => true;
    static int F(object a, object b) => 1;
    static IDisposable D() => null;
    
    static void F()                       
    {                              
        using (IDisposable x0 = D(), y0 = D())
        {
            int x1 = 1;
        
            G(() => x0);
            G(() => y0);
            G(() => x1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            // note that the two closures have a different syntax offset
            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""a, b"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""41"" endLine=""7"" endColumn=""42"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""f"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""41"" />
          <slot kind=""30"" offset=""89"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>3</methodOrdinal>
          <closure offset=""41"" />
          <closure offset=""89"" />
          <lambda offset=""147"" closure=""0"" />
          <lambda offset=""173"" closure=""0"" />
          <lambda offset=""199"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""36"" document=""1"" />
        <entry offset=""0x12"" startLine=""12"" startColumn=""38"" endLine=""12"" endColumn=""46"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x23"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x24"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2b"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""25"" document=""1"" />
        <entry offset=""0x3d"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""25"" document=""1"" />
        <entry offset=""0x4f"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""1"" />
        <entry offset=""0x61"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0x78"" hidden=""true"" document=""1"" />
        <entry offset=""0x79"" hidden=""true"" document=""1"" />
        <entry offset=""0x7b"" hidden=""true"" document=""1"" />
        <entry offset=""0x8f"" hidden=""true"" document=""1"" />
        <entry offset=""0x90"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x91"">
        <scope startOffset=""0x1"" endOffset=""0x90"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x90"" attributes=""0"" />
          <scope startOffset=""0x1d"" endOffset=""0x62"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x1d"" il_end=""0x62"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void IfStatement1()
        {
            string source = @"
class C
{
    static void F()
    {
        new System.Action(() =>
        {
            bool result = false;
            if (result)
                System.Console.WriteLine(1);
        })();
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("C.<>c.<F>b__0_0", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (bool V_0, //result
                bool V_1)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
 -IL_0003:  ldloc.0
  IL_0004:  stloc.1
 ~IL_0005:  ldloc.1
  IL_0006:  brfalse.s  IL_000f
 -IL_0008:  ldc.i4.1
  IL_0009:  call       ""void System.Console.WriteLine(int)""
  IL_000e:  nop
 -IL_000f:  ret
}
", displaySequencePoints: true);
        }

        [Fact]
        public void IfStatement2()
        {
            string source = @"
class C
{
    static void F()
    {
        new System.Action(() =>
        {
            {
                bool result = false;
                if (result)
                    System.Console.WriteLine(1);
            }
        })();
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("C.<>c.<F>b__0_0", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (bool V_0, //result
                bool V_1)
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldc.i4.0
  IL_0003:  stloc.0
 -IL_0004:  ldloc.0
  IL_0005:  stloc.1
 ~IL_0006:  ldloc.1
  IL_0007:  brfalse.s  IL_0010
 -IL_0009:  ldc.i4.1
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  nop
 -IL_0010:  nop
 -IL_0011:  ret
}
", displaySequencePoints: true);
        }

        [Fact]
        public void WithExpression()
        {
            var source = MarkedSource(WithWindowsLineBreaks(@"
using System;
record R(int X);

class Test
{
    public static void M(int a)
    <M:0><C:0>{
        var x = new R(1);
        var y = x with
        {
            X = new Func<int>(() => <L:0.0>a)()
        };
    }
}
"), removeTags: true); // We're validating offsets so need to remove tags entirely

            // Use NetCoreApp in order to use records
            var compilation = CreateCompilation(source.Tree, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            compilation.VerifyPdbLambdasAndClosures(source);
        }

        [Fact]
        public void WithExpression_2()
        {
            var source = MarkedSource(WithWindowsLineBreaks(@"
using System;
record R(int X, int Y);

class Test
{
    public static void M(int a)
    <M:0><C:0>{
        var x = new R(1, 2);
        var b = 1;
        var y = x with
        {
            X = new Func<int>(() => <L:0.0>a)(),
            Y = new Func<int>(() => <L:1.0>b)()
        };
    }
}
"), removeTags: true); // We're validating offsets so need to remove tags entirely

            // Use NetCoreApp in order to use records
            var compilation = CreateCompilation(source.Tree, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            compilation.VerifyPdbLambdasAndClosures(source);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/52068")]
        public void WithExpression_3()
        {
            var source = MarkedSource(WithWindowsLineBreaks(@"
using System;
record R(int X, int Y);
record Z(int A, R R);

class Test
{
    public static void M(int a)
    <M:0><C:0>{
        var r = new R(1, 2);
        var x = new Z(1, new R(2, 3));
        var b = 1;
        var y = x with
        {
            A = new Func<int>(() => <L:0.0>a)(),
            R = r with
            {
                X = 4,
                Y = new Func<int>(() => <L:1.0>b)()
            }
        };
    }
}
"), removeTags: true); // We're validating offsets so need to remove tags entirely

            // Use NetCoreApp in order to use records
            var compilation = CreateCompilation(source.Tree, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            compilation.VerifyPdbLambdasAndClosures(source);
        }

        [Fact]
        public void LiftedPrimaryParameter_Record()
        {
            var source = WithWindowsLineBreaks(@"
using System;
record D(int X)
{
    public int Y { get; set; } = new Func<int, int>(a => a + X).Invoke(1);
}
");

            // Use NetCoreApp in order to use records
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);

            compilation.VerifyPdb(@"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""D"" name="".ctor"" parameterNames=""X"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""-1"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""-1"" />
              <lambda offset=""-16"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" hidden=""true"" document=""1"" />
            <entry offset=""0x19"" startLine=""5"" startColumn=""34"" endLine=""5"" endColumn=""74"" document=""1"" />
            <entry offset=""0x31"" startLine=""3"" startColumn=""8"" endLine=""3"" endColumn=""16"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x39"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x39"" attributes=""0"" />
          </scope>
        </method>
        <method containingType=""D"" name=""get_X"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""3"" startColumn=""10"" endLine=""3"" endColumn=""15"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name=""set_X"" parameterNames=""value"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""3"" startColumn=""10"" endLine=""3"" endColumn=""15"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name=""get_Y"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""5"" startColumn=""20"" endLine=""5"" endColumn=""24"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name=""set_Y"" parameterNames=""value"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""5"" startColumn=""25"" endLine=""5"" endColumn=""29"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name="".ctor"" parameterNames=""original"">
          <customDebugInfo>
        <forward declaringType=""D"" methodName="".ctor"" parameterNames=""X"" />
          </customDebugInfo>
          <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""3"" startColumn=""8"" endLine=""3"" endColumn=""9"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D+&lt;&gt;c__DisplayClass0_0"" name=""&lt;.ctor&gt;b__0"" parameterNames=""a"">
          <customDebugInfo>
            <forward declaringType=""D"" methodName="".ctor"" parameterNames=""X"" />
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""5"" startColumn=""58"" endLine=""5"" endColumn=""63"" document=""1"" />
          </sequencePoints>
        </method>
      </methods>
    </symbols>", format: CodeAnalysis.Emit.DebugInformationFormat.Pdb);
        }

        [Fact]
        public void PrimaryBaseInitializer()
        {
            var source = WithWindowsLineBreaks(@"
using System;
record C(int X)
{
    public C(int x, Func<int> f)
        : this(x)
    {
    }
}

record D(int X) : C(F(X, out int z), () => z)
{
    static int F(int x, out int p)
    {
        p = 1;
        return x + 1;
    }
}
");

            // Use NetCoreApp in order to use records
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();

            compilation.VerifyPdb(@"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name="".ctor"" parameterNames=""X"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" startLine=""3"" startColumn=""8"" endLine=""3"" endColumn=""16"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0xf"">
            <namespace name=""System"" />
          </scope>
        </method>
        <method containingType=""C"" name=""get_X"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""3"" startColumn=""10"" endLine=""3"" endColumn=""15"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""C"" name=""set_X"" parameterNames=""value"">
          <sequencePoints>
            <entry offset=""0x0"" startLine=""3"" startColumn=""10"" endLine=""3"" endColumn=""15"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""C"" name="".ctor"" parameterNames=""x, f"">
          <customDebugInfo>
            <forward declaringType=""C"" methodName="".ctor"" parameterNames=""X"" />
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""6"" startColumn=""11"" endLine=""6"" endColumn=""18"" document=""1"" />
            <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0x9"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""C"" name="".ctor"" parameterNames=""original"">
          <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" parameterNames=""X"" />
          </customDebugInfo>
          <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x13"" startLine=""3"" startColumn=""8"" endLine=""3"" endColumn=""9"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name="".ctor"" parameterNames=""X"">
          <customDebugInfo>
            <forward declaringType=""C"" methodName="".ctor"" parameterNames=""X"" />
            <encLocalSlotMap>
              <slot kind=""30"" offset=""-1"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""-1"" />
              <lambda offset=""-2"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x6"" startLine=""11"" startColumn=""19"" endLine=""11"" endColumn=""46"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x26"">
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x26"" attributes=""0"" />
          </scope>
        </method>
        <method containingType=""D"" name=""F"" parameterNames=""x, p"">
          <customDebugInfo>
            <forward declaringType=""C"" methodName="".ctor"" parameterNames=""X"" />
            <encLocalSlotMap>
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""15"" document=""1"" />
            <entry offset=""0x4"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""22"" document=""1"" />
            <entry offset=""0xa"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D"" name="".ctor"" parameterNames=""original"">
          <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x8"" startLine=""11"" startColumn=""8"" endLine=""11"" endColumn=""9"" document=""1"" />
          </sequencePoints>
        </method>
        <method containingType=""D+&lt;&gt;c__DisplayClass0_0"" name=""&lt;.ctor&gt;b__0"">
          <customDebugInfo>
            <forward declaringType=""C"" methodName="".ctor"" parameterNames=""X"" />
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""11"" startColumn=""44"" endLine=""11"" endColumn=""45"" document=""1"" />
          </sequencePoints>
        </method>
      </methods>
    </symbols>", format: CodeAnalysis.Emit.DebugInformationFormat.Pdb);
        }

        [Fact]
        [WorkItem(32352, "https://github.com/dotnet/roslyn/issues/32352")]
        public void ClosureAllocationSequencePoints()
        {
            var source = @"
using System;

class C
{
    void F(bool outer)
    {
        if (outer) 
        {
            var inner = false;
            var f = new Func<bool>(() => inner & outer);
        }
    }
}";
            var c = CompileAndVerify(source, options: TestOptions.DebugDll);

            // TODO: https://github.com/dotnet/roslyn/issues/32352
            // The inner closure allocation on IL_0018 should be within the sequence point associated with opening brace of the if statement.
            // This sequence point is currently on  IL_0025, which causes NRE when the instruction pointer is moved to the opening brace.

            c.VerifyIL("C.F", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                bool V_1,
                C.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                System.Func<bool> V_3) //f
  // sequence point: <hidden>
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""bool C.<>c__DisplayClass0_0.outer""
  // sequence point: {
  IL_000d:  nop
  // sequence point: if (outer)
  IL_000e:  ldloc.0
  IL_000f:  ldfld      ""bool C.<>c__DisplayClass0_0.outer""
  IL_0014:  stloc.1
  // sequence point: <hidden>
  IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_003b
  // sequence point: <hidden>
  IL_0018:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
  IL_001d:  stloc.2
  IL_001e:  ldloc.2
  IL_001f:  ldloc.0
  IL_0020:  stfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  // sequence point: {
  IL_0025:  nop
  // sequence point: var inner = false;
  IL_0026:  ldloc.2
  IL_0027:  ldc.i4.0
  IL_0028:  stfld      ""bool C.<>c__DisplayClass0_1.inner""
  // sequence point: var f = new Func<bool>(() => inner & outer);
  IL_002d:  ldloc.2
  IL_002e:  ldftn      ""bool C.<>c__DisplayClass0_1.<F>b__0()""
  IL_0034:  newobj     ""System.Func<bool>..ctor(object, System.IntPtr)""
  IL_0039:  stloc.3
  // sequence point: }
  IL_003a:  nop
  // sequence point: }
  IL_003b:  ret
}
", displaySequencePoints: true, useEnhancedSequencePointDisplay: true);
        }
    }
}
