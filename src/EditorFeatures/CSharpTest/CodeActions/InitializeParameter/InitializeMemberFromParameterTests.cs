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
public sealed partial class InitializeMemberFromParameterTests : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpInitializeMemberFromParameterCodeRefactoringProvider();

    private readonly NamingStylesTestOptionSets options = new(LanguageNames.CSharp);

    [Fact]
    public Task TestInitializeFieldWithSameName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s)
                {
                    this.s = s;
                }
            }
            """);

    [Fact]
    public Task TestEndOfParameter1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C(string s[||])
                {
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s)
                {
                    this.s = s;
                }
            }
            """);

    [Fact]
    public Task TestEndOfParameter2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C(string s[||], string t)
                {
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s, string t)
                {
                    this.s = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithUnderscoreName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string _s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string _s;

                public C(string s)
                {
                    _s = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeWritableProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string S { get; }

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string S { get; }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithDifferentName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string t;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string t;

                public C(string s)
                {
                    S = s;
                }

                public string S { get; }
            }
            """);

    [Fact]
    public Task TestInitializeNonWritableProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string S => null;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string S => null;

                public string S1 { get; }

                public C(string s)
                {
                    S1 = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeDoesNotUsePropertyWithUnrelatedName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string T { get; }

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private string T { get; }
                public string S { get; }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithWrongType1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private int s;

                public C(string s)
                {
                    S = s;
                }

                public string S { get; }
            }
            """);

    [Fact]
    public Task TestInitializeFieldWithWrongType2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string s1;
                private int s;

                public C(string s)
                {
                    s1 = s;
                }
            }
            """, index: 1);

    [Fact]
    public Task TestInitializeFieldWithConvertibleType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private object s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private object s;

                public C(string s)
                {
                    this.s = s;
                }
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                private int s;
                private int x;

                public C([||]string s)
                {
                    x = s;
                }
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                private int s;
                private int x;

                public C([||]string s)
                {
                    x = s ?? throw new Exception();
                }
            }
            """);

    [Fact]
    public Task TestWhenAlreadyInitialized3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int s;

                public C([||]string s)
                {
                    s = 0;
                }
            }
            """,

            """
            class C
            {
                private int s;

                public C([||]string s)
                {
                    s = 0;
                    S = s;
                }

                public string S { get; }
            }
            """);

    [Fact]
    public Task TestInsertionLocation1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;
                private string t;

                public C([||]string s, string t)
                {
                    this.t = t;   
                }
            }
            """,
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, string t)
                {
                    this.s = s;
                    this.t = t;   
                }
            }
            """);

    [Fact]
    public Task TestInsertionLocation2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, [||]string t)
                {
                    this.s = s;   
                }
            }
            """,
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, string t)
                {
                    this.s = s;
                    this.t = t;
                }
            }
            """);

    [Fact]
    public Task TestInsertionLocation3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C([||]string s)
                {
                    if (true) { } 
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s)
                {
                    if (true) { }

                    this.s = s;
                }
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
    public Task TestInsertionLocation4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, [||]string t)
                    => this.s = s;   
            }
            """,
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, string t)
                {
                    this.s = s;
                    this.t = t;
                }
            }
            """);

    [Fact]
    public Task TestInsertionLocation5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;
                private string t;

                public C([||]string s, string t)
                    => this.t = t;   
            }
            """,
            """
            class C
            {
                private string s;
                private string t;

                public C(string s, string t)
                {
                    this.s = s;
                    this.t = t;
                }
            }
            """);

    [Fact]
    public Task TestInsertionLocation6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(string s, [||]string t)
                {
                    S = s;   
                }

                public string S { get; }
            }
            """,
            """
            class C
            {
                public C(string s, string t)
                {
                    S = s;
                    T = t;
                }

                public string S { get; }
                public string T { get; }
            }
            """);

    [Fact]
    public Task TestInsertionLocation7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string s, string t)
                {
                    T = t;   
                }

                public string T { get; }
            }
            """,
            """
            class C
            {
                public C(string s, string t)
                {
                    S = s;
                    T = t;   
                }

                public string S { get; }
                public string T { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19956")]
    public Task TestNoBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C(string s[||])
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s)
                {
                    this.s = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
    public Task TestInitializeFieldWithParameterNameSelected1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C(string [|s|])
                {
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s)
                {
                    this.s = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")]
    public Task TestInitializeField_ParameterNameSelected2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private string s;

                public C(string [|s|], int i)
                {
                }
            }
            """,
            """
            class C
            {
                private string s;

                public C(string s, int i)
                {
                    this.s = s;
                }
            }
            """);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int test2)
                {
                    Test2 = test2;
                }

                public int Test2 { get; }
            }
            """, index: 0, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int test2)
                {
                    Test2 = test2;
                }

                public int Test2 { get; }
            }
            """, index: 0, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeClassProperty_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int test2)
                {
                    Test2 = test2;
                }

                public int Test2 { get; }
            }
            """, index: 0, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;
                readonly int test2;

                public C(int test, int test2)
                {
                    this.test2 = test2;
                }
            }
            """, index: 1, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;
                readonly int test2;

                public C(int test, int test2)
                {
                    this.test2 = test2;
                }
            }
            """, index: 1, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeClassField_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                readonly int test = 5;

                public C(int test, int [|test2|])
                {
                }
            }
            """,
            """
            class C
            {
                readonly int test = 5;
                private readonly int test2;

                public C(int test, int test2)
                {
                    this.test2 = test2;
                }
            }
            """, index: 1, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                public Test(int test)
                {
                    Test = test;
                }

                public int Test { get; }
            }
            """, index: 0, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                public Test(int test)
                {
                    Test = test;
                }

                public int Test { get; }
            }
            """, index: 0, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeStructProperty_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                public Test(int test)
                {
                    Test = test;
                }

                public int Test { get; }
            }
            """, index: 0, parameters: Always_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityOmitIfDefault()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                readonly int test;

                public Test(int test)
                {
                    this.test = test;
                }
            }
            """, index: 1, parameters: OmitIfDefault_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityNever()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                readonly int test;

                public Test(int test)
                {
                    this.test = test;
                }
            }
            """, index: 1, parameters: Never_Warning);

    [Fact]
    public Task TestInitializeStructField_RequiredAccessibilityAlways()
        => TestInRegularAndScriptAsync(
            """
            struct S
            {
                public Test(int [|test|])
                {
                }
            }
            """,
            """
            struct S
            {
                private readonly int test;

                public Test(int test)
                {
                    this.test = test;
                }
            }
            """, index: 1, parameters: Always_Warning);

    [Fact]
    public Task TestNoParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string s)
                {
                    _s = s;
                }
            }
            """, index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCommonParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string t_s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string t_s)
                {
                    _s = t_s;
                }
            }
            """, index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string p_s_End)
                {
                    _s = p_s_End;
                }
            }
            """, index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string t_p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string t_p_s_End)
                {
                    _s = t_p_s_End;
                }
            }
            """, index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string p_t_s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C([||]string p_t_s)
                {
                    _s = p_t_s;
                }
            }
            """, index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));

    [Fact]
    public Task TestNoParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                public C(string s)
                {
                    S = s;
                }

                public string S { get; }
            }
            """, parameters: new TestParameters(options: options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestCommonParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string t_s)
                {
                }
            }
            """,
            """
            class C
            {
                public C(string t_s)
                {
                    S = t_s;
                }

                public string S { get; }
            }
            """, parameters: new TestParameters(options: options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                public C(string p_s_End)
                {
                    S = p_s_End;
                }

                public string S { get; }
            }
            """, parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string t_p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                public C(string t_p_s_End)
                {
                    S = t_p_s_End;
                }

                public string S { get; }
            }
            """, parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string p_t_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                public C([||]string p_t_s_End)
                {
                    S = p_t_s_End;
                }

                public string S { get; }
            }
            """, parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestNoParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string _s;

                public C([||]string s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string s)
                {
                    _s = s;
                }
            }
            """, index: 0, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCommonParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string _s;

                public C([||]string t_s)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string t_s)
                {
                    _s = t_s;
                }
            }
            """, index: 0, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string _s;

                public C([||]string p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string p_s_End)
                {
                    _s = p_s_End;
                }
            }
            """, index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_InitializeField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string _s;

                public C([||]string t_p_s_End)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string t_p_s_End)
                {
                    _s = t_p_s_End;
                }
            }
            """, index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeField()
        => TestInRegularAndScriptAsync(
    """
    class C
    {
        private readonly string _s;

        public C([||]string p_t_s_End)
        {
        }
    }
    """,
    """
    class C
    {
        private readonly string _s;

        public C([||]string p_t_s_End)
        {
            _s = p_t_s_End;
        }
    }
    """, index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestNoParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
"""
class C
{
    public C([||]string s)
    {
    }

