// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct BoundPatternBinding
    {
        public readonly BoundExpression VariableAccess;
        public readonly BoundDagTemp TempContainingValue;
        public BoundPatternBinding(BoundExpression variableAccess, BoundDagTemp tempContainingValue)
        {
            this.VariableAccess = variableAccess;
            this.TempContainingValue = tempContainingValue;
        }
        public override string ToString()
        {
            return GetDebuggerDisplay();
        }
        internal string GetDebuggerDisplay()
        {
            return $"({VariableAccess.GetDebuggerDisplay()} = {TempContainingValue.GetDebuggerDisplay()})";
        }
    }
}
