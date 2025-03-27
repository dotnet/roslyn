// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Specifies a kind of instrumentation to be applied in generated code.
    /// </summary>
    public enum InstrumentationKind
    {
        /// <summary>
        /// No instrumentation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Instruments the code to add test coverage.
        /// </summary>
        TestCoverage = 1,

        /// <summary>
        /// Instruments all methods, local functions and lambdas in the code with calls to <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/>,
        /// to guard against accidental stack overflow.
        /// </summary>
        StackOverflowProbing = 2,

        /// <summary>
        /// Instruments code with calls to <see cref="CancellationToken.ThrowIfCancellationRequested"/> on a module-level defined 
        /// <see cref="CancellationToken"/> to enable cancellation of code that hasn't necessarily been written as cancellable.
        /// </summary>
        /// <remarks>
        /// The <see cref="CancellationToken"/> is emitted to a static field <code>&lt;PrivateImplementationDetails&gt;.ModuleCancellationToken</code>.
        /// </remarks>
        ModuleCancellation = 3
    }

    internal static class InstrumentationKindExtensions
    {
        internal const InstrumentationKind LocalStateTracing = (InstrumentationKind)(-1);

        internal static bool IsValid(this InstrumentationKind value)
            => value is >= InstrumentationKind.None and <= InstrumentationKind.ModuleCancellation;
    }
}
