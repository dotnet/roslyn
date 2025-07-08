// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class SemanticSnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    public SemanticSnippetCompletionProviderTests()
    {
        ShowNewSnippetExperience = true;
    }

    internal override Type GetCompletionProviderType()
        => typeof(CSharpSnippetCompletionProvider);

    [WpfFact]
    public Task InsertConsoleSnippetWithInvocationBeforeAndAfterCursorTest()
        => VerifyCustomCommitProviderAsync("""
            class Program
            {
                public void Method()
                {
                    Wr$$Blah
                }
            }
            """, "cw", """
            using System;

            class Program
            {
                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);

    [WpfFact]
    public Task InsertConsoleSnippetWithInvocationUnderscoreBeforeAndAfterCursorTest()
        => VerifyCustomCommitProviderAsync("""
            class Program
            {
                public void Method()
                {
                    _Wr$$Blah_
                }
            }
            """, "cw", """
            using System;

            class Program
            {
                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);
}
