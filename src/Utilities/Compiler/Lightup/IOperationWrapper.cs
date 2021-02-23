// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using Microsoft.CodeAnalysis;

    internal interface IOperationWrapper
    {
        IOperation? WrappedOperation { get; }
    }
}

#endif
