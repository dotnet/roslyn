// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseObjectInitializer
{
    public partial class UseObjectInitializerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseObjectInitializerDiagnosticAnalyzer(), new CSharpUseObjectInitializerCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnVariableDeclarator()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        var c = [||]new C();
        c.i = 1;
    }
}",
@"class C
{
    int i;

    void M()
    {
        var c = new C
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestDoNotUpdateAssignmentThatReferencesInitializedValue1Async()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        var c = [||]new C();
        c.i = 1;
        c.i = c.i + 1;
    }
}",
@"class C
{
    int i;

    void M()
    {
        var c = new C
        {
            i = 1
        };
        c.i = c.i + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestDoNotUpdateAssignmentThatReferencesInitializedValue2Async()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        var c = [||]new C();
        c.i = c.i + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestDoNotUpdateAssignmentThatReferencesInitializedValue3Async()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        C c;
        c = [||]new C();
        c.i = 1;
        c.i = c.i + 1;
    }
}",
@"class C
{
    int i;

    void M()
    {
        C c;
        c = new C
        {
            i = 1
        };
        c.i = c.i + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestDoNotUpdateAssignmentThatReferencesInitializedValue4Async()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        C c;
        c = [||]new C();
        c.i = c.i + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnAssignmentExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        C c = null;
        c = [||]new C();
        c.i = 1;
    }
}",
@"class C
{
    int i;

    void M()
    {
        C c = null;
        c = new C
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestStopOnDuplicateMember()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;

    void M()
    {
        var c = [||]new C();
        c.i = 1;
        c.i = 2;
    }
}",
@"class C
{
    int i;

    void M()
    {
        var c = new C
        {
            i = 1
        };
        c.i = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestComplexInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        C[] array;
        array[0] = [||]new C();
        array[0].i = 1;
        array[0].j = 2;
    }
}",
@"class C
{
    int i;
    int j;

    void M()
    {
        C[] array;
        array[0] = new C
        {
            i = 1,
            j = 2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestNotOnCompoundAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        var c = [||]new C();
        c.i = 1;
        c.j += 1;
    }
}",
@"class C
{
    int i;
    int j;

    void M()
    {
        var c = new C
        {
            i = 1
        };
        c.j += 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingWithExistingInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        var c = [||]new C() { i = 1 };
        c.j = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingBeforeCSharp3()
        {
            await TestMissingAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        var c = [||]new C();
        c.j = 1;
    }
}", new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestFixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        var v = {|FixAllInDocument:new|} C(() => {
            var v2 = new C();
            v2.i = 1;
        });
        v.j = 2;
    }
}",
@"class C
{
    int i;
    int j;

    void M()
    {
        var v = new C(() =>
        {
            var v2 = new C
            {
                i = 1
            };
        })
        {
            j = 2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestFixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        var v = {|FixAllInDocument:new|} C();
        v.j = () => {
            var v2 = new C();
            v2.i = 1;
        };
    }
}",
@"class C
{
    int i;
    int j;

    void M()
    {
        var v = new C
        {
            j = () =>
            {
                var v2 = new C
                {
                    i = 1
                };
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestFixAllInDocument3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int i;
    int j;

    void M()
    {
        C[] array;
        array[0] = {|FixAllInDocument:new|} C();
        array[0].i = 1;
        array[0].j = 2;
        array[1] = new C();
        array[1].i = 3;
        array[1].j = 4;
    }
}",
@"class C
{
    int i;
    int j;

    void M()
    {
        C[] array;
        array[0] = new C
        {
            i = 1,
            j = 2
        };
        array[1] = new C
        {
            i = 3,
            j = 4
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;
    int j;
    void M()
    {
        var c = [||]new C();
        c.i = 1; // Goo
        c.j = 2; // Bar
    }
}",
@"
class C
{
    int i;
    int j;
    void M()
    {
        var c = new C
        {
            i = 1, // Goo
            j = 2 // Bar
        };
    }
}");
        }

        [WorkItem(15459, "https://github.com/dotnet/roslyn/issues/15459")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingInNonTopLevelObjectInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C {
	int a;
	C Add(int x) {
		var c = Add([||]new int());
		c.a = 1;
		return c;
	}
}");
        }

        [WorkItem(17853, "https://github.com/dotnet/roslyn/issues/17853")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingForDynamic()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Dynamic;

class C
{
    void Goo()
    {
        dynamic body = [||]new ExpandoObject();
        body.content = new ExpandoObject();
    }
}");
        }

        [WorkItem(17953, "https://github.com/dotnet/roslyn/issues/17953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingAcrossPreprocessorDirective()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Goo
{
    public void M()
    {
        var goo = [||]new Goo();
#if true
        goo.Value = "";
#endif
    }

    public string Value { get; set; }
}");
        }

        [WorkItem(17953, "https://github.com/dotnet/roslyn/issues/17953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestAvailableInsidePreprocessorDirective()
        {
            await TestInRegularAndScript1Async(
@"
public class Goo
{
    public void M()
    {
#if true
        var goo = [||]new Goo();
        goo.Value = "";
#endif
    }

    public string Value { get; set; }
}",
@"
public class Goo
{
    public void M()
    {
#if true
        var goo = new Goo
        {
            Value = "";
        };
#endif
    }

    public string Value { get; set; }
}");
        }

        [WorkItem(19253, "https://github.com/dotnet/roslyn/issues/19253")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestKeepBlankLinesAfter()
        {
            await TestInRegularAndScript1Async(
@"
class Goo
{
    public int Bar { get; set; }
}

class MyClass
{
    public void Main()
    {
        var goo = [||]new Goo();
        goo.Bar = 1;

        int horse = 1;
    }
}",
@"
class Goo
{
    public int Bar { get; set; }
}

class MyClass
{
    public void Main()
    {
        var goo = new Goo
        {
            Bar = 1
        };

        int horse = 1;
    }
}");
        }

        [WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestWithExplicitImplementedInterfaceMembers1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
interface IExample {
    string Name { get; set; }
}

class C : IExample {
    string IExample.Name { get; set; }
}

class MyClass
{
    public void Main()
    {
        IExample e = [||]new C();
        e.Name = string.Empty;
    }
}");
        }

        [WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestWithExplicitImplementedInterfaceMembers2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
interface IExample {
    string Name { get; set; }
    string LastName { get; set; }
}

class C : IExample {
    string IExample.Name { get; set; }
    public string LastName { get; set; }
}

class MyClass
{
    public void Main()
    {
        IExample e = [||]new C();
        e.Name = string.Empty;
        e.LastName = string.Empty;
    }
}");
        }

        [WorkItem(23368, "https://github.com/dotnet/roslyn/issues/23368")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestWithExplicitImplementedInterfaceMembers3()
        {
            await TestInRegularAndScript1Async(
@"
interface IExample {
    string Name { get; set; }
    string LastName { get; set; }
}

class C : IExample {
    string IExample.Name { get; set; }
    public string LastName { get; set; }
}

class MyClass
{
    public void Main()
    {
        IExample e = [||]new C();
        e.LastName = string.Empty;
        e.Name = string.Empty;
    }
}",
@"
interface IExample {
    string Name { get; set; }
    string LastName { get; set; }
}

class C : IExample {
    string IExample.Name { get; set; }
    public string LastName { get; set; }
}

class MyClass
{
    public void Main()
    {
        IExample e = new C
        {
            LastName = string.Empty
        };
        e.Name = string.Empty;
    }
}");
        }
    }
}
