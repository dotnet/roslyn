// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal class DisposableToolTip : IDisposable
    {
        public readonly ToolTip ToolTip;

        private PreviewWorkspace _workspaceOpt;

        private bool _disposed;

        public DisposableToolTip(ToolTip toolTip, PreviewWorkspace workspaceOpt)
        {
            ToolTip = toolTip;
            _workspaceOpt = workspaceOpt;
        }

        public void Dispose()
        {
            Debug.Assert(!_disposed);

            _disposed = true;
            _workspaceOpt?.Dispose();
            _workspaceOpt = null;
        }
    }
}

