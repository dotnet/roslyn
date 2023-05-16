// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal sealed class DifferenceViewerPreview : IDisposable
    {
        private IDifferenceViewer _viewer;

        private DifferenceViewerPreview(
            IDifferenceViewer viewer,
            ReferenceCountedDisposable<PreviewWorkspace>? leftWorkspace,
            ReferenceCountedDisposable<PreviewWorkspace>? rightWorkspace)
        {
            Contract.ThrowIfNull(viewer);
            _viewer = viewer;
            LeftWorkspace = leftWorkspace?.AddReference();
            RightWorkspace = rightWorkspace?.AddReference();
        }

        public static ReferenceCountedDisposable<DifferenceViewerPreview> CreateReferenceCounted(
            IDifferenceViewer viewer,
            ReferenceCountedDisposable<PreviewWorkspace>? leftWorkspace,
            ReferenceCountedDisposable<PreviewWorkspace>? rightWorkspace)
            => new(new DifferenceViewerPreview(viewer, leftWorkspace, rightWorkspace));

        public IDifferenceViewer Viewer => _viewer;

        private ReferenceCountedDisposable<PreviewWorkspace>? LeftWorkspace { get; }
        private ReferenceCountedDisposable<PreviewWorkspace>? RightWorkspace { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_viewer != null && !_viewer.IsClosed)
            {
                _viewer.Close();
            }

            _viewer = null!;

            LeftWorkspace?.Dispose();
            RightWorkspace?.Dispose();
        }

        ~DifferenceViewerPreview()
        {
            // make sure we are not leaking diff viewer
            // we can't close the view from finalizer thread since it must be same
            // thread (owner thread) this UI is created.
            if (Environment.HasShutdownStarted)
            {
                return;
            }

            FatalError.ReportAndCatch(new Exception($"Dispose is not called how? viewer state : {_viewer.IsClosed}"));
        }
    }
}
