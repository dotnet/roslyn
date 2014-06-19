// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked on each syntax tree in the compilation. Implementations
    /// should report diagnostics based primarily on the text of the program (implement <see cref="ISemanticModelAnalyzer"/>
    /// instead if you want to use semantic information).
    /// </summary>
    public interface ISyntaxTreeAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Called for each tree in the compilation.
        /// </summary>
        /// <param name="tree">A tree of the compilation</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        void AnalyzeSyntaxTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
    }
}
