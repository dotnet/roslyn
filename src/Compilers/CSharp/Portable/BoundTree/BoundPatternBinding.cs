// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct BoundPatternBinding
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
