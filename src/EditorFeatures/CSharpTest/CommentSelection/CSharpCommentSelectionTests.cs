﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CommentSelection
{
    [UseExportProvider]
    public class CSharpCommentSelectionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public void UncommentAndFormat1()
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
            UncommentSelection(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public void UncommentAndFormat2()
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
            UncommentSelection(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public void UncommentSingleLineCommentInPseudoBlockComment()
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

            UncommentSelection(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)]
        public void UncommentAndFormat3()
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
            UncommentSelection(code, expected);
        }

        private static void UncommentSelection(string markup, string expected)
        {
            using var workspace = TestWorkspace.CreateCSharp(markup);
            var doc = workspace.Documents.First();
            SetupSelection(doc.GetTextView(), doc.SelectedSpans.Select(s => Span.FromBounds(s.Start, s.End)));

            var commandHandler = new CommentUncommentSelectionCommandHandler(
                workspace.ExportProvider.GetExportedValue<ITextUndoHistoryRegistry>(),
                workspace.ExportProvider.GetExportedValue<IEditorOperationsFactoryService>());
            var textView = doc.GetTextView();
            var textBuffer = doc.GetTextBuffer();
            commandHandler.ExecuteCommand(textView, textBuffer, Operation.Uncomment, TestCommandExecutionContext.Create());

            Assert.Equal(expected, doc.GetTextBuffer().CurrentSnapshot.GetText());
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
