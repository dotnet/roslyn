// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.FindReferences;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
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

                return SpecializedTasks.EmptyTask;
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

                var listenerProvider = new AsynchronousOperationListenerProvider();

                var handler = new FindReferencesCommandHandler(
                    TestWaitIndicator.Default,
                    SpecializedCollections.SingletonEnumerable(new Lazy<IStreamingFindUsagesPresenter>(() => presenter)),
                    listenerProvider);

                var textView = workspace.Documents[0].GetTextView();
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, 7));
                handler.ExecuteCommand(new FindReferencesCommandArgs(
                    textView,
                    textView.TextBuffer), () => { });

                var waiter = listenerProvider.GetWaiter(FeatureAttribute.FindReferences);
                await waiter.CreateWaitTask();
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
