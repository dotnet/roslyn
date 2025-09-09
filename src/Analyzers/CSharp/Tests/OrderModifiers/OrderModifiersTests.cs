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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
            @"[|unsafe|] public delegate void D();",
            @"public unsafe delegate void D();");

    [Fact]
    public Task TestMethod()
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
@"[|partial|] public class C { }",
@"public partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass2()
        => TestInRegularAndScriptAsync(
            @"[|partial|] abstract class C { }",
            @"abstract partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass3()
        => TestInRegularAndScriptAsync(
            @"[|partial|] sealed class C { }",
            @"sealed partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass4()
        => TestInRegularAndScriptAsync(
            @"[|partial|] static class C { }",
            @"static partial class C { }");

    [Fact]
    public Task PartialAtTheEndClass5()
        => TestInRegularAndScriptAsync(
            @"[|partial|] unsafe class C { }",
            @"unsafe partial class C { }");

    [Fact]
    public Task PartialAtTheEndStruct1()
        => TestInRegularAndScriptAsync(
            @"[|partial|] public struct S { }",
            @"public partial struct S { }");

    [Fact]
    public Task PartialAtTheEndStruct2()
        => TestInRegularAndScriptAsync(
            @"[|partial|] unsafe struct S { }",
            @"unsafe partial struct S { }");

    [Fact]
    public Task PartialAtTheEndInterface()
        => TestInRegularAndScriptAsync(
            @"[|partial|] public interface I { }",
            @"public partial interface I { }");

    [Fact]
    public Task PartialAtTheEndMethod1()
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
        => TestInRegularAndScriptAsync(
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
