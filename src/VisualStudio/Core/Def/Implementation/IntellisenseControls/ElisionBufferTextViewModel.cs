// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    internal class ElisionBufferTextViewModel : ITextViewModel
    {
        public ElisionBufferTextViewModel(ITextDataModel dataModel, IElisionBuffer elisionBuffer)
        {
            DataModel = dataModel;
            ElisionBuffer = elisionBuffer;
            Properties = new PropertyCollection();
        }

        public IElisionBuffer ElisionBuffer { get; }
        public ITextDataModel DataModel { get; }

        public ITextBuffer DataBuffer => DataModel.DataBuffer;

        public ITextBuffer EditBuffer => ElisionBuffer;

        public ITextBuffer VisualBuffer => ElisionBuffer;

        public PropertyCollection Properties { get; }

        public void Dispose()
        {
        }

        public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint) => editBufferPoint;

        public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
            => editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);

        public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity) => true;
    }
}
