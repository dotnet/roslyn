// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpLockSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
{
    protected override string ItemToCommit => "lock";

    [WpfFact]
    public async Task InsertLockSnippetInMethodTest()
    {
        await VerifyCustomCommitProviderAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """, ItemToCommit, """
            class Program
            {
                public void Method()
                {
                    lock (this)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertLockSnippetInGlobalContextTest()
    {
        await VerifyCustomCommitProviderAsync("""
            $$
            """, ItemToCommit, """
            lock (this)
            {
                $$
            }
            """);
    }

    [WpfFact]
    public async Task NoLockSnippetInBlockNamespaceTest()
    {
        await VerifyItemIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """, ItemToCommit);
    }

    [WpfFact]
    public async Task NoLockSnippetInFileScopedNamespaceTest()
    {
        await VerifyItemIsAbsentAsync("""
            namespace Namespace;
            $$
            """, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertLockSnippetInConstructorTest()
    {
        await VerifyCustomCommitProviderAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """, ItemToCommit, """
            class Program
            {
                public Program()
                {
                    lock (this)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [WpfFact]
    public async Task NoLockSnippetInTypeBodyTest()
    {
        await VerifyItemIsAbsentAsync("""
            class Program
            {
                $$
            }
            """, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertLockSnippetInLocalFunctionTest()
    {
        await VerifyCustomCommitProviderAsync("""
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
            """, ItemToCommit, """
            class Program
            {
                public void Method()
                {
                    void LocalFunction()
                    {
                        lock (this)
                        {
                            $$
                        }
                    }
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertLockSnippetInAnonymousFunctionTest()
    {
        await VerifyCustomCommitProviderAsync("""
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
            """, ItemToCommit, """
            class Program
            {
                public void Method()
                {
                    var action = delegate()
                    {
                        lock (this)
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }

    [WpfFact]
    public async Task InsertLockSnippetInParenthesizedLambdaExpressionTest()
    {
        await VerifyCustomCommitProviderAsync("""
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
            """, ItemToCommit, """
            class Program
            {
                public void Method()
                {
                    var action = () =>
                    {
                        lock (this)
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }
}
