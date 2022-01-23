// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
