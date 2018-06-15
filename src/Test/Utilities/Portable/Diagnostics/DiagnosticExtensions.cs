﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis
{
    public static class DiagnosticExtensions
    {
        private const int EN_US = 1033;

        public static Action<Exception, DiagnosticAnalyzer, Diagnostic> FailFastOnAnalyzerException = (e, a, d) => FailFast.OnFatalException(e);

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

            var unmatched = actual.Select(d => new DiagnosticDescription(d, errorCodeOnly)).ToList();

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
                bool logAnalyzerExceptionAsDiagnostics = true,
                params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            return VerifyAnalyzerDiagnostics(c, analyzers, reportSuppressedDiagnostics: false, options: options, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics, expected: expected);
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(
                this TCompilation c,
                DiagnosticAnalyzer[] analyzers,
                bool reportSuppressedDiagnostics,
                AnalyzerOptions options = null,
                Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
                bool logAnalyzerExceptionAsDiagnostics = true,
                params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c = c.GetAnalyzerDiagnostics(analyzers, options, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, reportSuppressedDiagnostics, diagnostics: out var diagnostics);
            diagnostics.Verify(expected);
            return c; // note this is a new compilation
        }

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            bool logAnalyzerExceptionAsDiagnostics = true)
            where TCompilation : Compilation
        {
            return GetAnalyzerDiagnostics(c, analyzers, reportSuppressedDiagnostics: false, options: options, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
        }

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            bool reportSuppressedDiagnostics,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            bool logAnalyzerExceptionAsDiagnostics = true)
            where TCompilation : Compilation
        {
            c = GetAnalyzerDiagnostics(c, analyzers, options, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, reportSuppressedDiagnostics, out var diagnostics);
            return diagnostics;
        }

        private static TCompilation GetAnalyzerDiagnostics<TCompilation>(
                this TCompilation c,
                DiagnosticAnalyzer[] analyzers,
                AnalyzerOptions options,
                Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
                bool logAnalyzerExceptionAsDiagnostics,
                bool reportSuppressedDiagnostics,
                out ImmutableArray<Diagnostic> diagnostics)
            where TCompilation : Compilation
        {
            var analyzersArray = analyzers.ToImmutableArray();

            var exceptionDiagnostics = new ConcurrentSet<Diagnostic>();

            if (onAnalyzerException == null)
            {
                if (logAnalyzerExceptionAsDiagnostics)
                {
                    onAnalyzerException = (ex, analyzer, diagnostic) =>
                    {
                        exceptionDiagnostics.Add(diagnostic);
                    };
                }
                else
                {
                    // We want unit tests to throw if any analyzer OR the driver throws, unless the test explicitly provides a delegate.
                    onAnalyzerException = FailFastOnAnalyzerException;
                }
            }

            if (reportSuppressedDiagnostics != c.Options.ReportSuppressedDiagnostics)
            {
                c = (TCompilation)c.WithOptions(c.Options.WithReportSuppressedDiagnostics(reportSuppressedDiagnostics));
            }

            var analyzerManager = new AnalyzerManager(analyzersArray);
            var driver = AnalyzerDriver.CreateAndAttachToCompilation(c, analyzersArray, options, analyzerManager, onAnalyzerException, null, false, out var newCompilation, CancellationToken.None);
            var discarded = newCompilation.GetDiagnostics();
            diagnostics = driver.GetDiagnosticsAsync(newCompilation).Result.AddRange(exceptionDiagnostics);

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

        internal static string GetExpectedErrorLogHeader(string actualOutput, CommonCompiler compiler)
        {
            var expectedToolName = compiler.GetToolName();
            var expectedVersion = compiler.GetAssemblyVersion();
            var expectedSemanticVersion = compiler.GetAssemblyVersion().ToString(fieldCount: 3);
            var expectedFileVersion = compiler.GetAssemblyFileVersion();
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

        public static string Stringize(this Diagnostic e)
        {
            var retVal = string.Empty;
            if (e.Location.IsInSource)
            {
                retVal = e.Location.SourceSpan.ToString() + ": ";
            }
            else if (e.Location.IsInMetadata)
            {
                return "metadata: ";
            }
            else
            {
                return "no location: ";
            }

            retVal = e.Severity.ToString() + " " + e.Id + ": " + e.GetMessage(CultureInfo.CurrentCulture);
            return retVal;
        }

        public static string ToString(this Diagnostic d, IFormatProvider formatProvider)
        {
            IFormattable formattable = d;
            return formattable.ToString(null, formatProvider);
        }
    }
}
