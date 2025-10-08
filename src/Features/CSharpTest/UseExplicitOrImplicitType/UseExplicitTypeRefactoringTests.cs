// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseExplicitType;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseExplicitOrImplicitType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
public sealed class UseExplicitTypeRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExplicitTypeCodeRefactoringProvider();

    [Fact]
    public Task TestIntLocalDeclaration()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            class C
            {
                static void Main()
                {
                    var[||] i = 0;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    int i = 0;
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
                    System.Action notThisLocal = () => { foreach (var[||] i in new int[0]) { } };
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    System.Action notThisLocal = () => { foreach (int[||] i in new int[0]) { } };
                }
            }
            """);

    [Fact]
    public Task TestInVarPattern()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void Main()
                {
                    _ = 0 is var[||] i;
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
                    var[||] i = 0, j = j;
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
                    var[||] i;
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
                    for (var[||] i = 0;;) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    for (int i = 0;;) { }
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
                    using (var[||] c = new C()) { }
                }
            }
            """, """
            class C : System.IDisposable
            {
                static void Main()
                {
                    using (C c = new C()) { }
                }
            }
            """);

    [Fact]
    public Task TestTypelessVarLocalDeclaration()
        => TestMissingInRegularAndScriptAsync("""
            class var
            {
                static void Main()
                {
                    var[||] i = null;
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
                    foreach (var[||] i in new[] { 0 }) { }
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    foreach (int i in new[] { 0 }) { }
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
                    var[||] (i, j) = (0, 1);
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    (int i, int j) = (0, 1);
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
                    (var[||] i, var j) = (0, 1);
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    (int i, var j) = (0, 1);
                }
            }
            """);

    [Fact]
    public Task TestWithAnonymousType()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                static void Main()
                {
                    [|var|] x = new { Amount = 108, Message = "Hello" };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public Task NoSuggestionOnForeachCollectionExpression()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                void Method(List<int> var)
                {
                    foreach (int value in [|var|])
                    {
                        Console.WriteLine(value.Value);
                    }
                }
            }
            """);

    [Fact]
    public Task NotOnConstVar()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [||]var v = 0;
                }
            }
            """);

    [Fact]
    public Task TestWithTopLevelNullability()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string? s_data;

                static void Main()
                {
                    string? v = s_data;
                }
            }
            """);

    [Fact]
    public Task TestWithTopLevelAndNestedArrayNullability1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string?[]?[,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string?[]?[,]? s_data;

                static void Main()
                {
                    string?[]?[,]? v = s_data;
                }
            }
            """);

    [Fact]
    public Task TestWithTopLevelAndNestedArrayNullability2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string?[][,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string?[][,]? s_data;

                static void Main()
                {
                    string?[][,]? v = s_data;
                }
            }
            """);

    [Fact]
    public Task TestWithTopLevelAndNestedArrayNullability3()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string?[]?[,][,,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string?[]?[,][,,]? s_data;

                static void Main()
                {
                    string?[]?[,][,,]? v = s_data;
                }
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    string v = s_data;
                }
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    var[||] v = s_data;
                    v = null;
                }
            }
            """, """
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    string? v = s_data;
                    v = null;
                }
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment3()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    var[||] v = s_data;
                    v = GetNullableString();
                }

                static string? GetNullableString() => null;
            }
            """, """
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    string? v = s_data;
                    v = GetNullableString();
                }

                static string? GetNullableString() => null;
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Foreach1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System.Collections;

            class C
            {
                static void Main()
                {
                    foreach (var[||] item in new string?[] { "", null })
                    {
                    }
                }
            }
            """, """
            #nullable enable

            using System.Collections;

            class C
            {
                static void Main()
                {
                    foreach (string? item in new string?[] { "", null })
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Foreach2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System.Collections;

            class C
            {
                static void Main()
                {
                    foreach (var[||] item in new string[] { "" })
                    {
                    }
                }
            }
            """, """
            #nullable enable

            using System.Collections;

            class C
            {
                static void Main()
                {
                    foreach (string item in new string[] { "" })
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Lambda1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                private static string s_data = "";

                static void Main()
                {
                    Action a = () => {
                        var[||] v = s_data;
                        v = GetNullableString();
                    };
                }

                static string? GetNullableString() => null;
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                private static string s_data = "";

                static void Main()
                {
                    Action a = () => {
                        string? v = s_data;
                        v = GetNullableString();
                    };
                }

                static string? GetNullableString() => null;
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Lambda2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    Action a = () => {
                        var[||] v = "";
                        v = GetString();
                    };
                }

                static string GetString() => "";
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    Action a = () => {
                        string v = "";
                        v = GetString();
                    };
                }

                static string GetString() => "";
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Lambda3()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    [||]var v = "";
                    Action a = () => {
                        v = GetString();
                    };
                }

                static string GetString() => "";
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    string v = "";
                    Action a = () => {
                        v = GetString();
                    };
                }

                static string GetString() => "";
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Lambda4()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    var[||] v = "";
                    Action a = () => {
                        v = GetString();
                    };
                }

                static string? GetString() => null;
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                static void Main()
                {
                    string? v = "";
                    Action a = () => {
                        v = GetString();
                    };
                }

                static string? GetString() => null;
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Property1()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                string S
                {
                    get 
                    {
                        var[||] v = "";
                        v = GetString();
                        return v;
                    }
                }

                static void Main()
                {
                }

                static string GetString() => "";
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                string S
                {
                    get 
                    {
                        string v = "";
                        v = GetString();
                        return v;
                    }
                }

                static void Main()
                {
                }

                static string GetString() => "";
            }
            """);

    [Fact]
    public Task TestNullabilityAssignment_Property2()
        => TestInRegularAndScriptWhenDiagnosticNotAppliedAsync("""
            #nullable enable

            using System;

            class C
            {
                string? S
                {
                    get 
                    {
                        var[||] v = "";
                        v = GetString();
                        return v;
                    }
                }

                static void Main()
                {
                }

                static string? GetString() => null;
            }
            """, """
            #nullable enable

            using System;

            class C
            {
                string? S
                {
                    get 
                    {
                        string? v = "";
                        v = GetString();
                        return v;
                    }
                }

                static void Main()
                {
                }

                static string? GetString() => null;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public Task TestRefLocal1()
        => TestMissingAsync("""
            class C
            {
                static void Main()
                {
                    string str = "";

                    [||]ref var rStr1 = ref str;
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

                    ref [||]var rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref string rStr1 = ref str;
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

                    ref var [||]rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref string rStr1 = ref str;
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

                    ref readonly [||]var rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly string rStr1 = ref str;
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

                    ref readonly var[||] rStr1 = ref str;
                }
            }
            """, """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly string rStr1 = ref str;
                }
            }
            """);

    private async Task TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(string initialMarkup, string expectedMarkup)
    {
        // Enabled because the diagnostic is disabled
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferExplicitTypeWithNone()));

        // Enabled because the diagnostic is checking for the other direction
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithNone()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithSilent()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithInfo()));

        // Disabled because the diagnostic will report it instead
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithSilent()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithInfo()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithWarning()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithError()));

        // Currently this refactoring is still enabled in cases where it would cause a warning or error
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithWarning()));
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: this.PreferImplicitTypeWithError()));
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
