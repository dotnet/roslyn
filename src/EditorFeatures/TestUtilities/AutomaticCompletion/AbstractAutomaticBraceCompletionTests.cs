// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
{
    [UseExportProvider]
    public abstract class AbstractAutomaticBraceCompletionTests
    {
        internal void CheckStart(IBraceCompletionSession session, bool expectValidSession = true)
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

        internal void CheckBackspace(IBraceCompletionSession session)
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

        internal void CheckTab(IBraceCompletionSession session, bool allowTab = true)
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

        internal void CheckReturn(IBraceCompletionSession session, int indentation, string result = null)
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
                Assert.Equal(result, session.SubjectBuffer.CurrentSnapshot.GetText());
            }
        }

        internal void CheckText(IBraceCompletionSession session, string result)
        {
            Assert.Equal(result, session.SubjectBuffer.CurrentSnapshot.GetText());
        }

        internal void CheckReturnOnNonEmptyLine(IBraceCompletionSession session, int expectedVirtualSpace)
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

        internal void CheckOverType(IBraceCompletionSession session, bool allowOverType = true)
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

        internal void Type(IBraceCompletionSession session, string text)
        {
            var buffer = session.SubjectBuffer;
            var caret = session.TextView.GetCaretPoint(buffer).Value;

            using (var edit = buffer.CreateEdit())
            {
                edit.Insert(caret.Position, text);
                edit.Apply();
            }
        }

        internal Holder CreateSession(TestWorkspace workspace, char opening, char closing, Dictionary<OptionKey, object> changedOptionSet = null)
        {
            var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();
            var undoManager = workspace.ExportProvider.GetExportedValue<ITextBufferUndoManagerProvider>();
            var editorOperationsFactoryService = workspace.ExportProvider.GetExportedValue<IEditorOperationsFactoryService>();

            if (changedOptionSet != null)
            {
                var options = workspace.Options;
                foreach (var entry in changedOptionSet)
                {
                    options = options.WithChangedOption(entry.Key, entry.Value);
                }

                workspace.Options = options;
            }

            var document = workspace.Documents.First();

            var provider = new BraceCompletionSessionProvider(threadingContext, undoManager, editorOperationsFactoryService);
            var openingPoint = new SnapshotPoint(document.GetTextBuffer().CurrentSnapshot, document.CursorPosition.Value);
            if (provider.TryCreateSession(document.GetTextView(), openingPoint, opening, closing, out var session))
            {
                return new Holder(workspace, session);
            }

            workspace.Dispose();
            return null;
        }

        internal class Holder : IDisposable
        {
            public TestWorkspace Workspace { get; }
            public IBraceCompletionSession Session { get; }

            public Holder(TestWorkspace workspace, IBraceCompletionSession session)
            {
                this.Workspace = workspace;
                this.Session = session;
            }

            public void Dispose()
            {
                this.Workspace.Dispose();
            }
        }
    }
}
