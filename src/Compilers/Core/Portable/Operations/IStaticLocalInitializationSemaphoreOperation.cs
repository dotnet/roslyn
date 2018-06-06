// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents the check during initialization of a VB static local that is initialized on the first call of the function, and never again.
    /// If the semaphore operation returns true, the static local has not yet been initialized, and the initializer will be run. If it returns
    /// false, then the local has already been initialized, and the static local initializer region will be skipped.
    /// This node is produced only as part of a <see cref="ControlFlowGraph"/>.
    /// </summary>
    public interface IStaticLocalInitializationSemaphoreOperation : IOperation
    {
        /// <summary>
        /// The static local variable that is possibly initialized.
        /// </summary>
        ILocalSymbol Local { get; }
    }
}
