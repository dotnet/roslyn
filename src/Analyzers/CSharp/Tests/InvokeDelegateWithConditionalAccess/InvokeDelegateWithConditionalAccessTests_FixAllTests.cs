// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
    public partial class InvokeDelegateWithConditionalAccessTests
    {
        [Fact]
        public async Task TestFixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        {|FixAllInDocument:var|} v = a;
        if (v != null)
        {
            v();
        }

        var x = a;
        if (x != null)
        {
            x();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }

        [Fact]
        public async Task TestFixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        {|FixAllInDocument:if|} (v != null)
        {
            v();
        }

        var x = a;
        if (x != null)
        {
            x();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }

        [Fact]
        public async Task TestFixAllInDocument3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            {|FixAllInDocument:v|}();
        }

        var x = a;
        if (x != null)
        {
            x();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }

        [Fact]
        public async Task TestFixAllInDocument4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            v();
        }

        {|FixAllInDocument:var|} x = a;
        if (x != null)
        {
            x();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }

        [Fact]
        public async Task TestFixAllInDocument5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            v();
        }

        var x = a;
        {|FixAllInDocument:if|} (x != null)
        {
            x();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }

        [Fact]
        public async Task TestFixAllInDocument6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            v();
        }

        var x = a;
        if (x != null)
        {
            {|FixAllInDocument:x|}();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();

        a?.Invoke();
    }
}");
        }
    }
}
