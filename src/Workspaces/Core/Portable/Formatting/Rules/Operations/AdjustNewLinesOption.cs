// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
