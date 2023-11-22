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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InitializeParameter
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
    public partial class InitializeMemberFromPrimaryConstructorParameterTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider();

        private readonly NamingStylesTestOptionSets _options = new(LanguageNames.CSharp);

        private TestParameters OmitIfDefault_Warning => new TestParameters(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption2.Warning));
        private TestParameters Never_Warning => new TestParameters(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never, NotificationOption2.Warning));
        private TestParameters Always_Warning => new TestParameters(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always, NotificationOption2.Warning));

        [Fact]
        public async Task TestInitializeFieldWithSameName()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestEndOfParameter1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestEndOfParameter2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeFieldWithUnderscoreName()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeWritableProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeFieldWithDifferentName()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestInitializeNonWritableProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeDoesNotUsePropertyWithUnrelatedName()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestInitializeFieldWithWrongType1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeFieldWithWrongType2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeFieldWithConvertibleType()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestWhenAlreadyInitialized1()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C([||]string s)
                {
                    private int s;
                    private int x = s;
                }
                """);
        }

        [Fact]
        public async Task TestWhenAlreadyInitialized2()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C([||]string s)
                {
                    private int s;
                    private int x = s ?? throw new Exception();
                }
                """);
        }

        [Fact]
        public async Task TestWhenAlreadyInitialized3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInsertionLocation1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInsertionLocation2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNotInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    private string s;

                    public void M([||]string s)
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInsertionLocation6()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInsertionLocation7()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19956")]
        public async Task TestNoBlock1()
        {
            await TestInRegularAndScript1Async(
                """
                class C(string s[||])
                """,
                """
                class C(string s)
                {
                    public string S { get; } = s;
                }

                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19956")]
        public async Task TestNoBlock2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
        public async Task TestInitializeFieldWithParameterNameSelected1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
        public async Task TestInitializeField_ParameterNameSelected2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassProperty_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassProperty_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassProperty_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassField_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassField_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeClassField_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructProperty_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructProperty_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructProperty_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructField_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructField_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeStructField_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNoParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSpecifiedParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNoParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSpecifiedParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNoParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSpecifiedParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeField()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNoParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSpecifiedParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestBaseNameEmpty()
        {
            await TestMissingAsync(
                """
                class C([||]string p__End)
                {
                    public string S { get; }
                }
                """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact]
        public async Task TestSomeBaseNamesEmpty()
        {
            // Currently, this case does not offer a refactoring because selecting multiple parameters 
            // is not supported. If multiple parameters are supported in the future, this case should 
            // be updated to verify that only the parameter name that does not have an empty base is offered.
            await TestMissingAsync(
                """
                class C([|string p__End, string p_test_t|])
                {
                }
                """, parameters: new TestParameters(options: _options.MergeStyles(_options.PropertyNamesArePascalCase, _options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact]
        public async Task TestCreateFieldWithTopLevelNullability()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestCreatePropertyWithTopLevelNullability()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
        public async Task TestSingleLineBlock_BraceOnNextLine()
        {
            await TestInRegularAndScript1Async(
                """
                class C([||]string s) { }
                """,
                """
                class C(string s)
                {
                    public string S { get; } = s;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
        public async Task TestSingleLineBlock_BraceOnSameLine()
        {
            await TestInRegularAndScriptAsync(
                """
                class C([||]string s) { }
                """,
                """
                class C(string s) {
                    public string S { get; } = s;
                }
                """, options: this.Option(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Types));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
        public async Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
        public async Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingFields1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingFields2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingFields3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingFields4()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingProperties1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingProperties2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingProperties3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
        public async Task TestGenerateRemainingProperties4()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty4()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty5()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty6()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeThrowingProperty_DifferentFile1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceExistingField1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceExistingField2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceExistingProperty()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceExistingProperty2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceNewProperty1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestUpdateCodeToReferenceNewField1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeIntoFieldInDifferentPart()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInitializeIntoPropertyInDifferentPart()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeProperty_DifferentFile1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeProperty_DifferentFile2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeProperty_DifferentFile3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeProperty_DifferentFile4()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
        public async Task TestInitializeField_DifferentFile1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69459")]
        public async Task TestInLinkedFile()
        {
            await TestInRegularAndScriptAsync(
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
    }
}
