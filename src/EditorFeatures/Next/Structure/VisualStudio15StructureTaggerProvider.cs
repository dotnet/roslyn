// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Structure
{
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(VisualStudio15StructureTaggerProvider))]
    [TagType(typeof(IBlockTag2))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class VisualStudio15StructureTaggerProvider : 
        AbstractStructureTaggerProvider<IBlockTag2>
    {
        [ImportingConstructor]
        public VisualStudio15StructureTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(notificationService, textEditorFactoryService, editorOptionsFactoryService, projectionBufferFactoryService, asyncListeners)
        {
        }

        public override bool Equals(IBlockTag2 x, IBlockTag2 y)
        {
            // This is only called if the spans for the tags were the same. In that case, we consider ourselves the same
            // unless the CollapsedForm properties are different.
            return EqualityComparer<object>.Default.Equals(x.CollapsedForm, y.CollapsedForm);
        }

        public override int GetHashCode(IBlockTag2 obj)
        {
            return EqualityComparer<object>.Default.GetHashCode(obj.CollapsedForm);
        }

        protected override IBlockTag2 CreateTag(
            IBlockTag2 parentTag, ITextSnapshot snapshot, BlockSpan region)
        {
            return new RoslynRegionTag(this, parentTag, snapshot, region);
        }

        private class RoslynRegionTag : RegionTag, IBlockTag2
        {
            public IBlockTag2 Parent { get; }
            public int Level { get; }
            public SnapshotSpan Span { get; }
            public SnapshotSpan StatementSpan { get; }

            public string Type => BlockSpan.Type;
            public bool IsCollapsible => true;

            public RoslynRegionTag(
                AbstractStructureTaggerProvider<IBlockTag2> provider,
                IBlockTag2 parent,
                ITextSnapshot snapshot,
                BlockSpan outliningSpan) : 
                base(provider, snapshot, outliningSpan)
            {
                Parent = parent;
                Level = parent == null ? 0 : parent.Level + 1;
                Span = outliningSpan.TextSpan.ToSnapshotSpan(snapshot);
                StatementSpan = outliningSpan.HintSpan.ToSnapshotSpan(snapshot);
            }
        }
    }
}