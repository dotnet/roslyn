// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests.Verifiers
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal static class CSharpVerifierHelper
    {
        /// <summary>
        /// Gets the diagnostics the compiler reports for nullable reference types, configured with
        /// <see cref="ReportDiagnostic.Warn"/> severity.
        /// </summary>
        /// <remarks>
        /// By default, the compiler reports diagnostics for nullable reference types at
        /// <see cref="DiagnosticSeverity.Warning"/>, and the analyzer test framework defaults to only validating
        /// diagnostics at <see cref="DiagnosticSeverity.Error"/>. This map contains all compiler diagnostic IDs
        /// related to nullability mapped to <see cref="ReportDiagnostic.Error"/>, which is then used to enable all
        /// of these warnings for default validation during analyzer and code fix tests.
        /// </remarks>
        internal static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } = GetNullableWarningsFromCompiler();

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = { "/warnaserror:nullable" };
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            return commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;
        }
    }
}
