// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.Snippets;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpLSPSnippetTests : AbstractCSharpLSPSnippetTests
    {
        internal override Type GetCompletionProviderType() => typeof(CSharpSnippetCompletionProvider);

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertConsoleSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        $$
    }
}";

            var expectedLSPSnippet =
@"Console.WriteLine($0);";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, FeaturesResources.Write_to_the_console, expectedLSPSnippet);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        $$
    }
}";

            var expectedLSPSnippet =
@"if ({1:true}) 
{$0
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, FeaturesResources.Insert_an_if_statement, expectedLSPSnippet);
        }
    }
}
