// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateFromMembers.AddConstructorParameters
{
    public class AddConstructorParametersFromMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new AddConstructorParametersFromMembersCodeRefactoringProvider();

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
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
}");
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
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
}",
index: 1);
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddToConstructorWithMostMatchingParameters1()
        {
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
}");
        }

        [WorkItem(308077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)]
        public async Task TestAddOptionalToConstructorWithMostMatchingParameters1()
        {
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
}",
index: 1);
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
    }
}
