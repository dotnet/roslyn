// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal enum ScopeType
    {
        Variable,
        TryCatchFinally,
        Try,
        Catch,
        Filter,
        Finally,
        Fault,

        /// <summary>
        /// Scope of user-defined variable hoisted to state machine.
        /// </summary>
        StateMachineVariable,
    }
}
