// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal sealed class DisposableToolTip : IDisposable
    {
        public readonly ToolTip ToolTip;
        private readonly ReferenceCountedDisposable<PreviewWorkspace>? _workspace;

        private DisposableToolTip(ToolTip toolTip, ReferenceCountedDisposable<PreviewWorkspace>? workspace)
        {
            ToolTip = toolTip;
            _workspace = workspace?.AddReference();
        }

        public static ReferenceCountedDisposable<DisposableToolTip> CreateReferenceCounted(ToolTip toolTip, ReferenceCountedDisposable<PreviewWorkspace>? workspace)
            => new(new DisposableToolTip(toolTip, workspace));

        public void Dispose()
            => _workspace?.Dispose();
    }
}
