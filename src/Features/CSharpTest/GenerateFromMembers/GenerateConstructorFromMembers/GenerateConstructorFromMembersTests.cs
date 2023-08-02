// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateConstructorFromMembers
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
    public sealed class GenerateConstructorFromMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpGenerateConstructorFromMembersCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

        private readonly NamingStylesTestOptionSets options = new(LanguageNames.CSharp);

        [Fact]
        public async Task TestSingleField()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestSingleFieldWithCodeStyle()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [Fact]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact]
        public async Task TestMultipleFields()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultipleFields_VerticalSelection()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultipleFields_VerticalSelectionUpToExcludedField()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultipleFields_VerticalSelectionUpToMethod()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultipleFields_SelectionIncludingClassOpeningBrace()
        {
            await TestMissingAsync(
                """
                using System.Collections.Generic;

                class Z
                [|{
                    int a;
                    string b;|]
                }
                """);
        }

        [Fact]
        public async Task TestSecondField()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestFieldAssigningConstructor()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestFieldAssigningConstructor2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestDelegatingConstructor()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestDelegatingConstructorWithNullabilityDifferences()
        {
            // For this test we have a problem: the existing constructor has different nullability than
            // the underlying field. We will still offer to use the delegating constructor even though it has a nullability issue
            // the user can then easily fix. If they don't want that, they can also just use the first option.
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingWithExistingConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultipleProperties()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMultiplePropertiesWithQualification()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: Option(CodeStyleOptions2.QualifyPropertyAccess, true, NotificationOption2.Error));
        }

        [Fact]
        public async Task TestStruct()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestStructInitializingAutoProperty()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestStructNotInitializingAutoProperty()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestStruct2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestStruct3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestGenericType()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestSmartTagText1()
        {
            await TestSmartTagTextAsync(
                """
                using System.Collections.Generic;

                class Program
                {
                    [|bool b;
                    HashSet<string> s;|]
                }
                """,
string.Format(FeaturesResources.Generate_constructor_0_1, "Program", "bool b, HashSet<string> s"));
        }

        [Fact]
        public async Task TestSmartTagText2()
        {
            await TestSmartTagTextAsync(
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
string.Format(FeaturesResources.Generate_field_assigning_constructor_0_1, "Program", "bool b, HashSet<string> s"));
        }

        [Fact]
        public async Task TestSmartTagText3()
        {
            await TestSmartTagTextAsync(
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
        }

        [Fact]
        public async Task TestContextualKeywordName()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestGenerateConstructorNotOfferedForDuplicate()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task Tuple()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NullableReferenceType()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14219")]
        public async Task TestUnderscoreInName1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62162")]
        public async Task TestUnderscoreInName_KeepIfNameWithoutUnderscoreIsInvalid()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/62162")]
        [InlineData('m')]
        [InlineData('s')]
        [InlineData('t')]
        public async Task TestCommonPatternInName_KeepUnderscoreIfNameWithoutItIsInvalid(char commonPatternChar)
        {
            await TestInRegularAndScriptAsync(
$@"class Program
{{
    [|int {commonPatternChar}_0;|]
}}",
$@"class Program
{{
    int {commonPatternChar}_0;

    public Program(int _0{{|Navigation:)|}}
    {{
        {commonPatternChar}_0 = _0;
    }}
}}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14219")]
        public async Task TestUnderscoreInName_PreferThis()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13944")]
        public async Task TestGetter_Only_Auto_Props()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13944")]
        public async Task TestAbstract_Getter_Only_Auto_Props()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                abstract class Contribution
                {
                  [|public abstract string Title { get; }
                    public int Number { get; }|]
                }
                """,
new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement)));
        }

        [Fact]
        public async Task TestSingleFieldWithDialog()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new[] { "a" });
        }

        [Fact]
        public async Task TestSingleFieldWithDialog2()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new[] { "a" });
        }

        [Fact]
        public async Task TestMissingOnClassAttributes()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System.Collections.Generic;

                [X][||]
                class Z
                {
                    int a;
                }
                """);
        }

        [Fact]
        public async Task TestPickNoFieldWithDialog()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { });
        }

        [Fact]
        public async Task TestReorderFieldsWithDialog()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "b", "a" });
        }

        [Fact]
        public async Task TestAddNullChecks1()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41428")]
        public async Task TestAddNullChecksWithNullableReferenceType()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b", "c" },
optionsCallback: options => options[0].Value = true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41428")]
        public async Task TestAddNullChecksWithNullableReferenceTypeForGenerics()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b", "c" },
optionsCallback: options => options[0].Value = true);
        }

        [Fact]
        public async Task TestAddNullChecks2()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(options:
    Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));
        }

        [Fact]
        public async Task TestAddNullChecks3()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(options:
    Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));
        }

        [Fact]
        public async Task TestAddNullChecks_CSharp6()
        {
            await TestWithPickMembersDialogAsync(
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
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6),
    options: Option(CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.FalseWithSilentEnforcement)));
        }

        [Fact]
        public async Task TestMissingOnMember1()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System.Collections.Generic;

                class Z
                {
                    int a;
                    string b;
                    [||]public void M() { }
                }
                """);
        }

        [Fact]
        public async Task TestMissingOnMember2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnMember3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/21067")]
        public async Task TestFinalCaretPosition()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20595")]
        public async Task ProtectedConstructorShouldBeGeneratedForAbstractClass()
        {
            await TestInRegularAndScriptAsync(
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
options: Option(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOption2.TrueWithSuggestionEnforcement));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17643")]
        public async Task TestWithDialogNoBackingField()
        {
            await TestWithPickMembersDialogAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25690")]
        public async Task TestWithDialogNoIndexer()
        {
            await TestWithPickMembersDialogAsync(
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogSetterOnlyProperty()
        {
            await TestWithPickMembersDialogAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelection()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelection2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelection3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelectionBeforeIdentifier()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelectionAfterIdentifier()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelectionIdentifierNotSelected()
        {
            await TestMissingAsync(
                """
                class Z
                {
                    in[|t|] a;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestPartialFieldSelectionIdentifierNotSelected2()
        {
            await TestMissingAsync(
                """
                class Z
                {
                    int a [|= 3|];
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestMultiplePartialFieldSelection()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestMultiplePartialFieldSelection2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestMultiplePartialFieldSelection3_1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestMultiplePartialFieldSelection3_2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")]
        public async Task TestMultiplePartialFieldSelection4()
        {
            await TestMissingAsync(
                """
                class Z
                {
                    int a = [|2|], b = 3;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestNoFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestCommonFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestSpecifiedNamingStyle()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestSpecifiedAndCommonFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestSpecifiedAndCommonFieldNamingStyle2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestBaseNameEmpty()
        {
            await TestMissingAsync(
                """
                class Z
                {
                    int [|field__End|] = 2;
                }
                """, new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36741")]
        public async Task TestSomeBaseNamesEmpty()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45808")]
        public async Task TestUnsafeField()
        {
            await TestInRegularAndScriptAsync(
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
                """, compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45808")]
        public async Task TestUnsafeFieldInUnsafeClass()
        {
            await TestInRegularAndScriptAsync(
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
                """, compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53467")]
        public async Task TestMissingWhenTypeNotInCompilation()
        {
            await TestMissingAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField4()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField5()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29198")]
        public async Task TestWithSelectedGetPropertyThatReturnsField6()
        {
            await TestInRegularAndScriptAsync(
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
    }
}
