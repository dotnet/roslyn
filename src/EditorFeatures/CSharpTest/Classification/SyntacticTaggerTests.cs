// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public class SyntacticTaggerTests
    {
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032665")]
        [WpfFact]
        public async Task TestTagsChangedForPortionThatChanged()
        {
            var code =
                """
                class Program2
                {
                    string x = @"/// <summary>$$
                /// </summary>";
                }
                """;
            using var workspace = EditorTestWorkspace.CreateCSharp(code);
            var document = workspace.Documents.First();
            var subjectBuffer = document.GetTextBuffer();

            var checkpoint = new Checkpoint();

            var tagComputer = new SyntacticClassificationTaggerProvider.TagComputer(
                new SyntacticClassificationTaggerProvider(
                    workspace.GetService<IThreadingContext>(),
                    typeMap: null,
                    workspace.GetService<IGlobalOptionService>(),
                    AsynchronousOperationListenerProvider.NullProvider),
                subjectBuffer,
                diffTimeout: TimeSpan.MaxValue);

            // Capture the expected value before the await, in case it changes.
            var expectedLength = subjectBuffer.CurrentSnapshot.Length;
            int? actualVersionNumber = null;
            int? actualLength = null;
            var callstacks = new List<string>();
            tagComputer.TagsChanged += (s, e) =>
            {
                actualVersionNumber = e.Span.Snapshot.Version.VersionNumber;
                actualLength = e.Span.Length;
                callstacks.Add(new StackTrace().ToString());
                checkpoint.Release();
            };

            await checkpoint.Task;
            Assert.Equal(0, actualVersionNumber);
            Assert.Equal(expectedLength, actualLength);
            Assert.Equal(1, callstacks.Count);

            checkpoint = new Checkpoint();

            // Now apply an edit that require us to reclassify more that just the current line
            var snapshot = subjectBuffer.Insert(document.CursorPosition.Value, """
                "
                """);
            expectedLength = snapshot.Length;

            // NOTE: TagsChanged is raised on the UI thread, so there is no race between
            // assigning expected here and verifying in the event handler, because the
            // event handler can't run until we await.
            await checkpoint.Task;
            Assert.Equal(1, actualVersionNumber);
            Assert.Equal(37, actualLength);
            Assert.Equal(2, callstacks.Count);
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032665")]
        [WpfFact]
        public async Task TestTagsChangedAfterDelete()
        {
            var code =
@"class Goo";
            using var workspace = EditorTestWorkspace.CreateCSharp(code);
            var document = workspace.Documents.First();
            var subjectBuffer = document.GetTextBuffer();

            var checkpoint = new Checkpoint();

            var typeMap = workspace.ExportProvider.GetExportedValue<ClassificationTypeMap>();

            var tagComputer = new SyntacticClassificationTaggerProvider.TagComputer(
                new SyntacticClassificationTaggerProvider(
                    workspace.GetService<IThreadingContext>(),
                    typeMap,
                    workspace.GetService<IGlobalOptionService>(),
                    AsynchronousOperationListenerProvider.NullProvider),
                subjectBuffer,
                diffTimeout: TimeSpan.MaxValue);

            // Capture the expected value before the await, in case it changes.
            var expectedLength = subjectBuffer.CurrentSnapshot.Length;
            int? actualVersionNumber = null;
            int? actualLength = null;
            var callstacks = new List<string>();
            tagComputer.TagsChanged += (s, e) =>
            {
                actualVersionNumber = e.Span.Snapshot.Version.VersionNumber;
                actualLength = e.Span.Length;
                callstacks.Add(new StackTrace().ToString());
                checkpoint.Release();
            };

            await checkpoint.Task;
            Assert.Equal(0, actualVersionNumber);
            Assert.Equal(expectedLength, actualLength);
            Assert.Equal(1, callstacks.Count);

            checkpoint = new Checkpoint();

            // Now delete the last character.
            var snapshot = subjectBuffer.Delete(new Span(subjectBuffer.CurrentSnapshot.Length - 1, 1));

            // Try to get the tags prior to TagsChanged firing.  This will force us to use the previous data we've
            // cached to produce the new results.  We don't actually care about the tags, so we just pass an empty
            // buffer for them to go into.
            tagComputer.AddTags(new NormalizedSnapshotSpanCollection(subjectBuffer.CurrentSnapshot.GetFullSpan()), tags: []);

            expectedLength = snapshot.Length;

            // NOTE: TagsChanged is raised on the UI thread, so there is no race between
            // assigning expected here and verifying in the event handler, because the
            // event handler can't run until we await.
            await checkpoint.Task;

            Assert.Equal(1, actualVersionNumber);
            Assert.Equal(2, actualLength);
            Assert.Equal(2, callstacks.Count);
        }
    }
}
