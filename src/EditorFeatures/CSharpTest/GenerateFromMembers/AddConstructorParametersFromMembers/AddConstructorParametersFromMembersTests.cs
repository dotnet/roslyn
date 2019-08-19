// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateFromMembers.AddConstructorParameters
{
    public class AddConstructorParametersFromMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new AddConstructorParametersFromMembersCodeRefactoringProvider();

        private readonly NamingStylesTestOptionSets options = new NamingStylesTestOptionSets(LanguageNames.CSharp);

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAdd1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    [|int i;
    string s;|]

    public Program(int i)
    {
        this.i = i;
    }
}",
@"using System.Collections.Generic;

class Program
{
    int i;
    string s;

    public Program(int i, string s)
    {
        this.i = i;
        this.s = s;
    }
}", title: string.Format(FeaturesResources.Add_parameters_to_0, "Program(int)"));
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddOptional1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    [|int i;
    string s;|]

    public Program(int i)
    {
        this.i = i;
    }
}",
@"using System.Collections.Generic;

class Program
{
    int i;
    string s;

    public Program(int i, string s = null)
    {
        this.i = i;
        this.s = s;
    }
}", index: 1, title: string.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(int)"));
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddToConstructorWithMostMatchingParameters1()
        {
            // behavior change with 33603, now all constructors offered
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    [|int i;
    string s;
    bool b;|]

    public Program(int i)
    {
        this.i = i;
    }

    public Program(int i, string s) : this(i)
    {
        this.s = s;
    }
}",
@"using System.Collections.Generic;

class Program
{
    int i;
    string s;
    bool b;

    public Program(int i)
    {
        this.i = i;
    }

    public Program(int i, string s, bool b) : this(i)
    {
        this.s = s;
        this.b = b;
    }
}", index: 1, title: string.Format(FeaturesResources.Add_to_0, "Program(int, string)"));
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddOptionalToConstructorWithMostMatchingParameters1()
        {
            // Behavior change with #33603, now all constructors are offered
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Program
{
    [|int i;
    string s;
    bool b;|]

    public Program(int i)
    {
        this.i = i;
    }

    public Program(int i, string s) : this(i)
    {
        this.s = s;
    }
}",
@"using System.Collections.Generic;

class Program
{
    int i;
    string s;
    bool b;

    public Program(int i)
    {
        this.i = i;
    }

    public Program(int i, string s, bool b = false) : this(i)
    {
        this.s = s;
        this.b = b;
    }
}", index: 3, title: string.Format(FeaturesResources.Add_to_0, "Program(int, string)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSmartTagDisplayText1()
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
string.Format(FeaturesResources.Add_parameters_to_0, "Program(bool)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSmartTagDisplayText2()
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
string.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(bool)"),
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTuple()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int, string) i;
    (string, int) s;|]

    public Program((int, string) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int, string) i;
    (string, int) s;

    public Program((int, string) i, (string, int) s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int a, string b) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int a, string b) i;
    (string c, int d) s;

    public Program((int a, string b) i, (string c, int d) s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleWithDifferentNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int e, string f) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int e, string f) i, (string c, int d) s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptionalCSharp7()
        {
            await TestAsync(
@"class Program
{
    [|(int, string) i;
    (string, int) s;|]

    public Program((int, string) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int, string) i;
    (string, int) s;

    public Program((int, string) i, (string, int) s = default((string, int)))
    {
        this.i = i;
        this.s = s;
    }
}",
index: 1, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptional()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int, string) i;
    (string, int) s;|]

    public Program((int, string) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int, string) i;
    (string, int) s;

    public Program((int, string) i, (string, int) s = default)
    {
        this.i = i;
        this.s = s;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptionalWithNames_CSharp7()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int a, string b) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int a, string b) i;
    (string c, int d) s;

    public Program((int a, string b) i, (string c, int d) s = default((string c, int d)))
    {
        this.i = i;
        this.s = s;
    }
}",
parseOptions: TestOptions.Regular7,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptionalWithNamesCSharp7()
        {
            await TestAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int a, string b) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int a, string b) i;
    (string c, int d) s;

    public Program((int a, string b) i, (string c, int d) s = default((string c, int d)))
    {
        this.i = i;
        this.s = s;
    }
}",
index: 1, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptionalWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int a, string b) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int a, string b) i;
    (string c, int d) s;

    public Program((int a, string b) i, (string c, int d) s = default)
    {
        this.i = i;
        this.s = s;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleOptionalWithDifferentNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int e, string f) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    [|(int a, string b) i;
    (string c, int d) s;|]

    public Program((int e, string f) i, (string c, int d) s = default)
    {
        this.i = i;
        this.s = s;
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleWithNullable()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(int?, bool?) i;
    (byte?, long?) s;|]

    public Program((int?, bool?) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (int?, bool?) i;
    (byte?, long?) s;

    public Program((int?, bool?) i, (byte?, long?) s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestTupleWithGenericss()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    [|(List<int>, List<bool>) i;
    (List<byte>, List<long>) s;|]

    public Program((List<int>, List<bool>) i)
    {
        this.i = i;
    }
}",
@"class Program
{
    (List<int>, List<bool>) i;
    (List<byte>, List<long>) s;

    public Program((List<int>, List<bool>) i, (List<byte>, List<long>) s)
    {
        this.i = i;
        this.s = s;
    }
}");
        }

        [WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddParamtersToConstructorBySelectOneMember()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;
    [|(List<byte>, List<long>) s;|]
    int j;

    public C(int i, int j)
    {
        this.i = i;
        this.j = j;
    }
}",
@"
class C
{
    int i;
    (List<byte>, List<long>) s;
    int j;

