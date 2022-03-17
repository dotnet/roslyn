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
    public class CSharpIfSnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        private static readonly string s_itemToCommit = FeaturesResources.Insert_an_if_statement;

        internal override Type GetCompletionProviderType()
            => typeof(CSharpSnippetCompletionProvider);

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"$$class Program
{
    public void Method()
    {
        if (true)
        {
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, s_itemToCommit, expectedCodeAfterCommit);
        }
    }
}
