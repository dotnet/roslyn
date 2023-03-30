// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    /// <summary>
    /// With Roslyn 4.x diagnostics that have been suppressed by a DiagnosticSuppressor are no longer reported when 'report suppressed diagnostics' is not explicitly enabled via options.
    /// Since reporting suppression via DiagnosticSuppressor is now configured the same as reporting suppression via #pragma, 'report suppressed diagnostics' must be enabled during the diagnostic
    /// test stage to be able to test a DiagnosticSuppressor, but must be disabled during the suppressor test stage.
    /// To minimize the risk of side effects, 'report suppressed diagnostics' will only be enabled when testing a DiagnosticSuppressor.
    /// </summary>
    internal static class LightupCompilationWithAnalyzers
    {
        private static readonly Type CompilationType = typeof(CompilationWithAnalyzers);
        private static readonly Assembly FrameworkAssembly = CompilationType.GetTypeInfo().Assembly;
        private static readonly Type? OptionsType = FrameworkAssembly.DefinedTypes.FirstOrDefault(type => type.Name == "CompilationWithAnalyzersOptions")?.AsType();
        private static readonly Type? SuppressorType = FrameworkAssembly.DefinedTypes.FirstOrDefault(type => type.Name == "DiagnosticSuppressor")?.AsType();
        private static readonly int FrameworkMajorVersion = FrameworkAssembly.GetName().Version?.Major ?? 0;

        public static readonly Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, CancellationToken, CompilationWithAnalyzers> Create = BuildCreatorFunc();

        private static bool IsTestingDiagnosticSuppressors(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var suppressorType = SuppressorType;

            if (FrameworkMajorVersion < 4 || suppressorType == null)
            {
                return false;
            }

            return analyzers.Any(analyzer => suppressorType.IsInstanceOfType(analyzer));
        }

        private static Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, CancellationToken, CompilationWithAnalyzers> BuildCreatorFunc()
        {
            if (OptionsType != null && SuppressorType != null && FrameworkMajorVersion >= 4)
            {
                return (compilation, analyzers, options, cancellationToken) =>
                {
                    if (!IsTestingDiagnosticSuppressors(analyzers))
                    {
                        return compilation.WithAnalyzers(analyzers, options, cancellationToken);
                    }

                    var compilationWithAnalyzersOptions = Activator.CreateInstance(OptionsType, options, null, true, false, true);

                    return (CompilationWithAnalyzers)Activator.CreateInstance(CompilationType, compilation, analyzers, compilationWithAnalyzersOptions)!;
                };
            }

            return (compilation, analyzers, options, cancellationToken) => compilation.WithAnalyzers(analyzers, options, cancellationToken);
        }
    }
}