    public C(int i, int j, (List<byte>, List<long>) s)
    {
        this.i = i;
        this.j = j;
        this.s = s;
    }
}");
        }

        [WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestParametersAreStillRightIfMembersAreOutOfOrder()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    [|int i;
    int k;
    int j;|]

    public C(int i, int j)
    {
        this.i = i;
        this.j = j;
    }
}",
@"
class C
{
    int i;
    int k;
    int j;

    public C(int i, int j, int k)
    {
        this.i = i;
        this.j = j;
        this.k = k;
    }
}");
        }

        [WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMissingIfFieldsAlreadyExistingInConstructor()
        {
            await TestMissingAsync(
@"
class C
{
    [|string _barBar;
    int fooFoo;|]
    public C(string barBar, int fooFoo)
    {
    }
}"
            );
        }

        [WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMissingIfPropertyAlreadyExistingInConstructor()
        {
            await TestMissingAsync(
@"
class C
{
    [|string bar;
    int HelloWorld { get; set; }|]
    public C(string bar, int helloWorld)
    {
    }
}"
            );

        }

        [WorkItem(28775, "https://github.com/dotnet/roslyn/issues/28775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestNormalProperty()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    [|int i;
    int Hello { get; set; }|]
    public C(int i)
    {
    }
}",
@"
class C
{
    int i;
    int Hello { get; set; }
    public C(int i, int hello)
    {
        Hello = hello;
    }
}"
            );
        }

        [WorkItem(33602, "https://github.com/dotnet/roslyn/issues/33602")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestConstructorWithNoParameters()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    [|int i;
    int Hello { get; set; }|]
    public C()
    {
    }
}",
@"
class C
{
    int i;
    int Hello { get; set; }
    public C(int i, int hello)
    {
        this.i = i;
        Hello = hello;
    }
}"
            );
        }

        [WorkItem(33602, "https://github.com/dotnet/roslyn/issues/33602")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestDefaultConstructor()
        {
            await TestMissingAsync(
@"
class C
{
    [|int i;|]
    int Hello { get; set; }
}");
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestPartialSelected()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;
    int [|j|];
    public C(int i)
    {
    }
}",
@"
class C
{
    int i;
    int j;
    public C(int i, int j)
    {
        this.j = j;
    }
}"
            );
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestPartialMultipleSelected()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;
    int [|j;
    int k|];
    public C(int i)
    {
    }
}",
@"
class C
{
    int i;
    int j;
    int k;
    public C(int i, int j, int k)
    {
        this.j = j;
        this.k = k;
    }
}"
            );
        }

        [WorkItem(33601, "https://github.com/dotnet/roslyn/issues/33601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestPartialMultipleSelected2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;
    int [|j;
    int |]k;
    public C(int i)
    {
    }
}",
@"
class C
{
    int i;
    int j;
    int k;
    public C(int i, int j)
    {
        this.j = j;
    }
}"
            );
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_FirstofThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int l;
    public C(int i, int l)
    {
        this.l = l;
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 0, title: string.Format(FeaturesResources.Add_to_0, "C(int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_SecondOfThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int l;
    public C(int i)
    {
    }
    public C(int i, int j, int l)
    {
        this.l = l;
    }
    public C(int i, int j, int k)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 1, title: string.Format(FeaturesResources.Add_to_0, "C(int, int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_ThirdOfThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";

            var expected =
@"
class C
{
    int l;
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k, int l)
    {
        this.l = l;
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 2, title: string.Format(FeaturesResources.Add_to_0, "C(int, int, int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_FirstOptionalOfThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int l;
    public C(int i, int l = 0)
    {
        this.l = l;
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 3, title: string.Format(FeaturesResources.Add_to_0, "C(int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_SecondOptionalOfThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j, int l = 0)
    {
        this.l = l;
    }
    public C(int i, int j, int k)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 4, title: string.Format(FeaturesResources.Add_to_0, "C(int, int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_ThirdOptionalOfThree()
        {
            var source =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int [|l|];
    public C(int i)
    {
    }
    public C(int i, int j)
    {
    }
    public C(int i, int j, int k, int l = 0)
    {
        this.l = l;
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 5, title: string.Format(FeaturesResources.Add_to_0, "C(int, int, int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_OneMustBeOptional()
        {
            var source =
@"
class C
{
    int [|l|];

    // index 0 as required
    // index 2 as optional
    public C(int i)
    {
    }

    // index 3 as optional
    public C(int i, double j = 0)
    {
    }

    // index 1 as required
    // index 4 as optional
    public C(int i, double j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int [|l|];

    // index 0 as required
    // index 2 as optional
    public C(int i)
    {
    }

    // index 3 as optional
    public C(int i, double j = 0)
    {
    }

    // index 1 as required
    // index 4 as optional
    public C(int i, double j, int k, int l)
    {
        this.l = l;
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 1, title: string.Format(FeaturesResources.Add_to_0, "C(int, double, int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_OneMustBeOptional2()
        {
            var source =
@"
class C
{
    int [|l|];

    // index 0, and 2 as optional
    public C(int i)
    {
    }

    // index 3 as optional
    public C(int i, double j = 0)
    {
    }

    // index 1, and 4 as optional
    public C(int i, double j, int k)
    {
    }
}";
            var expected =
@"
class C
{
    int [|l|];

    // index 0, and 2 as optional
    public C(int i)
    {
    }

    // index 3 as optional
    public C(int i, double j = 0, int l = 0)
    {
        this.l = l;
    }

    // index 1, and 4 as optional
    public C(int i, double j, int k)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 3, title: string.Format(FeaturesResources.Add_to_0, "C(int, double)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_AllMustBeOptional()
        {
            var source =
@"
class C
{
    int [|p|];
    public C(int i = 0)
    {
    }
    public C(double j, int k = 0)
    {
    }
    public C(int l, double m, int n = 0)
    {
    }
}";
            var expected =
@"
class C
{
    int [|p|];
    public C(int i = 0, int p = 0)
    {
        this.p = p;
    }
    public C(double j, int k = 0)
    {
    }
    public C(int l, double m, int n = 0)
    {
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 0, title: string.Format(FeaturesResources.Add_to_0, "C(int)"));
        }

        [WorkItem(33603, "https://github.com/dotnet/roslyn/issues/33603")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestMultipleConstructors_AllMustBeOptional2()
        {
            var source =
@"
class C
{
    int [|p|];
    public C(int i = 0)
    {
    }
    public C(double j, int k = 0)
    {
    }
    public C(int l, double m, int n = 0)
    {
    }
}";
            var expected =
@"
class C
{
    int [|p|];
    public C(int i = 0)
    {
    }
    public C(double j, int k = 0)
    {
    }
    public C(int l, double m, int n = 0, int p = 0)
    {
        this.p = p;
    }
}";
            await TestInRegularAndScriptAsync(source, expected, index: 2, title: string.Format(FeaturesResources.Add_to_0, "C(int, double, int)"));
        }

        [WorkItem(33623, "https://github.com/dotnet/roslyn/issues/33623")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestDeserializationConstructor()
        {
            await TestMissingAsync(
@"
using System;
using System.Runtime.Serialization;
 
class C : ISerializable
{
    int [|i|];

    private C(SerializationInfo info, StreamingContext context)
    {
    }
}
");
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestNoFieldNamingStyle_ParameterPrefixAndSuffix()
        {
            var source =
@"
class C
{
    private int [|v|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int v;
    public C(int p_v_End)
    {
        v = p_v_End;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix);
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestCommonFieldNamingStyle()
        {
            var source =
@"
class C
{
    private int [|t_v|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int t_v;
    public C(int p_v)
    {
        t_v = p_v;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSpecifiedFieldNamingStyle()
        {
            var source =
@"
class C
{
    private int [|field_v|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int field_v;
    public C(int p_v)
    {
        field_v = p_v;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.MergeStyles(
                options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp));
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSpecifiedAndCommonFieldNamingStyle()
        {
            var source =
@"
class C
{
    private int [|field_s_v|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int field_s_v;
    public C(int p_v)
    {
        field_s_v = p_v;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.MergeStyles(
                options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp));
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSpecifiedAndCommonFieldNamingStyle2()
        {
            var source =
@"
class C
{
    private int [|s_field_v|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int s_field_v;
    public C(int p_v)
    {
        s_field_v = p_v;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.MergeStyles(
                options.FieldNamesAreCamelCaseWithFieldUnderscorePrefix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp));
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestBaseNameEmpty()
        {
            var source =
@"
class C
{
    private int [|field__End|];
    public C()
    {
    }
}
";
            await TestMissingAsync(source, parameters: new TestParameters(options: options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix));
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestSomeBaseNamesAreEmpty()
        {
            var source =
@"
class C
{
    private int [|field_test_End;
    private int field__End|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int field_test_End;
    private int field__End;
    public C(int p_test)
    {
        field_test_End = p_test;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.MergeStyles(
                options.FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix, options.ParameterNamesAreCamelCaseWithPUnderscorePrefix, LanguageNames.CSharp));
        }

        [WorkItem(35775, "https://github.com/dotnet/roslyn/issues/35775")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestManyCommonPrefixes()
        {
            var source =
@"
class C
{
    private int [|______test|];
    public C()
    {
    }
}
";

            var expected =
@"
class C
{
    private int ______test;
    public C(int p_test)
    {
        ______test = p_test;
    }
}
";
            await TestInRegularAndScriptAsync(source, expected, index: 0, options: options.ParameterNamesAreCamelCaseWithPUnderscorePrefix);
        }
    }
}
