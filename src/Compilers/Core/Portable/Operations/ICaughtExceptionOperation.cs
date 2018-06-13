﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a exception instance passed by an execution environment to an exception filter or handler.
    /// This node is produced only as part of a <see cref="ControlFlowGraph"/>.
    /// </summary>
    public interface ICaughtExceptionOperation : IOperation
    {
    }
}
