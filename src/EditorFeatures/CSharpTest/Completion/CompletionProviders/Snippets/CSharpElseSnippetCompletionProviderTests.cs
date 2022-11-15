// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpElseSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "else";

        [WpfFact]
        public async Task InsertElseSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        else
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertElseSnippetGlobalTest()
        {
            var markupBeforeCommit =
@"$$
class Program
{
    public async Task MethodAsync()
    {
    }
}";

            var expectedCodeAfterCommit =
@"else
{
    $$
}
class Program
{
    public async Task MethodAsync()
    {
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoElseSnippetInBlockNamespaceTest()
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
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoElseSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
@"
namespace Namespace;
$$
class Program
{
    public async Task MethodAsync()
    {
    }
}
";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task InsertElseSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        else
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertElseSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            $$
        }
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            else
            {
                $$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
