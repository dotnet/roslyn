// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public partial class SynthesizedLocalsTests : EditAndContinueTestBase
    {
        /// <summary>
        /// Synthesized locals should only be named in debug builds.
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

            CompilationTestData testData;
            ImmutableArray<string> names;

            testData = new CompilationTestData();
            debug.EmitToArray(testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            AssertEx.Equal(new string[] { "CS$2$0000", "CS$520$0001", "CS$3$0002" }, names);

            testData = new CompilationTestData();
            release.EmitToArray(testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            AssertEx.Equal(new string[] { null, null }, names);
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (System.IDisposable V_0, //CS$3$0000
           System.IDisposable V_1, //u
           System.IDisposable V_2) //CS$3$0001
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (object V_0, //CS$2$0000
           bool V_1, //CS$520$0001
           object V_2, //CS$2$0002
           bool V_3) //CS$520$0003
 -IL_0000:  nop       
 ~IL_0001:  ldc.i4.0  
  IL_0002:  stloc.1   
  .try
  {
   -IL_0003:  call       ""object C.F()""
    IL_0008:  stloc.0   
    IL_0009:  ldloc.0   
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  nop       
   -IL_0012:  nop       
   ~IL_0013:  ldc.i4.0  
    IL_0014:  stloc.3   
    .try
    {
     -IL_0015:  call       ""object C.F()""
      IL_001a:  stloc.2   
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
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (object V_0) //CS$2$0000
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeDebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size       83 (0x53)
  .maxstack  2
  .locals init (char* V_0, //p
                [unchanged] V_1,
                pinned int& V_2, //q
                [unchanged] V_3,
                char* V_4, //r
                [unchanged] V_5,
                pinned string V_6, //CS$519$0000
                int[] V_7,
                pinned string V_8) //CS$519$0001
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_6
  IL_0004:  ldloc.s    V_6
  IL_0006:  conv.i
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0013
  IL_000b:  ldloc.0
  IL_000c:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  nop
  IL_0014:  ldarg.1
  IL_0015:  dup
  IL_0016:  stloc.s    V_7
  IL_0018:  brfalse.s  IL_0020
  IL_001a:  ldloc.s    V_7
  IL_001c:  ldlen
  IL_001d:  conv.i4
  IL_001e:  brtrue.s   IL_0025
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.2
  IL_0023:  br.s       IL_002e
  IL_0025:  ldloc.s    V_7
  IL_0027:  ldc.i4.0
  IL_0028:  ldelema    ""int""
  IL_002d:  stloc.2
  IL_002e:  nop
  IL_002f:  nop
  IL_0030:  ldc.i4.0
  IL_0031:  conv.u
  IL_0032:  stloc.2
  IL_0033:  ldarg.0
  IL_0034:  stloc.s    V_8
  IL_0036:  ldloc.s    V_8
  IL_0038:  conv.i
  IL_0039:  stloc.s    V_4
  IL_003b:  ldloc.s    V_4
  IL_003d:  brfalse.s  IL_0049
  IL_003f:  ldloc.s    V_4
  IL_0041:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0046:  add
  IL_0047:  stloc.s    V_4
  IL_0049:  nop
  IL_004a:  nop
  IL_004b:  ldnull
  IL_004c:  stloc.s    V_8
  IL_004e:  nop
  IL_004f:  ldnull
  IL_0050:  stloc.s    V_6
  IL_0052:  ret
}");
        }

        /// <summary>
        /// Dev11 generates C$519$0000, CS$520$0001, CS$521$0002, ... for
        /// multiple declarations within a single fixed statement.
        /// Roslyn generates C$519$0000, CS$519$0001, CS$519$0002, ...
        /// rather than using a unique TempKind for each.
        /// </summary>
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.UnsafeDebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      172 (0xac)
  .maxstack  2
  .locals init (char* V_0, //p1
                char* V_1, //p2
                [unchanged] V_2,
                [unchanged] V_3,
                char* V_4, //p1
                char* V_5, //p3
                char* V_6, //p2
                [unchanged] V_7,
                [unchanged] V_8,
                [unchanged] V_9,
                char* V_10, //p4
                [unchanged] V_11,
                pinned string V_12, //CS$519$0000
                pinned string V_13, //CS$519$0001
                pinned string V_14, //CS$519$0002
                pinned string V_15, //CS$519$0003
                pinned string V_16, //CS$519$0004
                pinned string V_17) //CS$519$0005
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_12
  IL_0004:  ldloc.s    V_12
  IL_0006:  conv.i
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0013
  IL_000b:  ldloc.0
  IL_000c:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  ldarg.1
  IL_0014:  stloc.s    V_13
  IL_0016:  ldloc.s    V_13
  IL_0018:  conv.i
  IL_0019:  stloc.1
  IL_001a:  ldloc.1
  IL_001b:  brfalse.s  IL_0025
  IL_001d:  ldloc.1
  IL_001e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0023:  add
  IL_0024:  stloc.1
  IL_0025:  nop
  IL_0026:  ldloc.0
  IL_0027:  ldloc.1
  IL_0028:  ldind.u2
  IL_0029:  stind.i2
  IL_002a:  nop
  IL_002b:  ldnull
  IL_002c:  stloc.s    V_12
  IL_002e:  ldnull
  IL_002f:  stloc.s    V_13
  IL_0031:  ldarg.0
  IL_0032:  stloc.s    V_14
  IL_0034:  ldloc.s    V_14
  IL_0036:  conv.i
  IL_0037:  stloc.s    V_4
  IL_0039:  ldloc.s    V_4
  IL_003b:  brfalse.s  IL_0047
  IL_003d:  ldloc.s    V_4
  IL_003f:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0044:  add
  IL_0045:  stloc.s    V_4
  IL_0047:  ldarg.2
  IL_0048:  stloc.s    V_15
  IL_004a:  ldloc.s    V_15
  IL_004c:  conv.i
  IL_004d:  stloc.s    V_5
  IL_004f:  ldloc.s    V_5
  IL_0051:  brfalse.s  IL_005d
  IL_0053:  ldloc.s    V_5
  IL_0055:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_005a:  add
  IL_005b:  stloc.s    V_5
  IL_005d:  ldarg.3
  IL_005e:  stloc.s    V_16
  IL_0060:  ldloc.s    V_16
  IL_0062:  conv.i
  IL_0063:  stloc.s    V_6
  IL_0065:  ldloc.s    V_6
  IL_0067:  brfalse.s  IL_0073
  IL_0069:  ldloc.s    V_6
  IL_006b:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0070:  add
  IL_0071:  stloc.s    V_6
  IL_0073:  nop
  IL_0074:  ldloc.s    V_4
  IL_0076:  ldloc.s    V_6
  IL_0078:  ldind.u2
  IL_0079:  stind.i2
  IL_007a:  ldloc.s    V_6
  IL_007c:  ldloc.s    V_5
  IL_007e:  ldind.u2
  IL_007f:  stind.i2
  IL_0080:  ldarg.1
  IL_0081:  stloc.s    V_17
  IL_0083:  ldloc.s    V_17
  IL_0085:  conv.i
  IL_0086:  stloc.s    V_10
  IL_0088:  ldloc.s    V_10
  IL_008a:  brfalse.s  IL_0096
  IL_008c:  ldloc.s    V_10
  IL_008e:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0093:  add
  IL_0094:  stloc.s    V_10
  IL_0096:  nop
  IL_0097:  ldloc.s    V_5
  IL_0099:  ldloc.s    V_10
  IL_009b:  ldind.u2
  IL_009c:  stind.i2
  IL_009d:  nop
  IL_009e:  ldnull
  IL_009f:  stloc.s    V_17
  IL_00a1:  nop
  IL_00a2:  ldnull
  IL_00a3:  stloc.s    V_14
  IL_00a5:  ldnull
  IL_00a6:  stloc.s    V_15
  IL_00a8:  ldnull
  IL_00a9:  stloc.s    V_16
  IL_00ab:  ret
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
        foreach (var @x/*CS$5$0000*/ in F1())
        {
            foreach (object y/*CS$5$0001*/ in F2()) { }
        }
        foreach (var x/*CS$5$0001*/ in F4())
        {
            foreach (var y/*CS$5$0000*/ in F3()) { }
            foreach (var z/*CS$5$0004*/ in F2()) { }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      274 (0x112)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0, //CS$5$0000
                [object] V_1,
                System.Collections.Generic.List<object>.Enumerator V_2, //CS$5$0001
                [object] V_3,
                [unchanged] V_4,
                System.Collections.Generic.List<object>.Enumerator V_5, //CS$5$0002
                [object] V_6,
                System.Collections.IEnumerator V_7, //CS$5$0003
                [object] V_8,
                System.Collections.Generic.List<object>.Enumerator V_9, //CS$5$0004
                [object] V_10,
                object V_11, //x
                object V_12, //y
                System.IDisposable V_13,
                object V_14, //x
                object V_15, //y
                object V_16) //z
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.Collections.IEnumerable C.F1()""
  IL_0007:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_000c:  stloc.0
  .try
  {
    IL_000d:  br.s       IL_004c
    IL_000f:  ldloc.0
    IL_0010:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0015:  stloc.s    V_11
    IL_0017:  nop
    IL_0018:  nop
    IL_0019:  call       ""System.Collections.Generic.List<object> C.F2()""
    IL_001e:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
    IL_0023:  stloc.2
    .try
    {
      IL_0024:  br.s       IL_0031
      IL_0026:  ldloca.s   V_2
      IL_0028:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
      IL_002d:  stloc.s    V_12
      IL_002f:  nop
      IL_0030:  nop
      IL_0031:  ldloca.s   V_2
      IL_0033:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
      IL_0038:  brtrue.s   IL_0026
      IL_003a:  leave.s    IL_004b
    }
    finally
    {
      IL_003c:  ldloca.s   V_2
      IL_003e:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
      IL_0044:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0049:  nop
      IL_004a:  endfinally
    }
    IL_004b:  nop
    IL_004c:  ldloc.0
    IL_004d:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0052:  brtrue.s   IL_000f
    IL_0054:  leave.s    IL_006b
  }
  finally
  {
    IL_0056:  ldloc.0
    IL_0057:  isinst     ""System.IDisposable""
    IL_005c:  stloc.s    V_13
    IL_005e:  ldloc.s    V_13
    IL_0060:  brfalse.s  IL_006a
    IL_0062:  ldloc.s    V_13
    IL_0064:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0069:  nop
    IL_006a:  endfinally
  }
  IL_006b:  nop
  IL_006c:  call       ""System.Collections.Generic.List<object> C.F4()""
  IL_0071:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
  IL_0076:  stloc.s    V_5
  .try
  {
    IL_0078:  br.s       IL_00f4
    IL_007a:  ldloca.s   V_5
    IL_007c:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
    IL_0081:  stloc.s    V_14
    IL_0083:  nop
    IL_0084:  nop
    IL_0085:  call       ""System.Collections.IEnumerable C.F3()""
    IL_008a:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
    IL_008f:  stloc.s    V_7
    .try
    {
      IL_0091:  br.s       IL_009e
      IL_0093:  ldloc.s    V_7
      IL_0095:  callvirt   ""object System.Collections.IEnumerator.Current.get""
      IL_009a:  stloc.s    V_15
      IL_009c:  nop
      IL_009d:  nop
      IL_009e:  ldloc.s    V_7
      IL_00a0:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
      IL_00a5:  brtrue.s   IL_0093
      IL_00a7:  leave.s    IL_00bf
    }
    finally
    {
      IL_00a9:  ldloc.s    V_7
      IL_00ab:  isinst     ""System.IDisposable""
      IL_00b0:  stloc.s    V_13
      IL_00b2:  ldloc.s    V_13
      IL_00b4:  brfalse.s  IL_00be
      IL_00b6:  ldloc.s    V_13
      IL_00b8:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00bd:  nop
      IL_00be:  endfinally
    }
    IL_00bf:  nop
    IL_00c0:  call       ""System.Collections.Generic.List<object> C.F2()""
    IL_00c5:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
    IL_00ca:  stloc.s    V_9
    .try
    {
      IL_00cc:  br.s       IL_00d9
      IL_00ce:  ldloca.s   V_9
      IL_00d0:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
      IL_00d5:  stloc.s    V_16
      IL_00d7:  nop
      IL_00d8:  nop
      IL_00d9:  ldloca.s   V_9
      IL_00db:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
      IL_00e0:  brtrue.s   IL_00ce
      IL_00e2:  leave.s    IL_00f3
    }
    finally
    {
      IL_00e4:  ldloca.s   V_9
      IL_00e6:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
      IL_00ec:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00f1:  nop
      IL_00f2:  endfinally
    }
    IL_00f3:  nop
    IL_00f4:  ldloca.s   V_5
    IL_00f6:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
    IL_00fb:  brtrue     IL_007a
    IL_0100:  leave.s    IL_0111
  }
  finally
  {
    IL_0102:  ldloca.s   V_5
    IL_0104:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
    IL_010a:  callvirt   ""void System.IDisposable.Dispose()""
    IL_010f:  nop
    IL_0110:  endfinally
  }
  IL_0111:  ret
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (double[,,] V_0, //CS$6$0000
           int V_1, //CS$263$0001
           int V_2, //CS$264$0002
           int V_3, //CS$265$0003
           int V_4, //CS$7$0004
           int V_5, //CS$8$0005
           int V_6, //CS$9$0006
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

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (double[,,] V_0, //CS$6$0000
           int V_1, //CS$263$0001
           int V_2, //CS$264$0002
           int V_3, //CS$265$0003
           int V_4, //CS$7$0004
           int V_5, //CS$8$0005
           int V_6, //CS$9$0006
           [unchanged] V_7,
           double V_8) //x
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
  IL_0049:  stloc.s    V_8
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
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      185 (0xb9)
  .maxstack  4
  .locals init (string V_0, //CS$6$0000
                int V_1, //CS$7$0001
                [unchanged] V_2,
                object[] V_3, //CS$6$0002
                int V_4, //CS$7$0003
                [object] V_5,
                double[,,] V_6, //CS$6$0004
                int V_7, //CS$263$0005
                int V_8, //CS$264$0006
                int V_9, //CS$265$0007
                int V_10, //CS$7$0008
                int V_11, //CS$8$0009
                int V_12, //CS$9$0010
                [unchanged] V_13,
                char V_14, //x
                object V_15, //y
                double V_16) //x
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0034
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  callvirt   ""char string.this[int].get""
  IL_000f:  stloc.s    V_14
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  ldarg.1
  IL_0014:  stloc.3
  IL_0015:  ldc.i4.0
  IL_0016:  stloc.s    V_4
  IL_0018:  br.s       IL_0028
  IL_001a:  ldloc.3
  IL_001b:  ldloc.s    V_4
  IL_001d:  ldelem.ref
  IL_001e:  stloc.s    V_15
  IL_0020:  nop
  IL_0021:  nop
  IL_0022:  ldloc.s    V_4
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.s    V_4
  IL_0028:  ldloc.s    V_4
  IL_002a:  ldloc.3
  IL_002b:  ldlen
  IL_002c:  conv.i4
  IL_002d:  blt.s      IL_001a
  IL_002f:  nop
  IL_0030:  ldloc.1
  IL_0031:  ldc.i4.1
  IL_0032:  add
  IL_0033:  stloc.1
  IL_0034:  ldloc.1
  IL_0035:  ldloc.0
  IL_0036:  callvirt   ""int string.Length.get""
  IL_003b:  blt.s      IL_0008
  IL_003d:  nop
  IL_003e:  ldarg.2
  IL_003f:  stloc.s    V_6
  IL_0041:  ldloc.s    V_6
  IL_0043:  ldc.i4.0
  IL_0044:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0049:  stloc.s    V_7
  IL_004b:  ldloc.s    V_6
  IL_004d:  ldc.i4.1
  IL_004e:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0053:  stloc.s    V_8
  IL_0055:  ldloc.s    V_6
  IL_0057:  ldc.i4.2
  IL_0058:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_005d:  stloc.s    V_9
  IL_005f:  ldloc.s    V_6
  IL_0061:  ldc.i4.0
  IL_0062:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0067:  stloc.s    V_10
  IL_0069:  br.s       IL_00b2
  IL_006b:  ldloc.s    V_6
  IL_006d:  ldc.i4.1
  IL_006e:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0073:  stloc.s    V_11
  IL_0075:  br.s       IL_00a6
  IL_0077:  ldloc.s    V_6
  IL_0079:  ldc.i4.2
  IL_007a:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_007f:  stloc.s    V_12
  IL_0081:  br.s       IL_009a
  IL_0083:  ldloc.s    V_6
  IL_0085:  ldloc.s    V_10
  IL_0087:  ldloc.s    V_11
  IL_0089:  ldloc.s    V_12
  IL_008b:  call       ""double[*,*,*].Get""
  IL_0090:  stloc.s    V_16
  IL_0092:  nop
  IL_0093:  nop
  IL_0094:  ldloc.s    V_12
  IL_0096:  ldc.i4.1
  IL_0097:  add
  IL_0098:  stloc.s    V_12
  IL_009a:  ldloc.s    V_12
  IL_009c:  ldloc.s    V_9
  IL_009e:  ble.s      IL_0083
  IL_00a0:  ldloc.s    V_11
  IL_00a2:  ldc.i4.1
  IL_00a3:  add
  IL_00a4:  stloc.s    V_11
  IL_00a6:  ldloc.s    V_11
  IL_00a8:  ldloc.s    V_8
  IL_00aa:  ble.s      IL_0077
  IL_00ac:  ldloc.s    V_10
  IL_00ae:  ldc.i4.1
  IL_00af:  add
  IL_00b0:  stloc.s    V_10
  IL_00b2:  ldloc.s    V_10
  IL_00b4:  ldloc.s    V_7
  IL_00b6:  ble.s      IL_006b
  IL_00b8:  ret
}");
        }

        /// <summary>
        /// TempKind expects array with at most 256 dimensions.
        /// (Should any edits in such cases be considered rude edits?
        /// Or should we generate compile errors since the CLR throws
        /// TypeLoadException if the number of dimensions exceeds 256?)
        /// </summary>
        //[Fact(Skip = "ArgumentException")]
        public void ForEachArray_Overflow()
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
            Assert.True(source.IndexOf(tooManyCommas) > 0);

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (object V_0, //CS$2$0001
           bool V_1, //CS$520$0002
           string V_2, //CS$6$0003
           int V_3, //CS$7$0004
           [unchanged] V_4,
           [unchanged] V_5,
           char V_6, //c
           System.IDisposable V_7) //CS$3$0000
 -IL_0000:  nop       
 -IL_0001:  call       ""System.IDisposable C.F3()""
  IL_0006:  stloc.s    V_7
  .try
  {
   -IL_0008:  nop       
   -IL_0009:  nop       
    IL_000a:  leave.s    IL_0019
  }
  finally
  {
   ~IL_000c:  ldloc.s    V_7
    IL_000e:  brfalse.s  IL_0018
    IL_0010:  ldloc.s    V_7
    IL_0012:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0017:  nop       
    IL_0018:  endfinally
  }
 ~IL_0019:  ldc.i4.0  
  IL_001a:  stloc.1   
  .try
  {
   -IL_001b:  call       ""object C.F1()""
    IL_0020:  stloc.0   
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
  IL_004b:  stloc.s    V_6
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

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
  .locals init (object V_0, //CS$2$0002
                bool V_1, //CS$520$0003
                [object] V_2,
                [bool] V_3,
                object V_4, //CS$2$0000
                bool V_5, //CS$520$0001
                object V_6, //CS$2$0004
                bool V_7) //CS$520$0005
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.s    V_5
  .try
  {
    IL_0004:  call       ""object C.F3()""
    IL_0009:  stloc.s    V_4
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
  IL_0026:  ldc.i4.0
  IL_0027:  stloc.1
  .try
  {
    IL_0028:  call       ""object C.F1()""
    IL_002d:  stloc.0
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
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.s    V_7
  .try
  {
    IL_0049:  call       ""object C.F4()""
    IL_004e:  stloc.s    V_6
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
        a[/*V_9*/F()]++;
        using (/*CS$3$0000*/F()) { }
        lock (/*CS$2$0001*/F()) { }
        foreach (var o in /*CS$5$0003*/G()) { }
    }
}";

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      145 (0x91)
  .maxstack  4
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                [int] V_2,
                A V_3, //CS$3$0000
                A V_4, //CS$2$0001
                bool V_5, //CS$520$0002
                A V_6, //CS$5$0003
                [object] V_7,
                A V_8,
                A V_9,
                int V_10,
                object V_11) //o
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_8
  IL_0004:  call       ""A C.F()""
  IL_0009:  stloc.s    V_9
  IL_000b:  ldloc.s    V_8
  IL_000d:  ldloc.s    V_9
  IL_000f:  callvirt   ""int A.this[A].get""
  IL_0014:  stloc.s    V_10
  IL_0016:  ldloc.s    V_8
  IL_0018:  ldloc.s    V_9
  IL_001a:  ldloc.s    V_10
  IL_001c:  ldc.i4.1
  IL_001d:  add
  IL_001e:  callvirt   ""void A.this[A].set""
  IL_0023:  nop
  IL_0024:  call       ""A C.F()""
  IL_0029:  stloc.3
  .try
  {
    IL_002a:  nop
    IL_002b:  nop
    IL_002c:  leave.s    IL_0039
  }
  finally
  {
    IL_002e:  ldloc.3
    IL_002f:  brfalse.s  IL_0038
    IL_0031:  ldloc.3
    IL_0032:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0037:  nop
    IL_0038:  endfinally
  }
  IL_0039:  ldc.i4.0
  IL_003a:  stloc.s    V_5
  .try
  {
    IL_003c:  call       ""A C.F()""
    IL_0041:  stloc.s    V_4
    IL_0043:  ldloc.s    V_4
    IL_0045:  ldloca.s   V_5
    IL_0047:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_004c:  nop
    IL_004d:  nop
    IL_004e:  nop
    IL_004f:  leave.s    IL_005e
  }
  finally
  {
    IL_0051:  ldloc.s    V_5
    IL_0053:  brfalse.s  IL_005d
    IL_0055:  ldloc.s    V_4
    IL_0057:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_005c:  nop
    IL_005d:  endfinally
  }
  IL_005e:  nop
  IL_005f:  call       ""B C.G()""
  IL_0064:  callvirt   ""A B.GetEnumerator()""
  IL_0069:  stloc.s    V_6
  .try
  {
    IL_006b:  br.s       IL_0078
    IL_006d:  ldloc.s    V_6
    IL_006f:  callvirt   ""object A.Current.get""
    IL_0074:  stloc.s    V_11
    IL_0076:  nop
    IL_0077:  nop
    IL_0078:  ldloc.s    V_6
    IL_007a:  callvirt   ""bool A.MoveNext()""
    IL_007f:  brtrue.s   IL_006d
    IL_0081:  leave.s    IL_0090
  }
  finally
  {
    IL_0083:  ldloc.s    V_6
    IL_0085:  brfalse.s  IL_008f
    IL_0087:  ldloc.s    V_6
    IL_0089:  callvirt   ""void System.IDisposable.Dispose()""
    IL_008e:  nop
    IL_008f:  endfinally
  }
  IL_0090:  ret
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            // Validate presence of a hidden sequence point @IL_0007 that is required for proper function remapping.
            v0.VerifyIL("C.M", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (string V_0) //CS$4$0000
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

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (string V_0) //CS$4$0000
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (int V_0) //CS$4$0000
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
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""9"" end_row=""7"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""9"" start_column=""21"" end_row=""9"" end_column=""49"" file_ref=""0"" />
        <entry il_offset=""0x1a"" start_row=""9"" start_column=""50"" end_row=""9"" end_column=""56"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""10"" start_column=""21"" end_row=""10"" end_column=""49"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""10"" start_column=""50"" end_row=""10"" end_column=""56"" file_ref=""0"" />
        <entry il_offset=""0x25"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$4$0000"" il_index=""0"" il_start=""0x1"" il_end=""0x13"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x26"">
        <scope startOffset=""0x1"" endOffset=""0x13"">
          <local name=""CS$4$0000"" il_index=""0"" il_start=""0x1"" il_end=""0x13"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.SwitchStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int V_0) //CS$4$0000
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""17"" file_ref=""0"" />
        <entry il_offset=""0x7"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x12"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$4$0000"" il_index=""0"" il_start=""0x1"" il_end=""0xa"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <scope startOffset=""0x1"" endOffset=""0xa"">
          <local name=""CS$4$0000"" il_index=""0"" il_start=""0x1"" il_end=""0xa"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.IfStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""8"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x4"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0xb"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x12"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x15"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$4$0000"" il_index=""0"" il_start=""0xc"" il_end=""0x15"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <scope startOffset=""0xc"" endOffset=""0x15"">
          <local name=""CS$4$0000"" il_index=""0"" il_start=""0xc"" il_end=""0x15"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.WhileStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
        public void Do()
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            v0.VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
    <method containingType=""C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""F"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0x2"" start_row=""10"" start_column=""13"" end_row=""10"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""10"" file_ref=""0"" />
        <entry il_offset=""0xa"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x10"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""0"" />
        <entry il_offset=""0x13"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""CS$4$0000"" il_index=""0"" il_start=""0xa"" il_end=""0x13"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <scope startOffset=""0xa"" endOffset=""0x13"">
          <local name=""CS$4$0000"" il_index=""0"" il_start=""0xa"" il_end=""0x13"" attributes=""1"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.DoStatement), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (bool V_0) //CS$4$0000
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
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            // Validate presence of a hidden sequence point @IL_001c that is required for proper function remapping.
            v0.VerifyIL("C.M", @"
{
  // Code size       32 (0x20)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1) //CS$4$0000
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

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.ForStatement, SyntaxKind.VariableDeclarator), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (int V_0, //i
  bool V_1) //CS$4$0000
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
    }
}