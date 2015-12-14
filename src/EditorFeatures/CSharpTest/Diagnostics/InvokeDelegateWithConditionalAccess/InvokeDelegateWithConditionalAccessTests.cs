// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess
{
    public class InvokeDelegateWithConditionalAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new InvokeDelegateWithConditionalAccessAnalyzer(),
                new InvokeDelegateWithConditionalAccessCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task Test1()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInvertedIf()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (null != v)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestIfWithNoBraces()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (null != v)
            v();
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestWithComplexExpression()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        bool b = true;
        [||]var v = b ? a : null;
        if (v != null)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        bool b = true;
        (b ? a : null)?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingWithElseClause()
        {
            await TestMissingAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
        else {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingWithMultipleVariables()
        {
            await TestMissingAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a, x = a;
        if (v != null)
        {
            v();
        }
        else {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingIfUsedOutside()
        {
            await TestMissingAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }

        v = null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestSimpleForm1()
        {
            await TestAsync(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInElseClause1()
        {
            await TestAsync(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else
        {
            this.E?.Invoke(this, EventArgs.Empty);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInElseClause2()
        {
            await TestAsync(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
            this.E(this, EventArgs.Empty);
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestTrivia1()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        a?.Invoke();
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestTrivia2()
        {
            await TestAsync(
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        [||]if (a != null)
        {
            a();
        }
    }
}",
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        a?.Invoke();
    }
}", compareTokens: false);
        }
    }
}