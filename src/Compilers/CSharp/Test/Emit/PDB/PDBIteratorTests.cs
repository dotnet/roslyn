// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBIteratorTests : CSharpPDBTestBase
    {
        [WorkItem(543376, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543376")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SimpleIterator1()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Goo()
    {
        yield break;
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Goo"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Goo&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;Goo&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x18"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543376, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543376")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SimpleIterator2()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Goo()
    {
        yield break;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Goo"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Goo&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;Goo&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x18"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543490")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SimpleIterator3()
        {
            var text = @"
class Program
{
    System.Collections.Generic.IEnumerable<int> Goo()
    {
        yield return 1; //hidden sequence point after this.
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Goo"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Goo&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;Goo&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""24"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x37"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void IteratorWithLocals_ReleasePdb()
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
            var c = CompileAndVerify(text, options: TestOptions.ReleaseDll, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>3__i0",
                    "<>3__i1"
                }, module.GetFieldNames("Program.<IEI>d__0"));
            });

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""IEI"" parameterNames=""i0, i1"">
      <customDebugInfo>
        <forwardIterator name=""&lt;IEI&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;IEI&gt;d__0`1"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <hoistedLocalScopes>
          <slot />
          <slot startOffset=""0x2a"" endOffset=""0xb4"" />
          <slot startOffset=""0x6e"" endOffset=""0xb2"" />
        </hoistedLocalScopes>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""20"" document=""1"" />
        <entry offset=""0x36"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0x4b"" hidden=""true"" document=""1"" />
        <entry offset=""0x52"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""1"" />
        <entry offset=""0x67"" hidden=""true"" document=""1"" />
        <entry offset=""0x6e"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x7a"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""28"" document=""1"" />
        <entry offset=""0x8f"" hidden=""true"" document=""1"" />
        <entry offset=""0x96"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""28"" document=""1"" />
        <entry offset=""0xab"" hidden=""true"" document=""1"" />
        <entry offset=""0xb2"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void IteratorWithLocals_DebugPdb()
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

            var c = CompileAndVerify(text, options: TestOptions.DebugDll, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>3__i0",
                    "<>3__i1",
                    "<>4__this",
                }, module.GetFieldNames("Program.<IEI>d__0"));
            });
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""IEI"" parameterNames=""i0, i1"">
      <customDebugInfo>
        <forwardIterator name=""&lt;IEI&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""101"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;IEI&gt;d__0`1"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x39"" endOffset=""0xc6"" />
          <slot startOffset=""0x7e"" endOffset=""0xc4"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x39"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x3a"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""20"" document=""1"" />
        <entry offset=""0x46"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0x62"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""1"" />
        <entry offset=""0x77"" hidden=""true"" document=""1"" />
        <entry offset=""0x7e"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x7f"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x8b"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""28"" document=""1"" />
        <entry offset=""0xa0"" hidden=""true"" document=""1"" />
        <entry offset=""0xa7"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""28"" document=""1"" />
        <entry offset=""0xbc"" hidden=""true"" document=""1"" />
        <entry offset=""0xc3"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc4"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test`1"" name=""M"" parameterNames=""items"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""5"" offset=""42"" />
          <slot kind=""0"" offset=""42"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""Test`1+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x32"" endOffset=""0xe2"" />
          <slot />
          <slot startOffset=""0x5b"" endOffset=""0xa5"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""temp"" />
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x32"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x33"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""28"" document=""1"" />
        <entry offset=""0x3f"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""16"" document=""1"" />
        <entry offset=""0x40"" startLine=""11"" startColumn=""28"" endLine=""11"" endColumn=""33"" document=""1"" />
        <entry offset=""0x59"" hidden=""true"" document=""1"" />
        <entry offset=""0x5b"" startLine=""11"" startColumn=""18"" endLine=""11"" endColumn=""24"" document=""1"" />
        <entry offset=""0x6c"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6d"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""24"" document=""1"" />
        <entry offset=""0x79"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""30"" document=""1"" />
        <entry offset=""0x90"" hidden=""true"" document=""1"" />
        <entry offset=""0x98"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa5"" startLine=""11"" startColumn=""25"" endLine=""11"" endColumn=""27"" document=""1"" />
        <entry offset=""0xc0"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""26"" document=""1"" />
        <entry offset=""0xd7"" hidden=""true"" document=""1"" />
        <entry offset=""0xde"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe2"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xec"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(542705, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542705"), WorkItem(528790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528790"), WorkItem(543490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543490")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
            using (new CultureContext(new CultureInfo("en-US", useUserOverride: false)))
            {
                var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.ReleaseExe);
                c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""21"" startColumn=""27"" endLine=""21"" endColumn=""38"" document=""1"" />
        <entry offset=""0x10"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""21"" startColumn=""18"" endLine=""21"" endColumn=""23"" document=""1"" />
        <entry offset=""0x18"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""41"" document=""1"" />
        <entry offset=""0x1d"" startLine=""21"" startColumn=""24"" endLine=""21"" endColumn=""26"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x31"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x32"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x26"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""25"" document=""1"" />
        <entry offset=""0x3f"" hidden=""true"" document=""1"" />
        <entry offset=""0x46"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""25"" document=""1"" />
        <entry offset=""0x60"" hidden=""true"" document=""1"" />
        <entry offset=""0x67"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""29"" document=""1"" />
        <entry offset=""0x80"" hidden=""true"" document=""1"" />
        <entry offset=""0x87"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""21"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x89"">
        <scope startOffset=""0x26"" endOffset=""0x89"">
          <constant name=""d1"" value=""0.1"" type=""Decimal"" />
          <constant name=""dx"" value=""1.23"" type=""Decimal"" />
          <scope startOffset=""0x67"" endOffset=""0x87"">
            <constant name=""d2"" value=""0.2"" type=""Decimal"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
            }
        }

        [WorkItem(543490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543490")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void IteratorMultipleEnumerables()
        {
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
            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""Test"" methodName=""Main"" />
  <methods>
    <method containingType=""Test`1"" name=""System.Collections.IEnumerable.GetEnumerator"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""32"" document=""1"" />
        <entry offset=""0xa"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xc"">
        <namespace name=""System"" />
        <namespace name=""System.Collections"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""Test`1"" name=""GetEnumerator"">
      <customDebugInfo>
        <forwardIterator name=""&lt;GetEnumerator&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""5"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""5"" offset=""104"" />
          <slot kind=""0"" offset=""104"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""Test`1"" name=""get_IterProp"">
      <customDebugInfo>
        <forwardIterator name=""&lt;get_IterProp&gt;d__3"" />
      </customDebugInfo>
    </method>
    <method containingType=""Test`1"" name=""IterMethod"">
      <customDebugInfo>
        <forwardIterator name=""&lt;IterMethod&gt;d__4"" />
      </customDebugInfo>
    </method>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" />
        <encLocalSlotMap>
          <slot kind=""5"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""45"" startColumn=""5"" endLine=""45"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""46"" startColumn=""9"" endLine=""46"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""46"" startColumn=""27"" endLine=""46"" endColumn=""45"" document=""1"" />
        <entry offset=""0xd"" hidden=""true"" document=""1"" />
        <entry offset=""0xf"" startLine=""46"" startColumn=""18"" endLine=""46"" endColumn=""23"" document=""1"" />
        <entry offset=""0x16"" startLine=""46"" startColumn=""47"" endLine=""46"" endColumn=""48"" document=""1"" />
        <entry offset=""0x17"" startLine=""46"" startColumn=""49"" endLine=""46"" endColumn=""50"" document=""1"" />
        <entry offset=""0x18"" startLine=""46"" startColumn=""24"" endLine=""46"" endColumn=""26"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x2c"" hidden=""true"" document=""1"" />
        <entry offset=""0x2d"" startLine=""47"" startColumn=""5"" endLine=""47"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2e"">
        <scope startOffset=""0xf"" endOffset=""0x18"">
          <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test`1+&lt;GetEnumerator&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" />
        <hoistedLocalScopes>
          <slot />
          <slot startOffset=""0x54"" endOffset=""0x95"" />
          <slot />
          <slot startOffset=""0xd1"" endOffset=""0x10f"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""temp"" />
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x32"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x33"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""16"" document=""1"" />
        <entry offset=""0x34"" startLine=""15"" startColumn=""27"" endLine=""15"" endColumn=""40"" document=""1"" />
        <entry offset=""0x52"" hidden=""true"" document=""1"" />
        <entry offset=""0x54"" startLine=""15"" startColumn=""18"" endLine=""15"" endColumn=""23"" document=""1"" />
        <entry offset=""0x65"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0x66"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""28"" document=""1"" />
        <entry offset=""0x80"" hidden=""true"" document=""1"" />
        <entry offset=""0x88"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""1"" />
        <entry offset=""0x95"" startLine=""15"" startColumn=""24"" endLine=""15"" endColumn=""26"" document=""1"" />
        <entry offset=""0xb0"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""16"" document=""1"" />
        <entry offset=""0xb1"" startLine=""19"" startColumn=""27"" endLine=""19"" endColumn=""39"" document=""1"" />
        <entry offset=""0xcf"" hidden=""true"" document=""1"" />
        <entry offset=""0xd1"" startLine=""19"" startColumn=""18"" endLine=""19"" endColumn=""23"" document=""1"" />
        <entry offset=""0xe2"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe3"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""28"" document=""1"" />
        <entry offset=""0xfa"" hidden=""true"" document=""1"" />
        <entry offset=""0x102"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x10f"" startLine=""19"" startColumn=""24"" endLine=""19"" endColumn=""26"" document=""1"" />
        <entry offset=""0x12a"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x12e"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test`1+&lt;get_IterProp&gt;d__3"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2b"" startLine=""29"" startColumn=""13"" endLine=""29"" endColumn=""31"" document=""1"" />
        <entry offset=""0x40"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" startLine=""30"" startColumn=""13"" endLine=""30"" endColumn=""31"" document=""1"" />
        <entry offset=""0x5c"" hidden=""true"" document=""1"" />
        <entry offset=""0x63"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test`1+&lt;IterMethod&gt;d__4"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""35"" startColumn=""5"" endLine=""35"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2b"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""33"" document=""1"" />
        <entry offset=""0x40"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" startLine=""37"" startColumn=""9"" endLine=""37"" endColumn=""27"" document=""1"" />
        <entry offset=""0x5c"" hidden=""true"" document=""1"" />
        <entry offset=""0x63"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void VariablesWithSubstitutedType1()
        {
            var text = @"
using System.Collections.Generic;
class C
{
    static IEnumerable<T> F<T>(T[] o)
    {
        int i = 0;
        T t = default(T);
        yield return t;
        yield return o[i];
    }
}
";

            var v = CompileAndVerify(text, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "o", "<>3__o",
                    "<i>5__1",
                    "<t>5__2"
                }, module.GetFieldNames("C.<F>d__0"));
            });

            v.VerifyPdb("C+<F>d__0`1.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0`1"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x2a"" endOffset=""0x83"" />
          <slot startOffset=""0x2a"" endOffset=""0x83"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2b"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" document=""1"" />
        <entry offset=""0x32"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0x3e"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""24"" document=""1"" />
        <entry offset=""0x53"" hidden=""true"" document=""1"" />
        <entry offset=""0x5a"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""27"" document=""1"" />
        <entry offset=""0x7a"" hidden=""true"" document=""1"" />
        <entry offset=""0x81"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x83"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void IteratorWithConditionalBranchDiscriminator1()
        {
            var text = @"
using System.Collections.Generic;
class C
{
    static bool B() => false;

    static IEnumerable<int> F()
    {
        if (B())
        {
            yield return 1;
        }
    }
}
";
            // Note that conditional branch discriminator is not hoisted.

            var v = CompileAndVerify(text, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                }, module.GetFieldNames("C.<F>d__1"));
            });

            v.VerifyIL("C.<F>d__1.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003a
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__1.<>1__state""
  IL_001f:  nop
  IL_0020:  call       ""bool C.B()""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  brfalse.s  IL_0042
  IL_0029:  nop
  IL_002a:  ldarg.0
  IL_002b:  ldc.i4.1
  IL_002c:  stfld      ""int C.<F>d__1.<>2__current""
  IL_0031:  ldarg.0
  IL_0032:  ldc.i4.1
  IL_0033:  stfld      ""int C.<F>d__1.<>1__state""
  IL_0038:  ldc.i4.1
  IL_0039:  ret
  IL_003a:  ldarg.0
  IL_003b:  ldc.i4.m1
  IL_003c:  stfld      ""int C.<F>d__1.<>1__state""
  IL_0041:  nop
  IL_0042:  ldc.i4.0
  IL_0043:  ret
}");

            v.VerifyPdb("C+<F>d__1.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""B"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""17"" document=""1"" />
        <entry offset=""0x26"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2a"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""28"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x41"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x42"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SynthesizedVariables1()
        {
            var source =
@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> M(IDisposable disposable)
    {
        foreach (var item in new[] { 1, 2, 3 }) { lock (this) { yield return 1; } }
        foreach (var item in new[] { 1, 2, 3 }) { }
        lock (this) { yield return 2; }
        if (disposable != null) { using (disposable) { yield return 3; } }
        lock (this) { yield return 4; }
        if (disposable != null) { using (disposable) { } }
        lock (this) { }
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<>4__this",
                    "disposable",
                    "<>3__disposable",
                    "<>7__wrap1",
                    "<>7__wrap2",
                    "<>7__wrap3",
                    "<>7__wrap4",
                    "<>7__wrap5",
                }, module.GetFieldNames("C.<M>d__0"));
            });

            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "disposable",
                    "<>3__disposable",
                    "<>4__this",
                    "<>s__1",
                    "<>s__2",
                    "<item>5__3",
                    "<>s__4",
                    "<>s__5",
                    "<>s__6",
                    "<>s__7",
                    "<item>5__8",
                    "<>s__9",
                    "<>s__10",
                    "<>s__11",
                    "<>s__12",
                    "<>s__13",
                    "<>s__14",
                    "<>s__15",
                    "<>s__16"
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""disposable"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""3"" offset=""53"" />
          <slot kind=""2"" offset=""53"" />
          <slot kind=""6"" offset=""96"" />
          <slot kind=""8"" offset=""96"" />
          <slot kind=""0"" offset=""96"" />
          <slot kind=""3"" offset=""149"" />
          <slot kind=""2"" offset=""149"" />
          <slot kind=""4"" offset=""216"" />
          <slot kind=""3"" offset=""266"" />
          <slot kind=""2"" offset=""266"" />
          <slot kind=""4"" offset=""333"" />
          <slot kind=""3"" offset=""367"" />
          <slot kind=""2"" offset=""367"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DisplayClass_AcrossSuspensionPoints_Debug()
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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<>8__1",                   // hoisted display class
                }, module.GetFieldNames("C.<M>d__0"));
            });

            // One iterator local entry for the lambda local.
            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x30"" endOffset=""0xeb"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x3b"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x3c"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x48"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x54"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x60"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x77"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""35"" document=""1"" />
        <entry offset=""0xa9"" hidden=""true"" document=""1"" />
        <entry offset=""0xb0"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""35"" document=""1"" />
        <entry offset=""0xe2"" hidden=""true"" document=""1"" />
        <entry offset=""0xe9"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DisplayClass_InBetweenSuspensionPoints_Release()
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
            // TODO: Currently we don't have means necessary to pass information about the display 
            // class being pushed on evaluation stack, so that EE could find the locals.
            // Thus the locals are not available in EE.

            var v = CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyIL("C.<M>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0051
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0017:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_001c:  dup
  IL_001d:  ldc.i4.1
  IL_001e:  stfld      ""byte C.<>c__DisplayClass0_0.x1""
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  stfld      ""byte C.<>c__DisplayClass0_0.x2""
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  stfld      ""byte C.<>c__DisplayClass0_0.x3""
  IL_0031:  ldftn      ""void C.<>c__DisplayClass0_0.<M>b__0()""
  IL_0037:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003c:  callvirt   ""void System.Action.Invoke()""
  IL_0041:  ldarg.0
  IL_0042:  ldc.i4.1
  IL_0043:  stfld      ""int C.<M>d__0.<>2__current""
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.1
  IL_004a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_004f:  ldc.i4.1
  IL_0050:  ret
  IL_0051:  ldarg.0
  IL_0052:  ldc.i4.m1
  IL_0053:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0058:  ldc.i4.0
  IL_0059:  ret
}
");

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x2a"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x31"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x41"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x51"" hidden=""true"" document=""1"" />
        <entry offset=""0x58"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DisplayClass_InBetweenSuspensionPoints_Debug()
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

        // Possible EnC edit - add lambda:
        // () => { x1 }
    }
}
";
            // We need to hoist display class variable to allow adding a new lambda after yield return 
            // that shares closure with the existing lambda.

            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<>8__1",                   // hoisted display class
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyIL("C.<M>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      127 (0x7f)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0076
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldarg.0
  IL_0020:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0025:  stfld      ""C.<>c__DisplayClass0_0 C.<M>d__0.<>8__1""
  IL_002a:  nop
  IL_002b:  ldarg.0
  IL_002c:  ldfld      ""C.<>c__DisplayClass0_0 C.<M>d__0.<>8__1""
  IL_0031:  ldc.i4.1
  IL_0032:  stfld      ""byte C.<>c__DisplayClass0_0.x1""
  IL_0037:  ldarg.0
  IL_0038:  ldfld      ""C.<>c__DisplayClass0_0 C.<M>d__0.<>8__1""
  IL_003d:  ldc.i4.1
  IL_003e:  stfld      ""byte C.<>c__DisplayClass0_0.x2""
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""C.<>c__DisplayClass0_0 C.<M>d__0.<>8__1""
  IL_0049:  ldc.i4.1
  IL_004a:  stfld      ""byte C.<>c__DisplayClass0_0.x3""
  IL_004f:  ldarg.0
  IL_0050:  ldfld      ""C.<>c__DisplayClass0_0 C.<M>d__0.<>8__1""
  IL_0055:  ldftn      ""void C.<>c__DisplayClass0_0.<M>b__0()""
  IL_005b:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0060:  callvirt   ""void System.Action.Invoke()""
  IL_0065:  nop
  IL_0066:  ldarg.0
  IL_0067:  ldc.i4.1
  IL_0068:  stfld      ""int C.<M>d__0.<>2__current""
  IL_006d:  ldarg.0
  IL_006e:  ldc.i4.1
  IL_006f:  stfld      ""int C.<M>d__0.<>1__state""
  IL_0074:  ldc.i4.1
  IL_0075:  ret
  IL_0076:  ldarg.0
  IL_0077:  ldc.i4.m1
  IL_0078:  stfld      ""int C.<M>d__0.<>1__state""
  IL_007d:  ldc.i4.0
  IL_007e:  ret
}");

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c__DisplayClass0_0"" methodName=""&lt;M&gt;b__0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x1f"" endOffset=""0x7f"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2b"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x37"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x43"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""21"" document=""1"" />
        <entry offset=""0x4f"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x66"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""24"" document=""1"" />
        <entry offset=""0x76"" hidden=""true"" document=""1"" />
        <entry offset=""0x7d"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""95"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DynamicLocal_AcrossSuspensionPoints_Debug()
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
            var v = CompileAndVerify(source, new[] { CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<d>5__1"
                }, module.GetFieldNames("C.<M>d__0"));
            });

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x1f"" endOffset=""0xe3"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x2c"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""24"" document=""1"" />
        <entry offset=""0x82"" hidden=""true"" document=""1"" />
        <entry offset=""0x89"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0xe1"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe3"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
            v.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")]
        [WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337")]
        [WorkItem(1070519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070519")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DynamicLocal_InBetweenSuspensionPoints_Release()
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
            var v = CompileAndVerify(source, new[] { CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1e"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""24"" document=""1"" />
        <entry offset=""0x6d"" hidden=""true"" document=""1"" />
        <entry offset=""0x74"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x76"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x17"" endOffset=""0x76"">
          <local name=""d"" il_index=""1"" il_start=""0x17"" il_end=""0x76"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(1070519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070519")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DynamicLocal_InBetweenSuspensionPoints_Debug()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
        dynamic d = 1;
        yield return d;

        // Possible EnC edit:
        // System.Console.WriteLine(d);
    }
}
";
            var v = CompileAndVerify(source, new[] { CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<d>5__1",
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x1f"" endOffset=""0x8b"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x2c"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""24"" document=""1"" />
        <entry offset=""0x82"" hidden=""true"" document=""1"" />
        <entry offset=""0x89"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8b"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(667579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667579")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DebuggerHiddenIterator()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    static void Main(string[] args)
    {
        foreach (var x in F()) ;
    }

    [DebuggerHidden]
    static IEnumerable<int> F()
    {
        throw new Exception();
        yield break;
    }
}";
            var c = CreateCompilationWithMscorlib40AndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb("C+<F>d__1.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
        <entry offset=""0x18"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""31"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(8473, "https://github.com/dotnet/roslyn/issues/8473")]
        public void PortableStateMachineDebugInfo()
        {
            string src = @"
using System.Collections.Generic;
public class C
{
    IEnumerable<int> M() { yield return 1; }
}";
            var compilation = CreateCompilation(src, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();

            var peStream = new MemoryStream();
            var pdbStream = new MemoryStream();

            var result = compilation.Emit(
               peStream,
               pdbStream,
               options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));

            pdbStream.Position = 0;
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
            {
                var mdReader = provider.GetMetadataReader();
                var writer = new StringWriter();
                var visualizer = new MetadataVisualizer(mdReader, writer);
                visualizer.WriteMethodDebugInformation();

                AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
MethodDebugInformation (index: 0x31, size: 40): 
==================================================
1: nil
2: nil
3: nil
4: nil
5: #22
{
  Kickoff Method: 0x06000001 (MethodDef)
  Locals: 0x11000001 (StandAloneSig)
  Document: #1
  IL_0000: <hidden>
  IL_001F: (5, 26) - (5, 27)
  IL_0020: (5, 28) - (5, 43)
  IL_0030: <hidden>
  IL_0037: (5, 44) - (5, 45)
}
6: nil
7: nil
8: nil
9: nil
a: nil
",
                    writer.ToString());
            }
        }
    }
}
