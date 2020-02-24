// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeRefactoringTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        /// <summary>
        /// Gets the syntax kind enumeration type for the current code refactoring test.
        /// </summary>
        public abstract Type SyntaxKindType { get; }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => Enumerable.Empty<DiagnosticAnalyzer>();

        /// <summary>
        /// Returns the code refactorings being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeRefactoringProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders();
    }
}
