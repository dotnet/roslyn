// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The base interface required to implement a source generator
    /// </summary>
    /// <remarks>
    /// The lifetime of a generator is controlled by the compiler.
    /// State should not be stored directly on the generator, as there
    /// is no guarantee that the same instance will be used on a 
    /// subsequent generation pass.
    /// </remarks>
    public interface ISourceGenerator
    {
        /// <summary>
        /// Called before generation occurs. A generator can use the <paramref name="context"/>
        /// to register callbacks required to perform generation.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorInitializationContext"/> to register callbacks on</param>
        void Initialize(GeneratorInitializationContext context);

        /// <summary>
        /// Called to perform source generation. A generator can use the <paramref name="context"/>
        /// to add source files via the <see cref="GeneratorExecutionContext.AddSource(string, SourceText)"/> 
        /// method.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorExecutionContext"/> to add source to</param>
        /// <remarks>
        /// This call represents the main generation step. It is called after a <see cref="Compilation"/> is 
        /// created that contains the user written code. 
        /// 
        /// A generator can use the <see cref="GeneratorExecutionContext.Compilation"/> property to
        /// discover information about the users compilation and make decisions on what source to 
        /// provide. 
        /// </remarks>
        void Execute(GeneratorExecutionContext context);
    }
}

