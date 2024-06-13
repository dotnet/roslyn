// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class LocalSlotMappingTests : EditAndContinueTestBase
    {
        /// <summary>
        /// If no changes were made we don't produce a syntax map.
        /// If we don't have syntax map and preserve variables is true we should still successfully map the locals to their previous slots.
        /// </summary>
        [Fact]
        public void SlotMappingWithNoChanges()
        {
            var source0 = @"
using System;

class C
{
    static void Main(string[] args)
    {
        var b = true;
        do
        {
            Console.WriteLine(""hi"");
        } while (b == true);
    }
}
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source0);

            var v0 = CompileAndVerify(compilation0);

            var methodData0 = v0.TestData.GetMethodData("C.Main");
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var method1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            v0.VerifyIL("C.Main", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  .locals init (bool V_0, //b
                bool V_1)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
 -IL_0003:  nop
 -IL_0004:  ldstr      ""hi""
  IL_0009:  call       ""void System.Console.WriteLine(string)""
  IL_000e:  nop
 -IL_000f:  nop
 -IL_0010:  ldloc.0
  IL_0011:  stloc.1
 ~IL_0012:  ldloc.1
  IL_0013:  brtrue.s   IL_0003
 -IL_0015:  ret
}", sequencePoints: "C.Main");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, syntaxMap: null, preserveLocalVariables: true)));

            diff1.VerifyIL("C.Main", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  .locals init (bool V_0, //b
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldstr      ""hi""
  IL_0009:  call       ""void System.Console.WriteLine(string)""
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  brtrue.s   IL_0003
  IL_0015:  ret

}");
        }

        [Fact]
        public void OutOfOrderUserLocals()
        {
            var source = WithWindowsLineBreaks(@"
using System;

public class C
{
    public static void M()
    {
        for (int i = 1; i < 1; i++) Console.WriteLine(1);
        for (int i = 1; i < 2; i++) Console.WriteLine(2);

        int j;
        for (j = 1; j < 3; j++) Console.WriteLine(3);
    }
}");
            var compilation0 = CreateCompilation(source, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.M", @"
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0, //j
                int V_1, //i
                bool V_2,
                int V_3, //i
                bool V_4,
                bool V_5)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  br.s       IL_0010
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  nop
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.1
  IL_0012:  clt
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  brtrue.s   IL_0005
  IL_0018:  ldc.i4.1
  IL_0019:  stloc.3
  IL_001a:  br.s       IL_0027
  IL_001c:  ldc.i4.2
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
  IL_0023:  ldloc.3
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.3
  IL_0027:  ldloc.3
  IL_0028:  ldc.i4.2
  IL_0029:  clt
  IL_002b:  stloc.s    V_4
  IL_002d:  ldloc.s    V_4
  IL_002f:  brtrue.s   IL_001c
  IL_0031:  ldc.i4.1
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_0040
  IL_0035:  ldc.i4.3
  IL_0036:  call       ""void System.Console.WriteLine(int)""
  IL_003b:  nop
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.1
  IL_003e:  add
  IL_003f:  stloc.0
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.3
  IL_0042:  clt
  IL_0044:  stloc.s    V_5
  IL_0046:  ldloc.s    V_5
  IL_0048:  brtrue.s   IL_0035
  IL_004a:  ret
}
");
            v0.VerifyPdb("C.M", @"
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
          <slot kind=""0"" offset=""135"" />
          <slot kind=""0"" offset=""20"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""0"" offset=""79"" />
          <slot kind=""1"" offset=""70"" />
          <slot kind=""1"" offset=""147"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""14"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x3"" hidden=""true"" document=""1"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""37"" endLine=""8"" endColumn=""58"" document=""1"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""32"" endLine=""8"" endColumn=""35"" document=""1"" />
        <entry offset=""0x10"" startLine=""8"" startColumn=""25"" endLine=""8"" endColumn=""30"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x18"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1a"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""37"" endLine=""9"" endColumn=""58"" document=""1"" />
        <entry offset=""0x23"" startLine=""9"" startColumn=""32"" endLine=""9"" endColumn=""35"" document=""1"" />
        <entry offset=""0x27"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x2d"" hidden=""true"" document=""1"" />
        <entry offset=""0x31"" startLine=""12"" startColumn=""14"" endLine=""12"" endColumn=""19"" document=""1"" />
        <entry offset=""0x33"" hidden=""true"" document=""1"" />
        <entry offset=""0x35"" startLine=""12"" startColumn=""33"" endLine=""12"" endColumn=""54"" document=""1"" />
        <entry offset=""0x3c"" startLine=""12"" startColumn=""28"" endLine=""12"" endColumn=""31"" document=""1"" />
        <entry offset=""0x40"" startLine=""12"" startColumn=""21"" endLine=""12"" endColumn=""26"" document=""1"" />
        <entry offset=""0x46"" hidden=""true"" document=""1"" />
        <entry offset=""0x4a"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4b"">
        <namespace name=""System"" />
        <local name=""j"" il_index=""0"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
        <scope startOffset=""0x1"" endOffset=""0x18"">
          <local name=""i"" il_index=""1"" il_start=""0x1"" il_end=""0x18"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x18"" endOffset=""0x31"">
          <local name=""i"" il_index=""3"" il_start=""0x18"" il_end=""0x31"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
            var symReader = v0.CreateSymReader();

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), symReader.GetEncMethodDebugInfo);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            // check that all user-defined and long-lived synthesized local slots are reused
            diff1.VerifyIL("C.M", @"
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0, //j
                int V_1, //i
                bool V_2,
                int V_3, //i
                bool V_4,
                bool V_5)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  br.s       IL_0010
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  nop
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.1
  IL_0012:  clt
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  brtrue.s   IL_0005
  IL_0018:  ldc.i4.1
  IL_0019:  stloc.3
  IL_001a:  br.s       IL_0027
  IL_001c:  ldc.i4.2
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
  IL_0023:  ldloc.3
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.3
  IL_0027:  ldloc.3
  IL_0028:  ldc.i4.2
  IL_0029:  clt
  IL_002b:  stloc.s    V_4
  IL_002d:  ldloc.s    V_4
  IL_002f:  brtrue.s   IL_001c
  IL_0031:  ldc.i4.1
  IL_0032:  stloc.0
  IL_0033:  br.s       IL_0040
  IL_0035:  ldc.i4.3
  IL_0036:  call       ""void System.Console.WriteLine(int)""
  IL_003b:  nop
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.1
  IL_003e:  add
  IL_003f:  stloc.0
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.3
  IL_0042:  clt
  IL_0044:  stloc.s    V_5
  IL_0046:  ldloc.s    V_5
  IL_0048:  brtrue.s   IL_0035
  IL_004a:  ret
}
");
        }

        /// <summary>
        /// Enc debug info is only present in debug builds.
        /// </summary>
        [Fact]
        public void DebugOnly()
        {
            var source = WithWindowsLineBreaks(
@"class C
{
    static System.IDisposable F()
    {
        return null;
    }
    static void M()
    {
        lock (F()) { }
        using (F()) { }
    }
}");
            var debug = CreateCompilation(source, options: TestOptions.DebugDll);
            var release = CreateCompilation(source, options: TestOptions.ReleaseDll);

            CompileAndVerify(debug).VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""3"" offset=""11"" />
          <slot kind=""2"" offset=""11"" />
          <slot kind=""4"" offset=""35"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
        <entry offset=""0x12"" startLine=""9"" startColumn=""20"" endLine=""9"" endColumn=""21"" document=""1"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x20"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" document=""1"" />
        <entry offset=""0x27"" startLine=""10"" startColumn=""21"" endLine=""10"" endColumn=""22"" document=""1"" />
        <entry offset=""0x28"" startLine=""10"" startColumn=""23"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2b"" hidden=""true"" document=""1"" />
        <entry offset=""0x35"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
            CompileAndVerify(release).VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" document=""1"" />
        <entry offset=""0x22"" startLine=""10"" startColumn=""23"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x2d"" hidden=""true"" document=""1"" />
        <entry offset=""0x2e"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Using()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class C
{
    static IDisposable F() => null;

    static void M()
    {
        using (F())
        {
            using (var u = F())
            {
            }
            using (F())
            {
            }
        }
    }
}");
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => methodData0.GetEncDebugInfo());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (System.IDisposable V_0,
                System.IDisposable V_1, //u
                System.IDisposable V_2)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  nop
    IL_0008:  call       ""System.IDisposable C.F()""
    IL_000d:  stloc.1
    .try
    {
      IL_000e:  nop
      IL_000f:  nop
      IL_0010:  leave.s    IL_001d
    }
    finally
    {
      IL_0012:  ldloc.1
      IL_0013:  brfalse.s  IL_001c
      IL_0015:  ldloc.1
      IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
      IL_001b:  nop
      IL_001c:  endfinally
    }
    IL_001d:  call       ""System.IDisposable C.F()""
    IL_0022:  stloc.2
    .try
    {
      IL_0023:  nop
      IL_0024:  nop
      IL_0025:  leave.s    IL_0032
    }
    finally
    {
      IL_0027:  ldloc.2
      IL_0028:  brfalse.s  IL_0031
      IL_002a:  ldloc.2
      IL_002b:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0030:  nop
      IL_0031:  endfinally
    }
    IL_0032:  nop
    IL_0033:  leave.s    IL_0040
  }
  finally
  {
    IL_0035:  ldloc.0
    IL_0036:  brfalse.s  IL_003f
    IL_0038:  ldloc.0
    IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003e:  nop
    IL_003f:  endfinally
  }
  IL_0040:  ret
}");
        }

        [Fact]
        public void Using_VariableSwap()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static IDisposable F() => null;

    static void M()
    {
        using (IDisposable <N:0>u = F()</N:0>, <N:1>v = F()</N:1>) { }
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static IDisposable F() => null;

    static void M()
    {
        using (IDisposable <N:1>v = F()</N:1>, <N:0>u = F()</N:0>) { }
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            v0.VerifyPdb("C.M", @"
 <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""35"" />
          <slot kind=""0"" offset=""55"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x2a"">
        <scope startOffset=""0x1"" endOffset=""0x29"">
          <local name=""u"" il_index=""0"" il_start=""0x1"" il_end=""0x29"" attributes=""0"" />
          <local name=""v"" il_index=""1"" il_start=""0x1"" il_end=""0x29"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            diff1.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (System.IDisposable V_0, //u
                System.IDisposable V_1) //v
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  call       ""System.IDisposable C.F()""
    IL_000c:  stloc.0
    .try
    {
      IL_000d:  nop
      IL_000e:  nop
      IL_000f:  leave.s    IL_001c
    }
    finally
    {
      IL_0011:  ldloc.0
      IL_0012:  brfalse.s  IL_001b
      IL_0014:  ldloc.0
      IL_0015:  callvirt   ""void System.IDisposable.Dispose()""
      IL_001a:  nop
      IL_001b:  endfinally
    }
    IL_001c:  leave.s    IL_0029
  }
  finally
  {
    IL_001e:  ldloc.1
    IL_001f:  brfalse.s  IL_0028
    IL_0021:  ldloc.1
    IL_0022:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0027:  nop
    IL_0028:  endfinally
  }
  IL_0029:  ret
}
");
        }

        [Fact]
        public void Using_VariableDeclaration_VariableSwap()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static IDisposable F() => null;

    static void M()
    {
        using IDisposable <N:0>u = F()</N:0>, <N:1>v = F()</N:1>;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static IDisposable F() => null;

    static void M()
    {
        using IDisposable <N:1>v = F()</N:1>, <N:0>u = F()</N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""54"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x26"">
        <local name=""u"" il_index=""0"" il_start=""0x0"" il_end=""0x26"" attributes=""0"" />
        <local name=""v"" il_index=""1"" il_start=""0x0"" il_end=""0x26"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            diff1.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (System.IDisposable V_0, //u
                System.IDisposable V_1) //v
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  call       ""System.IDisposable C.F()""
    IL_000c:  stloc.0
    .try
    {
      IL_000d:  leave.s    IL_0025
    }
    finally
    {
      IL_000f:  ldloc.0
      IL_0010:  brfalse.s  IL_0019
      IL_0012:  ldloc.0
      IL_0013:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0018:  nop
      IL_0019:  endfinally
    }
  }
  finally
  {
    IL_001a:  ldloc.1
    IL_001b:  brfalse.s  IL_0024
    IL_001d:  ldloc.1
    IL_001e:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0023:  nop
    IL_0024:  endfinally
  }
  IL_0025:  ret
}
");
        }

        [Fact]
        public void AwaitUsing_VariableSwap()
        {
            var source0 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static IAsyncDisposable F() => null;

    static async Task M()
    {
        await using (IAsyncDisposable <N:0>u = F()</N:0>, <N:1>v = F()</N:1>) { }
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static IAsyncDisposable F() => null;

    static async Task M()
    {
        await using (IAsyncDisposable <N:1>v = F()</N:1>, <N:0>u = F()</N:0>) { }
    }
}");
            var asyncStreamsTree = Parse(AsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options);
            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, asyncStreamsTree });

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            v0.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""46"" />
          <slot kind=""0"" offset=""66"" />
          <slot kind=""22"" offset=""46"" />
          <slot kind=""23"" offset=""46"" />
          <slot kind=""22"" offset=""66"" />
          <slot kind=""23"" offset=""66"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""1"" offset=""46"" />
          <state number=""0"" offset=""66"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);

            v0.VerifyLocalSignature("C.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
.locals init (int V_0,
              object V_1,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
              System.Threading.Tasks.ValueTask V_3,
              C.<M>d__1 V_4,
              System.Exception V_5,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_6)
");

            diff1.VerifyLocalSignature("C.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
.locals init (int V_0,
              object V_1,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
              System.Threading.Tasks.ValueTask V_3,
              C.<M>d__1 V_4,
              System.Exception V_5,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_6)
");
        }

        [Fact]
        public void AwaitUsing_VariableDeclaration_VariableSwap()
        {
            var source0 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static IAsyncDisposable F() => null;

    static async Task M()
    {
        await using IAsyncDisposable <N:0>u = F()</N:0>, <N:1>v = F()</N:1>;
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static IAsyncDisposable F() => null;

    static async Task M()
    {
        await using IAsyncDisposable <N:1>v = F()</N:1>, <N:0>u = F()</N:0>;
    }
}");
            var asyncStreamsTree = Parse(AsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options);
            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, asyncStreamsTree });

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            v0.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""45"" />
          <slot kind=""0"" offset=""65"" />
          <slot kind=""22"" offset=""45"" />
          <slot kind=""23"" offset=""45"" />
          <slot kind=""22"" offset=""65"" />
          <slot kind=""23"" offset=""65"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""1"" offset=""45"" />
          <state number=""0"" offset=""65"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);

            v0.VerifyLocalSignature("C.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
.locals init (int V_0,
              object V_1,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
              System.Threading.Tasks.ValueTask V_3,
              C.<M>d__1 V_4,
              System.Exception V_5,
              int V_6,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_7)
");

            diff1.VerifyLocalSignature("C.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
.locals init (int V_0,
              object V_1,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_2,
              System.Threading.Tasks.ValueTask V_3,
              C.<M>d__1 V_4,
              System.Exception V_5,
              int V_6,
              System.Runtime.CompilerServices.ValueTaskAwaiter V_7)
");
        }

        [Fact]
        public void Lock()
        {
            var source =
@"class C
{
    static object F()
    {
        return null;
    }
    static void M()
    {
        lock (F())
        {
            lock (F())
            {
            }
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                object V_2,
                bool V_3)
 -IL_0000:  nop
 -IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldloc.0
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  nop
   -IL_0012:  nop
   -IL_0013:  call       ""object C.F()""
    IL_0018:  stloc.2
    IL_0019:  ldc.i4.0
    IL_001a:  stloc.3
    .try
    {
      IL_001b:  ldloc.2
      IL_001c:  ldloca.s   V_3
      IL_001e:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
      IL_0023:  nop
     -IL_0024:  nop
     -IL_0025:  nop
      IL_0026:  leave.s    IL_0033
    }
    finally
    {
     ~IL_0028:  ldloc.3
      IL_0029:  brfalse.s  IL_0032
      IL_002b:  ldloc.2
      IL_002c:  call       ""void System.Threading.Monitor.Exit(object)""
      IL_0031:  nop
     ~IL_0032:  endfinally
    }
   -IL_0033:  nop
    IL_0034:  leave.s    IL_0041
  }
  finally
  {
   ~IL_0036:  ldloc.1
    IL_0037:  brfalse.s  IL_0040
    IL_0039:  ldloc.0
    IL_003a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_003f:  nop
   ~IL_0040:  endfinally
  }
 -IL_0041:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        /// <summary>
        /// Using Monitor.Enter(object).
        /// </summary>
        [Fact]
        public void Lock_Pre40()
        {
            var source =
@"class C
{
    static object F()
    {
        return null;
    }
    static void M()
    {
        lock (F())
        {
        }
    }
}";
            var compilation0 = CreateEmptyCompilation(source, options: TestOptions.DebugDll, references: new[] { MscorlibRef_v20 });
            var compilation1 = CreateEmptyCompilation(source, options: TestOptions.DebugDll, references: new[] { MscorlibRef_v20 });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""void System.Threading.Monitor.Enter(object)""
  IL_000d:  nop
  .try
  {
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  leave.s    IL_001a
  }
  finally
  {
    IL_0012:  ldloc.0
    IL_0013:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0018:  nop
    IL_0019:  endfinally
  }
  IL_001a:  ret
}");
        }

        [Fact]
        public void Fixed()
        {
            var source =
@"class C
{
    unsafe static void M(string s, int[] i)
    {
        fixed (char *p = s)
        {
            fixed (int *q = i)
            {
            }
            fixed (char *r = s)
            {
            }
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                int* V_2, //q
                [unchanged] V_3,
                char* V_4, //r
                pinned string V_5,
                pinned int[] V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  conv.u
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0011
  IL_0009:  ldloc.0
  IL_000a:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_000f:  add
  IL_0010:  stloc.0
  IL_0011:  nop
  IL_0012:  ldarg.1
  IL_0013:  dup
  IL_0014:  stloc.s    V_6
  IL_0016:  brfalse.s  IL_001e
  IL_0018:  ldloc.s    V_6
  IL_001a:  ldlen
  IL_001b:  conv.i4
  IL_001c:  brtrue.s   IL_0023
  IL_001e:  ldc.i4.0
  IL_001f:  conv.u
  IL_0020:  stloc.2
  IL_0021:  br.s       IL_002d
  IL_0023:  ldloc.s    V_6
  IL_0025:  ldc.i4.0
  IL_0026:  ldelema    ""int""
  IL_002b:  conv.u
  IL_002c:  stloc.2
  IL_002d:  nop
  IL_002e:  nop
  IL_002f:  ldnull
  IL_0030:  stloc.s    V_6
  IL_0032:  ldarg.0
  IL_0033:  stloc.s    V_5
  IL_0035:  ldloc.s    V_5
  IL_0037:  conv.u
  IL_0038:  stloc.s    V_4
  IL_003a:  ldloc.s    V_4
  IL_003c:  brfalse.s  IL_0048
  IL_003e:  ldloc.s    V_4
  IL_0040:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0045:  add
  IL_0046:  stloc.s    V_4
  IL_0048:  nop
  IL_0049:  nop
  IL_004a:  ldnull
  IL_004b:  stloc.s    V_5
  IL_004d:  nop
  IL_004e:  ldnull
  IL_004f:  stloc.1
  IL_0050:  ret
}");
        }

        [WorkItem(770053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770053")]
        [Fact]
        public void FixedMultiple()
        {
            var source =
@"class C
{
    unsafe static void M(string s1, string s2, string s3, string s4)
    {
        fixed (char* p1 = s1, p2 = s2)
        {
            *p1 = *p2;
        }
        fixed (char* p1 = s1, p3 = s3, p2 = s4)
        {
            *p1 = *p2;
            *p2 = *p3;
            fixed (char *p4 = s2)
            {
                *p3 = *p4;
            }
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      166 (0xa6)
  .maxstack  2
  .locals init (char* V_0, //p1
                char* V_1, //p2
                pinned string V_2,
                pinned string V_3,
                char* V_4, //p1
                char* V_5, //p3
                char* V_6, //p2
                pinned string V_7,
                pinned string V_8,
                pinned string V_9,
                char* V_10, //p4
                pinned string V_11)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  conv.u
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0011
  IL_0009:  ldloc.0
  IL_000a:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_000f:  add
  IL_0010:  stloc.0
  IL_0011:  ldarg.1
  IL_0012:  stloc.3
  IL_0013:  ldloc.3
  IL_0014:  conv.u
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  brfalse.s  IL_0021
  IL_0019:  ldloc.1
  IL_001a:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_001f:  add
  IL_0020:  stloc.1
  IL_0021:  nop
  IL_0022:  ldloc.0
  IL_0023:  ldloc.1
  IL_0024:  ldind.u2
  IL_0025:  stind.i2
  IL_0026:  nop
  IL_0027:  ldnull
  IL_0028:  stloc.2
  IL_0029:  ldnull
  IL_002a:  stloc.3
  IL_002b:  ldarg.0
  IL_002c:  stloc.s    V_7
  IL_002e:  ldloc.s    V_7
  IL_0030:  conv.u
  IL_0031:  stloc.s    V_4
  IL_0033:  ldloc.s    V_4
  IL_0035:  brfalse.s  IL_0041
  IL_0037:  ldloc.s    V_4
  IL_0039:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_003e:  add
  IL_003f:  stloc.s    V_4
  IL_0041:  ldarg.2
  IL_0042:  stloc.s    V_8
  IL_0044:  ldloc.s    V_8
  IL_0046:  conv.u
  IL_0047:  stloc.s    V_5
  IL_0049:  ldloc.s    V_5
  IL_004b:  brfalse.s  IL_0057
  IL_004d:  ldloc.s    V_5
  IL_004f:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0054:  add
  IL_0055:  stloc.s    V_5
  IL_0057:  ldarg.3
  IL_0058:  stloc.s    V_9
  IL_005a:  ldloc.s    V_9
  IL_005c:  conv.u
  IL_005d:  stloc.s    V_6
  IL_005f:  ldloc.s    V_6
  IL_0061:  brfalse.s  IL_006d
  IL_0063:  ldloc.s    V_6
  IL_0065:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_006a:  add
  IL_006b:  stloc.s    V_6
  IL_006d:  nop
  IL_006e:  ldloc.s    V_4
  IL_0070:  ldloc.s    V_6
  IL_0072:  ldind.u2
  IL_0073:  stind.i2
  IL_0074:  ldloc.s    V_6
  IL_0076:  ldloc.s    V_5
  IL_0078:  ldind.u2
  IL_0079:  stind.i2
  IL_007a:  ldarg.1
  IL_007b:  stloc.s    V_11
  IL_007d:  ldloc.s    V_11
  IL_007f:  conv.u
  IL_0080:  stloc.s    V_10
  IL_0082:  ldloc.s    V_10
  IL_0084:  brfalse.s  IL_0090
  IL_0086:  ldloc.s    V_10
  IL_0088:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_008d:  add
  IL_008e:  stloc.s    V_10
  IL_0090:  nop
  IL_0091:  ldloc.s    V_5
  IL_0093:  ldloc.s    V_10
  IL_0095:  ldind.u2
  IL_0096:  stind.i2
  IL_0097:  nop
  IL_0098:  ldnull
  IL_0099:  stloc.s    V_11
  IL_009b:  nop
  IL_009c:  ldnull
  IL_009d:  stloc.s    V_7
  IL_009f:  ldnull
  IL_00a0:  stloc.s    V_8
  IL_00a2:  ldnull
  IL_00a3:  stloc.s    V_9
  IL_00a5:  ret
}
");
        }

        [Fact]
        public void ForEach()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C
{
    static IEnumerable F1() { return null; }
    static List<object> F2() { return null; }
    static IEnumerable F3() { return null; }
    static List<object> F4() { return null; }
    static void M()
    {
        foreach (var @x in F1())
        {
            foreach (object y in F2()) { }
        }
        foreach (var x in F4())
        {
            foreach (var y in F3()) { }
            foreach (var z in F2()) { }
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      272 (0x110)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
                object V_1, //x
                System.Collections.Generic.List<object>.Enumerator V_2,
                object V_3, //y
                [unchanged] V_4,
                System.Collections.Generic.List<object>.Enumerator V_5,
                object V_6, //x
                System.Collections.IEnumerator V_7,
                object V_8, //y
                System.Collections.Generic.List<object>.Enumerator V_9,
                object V_10, //z
                System.IDisposable V_11)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.Collections.IEnumerable C.F1()""
  IL_0007:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_000c:  stloc.0
  .try
  {
    IL_000d:  br.s       IL_004a
    IL_000f:  ldloc.0
    IL_0010:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0015:  stloc.1
    IL_0016:  nop
    IL_0017:  nop
    IL_0018:  call       ""System.Collections.Generic.List<object> C.F2()""
    IL_001d:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
    IL_0022:  stloc.2
    .try
    {
      IL_0023:  br.s       IL_002f
      IL_0025:  ldloca.s   V_2
      IL_0027:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
      IL_002c:  stloc.3
      IL_002d:  nop
      IL_002e:  nop
      IL_002f:  ldloca.s   V_2
      IL_0031:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
      IL_0036:  brtrue.s   IL_0025
      IL_0038:  leave.s    IL_0049
    }
    finally
    {
      IL_003a:  ldloca.s   V_2
      IL_003c:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
      IL_0042:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0047:  nop
      IL_0048:  endfinally
    }
    IL_0049:  nop
    IL_004a:  ldloc.0
    IL_004b:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0050:  brtrue.s   IL_000f
    IL_0052:  leave.s    IL_0069
  }
  finally
  {
    IL_0054:  ldloc.0
    IL_0055:  isinst     ""System.IDisposable""
    IL_005a:  stloc.s    V_11
    IL_005c:  ldloc.s    V_11
    IL_005e:  brfalse.s  IL_0068
    IL_0060:  ldloc.s    V_11
    IL_0062:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0067:  nop
    IL_0068:  endfinally
  }
  IL_0069:  nop
  IL_006a:  call       ""System.Collections.Generic.List<object> C.F4()""
  IL_006f:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
  IL_0074:  stloc.s    V_5
  .try
  {
    IL_0076:  br.s       IL_00f2
    IL_0078:  ldloca.s   V_5
    IL_007a:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
    IL_007f:  stloc.s    V_6
    IL_0081:  nop
    IL_0082:  nop
    IL_0083:  call       ""System.Collections.IEnumerable C.F3()""
    IL_0088:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
    IL_008d:  stloc.s    V_7
    .try
    {
      IL_008f:  br.s       IL_009c
      IL_0091:  ldloc.s    V_7
      IL_0093:  callvirt   ""object System.Collections.IEnumerator.Current.get""
      IL_0098:  stloc.s    V_8
      IL_009a:  nop
      IL_009b:  nop
      IL_009c:  ldloc.s    V_7
      IL_009e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
      IL_00a3:  brtrue.s   IL_0091
      IL_00a5:  leave.s    IL_00bd
    }
    finally
    {
      IL_00a7:  ldloc.s    V_7
      IL_00a9:  isinst     ""System.IDisposable""
      IL_00ae:  stloc.s    V_11
      IL_00b0:  ldloc.s    V_11
      IL_00b2:  brfalse.s  IL_00bc
      IL_00b4:  ldloc.s    V_11
      IL_00b6:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00bb:  nop
      IL_00bc:  endfinally
    }
    IL_00bd:  nop
    IL_00be:  call       ""System.Collections.Generic.List<object> C.F2()""
    IL_00c3:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
    IL_00c8:  stloc.s    V_9
    .try
    {
      IL_00ca:  br.s       IL_00d7
      IL_00cc:  ldloca.s   V_9
      IL_00ce:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
      IL_00d3:  stloc.s    V_10
      IL_00d5:  nop
      IL_00d6:  nop
      IL_00d7:  ldloca.s   V_9
      IL_00d9:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
      IL_00de:  brtrue.s   IL_00cc
      IL_00e0:  leave.s    IL_00f1
    }
    finally
    {
      IL_00e2:  ldloca.s   V_9
      IL_00e4:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
      IL_00ea:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00ef:  nop
      IL_00f0:  endfinally
    }
    IL_00f1:  nop
    IL_00f2:  ldloca.s   V_5
    IL_00f4:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
    IL_00f9:  brtrue     IL_0078
    IL_00fe:  leave.s    IL_010f
  }
  finally
  {
    IL_0100:  ldloca.s   V_5
    IL_0102:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
    IL_0108:  callvirt   ""void System.IDisposable.Dispose()""
    IL_010d:  nop
    IL_010e:  endfinally
  }
  IL_010f:  ret
}");
        }

        [Fact]
        public void ForEachArray1()
        {
            var source =
@"class C
{
    static void M(double[,,] c)
    {
        foreach (var x in c)
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (double[,,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5,
                int V_6,
                double V_7) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.0
  IL_0006:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0013:  stloc.2
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_001b:  stloc.3
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0023:  stloc.s    V_4
 ~IL_0025:  br.s       IL_0069
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.1
  IL_0029:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002e:  stloc.s    V_5
 ~IL_0030:  br.s       IL_005e
  IL_0032:  ldloc.0
  IL_0033:  ldc.i4.2
  IL_0034:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0039:  stloc.s    V_6
 ~IL_003b:  br.s       IL_0053
 -IL_003d:  ldloc.0
  IL_003e:  ldloc.s    V_4
  IL_0040:  ldloc.s    V_5
  IL_0042:  ldloc.s    V_6
  IL_0044:  call       ""double[*,*,*].Get""
  IL_0049:  stloc.s    V_7
 -IL_004b:  nop
 -IL_004c:  nop
 ~IL_004d:  ldloc.s    V_6
  IL_004f:  ldc.i4.1
  IL_0050:  add
  IL_0051:  stloc.s    V_6
 -IL_0053:  ldloc.s    V_6
  IL_0055:  ldloc.3
  IL_0056:  ble.s      IL_003d
 ~IL_0058:  ldloc.s    V_5
  IL_005a:  ldc.i4.1
  IL_005b:  add
  IL_005c:  stloc.s    V_5
 -IL_005e:  ldloc.s    V_5
  IL_0060:  ldloc.2
  IL_0061:  ble.s      IL_0032
 ~IL_0063:  ldloc.s    V_4
  IL_0065:  ldc.i4.1
  IL_0066:  add
  IL_0067:  stloc.s    V_4
 -IL_0069:  ldloc.s    V_4
  IL_006b:  ldloc.1
  IL_006c:  ble.s      IL_0027
 -IL_006e:  ret
}", sequencePoints: "C.M");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (double[,,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5,
                int V_6,
                double V_7) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.0
  IL_0006:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_000b:  stloc.1
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0013:  stloc.2
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_001b:  stloc.3
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.0
  IL_001e:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0023:  stloc.s    V_4
 ~IL_0025:  br.s       IL_0069
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.1
  IL_0029:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002e:  stloc.s    V_5
 ~IL_0030:  br.s       IL_005e
  IL_0032:  ldloc.0
  IL_0033:  ldc.i4.2
  IL_0034:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0039:  stloc.s    V_6
 ~IL_003b:  br.s       IL_0053
 -IL_003d:  ldloc.0
  IL_003e:  ldloc.s    V_4
  IL_0040:  ldloc.s    V_5
  IL_0042:  ldloc.s    V_6
  IL_0044:  call       ""double[*,*,*].Get""
  IL_0049:  stloc.s    V_7
 -IL_004b:  nop
 -IL_004c:  nop
 ~IL_004d:  ldloc.s    V_6
  IL_004f:  ldc.i4.1
  IL_0050:  add
  IL_0051:  stloc.s    V_6
 -IL_0053:  ldloc.s    V_6
  IL_0055:  ldloc.3
  IL_0056:  ble.s      IL_003d
 ~IL_0058:  ldloc.s    V_5
  IL_005a:  ldc.i4.1
  IL_005b:  add
  IL_005c:  stloc.s    V_5
 -IL_005e:  ldloc.s    V_5
  IL_0060:  ldloc.2
  IL_0061:  ble.s      IL_0032
 ~IL_0063:  ldloc.s    V_4
  IL_0065:  ldc.i4.1
  IL_0066:  add
  IL_0067:  stloc.s    V_4
 -IL_0069:  ldloc.s    V_4
  IL_006b:  ldloc.1
  IL_006c:  ble.s      IL_0027
 -IL_006e:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void ForEachArray2()
        {
            var source =
@"class C
{
    static void M(string a, object[] b, double[,,] c)
    {
        foreach (var x in a)
        {
            foreach (var y in b)
            {
            }
        }
        foreach (var x in c)
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      184 (0xb8)
  .maxstack  4
  .locals init (string V_0,
                int V_1,
                char V_2, //x
                object[] V_3,
                int V_4,
                object V_5, //y
                double[,,] V_6,
                int V_7,
                int V_8,
                int V_9,
                int V_10,
                int V_11,
                int V_12,
                double V_13) //x
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0033
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  callvirt   ""char string.this[int].get""
  IL_000f:  stloc.2
  IL_0010:  nop
  IL_0011:  nop
  IL_0012:  ldarg.1
  IL_0013:  stloc.3
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.s    V_4
  IL_0017:  br.s       IL_0027
  IL_0019:  ldloc.3
  IL_001a:  ldloc.s    V_4
  IL_001c:  ldelem.ref
  IL_001d:  stloc.s    V_5
  IL_001f:  nop
  IL_0020:  nop
  IL_0021:  ldloc.s    V_4
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  stloc.s    V_4
  IL_0027:  ldloc.s    V_4
  IL_0029:  ldloc.3
  IL_002a:  ldlen
  IL_002b:  conv.i4
  IL_002c:  blt.s      IL_0019
  IL_002e:  nop
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4.1
  IL_0031:  add
  IL_0032:  stloc.1
  IL_0033:  ldloc.1
  IL_0034:  ldloc.0
  IL_0035:  callvirt   ""int string.Length.get""
  IL_003a:  blt.s      IL_0008
  IL_003c:  nop
  IL_003d:  ldarg.2
  IL_003e:  stloc.s    V_6
  IL_0040:  ldloc.s    V_6
  IL_0042:  ldc.i4.0
  IL_0043:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0048:  stloc.s    V_7
  IL_004a:  ldloc.s    V_6
  IL_004c:  ldc.i4.1
  IL_004d:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0052:  stloc.s    V_8
  IL_0054:  ldloc.s    V_6
  IL_0056:  ldc.i4.2
  IL_0057:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_005c:  stloc.s    V_9
  IL_005e:  ldloc.s    V_6
  IL_0060:  ldc.i4.0
  IL_0061:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0066:  stloc.s    V_10
  IL_0068:  br.s       IL_00b1
  IL_006a:  ldloc.s    V_6
  IL_006c:  ldc.i4.1
  IL_006d:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0072:  stloc.s    V_11
  IL_0074:  br.s       IL_00a5
  IL_0076:  ldloc.s    V_6
  IL_0078:  ldc.i4.2
  IL_0079:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_007e:  stloc.s    V_12
  IL_0080:  br.s       IL_0099
  IL_0082:  ldloc.s    V_6
  IL_0084:  ldloc.s    V_10
  IL_0086:  ldloc.s    V_11
  IL_0088:  ldloc.s    V_12
  IL_008a:  call       ""double[*,*,*].Get""
  IL_008f:  stloc.s    V_13
  IL_0091:  nop
  IL_0092:  nop
  IL_0093:  ldloc.s    V_12
  IL_0095:  ldc.i4.1
  IL_0096:  add
  IL_0097:  stloc.s    V_12
  IL_0099:  ldloc.s    V_12
  IL_009b:  ldloc.s    V_9
  IL_009d:  ble.s      IL_0082
  IL_009f:  ldloc.s    V_11
  IL_00a1:  ldc.i4.1
  IL_00a2:  add
  IL_00a3:  stloc.s    V_11
  IL_00a5:  ldloc.s    V_11
  IL_00a7:  ldloc.s    V_8
  IL_00a9:  ble.s      IL_0076
  IL_00ab:  ldloc.s    V_10
  IL_00ad:  ldc.i4.1
  IL_00ae:  add
  IL_00af:  stloc.s    V_10
  IL_00b1:  ldloc.s    V_10
  IL_00b3:  ldloc.s    V_7
  IL_00b5:  ble.s      IL_006a
  IL_00b7:  ret
}");
        }

        /// <summary>
        /// Unlike Dev12 we can handle array with more than 256 dimensions.
        /// </summary>
        [Fact]
        public void ForEachArray_ToManyDimensions()
        {
            var source =
@"class C
{
    static void M(object o)
    {
        foreach (var x in (object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])o)
        {
        }
    }
}";
            // Make sure the source contains an array with too many dimensions.
            var tooManyCommas = new string(',', 256);
            Assert.True(source.IndexOf(tooManyCommas, StringComparison.Ordinal) > 0);

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
        }

        [Fact]
        public void ForEachWithDynamicAndTuple()
        {
            var source =
@"class C
{
    static void M((dynamic, int) t)
    {
        foreach (var o in t.Item1)
        {
        }
    }
}";
            var compilation0 = CreateCompilation(
                source,
                options: TestOptions.DebugDll,
                references: new[] { CSharpRef });
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      119 (0x77)
  .maxstack  3
  .locals init (System.Collections.IEnumerator V_0,
                object V_1, //o
                [unchanged] V_2,
                System.IDisposable V_3)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> C.<>o__0#1.<>p__0""
  IL_0007:  brfalse.s  IL_000b
  IL_0009:  br.s       IL_002f
  IL_000b:  ldc.i4.0
  IL_000c:  ldtoken    ""System.Collections.IEnumerable""
  IL_0011:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0016:  ldtoken    ""C""
  IL_001b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0020:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0025:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_002a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> C.<>o__0#1.<>p__0""
  IL_002f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> C.<>o__0#1.<>p__0""
  IL_0034:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>>.Target""
  IL_0039:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> C.<>o__0#1.<>p__0""
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""dynamic System.ValueTuple<dynamic, int>.Item1""
  IL_0044:  callvirt   ""System.Collections.IEnumerable System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0049:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_004e:  stloc.0
  .try
  {
    IL_004f:  br.s       IL_005a
    IL_0051:  ldloc.0
    IL_0052:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0057:  stloc.1
    IL_0058:  nop
    IL_0059:  nop
    IL_005a:  ldloc.0
    IL_005b:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0060:  brtrue.s   IL_0051
    IL_0062:  leave.s    IL_0076
  }
  finally
  {
    IL_0064:  ldloc.0
    IL_0065:  isinst     ""System.IDisposable""
    IL_006a:  stloc.3
    IL_006b:  ldloc.3
    IL_006c:  brfalse.s  IL_0075
    IL_006e:  ldloc.3
    IL_006f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0074:  nop
    IL_0075:  endfinally
  }
  IL_0076:  ret
}");
        }

        [Fact]
        public void RemoveRestoreNullableAtArrayElement()
        {
            var source0 = MarkedSource(
@"using System;
class C
{
    public static void M()
    {
        var <N:1>arr</N:1> = new string?[] { ""0"" };
        <N:0>foreach</N:0> (var s in arr)
        {
            Console.WriteLine(1);
        }
    }
}");
            // Remove nullable
            var source1 = MarkedSource(
@"using System;
class C
{
    public static void M()
    {
        var <N:1>arr</N:1> = new string[] { ""0"" };
        <N:0>foreach</N:0> (var s in arr)
        {
            Console.WriteLine(1);
        }
    }
}");
            // Restore nullable
            var source2 = source0;

            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.M", @"
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (string[] V_0, //arr
                string[] V_1,
                int V_2,
                string V_3) //s
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""string""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""0""
  IL_000e:  stelem.ref
  IL_000f:  stloc.0
  IL_0010:  nop
  IL_0011:  ldloc.0
  IL_0012:  stloc.1
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
  IL_0015:  br.s       IL_0028
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldelem.ref
  IL_001a:  stloc.3
  IL_001b:  nop
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
  IL_0023:  nop
  IL_0024:  ldloc.2
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stloc.2
  IL_0028:  ldloc.2
  IL_0029:  ldloc.1
  IL_002a:  ldlen
  IL_002b:  conv.i4
  IL_002c:  blt.s      IL_0017
  IL_002e:  ret
}");

            var compilation1 = CreateCompilation(source1.Tree, options: TestOptions.DebugDll);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (string[] V_0, //arr
                string[] V_1,
                int V_2,
                string V_3) //s
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""string""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""0""
  IL_000e:  stelem.ref
  IL_000f:  stloc.0
  IL_0010:  nop
  IL_0011:  ldloc.0
  IL_0012:  stloc.1
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
  IL_0015:  br.s       IL_0028
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldelem.ref
  IL_001a:  stloc.3
  IL_001b:  nop
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
  IL_0023:  nop
  IL_0024:  ldloc.2
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stloc.2
  IL_0028:  ldloc.2
  IL_0029:  ldloc.1
  IL_002a:  ldlen
  IL_002b:  conv.i4
  IL_002c:  blt.s      IL_0017
  IL_002e:  ret
}");

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (string[] V_0, //arr
                string[] V_1,
                int V_2,
                string V_3) //s
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""string""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldstr      ""0""
  IL_000e:  stelem.ref
  IL_000f:  stloc.0
 -IL_0010:  nop
 -IL_0011:  ldloc.0
  IL_0012:  stloc.1
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
 ~IL_0015:  br.s       IL_0028
 -IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldelem.ref
  IL_001a:  stloc.3
 -IL_001b:  nop
 -IL_001c:  ldc.i4.1
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
 -IL_0023:  nop
 ~IL_0024:  ldloc.2
  IL_0025:  ldc.i4.1
  IL_0026:  add
  IL_0027:  stloc.2
 -IL_0028:  ldloc.2
  IL_0029:  ldloc.1
  IL_002a:  ldlen
  IL_002b:  conv.i4
  IL_002c:  blt.s      IL_0017
 -IL_002e:  ret
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void AddAndDelete()
        {
            var source0 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        lock (F1()) { }
        foreach (var c in F2()) { }
        using (F3()) { }
    }
}";
            // Delete one statement.
            var source1 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        lock (F1()) { }
        foreach (var c in F2()) { }
    }
}";
            // Add statement with same temp kind.
            var source2 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        using (F3()) { }
        lock (F1()) { }
        foreach (var c in F2()) { }
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.M", @"
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                string V_2,
                int V_3,
                char V_4, //c
                System.IDisposable V_5)
  IL_0000:  nop
  IL_0001:  call       ""object C.F1()""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldloc.0
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  leave.s    IL_0021
  }
  finally
  {
    IL_0016:  ldloc.1
    IL_0017:  brfalse.s  IL_0020
    IL_0019:  ldloc.0
    IL_001a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001f:  nop
    IL_0020:  endfinally
  }
  IL_0021:  nop
  IL_0022:  call       ""string C.F2()""
  IL_0027:  stloc.2
  IL_0028:  ldc.i4.0
  IL_0029:  stloc.3
  IL_002a:  br.s       IL_003b
  IL_002c:  ldloc.2
  IL_002d:  ldloc.3
  IL_002e:  callvirt   ""char string.this[int].get""
  IL_0033:  stloc.s    V_4
  IL_0035:  nop
  IL_0036:  nop
  IL_0037:  ldloc.3
  IL_0038:  ldc.i4.1
  IL_0039:  add
  IL_003a:  stloc.3
  IL_003b:  ldloc.3
  IL_003c:  ldloc.2
  IL_003d:  callvirt   ""int string.Length.get""
  IL_0042:  blt.s      IL_002c
  IL_0044:  call       ""System.IDisposable C.F3()""
  IL_0049:  stloc.s    V_5
  .try
  {
    IL_004b:  nop
    IL_004c:  nop
    IL_004d:  leave.s    IL_005c
  }
  finally
  {
    IL_004f:  ldloc.s    V_5
    IL_0051:  brfalse.s  IL_005b
    IL_0053:  ldloc.s    V_5
    IL_0055:  callvirt   ""void System.IDisposable.Dispose()""
    IL_005a:  nop
    IL_005b:  endfinally
  }
  IL_005c:  ret
}");

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll);
            var compilation2 = compilation0.WithSource(source2);

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                string V_2,
                int V_3,
                char V_4, //c
                [unchanged] V_5)
  IL_0000:  nop
  IL_0001:  call       ""object C.F1()""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldloc.0
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  leave.s    IL_0021
  }
  finally
  {
    IL_0016:  ldloc.1
    IL_0017:  brfalse.s  IL_0020
    IL_0019:  ldloc.0
    IL_001a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001f:  nop
    IL_0020:  endfinally
  }
  IL_0021:  nop
  IL_0022:  call       ""string C.F2()""
  IL_0027:  stloc.2
  IL_0028:  ldc.i4.0
  IL_0029:  stloc.3
  IL_002a:  br.s       IL_003b
  IL_002c:  ldloc.2
  IL_002d:  ldloc.3
  IL_002e:  callvirt   ""char string.this[int].get""
  IL_0033:  stloc.s    V_4
  IL_0035:  nop
  IL_0036:  nop
  IL_0037:  ldloc.3
  IL_0038:  ldc.i4.1
  IL_0039:  add
  IL_003a:  stloc.3
  IL_003b:  ldloc.3
  IL_003c:  ldloc.2
  IL_003d:  callvirt   ""int string.Length.get""
  IL_0042:  blt.s      IL_002c
  IL_0044:  ret
}");

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                string V_2,
                int V_3,
                char V_4, //c
                [unchanged] V_5,
                System.IDisposable V_6)
 -IL_0000:  nop
 -IL_0001:  call       ""System.IDisposable C.F3()""
  IL_0006:  stloc.s    V_6
  .try
  {
   -IL_0008:  nop
   -IL_0009:  nop
    IL_000a:  leave.s    IL_0019
  }
  finally
  {
   ~IL_000c:  ldloc.s    V_6
    IL_000e:  brfalse.s  IL_0018
    IL_0010:  ldloc.s    V_6
    IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0017:  nop
   ~IL_0018:  endfinally
  }
 -IL_0019:  call       ""object C.F1()""
  IL_001e:  stloc.0
  IL_001f:  ldc.i4.0
  IL_0020:  stloc.1
  .try
  {
    IL_0021:  ldloc.0
    IL_0022:  ldloca.s   V_1
    IL_0024:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0029:  nop
   -IL_002a:  nop
   -IL_002b:  nop
    IL_002c:  leave.s    IL_0039
  }
  finally
  {
   ~IL_002e:  ldloc.1
    IL_002f:  brfalse.s  IL_0038
    IL_0031:  ldloc.0
    IL_0032:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0037:  nop
   ~IL_0038:  endfinally
  }
 -IL_0039:  nop
 -IL_003a:  call       ""string C.F2()""
  IL_003f:  stloc.2
  IL_0040:  ldc.i4.0
  IL_0041:  stloc.3
 ~IL_0042:  br.s       IL_0053
 -IL_0044:  ldloc.2
  IL_0045:  ldloc.3
  IL_0046:  callvirt   ""char string.this[int].get""
  IL_004b:  stloc.s    V_4
 -IL_004d:  nop
 -IL_004e:  nop
 ~IL_004f:  ldloc.3
  IL_0050:  ldc.i4.1
  IL_0051:  add
  IL_0052:  stloc.3
 -IL_0053:  ldloc.3
  IL_0054:  ldloc.2
  IL_0055:  callvirt   ""int string.Length.get""
  IL_005a:  blt.s      IL_0044
 -IL_005c:  ret
}", methodToken: diff2.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void Insert()
        {
            var source0 =
@"class C
{
    static object F1() { return null; }
    static object F2() { return null; }
    static object F3() { return null; }
    static object F4() { return null; }
    static void M()
    {
        lock (F1()) { }
        lock (F2()) { }
    }
}";
            var source1 =
@"class C
{
    static object F1() { return null; }
    static object F2() { return null; }
    static object F3() { return null; }
    static object F4() { return null; }
    static void M()
    {
        lock (F3()) { } // added
        lock (F1()) { }
        lock (F4()) { } // replaced
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            // Note that the order of unique ids in temporaries follows the
            // order of declaration in the updated method. Specifically, the
            // original temporary names (and unique ids) are not preserved.
            // (Should not be an issue since the names are used by EnC only.)
            diff1.VerifyIL("C.M",
@"{
  // Code size      108 (0x6c)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                [object] V_2,
                [bool] V_3,
                object V_4,
                bool V_5,
                object V_6,
                bool V_7)
  IL_0000:  nop
  IL_0001:  call       ""object C.F3()""
  IL_0006:  stloc.s    V_4
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.s    V_5
  .try
  {
    IL_000b:  ldloc.s    V_4
    IL_000d:  ldloca.s   V_5
    IL_000f:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0014:  nop
    IL_0015:  nop
    IL_0016:  nop
    IL_0017:  leave.s    IL_0026
  }
  finally
  {
    IL_0019:  ldloc.s    V_5
    IL_001b:  brfalse.s  IL_0025
    IL_001d:  ldloc.s    V_4
    IL_001f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0024:  nop
    IL_0025:  endfinally
  }
  IL_0026:  call       ""object C.F1()""
  IL_002b:  stloc.0
  IL_002c:  ldc.i4.0
  IL_002d:  stloc.1
  .try
  {
    IL_002e:  ldloc.0
    IL_002f:  ldloca.s   V_1
    IL_0031:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0036:  nop
    IL_0037:  nop
    IL_0038:  nop
    IL_0039:  leave.s    IL_0046
  }
  finally
  {
    IL_003b:  ldloc.1
    IL_003c:  brfalse.s  IL_0045
    IL_003e:  ldloc.0
    IL_003f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0044:  nop
    IL_0045:  endfinally
  }
  IL_0046:  call       ""object C.F4()""
  IL_004b:  stloc.s    V_6
  IL_004d:  ldc.i4.0
  IL_004e:  stloc.s    V_7
  .try
  {
    IL_0050:  ldloc.s    V_6
    IL_0052:  ldloca.s   V_7
    IL_0054:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0059:  nop
    IL_005a:  nop
    IL_005b:  nop
    IL_005c:  leave.s    IL_006b
  }
  finally
  {
    IL_005e:  ldloc.s    V_7
    IL_0060:  brfalse.s  IL_006a
    IL_0062:  ldloc.s    V_6
    IL_0064:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0069:  nop
    IL_006a:  endfinally
  }
  IL_006b:  ret
}");
        }

        /// <summary>
        /// Should not reuse temporary locals
        /// having different temporary kinds.
        /// </summary>
        [Fact]
        public void NoReuseDifferentTempKind()
        {
            var source =
@"class A : System.IDisposable
{
    public object Current { get { return null; } }
    public bool MoveNext() { return false; }
    public void Dispose() { }
    internal int this[A a] { get { return 0; } set { } }
}
class B
{
    public A GetEnumerator() { return null; }
}
class C
{
    static A F() { return null; }
    static B G() { return null; }
    static void M(A a)
    {
        a[F()]++;
        using (F()) { }
        lock (F()) { }
        foreach (var o in G()) { }
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      137 (0x89)
  .maxstack  4
  .locals init ([unchanged] V_0,
                [int] V_1,
                A V_2,
                A V_3,
                bool V_4,
                A V_5,
                object V_6, //o
                A V_7,
                int V_8)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""A C.F()""
  IL_0007:  stloc.s    V_7
  IL_0009:  dup
  IL_000a:  ldloc.s    V_7
  IL_000c:  callvirt   ""int A.this[A].get""
  IL_0011:  stloc.s    V_8
  IL_0013:  ldloc.s    V_7
  IL_0015:  ldloc.s    V_8
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  callvirt   ""void A.this[A].set""
  IL_001e:  nop
  IL_001f:  call       ""A C.F()""
  IL_0024:  stloc.2
  .try
  {
    IL_0025:  nop
    IL_0026:  nop
    IL_0027:  leave.s    IL_0034
  }
  finally
  {
    IL_0029:  ldloc.2
    IL_002a:  brfalse.s  IL_0033
    IL_002c:  ldloc.2
    IL_002d:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0032:  nop
    IL_0033:  endfinally
  }
  IL_0034:  call       ""A C.F()""
  IL_0039:  stloc.3
  IL_003a:  ldc.i4.0
  IL_003b:  stloc.s    V_4
  .try
  {
    IL_003d:  ldloc.3
    IL_003e:  ldloca.s   V_4
    IL_0040:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0045:  nop
    IL_0046:  nop
    IL_0047:  nop
    IL_0048:  leave.s    IL_0056
  }
  finally
  {
    IL_004a:  ldloc.s    V_4
    IL_004c:  brfalse.s  IL_0055
    IL_004e:  ldloc.3
    IL_004f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0054:  nop
    IL_0055:  endfinally
  }
  IL_0056:  nop
  IL_0057:  call       ""B C.G()""
  IL_005c:  callvirt   ""A B.GetEnumerator()""
  IL_0061:  stloc.s    V_5
  .try
  {
    IL_0063:  br.s       IL_0070
    IL_0065:  ldloc.s    V_5
    IL_0067:  callvirt   ""object A.Current.get""
    IL_006c:  stloc.s    V_6
    IL_006e:  nop
    IL_006f:  nop
    IL_0070:  ldloc.s    V_5
    IL_0072:  callvirt   ""bool A.MoveNext()""
    IL_0077:  brtrue.s   IL_0065
    IL_0079:  leave.s    IL_0088
  }
  finally
  {
    IL_007b:  ldloc.s    V_5
    IL_007d:  brfalse.s  IL_0087
    IL_007f:  ldloc.s    V_5
    IL_0081:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0086:  nop
    IL_0087:  endfinally
  }
  IL_0088:  ret
}");
        }

        [Fact]
        public void Switch_String()
        {
            var source0 =
@"class C
{
    static string F() { return null; }
    
    static void M()
    {
        switch (F())
        {
            case ""a"": System.Console.WriteLine(1); break;
            case ""b"": System.Console.WriteLine(2); break; 
        }
    }
}";
            var source1 =
            @"class C
{
    static string F() { return null; }
    
    static void M()
    {
        switch (F())
        {
            case ""a"": System.Console.WriteLine(10); break;
            case ""b"": System.Console.WriteLine(20); break; 
        }
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            // Validate presence of a hidden sequence point @IL_0007 that is required for proper function remapping.
            v0.VerifyIL("C.M", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (string V_0,
                string V_1)
 -IL_0000:  nop
 -IL_0001:  call       ""string C.F()""
  IL_0006:  stloc.1
 ~IL_0007:  ldloc.1
  IL_0008:  stloc.0
 ~IL_0009:  ldloc.0
  IL_000a:  ldstr      ""a""
  IL_000f:  call       ""bool string.op_Equality(string, string)""
  IL_0014:  brtrue.s   IL_0025
  IL_0016:  ldloc.0
  IL_0017:  ldstr      ""b""
  IL_001c:  call       ""bool string.op_Equality(string, string)""
  IL_0021:  brtrue.s   IL_002e
  IL_0023:  br.s       IL_0037
 -IL_0025:  ldc.i4.1
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  nop
 -IL_002c:  br.s       IL_0037
 -IL_002e:  ldc.i4.2
  IL_002f:  call       ""void System.Console.WriteLine(int)""
  IL_0034:  nop
 -IL_0035:  br.s       IL_0037
 -IL_0037:  ret
}", sequencePoints: "C.M");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (string V_0,
                string V_1)
 -IL_0000:  nop
 -IL_0001:  call       ""string C.F()""
  IL_0006:  stloc.1
 ~IL_0007:  ldloc.1
  IL_0008:  stloc.0
 ~IL_0009:  ldloc.0
  IL_000a:  ldstr      ""a""
  IL_000f:  call       ""bool string.op_Equality(string, string)""
  IL_0014:  brtrue.s   IL_0025
  IL_0016:  ldloc.0
  IL_0017:  ldstr      ""b""
  IL_001c:  call       ""bool string.op_Equality(string, string)""
  IL_0021:  brtrue.s   IL_002f
  IL_0023:  br.s       IL_0039
 -IL_0025:  ldc.i4.s   10
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  nop
 -IL_002d:  br.s       IL_0039
 -IL_002f:  ldc.i4.s   20
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  nop
 -IL_0037:  br.s       IL_0039
 -IL_0039:  ret
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void Switch_Integer()
        {
            var source0 = WithWindowsLineBreaks(
@"class C
{
    static int F() { return 1; }
    
    static void M()
    {
        switch (F())
        {
            case 1: System.Console.WriteLine(1); break;
            case 2: System.Console.WriteLine(2); break; 
        }
    }
}");
            var source1 = WithWindowsLineBreaks(
@"class C
{
    static int F() { return 1; }
    
    static void M()
    {
        switch (F())
        {
            case 1: System.Console.WriteLine(10); break;
            case 2: System.Console.WriteLine(20); break; 
        }
    }
}");
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  call       ""int C.F()""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.1
  IL_000b:  beq.s      IL_0015
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.2
  IL_0011:  beq.s      IL_001e
  IL_0013:  br.s       IL_0027
  IL_0015:  ldc.i4.1
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  nop
  IL_001c:  br.s       IL_0027
  IL_001e:  ldc.i4.2
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  nop
  IL_0025:  br.s       IL_0027
  IL_0027:  ret
}");
            // Validate that we emit a hidden sequence point @IL_0007.
            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""21"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""21"" endLine=""9"" endColumn=""49"" document=""1"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""50"" endLine=""9"" endColumn=""56"" document=""1"" />
        <entry offset=""0x1e"" startLine=""10"" startColumn=""21"" endLine=""10"" endColumn=""49"" document=""1"" />
        <entry offset=""0x25"" startLine=""10"" startColumn=""50"" endLine=""10"" endColumn=""56"" document=""1"" />
        <entry offset=""0x27"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  call       ""int C.F()""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.1
  IL_000b:  beq.s      IL_0015
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.2
  IL_0011:  beq.s      IL_001f
  IL_0013:  br.s       IL_0029
  IL_0015:  ldc.i4.s   10
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  nop
  IL_001d:  br.s       IL_0029
  IL_001f:  ldc.i4.s   20
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  nop
  IL_0027:  br.s       IL_0029
  IL_0029:  ret
}");
        }

        [Fact]
        public void Switch_Patterns()
        {
            var source = WithWindowsLineBreaks(@"
using static System.Console;
class C
{
    static object F() => 1;
    static bool P() => false;
    
    static void M()
    {
        switch (F())
        {
            case 1: WriteLine(""int 1""); break;
            case byte b when P(): WriteLine(b); break; 
            case int i when P(): WriteLine(i); break;
            case (byte)1: WriteLine(""byte 1""); break; 
            case int j: WriteLine(j); break; 
            case object o: WriteLine(o); break; 
        }
    }
}");
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""106"" />
          <slot kind=""0"" offset=""162"" />
          <slot kind=""0"" offset=""273"" />
          <slot kind=""0"" offset=""323"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.ExcludeScopes | PdbValidationOptions.ExcludeSequencePoints);

            v0.VerifyIL("C.M", @"
{
  // Code size      147 (0x93)
  .maxstack  2
  .locals init (byte V_0, //b
                int V_1, //i
                int V_2, //j
                object V_3, //o
                object V_4)
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.s    V_4
  IL_0008:  ldloc.s    V_4
  IL_000a:  stloc.3
  IL_000b:  ldloc.3
  IL_000c:  isinst     ""int""
  IL_0011:  brfalse.s  IL_0020
  IL_0013:  ldloc.3
  IL_0014:  unbox.any  ""int""
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4.1
  IL_001c:  beq.s      IL_003c
  IL_001e:  br.s       IL_005b
  IL_0020:  ldloc.3
  IL_0021:  isinst     ""byte""
  IL_0026:  brfalse.s  IL_0037
  IL_0028:  ldloc.3
  IL_0029:  unbox.any  ""byte""
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0049
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  beq.s      IL_006d
  IL_0035:  br.s       IL_0087
  IL_0037:  ldloc.3
  IL_0038:  brtrue.s   IL_0087
  IL_003a:  br.s       IL_0092
  IL_003c:  ldstr      ""int 1""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  nop
  IL_0047:  br.s       IL_0092
  IL_0049:  call       ""bool C.P()""
  IL_004e:  brtrue.s   IL_0052
  IL_0050:  br.s       IL_0031
  IL_0052:  ldloc.0
  IL_0053:  call       ""void System.Console.WriteLine(int)""
  IL_0058:  nop
  IL_0059:  br.s       IL_0092
  IL_005b:  call       ""bool C.P()""
  IL_0060:  brtrue.s   IL_0064
  IL_0062:  br.s       IL_007a
  IL_0064:  ldloc.1
  IL_0065:  call       ""void System.Console.WriteLine(int)""
  IL_006a:  nop
  IL_006b:  br.s       IL_0092
  IL_006d:  ldstr      ""byte 1""
  IL_0072:  call       ""void System.Console.WriteLine(string)""
  IL_0077:  nop
  IL_0078:  br.s       IL_0092
  IL_007a:  ldloc.1
  IL_007b:  stloc.2
  IL_007c:  br.s       IL_007e
  IL_007e:  ldloc.2
  IL_007f:  call       ""void System.Console.WriteLine(int)""
  IL_0084:  nop
  IL_0085:  br.s       IL_0092
  IL_0087:  br.s       IL_0089
  IL_0089:  ldloc.3
  IL_008a:  call       ""void System.Console.WriteLine(object)""
  IL_008f:  nop
  IL_0090:  br.s       IL_0092
  IL_0092:  ret
}");
            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size      147 (0x93)
  .maxstack  2
  .locals init (byte V_0, //b
                int V_1, //i
                int V_2, //j
                object V_3, //o
                object V_4)
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.s    V_4
  IL_0008:  ldloc.s    V_4
  IL_000a:  stloc.3
  IL_000b:  ldloc.3
  IL_000c:  isinst     ""int""
  IL_0011:  brfalse.s  IL_0020
  IL_0013:  ldloc.3
  IL_0014:  unbox.any  ""int""
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4.1
  IL_001c:  beq.s      IL_003c
  IL_001e:  br.s       IL_005b
  IL_0020:  ldloc.3
  IL_0021:  isinst     ""byte""
  IL_0026:  brfalse.s  IL_0037
  IL_0028:  ldloc.3
  IL_0029:  unbox.any  ""byte""
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0049
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  beq.s      IL_006d
  IL_0035:  br.s       IL_0087
  IL_0037:  ldloc.3
  IL_0038:  brtrue.s   IL_0087
  IL_003a:  br.s       IL_0092
  IL_003c:  ldstr      ""int 1""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  nop
  IL_0047:  br.s       IL_0092
  IL_0049:  call       ""bool C.P()""
  IL_004e:  brtrue.s   IL_0052
  IL_0050:  br.s       IL_0031
  IL_0052:  ldloc.0
  IL_0053:  call       ""void System.Console.WriteLine(int)""
  IL_0058:  nop
  IL_0059:  br.s       IL_0092
  IL_005b:  call       ""bool C.P()""
  IL_0060:  brtrue.s   IL_0064
  IL_0062:  br.s       IL_007a
  IL_0064:  ldloc.1
  IL_0065:  call       ""void System.Console.WriteLine(int)""
  IL_006a:  nop
  IL_006b:  br.s       IL_0092
  IL_006d:  ldstr      ""byte 1""
  IL_0072:  call       ""void System.Console.WriteLine(string)""
  IL_0077:  nop
  IL_0078:  br.s       IL_0092
  IL_007a:  ldloc.1
  IL_007b:  stloc.2
  IL_007c:  br.s       IL_007e
  IL_007e:  ldloc.2
  IL_007f:  call       ""void System.Console.WriteLine(int)""
  IL_0084:  nop
  IL_0085:  br.s       IL_0092
  IL_0087:  br.s       IL_0089
  IL_0089:  ldloc.3
  IL_008a:  call       ""void System.Console.WriteLine(object)""
  IL_008f:  nop
  IL_0090:  br.s       IL_0092
  IL_0092:  ret
}");
        }

        [Fact]
        public void If()
        {
            var source0 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        if (F())
        {
            System.Console.WriteLine(1);
        }
    }
}");
            var source1 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        if (F())
        {
            System.Console.WriteLine(10);
        }
    }
}");
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  call       ""bool C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0013
  IL_000a:  nop
  IL_000b:  ldc.i4.1
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  ret
}
");
            // Validate presence of a hidden sequence point @IL_0007 that is required for proper function remapping.
            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""17"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""1"" />
        <entry offset=""0x12"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x13"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.IfStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  call       ""bool C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  nop
  IL_000b:  ldc.i4.s   10
  IL_000d:  call       ""void System.Console.WriteLine(int)""
  IL_0012:  nop
  IL_0013:  nop
  IL_0014:  ret
}");
        }

        [Fact]
        public void While()
        {
            var source0 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        while (F())
        {
            System.Console.WriteLine(1);
        }
    }
}");
            var source1 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        while (F())
        {
            System.Console.WriteLine(10);
        }
    }
}");
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  br.s       IL_000c
  IL_0003:  nop
  IL_0004:  ldc.i4.1
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  nop
  IL_000b:  nop
  IL_000c:  call       ""bool C.F()""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  brtrue.s   IL_0003
  IL_0015:  ret
}
");
            // Validate presence of a hidden sequence point @IL_0012 that is required for proper function remapping.
            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0x3"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""1"" />
        <entry offset=""0xb"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""20"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.WhileStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  br.s       IL_000d
  IL_0003:  nop
  IL_0004:  ldc.i4.s   10
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  nop
  IL_000c:  nop
  IL_000d:  call       ""bool C.F()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  brtrue.s   IL_0003
  IL_0016:  ret
}");
        }

        [Fact]
        public void Do1()
        {
            var source0 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        do
        {
            System.Console.WriteLine(1);
        }
        while (F());
    }
}");
            var source1 = WithWindowsLineBreaks(@"
class C
{
    static bool F() { return true; }
    
    static void M()
    {
        do
        {
            System.Console.WriteLine(10);
        }
        while (F());
    }
}");

            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""void System.Console.WriteLine(int)""
  IL_0008:  nop
  IL_0009:  nop
  IL_000a:  call       ""bool C.F()""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  brtrue.s   IL_0001
  IL_0013:  ret
}");
            // Validate presence of a hidden sequence point @IL_0010 that is required for proper function remapping.
            v0.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""1"" />
        <entry offset=""0x9"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""21"" document=""1"" />
        <entry offset=""0x10"" hidden=""true"" document=""1"" />
        <entry offset=""0x13"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.DoStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldc.i4.s   10
  IL_0004:  call       ""void System.Console.WriteLine(int)""
  IL_0009:  nop
  IL_000a:  nop
  IL_000b:  call       ""bool C.F()""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  brtrue.s   IL_0001
  IL_0014:  ret
}");
        }

        [Fact]
        public void For()
        {
            var source0 = @"
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
            var source1 = @"
class C
{
    static bool F(int i) { return true; }
    static void G(int i) { }
    
    static void M()
    {
        for (int i = 1; F(i); G(i))
        {
            System.Console.WriteLine(10);
        }
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            // Validate presence of a hidden sequence point @IL_001c that is required for proper function remapping.
            v0.VerifyIL("C.M", @"
{
  // Code size       32 (0x20)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
 ~IL_0003:  br.s       IL_0015
 -IL_0005:  nop
 -IL_0006:  ldc.i4.1
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  nop
 -IL_000d:  nop
 -IL_000e:  ldloc.0
  IL_000f:  call       ""void C.G(int)""
  IL_0014:  nop
 -IL_0015:  ldloc.0
  IL_0016:  call       ""bool C.F(int)""
  IL_001b:  stloc.1
 ~IL_001c:  ldloc.1
  IL_001d:  brtrue.s   IL_0005
 -IL_001f:  ret
}", sequencePoints: "C.M");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.ForStatement, SyntaxKind.VariableDeclarator), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0016
  IL_0005:  nop
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""void System.Console.WriteLine(int)""
  IL_000d:  nop
  IL_000e:  nop
  IL_000f:  ldloc.0
  IL_0010:  call       ""void C.G(int)""
  IL_0015:  nop
  IL_0016:  ldloc.0
  IL_0017:  call       ""bool C.F(int)""
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  brtrue.s   IL_0005
  IL_0020:  ret
}
");
        }

        [Fact]
        public void SynthesizedVariablesInLambdas1()
        {
            var source =
@"class C
{
    static object F()
    {
        return null;
    }
    static void M()
    {
        lock (F())
        {
            var f = new System.Action(() => 
            {
                lock (F())
                {
                }
            });
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.<>c.<M>b__1_0()", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldloc.0
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  leave.s    IL_0021
  }
  finally
  {
    IL_0016:  ldloc.1
    IL_0017:  brfalse.s  IL_0020
    IL_0019:  ldloc.0
    IL_001a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001f:  nop
    IL_0020:  endfinally
  }
  IL_0021:  ret
}");

#if TODO // identify the lambda in a semantic edit
            var methodData0 = v0.TestData.GetMethodData("C.<M>b__0");
            var method0 = compilation0.GetMember<MethodSymbol>("C.<M>b__0");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.<M>b__0");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.<M>b__0", @"
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
#endif
        }

        [Fact]
        public void SynthesizedVariablesInLambdas2()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static void M()
    {
        var <N:0>f1</N:0> = new Action<int[], int>(<N:1>(a, _) =>
        {
            <N:2>foreach</N:2> (var x in a)
            {
                Console.WriteLine(1); // change to 10 and then to 100
            }
        }</N:1>);

        var <N:3>f2</N:3> = new Action<int[], int>(<N:4>(a, _) =>
        {
            <N:5>foreach</N:5> (var x in a)
            {
                Console.WriteLine(20);
            }
        }</N:4>);

        f1(new[] { 1, 2 }, 1);
        f2(new[] { 1, 2 }, 1);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static void M()
    {
        var <N:0>f1</N:0> = new Action<int[], int>(<N:1>(a, _) =>
        {
            <N:2>foreach</N:2> (var x in a)
            {
                Console.WriteLine(10); // change to 10 and then to 100
            }
        }</N:1>);

        var <N:3>f2</N:3> = new Action<int[], int>(<N:4>(a, _) =>
        {
            <N:5>foreach</N:5> (var x in a)
            {
                Console.WriteLine(20);
            }
        }</N:4>);

        f1(new[] { 1, 2 }, 1);
        f2(new[] { 1, 2 }, 1);
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static void M()
    {
        var <N:0>f1</N:0> = new Action<int[], int>(<N:1>(a, _) =>
        {
            <N:2>foreach</N:2> (var x in a)
            {
                Console.WriteLine(100); // change to 10 and then to 100
            }
        }</N:1>);

        var <N:3>f2</N:3> = new Action<int[], int>(<N:4>(a, _) =>
        {
            <N:5>foreach</N:5> (var x in a)
            {
                Console.WriteLine(20);
            }
        }</N:4>);

        f1(new[] { 1, 2 }, 1);
        f2(new[] { 1, 2 }, 1);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var m0 = compilation0.GetMember<MethodSymbol>("C.M");
            var m1 = compilation1.GetMember<MethodSymbol>("C.M");
            var m2 = compilation2.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, m1, m2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <M>b__0_0, <M>b__0_1}");

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <M>b__0_0, <M>b__0_1}");

            var expectedIL = @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int[] V_0,
                int V_1,
                int V_2) //x
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.1
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_001a
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  ldelem.i4
  IL_000b:  stloc.2
  IL_000c:  nop
  IL_000d:  ldc.i4.s   20
  IL_000f:  call       ""void System.Console.WriteLine(int)""
  IL_0014:  nop
  IL_0015:  nop
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  ldloc.0
  IL_001c:  ldlen
  IL_001d:  conv.i4
  IL_001e:  blt.s      IL_0008
  IL_0020:  ret
}";

            diff1.VerifyIL(@"C.<>c.<M>b__0_1", expectedIL);
            diff2.VerifyIL(@"C.<>c.<M>b__0_1", expectedIL);
        }

        [Fact]
        public void SynthesizedVariablesInIterator1()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        lock (F()) { }
        yield return 1;
    }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      131 (0x83)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_007a
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldarg.0
  IL_0022:  ldfld      ""C C.<F>d__0.<>4__this""
  IL_0027:  call       ""System.Collections.Generic.IEnumerable<int> C.F()""
  IL_002c:  stfld      ""System.Collections.Generic.IEnumerable<int> C.<F>d__0.<>s__1""
  IL_0031:  ldarg.0
  IL_0032:  ldc.i4.0
  IL_0033:  stfld      ""bool C.<F>d__0.<>s__2""
  .try
  {
    IL_0038:  ldarg.0
    IL_0039:  ldfld      ""System.Collections.Generic.IEnumerable<int> C.<F>d__0.<>s__1""
    IL_003e:  ldarg.0
    IL_003f:  ldflda     ""bool C.<F>d__0.<>s__2""
    IL_0044:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0049:  nop
    IL_004a:  nop
    IL_004b:  nop
    IL_004c:  leave.s    IL_0063
  }
  finally
  {
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""bool C.<F>d__0.<>s__2""
    IL_0054:  brfalse.s  IL_0062
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Collections.Generic.IEnumerable<int> C.<F>d__0.<>s__1""
    IL_005c:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0061:  nop
    IL_0062:  endfinally
  }
  IL_0063:  ldarg.0
  IL_0064:  ldnull
  IL_0065:  stfld      ""System.Collections.Generic.IEnumerable<int> C.<F>d__0.<>s__1""
  IL_006a:  ldarg.0
  IL_006b:  ldc.i4.1
  IL_006c:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0071:  ldarg.0
  IL_0072:  ldc.i4.1
  IL_0073:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0078:  ldc.i4.1
  IL_0079:  ret
  IL_007a:  ldarg.0
  IL_007b:  ldc.i4.m1
  IL_007c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0081:  ldc.i4.0
  IL_0082:  ret
}");

