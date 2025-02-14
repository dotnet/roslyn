// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpSwitchSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "switch";

    [Fact]
    public async Task InsertSwitchSnippetInMethodTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    switch ({|0:switch_on|})
                    {
                        default:
                            break;$$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertSwitchSnippetInGlobalContextTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            switch ({|0:switch_on|})
            {
                default:
                    break;$$
            }
            """);
    }

    [Fact]
    public async Task NoSwitchSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoSwitchSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            $$
            """);
    }

    [Fact]
    public async Task InsertSwitchSnippetInConstructorTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """, """
            class Program
            {
                public Program()
                {
                    switch ({|0:switch_on|})
                    {
                        default:
                            break;$$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoSwitchSnippetInTypeBodyTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertSwitchSnippetInLocalFunctionTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    void LocalFunction()
                    {
                        $$
                    }
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    void LocalFunction()
                    {
                        switch ({|0:switch_on|})
                        {
                            default:
                                break;$$
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertSwitchSnippetInAnonymousFunctionTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var action = delegate()
                    {
                        $$
                    };
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    var action = delegate()
                    {
                        switch ({|0:switch_on|})
                        {
                            default:
                                break;$$
                        }
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task InsertSwitchSnippetInParenthesizedLambdaExpressionTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var action = () =>
                    {
                        $$
                    };
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    var action = () =>
                    {
                        switch ({|0:switch_on|})
                        {
                            default:
                                break;$$
                        }
                    };
                }
            }
            """);
    }
}
