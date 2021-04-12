﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    public partial class InitializeMemberFromParameterTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInitializeMemberFromParameterCodeRefactoringProvider();

        private readonly NamingStylesTestOptionSets options = new NamingStylesTestOptionSets(LanguageNames.CSharp);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithSameName()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestEndOfParameter1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||])
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestEndOfParameter2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||], string t)
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s, string t)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithUnderscoreName()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string _s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string _s;

    public C(string s)
    {
        _s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeWritableProperty()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string S { get; }

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string S { get; }

    public C(string s)
    {
        S = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithDifferentName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private string t;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string t;

    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeNonWritableProperty()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string S => null;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string S => null;

    public string S1 { get; }

    public C(string s)
    {
        S1 = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeDoesNotUsePropertyWithUnrelatedName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private string T { get; }

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string T { get; }
    public string S { get; }

    public C(string s)
    {
        S = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithWrongType1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private int s;

    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithWrongType2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private readonly string s1;
    private int s;

    public C(string s)
    {
        s1 = s;
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithConvertibleType()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private object s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private object s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private int s;
    private int x;

    public C([||]string s)
    {
        x = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private int s;
    private int x;

    public C([||]string s)
    {
        x = s ?? throw new Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
        s = 0;
    }
}",

@"
class C
{
    private int s;

    public C([||]string s)
    {
        s = 0;
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C([||]string s, string t)
    {
        this.t = t;   
    }
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;   
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C(string s, [||]string t)
    {
        this.s = s;   
    }
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C([||]string s)
    {
        if (true) { } 
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        if (true) { }

        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private string s;

    public void M([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation4()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C(string s, [||]string t)
        => this.s = s;   
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation5()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C([||]string s, string t)
        => this.t = t;   
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation6()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C(string s, [||]string t)
    {
        S = s;   
    }

    public string S { get; }
}",
@"
class C
{
    public C(string s, string t)
    {
        S = s;
        T = t;
    }

    public string S { get; }
    public string T { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation7()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C([||]string s, string t)
    {
        T = t;   
    }

    public string T { get; }
}",
@"
class C
{
    public C(string s, string t)
    {
        S = s;
        T = t;   
    }

    public string S { get; }
    public string T { get; }
}");
        }

        [WorkItem(19956, "https://github.com/dotnet/roslyn/issues/19956")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||])
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithParameterNameSelected1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string [|s|])
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [WorkItem(29190, "https://github.com/dotnet/roslyn/issues/29190")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeField_ParameterNameSelected2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string [|s|], int i)
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s, int i)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassProperty_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;

    public C(int test, int test2)
    {
        Test2 = test2;
    }

    public int Test2 { get; }
}", index: 0, parameters: OmitIfDefault_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassProperty_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;

    public C(int test, int test2)
    {
        Test2 = test2;
    }

    public int Test2 { get; }
}", index: 0, parameters: Never_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassProperty_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;

    public C(int test, int test2)
    {
        Test2 = test2;
    }

    public int Test2 { get; }
}", index: 0, parameters: Always_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassField_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;
    readonly int test2;

    public C(int test, int test2)
    {
        this.test2 = test2;
    }
}", index: 1, parameters: OmitIfDefault_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassField_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;
    readonly int test2;

    public C(int test, int test2)
    {
        this.test2 = test2;
    }
}", index: 1, parameters: Never_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeClassField_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    readonly int test = 5;

    public C(int test, int [|test2|])
    {
    }
}",
@"
class C
{
    readonly int test = 5;
    private readonly int test2;

