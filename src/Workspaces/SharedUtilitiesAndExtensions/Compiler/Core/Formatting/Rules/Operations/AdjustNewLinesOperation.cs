// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

/// <summary>
/// indicate how many lines are needed between two tokens
/// </summary>
internal sealed class AdjustNewLinesOperation
{
    internal AdjustNewLinesOperation(int line, AdjustNewLinesOption option)
    {
        Contract.ThrowIfFalse(option != AdjustNewLinesOption.ForceLines || line > 0);
        Contract.ThrowIfFalse(option != AdjustNewLinesOption.PreserveLines || line >= 0);
        Contract.ThrowIfFalse(option != AdjustNewLinesOption.ForceLinesIfOnSingleLine || line > 0);

        this.Line = line;
        this.Option = option;
    }

    public int Line { get; }
    public AdjustNewLinesOption Option { get; }
}
