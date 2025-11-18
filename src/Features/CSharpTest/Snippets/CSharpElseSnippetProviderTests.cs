// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpElseSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "else";

    [Fact]
    public Task InsertElseSnippetInMethodTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    if (true)
                    {
                    }
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    if (true)
                    {
                    }
                    else
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task NoElseSnippetInMethodWithoutIfStatementTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);

    [Fact]
    public Task InsertElseSnippetGlobalTest()
        => VerifySnippetAsync("""
            if (true)
            {
            }
            $$
            """, """
            if (true)
            {
            }
            else
            {
                $$
            }
            """);

    [Fact]
    public Task NoElseSnippetInBlockNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                if (true)
                {
                }
                $$
            }
            """);

    [Fact]
    public Task NoElseSnippetInFileScopedNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            if (true)
            {
            }
            $$
            """);

    [Fact]
    public Task InsertElseSnippetInConstructorTest()
        => VerifySnippetAsync("""
            class Program
            {
                public Program()
                {
                    if (true)
                    {
                    }
                    $$
                }
            }
            """, """
            class Program
            {
                public Program()
                {
                    if (true)
                    {
                    }
                    else
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertElseSnippetInLocalFunctionTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        if (true)
                        {

                        }
                        $$
                    }
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        if (true)
                        {

                        }
                        else
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task InsertElseSnippetSingleLineIfWithBlockTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    if (true) {}
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    if (true) {}
                    else
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertElseSnippetSingleLineIfTest()
        => VerifySnippetAsync("""
            using System;

            class Program
            {
                public void Method()
                {
                    if (true) Console.WriteLine(5);
                    $$
                }
            }
            """, """
            using System;

            class Program
            {
                public void Method()
                {
                    if (true) Console.WriteLine(5);
                    else
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertElseSnippetNestedIfTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    if (true)
                    {
                        if (true)
                        {
                        }
                    }   
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    if (true)
                    {
                        if (true)
                        {
                        }
                    }
                    else
                    {
                        $$
                    }
                }
            }
            """);
}
