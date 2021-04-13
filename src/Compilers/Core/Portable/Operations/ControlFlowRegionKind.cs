// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Defines kinds of regions that can be present in a <see cref="ControlFlowGraph"/>
    /// </summary>
    public enum ControlFlowRegionKind
    {
        /// <summary>
        /// A root region encapsulating all <see cref="BasicBlock"/>s in a <see cref="ControlFlowGraph"/>
        /// </summary>
        Root,

        /// <summary>
        /// Region with the only purpose to represent the life-time of locals, intermediate results, and nested methods (local functions, lambdas).
        /// The lifetime of a local variable is the portion of program execution during which storage is guaranteed to be reserved for it.
        /// The lifetime of a nested method is the portion of program execution within which the method can be referenced.
        /// The lifetime of an intermediate result (capture) is the portion of program execution within which the result can be referenced.
        /// </summary>
        LocalLifetime,

        /// <summary>
        /// Region representing a try region. For example, <see cref="ITryOperation.Body"/>
        /// </summary>
        Try,

        /// <summary>
        /// Region representing <see cref="ICatchClauseOperation.Filter"/>
        /// </summary>
        Filter,

        /// <summary>
        /// Region representing <see cref="ICatchClauseOperation.Handler"/>
        /// </summary>
        Catch,

        /// <summary>
        /// Region representing a union of a <see cref="Filter"/> and the corresponding catch <see cref="Catch"/> regions. 
        /// Doesn't contain any <see cref="BasicBlock"/>s directly.
        /// </summary>
        FilterAndHandler,

        /// <summary>
        /// Region representing a union of a <see cref="Try"/> and all corresponding catch <see cref="Catch"/>
        /// and <see cref="FilterAndHandler"/> regions. Doesn't contain any <see cref="BasicBlock"/>s directly.
        /// </summary>
        TryAndCatch,

        /// <summary>
        /// Region representing <see cref="ITryOperation.Finally"/>
        /// </summary>
        Finally,

        /// <summary>
        /// Region representing a union of a <see cref="Try"/> and corresponding finally <see cref="Finally"/>
        /// region. Doesn't contain any <see cref="BasicBlock"/>s directly.
        /// 
        /// An <see cref="ITryOperation"/> that has a set of <see cref="ITryOperation.Catches"/> and a <see cref="ITryOperation.Finally"/> 
        /// at the same time is mapped to a <see cref="TryAndFinally"/> region with <see cref="TryAndCatch"/> region inside its <see cref="Try"/> region.
        /// </summary>
        TryAndFinally,

        /// <summary>
        /// Region representing the initialization for a VB <code>Static</code> local variable. This region will only be executed
        /// the first time a function is called.
        /// </summary>
        StaticLocalInitializer,

        /// <summary>
        /// Region representing erroneous block of code that is unreachable from the entry block.
        /// </summary>
        ErroneousBody,
    }
}
