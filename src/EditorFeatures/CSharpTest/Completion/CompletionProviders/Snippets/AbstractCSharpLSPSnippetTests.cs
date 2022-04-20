// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Snippets
{
    [UseExportProvider]
    public abstract class AbstractCSharpLSPSnippetTests : AbstractCSharpCompletionProviderTests
    {
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
            var generatedLSPSnippet = commit.LSPSnippet;
            AssertEx.EqualOrDiff(expectedLSPSnippet, generatedLSPSnippet);
        }
    }
}
