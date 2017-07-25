// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateConstructorFromMembers
{
    public class GenerateConstructorFromMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateConstructorFromMembersCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

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

    public Z(int a)
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

    public Z(int a) => this . a = a ;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithNoneEnforcement));
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

    public Z(int a) => this . a = a ;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement));
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

    public Z(int a, int b)
    {
        this . a = a ;
        this . b = b ;
    }
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement));
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

    public Z(int a, string b)
    {
        this.a = a;
        this.b = b;
    }
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

    public Z(string b)
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

    public Z(int a, string b)
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

    public Z(int a, string b)
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

    public Z(int a, string b) : this(a)
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

    public Z(int a, string b)
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
    public Z(int a, string b)
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
    public Z(int a, string b)
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

    public S(int i)
    {
        this.i = i;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructorFromMembers)]
        public async Task TestStruct1()
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
    public S(int i) : this()
    {
        this.i = i;
    }

    int i { get; set; }
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

    public S(int y) : this()
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

    public S(int i) : this()
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

    public Program(int i)
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

    public Program(int yield)
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

    public Z((int, string) a)
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

    public Program(int field)
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

    public Program(int field)
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
    protected Contribution(string title, int number)
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

    public Z(int a)
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

    public Z(int a)
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

    public Z()
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

    public Z(string b, int a)
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

    public Z(int a, string b)
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

    public Z(int a, string b)
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
parameters: new TestParameters(options:
    Option(CodeStyleOptions.PreferThrowExpression, CodeStyleOptions.FalseWithNoneEnforcement)));
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
    protected C(int prop) 
    {
        Prop = prop;
    } 

    public int Prop { get; set; }
}",
options: Option(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }
    }
}
