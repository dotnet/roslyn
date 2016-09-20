// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseObjectInitializer
{
    public partial class UseObjectInitializerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseObjectInitializerDiagnosticAnalyzer(),
                new CSharpUseObjectInitializerCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnVariableDeclarator()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        var c = [||]new C();
        c.i = 1;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        var c = new C()
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnAssignmentExpression()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        C c = null;
        c = [||]new C();
        c.i = 1;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        C c = null;
        c = new C()
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestStopOnDuplicateMember()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        var c = [||]new C();
        c.i = 1;
        c.i = 2;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        var c = new C()
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
            await TestAsync(
@"
class C
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
@"
class C
{
    int i;
    int j;
    void M()
    {
        C[] array;

        array[0] = new C()
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
            await TestAsync(
@"
class C
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
@"
class C
{
    int i;
    int j;
    void M()
    {
        var c = new C()
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
            await TestMissingAsync(
@"
class C
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
        public async Task TestFixAllInDocument()
        {
            await TestAsync(
@"
class C
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
@"
class C
{
    int i;
    int j;
    void M()
    {
        C[] array;

        array[0] = new C()
        {
            i = 1,
            j = 2
        };
        array[1] = new C()
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
            await TestAsync(
@"
class C
{
    int i;
    int j;
    void M()
    {
        var c = [||]new C();
        c.i = 1; // Foo
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
        var c = new C()
        {
            i = 1, // Foo
            j = 2 // Bar
        };
    }
}",
compareTokens: false);
        }
    }
}