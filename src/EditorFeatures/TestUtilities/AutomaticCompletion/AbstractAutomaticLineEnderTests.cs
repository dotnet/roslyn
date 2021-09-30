﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
{
    [UseExportProvider]
    public abstract class AbstractAutomaticLineEnderTests
    {
        protected abstract string Language { get; }
        protected abstract Action CreateNextHandler(TestWorkspace workspace);

        internal abstract IChainedCommandHandler<AutomaticLineEnderCommandArgs> GetCommandHandler(TestWorkspace workspace);

        protected void Test(string expected, string markupCode, bool completionActive = false, bool assertNextHandlerInvoked = false)
        {
            TestFileMarkupParser.GetPositionsAndSpans(markupCode, out var code, out var positions, out _);
            Assert.NotEmpty(positions);

            foreach (var position in positions)
            {
                // Run the test once for each input position. All marked positions in the input for a test are expected
                // to have the same result.
                Test(expected, code, position, completionActive, assertNextHandlerInvoked);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter - https://github.com/dotnet/roslyn/issues/45892
        private void Test(string expected, string code, int position, bool completionActive = false, bool assertNextHandlerInvoked = false)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var markupCode = code[0..position] + "$$" + code[position..];

            // WPF is required for some reason: https://github.com/dotnet/roslyn/issues/46286
            using var workspace = TestWorkspace.Create(Language, compilationOptions: null, parseOptions: null, new[] { markupCode }, composition: EditorTestCompositions.EditorFeaturesWpf);

            var view = workspace.Documents.Single().GetTextView();
            var buffer = workspace.Documents.Single().GetTextBuffer();
            var nextHandlerInvoked = false;

            view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value));

            var commandHandler = GetCommandHandler(workspace);
            var nextHandler = assertNextHandlerInvoked ? () => nextHandlerInvoked = true : CreateNextHandler(workspace);

            commandHandler.ExecuteCommand(new AutomaticLineEnderCommandArgs(view, buffer), nextHandler, TestCommandExecutionContext.Create());

            Test(view, buffer, expected);

            Assert.Equal(assertNextHandlerInvoked, nextHandlerInvoked);
        }

        private static void Test(ITextView view, ITextBuffer buffer, string expectedWithAnnotations)
        {
            MarkupTestFile.GetPosition(expectedWithAnnotations, out var expected, out int expectedPosition);

            // Remove any virtual space from the expected text.
            var virtualPosition = view.Caret.Position.VirtualBufferPosition;
            expected = expected.Remove(virtualPosition.Position, virtualPosition.VirtualSpaces);

            Assert.Equal(expected, buffer.CurrentSnapshot.GetText());
            Assert.Equal(expectedPosition, virtualPosition.Position.Position + virtualPosition.VirtualSpaces);
        }

        public static T GetService<T>(TestWorkspace workspace)
            => workspace.GetService<T>();

        public static T GetExportedValue<T>(TestWorkspace workspace)
            => workspace.ExportProvider.GetExportedValue<T>();
    }
}
