// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InitializeParameter;

[Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
public sealed partial class InitializeMemberFromPrimaryConstructorParameterTests : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider();

    private readonly NamingStylesTestOptionSets _options = new(LanguageNames.CSharp);

    private TestParameters OmitIfDefault_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption2.Warning));
    private TestParameters Never_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never, NotificationOption2.Warning));
    private TestParameters Always_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always, NotificationOption2.Warning));

    [Fact]
    public Task TestInitializeFieldWithSameName()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string s;
            }
            """,
            """
            class C(string s)
            {
                private string s = s;
            }
            """);

    [Fact]
    public Task TestEndOfParameter1()
        => TestInRegularAndScriptAsync(
            """
            class C(string s[||])
            {
                private string s;
            }
            """,
            """
            class C(string s)
            {
                private string s = s;
            }
            """);

    [Fact]
    public Task TestEndOfParameter2()
        => TestInRegularAndScriptAsync(
            """
            class C(string s[||], string t)
            {
                private string s;
            }
            """,
            """
            class C(string s, string t)
            {
                private string s = s;
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithUnderscoreName()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string _s;
            }
            """,
            """
            class C(string s)
            {
                private string _s = s;
            }
            """);

    [Fact]
    public Task TestInitializeWritableProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string S { get; }
            }
            """,
            """
            class C(string s)
            {
                private string S { get; } = s;
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithDifferentName()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string t;
            }
            """,
            """
            class C(string s)
            {
                private string t;

                public string S { get; } = s;
            }
            """);

    [Fact]
    public Task TestInitializeNonWritableProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string S => null;
            }
            """,
            """
            class C(string s)
            {
                public string S1 { get; } = s;

                private string S => null;
            }
            """);

    [Fact]
    public Task TestInitializeDoesNotUsePropertyWithUnrelatedName()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private string T { get; }
            }
            """,
            """
            class C(string s)
            {
                public string S { get; } = s;
                private string T { get; }
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithWrongType1()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private int s;
            }
            """,
            """
            class C(string s)
            {
                private int s;

                public string S { get; } = s;
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithWrongType2()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private int s;
            }
            """,
            """
            class C(string s)
            {
                private readonly string s1 = s;
                private int s;
            }
            """, index: 1);

    [Fact]
    public Task TestInitializeFieldWithConvertibleType()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private object s;
            }
            """,
            """
            class C(string s)
            {
                private object s = s;
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private int s;
                private int x = s;
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private int s;
                private int x = s ?? throw new Exception();
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized3()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private int s = 0;
            }
            """,

            """
            class C([||]string s)
            {
                private int s = 0;

                public string S { get; } = s;
            }
            """);

    [Fact]
    public Task TestInsertionLocation1()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s, string t)
            {
                private string s;
                private string t = t;
            }
            """,
            """
            class C(string s, string t)
            {
                private string s = s;
                private string t = t;
            }
            """);

    [Fact]
    public Task TestInsertionLocation2()
        => TestInRegularAndScriptAsync(
            """
            class C(string s, [||]string t)
            {
                private string s = s;
                private string t;
            }
            """,
            """
            class C(string s, string t)
            {
                private string s = s;
                private string t = t;
            }
            """);

    [Fact]
    public Task TestNotInMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public void M([||]string s)
                {
                }
            }
            """);

    [Fact]
    public Task TestInsertionLocation6()
        => TestInRegularAndScriptAsync(
            """
            class C(string s, [||]string t)
            {
                public string S { get; } = s;
            }
            """,
            """
            class C(string s, string t)
            {
                public string S { get; } = s;
                public string T { get; } = t;
            }
            """);

    [Fact]
    public Task TestInsertionLocation7()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s, string t)
            {
                public string T { get; } = t;
            }
            """,
            """
            class C(string s, string t)
            {
                public string S { get; } = s;
                public string T { get; } = t;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19956")]
    public Task TestNoBlock1()
        => TestInRegularAndScriptAsync(
            """
            class C(string s[||])
            """,
            """
            class C(string s)
            {
                public string S { get; } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19956")]
    public Task TestNoBlock2()
        => TestInRegularAndScriptAsync(
            """
            class C(string s[||])
            """,
            """
            class C(string s)
            {
                private readonly string s = s;
            }
            """,
            index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
    public Task TestInitializeFieldWithParameterNameSelected1()
        => TestInRegularAndScriptAsync(
            """
            class C(string [|s|])
            {
                private string s;
            }
            """,
            """
            class C(string s)
            {
                private string s = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
    public Task TestInitializeField_ParameterNameSelected2()
        => TestInRegularAndScriptAsync(
            """
            class C(string [|s|], int i)
            {
                private string s;
            }
            """,
            """
            class C(string s, int i)
            {
                private string s = s;
            }
            """);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;

                public int Test2 { get; } = test2;
            }
            """, index: 0, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;

                public int Test2 { get; } = test2;
            }
            """, index: 0, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;

                public int Test2 { get; } = test2;
            }
            """, index: 0, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;
                readonly int test2 = test2;
            }
            """, index: 1, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;
                readonly int test2 = test2;
            }
            """, index: 1, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            class C(int test, int [|test2|])
            {
                readonly int test = 5;
            }
            """,
            """
            class C(int test, int test2)
            {
                readonly int test = 5;
                private readonly int test2 = test2;
            }
            """, index: 1, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                public int Test { get; } = test;
            }
            """, index: 0, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                public int Test { get; } = test;
            }
            """, index: 0, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                public int Test { get; } = test;
            }
            """, index: 0, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                readonly int test = test;
            }
            """, index: 1, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                readonly int test = test;
            }
            """, index: 1, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            struct S(int [|test|])
            {
            }
            """,
            """
            struct S(int test)
            {
                private readonly int test = test;
            }
            """, index: 1, parameters: Always_Warning);

    [Fact]
    public Task TestNoParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
            }
            """,
            """
            class C(string s)
            {
                private readonly string _s = s;
            }
            """, index: 1, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCommonParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_s)
            {
            }
            """,
            """
            class C(string t_s)
            {
                private readonly string _s = t_s;
            }
            """, index: 1, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_s_End)
            {
            }
            """,
            """
            class C(string p_s_End)
            {
                private readonly string _s = p_s_End;
            }
            """, index: 1, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_p_s_End)
            {
            }
            """,
            """
            class C(string t_p_s_End)
            {
                private readonly string _s = t_p_s_End;
            }
            """, index: 1, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_t_s)
            {
            }
            """,
            """
            class C([||]string p_t_s)
            {
                private readonly string _s = p_t_s;
            }
            """, index: 1, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));

    [Fact]
    public Task TestNoParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
            }
            """,
            """
            class C(string s)
            {
                public string S { get; } = s;
            }
            """, parameters: new TestParameters(options: _options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestCommonParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_s)
            {
            }
            """,
            """
            class C(string t_s)
            {
                public string S { get; } = t_s;
            }
            """, parameters: new TestParameters(options: _options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_s_End)
            {
            }
            """,
            """
            class C(string p_s_End)
            {
                public string S { get; } = p_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_p_s_End)
            {
            }
            """,
            """
            class C(string t_p_s_End)
            {
                public string S { get; } = t_p_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_t_s_End)
            {
            }
            """,
            """
            class C(string p_t_s_End)
            {
                public string S { get; } = p_t_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestNoParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                private readonly string _s;
            }
            """,
            """
            class C(string s)
            {
                private readonly string _s = s;
            }
            """, index: 0, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCommonParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_s)
            {
                private readonly string _s;
            }
            """,
            """
            class C(string t_s)
            {
                private readonly string _s = t_s;
            }
            """, index: 0, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_s_End)
            {
                private readonly string _s;
            }
            """,
            """
            class C(string p_s_End)
            {
                private readonly string _s = p_s_End;
            }
            """, index: 0, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_p_s_End)
            {
                private readonly string _s;
            }
            """,
            """
            class C(string t_p_s_End)
            {
                private readonly string _s = t_p_s_End;
            }
            """, index: 0, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_t_s_End)
            {
                private readonly string _s;
            }
            """,
            """
            class C([||]string p_t_s_End)
            {
                private readonly string _s = p_t_s_End;
            }
            """, index: 0, parameters: new TestParameters(options: _options.MergeStyles(_options.FieldNamesAreCamelCaseWithUnderscorePrefix, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestNoParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s)
            {
                public string S { get; }
            }
            """,
            """
            class C(string s)
            {
                public string S { get; } = s;
            }
            """, parameters: new TestParameters(options: _options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestCommonParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_s)
            {
                public string S { get; }
            }
            """,
            """
            class C(string t_s)
            {
                public string S { get; } = t_s;
            }
            """, parameters: new TestParameters(options: _options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_s_End)
            {
                public string S { get; }
            }
            """,
            """
            class C(string p_s_End)
            {
                public string S { get; } = p_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string t_p_s_End)
            {
                public string S { get; }
            }
            """,
            """
            class C(string t_p_s_End)
            {
                public string S { get; } = t_p_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeProperty()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string p_t_s_End)
            {
                public string S { get; }
            }
            """,
            """
            class C([||]string p_t_s_End)
            {
                public string S { get; } = p_t_s_End;
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestBaseNameEmpty()
        => TestMissingAsync(
            """
            class C([||]string p__End)
            {
                public string S { get; }
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestSomeBaseNamesEmpty()
        => TestMissingAsync(
            """
            class C([|string p__End, string p_test_t|])
            {
            }
            """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCreateFieldWithTopLevelNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C([||]string? s)
            {
            }
            """,
            """
            #nullable enable
            class C(string? s)
            {
                private readonly string? _s = s;
            }
            """, index: 1, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCreatePropertyWithTopLevelNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C([||]string? s)
            {
            }
            """,
            """
            #nullable enable
            class C(string? s)
            {
                public string? S { get; } = s;
            }
            """, parameters: new TestParameters(options: _options.PropertyNamesArePascalCase));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
    public Task TestSingleLineBlock_BraceOnNextLine()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s) { }
            """,
            """
            class C(string s)
            {
                public string S { get; } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
    public Task TestSingleLineBlock_BraceOnSameLine()
        => TestInRegularAndScriptAsync(
            """
            class C([||]string s) { }
            """,
            """
            class C(string s) {
                public string S { get; } = s;
            }
            """, new(options: this.Option(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Types)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment()
        => TestInRegularAndScriptAsync(
            """
            class C(string s, [||]int i)
            {
                private readonly string s = s;
            }
            """,
            """
            class C(string s, int i)
            {
                private readonly string s = s;
                private readonly int i = i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, string s)
            {
                private readonly string s = s;
            }
            """,
            """
            class C(int i, string s)
            {
                private readonly int i = i;
                private readonly string s = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields1()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int i = i;
                private readonly int j = j;
                private readonly int k = k;
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields2()
        => TestInRegularAndScriptAsync(
            """
            class C(int i, [||]int j, int k)
            {
                private readonly int i = i;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int i = i;
                private readonly int j = j;
                private readonly int k = k;
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields3()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
                private readonly int j = j;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int i = i;
                private readonly int j = j;
                private readonly int k = k;
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields4()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
                private readonly int k = k;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int i = i;
                private readonly int j = j;
                private readonly int k = k;
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties1()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
            }
            """,
            """
            class C(int i, int j, int k)
            {
                public int I { get; } = i;
                public int J { get; } = j;
                public int K { get; } = k;
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties2()
        => TestInRegularAndScriptAsync(
            """
            class C(int i, [||]int j, int k)
            {
                private readonly int i = i;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int i = i;

                public int J { get; } = j;
                public int K { get; } = k;
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties3()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
                private readonly int j = j;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int j = j;

                public int I { get; } = i;
                public int K { get; } = k;
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties4()
        => TestInRegularAndScriptAsync(
            """
            class C([||]int i, int j, int k)
            {
                private readonly int k = k;
            }
            """,
            """
            class C(int i, int j, int k)
            {
                private readonly int k = k;

                public int I { get; } = i;
                public int J { get; } = j;
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C(string s)
            {
                private string S { get; } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S
                {
                    get => throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C(string s)
            {
                private string S
                {
                    get;
                } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S
                {
                    get { throw new NotImplementedException(); }
                }
            }
            """,
            """
            using System;

            class C(string s)
            {
                private string S
                {
                    get;
                } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S
                {
                    get => throw new NotImplementedException();
                    set => throw new NotImplementedException();
                }
            }
            """,
            """
            using System;

            class C(string s)
            {
                private string S
                {
                    get;
                    set;
                } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty5()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }
            }
            """,
            """
            using System;

            class C(string s)
            {
                private string S
                {
                    get;
                    set;
                } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty6()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S => throw new InvalidOperationException();
            }
            """,
            """
            using System;

            class C(string s)
            {
                public string S1 { get; } = s;

                private string S => throw new InvalidOperationException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty_DifferentFile1()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string [||]name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Name => throw new NotImplementedException();
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Name { get; } = name;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76565")]
    public Task TestCouldInitializeThrowingProperty_ButGeneratePropertyInstead()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C(string s)
            {
                public string S1 { get; } = s;

                private string S => throw new NotImplementedException();
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76565")]
    public Task TestCouldInitializeThrowingProperty_ButGenerateFieldInstead()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string S => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C(string s)
            {
                private readonly string s = s;

                private string S => throw new NotImplementedException();
            }
            """, index: 2);

    [Fact]
    public Task TestUpdateCodeToReferenceExistingField1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string _s;

                private void M()
                {
                    Console.WriteLine(s);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                private string _s = s;
            
                private void M()
                {
                    Console.WriteLine(_s);
                    var v = new C(s: "");
                }
            }
            """);

    [Fact]
    public Task TestUpdateCodeToReferenceExistingField2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private string _s;

                private void M()
                {
                    Console.WriteLine(/*t*/ s /*t2*/);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                private string _s = s;
            
                private void M()
                {
                    Console.WriteLine(/*t*/ _s /*t2*/);
                    var v = new C(s: "");
                }
            }
            """);

    [Fact]
    public Task TestUpdateCodeToReferenceExistingProperty()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                public string S { get; }

                private void M()
                {
                    Console.WriteLine(s);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                public string S { get; } = s;
            
                private void M()
                {
                    Console.WriteLine(S);
                    var v = new C(s: "");
                }
            }
            """);

    [Fact]
    public Task TestUpdateCodeToReferenceExistingProperty2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                public string S { get; }

                private void M()
                {
                    Console.WriteLine(/*t*/ s /*t2*/);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                public string S { get; } = s;
            
                private void M()
                {
                    Console.WriteLine(/*t*/ S /*t2*/);
                    var v = new C(s: "");
                }
            }
            """);

    [Fact]
    public Task TestUpdateCodeToReferenceNewProperty1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private void M()
                {
                    Console.WriteLine(s);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                public string S { get; } = s;

                private void M()
                {
                    Console.WriteLine(S);
                    var v = new C(s: "");
                }
            }
            """);

    [Fact]
    public Task TestUpdateCodeToReferenceNewField1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C([||]string s)
            {
                private void M()
                {
                    Console.WriteLine(s);
                    var v = new C(s: "");
                }
            }
            """,
            """
            using System;
            
            class C(string s)
            {
                private readonly string _s = s;

                private void M()
                {
                    Console.WriteLine(_s);
                    var v = new C(s: "");
                }
            }
            """, index: 1, parameters: new TestParameters(options: _options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestInitializeIntoFieldInDifferentPart()
        => TestInRegularAndScriptAsync(
            """
            partial class C([||]string s)
            {
            }

            partial class C
            {
                private string s;
            }
            """,
            """
            partial class C(string s)
            {
            }
            
            partial class C
            {
                private string s = s;
            }
            """);

    [Fact]
    public Task TestInitializeIntoPropertyInDifferentPart()
        => TestInRegularAndScriptAsync(
            """
            partial class C([||]string s)
            {
            }

            partial class C
            {
                private string S { get; }
            }
            """,
            """
            partial class C(string s)
            {
            }
            
            partial class C
            {
                private string S { get; } = s;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeProperty_DifferentFile1()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string [||]name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Name { get; }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Name { get; } = name;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeProperty_DifferentFile2()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string x, string [||]y)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string X { get; } = x;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string x, string y)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string X { get; } = x;
                public string Y { get; } = y;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeProperty_DifferentFile3()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string [||]x, string y)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Y { get; } = y;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string x, string y)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string X { get; } = x;
                public string Y { get; } = y;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeProperty_DifferentFile4()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string [||]x, string y, string z)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string Y { get; } = y;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string x, string y, string z)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                public string X { get; } = x;
                public string Y { get; } = y;
                public string Z { get; } = z;
            }
                    </Document>
                </Project>
            </Workspace>
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeField_DifferentFile1()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string [||]name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                private readonly string name;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo(string name)
            {
            }
                    </Document>
                    <Document>
            using System;
            public partial class Goo
            {
                private readonly string name = name;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69459")]
    public Task TestInLinkedFile()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            using System;
            
            class C([||]string s)
            {
                private string _s;
            
                private void M()
                {
                    Console.WriteLine(s);
                    var v = new C(s: "");
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            using System;
            
            class C(string s)
            {
                private string _s = s;
            
                private void M()
                {
                    Console.WriteLine(_s);
                    var v = new C(s: "");
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """);
}
