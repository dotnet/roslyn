// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class SourceGeneratorTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => Enumerable.Empty<DiagnosticAnalyzer>();

        /// <summary>
        /// Returns the source generators being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="ISourceGenerator"/> and/or <see cref="T:Microsoft.CodeAnalysis.IIncrementalGenerator"/> to be used.</returns>
        protected override abstract IEnumerable<Type> GetSourceGenerators();
    }
}