    public string S { get; }
}
""",
"""
class C
{
    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}
""", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestCommonParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
"""
class C
{
    public C([||]string t_s)
    {
    }

    public string S { get; }
}
""",
"""
class C
{
    public C(string t_s)
    {
        S = t_s;
    }

    public string S { get; }
}
""", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));

    [Fact]
    public Task TestSpecifiedParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
"""
class C
{
    public C([||]string p_s_End)
    {
    }

    public string S { get; }
}
""",
"""
class C
{
    public C(string p_s_End)
    {
        S = p_s_End;
    }

    public string S { get; }
}
""", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle_InitializeProperty()
        => TestInRegularAndScriptAsync(
"""
class C
{
    public C([||]string t_p_s_End)
    {
    }

    public string S { get; }
}
""",
"""
class C
{
    public C(string t_p_s_End)
    {
        S = t_p_s_End;
    }

    public string S { get; }
}
""", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeProperty()
        => TestInRegularAndScriptAsync(
    """
    class C
    {
        public C([||]string p_t_s_End)
        {
        }

        public string S { get; }
    }
    """,
    """
    class C
    {
        public C([||]string p_t_s_End)
        {
            S = p_t_s_End;
        }

        public string S { get; }
    }
    """, parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestBaseNameEmpty()
        => TestMissingAsync(
"""
class C
{
    public C([||]string p__End)
    {
    }

