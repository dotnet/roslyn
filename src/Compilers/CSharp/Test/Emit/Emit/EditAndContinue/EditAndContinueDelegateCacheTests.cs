// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

public class EditAndContinueDelegateCacheTests : EditAndContinueTestBase
{
    [Fact]
    public void TargetChanged0()
    {
        var source0 = @"
class C
{
    static int Target0() => 0;
    static int Target1() => 1;

    System.Func<int> F() => Target0;
}
";
        var source1 = @"
class C
{
    static int Target0() => 0;
    static int Target1() => 1;

    System.Func<int> F() => Target1;
}
";
        var compilation0 = CreateCompilation(source0);
        var compilation1 = compilation0.WithSource(source1);

        Assert.Equal(compilation0.LanguageVersion, compilation1.LanguageVersion);

        var f0 = compilation0.GetMember<MethodSymbol>("C.F");
        var f1 = compilation1.GetMember<MethodSymbol>("C.F");

        var v0 = CompileAndVerify(compilation0);
        using var moduleData0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
        var methodData0 = v0.TestData.GetMethodData("C.F");

        var generation0 = EmitBaseline.CreateInitialBaseline(moduleData0, methodData0.EncDebugInfoProvider());
        var diff1 = compilation1.EmitDifference(
            generation0,
            ImmutableArray.Create(
                SemanticEdit.Create(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

        diff1.EmitResult.Diagnostics.Verify();
        diff1.VerifyIL("C.F", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> C.<>O#1.<0>__Target1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C.Target1()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> C.<>O#1.<0>__Target1""
  IL_001b:  ret
}
");

        var reader0 = moduleData0.MetadataReader;
        var reader1 = diff1.GetMetadata().Reader;

        CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>O");
        CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>O#1");
    }

    [Fact]
    public void TargetChanged1()
    {
        var source0 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F() => Target0<T>;
}
";
        var source1 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F() => Target1<T>;
}
";
        var compilation0 = CreateCompilation(source0);
        var compilation1 = compilation0.WithSource(source1);

        Assert.Equal(compilation0.LanguageVersion, compilation1.LanguageVersion);

        var f0 = compilation0.GetMember<MethodSymbol>("C.F");
        var f1 = compilation1.GetMember<MethodSymbol>("C.F");

        var v0 = CompileAndVerify(compilation0);
        using var moduleData0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
        var methodData0 = v0.TestData.GetMethodData("C<T>.F");

        var generation0 = EmitBaseline.CreateInitialBaseline(moduleData0, methodData0.EncDebugInfoProvider());
        var diff1 = compilation1.EmitDifference(
            generation0,
            ImmutableArray.Create(
                SemanticEdit.Create(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

        diff1.EmitResult.Diagnostics.Verify();
        diff1.VerifyIL("C<T>.F", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> C<T>.<>O#1.<0>__Target1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C<T>.Target1<T>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> C<T>.<>O#1.<0>__Target1""
  IL_001b:  ret
}
");

        var reader0 = moduleData0.MetadataReader;
        var reader1 = diff1.GetMetadata().Reader;

        CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "<>O");
        CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>O#1");
    }

    [Fact]
    public void TargetChanged2()
    {
        var source0 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target0<T>;
}
";
        var source1 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target1<T>;
}
";
        var compilation0 = CreateCompilation(source0);
        var compilation1 = compilation0.WithSource(source1);

        Assert.Equal(compilation0.LanguageVersion, compilation1.LanguageVersion);

        var f0 = compilation0.GetMember<MethodSymbol>("C.F");
        var f1 = compilation1.GetMember<MethodSymbol>("C.F");

        var v0 = CompileAndVerify(compilation0);
        using var moduleData0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
        var methodData0 = v0.TestData.GetMethodData("C<T>.F<G>");

        var generation0 = EmitBaseline.CreateInitialBaseline(moduleData0, methodData0.EncDebugInfoProvider());
        var diff1 = compilation1.EmitDifference(
            generation0,
            ImmutableArray.Create(
                SemanticEdit.Create(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

        diff1.EmitResult.Diagnostics.Verify();
        diff1.VerifyIL("C<T>.F<G>", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> C<T>.<>O#1.<0>__Target1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C<T>.Target1<T>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> C<T>.<>O#1.<0>__Target1""
  IL_001b:  ret
}
");

        var reader0 = moduleData0.MetadataReader;
        var reader1 = diff1.GetMetadata().Reader;

        CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "<>O");
        CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>O#1");
    }

    [Fact]
    public void TargetChanged3()
    {
        var source0 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target0<T>;
}
";
        var source1 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target1<G>;
}
";
        var compilation0 = CreateCompilation(source0);
        var compilation1 = compilation0.WithSource(source1);

        Assert.Equal(compilation0.LanguageVersion, compilation1.LanguageVersion);

        var f0 = compilation0.GetMember<MethodSymbol>("C.F");
        var f1 = compilation1.GetMember<MethodSymbol>("C.F");

        var v0 = CompileAndVerify(compilation0);
        using var moduleData0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
        var methodData0 = v0.TestData.GetMethodData("C<T>.F<G>");

        var generation0 = EmitBaseline.CreateInitialBaseline(moduleData0, methodData0.EncDebugInfoProvider());
        var diff1 = compilation1.EmitDifference(
            generation0,
            ImmutableArray.Create(
                SemanticEdit.Create(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

        diff1.EmitResult.Diagnostics.Verify();
        diff1.VerifyIL("C<T>.F<G>", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> C<T>.<F>O__2_0#1<G>.<0>__Target1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C<T>.Target1<G>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> C<T>.<F>O__2_0#1<G>.<0>__Target1""
  IL_001b:  ret
}
");

        var reader0 = moduleData0.MetadataReader;
        var reader1 = diff1.GetMetadata().Reader;

        CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "<>O");
        CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<F>O__2_0#1`1");
    }

    [Fact]
    public void TargetChanged4()
    {
        var source0 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target0<G>;
}
";
        var source1 = @"
class C<T>
{
    static int Target0<G>() => 0;
    static int Target1<G>() => 1;

    System.Func<int> F<G>() => Target1<G>;
}
";
        var compilation0 = CreateCompilation(source0);
        var compilation1 = compilation0.WithSource(source1);

        Assert.Equal(compilation0.LanguageVersion, compilation1.LanguageVersion);

        var f0 = compilation0.GetMember<MethodSymbol>("C.F");
        var f1 = compilation1.GetMember<MethodSymbol>("C.F");

        var v0 = CompileAndVerify(compilation0);
        using var moduleData0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
        var methodData0 = v0.TestData.GetMethodData("C<T>.F<G>");

        var generation0 = EmitBaseline.CreateInitialBaseline(moduleData0, methodData0.EncDebugInfoProvider());
        var diff1 = compilation1.EmitDifference(
            generation0,
            ImmutableArray.Create(
                SemanticEdit.Create(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

        diff1.EmitResult.Diagnostics.Verify();
        diff1.VerifyIL("C<T>.F<G>", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> C<T>.<F>O__2_0#1<G>.<0>__Target1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int C<T>.Target1<G>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> C<T>.<F>O__2_0#1<G>.<0>__Target1""
  IL_001b:  ret
}
");

        var reader0 = moduleData0.MetadataReader;
        var reader1 = diff1.GetMetadata().Reader;

        CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "<F>O__2_0`1");
        CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<F>O__2_0#1`1");
    }

}
