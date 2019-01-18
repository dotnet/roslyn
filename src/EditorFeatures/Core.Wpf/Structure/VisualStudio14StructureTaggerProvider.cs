// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    /// <summary>
    /// Shared implementation of the outliner tagger provider.
    /// 
    /// Note: the outliner tagger is a normal buffer tagger provider and not a view tagger provider.
    /// This is important for two reasons.  The first is that if it were view-based then we would lose
    /// the state of the collapsed/open regions when they scrolled in and out of view.  Also, if the
    /// editor doesn't know about all the regions in the file, then it wouldn't be able to
    /// persist them to the SUO file to persist this data across sessions.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(VisualStudio14StructureTaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class VisualStudio14StructureTaggerProvider :
        AbstractStructureTaggerProvider<IOutliningRegionTag>
    {
        [ImportingConstructor]
        public VisualStudio14StructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, notificationService, textEditorFactoryService, editorOptionsFactoryService, projectionBufferFactoryService, listenerProvider)
        {
        }

        protected override IOutliningRegionTag CreateTag(
            IOutliningRegionTag parentTag, ITextSnapshot snapshot, BlockSpan blockSpan)
        {
            // Don't make outlining spans for non-collapsible block spans
            if (!blockSpan.IsCollapsible)
            {
                return null;
            }

            return new RoslynOutliningRegionTag(
                ThreadingContext,
                this.TextEditorFactoryService,
                this.ProjectionBufferFactoryService,
                this.EditorOptionsFactoryService,
                snapshot, blockSpan);
        }
    }
}
