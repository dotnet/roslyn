// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
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
    [Export(typeof(OutliningTaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class OutliningTaggerProvider : 
        AbstractOutliningTaggerProvider<IOutliningRegionTag>
    {
        [ImportingConstructor]
        public OutliningTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(notificationService, textEditorFactoryService, editorOptionsFactoryService, projectionBufferFactoryService, asyncListeners)
        {
        }

        public override bool Equals(IOutliningRegionTag x, IOutliningRegionTag y)
        {
            // This is only called if the spans for the tags were the same. In that case, we consider ourselves the same
            // unless the CollapsedForm properties are different.
            return EqualityComparer<object>.Default.Equals(x.CollapsedForm, y.CollapsedForm);
        }

        public override int GetHashCode(IOutliningRegionTag obj)
        {
            return EqualityComparer<object>.Default.GetHashCode(obj.CollapsedForm);
        }

        protected override IOutliningRegionTag CreateTag(
            ITextSnapshot snapshot, OutliningSpan region)
        {
            return new Tag(
                snapshot.TextBuffer,
                region.BannerText,
                new SnapshotSpan(snapshot, region.HintSpan.ToSpan()),
                region.AutoCollapse,
                region.IsDefaultCollapsed,
                TextEditorFactoryService,
                ProjectionBufferFactoryService, 
                EditorOptionsFactoryService);               
        }
    }
}