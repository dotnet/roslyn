// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseDeconstruction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDeconstruction
{
    public class UseDeconstructionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public UseDeconstructionTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseDeconstructionDiagnosticAnalyzer(), new CSharpUseDeconstructionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestVar()
        {
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        /*1*/(/*2*/int/*3*/ name, /*4*/int/*5*/ age)/*6*/ [|t1|] = GetPerson();
        Console.WriteLine(/*7*/t1.name/*8*/ + "" "" + /*9*/t1.age/*10*/);
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        /*1*/(/*2*/int/*3*/ name, /*4*/int/*5*/ age)/*6*/ = GetPerson();
        Console.WriteLine(/*7*/name/*8*/ + "" "" + /*9*/age/*10*/);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [WorkItem(25260, "https://github.com/dotnet/roslyn/issues/25260")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotWithDefaultLiteralInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        (string name, int age) [|person|] = default;
        Console.WriteLine(person.name + "" "" + person.age);
    }
}", new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithDefaultExpressionInitializer()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        (string name, int age) [|person|] = default((string, int));
        Console.WriteLine(person.name + "" "" + person.age);
    }
}",
@"class C
{
    void M()
    {
        (string name, int age) = default((string, int));
        Console.WriteLine(name + "" "" + age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotWithImplicitConversionFromNonTuple()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        (string name, int age) [|person|] = new Person();
        Console.WriteLine(person.name + "" "" + person.age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithExplicitImplicitConversionFromNonTuple()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        (string name, int age) [|person|] = ((string, int))new Person();
        Console.WriteLine(person.name + "" "" + person.age);
    }
}",
@"class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        (string name, int age) = ((string, int))new Person();
        Console.WriteLine(name + "" "" + age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestNotWithImplicitConversionFromNonTupleInForEach()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        foreach ((string name, int age) [|person|] in new Person[] { })
            Console.WriteLine(person.name + "" "" + person.age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithExplicitImplicitConversionFromNonTupleInForEach()
        {
            await TestInRegularAndScript1Async(
@"using System.Linq;
class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        foreach ((string name, int age) [|person|] in new Person[] { }.Cast<(string, int)>())
            Console.WriteLine(person.name + "" "" + person.age);
    }
}",
@"using System.Linq;
class C
{
    class Person
    {
        public static implicit operator (string, int)(Person person) => default;
    }

    void M()
    {
        foreach ((string name, int age) in new Person[] { }.Cast<(string, int)>())
            Console.WriteLine(name + "" "" + age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithTupleLiteralConversion()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        (object name, double age) [|person|] = (null, 0);
        Console.WriteLine(person.name + "" "" + person.age);
    }
}",
@"class C
{
    void M()
    {
        (object name, double age) = (null, 0);
        Console.WriteLine(name + "" "" + age);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithImplicitTupleConversion()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        (object name, double age) [|person|] = GetPerson();
        Console.WriteLine(person.name + "" "" + person.age);
    }

    (string name, int age) GetPerson() => default;
}",
@"class C
{
    void M()
    {
        (object name, double age) = GetPerson();
        Console.WriteLine(name + "" "" + age);
    }

    (string name, int age) GetPerson() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestWithImplicitTupleConversionInForEach()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
class C
{
    void M()
    {
        foreach ((object name, double age) [|person|] in GetPeople())
            Console.WriteLine(person.name + "" "" + person.age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}",
@"using System.Collections.Generic;
class C
{
    void M()
    {
        foreach ((object name, double age) in GetPeople())
            Console.WriteLine(name + "" "" + age);
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}");
        }

        [WorkItem(27251, "https://github.com/dotnet/roslyn/issues/27251")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestEscapedContextualKeywordAsTupleName()
        {
            await TestInRegularAndScript1Async(
@"using System.Collections.Generic;
class C
{
    void M()
    {
        var collection = new List<(int position, int @delegate)>();
        foreach (var it[||]em in collection)
        {
            // Do something
        }
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}",
@"using System.Collections.Generic;
class C
{
    void M()
    {
        var collection = new List<(int position, int @delegate)>();
        foreach (var (position, @delegate) in collection)
        {
            // Do something
        }
    }

    IEnumerable<(string name, int age)> GetPeople() => default;
}");
        }

        [Fact]
        [WorkItem(42770, "https://github.com/dotnet/roslyn/issues/42770")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
        public async Task TestPreserveAwait()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        [Goo]
        await foreach (var [|t|] in Sequence())
        {
            Console.WriteLine(t.x + t.y);
        }
    }

    static async IAsyncEnumerable<(int x, int y)> Sequence()
    {
        yield return (0, 0);
        await Task.Yield();
    }
}" + IAsyncEnumerable,
@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        [Goo]
        await foreach (var (x, y) in Sequence())
        {
            Console.WriteLine(x + y);
        }
    }

    static async IAsyncEnumerable<(int x, int y)> Sequence()
    {
        yield return (0, 0);
        await Task.Yield();
    }
}" + IAsyncEnumerable);
        }
    }
}