#if TODO 
            var methodData0 = v0.TestData.GetMethodData("?");
            var method0 = compilation0.GetMember<MethodSymbol>("?");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("?");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("?", @"
{", methodToken: diff1.EmitResult.UpdatedMethods.Single());
#endif
        }

        [Fact]
        public void SynthesizedVariablesInAsyncMethod1()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        lock (F()) { }
        await F();
        return 1;
    }
}
";
            var compilation0 = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      246 (0xf6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_0011
    IL_000c:  br         IL_009e
   -IL_0011:  nop
   -IL_0012:  ldarg.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0019:  call       ""System.Threading.Tasks.Task<int> C.F()""
    IL_001e:  stfld      ""System.Threading.Tasks.Task<int> C.<F>d__0.<>s__1""
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  stfld      ""bool C.<F>d__0.<>s__2""
    .try
    {
      IL_002a:  ldarg.0
      IL_002b:  ldfld      ""System.Threading.Tasks.Task<int> C.<F>d__0.<>s__1""
      IL_0030:  ldarg.0
      IL_0031:  ldflda     ""bool C.<F>d__0.<>s__2""
      IL_0036:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
      IL_003b:  nop
     -IL_003c:  nop
     -IL_003d:  nop
      IL_003e:  leave.s    IL_0059
    }
    finally
    {
     ~IL_0040:  ldloc.0
      IL_0041:  ldc.i4.0
      IL_0042:  bge.s      IL_0058
      IL_0044:  ldarg.0
      IL_0045:  ldfld      ""bool C.<F>d__0.<>s__2""
      IL_004a:  brfalse.s  IL_0058
      IL_004c:  ldarg.0
      IL_004d:  ldfld      ""System.Threading.Tasks.Task<int> C.<F>d__0.<>s__1""
      IL_0052:  call       ""void System.Threading.Monitor.Exit(object)""
      IL_0057:  nop
     ~IL_0058:  endfinally
    }
   ~IL_0059:  ldarg.0
    IL_005a:  ldnull
    IL_005b:  stfld      ""System.Threading.Tasks.Task<int> C.<F>d__0.<>s__1""
   -IL_0060:  ldarg.0
    IL_0061:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0066:  call       ""System.Threading.Tasks.Task<int> C.F()""
    IL_006b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0070:  stloc.2
   ~IL_0071:  ldloca.s   V_2
    IL_0073:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0078:  brtrue.s   IL_00ba
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.0
    IL_007c:  dup
    IL_007d:  stloc.0
    IL_007e:  stfld      ""int C.<F>d__0.<>1__state""
   <IL_0083:  ldarg.0
    IL_0084:  ldloc.2
    IL_0085:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_008a:  ldarg.0
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0092:  ldloca.s   V_2
    IL_0094:  ldloca.s   V_3
    IL_0096:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_009b:  nop
    IL_009c:  leave.s    IL_00f5
   >IL_009e:  ldarg.0
    IL_009f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00a4:  stloc.2
    IL_00a5:  ldarg.0
    IL_00a6:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00ab:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.m1
    IL_00b3:  dup
    IL_00b4:  stloc.0
    IL_00b5:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00ba:  ldloca.s   V_2
    IL_00bc:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00c1:  pop
   -IL_00c2:  ldc.i4.1
    IL_00c3:  stloc.1
    IL_00c4:  leave.s    IL_00e0
  }
  catch System.Exception
  {
   ~IL_00c6:  stloc.s    V_4
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.s   -2
    IL_00cb:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_00d6:  ldloc.s    V_4
    IL_00d8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00dd:  nop
    IL_00de:  leave.s    IL_00f5
  }
 -IL_00e0:  ldarg.0
  IL_00e1:  ldc.i4.s   -2
  IL_00e3:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_00e8:  ldarg.0
  IL_00e9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00ee:  ldloc.1
  IL_00ef:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00f4:  nop
  IL_00f5:  ret
}", sequencePoints: "C+<F>d__0.MoveNext");

