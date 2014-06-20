// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis
{
    public static class DiagnosticExtensions
    {
        private const int EN_US = 1033;
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

        public static void Verify(this ImmutableArray<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            if (CultureInfo.CurrentCulture.LCID == EN_US || CultureInfo.CurrentUICulture.LCID == EN_US || CultureInfo.CurrentCulture == CultureInfo.InvariantCulture || CultureInfo.CurrentUICulture == CultureInfo.InvariantCulture)
            {
                Verify((IEnumerable<Diagnostic>)actual, expected);
            }
            else
            {
                actual.VerifyErrorCodes(expected);
            }
        }

        private static void Verify(IEnumerable<Diagnostic> actual, DiagnosticDescription[] expected, bool errorCodeOnly)
        {
            if (expected == null)
            {
                throw new ArgumentException("Must specify expected errors.", "expected");
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
            return c;
        }

        public static CSharpCompilation VerifyAnalyzerDiagnostics3(this CSharpCompilation c, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
        {
            return VerifyAnalyzerDiagnostics3(c, n => n.CSharpKind(), null, analyzers, expected);
        }

        public static TCompilation VerifyAnalyzerDiagnostics3<TCompilation, TSyntaxKind>(
                this TCompilation c, Func<SyntaxNode, TSyntaxKind> getKind, AnalyzerOptions options, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
            where TSyntaxKind : struct
        {
            var driver = new AnalyzerDriver3<TSyntaxKind>(analyzers, getKind, options, default(CancellationToken));
            c = (TCompilation)c.WithEventQueue(driver.CompilationEventQueue);
            var discarded = c.GetDiagnostics();
            driver.DiagnosticsAsync().Result.Verify(expected);
            return c; // note this is a new compilation
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(this TCompilation c, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            AnalyzerDriver.GetDiagnostics(c, analyzers, null, default(CancellationToken)).Verify(expected);
            return c;
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.Emit(new MemoryStream(), pdbStream: new MemoryStream()).Diagnostics.Verify(expected);
            return c;
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, IEnumerable<ResourceDescription> manifestResources, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.Emit(new MemoryStream(), pdbStream: new MemoryStream(), manifestResources: manifestResources).Diagnostics.Verify(expected);
            return c;
        }

        public static string Concat(this string[] str)
        {
            return str.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s), sb => sb.ToString());
        }
    }
}
