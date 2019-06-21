// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindReferences;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
    [UseExportProvider]
    public class FindReferencesCommandHandlerTests
    {
        private class MockFindUsagesContext : FindUsagesContext
        {
            public readonly List<DefinitionItem> Result = new List<DefinitionItem>();

            public override Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                lock (Result)
                {
                    Result.Add(definition);
                }

                return Task.CompletedTask;
            }
        }

        private class MockStreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
        {
            private readonly FindUsagesContext _context;

            public MockStreamingFindUsagesPresenter(FindUsagesContext context)
            {
                _context = context;
            }

            public FindUsagesContext StartSearch(string title, bool supportsReferences)
                => _context;

            public void ClearAll()
            {
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task TestFindReferencesAsynchronousCall()
        {
            using (var workspace = TestWorkspace.CreateCSharp("class C { C() { new C(); } }"))
            {
                var context = new MockFindUsagesContext();
                var presenter = new MockStreamingFindUsagesPresenter(context);

                var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

                var handler = new FindReferencesCommandHandler(
                    presenter,
                    listenerProvider);

                var textView = workspace.Documents[0].GetTextView();
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, 7));
                handler.ExecuteCommand(new FindReferencesCommandArgs(
                    textView,
                    textView.TextBuffer), TestCommandExecutionContext.Create());

                var waiter = listenerProvider.GetWaiter(FeatureAttribute.FindReferences);
                await waiter.CreateExpeditedWaitTask();
                AssertResult(context.Result, "C.C()", "class C");
            }
        }

        private void AssertResult(
            List<DefinitionItem> result,
            params string[] definitions)
        {
            Assert.Equal(result.Select(kvp => kvp.DisplayParts.JoinText()).OrderBy(a => a),
                         definitions.OrderBy(a => a));
        }
    }
}
