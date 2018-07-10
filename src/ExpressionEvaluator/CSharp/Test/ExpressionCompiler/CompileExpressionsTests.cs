// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
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
                        new[] { $"(1,11): error CS8185: { CSharpResources.ERR_DeclarationExpressionNotPermitted }" },
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

        [WorkItem(482753, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=482753")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
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
    }
}
