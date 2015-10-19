﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [WorkItem(1032665)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestTagsChangedForEntireFile()
        {
            var code =
@"class Program2
{
    string x = @""/// <summary>$$
/// </summary>"";
}";
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
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

                SnapshotSpan span = default(SnapshotSpan);
                tagComputer.TagsChanged += (s, e) =>
                {
                    span = e.Span;
                    checkpoint.Release();
                };

                await checkpoint.Task.ConfigureAwait(true);
                checkpoint = new Checkpoint();

                // Now apply an edit that require us to reclassify more that just the current line
                subjectBuffer.Insert(document.CursorPosition.Value, "\"");

                await checkpoint.Task.ConfigureAwait(true);
                Assert.Equal(subjectBuffer.CurrentSnapshot.Length, span.Length);
            }
        }
    }
}
