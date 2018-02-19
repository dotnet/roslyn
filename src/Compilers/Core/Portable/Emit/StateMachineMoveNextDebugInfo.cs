// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
