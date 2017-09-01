// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseDeconstruction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDeconstruction
{
    public class UseDeconstructionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseDeconstructionDiagnosticAnalyzer(), new CSharpUseDeconstructionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (name, age) = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfNameInInnerScope()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        {
            int age;
        }
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfNameInOuterScope()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int age;

    void M()
    {
        var [|t1|] = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestUpdateReference()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1.name + "" "" + t1.age);
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (name, age) = GetPerson();
        Console.WriteLine(name + "" "" + age);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestTupleType()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        (int name, int age) [|t1|] = GetPerson();
        Console.WriteLine(t1.name + "" "" + t1.age);
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        (int name, int age) = GetPerson();
        Console.WriteLine(name + "" "" + age);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestVarInForEach()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        foreach (var [|t1|] in GetPeople())
            Console.WriteLine(t1.name + "" "" + t1.age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        foreach (var (name, age) in GetPeople())
            Console.WriteLine(name + "" "" + age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestTupleTypeInForEach()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        foreach ((string name, int age) [|t1|] in GetPeople())
            Console.WriteLine(t1.name + "" "" + t1.age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        foreach ((string name, int age) in GetPeople())
            Console.WriteLine(name + "" "" + age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var {|FixAllInDocument:t1|} = GetPerson();
        var t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (name, age) = GetPerson();
        var t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var {|FixAllInDocument:t1|} = GetPerson();
    }

    void M2()
    {
        var t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (name, age) = GetPerson();
    }

    void M2()
    {
        var (name, age) = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestFixAll3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        (string name1, int age1) {|FixAllInDocument:t1|} = GetPerson();
        (string name2, int age2) t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        (string name1, int age1) = GetPerson();
        (string name2, int age2) = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestFixAll4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        (string name, int age) {|FixAllInDocument:t1|} = GetPerson();
        (string name, int age) t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        (string name, int age) = GetPerson();
        (string name, int age) t2 = GetPerson();
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfDefaultTupleNameWithVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
    }

    (string, int) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithUserNamesThatMatchDefaultTupleNameWithVar1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
    }

    (string Item1, int Item2) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (Item1, Item2) = GetPerson();
    }

    (string Item1, int Item2) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithUserNamesThatMatchDefaultTupleNameWithVar2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1.Item1);
    }

    (string Item1, int Item2) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        var (Item1, Item2) = GetPerson();
        Console.WriteLine(Item1);
    }

    (string Item1, int Item2) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfDefaultTupleNameWithTupleType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        (string, int) [|t1|] = GetPerson();
    }

    (string, int) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfTupleIsUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfTupleMethodIsUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1.ToString());
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfTupleDefaultElementNameUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1.Item1);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotIfTupleRandomNameUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var [|t1|] = GetPerson();
        Console.WriteLine(t1.Unknown);
    }

    (string name, int age) GetPerson() => default;
}");
        }
    }
}
