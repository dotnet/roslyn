// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked when the compiler has analyzed a method body or field initializer, and which can return
    /// an additional analyzer to be used for method body.
    /// </summary>
    public interface ICodeBlockStartedAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Invoked when the compiler has performed semantic analysis on a method body or field
        /// initializer, and which can return an additional analyzer to be used for the body.
        /// </summary>
        /// <param name="codeBlock">The code block of a method or a field initializer</param>
        /// <param name="ownerSymbol">The method or field</param>
        /// <param name="semanticModel">A SemanticModel for the compilation unit</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="options">A set of options passed in from the host.</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        /// <returns>An analyzer that will be used for the method body, or null</returns>
        ICodeBlockEndedAnalyzer OnCodeBlockStarted(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken);
    }
}
