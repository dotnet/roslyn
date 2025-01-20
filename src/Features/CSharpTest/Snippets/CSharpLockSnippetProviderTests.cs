// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpLockSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "lock";

    [Fact]
    public async Task InsertLockSnippetInMethodTest()
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
                    lock ({|0:this|})
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertLockSnippetInGlobalContextTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            lock ({|0:this|})
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoLockSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoLockSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            $$
            """);
    }

    [Fact]
    public async Task InsertLockSnippetInConstructorTest()
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
                    lock ({|0:this|})
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task NoLockSnippetInTypeBodyTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertLockSnippetInLocalFunctionTest()
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
                        lock ({|0:this|})
                        {
                            $$
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertLockSnippetInAnonymousFunctionTest()
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
                        lock ({|0:this|})
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task InsertLockSnippetInParenthesizedLambdaExpressionTest()
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
                        lock ({|0:this|})
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }
}
