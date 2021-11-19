// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Test.Utilities
{
    public static class CompilationUtils
    {
        public static void ValidateNoCompileErrors(ImmutableArray<Diagnostic> compilerDiagnostics)
        {
            var compileErrors = compilerDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

            if (compileErrors.Any())
            {
                var builder = new StringBuilder();
                builder.Append($"Test contains compilation error(s):");
                builder.Append(string.Concat(compileErrors.Select(x => "\n" + x.ToString())));

                string message = builder.ToString();
                Assert.True(false, message);
            }
        }
    }
}
