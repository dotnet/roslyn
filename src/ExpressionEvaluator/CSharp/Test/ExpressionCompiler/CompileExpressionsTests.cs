// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class CompileExpressionsTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void NoRequests()
        {
            var source =
@"class C
{
    static void F()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(ImmutableArray<string>.Empty,
                        out var methodTokens,
                        out var errorMessages);
                    Assert.Null(assembly);
                    Assert.True(methodTokens.IsEmpty);
                    Assert.True(errorMessages.IsEmpty);
                });
        }

        [Fact]
        public void SingleRequest()
        {
            var source =
@"class C
{
    static void F()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("1"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.NotNull(assembly);
                    Assert.True(errorMessages.IsEmpty);
                    Assert.Equal(1, methodTokens.Length);
                    assembly.VerifyIL(methodTokens[0], "<>x0.<>m0",
@"{
  // Code size        2 (0x2)
  .maxstack  8
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");
                });
        }

        [Fact]
        public void MultipleRequests()
        {
            var source =
@"class C
{
    static void F(object x)
    {
        object y;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("x", "x ?? y"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.NotNull(assembly);
                    Assert.True(errorMessages.IsEmpty);
                    Assert.Equal(2, methodTokens.Length);
                    assembly.VerifyIL(methodTokens[0], "<>x0.<>m0",
@"Locals: object
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
                    assembly.VerifyIL(methodTokens[1], "<>x1.<>m0",
@"Locals: object
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0006
  IL_0004:  pop
  IL_0005:  ldloc.0
  IL_0006:  ret
}");
                });
        }

        [Fact]
        public void ParseErrors()
        {
            var source =
@"class C
{
    static void F(object x)
    {
        object y;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("x", "x ??", "?? z", "x ?? z"),
                        out var methodTokens,
                        out var errorMessages);

                    Assert.Null(assembly);
                    AssertEx.Equal(new[]
                    {
                        $"(1,5): error CS1733: { CSharpResources.ERR_ExpressionExpected }",
                        $"(1,1): error CS1525: { string.Format(CSharpResources.ERR_InvalidExprTerm, "??") }",
                    }, errorMessages);

                    Assert.True(methodTokens.IsEmpty);
                });
        }

        [Fact]
        public void BindingErrors()
        {
            var source =
@"class C
{
    static void F(object x)
    {
        object y;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create(
                            "x",
                            "z", // (1,1): error CS0103: The name 'z' does not exist in the current context
                            "x ?? y",
                            "x ?? z", // (1,6): error CS0103: The name 'z' does not exist in the current context
                            "0l"), // (1,2): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                        out var methodTokens,
                        out var errorMessages);

                    Assert.Null(assembly);
                    AssertEx.Equal(new[]
                    {
                        $"(1,1): error CS0103: { string.Format(CSharpResources.ERR_NameNotInContext,"z") }",
                        $"(1,6): error CS0103: { string.Format(CSharpResources.ERR_NameNotInContext,"z") }",
                    }, errorMessages);
                    Assert.True(methodTokens.IsEmpty);
                });
        }

        [Fact]
        public void EmitErrors()
        {
            var longName = new string('P', 1100);
            var source =
@"class C
{
    static void F()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create($"new {{ {longName} = 1 }}"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.Null(assembly);
                    AssertEx.Equal(new[]
                    {
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"<{longName}>i__Field") }",
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"<{longName}>j__TPar") }",
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"<{longName}>i__Field") }",
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"get_{longName}") }",
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"{longName}") }",
                        $"error CS7013: { string.Format(CSharpResources.ERR_MetadataNameTooLong, $"{longName}") }",
                    }, errorMessages);

                    Assert.True(methodTokens.IsEmpty);
                });
        }

        [Fact]
        public void Assignment()
        {
            var source =
@"class C
{
    object F;
    void M()
    {
        object o;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.M");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("o = F"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.NotNull(assembly);
                    Assert.True(errorMessages.IsEmpty);
                    Assert.Equal(1, methodTokens.Length);
                    assembly.VerifyIL(methodTokens[0], "<>x0.<>m0",
@"Locals: object
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x0A000006
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  ret
}");
                });
        }

        [Fact]
        public void VoidExpression()
        {
            var source =
@"class C
{
    static void F()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("F()"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.NotNull(assembly);
                    Assert.True(errorMessages.IsEmpty);
                    Assert.Equal(1, methodTokens.Length);
                    assembly.VerifyIL(methodTokens[0], "<>x0.<>m0",
@"{
  // Code size        6 (0x6)
  .maxstack  8
  IL_0000:  call       0x0A000006
  IL_0005:  ret
}");
                });
        }

        [Fact]
        public void Declaration()
        {
            var source =
@"class C
{
    static void F()
    {
    }
    static void G(out object o)
    {
        o = null;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("G(out var o)"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.Null(assembly);
                    AssertEx.Equal(
                        new[] { $"(1,11): error CS8185: {CSharpResources.ERR_DeclarationExpressionNotPermitted}" },
                        errorMessages);
                    Assert.True(methodTokens.IsEmpty);
                });
        }

        [Fact]
        public void PseudoVariables()
        {
            var source =
@"class C
{
    static void F()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("$exception", "$1 ?? $unknown"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.Null(assembly);
                    AssertEx.Equal(new[]
                    {
                        $"(1,1): error CS0103: { string.Format(CSharpResources.ERR_NameNotInContext, "$exception") }",
                        $"(1,1): error CS0103: { string.Format(CSharpResources.ERR_NameNotInContext, "$1") }",
                        $"(1,7): error CS0103: { string.Format(CSharpResources.ERR_NameNotInContext, "$unknown") }",
                    }, errorMessages);
                    Assert.True(methodTokens.IsEmpty);
                });
        }

        [Fact]
        public void GenericAndDynamic()
        {
            var source =
@"class C<T>
{
    static void F<U>(dynamic d)
    {
        d.F();
    }
}";
            var comp = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("default(T)", "default(U)", "d.F()"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.NotNull(assembly);
                    Assert.True(errorMessages.IsEmpty);
                    Assert.Equal(3, methodTokens.Length);
                    assembly.VerifyIL(methodTokens[0], "<>x0.<>m0",
@"Locals: !0
{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    0x1B000001
  IL_0008:  ldloc.0
  IL_0009:  ret
}");
                    assembly.VerifyIL(methodTokens[1], "<>x1.<>m0",
@"Locals: !!0
{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    0x1B000002
  IL_0008:  ldloc.0
  IL_0009:  ret
}");
                    assembly.VerifyIL(methodTokens[2], "<>x2.<>m0",
@"{
  // Code size       77 (0x4d)
  .maxstack  9
  IL_0000:  ldsfld     0x0A000008
  IL_0005:  brtrue.s   IL_0037
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      0x70000001
  IL_000d:  ldnull
  IL_000e:  ldtoken    0x1B000004
  IL_0013:  call       0x0A000009
  IL_0018:  ldc.i4.1
  IL_0019:  newarr     0x01000011
  IL_001e:  dup
  IL_001f:  ldc.i4.0
  IL_0020:  ldc.i4.0
  IL_0021:  ldnull
  IL_0022:  call       0x0A00000A
  IL_0027:  stelem.ref
  IL_0028:  call       0x0A00000B
  IL_002d:  call       0x0A00000C
  IL_0032:  stsfld     0x0A000008
  IL_0037:  ldsfld     0x0A000008
  IL_003c:  ldfld      0x0A00000D
  IL_0041:  ldsfld     0x0A000008
  IL_0046:  ldarg.0
  IL_0047:  callvirt   0x0A00000E
  IL_004c:  ret
}");
                });
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=482753")]
        public void LocalsInAsync()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static Task<object> E(object o, Func<object, bool> p)
    {
        throw new NotImplementedException();
    }
    object F()
    {
        throw new NotImplementedException();
    }
    Task G(object o)
    {
        throw new NotImplementedException();
    }
    async Task M(object x)
    {
        var z = await E(F(), y => x == y);
#line 999
        await G(z);
    }
}";
            // Test with CompileExpression rather than CompileExpressions
            // so field references in IL are named.
            // Debug build.
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { TestMetadata.Net40.SystemCore });
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.<M>d__3.MoveNext()", atLineNumber: 999);
                    string error;
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression("z ?? x", out error, testData);
                    Assert.NotNull(result.Assembly);
                    Assert.Null(error);
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<object> V_1,
                C.<M>d__3 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.<M>d__3.<z>5__2""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""C.<>c__DisplayClass3_0 C.<M>d__3.<>8__1""
  IL_0010:  ldfld      ""object C.<>c__DisplayClass3_0.x""
  IL_0015:  ret
}");
                });
            // Release build.
            comp = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, references: new[] { SystemCoreRef });
            {
                // Note from MoveNext() below that local CS$<>8__locals0 should not be
                // used in the compiled expression to access the display class since that
                // local is only set the first time through MoveNext() (see loc.2 below).
                var testData = new CompilationTestData();
                comp.EmitToArray(testData: testData);
                testData.GetMethodData("C.<M>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()").VerifyIL(
@"{
  // Code size      293 (0x125)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                C.<>c__DisplayClass3_0 V_2, //CS$<>8__locals0
                object V_3, //z
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__3.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""C C.<M>d__3.<>4__this""
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  brfalse.s  IL_0075
    IL_0011:  ldloc.0
    IL_0012:  ldc.i4.1
    IL_0013:  beq        IL_00d2
    IL_0018:  newobj     ""C.<>c__DisplayClass3_0..ctor()""
    IL_001d:  stloc.2
    IL_001e:  ldloc.2
    IL_001f:  ldarg.0
    IL_0020:  ldfld      ""object C.<M>d__3.x""
    IL_0025:  stfld      ""object C.<>c__DisplayClass3_0.x""
    IL_002a:  ldloc.1
    IL_002b:  call       ""object C.F()""
    IL_0030:  ldloc.2
    IL_0031:  ldftn      ""bool C.<>c__DisplayClass3_0.<M>b__0(object)""
    IL_0037:  newobj     ""System.Func<object, bool>..ctor(object, System.IntPtr)""
    IL_003c:  call       ""System.Threading.Tasks.Task<object> C.E(object, System.Func<object, bool>)""
    IL_0041:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<object> System.Threading.Tasks.Task<object>.GetAwaiter()""
    IL_0046:  stloc.s    V_4
    IL_0048:  ldloca.s   V_4
    IL_004a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<object>.IsCompleted.get""
    IL_004f:  brtrue.s   IL_0092
    IL_0051:  ldarg.0
    IL_0052:  ldc.i4.0
    IL_0053:  dup
    IL_0054:  stloc.0
    IL_0055:  stfld      ""int C.<M>d__3.<>1__state""
    IL_005a:  ldarg.0
    IL_005b:  ldloc.s    V_4
    IL_005d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<object> C.<M>d__3.<>u__1""
    IL_0062:  ldarg.0
    IL_0063:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__3.<>t__builder""
    IL_0068:  ldloca.s   V_4
    IL_006a:  ldarg.0
    IL_006b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>, C.<M>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<object>, ref C.<M>d__3)""
    IL_0070:  leave      IL_0124
    IL_0075:  ldarg.0
    IL_0076:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<object> C.<M>d__3.<>u__1""
    IL_007b:  stloc.s    V_4
    IL_007d:  ldarg.0
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<object> C.<M>d__3.<>u__1""
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<object>""
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.m1
    IL_008b:  dup
    IL_008c:  stloc.0
    IL_008d:  stfld      ""int C.<M>d__3.<>1__state""
    IL_0092:  ldloca.s   V_4
    IL_0094:  call       ""object System.Runtime.CompilerServices.TaskAwaiter<object>.GetResult()""
    IL_0099:  stloc.3
    IL_009a:  ldloc.1
    IL_009b:  ldloc.3
    IL_009c:  call       ""System.Threading.Tasks.Task C.G(object)""
    IL_00a1:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00a6:  stloc.s    V_5
    IL_00a8:  ldloca.s   V_5
    IL_00aa:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00af:  brtrue.s   IL_00ef
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.1
    IL_00b3:  dup
    IL_00b4:  stloc.0
    IL_00b5:  stfld      ""int C.<M>d__3.<>1__state""
    IL_00ba:  ldarg.0
    IL_00bb:  ldloc.s    V_5
    IL_00bd:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__3.<>u__2""
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__3.<>t__builder""
    IL_00c8:  ldloca.s   V_5
    IL_00ca:  ldarg.0
    IL_00cb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<M>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<M>d__3)""
    IL_00d0:  leave.s    IL_0124
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__3.<>u__2""
    IL_00d8:  stloc.s    V_5
    IL_00da:  ldarg.0
    IL_00db:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<M>d__3.<>u__2""
    IL_00e0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e6:  ldarg.0
    IL_00e7:  ldc.i4.m1
    IL_00e8:  dup
    IL_00e9:  stloc.0
    IL_00ea:  stfld      ""int C.<M>d__3.<>1__state""
    IL_00ef:  ldloca.s   V_5
    IL_00f1:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00f6:  leave.s    IL_0111
  }
  catch System.Exception
  {
    IL_00f8:  stloc.s    V_6
    IL_00fa:  ldarg.0
    IL_00fb:  ldc.i4.s   -2
    IL_00fd:  stfld      ""int C.<M>d__3.<>1__state""
    IL_0102:  ldarg.0
    IL_0103:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__3.<>t__builder""
    IL_0108:  ldloc.s    V_6
    IL_010a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010f:  leave.s    IL_0124
  }
  IL_0111:  ldarg.0
  IL_0112:  ldc.i4.s   -2
  IL_0114:  stfld      ""int C.<M>d__3.<>1__state""
  IL_0119:  ldarg.0
  IL_011a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__3.<>t__builder""
  IL_011f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0124:  ret
}");
            }
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.<M>d__3.MoveNext()", atLineNumber: 999);
                    string error;
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression("z ?? x", out error, testData);
                    Assert.NotNull(result.Assembly);
                    Assert.Null(error);
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0,
                C V_1,
                C.<>c__DisplayClass3_0 V_2, //CS$<>8__locals0
                object V_3, //z
                System.Runtime.CompilerServices.TaskAwaiter<object> V_4,
                System.Runtime.CompilerServices.TaskAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldloc.3
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000b
  IL_0004:  pop
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""object C.<M>d__3.x""
  IL_000b:  ret
}");
                });
        }

        [Fact]
        public void FileLocalType_01()
        {
            var source =
@"file class C
{
    public static int X = 42;
}

class Program
{
    public static void F()
    {
    }
}";
            var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular11, path: "path/to/MyFile.cs", Encoding.Default), options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.F");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "C.X",
                        out var error,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS0103: The name 'C' does not exist in the current context", error);
                    }
                    else
                    {
                        Assert.NotNull(result.Assembly);
                        Assert.Null(error);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size        6 (0x6)
                              .maxstack  1
                              IL_0000:  ldsfld     "int C.X"
                              IL_0005:  ret
                            }
                            """);
                    }
                });
        }

        [Fact]
        public void FileLocalType_02()
        {
            var source1 =
@"file class C
{
    public static int X = 42;
}

class Program
{
    public static void F()
    {
    }
}";
            var source2 =
@"file class C
{
    public static int X = 43;
}
";
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Regular11, path: "path/to/Source1.cs", Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.Regular11, path: "path/to/Source2.cs", Encoding.Default)
                }, options: TestOptions.DebugDll);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.F");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "C.X",
                        out var error,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS0103: The name 'C' does not exist in the current context", error);
                    }
                    else
                    {
                        Assert.Null(error);
                        Assert.NotNull(result.Assembly);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size        6 (0x6)
                              .maxstack  1
                              IL_0000:  ldsfld     "int C.X"
                              IL_0005:  ret
                            }
                            """);
                    }
                });
        }

        [Fact]
        public void FileLocalType_03()
        {
            var source1 =
@"file class Outer
{
    public class Inner
    {
        public static int X = 42;
    }
}

class Program
{
    public static void F()
    {
    }
}";
            var source2 =
@"file class Outer
{
    public class Inner
    {
        public static int X = 43;
    }
}
";
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Regular11, path: "path/to/Source1.cs", Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.Regular11, path: "path/to/Source2.cs", Encoding.Default)
                }, options: TestOptions.DebugDll);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.F");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "Outer.Inner.X",
                        out var error,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS0103: The name 'Outer' does not exist in the current context", error);
                    }
                    else
                    {
                        Assert.Null(error);
                        Assert.NotNull(result.Assembly);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size        6 (0x6)
                              .maxstack  1
                              IL_0000:  ldsfld     "int Outer.Inner.X"
                              IL_0005:  ret
                            }
                            """);
                    }
                });
        }

        [Fact]
        public void FileLocalType_04()
        {
            var source1 =
@"class Program
{
    public static void F()
    {
    }
}";
            var source2 =
@"file class C
{
    public static int X = 43;
}
";
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Regular11, path: "path/to/Source1.cs", Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.Regular11, path: "path/to/Source2.cs", Encoding.Default)
                }, options: TestOptions.DebugDll);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.F");
                    var assembly = context.CompileExpressions(
                        ImmutableArray.Create("C.X"),
                        out var methodTokens,
                        out var errorMessages);
                    Assert.Equal(new[] { "(1,1): error CS0103: The name 'C' does not exist in the current context" }, errorMessages);
                    Assert.Null(assembly);
                    Assert.Empty(methodTokens);
                });
        }

        [Fact]
        public void FileLocalType_05()
        {
            var source =
@"file class C
{
}

class Program
{
    static void Main()
    {
    }
}";
            var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular11, path: "path/to/MyFile.cs", Encoding.Default), options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.Main");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "new C()",
                        out var error,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)", error);
                    }
                    else
                    {
                        Assert.NotNull(result.Assembly);
                        Assert.Null(error);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size        6 (0x6)
                              .maxstack  1
                              IL_0000:  newobj     "C..ctor()"
                              IL_0005:  ret
                            }
                            """);
                    }
                });
        }

        [Fact]
        public void FileLocalType_06()
        {
            var source =
@"file class C
{
    int F = 1;
    void M()
    {
    }
}";
            var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular11, path: "path/to/MyFile.cs", Encoding.Default), options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "C.M");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "F",
                        out var error,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS0103: The name 'F' does not exist in the current context", error);
                    }
                    else
                    {
                        Assert.NotNull(result.Assembly);
                        Assert.Null(error);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.F"
                              IL_0006:  ret
                            }
                            """);
                    }
                });
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/66109")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/64098")]
        [Fact]
        public void FileLocalType_07()
        {
            var sourceA = """
                file class A
                {
                    public int F1() => 1;
                    public int F2() => 2;
                }
                class Program
                {
                    static void M1()
                    {
                        A x = new A();
                    }
                    static void M2()
                    {
                #line 100 "B.cs"
                        A y = new A();
                #line 200 "C.cs"
                        A z = new A();
                    }
                }
                """;
            var sourceB = """
                class B
                {
                }
                """;
            var sourceC = """
                class C
                {
                }
                """;
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(sourceA, path: "path/to/A.cs", encoding: Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(sourceB, path: "path/to/B.cs", encoding: Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(sourceC, path: "path/to/C.cs", encoding: Encoding.Default),
                },
                options: TestOptions.DebugDll);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.M1");
                    var testData = new CompilationTestData();
                    ResultProperties resultProperties;
                    string error;
                    ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                    var result = context.CompileExpression(
                        "x.F1() + (new A()).F2()",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        out resultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData);
                    if (runtime.DebugFormat == DebugInformationFormat.Pdb)
                    {
                        Assert.Null(result);
                        Assert.Equal("error CS1061: 'A' does not contain a definition for 'F1' and no accessible extension method 'F1' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)", error);
                    }
                    else
                    {
                        Assert.NotNull(result.Assembly);
                        Assert.Null(error);
                        testData.GetMethodData("<>x.<>m0").VerifyIL("""
                            {
                              // Code size       18 (0x12)
                              .maxstack  2
                              .locals init (A V_0) //x
                              IL_0000:  ldloc.0
                              IL_0001:  callvirt   "int A.F1()"
                              IL_0006:  newobj     "A..ctor()"
                              IL_000b:  call       "int A.F2()"
                              IL_0010:  add
                              IL_0011:  ret
                            }
                            """);
                    }

                    // https://github.com/dotnet/roslyn/issues/64098: EE doesn't handle methods that span multiple documents correctly
                    context = CreateMethodContext(runtime, "Program.M2");
                    testData = new CompilationTestData();
                    result = context.CompileExpression(
                        "y.F1() + (new A()).F2()",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        out resultProperties,
                        out error,
                        out missingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData);
                    Assert.Null(result);
                    Assert.Equal("error CS1061: 'A' does not contain a definition for 'F1' and no accessible extension method 'F1' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)", error);
                });
        }

        [Fact]
        public void IllFormedFilePath_01()
        {
            var source =
@"class C
{
    public static int X = 42;
}

class Program
{
    public static void F()
    {
    }
}";
            var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular11, path: "path/to/\uD800.cs", Encoding.Default), options: TestOptions.DebugDll);
            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "Program.F");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "C.X",
                        out var error,
                        testData);
                    Assert.NotNull(result.Assembly);
                    Assert.Null(error);
                    testData.GetMethodData("<>x.<>m0").VerifyIL("""
                        {
                          // Code size        6 (0x6)
                          .maxstack  1
                          IL_0000:  ldsfld     "int C.X"
                          IL_0005:  ret
                        }
                        """);
                });
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/66109")]
        [Fact]
        public void SequencePointsMultipleDocuments_01()
        {
            var sourceA =
@"partial class Program
{
    private int x = 1;
    private void F()
    {
    }
}";
            var sourceB =
@"partial class Program
{
    private int y = 2;
    public Program()
    {
        F();
        int z = x + y;
    }
}";
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(sourceA, path: "A.cs", encoding: Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(sourceB, path: "B.cs", encoding: Encoding.Default)
                },
                options: TestOptions.DebugDll);

            comp.VerifyPdb("""
                <symbols>
                  <files>
                    <file id="1" name="A.cs" language="C#" checksumAlgorithm="SHA1" checksum="09-65-32-19-5F-F8-8A-58-BF-BC-0C-D3-68-2C-2C-7B-15-33-18-E4" />
                    <file id="2" name="B.cs" language="C#" checksumAlgorithm="SHA1" checksum="62-4B-E2-91-A3-E9-43-48-4F-A0-E6-E8-22-74-EB-90-24-C3-05-A5" />
                  </files>
                  <methods>
                    <method containingType="Program" name="F">
                      <customDebugInfo>
                        <using>
                          <namespace usingCount="0" />
                        </using>
                      </customDebugInfo>
                      <sequencePoints>
                        <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="6" document="1" />
                        <entry offset="0x1" startLine="6" startColumn="5" endLine="6" endColumn="6" document="1" />
                      </sequencePoints>
                    </method>
                    <method containingType="Program" name=".ctor">
                      <customDebugInfo>
                        <forward declaringType="Program" methodName="F" />
                        <encLocalSlotMap>
                          <slot kind="0" offset="29" />
                        </encLocalSlotMap>
                      </customDebugInfo>
                      <sequencePoints>
                        <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="23" document="1" />
                        <entry offset="0x7" startLine="3" startColumn="5" endLine="3" endColumn="23" document="2" />
                        <entry offset="0xe" startLine="4" startColumn="5" endLine="4" endColumn="21" document="2" />
                        <entry offset="0x15" startLine="5" startColumn="5" endLine="5" endColumn="6" document="2" />
                        <entry offset="0x16" startLine="6" startColumn="9" endLine="6" endColumn="13" document="2" />
                        <entry offset="0x1d" startLine="7" startColumn="9" endLine="7" endColumn="23" document="2" />
                        <entry offset="0x2b" startLine="8" startColumn="5" endLine="8" endColumn="6" document="2" />
                      </sequencePoints>
                      <scope startOffset="0x0" endOffset="0x2c">
                        <scope startOffset="0x15" endOffset="0x2c">
                          <local name="z" il_index="0" il_start="0x15" il_end="0x2c" attributes="0" />
                        </scope>
                      </scope>
                    </method>
                  </methods>
                </symbols>
                """,
                format: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Pdb);
            comp.VerifyPdb("""
                <symbols>
                  <files>
                    <file id="1" name="A.cs" language="C#" checksumAlgorithm="SHA1" checksum="09-65-32-19-5F-F8-8A-58-BF-BC-0C-D3-68-2C-2C-7B-15-33-18-E4" />
                    <file id="2" name="B.cs" language="C#" checksumAlgorithm="SHA1" checksum="62-4B-E2-91-A3-E9-43-48-4F-A0-E6-E8-22-74-EB-90-24-C3-05-A5" />
                  </files>
                  <methods>
                    <method containingType="Program" name="F">
                      <sequencePoints>
                        <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="6" document="1" />
                        <entry offset="0x1" startLine="6" startColumn="5" endLine="6" endColumn="6" document="1" />
                      </sequencePoints>
                    </method>
                    <method containingType="Program" name=".ctor">
                      <customDebugInfo>
                        <encLocalSlotMap>
                          <slot kind="0" offset="29" />
                        </encLocalSlotMap>
                      </customDebugInfo>
                      <sequencePoints>
                        <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="23" document="1" />
                        <entry offset="0x7" startLine="3" startColumn="5" endLine="3" endColumn="23" document="2" />
                        <entry offset="0xe" startLine="4" startColumn="5" endLine="4" endColumn="21" document="2" />
                        <entry offset="0x15" startLine="5" startColumn="5" endLine="5" endColumn="6" document="2" />
                        <entry offset="0x16" startLine="6" startColumn="9" endLine="6" endColumn="13" document="2" />
                        <entry offset="0x1d" startLine="7" startColumn="9" endLine="7" endColumn="23" document="2" />
                        <entry offset="0x2b" startLine="8" startColumn="5" endLine="8" endColumn="6" document="2" />
                      </sequencePoints>
                      <scope startOffset="0x0" endOffset="0x2c">
                        <scope startOffset="0x15" endOffset="0x2c">
                          <local name="z" il_index="0" il_start="0x15" il_end="0x2c" attributes="0" />
                        </scope>
                      </scope>
                    </method>
                  </methods>
                </symbols>
                """,
                format: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    GetContextState(runtime, "Program..ctor", out var blocks, out var moduleVersionId, out var symReader, out var methodToken, out var localSignatureToken);

                    var appDomain = new AppDomain();
                    uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader);
                    var context = CreateMethodContext(
                        appDomain,
                        blocks,
                        symReader,
                        moduleVersionId,
                        methodToken: methodToken,
                        methodVersion: 1,
                        ilOffset: 0x15, // offset matches startOffset of "z" scope
                        localSignatureToken: localSignatureToken,
                        kind: MakeAssemblyReferencesKind.AllAssemblies);
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "z",
                        out var error,
                        testData);
                    Assert.Null(error);
                    Assert.NotNull(result.Assembly);
                    testData.GetMethodData("<>x.<>m0").VerifyIL("""
                        {
                            // Code size        2 (0x2)
                            .maxstack  1
                            .locals init (int V_0) //z
                            IL_0000:  ldloc.0
                            IL_0001:  ret
                        }
                        """);

                    testData = new CompilationTestData();
                    var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                    string typeName;
                    var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                    Assert.Equal(2, locals.Count);
                    VerifyLocal(testData, typeName, locals[0], "<>m0", "this", expectedILOpt: """
                        {
                            // Code size        2 (0x2)
                            .maxstack  1
                            .locals init (int V_0) //z
                            IL_0000:  ldarg.0
                            IL_0001:  ret
                        }
                        """);
                    VerifyLocal(testData, typeName, locals[1], "<>m1", "z", expectedILOpt: """
                        {
                            // Code size        2 (0x2)
                            .maxstack  1
                            .locals init (int V_0) //z
                            IL_0000:  ldloc.0
                            IL_0001:  ret
                        }
                        """);
                    locals.Free();
                });
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/66109")]
        [Fact]
        public void SequencePointsMultipleDocuments_02()
        {
            var sourceA = """
                class A
                {
                    static void Main()
                    {
                        int x = 1;
                #line 100 "B.cs"
                        int y = 2;
                #line 200 "C.cs"
                        int z = 3;
                    }
                }
                """;
            var sourceB = """
                class B
                {
                }
                """;
            var sourceC = """
                class C
                {
                }
                """;
            var comp = CreateCompilation(
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(sourceA, path: "A.cs", encoding: Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(sourceB, path: "B.cs", encoding: Encoding.Default),
                    SyntaxFactory.ParseSyntaxTree(sourceC, path: "C.cs", encoding: Encoding.Default),
                },
                options: TestOptions.DebugDll);

            comp.VerifyPdb("""
                <symbols>
                  <files>
                    <file id="1" name="A.cs" language="C#" checksumAlgorithm="SHA1" checksum="8E-FF-02-A2-A9-6A-80-AA-31-CC-19-BE-FA-C4-84-88-5B-C8-09-08" />
                    <file id="2" name="B.cs" language="C#" checksumAlgorithm="SHA1" checksum="29-99-77-37-69-95-33-C4-02-3B-65-8D-5F-61-43-CF-F0-04-61-C2" />
                    <file id="3" name="C.cs" language="C#" checksumAlgorithm="SHA1" checksum="A2-ED-D2-5C-84-2F-E1-0E-AB-C5-11-C8-51-E6-76-03-C8-5A-6D-06" />
                  </files>
                  <methods>
                    <method containingType="A" name="Main">
                      <customDebugInfo>
                        <using>
                          <namespace usingCount="0" />
                        </using>
                        <encLocalSlotMap>
                          <slot kind="0" offset="15" />
                          <slot kind="0" offset="53" />
                          <slot kind="0" offset="91" />
                        </encLocalSlotMap>
                      </customDebugInfo>
                      <sequencePoints>
                        <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                        <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="19" document="1" />
                        <entry offset="0x3" startLine="100" startColumn="9" endLine="100" endColumn="19" document="2" />
                        <entry offset="0x5" startLine="200" startColumn="9" endLine="200" endColumn="19" document="3" />
                        <entry offset="0x7" startLine="201" startColumn="5" endLine="201" endColumn="6" document="3" />
                      </sequencePoints>
                      <scope startOffset="0x0" endOffset="0x8">
                        <local name="x" il_index="0" il_start="0x0" il_end="0x8" attributes="0" />
                        <local name="y" il_index="1" il_start="0x0" il_end="0x8" attributes="0" />
                        <local name="z" il_index="2" il_start="0x0" il_end="0x8" attributes="0" />
                      </scope>
                    </method>
                  </methods>
                </symbols>
                """,
                format: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Pdb);
            comp.VerifyPdb("""
                <symbols>
                  <files>
                    <file id="1" name="A.cs" language="C#" checksumAlgorithm="SHA1" checksum="8E-FF-02-A2-A9-6A-80-AA-31-CC-19-BE-FA-C4-84-88-5B-C8-09-08" />
                    <file id="2" name="B.cs" language="C#" checksumAlgorithm="SHA1" checksum="29-99-77-37-69-95-33-C4-02-3B-65-8D-5F-61-43-CF-F0-04-61-C2" />
                    <file id="3" name="C.cs" language="C#" checksumAlgorithm="SHA1" checksum="A2-ED-D2-5C-84-2F-E1-0E-AB-C5-11-C8-51-E6-76-03-C8-5A-6D-06" />
                  </files>
                  <methods>
                    <method containingType="A" name="Main">
                      <customDebugInfo>
                        <using>
                          <namespace usingCount="0" />
                        </using>
                        <encLocalSlotMap>
                          <slot kind="0" offset="15" />
                          <slot kind="0" offset="53" />
                          <slot kind="0" offset="91" />
                        </encLocalSlotMap>
                      </customDebugInfo>
                      <sequencePoints>
                        <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="6" document="1" />
                        <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="19" document="1" />
                        <entry offset="0x3" startLine="100" startColumn="9" endLine="100" endColumn="19" document="2" />
                        <entry offset="0x5" startLine="200" startColumn="9" endLine="200" endColumn="19" document="3" />
                        <entry offset="0x7" startLine="201" startColumn="5" endLine="201" endColumn="6" document="3" />
                      </sequencePoints>
                      <scope startOffset="0x0" endOffset="0x8">
                        <local name="x" il_index="0" il_start="0x0" il_end="0x8" attributes="0" />
                        <local name="y" il_index="1" il_start="0x0" il_end="0x8" attributes="0" />
                        <local name="z" il_index="2" il_start="0x0" il_end="0x8" attributes="0" />
                      </scope>
                    </method>
                  </methods>
                </symbols>
                """,
                format: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb);

            WithRuntimeInstance(
                comp,
                references: null,
                includeLocalSignatures: true,
                includeIntrinsicAssembly: false,
                validator: runtime =>
                {
                    var context = CreateMethodContext(runtime, "A.Main");
                    var testData = new CompilationTestData();
                    var result = context.CompileExpression(
                        "x + y + z",
                        out var error,
                        testData);
                    Assert.Null(error);
                    Assert.NotNull(result.Assembly);
                    testData.GetMethodData("<>x.<>m0").VerifyIL("""
                        {
                          // Code size        6 (0x6)
                          .maxstack  2
                          .locals init (int V_0, //x
                                        int V_1, //y
                                        int V_2) //z
                          IL_0000:  ldloc.0
                          IL_0001:  ldloc.1
                          IL_0002:  add
                          IL_0003:  ldloc.2
                          IL_0004:  add
                          IL_0005:  ret
                        }
                        """);

                    testData = new CompilationTestData();
                    var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
                    string typeName;
                    var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
                    Assert.Equal(3, locals.Count);
                    VerifyLocal(testData, typeName, locals[0], "<>m0", "x", expectedILOpt: """
                        {
                          // Code size        2 (0x2)
                          .maxstack  1
                          .locals init (int V_0, //x
                                        int V_1, //y
                                        int V_2) //z
                          IL_0000:  ldloc.0
                          IL_0001:  ret
                        }
                        """);
                    VerifyLocal(testData, typeName, locals[1], "<>m1", "y", expectedILOpt: """
                        {
                          // Code size        2 (0x2)
                          .maxstack  1
                          .locals init (int V_0, //x
                                        int V_1, //y
                                        int V_2) //z
                          IL_0000:  ldloc.1
                          IL_0001:  ret
                        }
                        """);
                    VerifyLocal(testData, typeName, locals[2], "<>m2", "z", expectedILOpt: """
                        {
                          // Code size        2 (0x2)
                          .maxstack  1
                          .locals init (int V_0, //x
                                        int V_1, //y
                                        int V_2) //z
                          IL_0000:  ldloc.2
                          IL_0001:  ret
                        }
                        """);
                    locals.Free();
                });
        }
    }
}
