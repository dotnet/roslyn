// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
    public class FindReferencesCommandHandlerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task TestFindReferencesSynchronousCall()
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync("class C { C() { new C(); } }"))
            {
                var findReferencesPresenter = new MockNavigableItemsPresenter();

                var handler = new FindReferencesCommandHandler(
                    TestWaitIndicator.Default,
                    SpecializedCollections.SingletonEnumerable(findReferencesPresenter));

                var textView = workspace.Documents[0].GetTextView();
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, 7));
                handler.ExecuteCommand(new FindReferencesCommandArgs(
                    textView,
                    textView.TextBuffer), () => { });

                AssertResult(findReferencesPresenter.Items, "C", ".ctor");
            }
        }

        private bool AssertResult(ImmutableArray<INavigableItem> result, params string[] definitions)
        {
            return result.Select(r => r.DisplayTaggedParts.JoinText()).SetEquals(definitions);
        }
    }
}