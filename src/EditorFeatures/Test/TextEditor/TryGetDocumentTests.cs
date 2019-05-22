// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor
{
    [UseExportProvider]
    public class TryGetDocumentTests
    {
        [Fact]
        [WorkItem(624315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624315")]
        public void MultipleTextChangesTest()
        {
            var code = @"class C
";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var hostDocument = workspace.Documents.First();
            var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentId(hostDocument));

            var buffer = hostDocument.GetTextBuffer();
            var container = buffer.AsTextContainer();
            var startPosition = buffer.CurrentSnapshot.GetLineFromLineNumber(1).Start.Position;

            buffer.Insert(startPosition, "{");
            buffer.Insert(startPosition + 1, " ");
            buffer.Insert(startPosition + 2, "}");

            var newDocument = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
            Assert.NotNull(newDocument);

            var expected = @"class C
{ }";

            var newSourceText = newDocument.GetTextAsync().Result;
            Assert.Equal(expected, newSourceText.ToString());

            Assert.True(container == newSourceText.Container);
        }

        [Fact]
        public void EmptyTextChanges()
        {
            var code = @"class C";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var hostDocument = workspace.Documents.First();
            var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentId(hostDocument));

            var buffer = hostDocument.GetTextBuffer();
            var startPosition = buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start.Position;
            var text = buffer.CurrentSnapshot.AsText();
            var container = buffer.AsTextContainer();
            Assert.Same(text.Container, container);

            using (var edit = buffer.CreateEdit(EditOptions.DefaultMinimalChange, null, null))
            {
                edit.Delete(0, 3);
                edit.Insert(0, "cla");
                edit.Apply();
            }

            Assert.True(buffer.CurrentSnapshot.Version.VersionNumber == 2);
            Assert.True(buffer.CurrentSnapshot.Version.ReiteratedVersionNumber == 1);

            var newText = buffer.CurrentSnapshot.AsText();

            // different buffer snapshot should never return same roslyn text snapshot
            Assert.NotSame(text, newText);

            var newDocument = newText.GetRelatedDocumentsWithChanges().First();

            // different text snapshot never gives back same roslyn snapshot
            Assert.NotSame(document, newDocument);
        }
    }
}