#if TODO
            var methodData0 = v0.TestData.GetMethodData("?");
            var method0 = compilation0.GetMember<MethodSymbol>("?");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("?");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("?", @"
{", methodToken: diff1.EmitResult.UpdatedMethods.Single());
#endif
        }

        [Fact]
        public void OutVar()
        {
            var source = @"
class C
{
    static void F(out int x, out int y) { x = 1; y = 2; }
    static int G() { F(out int x, out var y); return x + y; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                [int] V_2,
                int V_3)
 -IL_0000:  nop
 -IL_0001:  ldloca.s   V_0
  IL_0003:  ldloca.s   V_1
  IL_0005:  call       ""void C.F(out int, out int)""
  IL_000a:  nop
 -IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  add
  IL_000e:  stloc.3
  IL_000f:  br.s       IL_0011
 -IL_0011:  ldloc.3
  IL_0012:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void PatternVariable()
        {
            var source = @"
class C
{
    static int F(object o) { if (o is int i) { return i; } return 0; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1,
                [int] V_2,
                int V_3)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
 ~IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_001d
 -IL_0018:  nop
 -IL_0019:  ldloc.0
  IL_001a:  stloc.3
  IL_001b:  br.s       IL_0021
 -IL_001d:  ldc.i4.0
  IL_001e:  stloc.3
  IL_001f:  br.s       IL_0021
 -IL_0021:  ldloc.3
  IL_0022:  ret
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void Tuple_Parenthesized()
        {
            var source = @"
class C
{
    static int F() { (int, (int, int)) x = (1, (2, 3)); return x.Item1 + x.Item2.Item1 + x.Item2.Item2; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (System.ValueTuple<int, System.ValueTuple<int, int>> V_0, //x
                [int] V_1,
                int V_2)
 -IL_0000:  nop
 -IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  call       ""System.ValueTuple<int, System.ValueTuple<int, int>>..ctor(int, System.ValueTuple<int, int>)""
 -IL_0010:  ldloc.0
  IL_0011:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<int, int>>.Item1""
  IL_0016:  ldloc.0
  IL_0017:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, System.ValueTuple<int, int>>.Item2""
  IL_001c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0021:  add
  IL_0022:  ldloc.0
  IL_0023:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, System.ValueTuple<int, int>>.Item2""
  IL_0028:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002d:  add
  IL_002e:  stloc.2
  IL_002f:  br.s       IL_0031
 -IL_0031:  ldloc.2
  IL_0032:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void Tuple_Decomposition()
        {
            var source = @"
class C
{
    static int F() { (int x, (int y, int z)) = (1, (2, 3)); return x + y + z; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                int V_2, //z
                [int] V_3,
                int V_4)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  stloc.1
  IL_0005:  ldc.i4.3
  IL_0006:  stloc.2
 -IL_0007:  ldloc.0
  IL_0008:  ldloc.1
  IL_0009:  add
  IL_000a:  ldloc.2
  IL_000b:  add
  IL_000c:  stloc.s    V_4
  IL_000e:  br.s       IL_0010
 -IL_0010:  ldloc.s    V_4
  IL_0012:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void PatternMatching_Variable()
        {
            var source = @"
class C
{
    static int F(object o) { if (o is int i) { return i; } return 0; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1,
                [int] V_2,
                int V_3)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
 ~IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_001d
 -IL_0018:  nop
 -IL_0019:  ldloc.0
  IL_001a:  stloc.3
  IL_001b:  br.s       IL_0021
 -IL_001d:  ldc.i4.0
  IL_001e:  stloc.3
  IL_001f:  br.s       IL_0021
 -IL_0021:  ldloc.3
  IL_0022:  ret
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void PatternMatching_NoVariable()
        {
            var source = @"
class C
{
    static int F(object o) { if ((o is bool) || (o is 0)) { return 0; } return 1; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (bool V_0,
                [int] V_1,
                int V_2)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  isinst     ""bool""
  IL_0007:  brtrue.s   IL_001f
  IL_0009:  ldarg.0
  IL_000a:  isinst     ""int""
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldarg.0
  IL_0012:  unbox.any  ""int""
  IL_0017:  ldc.i4.0
  IL_0018:  ceq
  IL_001a:  br.s       IL_001d
  IL_001c:  ldc.i4.0
  IL_001d:  br.s       IL_0020
  IL_001f:  ldc.i4.1
  IL_0020:  stloc.0
 ~IL_0021:  ldloc.0
  IL_0022:  brfalse.s  IL_0029
 -IL_0024:  nop
 -IL_0025:  ldc.i4.0
  IL_0026:  stloc.2
  IL_0027:  br.s       IL_002d
 -IL_0029:  ldc.i4.1
  IL_002a:  stloc.2
  IL_002b:  br.s       IL_002d
 -IL_002d:  ldloc.2
  IL_002e:  ret
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void VarPattern()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static object G(object o1, object o2)
    {
        return (o1, o2) switch
        {
            (int a, string b) => a,
            _ => 0
        };
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
    {
      // Code size       62 (0x3e)
      .maxstack  1
      .locals init (int V_0, //a
                    string V_1, //b
                    [int] V_2,
                    [object] V_3,
                    int V_4,
                    object V_5)
     -IL_0000:  nop
     -IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
     -IL_0004:  nop
     ~IL_0005:  ldarg.0
      IL_0006:  isinst     ""int""
      IL_000b:  brfalse.s  IL_0027
      IL_000d:  ldarg.0
      IL_000e:  unbox.any  ""int""
      IL_0013:  stloc.0
     ~IL_0014:  ldarg.1
      IL_0015:  isinst     ""string""
      IL_001a:  stloc.1
      IL_001b:  ldloc.1
      IL_001c:  brtrue.s   IL_0020
      IL_001e:  br.s       IL_0027
     ~IL_0020:  br.s       IL_0022
     -IL_0022:  ldloc.0
      IL_0023:  stloc.s    V_4
      IL_0025:  br.s       IL_002c
     -IL_0027:  ldc.i4.0
      IL_0028:  stloc.s    V_4
      IL_002a:  br.s       IL_002c
     ~IL_002c:  ldc.i4.1
      IL_002d:  brtrue.s   IL_0030
     -IL_002f:  nop
     ~IL_0030:  ldloc.s    V_4
      IL_0032:  box        ""int""
      IL_0037:  stloc.s    V_5
      IL_0039:  br.s       IL_003b
     -IL_003b:  ldloc.s    V_5
      IL_003d:  ret
    }
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void RecursiveSwitchExpression()
        {
            var source = @"
class C
{
    static object G(object o)
    {
        return o switch
        {
            int i => i switch
            {
                0  => 1,
                _ => 2,
            },
            _ => 3
        };
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
    {
      // Code size       76 (0x4c)
      .maxstack  1
      .locals init (int V_0, //i
                    [int] V_1,
                    [int] V_2,
                    [object] V_3,
                    int V_4,
                    int V_5,
                    object V_6)
     -IL_0000:  nop
     -IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
     -IL_0004:  nop
     ~IL_0005:  ldarg.0
      IL_0006:  isinst     ""int""
      IL_000b:  brfalse.s  IL_0035
      IL_000d:  ldarg.0
      IL_000e:  unbox.any  ""int""
      IL_0013:  stloc.0
     ~IL_0014:  br.s       IL_0016
     ~IL_0016:  br.s       IL_0018
      IL_0018:  ldc.i4.1
      IL_0019:  brtrue.s   IL_001c
     -IL_001b:  nop
     ~IL_001c:  ldloc.0
      IL_001d:  brfalse.s  IL_0021
      IL_001f:  br.s       IL_0026
     -IL_0021:  ldc.i4.1
      IL_0022:  stloc.s    V_5
      IL_0024:  br.s       IL_002b
     -IL_0026:  ldc.i4.2
      IL_0027:  stloc.s    V_5
      IL_0029:  br.s       IL_002b
     ~IL_002b:  ldc.i4.1
      IL_002c:  brtrue.s   IL_002f
     -IL_002e:  nop
     -IL_002f:  ldloc.s    V_5
      IL_0031:  stloc.s    V_4
      IL_0033:  br.s       IL_003a
     -IL_0035:  ldc.i4.3
      IL_0036:  stloc.s    V_4
      IL_0038:  br.s       IL_003a
     ~IL_003a:  ldc.i4.1
      IL_003b:  brtrue.s   IL_003e
     -IL_003d:  nop
     ~IL_003e:  ldloc.s    V_4
      IL_0040:  box        ""int""
      IL_0045:  stloc.s    V_6
      IL_0047:  br.s       IL_0049
     -IL_0049:  ldloc.s    V_6
      IL_004b:  ret
    }
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void RecursiveSwitchExpressionWithAwait()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static async Task<object> G(object o)
    {
        return o switch
        {
            Task<int> i when await i > 0 => await i switch
            {
                1 => 1,
                _ => 2,
            },
            _ => 3
        };
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, g0, g1, GetEquivalentNodesMap(g1, g0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
    {
      // Code size       56 (0x38)
      .maxstack  2
      .locals init (C.<G>d__0 V_0)
     ~IL_0000:  newobj     ""C.<G>d__0..ctor()""
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
     ~IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Create()""
      IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0011:  ldloc.0
      IL_0012:  ldarg.0
      IL_0013:  stfld      ""object C.<G>d__0.o""
      IL_0018:  ldloc.0
     -IL_0019:  ldc.i4.m1
     -IL_001a:  stfld      ""int C.<G>d__0.<>1__state""
      IL_001f:  ldloc.0
      IL_0020:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0025:  ldloca.s   V_0
      IL_0027:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<C.<G>d__0>(ref C.<G>d__0)""
      IL_002c:  ldloc.0
      IL_002d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0032:  call       ""System.Threading.Tasks.Task<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Task.get""
      IL_0037:  ret
    }
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void SwitchExpressionInsideAwait()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static async Task<object> G(Task<object> o)
    {
        return await o switch 
        {
            int i => 0,
            _ => 1
        };
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
    {
      // Code size       56 (0x38)
      .maxstack  2
      .locals init (C.<G>d__0 V_0)
     ~IL_0000:  newobj     ""C.<G>d__0..ctor()""
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
     ~IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Create()""
      IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0011:  ldloc.0
      IL_0012:  ldarg.0
      IL_0013:  stfld      ""System.Threading.Tasks.Task<object> C.<G>d__0.o""
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4.m1
      IL_001a:  stfld      ""int C.<G>d__0.<>1__state""
      IL_001f:  ldloc.0
      IL_0020:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0025:  ldloca.s   V_0
      IL_0027:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<C.<G>d__0>(ref C.<G>d__0)""
      IL_002c:  ldloc.0
     <IL_002d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> C.<G>d__0.<>t__builder""
      IL_0032:  call       ""System.Threading.Tasks.Task<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Task.get""
      IL_0037:  ret
    }
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void SwitchExpressionWithOutVar()
        {
            var source = @"
class C
{
    static object G()
    {
        return N(out var x) switch 
        {
            null => x switch {1 =>  1, _ => 2 },
            _ => 1
        };
    }

    static object N(out int x) { x = 1; return null; }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
    {
      // Code size       73 (0x49)
      .maxstack  2
      .locals init (int V_0, //x
                    [int] V_1,
                    [object] V_2,
                    [int] V_3,
                    [object] V_4,
                    int V_5,
                    object V_6,
                    int V_7,
                    object V_8)
     -IL_0000:  nop
     -IL_0001:  ldloca.s   V_0
      IL_0003:  call       ""object C.N(out int)""
      IL_0008:  stloc.s    V_6
      IL_000a:  ldc.i4.1
      IL_000b:  brtrue.s   IL_000e
     -IL_000d:  nop
     ~IL_000e:  ldloc.s    V_6
      IL_0010:  brfalse.s  IL_0014
      IL_0012:  br.s       IL_0032
     ~IL_0014:  ldc.i4.1
      IL_0015:  brtrue.s   IL_0018
     -IL_0017:  nop
     ~IL_0018:  ldloc.0
      IL_0019:  ldc.i4.1
      IL_001a:  beq.s      IL_001e
      IL_001c:  br.s       IL_0023
     -IL_001e:  ldc.i4.1
      IL_001f:  stloc.s    V_7
      IL_0021:  br.s       IL_0028
     -IL_0023:  ldc.i4.2
      IL_0024:  stloc.s    V_7
      IL_0026:  br.s       IL_0028
     ~IL_0028:  ldc.i4.1
      IL_0029:  brtrue.s   IL_002c
     -IL_002b:  nop
     -IL_002c:  ldloc.s    V_7
      IL_002e:  stloc.s    V_5
      IL_0030:  br.s       IL_0037
     -IL_0032:  ldc.i4.1
      IL_0033:  stloc.s    V_5
      IL_0035:  br.s       IL_0037
     ~IL_0037:  ldc.i4.1
      IL_0038:  brtrue.s   IL_003b
     -IL_003a:  nop
     ~IL_003b:  ldloc.s    V_5
      IL_003d:  box        ""int""
      IL_0042:  stloc.s    V_8
      IL_0044:  br.s       IL_0046
     -IL_0046:  ldloc.s    V_8
      IL_0048:  ret
    }
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void ForEachStatement_Deconstruction()
        {
            var source = @"
class C
{
    public static (int, (bool, double))[] F() => new[] { (1, (true, 2.0)) };

    public static void G()
    {        
        foreach (var (x, (y, z)) in F())
        {
            System.Console.WriteLine(x);
        }
    }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.G");
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.G");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [int] V_1,
                int V_2, //x
                bool V_3, //y
                double V_4, //z
                [unchanged] V_5,
                System.ValueTuple<int, System.ValueTuple<bool, double>>[] V_6,
                int V_7,
                System.ValueTuple<bool, double> V_8)
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  call       ""System.ValueTuple<int, System.ValueTuple<bool, double>>[] C.F()""
  IL_0007:  stloc.s    V_6
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.s    V_7
 ~IL_000c:  br.s       IL_0045
 -IL_000e:  ldloc.s    V_6
  IL_0010:  ldloc.s    V_7
  IL_0012:  ldelem     ""System.ValueTuple<int, System.ValueTuple<bool, double>>""
  IL_0017:  dup
  IL_0018:  ldfld      ""System.ValueTuple<bool, double> System.ValueTuple<int, System.ValueTuple<bool, double>>.Item2""
  IL_001d:  stloc.s    V_8
  IL_001f:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<bool, double>>.Item1""
  IL_0024:  stloc.2
  IL_0025:  ldloc.s    V_8
  IL_0027:  ldfld      ""bool System.ValueTuple<bool, double>.Item1""
  IL_002c:  stloc.3
  IL_002d:  ldloc.s    V_8
  IL_002f:  ldfld      ""double System.ValueTuple<bool, double>.Item2""
  IL_0034:  stloc.s    V_4
 -IL_0036:  nop
 -IL_0037:  ldloc.2
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  nop
 -IL_003e:  nop
 ~IL_003f:  ldloc.s    V_7
  IL_0041:  ldc.i4.1
  IL_0042:  add
  IL_0043:  stloc.s    V_7
 -IL_0045:  ldloc.s    V_7
  IL_0047:  ldloc.s    V_6
  IL_0049:  ldlen
  IL_004a:  conv.i4
  IL_004b:  blt.s      IL_000e
 -IL_004d:  ret
}
", methodToken: diff1.EmitResult.UpdatedMethods.Single());
        }

        [Fact]
        public void ComplexTypes()
        {
            var sourceText = @"
using System;
using System.Collections.Generic;

class C1<T>
{
    public enum E
    {
        A
    }
}

class C
{
    public unsafe static void G()
    {        
        var <N:0>a</N:0> = new { key = ""a"", value = new List<(int, int)>()};
        var <N:1>b</N:1> = (number: 5, value: a);
        var <N:2>c</N:2> = new[] { b };
        int[] <N:3>array</N:3> = { 1, 2, 3 };
        ref int <N:4>d</N:4> = ref array[0];
        ref readonly int <N:5>e</N:5> = ref array[0];
        C1<(int, dynamic)>.E***[,,] <N:6>x</N:6> = null;
        var <N:7>f</N:7> = new List<string?>();
    }
}
";
            var source0 = MarkedSource(sourceText, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
            var source1 = MarkedSource(sourceText, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
            var source2 = MarkedSource(sourceText, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithAllowUnsafe(true));

            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.G");
            var f1 = compilation1.GetMember<MethodSymbol>("C.G");
            var f2 = compilation2.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.G", @"
    {
      // Code size       88 (0x58)
      .maxstack  4
      .locals init (<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>> V_0, //a
                    System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>> V_1, //b
                    System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>[] V_2, //c
                    int[] V_3, //array
                    int& V_4, //d
                    int& V_5, //e
                    C1<System.ValueTuple<int, dynamic>>.E***[,,] V_6, //x
                    System.Collections.Generic.List<string> V_7) //f
      IL_0000:  nop
      IL_0001:  ldstr      ""a""
      IL_0006:  newobj     ""System.Collections.Generic.List<System.ValueTuple<int, int>>..ctor()""
      IL_000b:  newobj     ""<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>>..ctor(string, System.Collections.Generic.List<System.ValueTuple<int, int>>)""
      IL_0010:  stloc.0
      IL_0011:  ldloca.s   V_1
      IL_0013:  ldc.i4.5
      IL_0014:  ldloc.0
      IL_0015:  call       ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>..ctor(int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>)""
      IL_001a:  ldc.i4.1
      IL_001b:  newarr     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
      IL_0020:  dup
      IL_0021:  ldc.i4.0
      IL_0022:  ldloc.1
      IL_0023:  stelem     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
      IL_0028:  stloc.2
      IL_0029:  ldc.i4.3
      IL_002a:  newarr     ""int""
      IL_002f:  dup
      IL_0030:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
      IL_0035:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
      IL_003a:  stloc.3
      IL_003b:  ldloc.3
      IL_003c:  ldc.i4.0
      IL_003d:  ldelema    ""int""
      IL_0042:  stloc.s    V_4
      IL_0044:  ldloc.3
      IL_0045:  ldc.i4.0
      IL_0046:  ldelema    ""int""
      IL_004b:  stloc.s    V_5
      IL_004d:  ldnull
      IL_004e:  stloc.s    V_6
      IL_0050:  newobj     ""System.Collections.Generic.List<string>..ctor()""
      IL_0055:  stloc.s    V_7
      IL_0057:  ret
    }
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>> V_0, //a
                System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>> V_1, //b
                System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>[] V_2, //c
                int[] V_3, //array
                int& V_4, //d
                int& V_5, //e
                C1<System.ValueTuple<int, dynamic>>.E***[,,] V_6, //x
                System.Collections.Generic.List<string> V_7) //f
  IL_0000:  nop
  IL_0001:  ldstr      ""a""
  IL_0006:  newobj     ""System.Collections.Generic.List<System.ValueTuple<int, int>>..ctor()""
  IL_000b:  newobj     ""<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>>..ctor(string, System.Collections.Generic.List<System.ValueTuple<int, int>>)""
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.5
  IL_0014:  ldloc.0
  IL_0015:  call       ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>..ctor(int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>)""
  IL_001a:  ldc.i4.1
  IL_001b:  newarr     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldloc.1
  IL_0023:  stelem     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
  IL_0028:  stloc.2
  IL_0029:  ldc.i4.3
  IL_002a:  newarr     ""int""
  IL_002f:  dup
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i4
  IL_0033:  dup
  IL_0034:  ldc.i4.1
  IL_0035:  ldc.i4.2
  IL_0036:  stelem.i4
  IL_0037:  dup
  IL_0038:  ldc.i4.2
  IL_0039:  ldc.i4.3
  IL_003a:  stelem.i4
  IL_003b:  stloc.3
  IL_003c:  ldloc.3
  IL_003d:  ldc.i4.0
  IL_003e:  ldelema    ""int""
  IL_0043:  stloc.s    V_4
  IL_0045:  ldloc.3
  IL_0046:  ldc.i4.0
  IL_0047:  ldelema    ""int""
  IL_004c:  stloc.s    V_5
  IL_004e:  ldnull
  IL_004f:  stloc.s    V_6
  IL_0051:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0056:  stloc.s    V_7
  IL_0058:  ret
}
");

            var diff2 = compilation2.EmitDifference(
               diff1.NextGeneration,
               ImmutableArray.Create(
                   SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.G", @"
{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>> V_0, //a
                System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>> V_1, //b
                System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>[] V_2, //c
                int[] V_3, //array
                int& V_4, //d
                int& V_5, //e
                C1<System.ValueTuple<int, dynamic>>.E***[,,] V_6, //x
                System.Collections.Generic.List<string> V_7) //f
  IL_0000:  nop
  IL_0001:  ldstr      ""a""
  IL_0006:  newobj     ""System.Collections.Generic.List<System.ValueTuple<int, int>>..ctor()""
  IL_000b:  newobj     ""<>f__AnonymousType0<string, System.Collections.Generic.List<System.ValueTuple<int, int>>>..ctor(string, System.Collections.Generic.List<System.ValueTuple<int, int>>)""
  IL_0010:  stloc.0
  IL_0011:  ldloca.s   V_1
  IL_0013:  ldc.i4.5
  IL_0014:  ldloc.0
  IL_0015:  call       ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>..ctor(int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>)""
  IL_001a:  ldc.i4.1
  IL_001b:  newarr     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  ldloc.1
  IL_0023:  stelem     ""System.ValueTuple<int, <anonymous type: string key, System.Collections.Generic.List<System.ValueTuple<int, int>> value>>""
  IL_0028:  stloc.2
  IL_0029:  ldc.i4.3
  IL_002a:  newarr     ""int""
  IL_002f:  dup
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i4
  IL_0033:  dup
  IL_0034:  ldc.i4.1
  IL_0035:  ldc.i4.2
  IL_0036:  stelem.i4
  IL_0037:  dup
  IL_0038:  ldc.i4.2
  IL_0039:  ldc.i4.3
  IL_003a:  stelem.i4
  IL_003b:  stloc.3
  IL_003c:  ldloc.3
  IL_003d:  ldc.i4.0
  IL_003e:  ldelema    ""int""
  IL_0043:  stloc.s    V_4
  IL_0045:  ldloc.3
  IL_0046:  ldc.i4.0
  IL_0047:  ldelema    ""int""
  IL_004c:  stloc.s    V_5
  IL_004e:  ldnull
  IL_004f:  stloc.s    V_6
  IL_0051:  newobj     ""System.Collections.Generic.List<string>..ctor()""
  IL_0056:  stloc.s    V_7
  IL_0058:  ret
}
");
        }
    }
}
