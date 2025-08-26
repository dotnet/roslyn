// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

internal sealed class DisposableToolTip(ToolTip toolTip, ReferenceCountedDisposable<PreviewWorkspace> workspaceOpt) : IDisposable
{
    public readonly ToolTip ToolTip = toolTip;
    private readonly ReferenceCountedDisposable<PreviewWorkspace> _workspaceOpt = workspaceOpt;

    public void Dispose()
        => _workspaceOpt.Dispose();
}
