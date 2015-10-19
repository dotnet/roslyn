// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class LocalSlotMappingTests : EditAndContinueTestBase
    {
        /// <summary>
        /// If no changes were made we don't product a syntax map.
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, syntaxMap: null, preserveLocalVariables: true)));

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
            var source = @"
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: ComSafeDebugDll);
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
            v0.VerifyPdb("C.M", @"<symbols>
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""14"" endLine=""8"" endColumn=""23"" />
        <entry offset=""0x3"" hidden=""true"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""37"" endLine=""8"" endColumn=""58"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""32"" endLine=""8"" endColumn=""35"" />
        <entry offset=""0x10"" startLine=""8"" startColumn=""25"" endLine=""8"" endColumn=""30"" />
        <entry offset=""0x15"" hidden=""true"" />
        <entry offset=""0x18"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" />
        <entry offset=""0x1a"" hidden=""true"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""37"" endLine=""9"" endColumn=""58"" />
        <entry offset=""0x23"" startLine=""9"" startColumn=""32"" endLine=""9"" endColumn=""35"" />
        <entry offset=""0x27"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""30"" />
        <entry offset=""0x2d"" hidden=""true"" />
        <entry offset=""0x31"" startLine=""12"" startColumn=""14"" endLine=""12"" endColumn=""19"" />
        <entry offset=""0x33"" hidden=""true"" />
        <entry offset=""0x35"" startLine=""12"" startColumn=""33"" endLine=""12"" endColumn=""54"" />
        <entry offset=""0x3c"" startLine=""12"" startColumn=""28"" endLine=""12"" endColumn=""31"" />
        <entry offset=""0x40"" startLine=""12"" startColumn=""21"" endLine=""12"" endColumn=""26"" />
        <entry offset=""0x46"" hidden=""true"" />
        <entry offset=""0x4a"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
            var source =
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
}";
            var debug = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var release = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);

            CompileAndVerify(debug).VerifyPdb("C.M", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" />
        <entry offset=""0x12"" startLine=""9"" startColumn=""20"" endLine=""9"" endColumn=""21"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""23"" />
        <entry offset=""0x16"" hidden=""true"" />
        <entry offset=""0x21"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" />
        <entry offset=""0x27"" startLine=""10"" startColumn=""21"" endLine=""10"" endColumn=""22"" />
        <entry offset=""0x28"" startLine=""10"" startColumn=""23"" endLine=""10"" endColumn=""24"" />
        <entry offset=""0x2b"" hidden=""true"" />
        <entry offset=""0x36"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
            CompileAndVerify(release).VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""23"" />
        <entry offset=""0x12"" hidden=""true"" />
        <entry offset=""0x1c"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" />
        <entry offset=""0x22"" startLine=""10"" startColumn=""23"" endLine=""10"" endColumn=""24"" />
        <entry offset=""0x24"" hidden=""true"" />
        <entry offset=""0x2e"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Using()
        {
            var source =
@"class C : System.IDisposable
{
    public void Dispose()
    {
    }
    static System.IDisposable F()
    {
        return new C();
    }
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => methodData0.GetEncDebugInfo());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
      IL_0032:  endfinally
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
    IL_0040:  endfinally
  }
 -IL_0041:  ret
}
", methodToken: diff1.UpdatedMethods.Single());
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
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, references: new[] { MscorlibRef_v20 });
            var compilation1 = CreateCompilation(source, options: TestOptions.DebugDll, references: new[] { MscorlibRef_v20 });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeDebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (char* V_0, //p
                pinned string V_1,
                pinned int& V_2, //q
                [unchanged] V_3,
                char* V_4, //r
                pinned string V_5,
                int[] V_6)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  conv.i
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
  IL_0021:  br.s       IL_002c
  IL_0023:  ldloc.s    V_6
  IL_0025:  ldc.i4.0
  IL_0026:  ldelema    ""int""
  IL_002b:  stloc.2
  IL_002c:  nop
  IL_002d:  nop
  IL_002e:  ldc.i4.0
  IL_002f:  conv.u
  IL_0030:  stloc.2
  IL_0031:  ldarg.0
  IL_0032:  stloc.s    V_5
  IL_0034:  ldloc.s    V_5
  IL_0036:  conv.i
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.s    V_4
  IL_003b:  brfalse.s  IL_0047
  IL_003d:  ldloc.s    V_4
  IL_003f:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0044:  add
  IL_0045:  stloc.s    V_4
  IL_0047:  nop
  IL_0048:  nop
  IL_0049:  ldnull
  IL_004a:  stloc.s    V_5
  IL_004c:  nop
  IL_004d:  ldnull
  IL_004e:  stloc.1
  IL_004f:  ret
}");
        }

        [WorkItem(770053, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeDebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
  IL_0004:  conv.i
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
  IL_0014:  conv.i
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
  IL_0030:  conv.i
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
  IL_0046:  conv.i
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
  IL_005c:  conv.i
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
  IL_007f:  conv.i
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
", methodToken: diff1.UpdatedMethods.Single());
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);

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

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);
            var compilation2 = compilation0.WithSource(source2);

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

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
    IL_0018:  endfinally
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
    IL_0038:  endfinally
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
}", methodToken: diff1.UpdatedMethods.Single());
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            // Validate presence of a hidden sequence point @IL_0007 that is required for proper function remapping.
            v0.VerifyIL("C.M", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (string V_0)
 -IL_0000:  nop       
 -IL_0001:  call       ""string C.F()""
  IL_0006:  stloc.0   
 ~IL_0007:  ldloc.0   
  IL_0008:  ldstr      ""a""
  IL_000d:  call       ""bool string.op_Equality(string, string)""
  IL_0012:  brtrue.s   IL_0023
  IL_0014:  ldloc.0   
  IL_0015:  ldstr      ""b""
  IL_001a:  call       ""bool string.op_Equality(string, string)""
  IL_001f:  brtrue.s   IL_002c
  IL_0021:  br.s       IL_0035
 -IL_0023:  ldc.i4.1  
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  nop       
 -IL_002a:  br.s       IL_0035
 -IL_002c:  ldc.i4.2  
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  nop       
 -IL_0033:  br.s       IL_0035
 -IL_0035:  ret       
}
", sequencePoints: "C.M");

            var methodData0 = v0.TestData.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (string V_0)
 -IL_0000:  nop       
 -IL_0001:  call       ""string C.F()""
  IL_0006:  stloc.0   
 ~IL_0007:  ldloc.0   
  IL_0008:  ldstr      ""a""
  IL_000d:  call       ""bool string.op_Equality(string, string)""
  IL_0012:  brtrue.s   IL_0023
  IL_0014:  ldloc.0   
  IL_0015:  ldstr      ""b""
  IL_001a:  call       ""bool string.op_Equality(string, string)""
  IL_001f:  brtrue.s   IL_002d
  IL_0021:  br.s       IL_0037
 -IL_0023:  ldc.i4.s   10
  IL_0025:  call       ""void System.Console.WriteLine(int)""
  IL_002a:  nop       
 -IL_002b:  br.s       IL_0037
 -IL_002d:  ldc.i4.s   20
  IL_002f:  call       ""void System.Console.WriteLine(int)""
  IL_0034:  nop       
 -IL_0035:  br.s       IL_0037
 -IL_0037:  ret       
}", methodToken: diff1.UpdatedMethods.Single());
        }

        [Fact]
        public void Switch_Integer()
        {
            var source0 =
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
}";
            var source1 =
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""int C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  beq.s      IL_0013
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.2
  IL_000f:  beq.s      IL_001c
  IL_0011:  br.s       IL_0025
  IL_0013:  ldc.i4.1
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  nop
  IL_001a:  br.s       IL_0025
  IL_001c:  ldc.i4.2
  IL_001d:  call       ""void System.Console.WriteLine(int)""
  IL_0022:  nop
  IL_0023:  br.s       IL_0025
  IL_0025:  ret
}");
            // Validate that we emit a hidden sequence point @IL_0007.
            v0.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""21"" />
        <entry offset=""0x7"" hidden=""true"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""21"" endLine=""9"" endColumn=""49"" />
        <entry offset=""0x1a"" startLine=""9"" startColumn=""50"" endLine=""9"" endColumn=""56"" />
        <entry offset=""0x1c"" startLine=""10"" startColumn=""21"" endLine=""10"" endColumn=""49"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""50"" endLine=""10"" endColumn=""56"" />
        <entry offset=""0x25"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  call       ""int C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  beq.s      IL_0013
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.2
  IL_000f:  beq.s      IL_001d
  IL_0011:  br.s       IL_0027
  IL_0013:  ldc.i4.s   10
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  nop
  IL_001b:  br.s       IL_0027
  IL_001d:  ldc.i4.s   20
  IL_001f:  call       ""void System.Console.WriteLine(int)""
  IL_0024:  nop
  IL_0025:  br.s       IL_0027
  IL_0027:  ret
}");
        }

        [Fact]
        public void If()
        {
            var source0 = @"
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
}";
            var source1 = @"
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""17"" />
        <entry offset=""0x7"" hidden=""true"" />
        <entry offset=""0xa"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0xb"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" />
        <entry offset=""0x12"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x13"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.IfStatement), preserveLocalVariables: true)));

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
            var source0 = @"
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
}";
            var source1 = @"
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" hidden=""true"" />
        <entry offset=""0x3"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x4"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" />
        <entry offset=""0xb"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""20"" />
        <entry offset=""0x12"" hidden=""true"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.WhileStatement), preserveLocalVariables: true)));

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
            var source0 = @"
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
}";
            var source1 = @"
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
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" />
        <entry offset=""0x9"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""21"" />
        <entry offset=""0x10"" hidden=""true"" />
        <entry offset=""0x13"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.DoStatement), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.ForStatement, SyntaxKind.VariableDeclarator), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.<M>b__0", @"
