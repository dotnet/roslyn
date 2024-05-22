// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview;

internal interface IDifferenceViewerPreview<TDifferenceViewer> : IDisposable
    where TDifferenceViewer : IDifferenceViewer
{
    public TDifferenceViewer Viewer { get; }
}
