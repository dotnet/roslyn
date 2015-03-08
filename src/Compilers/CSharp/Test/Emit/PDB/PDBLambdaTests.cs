// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBLambdaTests : CSharpPDBTestBase
    {
        [WorkItem(539898, "DevDiv")]
        [Fact]
        public void SequencePoints_Body()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""23"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""38"" document=""0"" />
        <entry offset=""0x21"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""13"" document=""0"" />
        <entry offset=""0x28"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""d"" il_index=""0"" il_start=""0x0"" il_end=""0x29"" attributes=""0"" />
      </locals>
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
        <entry offset=""0x0"" startLine=""8"" startColumn=""21"" endLine=""8"" endColumn=""37"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void Nested()
        {
            var source = @"
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
  <entryPoint declaringType=""Test"" methodName=""Main"" />
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""12"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""10"" endLine=""7"" endColumn=""25"" document=""0"" />
        <entry offset=""0xf"" hidden=""true"" document=""0"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""22"" document=""0"" />
        <entry offset=""0x16"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""0"" />
        <entry offset=""0x1a"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
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
          <slot kind=""temp"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""56"" />
          <lambda offset=""56"" closure=""0"" />
          <lambda offset=""136"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""22"" endColumn=""11"" document=""0"" />
        <entry offset=""0x1b"" startLine=""23"" startColumn=""9"" endLine=""23"" endColumn=""22"" document=""0"" />
        <entry offset=""0x25"" startLine=""24"" startColumn=""5"" endLine=""24"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_0"" name=""&lt;M&gt;b__0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""54"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x14"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""0"" />
        <entry offset=""0x15"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""23"" document=""0"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""13"" endLine=""20"" endColumn=""15"" document=""0"" />
        <entry offset=""0x29"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""26"" document=""0"" />
        <entry offset=""0x33"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_1"" name=""&lt;M&gt;b__1"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""14"" document=""0"" />
        <entry offset=""0x1"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""38"" document=""0"" />
        <entry offset=""0x1f"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""14"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void InitialSequencePoints()
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
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""0"" />
        <entry offset=""0xe"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""39"" document=""0"" />
        <entry offset=""0x1b"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""14"" document=""0"" />
        <entry offset=""0x22"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass0_0"" name=""&lt;Foo&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Foo"" parameterNames=""p"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""37"" endLine=""6"" endColumn=""38"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(543479, "DevDiv")]
        public void Nested_InitialSequencePoints()
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
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""24"" document=""0"" />
        <entry offset=""0xf"" hidden=""true"" document=""0"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""22"" document=""0"" />
        <entry offset=""0x16"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""0"" />
        <entry offset=""0x1a"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals />
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
          <slot kind=""temp"" />
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
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0xd"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""19"" endColumn=""11"" document=""0"" />
        <entry offset=""0x1b"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""22"" document=""0"" />
        <entry offset=""0x25"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x27"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
        <local name=""f1"" il_index=""1"" il_start=""0x0"" il_end=""0x27"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_0"" name=""&lt;M&gt;b__0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""0"" offset=""54"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x14"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""0"" />
        <entry offset=""0x15"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""23"" document=""0"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""66"" document=""0"" />
        <entry offset=""0x29"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""26"" document=""0"" />
        <entry offset=""0x33"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""f2"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Test+&lt;&gt;c__DisplayClass1_1"" name=""&lt;M&gt;b__1"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""40"" endLine=""17"" endColumn=""41"" document=""0"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""42"" endLine=""17"" endColumn=""63"" document=""0"" />
        <entry offset=""0x1f"" startLine=""17"" startColumn=""64"" endLine=""17"" endColumn=""65"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FieldAndPropertyInitializers()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""B"" name="".ctor"" parameterNames=""f"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""0"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""28"" document=""0"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""29"" endLine=""6"" endColumn=""30"" document=""0"" />
      </sequencePoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""get_P"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""19"" endLine=""13"" endColumn=""23"" document=""0"" />
      </sequencePoints>
      <locals />
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
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""28"" document=""0"" />
        <entry offset=""0x25"" startLine=""13"" startColumn=""28"" endLine=""13"" endColumn=""38"" document=""0"" />
        <entry offset=""0x4a"" startLine=""14"" startColumn=""18"" endLine=""14"" endColumn=""31"" document=""0"" />
        <entry offset=""0x70"" startLine=""14"" startColumn=""32"" endLine=""14"" endColumn=""33"" document=""0"" />
        <entry offset=""0x71"" startLine=""14"" startColumn=""33"" endLine=""14"" endColumn=""34"" document=""0"" />
      </sequencePoints>
      <locals />
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
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""35"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""29"" endLine=""14"" endColumn=""30"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_1"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""26"" endLine=""11"" endColumn=""27"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__5_2"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""34"" endLine=""13"" endColumn=""38"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__6_0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""33"" endLine=""12"" endColumn=""34"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ClosuresInCtor()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""B"" name="".ctor"" parameterNames=""f"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""0"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""28"" document=""0"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""29"" endLine=""6"" endColumn=""30"" document=""0"" />
      </sequencePoints>
      <locals />
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
          <lambda offset=""63"" closure=""this"" />
          <lambda offset=""87"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x27"" hidden=""true"" document=""0"" />
        <entry offset=""0x2d"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""0"" />
        <entry offset=""0x2e"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""19"" document=""0"" />
        <entry offset=""0x35"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""21"" document=""0"" />
        <entry offset=""0x47"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""23"" document=""0"" />
        <entry offset=""0x59"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""21"" document=""0"" />
        <entry offset=""0x6b"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x6c"" attributes=""0"" />
        <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x27"" il_end=""0x6b"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x6c"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x6c"" attributes=""0"" />
        <scope startOffset=""0x27"" endOffset=""0x6b"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x27"" il_end=""0x6b"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name=""&lt;.ctor&gt;b__3_2"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""19"" endLine=""17"" endColumn=""22"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""41"" endLine=""13"" endColumn=""42"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_0"" name=""&lt;.ctor&gt;b__1"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""19"" endLine=""16"" endColumn=""20"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass3_1"" name=""&lt;.ctor&gt;b__3"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""19"" endLine=""18"" endColumn=""20"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Queries1()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x6"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" document=""0"" />
        <entry offset=""0xe"" startLine=""9"" startColumn=""9"" endLine=""12"" endColumn=""31"" document=""0"" />
        <entry offset=""0x79"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
        <local name=""x"" il_index=""1"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x7a"">
        <namespace name=""System.Linq"" />
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
        <local name=""x"" il_index=""1"" il_start=""0x0"" il_end=""0x7a"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;M&gt;b__0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""17"" endLine=""10"" endColumn=""30"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_1"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""23"" endLine=""11"" endColumn=""29"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_2"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""24"" endLine=""12"" endColumn=""30"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Queries_GroupBy1()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics();

            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""11"" endColumn=""40"" document=""0"" />
        <entry offset=""0xe6"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""0"" />
      </sequencePoints>
      <locals>
        <local name=""result"" il_index=""0"" il_start=""0x0"" il_end=""0xe7"" attributes=""0"" />
      </locals>
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
        <entry offset=""0x0"" startLine=""9"" startColumn=""52"" endLine=""9"" endColumn=""57"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_1"" parameterNames=""b"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""65"" endLine=""9"" endColumn=""70"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_2"" parameterNames=""a, b"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""70"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_3"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""57"" endLine=""10"" endColumn=""74"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_4"" parameterNames=""&lt;&gt;h__TransparentIdentifier0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""33"" endLine=""10"" endColumn=""53"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;F&gt;b__0_5"" parameterNames=""d"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""34"" endLine=""11"" endColumn=""39"" document=""0"" />
      </sequencePoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }
    }
}
