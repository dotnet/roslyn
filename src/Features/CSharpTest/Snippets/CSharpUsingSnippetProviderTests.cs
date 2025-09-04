// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpUsingSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "using";

    [Fact]
    public Task InsertUsingSnippetInMethodTest()
        => VerifySnippetAsync("""
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
                    using ({|0:resource|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertUsingSnippetInGlobalContextTest()
        => VerifySnippetAsync("""
            $$
            """, """
            using ({|0:resource|})
            {
                $$
            }
            """);

    [Fact]
    public Task NoUsingSnippetInBusingNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);

    [Fact]
    public Task NoUsingSnippetInFileScopedNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            $$
            """);

    [Fact]
    public Task InsertUsingSnippetInConstructorTest()
        => VerifySnippetAsync("""
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
                    using ({|0:resource|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task NoUsingSnippetInTypeBodyTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                $$
            }
            """);

    [Fact]
    public Task InsertUsingSnippetInLocalFunctionTest()
        => VerifySnippetAsync("""
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
                        using ({|0:resource|})
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task InsertUsingSnippetInAnonymousFunctionTest()
        => VerifySnippetAsync("""
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
                        using ({|0:resource|})
                        {
                            $$
                        }
                    };
                }
            }
            """);

    [Fact]
    public Task InsertUsingSnippetInParenthesizedLambdaExpressionTest()
        => VerifySnippetAsync("""
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
                        using ({|0:resource|})
                        {
                            $$
                        }
                    };
                }
            }
            """);
}
