// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked when compilation starts, and which can return an additional analyzer to be used for the compilation.
    /// </summary>
    public interface ICompilationStartedAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Called at the beginning of the compilation.
        /// </summary>
        /// <param name="compilation">The compilation</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        /// <returns>An analyzer that is used for the compilation, or null.</returns>
        ICompilationEndedAnalyzer OnCompilationStarted(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
    }
}
