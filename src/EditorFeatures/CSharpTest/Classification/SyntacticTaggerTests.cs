// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public class SyntacticTaggerTests
    {
        [WorkItem(1032665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032665")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestTagsChangedForEntireFile()
        {
            var code =
@"class Program2
{
    string x = @""/// <summary>$$
/// </summary>"";
}";
            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var document = workspace.Documents.First();
                var subjectBuffer = document.TextBuffer;
                var checkpoint = new Checkpoint();
                var tagComputer = new SyntacticClassificationTaggerProvider.TagComputer(
                    subjectBuffer,
                    workspace.GetService<IForegroundNotificationService>(),
                    AggregateAsynchronousOperationListener.CreateEmptyListener(),
                    null,
                    new SyntacticClassificationTaggerProvider(null, null, null));

                var expectedLength = subjectBuffer.CurrentSnapshot.Length;
                int? actualVersionNumber = null;
                int? actualLength = null;
                tagComputer.TagsChanged += (s, e) =>
                {
                    actualVersionNumber = e.Span.Snapshot.Version.VersionNumber;
                    actualLength = e.Span.Length;
                    checkpoint.Release();
                };

                await checkpoint.Task;
                Assert.Equal(1, actualVersionNumber);
                Assert.Equal(expectedLength, actualLength);

                checkpoint = new Checkpoint();

                // Now apply an edit that require us to reclassify more that just the current line
                var snapshot = subjectBuffer.Insert(document.CursorPosition.Value, "\"");
                expectedLength = snapshot.Length;

                // NOTE: TagsChanged is raised on the UI thread, so there is no race between
                // assigning expected here and verifying in the event handler, because the
                // event handler can't run until we await.
                await checkpoint.Task;
                Assert.Equal(2, actualVersionNumber);
                Assert.Equal(expectedLength, actualLength);
            }
        }
    }
}
