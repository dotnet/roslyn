// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The base interface required to implement an incremental generator
    /// </summary>
    /// <remarks>
    /// The lifetime of a generator is controlled by the compiler.
    /// State should not be stored directly on the generator, as there
    /// is no guarantee that the same instance will be used on a 
    /// subsequent generation pass.
    /// </remarks>
    public interface IIncrementalGenerator
    {
        /// <summary>
        /// Called to initialize the generator and register generation steps via callbacks
        /// on the <paramref name="context"/>
        /// </summary>
        /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/> to register callbacks on</param>
        void Initialize(IncrementalGeneratorInitializationContext context);
    }
}
