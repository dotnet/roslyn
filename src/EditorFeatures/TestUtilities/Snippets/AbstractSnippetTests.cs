/*// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Snippets
{
    [UseExportProvider]
    public abstract class AbstractSnippetTests : AbstractCSharpCompletionProviderTests
    {
        protected virtual async Task VerifyCustomCommitProviderWorkerAsync(string codeBeforeCommit, string expectedLSPSnippet, int position, string itemToCommit, SourceCodeKind sourceCodeKind, char? commitChar = null)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();
            var workspace = workspaceFixture.Target.GetWorkspace();

            // Set options that are not CompletionOptions
            workspace.SetOptions(WithChangedNonCompletionOptions(workspace.Options));

            var document1 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            await VerifyCustomCommitProviderLSPSnippetAsync(document1, expectedLSPSnippet, position, itemToCommit, commitChar);

            if (await CanUseSpeculativeSemanticModelAsync(document1, position))
            {
                var document2 = workspaceFixture.Target.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                await VerifyCustomCommitProviderLSPSnippetAsync(document2, expectedLSPSnippet, position, itemToCommit, commitChar);
            }
        }

        private async Task VerifyCustomCommitProviderLSPSnippetAsync(Document document, string expectedLSPSnippet, int position, string itemToCommit, char? commitChar = null)
        {
            var service = GetCompletionService(document.Project);
            var completionList = await GetCompletionListAsync(service, document, position, CompletionTrigger.Invoke);
            var items = completionList.Items;

            Assert.Contains(itemToCommit, items.Select(x => x.DisplayText), GetStringComparer());
            var firstItem = items.First(i => CompareItems(i.DisplayText, itemToCommit));
            await VerifyCustomCommitWorkerAsync(service, document, firstItem, expectedLSPSnippet, commitChar);
        }

        private async Task VerifyCustomCommitWorkerAsync(
            CompletionServiceWithProviders service,
            Document document,
            CompletionItem completionItem,
            string expectedLSPSnippet,
            char? commitChar = null)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            // textview is created lazily, so need to access it before making 
            // changes to document, so the cursor position is tracked correctly.
            var textView = workspaceFixture.Target.CurrentDocument.GetTextView();

            var commit = await service.GetChangeAsync(document, completionItem, commitChar, CancellationToken.None);
            var generatedLSPSnippet = commit.LSPSnippet;
            AssertEx.EqualOrDiff(expectedLSPSnippet, generatedLSPSnippet);
        }
    }
}
*/
