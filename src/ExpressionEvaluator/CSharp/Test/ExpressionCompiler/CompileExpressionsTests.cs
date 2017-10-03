// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
                        "(1,5): error CS1733: Expected expression",
                        "(1,1): error CS1525: Invalid expression term '??'"
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
                        "(1,1): error CS0103: The name 'z' does not exist in the current context",
                        "(1,6): error CS0103: The name 'z' does not exist in the current context"
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
                        $"error CS7013: Name '<{longName}>i__Field' exceeds the maximum length allowed in metadata.",
                        $"error CS7013: Name '<{longName}>j__TPar' exceeds the maximum length allowed in metadata.",
                        $"error CS7013: Name '<{longName}>i__Field' exceeds the maximum length allowed in metadata.",
                        $"error CS7013: Name 'get_{longName}' exceeds the maximum length allowed in metadata.",
                        $"error CS7013: Name '{longName}' exceeds the maximum length allowed in metadata.",
                        $"error CS7013: Name '{longName}' exceeds the maximum length allowed in metadata."
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
                        new[] { "(1,11): error CS8185: A declaration is not allowed in this context." },
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
            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);
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
                        "(1,1): error CS0103: The name '$exception' does not exist in the current context",
                        "(1,1): error CS0103: The name '$1' does not exist in the current context",
                        "(1,7): error CS0103: The name '$unknown' does not exist in the current context",
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
            var comp = CreateStandardCompilation(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.DebugDll);
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
    }
}
