// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Microsoft.CodeAnalysis.Editor.Structure
{
    [Name(nameof(RoslynBlockContextProvider)), Order]
    [Export(typeof(IBlockContextProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class RoslynBlockContextProvider : ForegroundThreadAffinitizedObject, IBlockContextProvider
    {
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynBlockContextProvider(
            IThreadingContext threadingContext,
            ITextEditorFactoryService textEditorFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService)
            : base(threadingContext)
        {
            _textEditorFactoryService = textEditorFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
        }

        public Task<IBlockContextSource> TryCreateBlockContextSourceAsync(
            ITextBuffer textBuffer, CancellationToken token)
        {
            this.AssertIsForeground();

            var result = textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new BlockContextSource(this));
            return Task.FromResult<IBlockContextSource>(result);
        }

        private class BlockContextSource : IBlockContextSource
        {
            private readonly RoslynBlockContextProvider _provider;

            public BlockContextSource(RoslynBlockContextProvider provider)
            {
                _provider = provider;
            }

            public void Dispose()
            {
            }

            public Task<IBlockContext> GetBlockContextAsync(
                IBlockTag blockTag, ITextView view, CancellationToken token)
            {
                if (blockTag is RoslynBlockTag)
                {
                    var result = new RoslynBlockContext(_provider, blockTag, view);
                    return Task.FromResult<IBlockContext>(result);
                }

                return SpecializedTasks.Default<IBlockContext>();
            }
        }

        private class RoslynBlockContext : ForegroundThreadAffinitizedObject, IBlockContext
        {
            private readonly RoslynBlockContextProvider _provider;

            public IBlockTag BlockTag { get; }

            public ITextView TextView { get; }

            public RoslynBlockContext(
                RoslynBlockContextProvider provider,
                IBlockTag blockTag,
                ITextView textView)
                : base(provider.ThreadingContext)
            {
                _provider = provider;
                BlockTag = blockTag;
                TextView = textView;
            }

            public object Content => CreateContent();

            private object CreateContent()
            {
                this.AssertIsForeground();

                var result = new ViewHostingControl(
                    CreateElisionBufferView, CreateProjectionBufferForBlockHeaders);

                return result;
            }

            private IWpfTextView CreateElisionBufferView(ITextBuffer finalBuffer)
            {
                return BlockTagState.CreateShrunkenTextView(
                    _provider.ThreadingContext,
                    _provider._textEditorFactoryService, finalBuffer);
            }

            private ITextBuffer CreateProjectionBufferForBlockHeaders()
            {
                // We want to create a projection buffer that will show the block tags like so:
                //
                //      namespace N
                //          class C
                //              public void M()
                //
                // To do this, we grab the 'header' part of each block span and stich them 
                // all together one after the other.
                //
                // We consider hte 'header' to be from the start of the line containing the
                // block all the way to the end of the line, or the start of the collapsed
                // block region (whichever is closer).  That way, if you have multi-line
                // statement start (for example, with a multi-line if-expression) we'll only
                // take the first line.  In the case where we're cutting things off, we'll
                // put in a ... to indicate as such.

                var blockTags = GetBlockTags();

                var textSnapshot = blockTags[0].StatementSpan.Snapshot;

                // The list of mapping spans, ...'s and newlines that we'll build the 
                // projection buffer out of.
                var objects = new List<object>();

                for (var i = 0; i < blockTags.Count; i++)
                {
                    var blockTag = blockTags[i];
                    var fullStatementSpan = blockTag.StatementSpan;

                    if (blockTag.Parent != null &&
                        textSnapshot.AreOnSameLine(fullStatementSpan.Start, blockTag.Parent.StatementSpan.Start))
                    {
                        // Ignore block tags on the same line as their parent.  We'll have already included
                        // the contents of the line when we added the parent tag, and the editor will crash
                        // if we create overlapping spans in the projection buffer.
                        continue;
                    }

                    if (i > 0)
                    {
                        objects.Add("\r\n");
                    }

                    var statementLine = textSnapshot.GetLineFromPosition(fullStatementSpan.Start);

                    var lineStart = statementLine.Start.Position;
                    var lineEnd = statementLine.End.Position;

                    var headerSpan = new SnapshotSpan(textSnapshot, Span.FromBounds(lineStart, lineEnd));
                    var mappingSpan = headerSpan.CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);

                    objects.Add(mappingSpan);
                    if (lineEnd < blockTag.Span.Start)
                    {
                        // If we had to cut off the line, then add a ... to indicate as such.
                        objects.Add("...");
                    }
                }

                return _provider._projectionBufferFactoryService.CreateProjectionBuffer(
                    null, objects, ProjectionBufferOptions.None);
            }

            private List<IBlockTag> GetBlockTags()
            {
                var result = new List<IBlockTag>();
                AddBlockTags(result, BlockTag);
                return result;
            }

            private void AddBlockTags(List<IBlockTag> result, IBlockTag blockTag)
            {
                if (blockTag == null)
                {
                    return;
                }

                AddBlockTags(result, blockTag.Parent);
                result.Add(blockTag);
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
