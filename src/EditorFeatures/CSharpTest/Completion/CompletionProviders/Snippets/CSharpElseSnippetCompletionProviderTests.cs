// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class CSharpElseSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
{
    protected override string ItemToCommit => "else";

    [WpfFact]
    public async Task InsertElseSnippetInMethodTest()
    {
        var markupBeforeCommit =
            """
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
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task NoElseSnippetInMethodWithoutIfStatementTest()
    {
        var markupBeforeCommit =
            """
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """;
        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetGlobalTest()
    {
        var markupBeforeCommit =
            """
            if (true)
            {
            }
            $$
            class Program
            {
                public async Task MethodAsync()
                {
                }
            }
            """;

        var expectedCodeAfterCommit =
            """
            if (true)
            {
            }
            else
            {
                $$
            }
            class Program
            {
                public async Task MethodAsync()
                {
                }
            }
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task NoElseSnippetInBlockNamespaceTest()
    {
        var markupBeforeCommit =
            """
            namespace Namespace
            {
                if (true)
                {
                }
                $$
                class Program
                {
                    public async Task MethodAsync()
                    {
                    }
                }
            }
            """;
        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task NoElseSnippetInFileScopedNamespaceTest()
    {
        var markupBeforeCommit =
            """
            namespace Namespace;
            if (true)
            {
            }
            $$
            class Program
            {
                public async Task MethodAsync()
                {
                }
            }
            """;
        await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetInConstructorTest()
    {
        var markupBeforeCommit =
            """
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
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetInLocalFunctionTest()
    {
        var markupBeforeCommit =
            """
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
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetSingleLineIfWithBlockTest()
    {
        var markupBeforeCommit =
            """
            class Program
            {
                public void Method()
                {
                    if (true) {}
                    $$
                }
            }
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetSingleLineIfTest()
    {
        var markupBeforeCommit =
            """
            using System;
            class Program
            {
                public void Method()
                {
                    if (true) Console.WriteLine(5);
                    $$
                }
            }
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }

    [WpfFact]
    public async Task InsertElseSnippetNestedIfTest()
    {
        var markupBeforeCommit =
            """
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
            """;

        var expectedCodeAfterCommit =
            """
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
            """;
        await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
    }
}
