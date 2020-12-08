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
    public class PDBLocalFunctionTests : CSharpPDBTestBase
    {
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
    int r;

    public C(int a, int b) : base(() => a) 
    {
        int c = 1;
        int f() => b;
        int g() => f();
        int h() => c;
        r = g() + h();
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
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""-1"" />
          <closure offset=""0"" />
          <lambda offset=""-2"" closure=""0"" />
          <lambda offset=""42"" closure=""0"" />
          <lambda offset=""65"" closure=""0"" />
          <lambda offset=""90"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""30"" endLine=""13"" endColumn=""43"" document=""1"" />
        <entry offset=""0x27"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x28"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""19"" document=""1"" />
        <entry offset=""0x33"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""23"" document=""1"" />
        <entry offset=""0x47"" startLine=""20"" startColumn=""5"" endLine=""20"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x48"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x48"" attributes=""0"" />
        <scope startOffset=""0x27"" endOffset=""0x48"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x27"" il_end=""0x48"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name=""&lt;.ctor&gt;g__h|1_3"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""20"" endLine=""18"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""41"" endLine=""13"" endColumn=""42"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1_0"" name=""&lt;.ctor&gt;g__f|1"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""20"" endLine=""16"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass1_0"" name=""&lt;.ctor&gt;g__g|2"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""f"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""20"" endLine=""17"" endColumn=""23"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
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
            int f0(int a) => x0;
            int f1(int a) => x1;
            G(f0);   
            G(f1);
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
          <lambda offset=""226"" closure=""0"" />
          <lambda offset=""260"" closure=""1"" />
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
        <entry offset=""0x30"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""19"" document=""1"" />
        <entry offset=""0x43"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""19"" document=""1"" />
        <entry offset=""0x56"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x57"" hidden=""true"" document=""1"" />
        <entry offset=""0x5b"" startLine=""10"" startColumn=""25"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x61"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x62"">
        <scope startOffset=""0x11"" endOffset=""0x57"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""2"" il_start=""0x11"" il_end=""0x57"" attributes=""0"" />
          <scope startOffset=""0x20"" endOffset=""0x57"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""3"" il_start=""0x20"" il_end=""0x57"" attributes=""0"" />
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
            int f(int a) => x2;
            G(f); 
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
          <lambda offset=""158"" closure=""1"" />
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
        <entry offset=""0x26"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""18"" document=""1"" />
        <entry offset=""0x39"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3a"" startLine=""10"" startColumn=""34"" endLine=""10"" endColumn=""58"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0x67"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x68"">
        <scope startOffset=""0x1"" endOffset=""0x67"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x67"" attributes=""0"" />
          <scope startOffset=""0x17"" endOffset=""0x3a"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x17"" il_end=""0x3a"" attributes=""0"" />
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
        int f2() => x2;
        G(f2);

        switch (a)
        {
            case 1:
                int x0 = 1;
                int f0() => x0;
                G(f0);
                break;

            case 2:
                int x1 = 1;
                int f1() => x1;
                G(f1);
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
          <slot kind=""30"" offset=""75"" />
          <slot kind=""35"" offset=""75"" />
          <slot kind=""1"" offset=""75"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""75"" />
          <lambda offset=""44"" closure=""0"" />
          <lambda offset=""176"" closure=""1"" />
          <lambda offset=""309"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x6"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""20"" document=""1"" />
        <entry offset=""0xf"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""15"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x2f"" hidden=""true"" document=""1"" />
        <entry offset=""0x31"" hidden=""true"" document=""1"" />
        <entry offset=""0x3d"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""28"" document=""1"" />
        <entry offset=""0x45"" startLine=""21"" startColumn=""17"" endLine=""21"" endColumn=""23"" document=""1"" />
        <entry offset=""0x58"" startLine=""22"" startColumn=""17"" endLine=""22"" endColumn=""23"" document=""1"" />
        <entry offset=""0x5a"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""28"" document=""1"" />
        <entry offset=""0x62"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""23"" document=""1"" />
        <entry offset=""0x75"" startLine=""28"" startColumn=""17"" endLine=""28"" endColumn=""23"" document=""1"" />
        <entry offset=""0x77"" startLine=""30"" startColumn=""5"" endLine=""30"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x78"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x78"" attributes=""0"" />
        <scope startOffset=""0x22"" endOffset=""0x77"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x22"" il_end=""0x77"" attributes=""0"" />
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
        
            object f0() => x0;
            object f1() => x1;
            object g0() => y0;
            G(f0);
            G(g0);
            G(f1);
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
          <lambda offset=""154"" closure=""0"" />
          <lambda offset=""186"" closure=""1"" />
          <lambda offset=""218"" closure=""0"" />
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
        <entry offset=""0x2e"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""19"" document=""1"" />
        <entry offset=""0x40"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""19"" document=""1"" />
        <entry offset=""0x52"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""19"" document=""1"" />
        <entry offset=""0x64"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x67"" hidden=""true"" document=""1"" />
        <entry offset=""0x7b"" hidden=""true"" document=""1"" />
        <entry offset=""0x7c"" hidden=""true"" document=""1"" />
        <entry offset=""0x7e"" hidden=""true"" document=""1"" />
        <entry offset=""0x92"" hidden=""true"" document=""1"" />
        <entry offset=""0x93"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x94"">
        <scope startOffset=""0x1"" endOffset=""0x93"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x93"" attributes=""0"" />
          <scope startOffset=""0x1d"" endOffset=""0x65"">
            <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0x1d"" il_end=""0x65"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }
    }
}
