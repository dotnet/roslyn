// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBIteratorTests : CSharpPDBTestBase
    {
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Foo&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""21"" end_row=""9"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""23"" end_row=""9"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;Foo&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""F"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Foo&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""21"" end_row=""9"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""23"" end_row=""9"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;Foo&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""F"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Foo"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;Foo&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""21"" end_row=""9"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""23"" end_row=""9"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;Foo&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""F"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3b"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.ReleaseDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""IEI"" parameterNames=""i0, i1"">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;IEI&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""23"" end_row=""17"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;IEI&gt;d__1`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""28"" count=""2"">
          <slot startOffset=""0x2a"" endOffset=""0xb4"" />
          <slot startOffset=""0x6e"" endOffset=""0xb2"" />
        </hoistedLocalScopes>
      </customDebugInfo>
      <sequencepoints total=""12"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x36"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x4b"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x52"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x67"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x6e"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7a"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x8f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x96"" start_row=""12"" start_column=""13"" end_row=""12"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xab"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xb2"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Program"" name=""IEI"" parameterNames=""i0, i1"">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""28"" name=""&lt;IEI&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""101"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Program"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""21"" end_row=""17"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""23"" end_row=""17"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Program+&lt;IEI&gt;d__1`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Program"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""28"" count=""2"">
          <slot startOffset=""0x3b"" endOffset=""0xd8"" />
          <slot startOffset=""0x84"" endOffset=""0xd1"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
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
      <locals />
    </method>
  </methods>
</symbols>");
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Test`1"" name=""M"" parameterNames=""items"">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""0"" offset=""13"" />
          <slot kind=""5"" offset=""42"" />
          <slot kind=""0"" offset=""42"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Test`1"" name=""F"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""19"" start_column=""21"" end_row=""19"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""19"" start_column=""23"" end_row=""19"" end_column=""24"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""Test`1+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""36"" count=""3"">
          <slot startOffset=""0x32"" endOffset=""0xe2"" />
          <slot startOffset=""0x0"" endOffset=""0x0"" />
          <slot startOffset=""0x5b"" endOffset=""0xa5"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""temp"" />
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""17"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x3f"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""11"" start_column=""28"" end_row=""11"" end_column=""33"" file_ref=""0"" />
        <entry il_offset=""0x59"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5b"" start_row=""11"" start_column=""18"" end_row=""11"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x6c"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x6d"" start_row=""13"" start_column=""13"" end_row=""13"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x79"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x90"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x98"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xa5"" start_row=""11"" start_column=""25"" end_row=""11"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0xc0"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xd7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xde"" start_row=""17"" start_column=""5"" end_row=""17"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0xe2"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
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
            using (new CultureContext("en-US"))
            {
                var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.ReleaseExe);
                c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
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
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""21"" start_column=""27"" end_row=""21"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x10"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""21"" start_column=""18"" end_row=""21"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x18"" start_row=""23"" start_column=""13"" end_row=""23"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""21"" start_column=""24"" end_row=""21"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x27"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""25"" start_column=""5"" end_row=""25"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x32"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x26"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x3f"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x60"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x67"" start_row=""14"" start_column=""13"" end_row=""14"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x80"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x87"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""21"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <constant name=""d1"" value=""0.1"" type=""Decimal"" />
        <constant name=""dx"" value=""1.23"" type=""Decimal"" />
        <constant name=""d2"" value=""0.2"" type=""Decimal"" />
      </locals>
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

        [WorkItem(543490, "DevDiv")]
        [Fact()]
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
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugExe);
            c.VerifyPdb(@"
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
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""48"" name=""&lt;GetEnumerator&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""5"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""5"" offset=""104"" />
          <slot kind=""0"" offset=""104"" />
        </encLocalSlotMap>
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
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""44"" name=""&lt;IterMethod&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""5"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
        </encLocalSlotMap>
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
        <entry il_offset=""0x22"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""47"" start_column=""5"" end_row=""47"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x2e"">
        <scope startOffset=""0xf"" endOffset=""0x18"">
          <local name=""v"" il_index=""1"" il_start=""0xf"" il_end=""0x18"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""Test`1+&lt;GetEnumerator&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""44"" count=""4"">
          <slot startOffset=""0x0"" endOffset=""0x0"" />
          <slot startOffset=""0x54"" endOffset=""0x95"" />
          <slot startOffset=""0x0"" endOffset=""0x0"" />
          <slot startOffset=""0xd1"" endOffset=""0x10f"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""temp"" />
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""22"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x32"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x33"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""15"" start_column=""27"" end_row=""15"" end_column=""40"" file_ref=""0"" />
        <entry il_offset=""0x52"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x54"" start_row=""15"" start_column=""18"" end_row=""15"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x65"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x66"" start_row=""17"" start_column=""13"" end_row=""17"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x80"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x88"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x95"" start_row=""15"" start_column=""24"" end_row=""15"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0xb0"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""16"" file_ref=""0"" />
        <entry il_offset=""0xb1"" start_row=""19"" start_column=""27"" end_row=""19"" end_column=""39"" file_ref=""0"" />
        <entry il_offset=""0xcf"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xd1"" start_row=""19"" start_column=""18"" end_row=""19"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0xe2"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xe3"" start_row=""21"" start_column=""13"" end_row=""21"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0xfa"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x102"" start_row=""22"" start_column=""9"" end_row=""22"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x10f"" start_row=""19"" start_column=""24"" end_row=""19"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x12a"" start_row=""23"" start_column=""5"" end_row=""23"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x12e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Test`1+&lt;get_IterProp&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
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
      <locals />
    </method>
    <method containingType=""Test`1+&lt;IterMethod&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""Test`1"" methodName=""System.Collections.IEnumerable.GetEnumerator"" parameterNames="""" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
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
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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
                }, module.GetFieldNames("C.<F>d__1"));
            });

            v.VerifyPdb("C+<F>d__1`1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__1`1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""28"" count=""2"">
          <slot startOffset=""0x2c"" endOffset=""0x8b"" />
          <slot startOffset=""0x2c"" endOffset=""0x8b"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x34"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""26"" file_ref=""0"" />
        <entry il_offset=""0x40"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x57"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x5e"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x80"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x87"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x8b"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (int V_0,
                bool V_1,
                bool V_2)
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
  IL_0012:  br.s       IL_001a
  IL_0014:  br.s       IL_003e
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__1.<>1__state""
  IL_0021:  nop
  IL_0022:  call       ""bool C.B()""
  IL_0027:  stloc.2
  IL_0028:  ldloc.2
  IL_0029:  brfalse.s  IL_0046
  IL_002b:  nop
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  stfld      ""int C.<F>d__1.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int C.<F>d__1.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_0018
  IL_003e:  ldarg.0
  IL_003f:  ldc.i4.m1
  IL_0040:  stfld      ""int C.<F>d__1.<>1__state""
  IL_0045:  nop
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.1
  IL_0048:  br.s       IL_0018
}
");

            v.VerifyPdb("C+<F>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""16"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x28"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2b"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""11"" start_column=""13"" end_row=""11"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x3e"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x45"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x46"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x4a"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""disposable"">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""48"">
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
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DisplayClass_AccrossSuspensionPoints_Debug()
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            // One iterator local entry for the lambda local.
            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C+&lt;&gt;c__DisplayClass0"" methodName=""&lt;M&gt;b__1"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x32"" endOffset=""0xfc"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
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
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            // TODO: Currently we don't have means neccessary to pass information about the display 
            // class being pushed on evaluation stack, so that EE could find the locals.
            // Thus the locals are not available in EE.

            var v = CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyIL("C.<M>d__1.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__1.<>1__state""
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
  IL_0012:  stfld      ""int C.<M>d__1.<>1__state""
  IL_0017:  newobj     ""C.<>c__DisplayClass0..ctor()""
  IL_001c:  dup
  IL_001d:  ldc.i4.1
  IL_001e:  stfld      ""byte C.<>c__DisplayClass0.x1""
  IL_0023:  dup
  IL_0024:  ldc.i4.1
  IL_0025:  stfld      ""byte C.<>c__DisplayClass0.x2""
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  stfld      ""byte C.<>c__DisplayClass0.x3""
  IL_0031:  ldftn      ""void C.<>c__DisplayClass0.<M>b__1()""
  IL_0037:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003c:  callvirt   ""void System.Action.Invoke()""
  IL_0041:  ldarg.0
  IL_0042:  ldc.i4.1
  IL_0043:  stfld      ""int C.<M>d__1.<>2__current""
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.1
  IL_004a:  stfld      ""int C.<M>d__1.<>1__state""
  IL_004f:  ldc.i4.1
  IL_0050:  ret
  IL_0051:  ldarg.0
  IL_0052:  ldc.i4.m1
  IL_0053:  stfld      ""int C.<M>d__1.<>1__state""
  IL_0058:  ldc.i4.0
  IL_0059:  ret
}
");

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""9"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x17"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x2a"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x31"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x41"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x51"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x58"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
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
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyIL("C.<M>d__1.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      133 (0x85)
  .maxstack  2
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_001a
  IL_0014:  br.s       IL_007a
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<M>d__1.<>1__state""
  IL_0021:  ldarg.0
  IL_0022:  newobj     ""C.<>c__DisplayClass0..ctor()""
  IL_0027:  stfld      ""C.<>c__DisplayClass0 C.<M>d__1.<>8__1""
  IL_002c:  nop
  IL_002d:  ldarg.0
  IL_002e:  ldfld      ""C.<>c__DisplayClass0 C.<M>d__1.<>8__1""
  IL_0033:  ldc.i4.1
  IL_0034:  stfld      ""byte C.<>c__DisplayClass0.x1""
  IL_0039:  ldarg.0
  IL_003a:  ldfld      ""C.<>c__DisplayClass0 C.<M>d__1.<>8__1""
  IL_003f:  ldc.i4.1
  IL_0040:  stfld      ""byte C.<>c__DisplayClass0.x2""
  IL_0045:  ldarg.0
  IL_0046:  ldfld      ""C.<>c__DisplayClass0 C.<M>d__1.<>8__1""
  IL_004b:  ldc.i4.1
  IL_004c:  stfld      ""byte C.<>c__DisplayClass0.x3""
  IL_0051:  ldarg.0
  IL_0052:  ldfld      ""C.<>c__DisplayClass0 C.<M>d__1.<>8__1""
  IL_0057:  ldftn      ""void C.<>c__DisplayClass0.<M>b__1()""
  IL_005d:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0062:  callvirt   ""void System.Action.Invoke()""
  IL_0067:  nop
  IL_0068:  ldarg.0
  IL_0069:  ldc.i4.1
  IL_006a:  stfld      ""int C.<M>d__1.<>2__current""
  IL_006f:  ldarg.0
  IL_0070:  ldc.i4.1
  IL_0071:  stfld      ""int C.<M>d__1.<>1__state""
  IL_0076:  ldc.i4.1
  IL_0077:  stloc.1
  IL_0078:  br.s       IL_0018
  IL_007a:  ldarg.0
  IL_007b:  ldc.i4.m1
  IL_007c:  stfld      ""int C.<M>d__1.<>1__state""
  IL_0081:  ldc.i4.0
  IL_0082:  stloc.1
  IL_0083:  br.s       IL_0018
}
");

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x21"" endOffset=""0x85"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""10"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x2c"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x2d"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x39"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x45"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x51"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""47"" file_ref=""0"" />
        <entry il_offset=""0x68"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7a"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x81"" start_row=""19"" start_column=""5"" end_row=""19"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [Fact]
        public void DynamicLocal_AccrossSuspensionPoints_Debug()
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var v = CompileAndVerify(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<d>5__1"
                }, module.GetFieldNames("C.<M>d__1"));
            });

            // CHANGE: Dev12 emits a <dynamiclocal> entry for "d", but gives it slot "-1", preventing it from matching
            // any locals when consumed by the EE (i.e. it has no effect).  See FUNCBRECEE::IsLocalDynamic.
            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x21"" endOffset=""0xec"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
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
      <locals />
    </method>
  </methods>
</symbols>");

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forwardIterator version=""4"" kind=""ForwardIterator"" size=""24"" name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""0"" />
      <locals />
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(836491, "DevDiv")]
        [WorkItem(827337, "DevDiv")]
        [WorkItem(1070519, "DevDiv")]
        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var v = CompileAndVerify(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <dynamicLocals version=""4"" kind=""DynamicLocals"" size=""212"" bucketCount=""1"">
          <bucket flagCount=""1"" flags=""1"" slotId=""1"" localName=""d"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x1e"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x6d"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x74"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""d"" il_index=""1"" il_start=""0x17"" il_end=""0x76"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x76"">
        <scope startOffset=""0x17"" endOffset=""0x76"">
          <local name=""d"" il_index=""1"" il_start=""0x17"" il_end=""0x76"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(1070519, "DevDiv")]
        [Fact]
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

    public void F() { } // needs to be present to work around SymWriter bug #1068894
}
";
            var v = CompileAndVerify(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<d>5__1",
                }, module.GetFieldNames("C.<M>d__1"));
            });

            v.VerifyPdb("C+<M>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;M&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""3"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
        <hoistedLocalScopes version=""4"" kind=""StateMachineHoistedLocalScopes"" size=""20"" count=""1"">
          <slot startOffset=""0x21"" endOffset=""0x91"" />
        </hoistedLocalScopes>
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x21"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""23"" file_ref=""0"" />
        <entry il_offset=""0x2e"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x86"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x8d"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(667579, "DevDiv")]
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
            var c = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugDll);
            c.VerifyPdb("C+<F>d__1.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__1"" name=""MoveNext"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap version=""4"" kind=""EditAndContinueLocalSlotMap"" size=""12"">
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x19"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>");
        }
    }
}
