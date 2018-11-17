// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal sealed class DisposableToolTip : IDisposable
    {
        public readonly ToolTip ToolTip;
        private PreviewWorkspace _workspaceOpt;

        public DisposableToolTip(ToolTip toolTip, PreviewWorkspace workspaceOpt)
        {
            ToolTip = toolTip;
            _workspaceOpt = workspaceOpt;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _workspaceOpt, null)?.Dispose();
        }
    }
}
