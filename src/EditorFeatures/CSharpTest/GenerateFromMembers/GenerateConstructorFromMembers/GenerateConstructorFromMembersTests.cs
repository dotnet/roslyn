// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateConstructorFromMembers
{
    public class GenerateConstructorFromMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateConstructorFromMembersCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

        private readonly NamingStylesTestOptionSets options = new NamingStylesTestOptionSets(LanguageNames.CSharp);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSingleField()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSingleFieldWithCodeStyle()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|} => this.a = a;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|} => this.a = a;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;
    int b;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;
    int b;

    public Z(int a, int b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleFields()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;
    string b;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;

    public Z(int a, string b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleFields_VerticalSelection()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{[|
    int a;
    string b;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;

    public Z(int a, string b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleFields_VerticalSelectionUpToExcludedField()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    int a;[|
    string b;
    string c;|]
}",
@"using System.Collections.Generic;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleFields_VerticalSelectionUpToMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    void Foo() { }[|
    int a;
    string b;|]
}",
@"using System.Collections.Generic;

class Z
{
    void Foo() { }
    int a;
    string b;

    public Z(int a, string b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleFields_SelectionIncludingClassOpeningBrace()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;

class Z
[|{
    int a;
    string b;|]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSecondField()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    [|string b;|]

    public Z(int a)
    {
        this.a = a;
    }
}",
@"using System.Collections.Generic;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestFieldAssigningConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;
    string b;|]

    public Z(int a)
    {
        this.a = a;
    }
}",
@"using System.Collections.Generic;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestFieldAssigningConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;
    string b;|]

    public Z(int a)
    {
        this.a = a;
    }
}",
@"using System.Collections.Generic;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestDelegatingConstructor()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;
    string b;|]

    public Z(int a)
    {
        this.a = a;
    }
}",
@"using System.Collections.Generic;

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
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMissingWithExistingConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultipleProperties()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    [|public int A { get; private set; }
    public string B { get; private set; }|]
}",
@"class Z
{
    public Z(int a, string b{|Navigation:)|}
    {
        A = a;
        B = b;
    }

    public int A { get; private set; }
    public string B { get; private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultiplePropertiesWithQualification()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    [|public int A { get; private set; }
    public string B { get; private set; }|]
}",
@"class Z
{
    public Z(int a, string b{|Navigation:)|}
    {
        this.A = a;
        this.B = b;
    }

    public int A { get; private set; }
    public string B { get; private set; }
}", options: Option(CodeStyleOptions.QualifyPropertyAccess, true, NotificationOption.Error));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStruct()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

struct S
{
    [|int i;|]
}",
@"using System.Collections.Generic;

struct S
{
    int i;

    public S(int i{|Navigation:)|}
    {
        this.i = i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStructInitializingAutoProperty()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

struct S
{
    [|int i { get; set; }|]
}",
@"using System.Collections.Generic;

struct S
{
    public S(int i{|Navigation:)|}
    {
        this.i = i;
    }

    int i { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStructNotInitializingAutoProperty()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

struct S
{
    [|int i { get => f; set => f = value; }|]
    int j { get; set; }
}",
@"using System.Collections.Generic;

struct S
{
    public S(int i{|Navigation:)|} : this()
    {
        this.i = i;
    }

    int i { get => f; set => f = value; }
    int j { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStruct2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

struct S
{
    int i { get; set; }

    [|int y;|]
}",
@"using System.Collections.Generic;

struct S
{
    int i { get; set; }

    int y;

    public S(int y{|Navigation:)|} : this()
    {
        this.y = y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStruct3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

struct S
{
    [|int i { get; set; }|]

    int y;
}",
@"using System.Collections.Generic;

struct S
{
    int i { get; set; }

    int y;

    public S(int i{|Navigation:)|} : this()
    {
        this.i = i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestGenericType()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program<T>
{
    [|int i;|]
}",
@"using System.Collections.Generic;

class Program<T>
{
    int i;

    public Program(int i{|Navigation:)|}
    {
        this.i = i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSmartTagText1()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]
}",
string.Format(FeaturesResources.Generate_constructor_0_1, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSmartTagText2()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]

    public Program(bool b)
    {
        this.b = b;
    }
}",
string.Format(FeaturesResources.Generate_field_assigning_constructor_0_1, "Program", "bool, HashSet<string>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSmartTagText3()
        {
            await TestSmartTagTextAsync(
@"using System.Collections.Generic;

class Program
{
    [|bool b;
    HashSet<string> s;|]

    public Program(bool b)
    {
        this.b = b;
    }
}",
string.Format(FeaturesResources.Generate_delegating_constructor_0_1, "Program", "bool, HashSet<string>"),
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestContextualKeywordName()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|int yield;|]
}",
@"class Program
{
    int yield;

    public Program(int yield{|Navigation:)|}
    {
        this.yield = yield;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestGenerateConstructorNotOfferedForDuplicate()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class X
{
    public X(string v)
    {
    }

    static void Test()
    {
        new X(new [|string|]());
    }
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task Tuple()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|(int, string) a;|]
}",
@"using System.Collections.Generic;

class Z
{
    (int, string) a;

    public Z((int, string) a{|Navigation:)|}
    {
        this.a = a;
    }
}");
        }

        [WorkItem(14219, "https://github.com/dotnet/roslyn/issues/14219")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestUnderscoreInName1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|int _field;|]
}",
@"class Program
{
    int _field;

    public Program(int field{|Navigation:)|}
    {
        _field = field;
    }
}");
        }

        [WorkItem(14219, "https://github.com/dotnet/roslyn/issues/14219")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestUnderscoreInName_PreferThis()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|int _field;|]
}",
@"class Program
{
    int _field;

    public Program(int field{|Navigation:)|}
    {
        this._field = field;
    }
}",
options: Option(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }

        [WorkItem(13944, "https://github.com/dotnet/roslyn/issues/13944")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestGetter_Only_Auto_Props()
        {
            await TestInRegularAndScriptAsync(
@"abstract class Contribution
{
  [|public string Title { get; }
    public int Number { get; }|]
}",
@"abstract class Contribution
{
    protected Contribution(string title, int number{|Navigation:)|}
    {
        Title = title;
        Number = number;
    }

    public string Title { get; }
    public int Number { get; }
}",
options: Option(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }

        [WorkItem(13944, "https://github.com/dotnet/roslyn/issues/13944")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestAbstract_Getter_Only_Auto_Props()
        {
            await TestMissingInRegularAndScriptAsync(
@"abstract class Contribution
{
  [|public abstract string Title { get; }
    public int Number { get; }|]
}",
new TestParameters(options: Option(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.TrueWithSuggestionEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSingleFieldWithDialog()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    [||]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}",
chosenSymbols: new[] { "a" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSingleFieldWithDialog2()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class [||]Z
{
    int a;
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}",
chosenSymbols: new[] { "a" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMissingOnClassAttributes()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

[X][||]
class Z
{
    int a;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPickNoFieldWithDialog()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    [||]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z({|Navigation:)|}
    {
    }
}",
chosenSymbols: new string[] { });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestReorderFieldsWithDialog()
        {
            await TestWithPickMembersDialogAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;
    [||]
}",
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;

    public Z(string b, int a{|Navigation:)|}
    {
        this.b = b;
        this.a = a;
    }
}",
chosenSymbols: new string[] { "b", "a" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestAddNullChecks1()
        {
            await TestWithPickMembersDialogAsync(
@"
using System;
using System.Collections.Generic;

class Z
{
    int a;
    string b;
    [||]
}",
@"
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
}",
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestAddNullChecks2()
        {
            await TestWithPickMembersDialogAsync(
@"
using System;
using System.Collections.Generic;

class Z
{
    int a;
    string b;
    [||]
}",
@"
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
}",
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(options:
    Option(CodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestAddNullChecks3()
        {
            await TestWithPickMembersDialogAsync(
@"
using System;
using System.Collections.Generic;

class Z
{
    int a;
    int? b;
    [||]
}",
@"
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
}",
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(options:
    Option(CodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestAddNullChecks_CSharp6()
        {
            await TestWithPickMembersDialogAsync(
@"
using System;
using System.Collections.Generic;

class Z
{
    int a;
    string b;
    [||]
}",
@"
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
}",
chosenSymbols: new string[] { "a", "b" },
optionsCallback: options => options[0].Value = true,
parameters: new TestParameters(
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6),
    options: Option(CodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMissingOnMember1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;
    [||]public void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMissingOnMember2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;
    public void M()
    {
    }[||]

    public void N() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMissingOnMember3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    int a;
    string b;
    public void M()
    {
 [||] 
    }

    public void N() { }
}");
        }

        [WorkItem(21067, "https://github.com/dotnet/roslyn/pull/21067")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestFinalCaretPosition()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Z
{
    [|int a;|]
}",
@"using System.Collections.Generic;

class Z
{
    int a;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        [WorkItem(20595, "https://github.com/dotnet/roslyn/issues/20595")]
        public async Task ProtectedConstructorShouldBeGeneratedForAbstractClass()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C 
{
    [|public int Prop { get; set; }|]
}",
@"abstract class C 
{
    protected C(int prop{|Navigation:)|}
    {
        Prop = prop;
    }

    public int Prop { get; set; }
}",
options: Option(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }

        [WorkItem(17643, "https://github.com/dotnet/roslyn/issues/17643")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestWithDialogNoBackingField()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int F { get; set; }
    [||]
}",
@"
class Program
{
    public int F { get; set; }

    public Program(int f{|Navigation:)|}
    {
        F = f;
    }
}",
chosenSymbols: null);
        }

        [WorkItem(25690, "https://github.com/dotnet/roslyn/issues/25690")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestWithDialogNoIndexer()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int P { get => 0; set { } }
    public int this[int index] { get => 0; set { } }
    [||]
}",
@"
class Program
{
    public int P { get => 0; set { } }
    public int this[int index] { get => 0; set { } }

    public Program(int p{|Navigation:)|}
    {
        P = p;
    }
}",
chosenSymbols: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task TestWithDialogSetterOnlyProperty()
        {
            await TestWithPickMembersDialogAsync(
@"
class Program
{
    public int P { get => 0; set { } }
    public int S { set { } }
    [||]
}",
@"
class Program
{
    public int P { get => 0; set { } }
    public int S { set { } }

    public Program(int p, int s{|Navigation:)|}
    {
        P = p;
        S = s;
    }
}",
chosenSymbols: null);
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelection()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a|];
}",
@"class Z
{
    int a;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelection2()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a|]bcdefg;
}",
@"class Z
{
    int abcdefg;

    public Z(int abcdefg{|Navigation:)|}
    {
        this.abcdefg = abcdefg;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelection3()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int abcdef[|g|];
}",
@"class Z
{
    int abcdefg;

    public Z(int abcdefg{|Navigation:)|}
    {
        this.abcdefg = abcdefg;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelectionBeforeIdentifier()
        {
            await TestMissingAsync(
@"class Z
{
    int [||]a;
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelectionAfterIdentifier()
        {
            await TestMissingAsync(
@"class Z
{
    int a[||];
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelectionIdentifierNotSelected()
        {
            await TestMissingAsync(
@"class Z
{
    in[|t|] a;
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestPartialFieldSelectionIdentifierNotSelected2()
        {
            await TestMissingAsync(
@"class Z
{
    int a [|= 3|];
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultiplePartialFieldSelection()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a;
    int b|];
}",
@"class Z
{
    int a;
    int b;

    public Z(int a, int b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultiplePartialFieldSelection2()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a = 2;
    int|] b;
}",
@"class Z
{
    int a = 2;
    int b;

    public Z(int a{|Navigation:)|}
    {
        this.a = a;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultiplePartialFieldSelection3()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a|] = 2, b = 3;
}",
@"class Z
{
    int a = 2, b = 3;

    public Z(int a, int b{|Navigation:)|}
    {
        this.a = a;
        this.b = b;
    }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestMultiplePartialFieldSelection4()
        {
            await TestMissingAsync(
@"class Z
{
    int a = [|2|], b = 3;
}");
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestNoFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|a|] = 2;
}",
@"class Z
{
    int a = 2;

    public Z(int p_a{|Navigation:)|}
    {
        a = p_a;
    }
}", options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestCommonFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|s_a|] = 2;
}",
@"class Z
{
    int s_a = 2;

    public Z(int p_a{|Navigation:)|}
    {
        s_a = p_a;
    }
}", options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSpecifiedNamingStyle()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|field_a|] = 2;
}",
@"class Z
{
    int field_a = 2;

    public Z(int p_a_End{|Navigation:)|}
    {
        field_a = p_a_End;
    }
}", options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix, LanguageNames.CSharp));
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSpecifiedAndCommonFieldNamingStyle()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|field_s_a|] = 2;
}",
@"class Z
{
    int field_s_a = 2;

    public Z(int p_a_End{|Navigation:)|}
    {
        field_s_a = p_a_End;
    }
}", options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix, LanguageNames.CSharp));
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSpecifiedAndCommonFieldNamingStyle2()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|s_field_a|] = 2;
}",
@"class Z
{
    int s_field_a = 2;

    public Z(int p_a_End{|Navigation:)|}
    {
        s_field_a = p_a_End;
    }
}", options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix, LanguageNames.CSharp));
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestBaseNameEmpty()
        {
            await TestMissingAsync(
@"class Z
{
    int [|field__End|] = 2;
}", new TestParameters(options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp)));
        }

        [WorkItem(36741, "https://github.com/dotnet/roslyn/issues/36741")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestSomeBaseNamesEmpty()
        {
            await TestInRegularAndScriptAsync(
@"class Z
{
    int [|s_field_a = 2;
    int field__End |]= 3;
}",
@"class Z
{
    int s_field_a = 2;
    int field__End = 3;

    public Z(int p_a{|Navigation:)|}
    {
        s_field_a = p_a;
    }
}", options: options.MergeStyles(options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp));
        }
    }
}
