// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class ConsoleSnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(ConsoleSnippetCompletionProvider);

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConsoleSnippetTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Wr$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        Console.WriteLine($$);
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Write to the Console", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertAsyncConsoleSnippetTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public async Task MethodAsync()
    {
        Wr$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public async Task MethodAsync()
    {
        await Console.Out.WriteLineAsync($$);
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Write to the Console", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConsoleSnippetGlobalTest()
        {
            var markupBeforeCommit =
@"
$$
class Program
{
    public async Task MethodAsync()
    {
    }
}";

            var expectedCodeAfterCommit =
@"
Console.WriteLine($$);
class Program
{
    public async Task MethodAsync()
    {
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "Write to the Console", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConsoleSnippetInNamespaceTest()
        {
            var markupBeforeCommit =
@"
namespace Namespace
{
    $$
    class Program
    {
        public async Task MethodAsync()
        {
        }
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, "Write to the Console");
        }
    }
}
