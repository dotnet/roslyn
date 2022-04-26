// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpLSPSnippetTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => throw new NotImplementedException();

        protected override async Task VerifyCustomCommitProviderWorkerAsync(string codeBeforeCommit, int position, string itemToCommit, string expectedLSPSnippet, SourceCodeKind sourceCodeKind, char? commitChar = null)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();
            var workspace = workspaceFixture.Target.GetWorkspace();

            // Set options that are not CompletionOptions
            workspace.SetOptions(WithChangedNonCompletionOptions(workspace.Options));

            var document1 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            await VerifyCustomCommitProviderLSPSnippetAsync(document1, position, itemToCommit, expectedLSPSnippet, commitChar);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                await VerifyCustomCommitProviderLSPSnippetAsync(document2, position, itemToCommit, expectedLSPSnippet, commitChar);
            }
        }

        private async Task VerifyCustomCommitProviderLSPSnippetAsync(Document document, int position, string itemToCommit, string expectedLSPSnippet, char? commitChar = null)
        {
            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, CompletionTrigger.Invoke);
            var items = completionList.Items;

            Assert.Contains(itemToCommit, items.Select(x => x.DisplayText), GetStringComparer());
            var completionItem = items.First(i => CompareItems(i.DisplayText, itemToCommit));
            var commit = await service.GetChangeAsync(document, completionItem, commitChar, CancellationToken.None);
            commit.Properties!.TryGetValue("LSPSnippet", out var generatedLSPSnippet);
            AssertEx.EqualOrDiff(expectedLSPSnippet, generatedLSPSnippet);
        }

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
@"using System;

class Program
{
    public void Method()
    {
        Console.WriteLine($0);";

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
@"if (${1:true})
        {$0
        }";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, FeaturesResources.Insert_an_if_statement, expectedLSPSnippet);
        }
    }
}
