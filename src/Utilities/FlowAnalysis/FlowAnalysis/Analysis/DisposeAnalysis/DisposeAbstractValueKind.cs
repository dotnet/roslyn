// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    /// <summary>
    /// Abstract dispose value for <see cref="AbstractLocation"/>/<see cref="IOperation"/> tracked by <see cref="DisposeAnalysis"/>.
    /// </summary>
    public enum DisposeAbstractValueKind
    {
        /// <summary>
        /// Indicates locations that are not disposable, e.g. value types, constants, etc.
        /// </summary>
        NotDisposable,

        /// <summary>
        /// Indicates a value for disposable locations that are not feasible on the given program path.
        /// For example,
        /// <code>
        ///     var x = flag ? new Disposable() : null;
        ///     if (x == null)
        ///     {
        ///         // Disposable allocation above cannot exist on this code path.
        ///     }
        /// </code>
        /// </summary>
        Invalid,

        /// <summary>
        /// Indicates disposable locations that are not disposed.
        /// </summary>
        NotDisposed,

        /// <summary>
        /// Indicates disposable locations that have escaped the declaring method's scope.
        /// For example, a disposable allocation assigned to a field/property or
        /// escaped as a return value for a function, or assigned to a ref or out parameter, etc.
        /// </summary>
        Escaped,

        /// <summary>
        /// Indicates disposable locations that are either not disposed or escaped.
        /// </summary>
        NotDisposedOrEscaped,

        /// <summary>
        /// Indicates disposable locations that are disposed.
        /// </summary>
        Disposed,

        /// <summary>
        /// Indicates disposable locations that may be disposed on some program path(s).
        /// </summary>
        MaybeDisposed,

        /// <summary>
        /// Indicates disposable locations whose dispose state is unknown.
        /// </summary>
        Unknown,
    }
}
