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
    public async Task InsertElseSnippetInMethodTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoElseSnippetInMethodWithoutIfStatementTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertElseSnippetGlobalTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoElseSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                if (true)
                {
                }
                $$
            }
            """);
    }

    [Fact]
    public async Task NoElseSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;
            if (true)
            {
            }
            $$
            """);
    }

    [Fact]
    public async Task InsertElseSnippetInConstructorTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertElseSnippetInLocalFunctionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertElseSnippetSingleLineIfWithBlockTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertElseSnippetSingleLineIfTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertElseSnippetNestedIfTest()
    {
        await VerifySnippetAsync("""
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
}
