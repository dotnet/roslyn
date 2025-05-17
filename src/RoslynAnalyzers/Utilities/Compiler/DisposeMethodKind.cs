// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes different kinds of Dispose-like methods.
    /// </summary>
    internal enum DisposeMethodKind
    {
        /// <summary>
        /// Not a dispose-like method.
        /// </summary>
        None,

        /// <summary>
        /// An override of <see cref="System.IDisposable.Dispose"/>.
        /// </summary>
        Dispose,

        /// <summary>
        /// A virtual method named Dispose that takes a single Boolean parameter, as
        /// is used when implementing the standard Dispose pattern.
        /// </summary>
        DisposeBool,

        /// <summary>
        /// A method named DisposeAsync that has no parameters and returns Task.
        /// </summary>
        DisposeAsync,

        /// <summary>
        /// An overridden method named DisposeCoreAsync that takes a single Boolean parameter and returns Task, as
        /// is used when implementing the standard DisposeAsync pattern.
        /// </summary>
        DisposeCoreAsync,

        /// <summary>
        /// A method named Close on a type that implements <see cref="System.IDisposable"/>.
        /// </summary>
        Close,

        /// <summary>
        /// A method named CloseAsync that has no parameters and returns Task.
        /// </summary>
        CloseAsync,
    }
}
