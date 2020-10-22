// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal class DifferenceViewerPreview : IDisposable
    {
        private ICocoaDifferenceViewer _viewer;

        public DifferenceViewerPreview(ICocoaDifferenceViewer viewer)
        {
            Contract.ThrowIfNull(viewer);
            _viewer = viewer;
        }

        public ICocoaDifferenceViewer Viewer => _viewer;

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

            FatalError.ReportAndCatch(new Exception($"Dispose is not called how? viewer state : {_viewer.IsClosed}"));
        }
    }
}
