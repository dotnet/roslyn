// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess;

[Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
public sealed partial class InvokeDelegateWithConditionalAccessTests
{
    [Fact]
    public Task TestFixAllInDocument1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument3()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument4()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument5()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument6()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                    a?.Invoke();
                }
            }
            """);
}
