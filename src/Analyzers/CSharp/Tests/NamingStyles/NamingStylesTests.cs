// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamingStyles;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles;

[Trait(Traits.Feature, Traits.Features.NamingStyle)]
public class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public NamingStylesTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    private static readonly NamingStylesTestOptionSets s_options = new NamingStylesTestOptionSets(LanguageNames.CSharp);

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpNamingStyleDiagnosticAnalyzer(), new NamingStyleCodeFixProvider());

    protected override TestComposition GetComposition()
        => base.GetComposition().AddParts(typeof(TestSymbolRenamedCodeActionOperationFactoryWorkspaceService));

    [Fact]
    public async Task TestPascalCaseClass_CorrectName()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class [|C|]
            {
            }
            """, new TestParameters(options: s_options.ClassNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseClass_NameGetsCapitalized()
    {
        await TestInRegularAndScriptAsync(
            """
            class [|c|]
            {
            }
            """,
            """
            class C
            {
            }
            """,
            options: s_options.ClassNamesArePascalCase);
    }

    [Theory]
    [InlineData("M_bar", "bar")]
    [InlineData("S_bar", "bar")]
    [InlineData("T_bar", "bar")]
    [InlineData("_Bar", "bar")]
    [InlineData("__Bar", "bar")]
    [InlineData("M_s__t_Bar", "bar")]
    [InlineData("m_bar", "bar")]
    [InlineData("s_bar", "bar")]
    [InlineData("t_bar", "bar")]
    [InlineData("_bar", "bar")]
    [InlineData("__bar", "bar")]
    [InlineData("m_s__t_Bar", "bar")]
    // Special cases to ensure empty identifiers are not produced
    [InlineData("M_", "m_")]
    [InlineData("M__", "_")]
    [InlineData("S_", "s_")]
    [InlineData("T_", "t_")]
    [InlineData("M_S__T_", "t_")]
    public async Task TestCamelCaseField_PrefixGetsStripped(string fieldName, string correctedName)
    {
        await TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                int [|{{fieldName}}|];
            }
            """,
            $$"""
            class C
            {
                int [|{{correctedName}}|];
            }
            """,
            options: s_options.FieldNamesAreCamelCase);
    }

    [Theory]
    [InlineData("M_bar", "_bar")]
    [InlineData("S_bar", "_bar")]
    [InlineData("T_bar", "_bar")]
    [InlineData("_Bar", "_bar")]
    [InlineData("__Bar", "_bar")]
    [InlineData("M_s__t_Bar", "_bar")]
    [InlineData("m_bar", "_bar")]
    [InlineData("s_bar", "_bar")]
    [InlineData("t_bar", "_bar")]
    [InlineData("bar", "_bar")]
    [InlineData("__bar", "_bar")]
    [InlineData("__s_bar", "_bar")]
    [InlineData("m_s__t_Bar", "_bar")]
    // Special cases to ensure empty identifiers are not produced
    [InlineData("M_", "_m_")]
    [InlineData("M__", "_")]
    [InlineData("S_", "_s_")]
    [InlineData("T_", "_t_")]
    [InlineData("M_S__T_", "_t_")]
    public async Task TestCamelCaseField_PrefixGetsStrippedBeforeAddition(string fieldName, string correctedName)
    {
        await TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                int [|{{fieldName}}|];
            }
            """,
            $$"""
            class C
            {
                int [|{{correctedName}}|];
            }
            """,
            options: s_options.FieldNamesAreCamelCaseWithUnderscorePrefix);
    }

    [Fact]
    public async Task TestPascalCaseMethod_CorrectName()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void [|M|]()
                {
                }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private")]
    [InlineData("protected private")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20907")]
    public async Task TestPascalCaseMethod_NoneAndDefaultAccessibilities(string accessibility)
    {
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{accessibility}} void [|m|]()
                {
                }
            }
            """, new TestParameters(options: s_options.MethodNamesWithAccessibilityArePascalCase([])));

        await TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{accessibility}} void [|m|]()
                {
                }
            }
            """,
            $$"""
            class C
            {
                {{accessibility}} void M()
                {
                }
            }
            """, options: s_options.MethodNamesWithAccessibilityArePascalCase(accessibilities: default));
    }

    [Theory]
    [InlineData("} namespace [|c2|] {", "} namespace C2 {")]
    [InlineData("class [|c2|] { }", "class C2 { }")]
    [InlineData("struct [|c2|] { }", "struct C2 { }")]
    [InlineData("interface [|c2|] { }", "interface C2 { }")]
    [InlineData("delegate void [|c2|]();", "delegate void C2();")]
    [InlineData("enum [|c2|] { }", "enum C2 { }")]
    [InlineData("class M<[|t|]> {}", "class M<T> {}")]
    [InlineData("void M<[|t|]>() {}", "void M<T>() {}")]
    [InlineData("int [|m|] { get; }", "int M { get; }")]
    [InlineData("void [|m|]() {}", "void M() {}")]
    [InlineData("void Outer() { void [|m|]() {} }", "void Outer() { void M() {} }")]
    [InlineData("int [|m|];", "int M;")]
    [InlineData("event System.EventHandler [|m|];", "event System.EventHandler M;")]
    [InlineData("void Outer(int [|m|]) {}", "void Outer(int M) {}")]
    [InlineData("void Outer() { int [|m|]; }", "void Outer() { int M; }")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20907")]
    public async Task TestPascalCaseSymbol_NoneAndDefaultSymbolKinds(string camelCaseSymbol, string pascalCaseSymbol)
    {
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{camelCaseSymbol}}
            }
            """, new TestParameters(options: s_options.SymbolKindsArePascalCaseEmpty()));

        await TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{camelCaseSymbol}}
            }
            """,
            $$"""
            class C
            {
                {{pascalCaseSymbol}}
            }
            """, options: s_options.SymbolKindsArePascalCase(symbolKinds: default));
    }

    [Theory]
    [InlineData("} namespace [|c2|] {", "} namespace C2 {", SymbolKind.Namespace, Accessibility.Public)]
    [InlineData("class [|c2|] { }", "class C2 { }", TypeKind.Class, Accessibility.Private)]
    [InlineData("struct [|c2|] { }", "struct C2 { }", TypeKind.Struct, Accessibility.Private)]
    [InlineData("interface [|c2|] { }", "interface C2 { }", TypeKind.Interface, Accessibility.Private)]
    [InlineData("delegate void [|c2|]();", "delegate void C2();", TypeKind.Delegate, Accessibility.Private)]
    [InlineData("enum [|c2|] { }", "enum C2 { }", TypeKind.Enum, Accessibility.Private)]
    [InlineData("class M<[|t|]> {}", "class M<T> {}", SymbolKind.TypeParameter, Accessibility.Private)]
    [InlineData("void M<[|t|]>() {}", "void M<T>() {}", SymbolKind.TypeParameter, Accessibility.Private)]
    [InlineData("int [|m|] { get; }", "int M { get; }", SymbolKind.Property, Accessibility.Private)]
    [InlineData("void [|m|]() {}", "void M() {}", MethodKind.Ordinary, Accessibility.Private)]
    [InlineData("void Outer() { void [|m|]() {} }", "void Outer() { void M() {} }", MethodKind.LocalFunction, Accessibility.NotApplicable)]
    [InlineData("int [|m|];", "int M;", SymbolKind.Field, Accessibility.Private)]
    [InlineData("event System.EventHandler [|m|];", "event System.EventHandler M;", SymbolKind.Event, Accessibility.Private)]
    [InlineData("void Outer(int [|m|]) {}", "void Outer(int M) {}", SymbolKind.Parameter, Accessibility.Private)]
    [InlineData("void Outer() { void Inner(int [|m|]) {} }", "void Outer() { void Inner(int M) {} }", SymbolKind.Parameter, Accessibility.NotApplicable)]
    [InlineData("void Outer() { System.Action<int> action = [|m|] => {} }", "void Outer() { System.Action<int> action = M => {} }", SymbolKind.Parameter, Accessibility.NotApplicable)]
    [InlineData("void Outer() { System.Action<int> action = ([|m|]) => {} }", "void Outer() { System.Action<int> action = (M) => {} }", SymbolKind.Parameter, Accessibility.NotApplicable)]
    [InlineData("void Outer() { System.Action<int> action = (int [|m|]) => {} }", "void Outer() { System.Action<int> action = (int M) => {} }", SymbolKind.Parameter, Accessibility.NotApplicable)]
    [InlineData("void Outer() { System.Action<int> action = delegate (int [|m|]) {} }", "void Outer() { System.Action<int> action = delegate (int M) {} }", SymbolKind.Parameter, Accessibility.NotApplicable)]
    [InlineData("void Outer() { int [|m|]; }", "void Outer() { int M; }", SymbolKind.Local, Accessibility.NotApplicable)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20907")]
    public async Task TestPascalCaseSymbol_ExpectedSymbolAndAccessibility(string camelCaseSymbol, string pascalCaseSymbol, object symbolKind, Accessibility accessibility)
    {
        var alternateSymbolKind = TypeKind.Class.Equals(symbolKind) ? TypeKind.Interface : TypeKind.Class;
        var alternateAccessibility = accessibility == Accessibility.Public ? Accessibility.Protected : Accessibility.Public;

        // Verify that no diagnostic is reported if the symbol kind is wrong
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{camelCaseSymbol}}
            }
            """, new TestParameters(options: s_options.SymbolKindsArePascalCase(alternateSymbolKind)));

        // Verify that no diagnostic is reported if the accessibility is wrong
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{camelCaseSymbol}}
            }
            """, new TestParameters(options: s_options.AccessibilitiesArePascalCase(ImmutableArray.Create(alternateAccessibility))));

        await TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                {{camelCaseSymbol}}
            }
            """,
            $$"""
            class C
            {
                {{pascalCaseSymbol}}
            }
            """, options: s_options.AccessibilitiesArePascalCase(ImmutableArray.Create(accessibility)));
    }

    [Fact]
    public async Task TestPascalCaseMethod_NameGetsCapitalized()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void [|m|]()
                {
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                }
            }
            """,
            options: s_options.MethodNamesArePascalCase);
    }

    [Fact]
    public async Task TestPascalCaseMethod_ConstructorsAreIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class c
            {
                public [|c|]()
                {
                }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_PropertyAccessorsAreIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int P { [|get|]; set; }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_IndexerNameIsIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int [|this|][int index]
                {
                    get
                    {
                        return 1;
                    }
                }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_LocalFunctionIsIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    void [|f|]()
                    {
                    }
                }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestCamelCaseParameters()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void M(int [|X|])
                {
                }
            }
            """,
            """
            class C
            {
                public void M(int x)
                {
                }
            }
            """,
            options: s_options.ParameterNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_LocalDeclaration1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [|X|];
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x;
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_LocalDeclaration2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int X, [|Y|] = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int X, y = 0;
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_UsingVariable1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    using (object [|A|] = null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    using (object a = null)
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_UsingVariable2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    using (object A = null, [|B|] = null)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    using (object A = null, b = null)
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_ForVariable1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    for (int [|I|] = 0, J = 0; I < J; ++I)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    for (int i = 0, J = 0; i < J; ++i)
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_ForVariable2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    for (int I = 0, [|J|] = 0; I < J; ++J)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    for (int I = 0, j = 0; I < j; ++j)
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_ForEachVariable()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var [|X|] in new string[] { })
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    foreach (var x in new string[] { })
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_CatchVariable()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch (Exception [|Exception|])
                    {
                    }
                }
            }
            """,
            """
            using System;
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch (Exception exception)
                    {
                    }
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_CatchWithoutVariableIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch ([|Exception|])
                    {
                    }
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_CatchWithoutDeclarationIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    [|catch|]
                    {
                    }
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_Deconstruction1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    (int A, (string [|B|], var C)) = (0, (string.Empty, string.Empty));
                    System.Console.WriteLine(A + B + C);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    (int A, (string b, var C)) = (0, (string.Empty, string.Empty));
                    System.Console.WriteLine(A + b + C);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_Deconstruction2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var (A, (B, [|C|])) = (0, (string.Empty, string.Empty));
                    System.Console.WriteLine(A + B + C);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var (A, (B, [|c|])) = (0, (string.Empty, string.Empty));
                    System.Console.WriteLine(A + B + c);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_ForEachDeconstruction1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    foreach ((int A, (string [|B|], var C)) in new[] { (0, (string.Empty, string.Empty)) })
                        System.Console.WriteLine(A + B + C);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    foreach ((int A, (string b, var C)) in new[] { (0, (string.Empty, string.Empty)) })
                        System.Console.WriteLine(A + b + C);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_ForEachDeconstruction2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var (A, (B, [|C|])) in new[] { (0, (string.Empty, string.Empty)) })
                        System.Console.WriteLine(A + B + C);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    foreach (var (A, (B, c)) in new[] { (0, (string.Empty, string.Empty)) })
                        System.Console.WriteLine(A + B + c);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_OutVariable()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(string.Empty, out var [|Value|]))
                        System.Console.WriteLine(Value);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (int.TryParse(string.Empty, out var value))
                        System.Console.WriteLine(value);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_PatternVariable()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    if (new object() is int [|Value|])
                        System.Console.WriteLine(Value);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (new object() is int value)
                        System.Console.WriteLine(value);
                }
            }
            """,
            options: s_options.LocalNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocals_QueryFromClauseIgnored()
    {
        // This is an IRangeVariableSymbol, not ILocalSymbol
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                void M()
                {
                    var squares =
                        from [|STRING|] in new string[] { }
                        let Number = int.Parse(STRING)
                        select Number * Number;
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_QueryLetClauseIgnored()
    {
        // This is an IRangeVariableSymbol, not ILocalSymbol
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                void M()
                {
                    var squares =
                        from STRING in new string[] { }
                        let [|Number|] = int.Parse(STRING)
                        select Number * Number;
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_ParameterIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(int [|X|])
                {
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_TupleTypeElementNameIgnored1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    (int [|A|], string B) tuple;
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_TupleTypeElementNameIgnored2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    (int A, (string [|B|], string C)) tuple = (0, (string.Empty, string.Empty));
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestCamelCaseLocals_TupleExpressionElementNameIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var tuple = ([|A|]: 0, B: 0);
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact]
    public async Task TestUpperCaseConstants_ConstField()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                const int [|field|] = 0;
            }
            """,
            """
            class C
            {
                const int FIELD = 0;
            }
            """,
            options: s_options.ConstantsAreUpperCase);
    }

    [Fact]
    public async Task TestUpperCaseConstants_ConstLocal()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const int local1 = 0, [|local2|] = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const int local1 = 0, LOCAL2 = 0;
                }
            }
            """,
            options: s_options.ConstantsAreUpperCase);
    }

    [Fact]
    public async Task TestUpperCaseConstants_NonConstFieldIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                readonly int [|field|] = 0;
            }
            """, new TestParameters(options: s_options.ConstantsAreUpperCase));
    }

    [Fact]
    public async Task TestUpperCaseConstants_NonConstLocalIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int local1 = 0, [|local2|] = 0;
                }
            }
            """, new TestParameters(options: s_options.ConstantsAreUpperCase));
    }

    [Fact]
    public async Task TestCamelCaseLocalsUpperCaseConstants_ConstLocal()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const int [|PascalCase|] = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    const int PASCALCASE = 0;
                }
            }
            """,
            options: s_options.LocalsAreCamelCaseConstantsAreUpperCase);
    }

    [Fact]
    public async Task TestCamelCaseLocalsUpperCaseConstants_NonConstLocal()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [|PascalCase|] = 0;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int pascalCase = 0;
                }
            }
            """,
            options: s_options.LocalsAreCamelCaseConstantsAreUpperCase);
    }

    [Fact]
    public async Task TestCamelCaseLocalFunctions()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    void [|F|]()
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    void f()
                    {
                    }
                }
            }
            """,
            options: s_options.LocalFunctionNamesAreCamelCase);
    }

    [Fact]
    public async Task TestCamelCaseLocalFunctions_MethodIsIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void [|M|]()
                {
                }
            }
            """, new TestParameters(options: s_options.LocalFunctionNamesAreCamelCase));
    }

    [Fact]
    public async Task TestAsyncFunctions_AsyncMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                async void [|M|]()
                {
                }
            }
            """,
            """
            class C
            {
                async void MAsync()
                {
                }
            }
            """,
            options: s_options.AsyncFunctionNamesEndWithAsync);
    }

    [Fact]
    public async Task TestAsyncFunctions_AsyncLocalFunction()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    async void [|F|]()
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    async void FAsync()
                    {
                    }
                }
            }
            """,
            options: s_options.AsyncFunctionNamesEndWithAsync);
    }

    [Fact]
    public async Task TestAsyncFunctions_NonAsyncMethodIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void [|M|]()
                {
                    async void F()
                    {
                    }
                }
            }
            """, new TestParameters(options: s_options.AsyncFunctionNamesEndWithAsync));
    }

    [Fact]
    public async Task TestAsyncFunctions_NonAsyncLocalFunctionIgnored()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                async void M()
                {
                    void [|F|]()
                    {
                    }
                }
            }
            """, new TestParameters(options: s_options.AsyncFunctionNamesEndWithAsync));
    }

    [Fact]
    public async Task TestPascalCaseMethod_InInterfaceWithImplicitImplementation()
    {
        await TestInRegularAndScriptAsync(
            """
            interface I
            {
                void [|m|]();
            }

            class C : I
            {
                public void m() { }
            }
            """,
            """
            interface I
            {
                void M();
            }

            class C : I
            {
                public void M() { }
            }
            """,
            options: s_options.MethodNamesArePascalCase);
    }

    [Fact]
    public async Task TestPascalCaseMethod_InInterfaceWithExplicitImplementation()
    {
        await TestInRegularAndScriptAsync(
            """
            interface I
            {
                void [|m|]();
            }

            class C : I
            {
                void I.m() { }
            }
            """,
            """
            interface I
            {
                void M();
            }

            class C : I
            {
                void I.M() { }
            }
            """,
            options: s_options.MethodNamesArePascalCase);
    }

    [Fact]
    public async Task TestPascalCaseMethod_NotInImplicitInterfaceImplementation()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            interface I
            {
                void m();
            }

            class C : I
            {
                public void [|m|]() { }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_NotInExplicitInterfaceImplementation()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            interface I
            {
                void m();
            }

            class C : I
            {
                void I.[|m|]() { }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_InAbstractType()
    {
        await TestInRegularAndScriptAsync(
            """
            abstract class C
            {
                public abstract void [|m|]();
            }

            class D : C
            {
                public override void m() { }
            }
            """,
            """
            abstract class C
            {
                public abstract void M();
            }

            class D : C
            {
                public override void M() { }
            }
            """,
            options: s_options.MethodNamesArePascalCase);
    }

    [Fact]
    public async Task TestPascalCaseMethod_NotInAbstractMethodImplementation()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            abstract class C
            {
                public abstract void m();
            }

            class D : C
            {
                public override void [|m|]() { }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseProperty_InInterface()
    {
        await TestInRegularAndScriptAsync(
            """
            interface I
            {
                int [|p|] { get; set; }
            }

            class C : I
            {
                public int p { get { return 1; } set { } }
            }
            """,
            """
            interface I
            {
                int P { get; set; }
            }

            class C : I
            {
                public int P { get { return 1; } set { } }
            }
            """,
            options: s_options.PropertyNamesArePascalCase);
    }

    [Fact]
    public async Task TestPascalCaseProperty_NotInImplicitInterfaceImplementation()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            interface I
            {
                int p { get; set; }
            }

            class C : I
            {
                public int [|p|] { get { return 1; } set { } }
            }
            """, new TestParameters(options: s_options.PropertyNamesArePascalCase));
    }

    [Fact]
    public async Task TestPascalCaseMethod_OverrideInternalMethod()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            abstract class C
            {
                internal abstract void m();
            }

            class D : C
            {
                internal override void [|m|]() { }
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19106")]
    public async Task TestMissingOnSymbolsWithNoName()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            namespace Microsoft.CodeAnalysis.Host
            {
                internal interface 
            [|}|]
            """, new TestParameters(options: s_options.InterfaceNamesStartWithI));
    }

#if CODE_STYLE
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/42218")]
#else
    [Fact]
#endif
    [WorkItem("https://github.com/dotnet/roslyn/issues/16562")]
    public async Task TestRefactorNotify()
    {
        var markup = @"public class [|c|] { }";
        var testParameters = new TestParameters(options: s_options.ClassNamesArePascalCase);

        using var workspace = CreateWorkspaceFromOptions(markup, testParameters);
        var (_, action) = await GetCodeActionsAsync(workspace, testParameters);

        var previewOperations = await action.GetPreviewOperationsAsync(CancellationToken.None);
        Assert.Empty(previewOperations.OfType<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>());

        var commitOperations = await action.GetOperationsAsync(CancellationToken.None);
        Assert.Equal(2, commitOperations.Length);

        var symbolRenamedOperation = (TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation)commitOperations[1];
        Assert.Equal("c", symbolRenamedOperation._symbol.Name);
        Assert.Equal("C", symbolRenamedOperation._newName);
    }

#if CODE_STYLE
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/42218")]
#else
    [Fact]
#endif
    [WorkItem("https://github.com/dotnet/roslyn/issues/38513")]
    public async Task TestRefactorNotifyInterfaceNamesStartWithI()
    {
        var markup = @"public interface [|test|] { }";
        var testParameters = new TestParameters(options: s_options.InterfaceNamesStartWithI);

        using var workspace = CreateWorkspaceFromOptions(markup, testParameters);
        var (_, action) = await GetCodeActionsAsync(workspace, testParameters);

        var previewOperations = await action.GetPreviewOperationsAsync(CancellationToken.None);
        Assert.Empty(previewOperations.OfType<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>());

        var commitOperations = await action.GetOperationsAsync(CancellationToken.None);
        Assert.Equal(2, commitOperations.Length);

        var symbolRenamedOperation = (TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation)commitOperations[1];
        Assert.Equal("test", symbolRenamedOperation._symbol.Name);
        Assert.Equal("ITest", symbolRenamedOperation._newName);
    }

#if CODE_STYLE
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/42218")]
#else
    [Fact]
#endif
    [WorkItem("https://github.com/dotnet/roslyn/issues/38513")]
    public async Task TestRefactorNotifyTypeParameterNamesStartWithT()
    {
        var markup = """
            public class A
            {
                void DoOtherThing<[|arg|]>() { }
            }
            """;
        var testParameters = new TestParameters(options: s_options.TypeParameterNamesStartWithT);

        using var workspace = CreateWorkspaceFromOptions(markup, testParameters);
        var (_, action) = await GetCodeActionsAsync(workspace, testParameters);

        var previewOperations = await action.GetPreviewOperationsAsync(CancellationToken.None);
        Assert.Empty(previewOperations.OfType<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>());

        var commitOperations = await action.GetOperationsAsync(CancellationToken.None);
        Assert.Equal(2, commitOperations.Length);

        var symbolRenamedOperation = (TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation)commitOperations[1];
        Assert.Equal("arg", symbolRenamedOperation._symbol.Name);
        Assert.Equal("TArg", symbolRenamedOperation._newName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47508")]
    public async Task TestRecordParameter_NoDiagnosticWhenCorrect()
    {
        await TestMissingInRegularAndScriptAsync(
@"record Foo(int [|MyInt|]);",
            new TestParameters(options: s_options.MergeStyles(s_options.PropertyNamesArePascalCase, s_options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47508")]
    public async Task TestRecordConstructorParameter_NoDiagnosticWhenCorrect()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            record Foo(int MyInt)
            {
                public Foo(string [|p_myString|]) : this(1)
                {
                }
            }
            """,
            new TestParameters(options: s_options.MergeStyles(s_options.PropertyNamesArePascalCase, s_options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47508")]
    public async Task TestRecordParameter_ParameterFormattedAsProperties()
    {
        await TestInRegularAndScriptAsync(
@"public record Foo(int [|myInt|]);",
@"public record Foo(int [|MyInt|]);",
            options: s_options.MergeStyles(s_options.PropertyNamesArePascalCase, s_options.ParameterNamesAreCamelCaseWithPUnderscorePrefix));
    }

    [Theory]
    [InlineData("_")]
    [InlineData("_1")]
    [InlineData("_123")]
    [InlineData("__")]
    [InlineData("___")]
    public async Task TestDiscardParameterAsync(string identifier)
    {
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                void M(int [|{{identifier}}|])
                {
                }
            }
            """, new TestParameters(options: s_options.ParameterNamesAreCamelCase));
    }

    [Theory]
    [InlineData("_")]
    [InlineData("_1")]
    [InlineData("_123")]
    [InlineData("__")]
    [InlineData("___")]
    public async Task TestDiscardLocalAsync(string identifier)
    {
        await TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                void M()
                {
                    int [|{{identifier}}|] = 0;
                }
            }
            """, new TestParameters(options: s_options.LocalNamesAreCamelCase));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49535")]
    public async Task TestGlobalDirectiveAsync()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            interface I
            {
                int X { get; }
            }

            class C : I
            {
                int [|global::I.X|] => 0;
            }
            """, new TestParameters(options: s_options.PropertyNamesArePascalCase));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50734")]
    public async Task TestAsyncEntryPoint()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class C
            {
                static async Task [|Main|]()
                {
                    await Task.Delay(0);
                }
            }
            """, new TestParameters(options: s_options.AsyncFunctionNamesEndWithAsync));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49648")]
    public async Task TestAsyncEntryPoint_TopLevel()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            [|await Task.Delay(0);|]
            """, new TestParameters(options: s_options.AsyncFunctionNamesEndWithAsync));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51727")]
    public async Task TestExternAsync()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static extern void [|some_p_invoke()|];
            }
            """, new TestParameters(options: s_options.MethodNamesArePascalCase));
    }
}
