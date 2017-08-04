// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddParameter;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddParameter
{
    public class AddParameterTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddParameterCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMissingWithImplicitConstructor()
        {
            await TestMissingAsync(
@"
class C
{
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestOnEmptyConstructor()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C() { }
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}",
@"
class C
{
    public C(int v) { }
}

class D
{
    void M()
    {
        new C(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNamedArg()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C() { }
}

class D
{
    void M()
    {
        new C([|p|]: 1);
    }
}",
@"
class C
{
    public C(int p) { }
}

class D
{
    void M()
    {
        new C(p: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMissingWithConstructorWithSameNumberOfParams()
        {
            await TestMissingAsync(
@"
class C
{
    public C(bool b) { }
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestAddBeforeMatchingArg()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](true, 1);
    }
}",
@"
class C
{
    public C(bool v, int i) { }
}

class D
{
    void M()
    {
        new C(true, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestAddAfterMatchingConstructorParam()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](1, true);
    }
}",
@"
class C
{
    public C(int i, bool v) { }
}

class D
{
    void M()
    {
        new C(1, true);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestParams1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(params int[] i) { }
}

class D
{
    void M()
    {
        new C([|true|], 1);
    }
}",
@"
class C
{
    public C(bool v, params int[] i) { }
}

class D
{
    void M()
    {
        new C(true, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestParams2()
        {
            await TestMissingAsync(
@"
class C
{
    public C(params int[] i) { }
}

class D
{
    void M()
    {
        new [|C|](1, true);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](true, 0, 0);
    }
}",
@"
class C
{
    public C(bool v,
             int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(true, 0, 0);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, true, 0);
    }
}",
@"
class C
{
    public C(int i,
             bool v,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(0, true, 0);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters3()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, 0, true);
    }
}",
@"
class C
{
    public C(int i,
             /* goo */ int j,
             bool v)
    {

    }

    private void Goo()
    {
        new C(0, 0, true);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters4()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](true, 0, 0);
    }
}",
@"
class C
{
    public C(
        bool v,
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(true, 0, 0);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters5()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, true, 0);
    }
}",
@"
class C
{
    public C(
        int i,
        bool v,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(0, true, 0);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters6()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, 0, true);
    }
}",
@"
class C
{
    public C(
        int i,
        /* goo */ int j,
        bool v)
    {

    }

    private void Goo()
    {
        new C(0, 0, true);
    }
}",
ignoreTrivia: false);
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNullArg1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](null, 1);
    }
}",
@"
class C
{
    public C(object p, int i) { }
}

class D
{
    void M()
    {
        new C(null, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNullArg2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(string s) { }
}

class D
{
    void M()
    {
        new [|C|](null, 1);
    }
}",
@"
class C
{
    public C(string s, int v) { }
}

class D
{
    void M()
    {
        new C(null, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestDefaultArg1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](default, 1);
    }
}",
@"
class C
{
    public C(int i, int v) { }
}

class D
{
    void M()
    {
        new C(default, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestDefaultArg2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(string s) { }
}

class D
{
    void M()
    {
        new [|C|](default, 1);
    }
}",
@"
class C
{
    public C(string s, int v) { }
}

class D
{
    void M()
    {
        new C(default, 1);
    }
}");
        }
    }
}
