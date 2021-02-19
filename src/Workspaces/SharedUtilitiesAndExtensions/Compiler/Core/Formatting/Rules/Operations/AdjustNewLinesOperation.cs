// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// indicate how many lines are needed between two tokens
    /// </summary>
    internal readonly struct AdjustNewLinesOperation
    {
        public static readonly AdjustNewLinesOperation None = default;

        public readonly int Line;
        public readonly AdjustNewLinesOption Option;

        public AdjustNewLinesOperation(int line, AdjustNewLinesOption option)
        {
            Contract.ThrowIfFalse(option != AdjustNewLinesOption.ForceLines || line > 0);
            Contract.ThrowIfFalse(option != AdjustNewLinesOption.PreserveLines || line >= 0);
            Contract.ThrowIfFalse(option != AdjustNewLinesOption.ForceLinesIfOnSingleLine || line > 0);

            Line = line;
            Option = option;
        }
    }
}