", methodToken: diff1.UpdatedMethods.Single());
#endif
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
  IL_0027:  callvirt   ""System.Collections.Generic.IEnumerable<int> C.F()""
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("?", @"
{", methodToken: diff1.UpdatedMethods.Single());
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
  // Code size      254 (0xfe)
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
    IL_0019:  callvirt   ""System.Threading.Tasks.Task<int> C.F()""
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
    IL_0066:  callvirt   ""System.Threading.Tasks.Task<int> C.F()""
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
    IL_009c:  leave.s    IL_00fd
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
    IL_00c2:  ldloca.s   V_2
    IL_00c4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
   -IL_00ca:  ldc.i4.1
    IL_00cb:  stloc.1
    IL_00cc:  leave.s    IL_00e8
  }
  catch System.Exception
  {
   ~IL_00ce:  stloc.s    V_4
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.s   -2
    IL_00d3:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00d8:  ldarg.0
    IL_00d9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_00de:  ldloc.s    V_4
    IL_00e0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00e5:  nop
    IL_00e6:  leave.s    IL_00fd
  }
 -IL_00e8:  ldarg.0
  IL_00e9:  ldc.i4.s   -2
  IL_00eb:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_00f0:  ldarg.0
  IL_00f1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00f6:  ldloc.1
  IL_00f7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00fc:  nop
  IL_00fd:  ret
}
", sequencePoints: "C+<F>d__0.MoveNext");

#if TODO
            var methodData0 = v0.TestData.GetMethodData("?");
            var method0 = compilation0.GetMember<MethodSymbol>("?");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("?");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("?", @"
{", methodToken: diff1.UpdatedMethods.Single());
#endif
        }
    }
}
