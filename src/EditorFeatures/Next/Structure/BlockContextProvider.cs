// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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
        public RoslynBlockContextProvider(
            ITextEditorFactoryService textEditorFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService)
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
                if (blockTag is RoslynOutliningRegionTag)
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
                    CreateElisionBufferView, CreateElisionBuffer);

                return result;
            }

            private IWpfTextView CreateElisionBufferView(ITextBuffer finalBuffer)
            {
                return RoslynOutliningRegionTag.CreateElisionBufferView(
                    _provider._textEditorFactoryService, finalBuffer);
            }

            private ITextBuffer CreateElisionBuffer()
            {
                var statementBuffers = new List<ITextBuffer>();
                var blockTags = GetBlockTags();

                var textSnapshot = blockTags[0].StatementSpan.Snapshot;
                // var headerSpans = new List<SnapshotSpan>();

                var objects = new List<object>();

                for (var i = 0; i < blockTags.Count; i++)
                {
                    if (i > 0)
                    {
                        objects.Add("\r\n");
                    }

                    var blockTag = blockTags[i];
                    var fullStatementSpan = blockTag.StatementSpan;
                    var collapseSpan = blockTag.Span;

                    var statementLine = textSnapshot.GetLineFromPosition(blockTag.StatementSpan.Start);
                    var lineStart = statementLine.Start.Position;
                    var lineEnd = Math.Min(statementLine.End.Position, collapseSpan.Start);

                    var headerSpan = new SnapshotSpan(textSnapshot, Span.FromBounds(lineStart, lineEnd));
                    var mappingSpan = headerSpan.CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);

                    objects.Add(mappingSpan);
                    if (statementLine.End.Position < collapseSpan.Start)
                    {
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