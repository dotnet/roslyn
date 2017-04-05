// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
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
}
