// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    public class ProjectionBufferViewModel
    {
        public ObservableCollection<ITextBuffer> SourceBuffers { get; private set; }
        public ObservableCollection<SnapshotSpan> SourceSpans { get; private set; }

        public ProjectionBufferViewModel()
        {
            SourceBuffers = new ObservableCollection<ITextBuffer>();
            SourceSpans = new ObservableCollection<SnapshotSpan>();
        }
    }
}
