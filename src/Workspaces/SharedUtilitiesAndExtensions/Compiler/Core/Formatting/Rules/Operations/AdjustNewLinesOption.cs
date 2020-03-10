// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Options for AdjustNewLinesOperation.
    /// 
    /// PreserveLines means the operation will leave lineBreaks as it is if original lineBreaks are
    /// equal or greater than given lineBreaks
    /// 
    /// ForceLines means the operation will force existing lineBreaks to the given lineBreaks.
    /// </summary>
    internal enum AdjustNewLinesOption
    {
        PreserveLines,
        ForceLines,
        ForceLinesIfOnSingleLine,
    }
}
