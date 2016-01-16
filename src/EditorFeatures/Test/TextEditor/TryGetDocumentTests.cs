// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor
{
    public class TryGetDocumentTests
    {
        [Fact]
        [WorkItem(624315)]
        public async Task MultipleTextChangesTest()
        {
            var code = @"class C
";
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(code))
            {
                var hostDocument = workspace.Documents.First();
                var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentId(hostDocument));

                var buffer = hostDocument.GetTextBuffer();
                var container = buffer.AsTextContainer();
                var startPosition = buffer.CurrentSnapshot.GetLineFromLineNumber(1).Start.Position;

                buffer.Insert(startPosition, "{");
                buffer.Insert(startPosition + 1, " ");
                buffer.Insert(startPosition + 2, "}");

                Document newDocument = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
                Assert.NotNull(newDocument);

                var expected = @"class C
{ }";

                var newSourceText = newDocument.GetTextAsync().Result;
                Assert.Equal(expected, newSourceText.ToString());

                Assert.True(container == newSourceText.Container);
            }
        }

        [Fact]
        public async Task EmptyTextChanges()
        {
            var code = @"class C";
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(code))
            {
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
                Assert.Same(text, newText);

                Document newDocument = newText.GetRelatedDocumentsWithChanges().First();
                Assert.Same(document, newDocument);
            }
        }
    }
}
