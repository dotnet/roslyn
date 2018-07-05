// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    /// <summary>
    /// Abstract dispose value for <see cref="AbstractLocation"/>/<see cref="IOperation"/> tracked by <see cref="DisposeAnalysis"/>.
    /// </summary>
    internal enum DisposeAbstractValueKind
    {
        /// <summary>
        /// Indicates locations that are not disposable, e.g. value types, constants, etc.
        /// </summary>
        NotDisposable,

        /// <summary>
        /// Indicates disposable locations that are not disposed.
        /// </summary>
        NotDisposed,

        /// <summary>
        /// Indicates disposable locations that are disposed.
        /// </summary>
        Disposed,

        /// <summary>
        /// Indicates disposable locations that may be disposed.
        /// </summary>
        MaybeDisposed
    }
}