    public C(int test, int test2)
    {
        this.test2 = test2;
    }
}", index: 1, parameters: Always_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructProperty_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    public Test(int test)
    {
        Test = test;
    }

    public int Test { get; }
}", index: 0, parameters: OmitIfDefault_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructProperty_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    public Test(int test)
    {
        Test = test;
    }

    public int Test { get; }
}", index: 0, parameters: Never_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructProperty_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    public Test(int test)
    {
        Test = test;
    }

    public int Test { get; }
}", index: 0, parameters: Always_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructField_RequiredAccessibilityOmitIfDefault()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    readonly int test;

    public Test(int test)
    {
        this.test = test;
    }
}", index: 1, parameters: OmitIfDefault_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructField_RequiredAccessibilityNever()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    readonly int test;

    public Test(int test)
    {
        this.test = test;
    }
}", index: 1, parameters: Never_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeStructField_RequiredAccessibilityAlways()
        {
            await TestInRegularAndScript1Async(
@"
struct S
{
    public Test(int [|test|])
    {
    }
}",
@"
struct S
{
    private readonly int test;

    public Test(int test)
    {
        this.test = test;
    }
}", index: 1, parameters: Always_Warning);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string s)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string s)
    {
        _s = s;
    }
}", index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_s)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string t_s)
    {
        _s = t_s;
    }
}", index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecifiedParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_s_End)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string p_s_End)
    {
        _s = p_s_End;
    }
}", index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_p_s_End)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string t_p_s_End)
    {
        _s = t_p_s_End;
    }
}", index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_t_s)
    {
    }
}",
        @"
class C
{
    private readonly string _s;

    public C([||]string p_t_s)
    {
        _s = p_t_s;
    }
}", index: 1, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string s)
    {
    }
}",
    @"
class C
{
    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_s)
    {
    }
}",
    @"
class C
{
    public C(string t_s)
    {
        S = t_s;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecifiedParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_s_End)
    {
    }
}",
    @"
class C
{
    public C(string p_s_End)
    {
        S = p_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_p_s_End)
    {
    }
}",
    @"
class C
{
    public C(string t_p_s_End)
    {
        S = t_p_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_CreateAndInitProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_t_s_End)
    {
    }
}",
        @"
class C
{
    public C([||]string p_t_s_End)
    {
        S = p_t_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    private readonly string _s;

    public C([||]string s)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string s)
    {
        _s = s;
    }
}", index: 0, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    private readonly string _s;

    public C([||]string t_s)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string t_s)
    {
        _s = t_s;
    }
}", index: 0, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecifiedParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    private readonly string _s;

    public C([||]string p_s_End)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string p_s_End)
    {
        _s = p_s_End;
    }
}", index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_InitializeField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    private readonly string _s;

    public C([||]string t_p_s_End)
    {
    }
}",
    @"
class C
{
    private readonly string _s;

    public C(string t_p_s_End)
    {
        _s = t_p_s_End;
    }
}", index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeField()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    private readonly string _s;

    public C([||]string p_t_s_End)
    {
    }
}",
        @"
class C
{
    private readonly string _s;

    public C([||]string p_t_s_End)
    {
        _s = p_t_s_End;
    }
}", index: 0, parameters: new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string s)
    {
    }

    public string S { get; }
}",
    @"
class C
{
    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_s)
    {
    }

    public string S { get; }
}",
    @"
class C
{
    public C(string t_s)
    {
        S = t_s;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSpecifiedParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_s_End)
    {
    }

    public string S { get; }
}",
    @"
class C
{
    public C(string p_s_End)
    {
        S = p_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string t_p_s_End)
    {
    }

    public string S { get; }
}",
    @"
