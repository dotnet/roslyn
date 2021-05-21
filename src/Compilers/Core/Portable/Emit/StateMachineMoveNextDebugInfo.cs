// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Information associated with method body of a state machine MoveNext method.
    /// </summary>
    internal abstract class StateMachineMoveNextBodyDebugInfo
    {
        /// <summary>
        ///  Original async/iterator method transformed into MoveNext() 
        /// </summary>
        public readonly Cci.IMethodDefinition KickoffMethod;

        public StateMachineMoveNextBodyDebugInfo(Cci.IMethodDefinition kickoffMethod)
        {
            Debug.Assert(kickoffMethod != null);
            KickoffMethod = kickoffMethod;
        }
    }
}
