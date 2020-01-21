// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
{
    [UseExportProvider]
    public abstract class AbstractAutomaticLineEnderTests
    {
        protected abstract TestWorkspace CreateWorkspace(string code);
        protected abstract Action CreateNextHandler(TestWorkspace workspace);

        internal abstract IChainedCommandHandler<AutomaticLineEnderCommandArgs> CreateCommandHandler(
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations);

        protected void Test(string expected, string code, bool completionActive = false, bool assertNextHandlerInvoked = false)
        {
            using (var workspace = CreateWorkspace(code))
            {
                var view = workspace.Documents.Single().GetTextView();
                var buffer = workspace.Documents.Single().GetTextBuffer();
                var nextHandlerInvoked = false;

                view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value));

                var commandHandler = CreateCommandHandler(
                                        GetExportedValue<ITextUndoHistoryRegistry>(workspace),
                                        GetExportedValue<IEditorOperationsFactoryService>(workspace));

                commandHandler.ExecuteCommand(new AutomaticLineEnderCommandArgs(view, buffer),
                                                    assertNextHandlerInvoked
                                                        ? () => { nextHandlerInvoked = true; }
                : CreateNextHandler(workspace), TestCommandExecutionContext.Create());

                Test(view, buffer, expected);

                Assert.Equal(assertNextHandlerInvoked, nextHandlerInvoked);
            }
        }

        private void Test(ITextView view, ITextBuffer buffer, string expectedWithAnnotations)
        {
            MarkupTestFile.GetPosition(expectedWithAnnotations, out var expected, out int expectedPosition);

            // Remove any virtual space from the expected text.
            var virtualPosition = view.Caret.Position.VirtualBufferPosition;
            expected = expected.Remove(virtualPosition.Position, virtualPosition.VirtualSpaces);

            Assert.Equal(expected, buffer.CurrentSnapshot.GetText());
            Assert.Equal(expectedPosition, virtualPosition.Position.Position + virtualPosition.VirtualSpaces);
        }

        public T GetService<T>(TestWorkspace workspace)
        {
            return workspace.GetService<T>();
        }

        public T GetExportedValue<T>(TestWorkspace workspace)
        {
            return workspace.ExportProvider.GetExportedValue<T>();
        }
    }
}