    public string S { get; }
}
""", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    [Fact]
    public Task TestSomeBaseNamesEmpty()
        => TestMissingAsync(
"""
class C
{
    public C([|string p__End, string p_test_t|])
    {
    }
}
""", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));

    private TestParameters OmitIfDefault_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption2.Warning));
    private TestParameters Never_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never, NotificationOption2.Warning));
    private TestParameters Always_Warning => new(options: Option(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always, NotificationOption2.Warning));

    [Fact]
    public Task TestCreateFieldWithTopLevelNullability()
        => TestInRegularAndScriptAsync(
"""
#nullable enable
class C
{
    public C([||]string? s)
    {
    }
}
""",
"""
#nullable enable
class C
{
    private readonly string? _s;

    public C(string? s)
    {
        _s = s;
    }
}
""", index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));

    [Fact]
    public Task TestCreatePropertyWithTopLevelNullability()
        => TestInRegularAndScriptAsync(
"""
#nullable enable
class C
{
    public C([||]string? s)
    {
    }
}
""",
"""
#nullable enable
class C
{
    public C(string? s)
    {
        S = s;
    }

    public string? S { get; }
}
""", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
    public Task TestSingleLineBlock_BraceOnNextLine()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string s) { }
            }
            """,
            """
            class C
            {
                public C(string s)
                {
                    S = s;
                }

                public string S { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24526")]
    public Task TestSingleLineBlock_BraceOnSameLine()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]string s) { }
            }
            """,
            """
            class C
            {
                public C(string s) {
                    S = s;
                }

                public string S { get; }
            }
            """, new(options: this.Option(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Methods)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;

                public C(string s, [||]int i)
                {
                    this.s = s;
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly int i;

                public C(string s, int i)
                {
                    this.s = s;
                    this.i = i;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;

                public C([||]int i, string s)
                {
                    this.s = s;
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly string s;

                public C(int i, string s)
                {
                    this.i = i;
                    this.s = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment_TupleAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, string t, [||]int i)
                {
                    (this.s, this.t) = (s, t);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly int i;

                public C(string s, string t, int i)
                {
                    (this.s, this.t, this.i) = (s, t, i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment_TupleAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, string t, [||]int i) =>
                    (this.s, this.t) = (s, t);
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly int i;

                public C(string s, string t, int i) =>
                    (this.s, this.t, this.i) = (s, t, i);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment_TupleAssignment3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, string t, [||]int i)
                {
                    if (s is null) throw new ArgumentNullException();
                    (this.s, this.t) = (s, t);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly int i;

                public C(string s, string t, int i)
                {
                    if (s is null) throw new ArgumentNullException();
                    (this.s, this.t, this.i) = (s, t, i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment_TupleAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C([||]int i, string s, string t)
                {
                    (this.s, this.t) = (s, t);
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly string s;
                private readonly string t;

                public C(int i, string s, string t)
                {
                    (this.i, this.s, this.t) = (i, s, t);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment_TupleAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C([||]int i, string s, string t) =>
                    (this.s, this.t) = (s, t);
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly string s;
                private readonly string t;

                public C(int i, string s, string t) =>
                    (this.i, this.s, this.t) = (i, s, t);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterInMiddleOfExistingFieldAssignment_TupleAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, [||]int i, string t)
                {
                    (this.s, this.t) = (s, t);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly int i;
                private readonly string t;

                public C(string s, int i, string t)
                {
                    (this.s, this.i, this.t) = (s, i, t);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGenerateFieldIfParameterInMiddleOfExistingFieldAssignment_TupleAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, [||]int i, string t) =>
                    (this.s, this.t) = (s, t);
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly int i;
                private readonly string t;

                public C(string s, int i, string t) =>
                    (this.s, this.i, this.t) = (s, i, t);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23308")]
    public Task TestGeneratePropertyIfParameterFollowsExistingPropertyAssignment_TupleAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public string S { get; }
                public string T { get; }

                public C(string s, string t, [||]int i)
                {
                    (S, T) = (s, t);
                }
            }
            """,
            """
            class C
            {
                public string S { get; }
                public string T { get; }
                public int I { get; }

                public C(string s, string t, int i)
                {
                    (S, T, I) = (s, t, i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41824")]
    public Task TestMissingInArgList()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                private static void M()
                {
                    M2(__arglist(1, 2, 3, 5, 6));
                }

                public static void M2([||]__arglist)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]int i, int j, int k)
                {
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly int j;
                private readonly int k;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    this.j = j;
                    this.k = k;
                }
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int i;

                public C(int i, [||]int j, int k)
                {
                    this.i = i;
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly int j;
                private readonly int k;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    this.j = j;
                    this.k = k;
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int j;

                public C([||]int i, int j, int k)
                {
                    this.j = j;
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly int j;
                private readonly int k;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    this.j = j;
                    this.k = k;
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingFields4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int k;

                public C([||]int i, int j, int k)
                {
                    this.k = k;
                }
            }
            """,
            """
            class C
            {
                private readonly int i;
                private readonly int j;
                private readonly int k;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    this.j = j;
                    this.k = k;
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C([||]int i, int j, int k)
                {
                }
            }
            """,
            """
            class C
            {
                public C(int i, int j, int k)
                {
                    I = i;
                    J = j;
                    K = k;
                }

                public int I { get; }
                public int J { get; }
                public int K { get; }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int i;

                public C(int i, [||]int j, int k)
                {
                    this.i = i;
                }
            }
            """,
            """
            class C
            {
                private readonly int i;

                public C(int i, int j, int k)
                {
                    this.i = i;
                    J = j;
                    K = k;
                }

                public int J { get; }
                public int K { get; }
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int j;

                public C([||]int i, int j, int k)
                {
                    this.j = j;
                }
            }
            """,
            """
            class C
            {
                private readonly int j;

                public C(int i, int j, int k)
                {
                    I = i;
                    this.j = j;
                    K = k;
                }

                public int I { get; }
                public int K { get; }
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35665")]
    public Task TestGenerateRemainingProperties4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly int k;

                public C([||]int i, int j, int k)
                {
                    this.k = k;
                }
            }
            """,
            """
            class C
            {
                private readonly int k;

                public C(int i, int j, int k)
                {
                    I = i;
                    J = j;
                    this.k = k;
                }

                public int I { get; }
                public int J { get; }
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53467")]
    public Task TestMissingWhenTypeNotInCompilation()
        => TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1">
                    <Document>
            public class Goo
            {
                public Goo(int prop1)
                {
                    Prop1 = prop1;
                }

                public int Prop1 { get; }
            }

            public class Bar : Goo
            {
                public Bar(int prop1, int [||]prop2) : base(prop1) { }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S => throw new NotImplementedException();

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S { get; }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S
                {
                    get => throw new NotImplementedException();
                }

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S
                {
                    get;
                }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S
                {
                    get { throw new NotImplementedException(); }
                }

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S
                {
                    get;
                }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S
                {
                    get => throw new NotImplementedException();
                    set => throw new NotImplementedException();
                }

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S
                {
                    get;
                    set;
                }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty5()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S
                {
                    get;
                    set;
                }

                public C(string s)
                {
                    S = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty6()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                private string S => throw new InvalidOperationException();

                public C([||]string s)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                private string S => throw new InvalidOperationException();

                public string S1 { get; }

                public C(string s)
                {
                    S1 = s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36998")]
    public Task TestInitializeThrowingProperty_DifferentFile1()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public partial class Goo
            {
                public Goo(string [||]name)
                {
                }
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
            public partial class Goo
            {
                public Goo(string name)
                {
                    Name = name;
                }
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
            """);
}
