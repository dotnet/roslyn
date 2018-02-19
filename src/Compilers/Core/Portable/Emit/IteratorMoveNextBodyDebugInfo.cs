// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents additional info needed by iterator method implementation methods 
    /// (MoveNext methods) to properly emit necessary PDB data for iterator debugging.
    /// </summary>
    internal sealed class IteratorMoveNextBodyDebugInfo : StateMachineMoveNextBodyDebugInfo
    {
        public IteratorMoveNextBodyDebugInfo(Cci.IMethodDefinition kickoffMethod)
            : base(kickoffMethod)
        {
        }
    }
}
