// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal class DifferenceViewerPreview : IDisposable
    {
        private IWpfDifferenceViewer _viewer;

        public DifferenceViewerPreview(IWpfDifferenceViewer viewer)
        {
            Contract.ThrowIfNull(viewer);
            _viewer = viewer;
        }

        public IWpfDifferenceViewer Viewer => _viewer;

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_viewer != null && !_viewer.IsClosed)
            {
                _viewer.Close();
            }

            _viewer = null;
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

            FatalError.ReportWithoutCrash(new Exception($"Dispose is not called how? viewer state : {_viewer.IsClosed}"));
        }
    }
}
