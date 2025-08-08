// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseImplicitType;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitOrImplicitType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
public sealed class UseImplicitTypeRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseImplicitTypeCodeRefactoringProvider();

    [Fact]
    public Task TestIntLocalDeclaration()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    int[||] i = 0;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    var i = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestSelection1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    [|int i = 0;|]
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    var i = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestSelection2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    [|int|] i = 0;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    var i = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestSelectionNotType()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    int [|i|] = 0;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    var i = 0;
                }
            }
            """);

    [Fact]
    public Task TestForeachInsideLocalDeclaration()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    System.Action notThisLocal = () => { foreach (int[||] i in new int[0]) { } };
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    System.Action notThisLocal = () => { foreach (var[||] i in new int[0]) { } };
                }
            }
            """);

    [Fact]
    public Task TestInIntPattern()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void Main()
                {
                    _ = 0 is int[||] i;
                }
            }
            """);

    [Fact]
    public Task TestIntLocalDeclaration_Multiple()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void Main()
                {
                    int[||] i = 0, j = j;
                }
            }
            """);

    [Fact]
    public Task TestIntLocalDeclaration_NoInitializer()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void Main()
                {
                    int[||] i;
                }
            }
            """);

    [Fact]
    public Task TestIntForLoop()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    for (int[||] i = 0;;) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    for (var i = 0;;) { }
                }
            }
            """);

    [Fact]
    public Task TestInDispose()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C : System.IDisposable
            {
                static void Main()
                {
                    using (C[||] c = new C()) { }
                }
            }
            """, """
            class C : System.IDisposable
            {
                static void Main()
                {
                    using (var c = new C()) { }
                }
            }
            """);

    [Fact]
    public Task TestIntForeachLoop()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    foreach (int[||] i in new[] { 0 }) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    foreach (var i in new[] { 0 }) { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestIntForeachLoop2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    foreach ([|int|] i in new[] { 0 }) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    foreach (var i in new[] { 0 }) { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestIntForeachLoop3()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    foreach (int [|i|] in new[] { 0 }) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    foreach (var i in new[] { 0 }) { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestIntForeachLoop4()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    foreach ([|object|] i in new[] { new object() }) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    foreach (var i in new[] { new object() }) { }
                }
            }
            """);

    [Fact]
    public Task TestIntDeconstruction()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    (int[||] i, var j) = (0, 1);
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    (var i, var j) = (0, 1);
                }
            }
            """);

    [Fact]
    public Task TestIntDeconstruction2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    (int[||] i, var j) = (0, 1);
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    (var i, var j) = (0, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public Task NoSuggestionOnForeachCollectionExpression()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    foreach (string arg in [|args|])
                    {

                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task NoSuggestionWithinAnExpression()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System;

            class C
            {
                static void Main(string[] args)
                {
                    int a = 40 [||]+ 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefLocal1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    [||]ref string rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref var rStr1 = ref str;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefLocal2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref [||]string rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref var rStr1 = ref str;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefLocal3()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref string [||]rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref var rStr1 = ref str;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefReadonlyLocal1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly [||]string rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly var rStr1 = ref str;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefReadonlyLocal2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly string[||] rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly var rStr1 = ref str;
                }
            }
            """);

    private async Task TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(string initialMarkup, string expectedMarkup)
    {
        // Enabled because the diagnostic is disabled
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithNone()));

        // Enabled because the diagnostic is checking for the other direction
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithNone()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithSilent()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithInfo()));

        // Disabled because the diagnostic will report it instead
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithSilent()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithInfo()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithWarning()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithError()));

        // Currently this refactoring is still enabled in cases where it would cause a warning or error
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithWarning()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithError()));
    }

    private async Task TestMissingInRegularAndScriptAsync(string initialMarkup)
    {
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithNone()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithNone()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithSilent()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithSilent()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithInfo()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithInfo()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithWarning()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithWarning()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferImplicitTypeWithError()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithError()));
    }
}
