// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructors;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateConstructors;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
public sealed class GenerateConstructorsTests : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpGenerateConstructorsCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

    private readonly NamingStylesTestOptionSets options = new(LanguageNames.CSharp);

    [Fact]
    public Task TestSingleField()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact]
    public Task TestSingleFieldWithCodeStyle()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|} => this.a = a;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|} => this.a = a;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                int b;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                int b;

                public Z(int a, int b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestMultipleFields()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                string b;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestMultipleFields_VerticalSelection()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {[|
                int a;
                string b;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestMultipleFields_VerticalSelectionUpToExcludedField()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;[|
                string b;
                string c;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                string c;

                public Z(string b, string c{|Navigation:)|}
                {
                    this.b = b;
                    this.c = c;
                }
            }
            """);

    [Fact]
    public Task TestMultipleFields_VerticalSelectionUpToMethod()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                void Goo() { }[|
                int a;
                string b;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                void Goo() { }
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestMultipleFields_SelectionIncludingClassOpeningBrace()
        => TestMissingAsync(
            """
            using System.Collections.Generic;

            class Z
            [|{
                int a;
                string b;|]
            }
            """);

    [Fact]
    public Task TestSecondField()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                [|string b;|]

                public Z(int a)
                {
                    this.a = a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a)
                {
                    this.a = a;
                }

                public Z(string b{|Navigation:)|}
                {
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestFieldAssigningConstructor()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                string b;|]

                public Z(int a)
                {
                    this.a = a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a)
                {
                    this.a = a;
                }

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestFieldAssigningConstructor2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                string b;|]

                public Z(int a)
                {
                    this.a = a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a)
                {
                    this.a = a;
                }

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestDelegatingConstructor()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                string b;|]

                public Z(int a)
                {
                    this.a = a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a)
                {
                    this.a = a;
                }

                public Z(int a, string b{|Navigation:)|} : this(a)
                {
                    this.b = b;
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestDelegatingConstructorWithNullabilityDifferences()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System.Collections.Generic;

            class Z
            {
                [|string? a;
                int b;|]

                public Z(string a)
                {
                    this.a = a;
                }
            }
            """,
            """
            #nullable enable

            using System.Collections.Generic;

            class Z
            {
                string? a;
                int b;

                public Z(string a)
                {
                    this.a = a;
                }

                public Z(string? a, int b{|Navigation:)|} : this(a)
                {
                    this.b = b;
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestMissingWithExistingConstructor()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;
                string b;|]

                public Z(int a)
                {
                    this.a = a;
                }

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact]
    public Task TestMultipleProperties()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                [|public int A { get; private set; }
                public string B { get; private set; }|]
            }
            """,
            """
            class Z
            {
                public Z(int a, string b{|Navigation:)|}
                {
                    A = a;
                    B = b;
                }

                public int A { get; private set; }
                public string B { get; private set; }
            }
            """);

    [Fact]
    public Task TestMultiplePropertiesWithQualification()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                [|public int A { get; private set; }
                public string B { get; private set; }|]
            }
            """,
            """
            class Z
            {
                public Z(int a, string b{|Navigation:)|}
                {
                    this.A = a;
                    this.B = b;
                }

                public int A { get; private set; }
                public string B { get; private set; }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.QualifyPropertyAccess, true, NotificationOption2.Error)));

    [Fact]
    public Task TestStruct()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            struct S
            {
                [|int i;|]
            }
            """,
            """
            using System.Collections.Generic;

            struct S
            {
                int i;

                public S(int i{|Navigation:)|}
                {
                    this.i = i;
                }
            }
            """);

    [Fact]
    public Task TestStructInitializingAutoProperty()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            struct S
            {
                [|int i { get; set; }|]
            }
            """,
            """
            using System.Collections.Generic;

            struct S
            {
                public S(int i{|Navigation:)|}
                {
                    this.i = i;
                }

                int i { get; set; }
            }
            """);

    [Fact]
    public Task TestStructNotInitializingAutoProperty()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            struct S
            {
                [|int i { get => f; set => f = value; }|]
                int j { get; set; }
            }
            """,
            """
            using System.Collections.Generic;

            struct S
            {
                public S(int i{|Navigation:)|} : this()
                {
                    this.i = i;
                }

                int i { get => f; set => f = value; }
                int j { get; set; }
            }
            """);

    [Fact]
    public Task TestStruct2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            struct S
            {
                int i { get; set; }

                [|int y;|]
            }
            """,
            """
            using System.Collections.Generic;

            struct S
            {
                int i { get; set; }

                int y;

                public S(int y{|Navigation:)|} : this()
                {
                    this.y = y;
                }
            }
            """);

    [Fact]
    public Task TestStruct3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            struct S
            {
                [|int i { get; set; }|]

                int y;
            }
            """,
            """
            using System.Collections.Generic;

            struct S
            {
                int i { get; set; }

                int y;

                public S(int i{|Navigation:)|} : this()
                {
                    this.i = i;
                }
            }
            """);

    [Fact]
    public Task TestGenericType()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program<T>
            {
                [|int i;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Program<T>
            {
                int i;

                public Program(int i{|Navigation:)|}
                {
                    this.i = i;
                }
            }
            """);

    [Fact]
    public Task TestSmartTagText1()
        => TestSmartTagTextAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                [|bool b;
                HashSet<string> s;|]
            }
            """,
            string.Format(CodeFixesResources.Generate_constructor_0_1, "Program", "bool b, HashSet<string> s"));

    [Fact]
    public Task TestSmartTagText2()
        => TestSmartTagTextAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                [|bool b;
                HashSet<string> s;|]

                public Program(bool b)
                {
                    this.b = b;
                }
            }
            """,
            string.Format(CodeFixesResources.Generate_field_assigning_constructor_0_1, "Program", "bool b, HashSet<string> s"));

    [Fact]
    public Task TestSmartTagText3()
        => TestSmartTagTextAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                [|bool b;
                HashSet<string> s;|]

                public Program(bool b)
                {
                    this.b = b;
                }
            }
            """,
            string.Format(FeaturesResources.Generate_delegating_constructor_0_1, "Program", "bool b, HashSet<string> s"),
            index: 1);

    [Fact]
    public Task TestContextualKeywordName()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|int yield;|]
            }
            """,
            """
            class Program
            {
                int yield;

                public Program(int yield{|Navigation:)|}
                {
                    this.yield = yield;
                }
            }
            """);

    [Fact]
    public Task TestGenerateConstructorNotOfferedForDuplicate()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class X
            {
                public X(string v)
                {
                }

                static void Test()
                {
                    new X(new [|string|]());
                }
            }
            """);

    [Fact]
    public Task Tuple()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|(int, string) a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                (int, string) a;

                public Z((int, string) a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact]
    public Task NullableReferenceType()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class Z
            {
                [|string? a;|]
            }
            """,
            """
            #nullable enable

            class Z
            {
                string? a;

                public Z(string? a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14219")]
    public Task TestUnderscoreInName1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|int _field;|]
            }
            """,
            """
            class Program
            {
                int _field;

                public Program(int field{|Navigation:)|}
                {
                    _field = field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62162")]
    public Task TestUnderscoreInName_KeepIfNameWithoutUnderscoreIsInvalid()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|int _0;|]
            }
            """,
            """
            class Program
            {
                int _0;

                public Program(int _0{|Navigation:)|}
                {
                    this._0 = _0;
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/62162")]
    [InlineData('m')]
    [InlineData('s')]
    [InlineData('t')]
    public Task TestCommonPatternInName_KeepUnderscoreIfNameWithoutItIsInvalid(char commonPatternChar)
        => TestInRegularAndScriptAsync(
            $$"""
            class Program
            {
                [|int {{commonPatternChar}}_0;|]
            }
            """,
            $$"""
            class Program
            {
                int {{commonPatternChar}}_0;

                public Program(int _0{|Navigation:)|}
                {
                    {{commonPatternChar}}_0 = _0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14219")]
    public Task TestUnderscoreInName_PreferThis()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|int _field;|]
            }
            """,
            """
            class Program
            {
                int _field;

                public Program(int field{|Navigation:)|}
                {
                    this._field = field;
                }
            }
            """,
            new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13944")]
    public Task TestGetter_Only_Auto_Props()
        => TestInRegularAndScriptAsync(
            """
            abstract class Contribution
            {
              [|public string Title { get; }
                public int Number { get; }|]
            }
            """,
            """
            abstract class Contribution
            {
                protected Contribution(string title, int number{|Navigation:)|}
                {
                    Title = title;
                    Number = number;
                }

                public string Title { get; }
                public int Number { get; }
            }
            """,
            new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13944")]
    public Task TestAbstract_Getter_Only_Auto_Props()
        => TestMissingInRegularAndScriptAsync(
            """
            abstract class Contribution
            {
              [|public abstract string Title { get; }
                public int Number { get; }|]
            }
            """,
            new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact]
    public Task TestSingleFieldWithDialog()
        => TestWithPickMembersDialogAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                [||]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """,
            chosenSymbols: ["a"]);

    [Fact]
    public Task TestSingleFieldWithDialog2()
        => TestWithPickMembersDialogAsync(
            """
            using System.Collections.Generic;

            class [||]Z
            {
                int a;
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """,
            chosenSymbols: ["a"]);

    [Fact]
    public Task TestMissingOnClassAttributes()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            [X][||]
            class Z
            {
                int a;
            }
            """);

    [Fact]
    public Task TestPickNoFieldWithDialog()
        => TestWithPickMembersDialogAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                [||]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z({|Navigation:)|}
                {
                }
            }
            """,
            chosenSymbols: []);

    [Fact]
    public Task TestReorderFieldsWithDialog()
        => TestWithPickMembersDialogAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                [||]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(string b, int a{|Navigation:)|}
                {
                    this.b = b;
                    this.a = a;
                }
            }
            """,
            chosenSymbols: ["b", "a"]);

    [Fact]
    public Task TestAddNullChecks1()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b ?? throw new ArgumentNullException(nameof(b));
                }
            }
            """,
            chosenSymbols: ["a", "b"],
            optionsCallback: options => options[0].Value = true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41428")]
    public Task TestAddNullChecksWithNullableReferenceType()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;
            #nullable enable

            class Z
            {
                int a;
                string b;
                string? c;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            #nullable enable

            class Z
            {
                int a;
                string b;
                string? c;

                public Z(int a, string b, string? c{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b ?? throw new ArgumentNullException(nameof(b));
                    this.c = c;
                }
            }
            """,
            chosenSymbols: ["a", "b", "c"],
            optionsCallback: options => options[0].Value = true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41428")]
    public Task TestAddNullChecksWithNullableReferenceTypeForGenerics()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;
            #nullable enable

            class Z<T> where T : class
            {
                int a;
                string b;
                T? c;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            #nullable enable

            class Z<T> where T : class
            {
                int a;
                string b;
                T? c;

                public Z(int a, string b, T? c{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b ?? throw new ArgumentNullException(nameof(b));
                    this.c = c;
                }
            }
            """,
            chosenSymbols: ["a", "b", "c"],
            optionsCallback: options => options[0].Value = true);

    [Fact]
    public Task TestAddNullChecks2()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    if (b is null)
                    {
                        throw new ArgumentNullException(nameof(b));
                    }

                    this.a = a;
                    this.b = b;
                }
            }
            """,
            chosenSymbols: ["a", "b"],
            optionsCallback: options => options[0].Value = true,
            parameters: new TestParameters(options:
Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));

    [Fact]
    public Task TestAddNullChecks3()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                int? b;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                int? b;

                public Z(int a, int? b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """,
            chosenSymbols: ["a", "b"],
            optionsCallback: options => options[0].Value = true,
            parameters: new TestParameters(options:
                Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));

    [Fact]
    public Task TestAddNullChecks_CSharp6()
        => TestWithPickMembersDialogAsync(
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                [||]
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;

                public Z(int a, string b{|Navigation:)|}
                {
                    if (b == null)
                    {
                        throw new ArgumentNullException(nameof(b));
                    }

                    this.a = a;
                    this.b = b;
                }
            }
            """,
            chosenSymbols: ["a", "b"],
            optionsCallback: options => options[0].Value = true,
            parameters: new TestParameters(
                parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6),
                options: Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));

    [Fact]
    public Task TestMissingOnMember1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                [||]public void M() { }
            }
            """);

    [Fact]
    public Task TestMissingOnMember2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                public void M()
                {
                }[||]

                public void N() { }
            }
            """);

    [Fact]
    public Task TestMissingOnMember3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                int a;
                string b;
                public void M()
                {
             [||] 
                }

                public void N() { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/21067")]
    public Task TestFinalCaretPosition()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Z
            {
                [|int a;|]
            }
            """,
            """
            using System.Collections.Generic;

            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20595")]
    public Task ProtectedConstructorShouldBeGeneratedForAbstractClass()
        => TestInRegularAndScriptAsync(
            """
            abstract class C 
            {
                [|public int Prop { get; set; }|]
            }
            """,
            """
            abstract class C 
            {
                protected C(int prop{|Navigation:)|}
                {
                    Prop = prop;
                }

                public int Prop { get; set; }
            }
            """,
            new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17643")]
    public Task TestWithDialogNoBackingField()
        => TestWithPickMembersDialogAsync(
            """
            class Program
            {
                public int F { get; set; }
                [||]
            }
            """,
            """
            class Program
            {
                public int F { get; set; }

                public Program(int f{|Navigation:)|}
                {
                    F = f;
                }
            }
            """,
            chosenSymbols: null);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25690")]
    public Task TestWithDialogNoIndexer()
        => TestWithPickMembersDialogAsync(
            """
            class Program
            {
                public int P { get => 0; set { } }
                public int this[int index] { get => 0; set { } }
                [||]
            }
            """,
            """
            class Program
            {
                public int P { get => 0; set { } }
                public int this[int index] { get => 0; set { } }

                public Program(int p{|Navigation:)|}
                {
                    P = p;
                }
            }
            """,
            chosenSymbols: null);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
    public Task TestWithDialogSetterOnlyProperty()
        => TestWithPickMembersDialogAsync(
            """
            class Program
            {
                public int P { get => 0; set { } }
                public int S { set { } }
                [||]
            }
            """,
            """
            class Program
            {
                public int P { get => 0; set { } }
                public int S { set { } }

                public Program(int p, int s{|Navigation:)|}
                {
                    P = p;
                    S = s;
                }
            }
            """,
            chosenSymbols: null);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelection()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a|];
            }
            """,
            """
            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelection2()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a|]bcdefg;
            }
            """,
            """
            class Z
            {
                int abcdefg;

                public Z(int abcdefg{|Navigation:)|}
                {
                    this.abcdefg = abcdefg;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelection3()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int abcdef[|g|];
            }
            """,
            """
            class Z
            {
                int abcdefg;

                public Z(int abcdefg{|Navigation:)|}
                {
                    this.abcdefg = abcdefg;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelectionBeforeIdentifier()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [||]a;
            }
            """,
            """
            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelectionAfterIdentifier()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int a[||];
            }
            """,
            """
            class Z
            {
                int a;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelectionIdentifierNotSelected()
        => TestMissingAsync(
            """
            class Z
            {
                in[|t|] a;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestPartialFieldSelectionIdentifierNotSelected2()
        => TestMissingAsync(
            """
            class Z
            {
                int a [|= 3|];
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestMultiplePartialFieldSelection()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a;
                int b|];
            }
            """,
            """
            class Z
            {
                int a;
                int b;

                public Z(int a, int b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestMultiplePartialFieldSelection2()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a = 2;
                int|] b;
            }
            """,
            """
            class Z
            {
                int a = 2;
                int b;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestMultiplePartialFieldSelection3_1()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a|] = 2, b = 3;
            }
            """,
            """
            class Z
            {
                int a = 2, b = 3;

                public Z(int a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestMultiplePartialFieldSelection3_2()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a = 2, b|] = 3;
            }
            """,
            """
            class Z
            {
                int a = 2, b = 3;

                public Z(int a, int b{|Navigation:)|}
                {
                    this.a = a;
                    this.b = b;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
    public Task TestMultiplePartialFieldSelection4()
        => TestMissingAsync(
            """
            class Z
            {
                int a = [|2|], b = 3;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestNoFieldNamingStyle()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|a|] = 2;
            }
            """,
            """
            class Z
            {
                int a = 2;

                public Z(int p_a{|Navigation:)|}
                {
                    a = p_a;
                }
            }
            """, new TestParameters(options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestCommonFieldNamingStyle()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|s_a|] = 2;
            }
            """,
            """
            class Z
            {
                int s_a = 2;

                public Z(int p_a{|Navigation:)|}
                {
                    s_a = p_a;
                }
            }
            """, new TestParameters(options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestSpecifiedNamingStyle()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|field_a|] = 2;
            }
            """,
            """
            class Z
            {
                int field_a = 2;

                public Z(int p_a_End{|Navigation:)|}
                {
                    field_a = p_a_End;
                }
            }
            """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestSpecifiedAndCommonFieldNamingStyle()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|field_s_a|] = 2;
            }
            """,
            """
            class Z
            {
                int field_s_a = 2;

                public Z(int p_a_End{|Navigation:)|}
                {
                    field_s_a = p_a_End;
                }
            }
            """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestSpecifiedAndCommonFieldNamingStyle2()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|s_field_a|] = 2;
            }
            """,
            """
            class Z
            {
                int s_field_a = 2;

                public Z(int p_a_End{|Navigation:)|}
                {
                    s_field_a = p_a_End;
                }
            }
            """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestBaseNameEmpty()
        => TestMissingAsync(
            """
            class Z
            {
                int [|field__End|] = 2;
            }
            """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
    public Task TestSomeBaseNamesEmpty()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                int [|s_field_a = 2;
                int field__End |]= 3;
            }
            """,
            """
            class Z
            {
                int s_field_a = 2;
                int field__End = 3;

                public Z(int p_a{|Navigation:)|}
                {
                    s_field_a = p_a;
                }
            }
            """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45808")]
    public Task TestUnsafeField()
        => TestInRegularAndScriptAsync(
            """
            class Z
            {
                [|unsafe int* a;|]
            }
            """,
            """
            class Z
            {
                unsafe int* a;

                public unsafe Z(int* a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """, new TestParameters(compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45808")]
    public Task TestUnsafeFieldInUnsafeClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Z
            {
                [|int* a;|]
            }
            """,
            """
            unsafe class Z
            {
                int* a;

                public Z(int* a{|Navigation:)|}
                {
                    this.a = a;
                }
            }
            """, new TestParameters(compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53467")]
    public Task TestMissingWhenTypeNotInCompilation()
        => TestMissingAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1">
                    <Document>
            using System;
            using System.Collections.Generic;
            #nullable enable

            <![CDATA[ class Z<T> where T : class ]]>
            {
                int a;
                string b;
                T? c;
                [||]
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private int _value;

                [|public int Goo
                {
                    get { return _value; }
                }|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int value{|Navigation:)|}
                {
                    _value = value;
                }

                public int Goo
                {
                    get { return _value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private int _value;

                [|public int Goo
                {
                    get { return this._value; }
                }|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int value{|Navigation:)|}
                {
                    _value = value;
                }

                public int Goo
                {
                    get { return this._value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField3()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private int _value;

                [|public int Goo
                {
                    get => this._value;
                }|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int value{|Navigation:)|}
                {
                    _value = value;
                }

                public int Goo
                {
                    get => this._value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField4()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private int _value;

                [|public int Goo => this._value;|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int value{|Navigation:)|}
                {
                    _value = value;
                }

                public int Goo => this._value;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField5()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|private int _value;

                public int Goo
                {
                    get { return _value; }
                }|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int value{|Navigation:)|}
                {
                    _value = value;
                }

                public int Goo
                {
                    get { return _value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
    public Task TestWithSelectedGetPropertyThatReturnsField6()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private int _value;

                [|public int Goo
                {
                    get { return _value; }
                    set { _value = value; }
                }|]
            }
            """,
            """
            class Program
            {
                private int _value;

                public Program(int goo{|Navigation:)|}
                {
                    Goo = goo;
                }

                public int Goo
                {
                    get { return _value; }
                    set { _value = value; }
                }
            }
            """);
}
