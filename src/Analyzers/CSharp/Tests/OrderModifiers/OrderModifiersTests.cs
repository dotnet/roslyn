// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OrderModifiers;

[Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)]
public sealed class OrderModifiersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public OrderModifiersTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpOrderModifiersDiagnosticAnalyzer(), new CSharpOrderModifiersCodeFixProvider());

    [Fact]
    public Task TestClass()
        => TestInRegularAndScript1Async(
            """
            [|static|] internal class C
            {
            }
            """,
            """
            internal static class C
            {
            }
            """);

    [Fact]
    public Task TestStruct()
        => TestInRegularAndScript1Async(
            """
            [|unsafe|] public struct C
            {
            }
            """,
            """
            public unsafe struct C
            {
            }
            """);

    [Fact]
    public Task TestInterface()
        => TestInRegularAndScript1Async(
            """
            [|unsafe|] public interface C
            {
            }
            """,
            """
            public unsafe interface C
            {
            }
            """);

    [Fact]
    public Task TestEnum()
        => TestInRegularAndScript1Async(
            """
            [|internal|] protected enum C
            {
            }
            """,
            """
            protected internal enum C
            {
            }
            """);

    [Fact]
    public Task TestDelegate()
        => TestInRegularAndScript1Async(
            @"[|unsafe|] public delegate void D();",
            @"public unsafe delegate void D();");

    [Fact]
    public Task TestMethod()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|unsafe|] public void M() { }
            }
            """,
            """
            class C
            {
                public unsafe void M() { }
            }
            """);

    [Fact]
    public Task TestField()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|unsafe|] public int a;
            }
            """,
            """
            class C
            {
                public unsafe int a;
            }
            """);

    [Fact]
    public Task TestConstructor()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|unsafe|] public C() { }
            }
            """,
            """
            class C
            {
                public unsafe C() { }
            }
            """);

    [Fact]
    public Task TestProperty()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|unsafe|] public int P { get; }
            }
            """,
            """
            class C
            {
                public unsafe int P { get; }
            }
            """);

    [Fact]
    public Task TestAccessor()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                int P { [|internal|] protected get; }
            }
            """,
            """
            class C
            {
                int P { protected internal get; }
            }
            """);

    [Fact]
    public Task TestPropertyEvent()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|internal|] protected event Action P { add { } remove { } }
            }
            """,
            """
            class C
            {
                protected internal event Action P { add { } remove { } }
            }
            """);

    [Fact]
    public Task TestFieldEvent()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|internal|] protected event Action P;
            }
            """,
            """
            class C
            {
                protected internal event Action P;
            }
            """);

    [Fact]
    public Task TestOperator()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|static|] public C operator +(C c1, C c2) { }
            }
            """,
            """
            class C
            {
                public static C operator +(C c1, C c2) { }
            }
            """);

    [Fact]
    public Task TestConversionOperator()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                [|static|] public implicit operator bool(C c1) { }
            }
            """,
            """
            class C
            {
                public static implicit operator bool(C c1) { }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScript1Async(
            """
            {|FixAllInDocument:static|} internal class C
            {
                static internal class Nested { }
            }
            """,
            """
            internal static class C
            {
                internal static class Nested { }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScript1Async(
            """
            static internal class C
            {
                {|FixAllInDocument:static|} internal class Nested { }
            }
            """,
            """
            internal static class C
            {
                internal static class Nested { }
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScript1Async(
            """
            /// Doc comment
            [|static|] internal class C
            {
            }
            """,
            """
            /// Doc comment
            internal static class C
            {
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScript1Async(
            """
            /* start */ [|static|] /* middle */ internal /* end */ class C
            {
            }
            """,
            """
            /* start */ internal /* middle */ static /* end */ class C
            {
            }
            """);

    [Fact]
    public Task TestTrivia3()
        => TestInRegularAndScript1Async(
            """
            #if true
            [|static|] internal class C
            {
            }
            #endif
            """,
            """
            #if true
            internal static class C
            {
            }
            #endif
            """);

    [Fact]
    public Task PartialAtTheEndClass1()
        => TestInRegularAndScript1Async(
@"[|partial|] public class C { }",
@"public partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass2()
        => TestInRegularAndScript1Async(
            @"[|partial|] abstract class C { }",
            @"abstract partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass3()
        => TestInRegularAndScript1Async(
            @"[|partial|] sealed class C { }",
            @"sealed partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass4()
        => TestInRegularAndScript1Async(
            @"[|partial|] static class C { }",
            @"static partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass5()
        => TestInRegularAndScript1Async(
            @"[|partial|] unsafe class C { }",
            @"unsafe partial class C { }");

    [Fact]
    public Task PartialAtTheEndStruct1()
        => TestInRegularAndScript1Async(
            @"[|partial|] public struct S { }",
            @"public partial struct S { }");

    [Fact]
    public Task PartialAtTheEndStruct2()
        => TestInRegularAndScript1Async(
            @"[|partial|] unsafe struct S { }",
            @"unsafe partial struct S { }");

    [Fact]
    public Task PartialAtTheEndInterface()
        => TestInRegularAndScript1Async(
            @"[|partial|] public interface I { }",
            @"public partial interface I { }");

    [Fact]
    public Task PartialAtTheEndMethod1()
        => TestInRegularAndScript1Async(
            """
            partial class C
            {
                [|partial|] static void M();
            }
            """,
            """
            partial class C
            {
                static partial void M();
            }
            """);

    [Fact]
    public Task PartialAtTheEndMethod2()
        => TestInRegularAndScript1Async(
            """
            partial class C
            {
                [|partial|] unsafe void M();
            }
            """,
            """
            partial class C
            {
                unsafe partial void M();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/52297")]
    public Task TestInLocalFunction()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public static async void M()
                {
                    [|async|] static void Local() { }
                }
            }
            """);

    [Fact]
    public Task TestFixAllInContainingMember_NotApplicable()
        => TestMissingInRegularAndScriptAsync(
            """
            {|FixAllInContainingMember:static|} internal class C
            {
                static internal class Nested { }
            }

            static internal class C2
            {
                static internal class Nested { }
            }
            """);

    [Fact]
    public Task TestFixAllInContainingType()
        => TestInRegularAndScript1Async(
            """
            {|FixAllInContainingType:static|} internal class C
            {
                static internal class Nested { }
            }

            static internal class C2
            {
                static internal class Nested { }
            }
            """,
            """
            internal static class C
            {
                internal static class Nested { }
            }

            static internal class C2
            {
                static internal class Nested { }
            }
            """);

    [Fact]
    public Task RequiredAfterAllOnProp()
        => TestInRegularAndScriptAsync("""
            class C
            {
                [|required|] public virtual unsafe int Prop { get; init; }
            }
            """,
            """
            class C
            {
                public virtual unsafe required int Prop { get; init; }
            }
            """);

    [Fact]
    public Task RequiredAfterAllButVolatileOnField()
        => TestInRegularAndScriptAsync("""
            class C
            {
                [|required|] public unsafe volatile int Field;
            }
            """,
            """
            class C
            {
                public unsafe required volatile int Field;
            }
            """);

    [Fact]
    public Task TestFileClass()
        => TestInRegularAndScriptAsync("""
            [|abstract file|] class C
            {
            }
            """,
            """
            file abstract class C
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7553")]
    public Task TestEmptySelection()
        => TestInRegularAndScript1Async(
            """
            namespace M;
            [||]static internal class C
            {
            }
            """,
            """
            namespace M;
            internal static class C
            {
            }
            """, TestParameters.Default.WithIncludeDiagnosticsOutsideSelection(false));
}
