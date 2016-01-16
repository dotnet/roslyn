// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CommentSelection
{
    public class CSharpCommentSelectionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public async Task UncommentAndFormat1()
        {
            var code = @"class A
{
    [|          //            void  Method  (   )
                // {
                //
                //                      }|]
}";
            var expected = @"class A
{
    void Method()
    {

    }
}";
            await UncommentSelectionAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public async Task UncommentAndFormat2()
        {
            var code = @"class A
{
    [|          /*            void  Method  (   )
                 {
                
                                      } */|]
}";
            var expected = @"class A
{
    void Method()
    {

    }
}";
            await UncommentSelectionAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public async Task UncommentSingleLineCommentInPseudoBlockComment()
        {
            var code = @"
class C
{
    /// <include file='doc\Control.uex' path='docs/doc[@for=""Control.RtlTranslateAlignment1""]/*' />
    protected void RtlTranslateAlignment2()
    {
        //[|int x = 0;|]
    }
    /* Hello world */
}";

            var expected = @"
class C
{
    /// <include file='doc\Control.uex' path='docs/doc[@for=""Control.RtlTranslateAlignment1""]/*' />
    protected void RtlTranslateAlignment2()
    {
        int x = 0;
    }
    /* Hello world */
}";

            await UncommentSelectionAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public async Task UncommentAndFormat3()
        {
            var code = @"class A
{
    [|          //            void  Method  (   )       |]
    [|            // {                                  |]
    [|            //                                    |]
    [|            //                      }             |]
}";
            var expected = @"class A
{
    void Method()
    {

    }
}";
            await UncommentSelectionAsync(code, expected);
        }

        private static async Task UncommentSelectionAsync(string markup, string expected)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(markup))
            {
                var doc = workspace.Documents.First();
                SetupSelection(doc.GetTextView(), doc.SelectedSpans.Select(s => Span.FromBounds(s.Start, s.End)));

                var commandHandler = new CommentUncommentSelectionCommandHandler(
                    TestWaitIndicator.Default,
                    workspace.ExportProvider.GetExportedValue<ITextUndoHistoryRegistry>(),
                    workspace.ExportProvider.GetExportedValue<IEditorOperationsFactoryService>());
                var textView = doc.GetTextView();
                var textBuffer = doc.GetTextBuffer();
                commandHandler.ExecuteCommand(textView, textBuffer, CommentUncommentSelectionCommandHandler.Operation.Uncomment);

                Assert.Equal(expected, doc.TextBuffer.CurrentSnapshot.GetText());
            }
        }

        private static void SetupSelection(IWpfTextView textView, IEnumerable<Span> spans)
        {
            var snapshot = textView.TextSnapshot;
            if (spans.Count() == 1)
            {
                textView.Selection.Select(new SnapshotSpan(snapshot, spans.Single()), isReversed: false);
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Single().End));
            }
            else
            {
                textView.Selection.Mode = TextSelectionMode.Box;
                textView.Selection.Select(new VirtualSnapshotPoint(snapshot, spans.First().Start),
                                          new VirtualSnapshotPoint(snapshot, spans.Last().End));
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Last().End));
            }
        }
    }
}
