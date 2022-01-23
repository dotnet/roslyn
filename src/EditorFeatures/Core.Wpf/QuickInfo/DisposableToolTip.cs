// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => Interlocked.Exchange(ref _workspaceOpt, null)?.Dispose();
    }
}
