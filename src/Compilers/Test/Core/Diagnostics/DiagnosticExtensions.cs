// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis
{
    public static class DiagnosticExtensions
    {
        /// <summary>
        /// This is obsolete. Use Verify instead.
        /// </summary>
        public static void VerifyErrorCodes(this IEnumerable<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: true);
        }

        public static void VerifyErrorCodes(this ImmutableArray<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            VerifyErrorCodes((IEnumerable<Diagnostic>)actual, expected);
        }

        internal static void Verify(this DiagnosticBag actual, params DiagnosticDescription[] expected)
        {
            Verify(actual.AsEnumerable(), expected, errorCodeOnly: false);
        }

        public static void Verify(this IEnumerable<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: false);
        }

        public static void Verify(this IEnumerable<Diagnostic> actual, bool fallbackToErrorCodeOnlyForNonEnglish, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: fallbackToErrorCodeOnlyForNonEnglish && EnsureEnglishUICulture.PreferredOrNull != null);
        }

        public static void VerifyWithFallbackToErrorCodeOnlyForNonEnglish(this IEnumerable<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify(actual, true, expected);
        }

        public static void Verify(this ImmutableArray<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify((IEnumerable<Diagnostic>)actual, expected);
        }

        private static void Verify(IEnumerable<Diagnostic> actual, DiagnosticDescription[] expected, bool errorCodeOnly)
        {
            if (expected == null)
            {
                throw new ArgumentException("Must specify expected errors.", nameof(expected));
            }

            var includeDefaultSeverity = expected.Any() && expected.All(e => e.DefaultSeverity != null);
            var includeEffectiveSeverity = expected.Any() && expected.All(e => e.EffectiveSeverity != null);
            var unmatched = actual.Select(d => new DiagnosticDescription(d, errorCodeOnly, includeDefaultSeverity, includeEffectiveSeverity))
                                  .ToList();

            // Try to match each of the 'expected' errors to one of the 'actual' ones.
            // If any of the expected errors don't appear, fail test.
            foreach (var d in expected)
            {
                int index = unmatched.IndexOf(d);
                if (index > -1)
                {
                    unmatched.RemoveAt(index);
                }
                else
                {
                    Assert.True(false, DiagnosticDescription.GetAssertText(expected, actual));
                }
            }

            // If any 'extra' errors appear that were not in the 'expected' list, fail test.
            if (unmatched.Count > 0)
            {
                Assert.True(false, DiagnosticDescription.GetAssertText(expected, actual));
            }
        }

        public static TCompilation VerifyDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var diagnostics = c.GetDiagnostics();
            diagnostics.Verify(expected);
            VerifyAssemblyIds(c, diagnostics);

            return c;
        }

        private static void VerifyAssemblyIds<TCompilation>(
            TCompilation c, ImmutableArray<Diagnostic> diagnostics) where TCompilation : Compilation
        {
            foreach (var diagnostic in diagnostics)
            {
                // If this is a diagnostic about a missing assembly, make sure that we can get back
                // an AssemblyIdentity when we query the compiler.  If it's not a diagnostic about
                // a missing assembly, make sure we get no results back.
                if (c.IsUnreferencedAssemblyIdentityDiagnosticCode(diagnostic.Code))
                {
                    var assemblyIds = c.GetUnreferencedAssemblyIdentities(diagnostic);
                    Assert.False(assemblyIds.IsEmpty);

                    var diagnosticMessage = diagnostic.GetMessage();
                    foreach (var id in assemblyIds)
                    {
                        Assert.Contains(id.GetDisplayName(), diagnosticMessage);
                    }
                }
                else
                {
                    var assemblyIds = c.GetUnreferencedAssemblyIdentities(diagnostic);
                    Assert.True(assemblyIds.IsEmpty);
                }
            }
        }

        public static void VerifyAnalyzerOccurrenceCount<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            int expectedCount,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
            where TCompilation : Compilation
        {
            Assert.Equal(expectedCount, c.GetAnalyzerDiagnostics(analyzers, null, onAnalyzerException).Length);
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            return VerifyAnalyzerDiagnostics(c, analyzers, reportSuppressedDiagnostics: false, options, onAnalyzerException, expected);
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            bool reportSuppressedDiagnostics,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var newCompilation = c.GetCompilationWithAnalyzerDiagnostics(analyzers, options, onAnalyzerException, reportSuppressedDiagnostics, includeCompilerDiagnostics: false, CancellationToken.None, out var diagnostics);
            diagnostics.Verify(expected);
            return newCompilation;
        }

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            CancellationToken cancellationToken = default)
            where TCompilation : Compilation
        {
            return GetAnalyzerDiagnostics(c, analyzers, reportSuppressedDiagnostics: false, options, onAnalyzerException, cancellationToken);
        }

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            bool reportSuppressedDiagnostics,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            CancellationToken cancellationToken = default)
            where TCompilation : Compilation
        {
            _ = GetCompilationWithAnalyzerDiagnostics(c, analyzers, options, onAnalyzerException, reportSuppressedDiagnostics, includeCompilerDiagnostics: false, cancellationToken, out var diagnostics);
            return diagnostics;
        }

        public static TCompilation VerifySuppressedDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            CancellationToken cancellationToken = default,
            params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            // Verify suppression is unaffected by toggling /warnaserror.
            // Only perform this additional verification if the caller hasn't
            // explicitly overridden specific or general diagnostic options.
            if (c.Options.GeneralDiagnosticOption == ReportDiagnostic.Default &&
                c.Options.SpecificDiagnosticOptions.IsEmpty)
            {
                _ = c.VerifySuppressedDiagnostics(toggleWarnAsError: true, analyzers, options, onAnalyzerException, expected, cancellationToken);
            }

            return c.VerifySuppressedDiagnostics(toggleWarnAsError: false, analyzers, options, onAnalyzerException, expected, cancellationToken);
        }

        private static TCompilation VerifySuppressedDiagnostics<TCompilation>(
            this TCompilation c,
            bool toggleWarnAsError,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            DiagnosticDescription[] expectedDiagnostics,
            CancellationToken cancellationToken)
            where TCompilation : Compilation
        {
            if (toggleWarnAsError)
            {
                var toggledOption = c.Options.GeneralDiagnosticOption == ReportDiagnostic.Error ?
                    ReportDiagnostic.Default :
                    ReportDiagnostic.Error;
                c = (TCompilation)c.WithOptions(c.Options.WithGeneralDiagnosticOption(toggledOption));

                var builder = ArrayBuilder<DiagnosticDescription>.GetInstance(expectedDiagnostics.Length);
                foreach (var expected in expectedDiagnostics)
                {
                    // Toggle warnaserror and effective severity if following are true:
                    //  1. Default severity is not specified or specified as Warning
                    //  2. Effective severity is not specified or specified as Warning or Error
                    var defaultSeverityCheck = !expected.DefaultSeverity.HasValue ||
                        expected.DefaultSeverity.Value == DiagnosticSeverity.Warning;
                    var effectiveSeverityCheck = !expected.EffectiveSeverity.HasValue ||
                         expected.EffectiveSeverity.Value == DiagnosticSeverity.Warning ||
                         expected.EffectiveSeverity.Value == DiagnosticSeverity.Error;

                    DiagnosticDescription newExpected;
                    if (defaultSeverityCheck && effectiveSeverityCheck)
                    {
                        newExpected = expected.WithWarningAsError(!expected.IsWarningAsError);

                        if (expected.EffectiveSeverity.HasValue)
                        {
                            var newEffectiveSeverity = expected.EffectiveSeverity.Value == DiagnosticSeverity.Error ?
                                DiagnosticSeverity.Warning :
                                DiagnosticSeverity.Error;
                            newExpected = newExpected.WithEffectiveSeverity(newEffectiveSeverity);
                        }
                    }
                    else
                    {
                        newExpected = expected;
                    }

                    builder.Add(newExpected);
                }

                expectedDiagnostics = builder.ToArrayAndFree();
            }

            c = c.GetCompilationWithAnalyzerDiagnostics(analyzers, options, onAnalyzerException, reportSuppressedDiagnostics: true, includeCompilerDiagnostics: true, cancellationToken, out var diagnostics);
            diagnostics = diagnostics.WhereAsArray(d => d.IsSuppressed);
            diagnostics.Verify(expectedDiagnostics);
            return c; // note this is a new compilation
        }

        public static TCompilation VerifySuppressedAndFilteredDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            CancellationToken cancellationToken = default)
            where TCompilation : Compilation
        {
            // Verify suppressed diagnostics are filtered when reportSuppressedDiagnostics is false.
            // The actual verification is handled in GetCompilationWithAnalyzerDiagnostics.
            c = c.GetCompilationWithAnalyzerDiagnostics(analyzers, options, onAnalyzerException, reportSuppressedDiagnostics: false, includeCompilerDiagnostics: true, cancellationToken, out var diagnostics);
            return c; // note this is a new compilation
        }

        private static TCompilation GetCompilationWithAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            bool reportSuppressedDiagnostics,
            bool includeCompilerDiagnostics,
            CancellationToken cancellationToken,
            out ImmutableArray<Diagnostic> diagnostics)
            where TCompilation : Compilation
        {
            var analyzersArray = analyzers.ToImmutableArray();
            if (reportSuppressedDiagnostics != c.Options.ReportSuppressedDiagnostics)
            {
                c = (TCompilation)c.WithOptions(c.Options.WithReportSuppressedDiagnostics(reportSuppressedDiagnostics));
            }

            var analyzerManager = new AnalyzerManager(analyzersArray);
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(c, analyzersArray, options, analyzerManager, onAnalyzerException,
                analyzerExceptionFilter: null, reportAnalyzer: false, severityFilter: SeverityFilter.None, out var newCompilation, cancellationToken);
            Debug.Assert(newCompilation.SemanticModelProvider != null);
            var compilerDiagnostics = newCompilation.GetDiagnostics(cancellationToken);
            var analyzerDiagnostics = driver.GetDiagnosticsAsync(newCompilation).Result;
            var allDiagnostics = includeCompilerDiagnostics ?
                compilerDiagnostics.AddRange(analyzerDiagnostics) :
                analyzerDiagnostics;
            diagnostics = driver.ApplyProgrammaticSuppressionsAndFilterDiagnostics(allDiagnostics, newCompilation);

            if (!reportSuppressedDiagnostics)
            {
                Assert.True(diagnostics.All(d => !d.IsSuppressed));
            }

            return (TCompilation)newCompilation; // note this is a new compilation
        }

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(this Compilation compilation, IEnumerable<Diagnostic> diagnostics)
        {
            return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        public static bool IsDiagnosticAnalyzerSuppressed(this DiagnosticAnalyzer analyzer, CompilationOptions options)
        {
            return CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(analyzer, options);
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, EmitOptions options, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.GetEmitDiagnostics(options: options).Verify(expected);
            return c;
        }

        public static ImmutableArray<Diagnostic> GetEmitDiagnostics<TCompilation>(
            this TCompilation c,
            EmitOptions options = null,
            IEnumerable<ResourceDescription> manifestResources = null)
            where TCompilation : Compilation
        {
            var pdbStream = MonoHelpers.IsRunningOnMono() ? null : new MemoryStream();
            return c.Emit(new MemoryStream(), pdbStream: pdbStream, options: options, manifestResources: manifestResources).Diagnostics;
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            return VerifyEmitDiagnostics(c, EmitOptions.Default, expected);
        }

        public static ImmutableArray<Diagnostic> GetEmitDiagnostics<TCompilation>(this TCompilation c)
            where TCompilation : Compilation
        {
            return GetEmitDiagnostics(c, EmitOptions.Default);
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, IEnumerable<ResourceDescription> manifestResources, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.GetEmitDiagnostics(manifestResources: manifestResources).Verify(expected);
            return c;
        }

        public static string Concat(this string[] str)
        {
            return str.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s), sb => sb.ToString());
        }

        public static DiagnosticAnalyzer GetCompilerDiagnosticAnalyzer(string languageName)
        {
            return languageName == LanguageNames.CSharp ?
                (DiagnosticAnalyzer)new Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer() :
                new Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer();
        }

        public static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> GetCompilerDiagnosticAnalyzersMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
            builder.Add(LanguageNames.CSharp, ImmutableArray.Create(GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)));
            builder.Add(LanguageNames.VisualBasic, ImmutableArray.Create(GetCompilerDiagnosticAnalyzer(LanguageNames.VisualBasic)));
            return builder.ToImmutable();
        }

        public static AnalyzerReference GetCompilerDiagnosticAnalyzerReference(string languageName)
        {
            var analyzer = GetCompilerDiagnosticAnalyzer(languageName);
            return new AnalyzerImageReference(ImmutableArray.Create(analyzer), display: analyzer.GetType().FullName);
        }

        public static ImmutableDictionary<string, ImmutableArray<AnalyzerReference>> GetCompilerDiagnosticAnalyzerReferencesMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<AnalyzerReference>>();
            builder.Add(LanguageNames.CSharp, ImmutableArray.Create(GetCompilerDiagnosticAnalyzerReference(LanguageNames.CSharp)));
            builder.Add(LanguageNames.VisualBasic, ImmutableArray.Create(GetCompilerDiagnosticAnalyzerReference(LanguageNames.VisualBasic)));
            return builder.ToImmutable();
        }

        internal static string GetExpectedErrorLogHeader(CommonCompiler compiler)
        {
            var expectedToolName = compiler.GetToolName();
            var expectedVersion = compiler.GetAssemblyVersion();
            var expectedSemanticVersion = compiler.GetAssemblyVersion().ToString(fieldCount: 3);
            var expectedFileVersion = compiler.GetCompilerVersion();
            var expectedLanguage = compiler.GetCultureName();

            return string.Format(@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0"",
  ""version"": ""1.0.0"",
  ""runs"": [
    {{
      ""tool"": {{
        ""name"": ""{0}"",
        ""version"": ""{1}"",
        ""fileVersion"": ""{2}"",
        ""semanticVersion"": ""{3}"",
        ""language"": ""{4}""
      }},", expectedToolName, expectedVersion, expectedFileVersion, expectedSemanticVersion, expectedLanguage);
        }

        public static string Inspect(this Diagnostic e)
            => e.Location.IsInSource ? $"{e.Severity} {e.Id}: {e.GetMessage(CultureInfo.CurrentCulture)}" :
               e.Location.IsInMetadata ? "metadata: " : "no location: ";

        public static string ToString(this Diagnostic d, IFormatProvider formatProvider)
        {
            IFormattable formattable = d;
            return formattable.ToString(null, formatProvider);
        }
    }
}
