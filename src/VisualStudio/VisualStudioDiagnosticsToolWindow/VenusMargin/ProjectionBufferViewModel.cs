// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    public class ProjectionBufferViewModel
    {
        public ObservableCollection<ITextBuffer> SourceBuffers { get; }
        public ObservableCollection<SnapshotSpan> SourceSpans { get; }

        public ProjectionBufferViewModel()
        {
            SourceBuffers = [];
            SourceSpans = [];
        }
    }
}
