// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
{
    public abstract class AbstractAutomaticLineEnderTests
    {
        protected abstract TestWorkspace CreateWorkspace(string[] code);
        protected abstract Action CreateNextHandler(TestWorkspace workspace);

        internal abstract ICommandHandler<AutomaticLineEnderCommandArgs> CreateCommandHandler(
            Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations);

        protected void Test(string expected, string code, bool completionActive = false, bool assertNextHandlerInvoked = false)
        {
            using (var workspace = CreateWorkspace(new string[] { code }))
            {
                var view = workspace.Documents.Single().GetTextView();
                var buffer = workspace.Documents.Single().GetTextBuffer();
                var nextHandlerInvoked = false;

                view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value));

                var commandHandler = CreateCommandHandler(
                                        GetExportedValue<Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator>(workspace),
                                        GetExportedValue<ITextUndoHistoryRegistry>(workspace),
                                        GetExportedValue<IEditorOperationsFactoryService>(workspace));

                commandHandler.ExecuteCommand(new AutomaticLineEnderCommandArgs(view, buffer),
                                                    assertNextHandlerInvoked
                                                        ? () => { nextHandlerInvoked = true; }
                                                        : CreateNextHandler(workspace));

                Test(view, buffer, expected);

                Assert.Equal(assertNextHandlerInvoked, nextHandlerInvoked);
            }
        }

        private void Test(ITextView view, ITextBuffer buffer, string expectedWithAnnotations)
        {
            string expected;
            int expectedPosition;
            MarkupTestFile.GetPosition(expectedWithAnnotations, out expected, out expectedPosition);

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
