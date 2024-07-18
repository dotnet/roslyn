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
public class UseExplicitTypeRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new UseExplicitTypeCodeRefactoringProvider();

    [Fact]
    public async Task TestIntLocalDeclaration()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    var[||] i = 0;
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    int i = 0;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestForeachInsideLocalDeclaration()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    System.Action notThisLocal = () => { foreach (var[||] i in new int[0]) { } };
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    System.Action notThisLocal = () => { foreach (int[||] i in new int[0]) { } };
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestInVarPattern()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    _ = 0 is var[||] i;
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact]
    public async Task TestIntLocalDeclaration_Multiple()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    var[||] i = 0, j = j;
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact]
    public async Task TestIntLocalDeclaration_NoInitializer()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    var[||] i;
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact]
    public async Task TestIntForLoop()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    for (var[||] i = 0;;) { }
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    for (int i = 0;;) { }
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestInDispose()
    {
        var code = """
            class C : System.IDisposable
            {
                static void Main()
                {
                    using (var[||] c = new C()) { }
                }
            }
            """;

        var expected = """
            class C : System.IDisposable
            {
                static void Main()
                {
                    using (C c = new C()) { }
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestTypelessVarLocalDeclaration()
    {
        var code = """
            class var
            {
                static void Main()
                {
                    var[||] i = null;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact]
    public async Task TestIntForeachLoop()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    foreach (var[||] i in new[] { 0 }) { }
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    foreach (int i in new[] { 0 }) { }
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestIntDeconstruction()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    var[||] (i, j) = (0, 1);
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    (int i, int j) = (0, 1);
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestIntDeconstruction2()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    (var[||] i, var j) = (0, 1);
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    (int i, var j) = (0, 1);
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestWithAnonymousType()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    [|var|] x = new { Amount = 108, Message = "Hello" };
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public async Task NoSuggestionOnForeachCollectionExpression()
    {
        var code = """
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
            """;

        // We never want to get offered here under any circumstances.
        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact]
    public async Task NotOnConstVar()
    {
        // This error case is handled by a separate code fix (UseExplicitTypeForConst).
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [||]var v = 0;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithTopLevelNullability()
    {
        var code = """
            #nullable enable

            class C
            {
                private static string? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """;

        var expected = """
            #nullable enable

            class C
            {
                private static string? s_data;

                static void Main()
                {
                    string? v = s_data;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestWithTopLevelAndNestedArrayNullability1()
    {
        var code = """
            #nullable enable

            class C
            {
                private static string?[]?[,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """;

        var expected = """
            #nullable enable

            class C
            {
                private static string?[]?[,]? s_data;

                static void Main()
                {
                    string?[]?[,]? v = s_data;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestWithTopLevelAndNestedArrayNullability2()
    {
        var code = """
            #nullable enable

            class C
            {
                private static string?[][,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """;

        var expected = """
            #nullable enable

            class C
            {
                private static string?[][,]? s_data;

                static void Main()
                {
                    string?[][,]? v = s_data;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestWithTopLevelAndNestedArrayNullability3()
    {
        var code = """
            #nullable enable

            class C
            {
                private static string?[]?[,][,,]? s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """;

        var expected = """
            #nullable enable

            class C
            {
                private static string?[]?[,][,,]? s_data;

                static void Main()
                {
                    string?[]?[,][,,]? v = s_data;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment1()
    {
        var code = """
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    var[||] v = s_data;
                }
            }
            """;

        var expected = """
            #nullable enable

            class C
            {
                private static string s_data;

                static void Main()
                {
                    string v = s_data;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment2()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment3()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Foreach1()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Foreach2()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Lambda1()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Lambda2()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Lambda3()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Lambda4()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Property1()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact]
    public async Task TestNullabilityAssignment_Property2()
    {
        var code = """
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
            """;

        var expected = """
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
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public async Task TestRefLocal1()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    [||]ref var rStr1 = ref str;
                }
            }
            """;

        await TestMissingAsync(code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public async Task TestRefLocal2()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref [||]var rStr1 = ref str;
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref string rStr1 = ref str;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public async Task TestRefLocal3()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref var [||]rStr1 = ref str;
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref string rStr1 = ref str;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public async Task TestRefReadonlyLocal1()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly [||]var rStr1 = ref str;
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly string rStr1 = ref str;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42880")]
    public async Task TestRefReadonlyLocal2()
    {
        var code = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly var[||] rStr1 = ref str;
                }
            }
            """;

        var expected = """
            class C
            {
                static void Main()
                {
                    string str = "";

                    ref readonly string rStr1 = ref str;
                }
            }
            """;

        await TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(code, expected);
    }

    private async Task TestInRegularAndScriptWhenDiagnosticNotAppliedAsync(string initialMarkup, string expectedMarkup)
    {
        // Enabled because the diagnostic is disabled
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferExplicitTypeWithNone());

        // Enabled because the diagnostic is checking for the other direction
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferImplicitTypeWithNone());
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferImplicitTypeWithSilent());
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferImplicitTypeWithInfo());

        // Disabled because the diagnostic will report it instead
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithSilent()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithInfo()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithWarning()));
        await TestMissingInRegularAndScriptAsync(initialMarkup, parameters: new TestParameters(options: this.PreferExplicitTypeWithError()));

        // Currently this refactoring is still enabled in cases where it would cause a warning or error
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferImplicitTypeWithWarning());
        await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, options: this.PreferImplicitTypeWithError());
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
