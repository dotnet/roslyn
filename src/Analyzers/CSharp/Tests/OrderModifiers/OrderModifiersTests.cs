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
public class OrderModifiersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public OrderModifiersTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpOrderModifiersDiagnosticAnalyzer(), new CSharpOrderModifiersCodeFixProvider());

    [Fact]
    public async Task TestClass()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestStruct()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInterface()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestEnum()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestDelegate()
    {
        await TestInRegularAndScript1Async(
            @"[|unsafe|] public delegate void D();",
            @"public unsafe delegate void D();");
    }

    [Fact]
    public async Task TestMethod()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestField()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestConstructor()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestProperty()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestAccessor()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestPropertyEvent()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFieldEvent()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestOperator()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestConversionOperator()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestTrivia2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestTrivia3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task PartialAtTheEndClass1()
    {
        await TestInRegularAndScript1Async(
@"[|partial|] public class C { }",
@"public partial class C { }");
    }

    [Fact]
    public async Task PartialAtTheEndClass2()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] abstract class C { }",
            @"abstract partial class C { }");
    }

    [Fact]
    public async Task PartialAtTheEndClass3()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] sealed class C { }",
            @"sealed partial class C { }");
    }

    [Fact]
    public async Task PartialAtTheEndClass4()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] static class C { }",
            @"static partial class C { }");
    }

    [Fact]
    public async Task PartialAtTheEndClass5()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] unsafe class C { }",
            @"unsafe partial class C { }");
    }

    [Fact]
    public async Task PartialAtTheEndStruct1()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] public struct S { }",
            @"public partial struct S { }");
    }

    [Fact]
    public async Task PartialAtTheEndStruct2()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] unsafe struct S { }",
            @"unsafe partial struct S { }");
    }

    [Fact]
    public async Task PartialAtTheEndInterface()
    {
        await TestInRegularAndScript1Async(
            @"[|partial|] public interface I { }",
            @"public partial interface I { }");
    }

    [Fact]
    public async Task PartialAtTheEndMethod1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task PartialAtTheEndMethod2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/52297")]
    public async Task TestInLocalFunction()
    {
        // Not handled for performance reason.
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public static async void M()
                {
                    [|async|] static void Local() { }
                }
            }
            """);
    }

    [Fact]
    public async Task TestFixAllInContainingMember_NotApplicable()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestFixAllInContainingType()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task RequiredAfterAllOnProp()
    {
        await TestInRegularAndScriptAsync("""
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
    }

    [Fact]
    public async Task RequiredAfterAllButVolatileOnField()
    {
        await TestInRegularAndScriptAsync("""
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
    }

    [Fact]
    public async Task TestFileClass()
    {
        await TestInRegularAndScriptAsync("""
            [|abstract file|] class C
            {
            }
            """,
            """
            file abstract class C
            {
            }
            """);
    }
}