class C
{
    public C(string t_p_s_End)
    {
        S = t_p_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCommonAndSpecifiedParameterNamingStyle2_InitializeProperty()
        {
            await TestInRegularAndScript1Async(
    @"
class C
{
    public C([||]string p_t_s_End)
    {
    }

    public string S { get; }
}",
        @"
class C
{
    public C([||]string p_t_s_End)
    {
        S = p_t_s_End;
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestBaseNameEmpty()
        {
            await TestMissingAsync(
    @"
class C
{
    public C([||]string p__End)
    {
    }

    public string S { get; }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSomeBaseNamesEmpty()
        {
            // Currently, this case does not offer a refactoring because selecting multiple parameters 
            // is not supported. If multiple parameters are supported in the future, this case should 
            // be updated to verify that only the parameter name that does not have an empty base is offered.
            await TestMissingAsync(
    @"
class C
{
    public C([|string p__End, string p_test_t|])
    {
    }
}", parameters: new TestParameters(options: options.MergeStyles(options.PropertyNamesArePascalCase, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix)));
        }

        private TestParameters OmitIfDefault_Warning => new TestParameters(options: Option(CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault, NotificationOption2.Warning));
        private TestParameters Never_Warning => new TestParameters(options: Option(CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.Never, NotificationOption2.Warning));
        private TestParameters Always_Warning => new TestParameters(options: Option(CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.Always, NotificationOption2.Warning));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCreateFieldWithTopLevelNullability()
        {
            await TestInRegularAndScript1Async(
    @"
#nullable enable
class C
{
    public C([||]string? s)
    {
    }
}",
    @"
#nullable enable
class C
{
    private readonly string? _s;

    public C(string? s)
    {
        _s = s;
    }
}", index: 1, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithUnderscorePrefix));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestCreatePropertyWithTopLevelNullability()
        {
            await TestInRegularAndScript1Async(
    @"
#nullable enable
class C
{
    public C([||]string? s)
    {
    }
}",
    @"
#nullable enable
class C
{
    public C(string? s)
    {
        S = s;
    }

    public string? S { get; }
}", parameters: new TestParameters(options: options.PropertyNamesArePascalCase));
        }

        [WorkItem(24526, "https://github.com/dotnet/roslyn/issues/24526")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSingleLineBlock_BraceOnNextLine()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C([||]string s) { }
}",
@"
class C
{
    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}");
        }

        [WorkItem(24526, "https://github.com/dotnet/roslyn/issues/24526")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestSingleLineBlock_BraceOnSameLine()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C([||]string s) { }
}",
@"
class C
{
    public C(string s) {
        S = s;
    }

    public string S { get; }
}", options: this.Option(CSharpFormattingOptions2.NewLinesForBracesInMethods, false));
        }

        [WorkItem(23308, "https://github.com/dotnet/roslyn/issues/23308")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateFieldIfParameterFollowsExistingFieldAssignment()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly string s;

    public C(string s, [||]int i)
    {
        this.s = s;
    }
}",
@"
class C
{
    private readonly string s;
    private readonly int i;

    public C(string s, int i)
    {
        this.s = s;
        this.i = i;
    }
}");
        }

        [WorkItem(23308, "https://github.com/dotnet/roslyn/issues/23308")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateFieldIfParameterPrecedesExistingFieldAssignment()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly string s;

    public C([||]int i, string s)
    {
        this.s = s;
    }
}",
@"
class C
{
    private readonly int i;
    private readonly string s;

    public C(int i, string s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [WorkItem(41824, "https://github.com/dotnet/roslyn/issues/41824")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestMissingInArgList()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private static void M()
    {
        M2(__arglist(1, 2, 3, 5, 6));
    }

    public static void M2([||]__arglist)
    {
    }
}");
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingFields1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C([||]int i, int j, int k)
    {
    }
}",
@"
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
}", index: 3);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingFields2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int i;

    public C(int i, [||]int j, int k)
    {
        this.i = i;
    }
}",
@"
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
}", index: 2);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingFields3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int j;

    public C([||]int i, int j, int k)
    {
        this.j = j;
    }
}",
@"
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
}", index: 2);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingFields4()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int k;

    public C([||]int i, int j, int k)
    {
        this.k = k;
    }
}",
@"
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
}", index: 2);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingProperties1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C([||]int i, int j, int k)
    {
    }
}",
@"
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
}", index: 2);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingProperties2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int i;

    public C(int i, [||]int j, int k)
    {
        this.i = i;
    }
}",
@"
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
}", index: 3);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingProperties3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int j;

    public C([||]int i, int j, int k)
    {
        this.j = j;
    }
}",
@"
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
}", index: 3);
        }

        [WorkItem(35665, "https://github.com/dotnet/roslyn/issues/35665")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestGenerateRemainingProperties4()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private readonly int k;

    public C([||]int i, int j, int k)
    {
        this.k = k;
    }
}",
@"
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
}", index: 3);
        }
    }
}
