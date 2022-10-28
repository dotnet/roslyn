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
    public class CSharpTryCatchSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "try";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertTryCatchInMethodTest()
        {
            var markupBeforeCommit =
@"public class Class
{
    public void Method()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"public class Class
{
    public void Method()
    {
        try
        {
            
        }
        catch (Exception e)
        {
            $$
            throw;
        }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
