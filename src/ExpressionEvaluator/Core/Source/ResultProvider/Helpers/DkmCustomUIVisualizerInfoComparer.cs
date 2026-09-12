// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Evaluation;

#nullable enable
namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

/// <summary>
/// Allows filtering of duplicate <see cref="DkmCustomUIVisualizerInfo"/> instances that could be
/// placed in the same Success Evaluation Result so that the debugger UI does not show duplicates.
/// </summary>
internal class DkmCustomUIVisualizerInfoComparer : IEqualityComparer<DkmCustomUIVisualizerInfo>
{
    public static DkmCustomUIVisualizerInfoComparer Instance { get; } = new DkmCustomUIVisualizerInfoComparer();

    private DkmCustomUIVisualizerInfoComparer()
    {
    }

    // IEqualityComparer<DkmCustomUIVisualizerInfo> Members

    public bool Equals(DkmCustomUIVisualizerInfo? x, DkmCustomUIVisualizerInfo? y)
    {
        if (x is null || y is null)
        {
            return ReferenceEquals(x, y);
        }

        return string.Equals(x.UISideVisualizerAssemblyName, y.UISideVisualizerAssemblyName) &&
               string.Equals(x.UISideVisualizerTypeName, y.UISideVisualizerTypeName) &&
               string.Equals(x.DebuggeeSideVisualizerAssemblyName, y.DebuggeeSideVisualizerAssemblyName) &&
               string.Equals(x.DebuggeeSideVisualizerTypeName, y.DebuggeeSideVisualizerTypeName) &&
               string.Equals(x.Description, y.Description) &&
               x.ExtensionPartId == y.ExtensionPartId;
    }

    public int GetHashCode(DkmCustomUIVisualizerInfo obj)
    {
        int hash = 7;
        hash = 31 * hash + obj.UISideVisualizerAssemblyName.GetHashCode();
        hash = 31 * hash + obj.UISideVisualizerTypeName.GetHashCode();
        hash = 31 * hash + obj.DebuggeeSideVisualizerAssemblyName.GetHashCode();
        hash = 31 * hash + obj.DebuggeeSideVisualizerTypeName.GetHashCode();
        hash = 31 * hash + obj.Description.GetHashCode();
        hash = 31 * hash + obj.ExtensionPartId.GetHashCode();

        return hash;
    }
}
