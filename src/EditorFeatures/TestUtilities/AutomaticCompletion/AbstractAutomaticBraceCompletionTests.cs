// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
{
    [UseExportProvider]
    public abstract class AbstractAutomaticBraceCompletionTests
    {
        internal static void CheckStart(IBraceCompletionSession session, bool expectValidSession = true)
        {
            Type(session, session.OpeningBrace.ToString());

            session.Start();

            if (expectValidSession)
            {
                var closingPoint = session.ClosingPoint.GetPoint(session.SubjectBuffer.CurrentSnapshot).Subtract(1);
                Assert.Equal(closingPoint.GetChar(), session.ClosingBrace);
            }
            else
            {
                Assert.Null(session.OpeningPoint);
                Assert.Null(session.ClosingPoint);
            }
        }

        internal static void CheckBackspace(IBraceCompletionSession session)
        {
            session.TextView.TryMoveCaretToAndEnsureVisible(session.OpeningPoint.GetPoint(session.SubjectBuffer.CurrentSnapshot).Add(1));
            session.PreBackspace(out var handled);
            if (!handled)
            {
                session.PostBackspace();
            }

            Assert.Null(session.OpeningPoint);
            Assert.Null(session.ClosingPoint);
        }

        internal static void CheckTab(IBraceCompletionSession session, bool allowTab = true)
        {
            session.PreTab(out var handled);
            if (!handled)
            {
                session.PostTab();
            }

            var caret = session.TextView.GetCaretPoint(session.SubjectBuffer).Value;
            if (allowTab)
            {
                Assert.Equal(session.ClosingPoint.GetPosition(session.SubjectBuffer.CurrentSnapshot), caret.Position);
            }
            else
            {
                Assert.True(caret.Position < session.ClosingPoint.GetPosition(session.SubjectBuffer.CurrentSnapshot));
            }
        }

        internal static void CheckReturn(IBraceCompletionSession session, int indentation, string result = null)
        {
            session.PreReturn(out var handled);

            Type(session, Environment.NewLine);

            if (!handled)
            {
                session.PostReturn();
            }

            var virtualCaret = session.TextView.GetVirtualCaretPoint(session.SubjectBuffer).Value;
            Assert.True(indentation == virtualCaret.VirtualSpaces, $"Expected indentation was {indentation}, but the actual indentation was {virtualCaret.VirtualSpaces}");

            if (result != null)
            {
                AssertEx.EqualOrDiff(result, session.SubjectBuffer.CurrentSnapshot.GetText());
            }
        }

        internal static void CheckText(IBraceCompletionSession session, string result)
            => Assert.Equal(result, session.SubjectBuffer.CurrentSnapshot.GetText());

        internal static void CheckReturnOnNonEmptyLine(IBraceCompletionSession session, int expectedVirtualSpace)
        {
            session.PreReturn(out var handled);

            Type(session, Environment.NewLine);

            if (!handled)
            {
                session.PostReturn();
            }

            var virtualCaret = session.TextView.GetVirtualCaretPoint(session.SubjectBuffer).Value;
            Assert.Equal(expectedVirtualSpace, virtualCaret.VirtualSpaces);
        }

        internal static void CheckOverType(IBraceCompletionSession session, bool allowOverType = true)
        {
            var preClosingPoint = session.ClosingPoint.GetPoint(session.SubjectBuffer.CurrentSnapshot);
            Assert.Equal(session.ClosingBrace, preClosingPoint.Subtract(1).GetChar());
            session.PreOverType(out var handled);
            if (!handled)
            {
                session.PostOverType();
            }

            var postClosingPoint = session.ClosingPoint.GetPoint(session.SubjectBuffer.CurrentSnapshot);
            Assert.Equal(postClosingPoint.Subtract(1).GetChar(), session.ClosingBrace);

            var caret = session.TextView.GetCaretPoint(session.SubjectBuffer).Value;
            if (allowOverType)
            {
                Assert.Equal(postClosingPoint.Position, caret.Position);
            }
            else
            {
                Assert.True(caret.Position < postClosingPoint.Position);
            }
        }

        internal static void Type(IBraceCompletionSession session, string text)
        {
            var buffer = session.SubjectBuffer;
            var caret = session.TextView.GetCaretPoint(buffer).Value;

            using (var edit = buffer.CreateEdit())
            {
                edit.Insert(caret.Position, text);
                edit.Apply();
            }
        }

        internal static Holder CreateSession(EditorTestWorkspace workspace, char opening, char closing, OptionsCollection globalOptions = null)
        {
            var document = workspace.Documents.First();

            var provider = Assert.IsType<BraceCompletionSessionProvider>(workspace.GetService<IBraceCompletionSessionProvider>());

            var openingPoint = new SnapshotPoint(document.GetTextBuffer().CurrentSnapshot, document.CursorPosition.Value);
            var textView = document.GetTextView();

            globalOptions?.SetGlobalOptions(workspace.GlobalOptions);
            workspace.GlobalOptions.SetEditorOptions(textView.Options.GlobalOptions, document.Project.Language);

            if (provider.TryCreateSession(textView, openingPoint, opening, closing, out var session))
            {
                return new Holder(workspace, session);
            }

            workspace.Dispose();
            return null;
        }

        internal class Holder : IDisposable
        {
            public EditorTestWorkspace Workspace { get; }
            public IBraceCompletionSession Session { get; }

            public Holder(EditorTestWorkspace workspace, IBraceCompletionSession session)
            {
                this.Workspace = workspace;
                this.Session = session;
            }

            public void Dispose()
                => this.Workspace.Dispose();
        }
    }
}
