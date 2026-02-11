// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpUnsafeSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "unsafe";

    [Fact]
    public Task InsertUnsafeSnippetInMethodTest()
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
                    unsafe
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertUnsafeSnippetInGlobalContextTest()
        => VerifySnippetAsync("""
            $$
            """, """
            unsafe
            {
                $$
            }
            """);

    [Fact]
    public Task NoUnsafeSnippetInBlockNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);

    [Fact]
    public Task NoUnsafeSnippetInFileScopedNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            $$
            """);

    [Fact]
    public Task InsertUnsafeSnippetInConstructorTest()
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
                    unsafe
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task NoUnsafeSnippetInTypeBodyTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                $$
            }
            """);

    [Fact]
    public Task InsertUnsafeSnippetInLocalFunctionTest()
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
                        unsafe
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task InsertUnsafeSnippetInAnonymousFunctionTest()
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
                        unsafe
                        {
                            $$
                        }
                    };
                }
            }
            """);

    [Fact]
    public Task InsertUnsafeSnippetInParenthesizedLambdaExpressionTest()
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
                        unsafe
                        {
                            $$
                        }
                    };
                }
            }
            """);
}
