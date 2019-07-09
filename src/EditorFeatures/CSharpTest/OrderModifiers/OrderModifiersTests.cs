// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OrderModifiers
{
    public class OrderModifiersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpOrderModifiersDiagnosticAnalyzer(), new CSharpOrderModifiersCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestClass()
        {
            await TestInRegular73AndScript1Async(
@"[|static|] internal class C
{
}",
@"internal static class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestStruct()
        {
            await TestInRegular73AndScript1Async(
@"[|unsafe|] public struct C
{
}",
@"public unsafe struct C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestInterface()
        {
            await TestInRegular73AndScript1Async(
@"[|unsafe|] public interface C
{
}",
@"public unsafe interface C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestEnum()
        {
            await TestInRegular73AndScript1Async(
@"[|internal|] protected enum C
{
}",
@"protected internal enum C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestDelegate()
        {
            await TestInRegular73AndScript1Async(
@"[|unsafe|] public delegate void D();",
@"public unsafe delegate void D();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestMethod()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|unsafe|] public void M() { }
}",
@"class C
{
    public unsafe void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestField()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|unsafe|] public int a;
}",
@"class C
{
    public unsafe int a;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestConstructor()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|unsafe|] public C() { }
}",
@"class C
{
    public unsafe C() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestProperty()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|unsafe|] public int P { get; }
}",
@"class C
{
    public unsafe int P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestAccessor()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    int P { [|internal|] protected get; }
}",
@"class C
{
    int P { protected internal get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestPropertyEvent()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|internal|] protected event Action P { add { } remove { } }
}",
@"class C
{
    protected internal event Action P { add { } remove { } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestFieldEvent()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|internal|] protected event Action P;
}",
@"class C
{
    protected internal event Action P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestOperator()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|static|] public C operator +(C c1, C c2) { }
}",
@"class C
{
    public static C operator +(C c1, C c2) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestConversionOperator()
        {
            await TestInRegular73AndScript1Async(
@"class C
{
    [|static|] public implicit operator bool(C c1) { }
}",
@"class C
{
    public static implicit operator bool(C c1) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestFixAll1()
        {
            await TestInRegular73AndScript1Async(
@"{|FixAllInDocument:static|} internal class C
{
    static internal class Nested { }
}",
@"internal static class C
{
    internal static class Nested { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestFixAll2()
        {
            await TestInRegular73AndScript1Async(
@"static internal class C
{
    {|FixAllInDocument:static|} internal class Nested { }
}",
@"internal static class C
{
    internal static class Nested { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestTrivia1()
        {
            await TestInRegular73AndScript1Async(
@"
/// Doc comment
[|static|] internal class C
{
}",
@"
/// Doc comment
internal static class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestTrivia2()
        {
            await TestInRegular73AndScript1Async(
@"
/* start */ [|static|] /* middle */ internal /* end */ class C
{
}",
@"
/* start */ internal /* middle */ static /* end */ class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task TestTrivia3()
        {
            await TestInRegular73AndScript1Async(
@"
#if true
[|static|] internal class C
{
}
#endif
",
@"
#if true
internal static class C
{
}
#endif
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndClass1()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] public class C { }",
@"public partial class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndClass2()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] abstract class C { }",
@"abstract partial class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndClass3()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] sealed class C { }",
@"sealed partial class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndClass4()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] static class C { }",
@"static partial class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndClass5()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] unsafe class C { }",
@"unsafe partial class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndStruct1()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] public struct S { }",
@"public partial struct S { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndStruct2()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] unsafe struct S { }",
@"unsafe partial struct S { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndInterface()
        {
            await TestInRegular73AndScript1Async(
@"[|partial|] public interface I { }",
@"public partial interface I { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndMethod1()
        {
            await TestInRegular73AndScript1Async(
@"partial class C
{
    [|partial|] static void M();
}",
@"partial class C
{
    static partial void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
        public async Task PartialAtTheEndMethod2()
        {
            await TestInRegular73AndScript1Async(
@"partial class C
{
    [|partial|] unsafe void M();
}",
@"partial class C
{
    unsafe partial void M();
}");
        }
    }
}
