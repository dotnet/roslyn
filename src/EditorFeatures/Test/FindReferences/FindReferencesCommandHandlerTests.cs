// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
                var findReferencesPresenter = new MockDefinitionsAndReferencesPresenter();

                var handler = new FindReferencesCommandHandler(
                    TestWaitIndicator.Default,
                    SpecializedCollections.SingletonEnumerable(findReferencesPresenter),
                    SpecializedCollections.EmptyEnumerable<Lazy<IStreamingFindReferencesPresenter>>(),
                    workspace.ExportProvider.GetExports<IAsynchronousOperationListener, FeatureMetadata>());

                var textView = workspace.Documents[0].GetTextView();
                textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, 7));
                handler.ExecuteCommand(new FindReferencesCommandArgs(
                    textView,
                    textView.TextBuffer), () => { });

                AssertResult(findReferencesPresenter.DefinitionsAndReferences, "C", ".ctor");
            }
        }

        private bool AssertResult(
            DefinitionsAndReferences definitionsAndReferences,
            params string[] definitions)
        {
            return definitionsAndReferences.Definitions.Select(r => r.DisplayParts.JoinText())
                                                       .SetEquals(definitions);
        }
    }
}