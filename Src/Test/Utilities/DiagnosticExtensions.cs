// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

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

        public static void Verify(this IEnumerable<Diagnostic> actual, bool suppressSerializabilityVerification, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: false, suppressSerializabilityVerification: suppressSerializabilityVerification);
        }

        public static void Verify(this ImmutableArray<Diagnostic> actual, bool suppressSerializabilityVerification, params DiagnosticDescription[] expected)
        {
            Verify((IEnumerable<Diagnostic>)actual, suppressSerializabilityVerification, expected);
        }

        private static void Verify(IEnumerable<Diagnostic> actual, DiagnosticDescription[] expected, bool errorCodeOnly, bool suppressSerializabilityVerification = false)
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

            // Lastly, verify that all the errors generated are serializable.
            if (!suppressSerializabilityVerification)
            {
                VerifySerializability(actual);
            }
        }

        public static void VerifySerializability(this IEnumerable<Diagnostic> diagnostics)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                foreach (var diagnostic in diagnostics)
                {
                    formatter.Serialize(stream, diagnostic);
                    stream.Seek(0, SeekOrigin.Begin);
                    var deserialized = (Diagnostic)formatter.Deserialize(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    string diagnosticStr = diagnostic.ToString();
                    string deserializedStr = deserialized.ToString();
                    var invalidCharactersReplaced = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(diagnosticStr));

                    Assert.Equal(invalidCharactersReplaced, deserializedStr);
                    stream.SetLength(0);
                }
            }
        }

        public static TCompilation VerifyDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var diagnostics = c.GetDiagnostics();
            diagnostics.Verify(expected);
            return c;
        }

        public static CSharp.CSharpCompilation VerifyAnalyzerDiagnostics3(
                this CSharp.CSharpCompilation c, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
        {
            return VerifyAnalyzerDiagnostics3<CSharp.CSharpCompilation, CSharp.SyntaxKind>(c, n => n.CSharpKind(), analyzers, expected);
        }

        public static TCompilation VerifyAnalyzerDiagnostics3<TCompilation, TSyntaxKind>(
                this TCompilation c, Func<SyntaxNode, TSyntaxKind> getKind, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
            where TSyntaxKind : struct
        {
            var driver = new AnalyzerDriver3<TSyntaxKind>(analyzers, getKind, default(CancellationToken));
            c = (TCompilation)c.WithEventQueue(driver.CompilationEventQueue);
            var discarded = c.GetDiagnostics();
            driver.DiagnosticsAsync().Result.Verify(expected);
            return c; // note this is a new compilation
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(this TCompilation c, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            AnalyzerDriver.GetDiagnostics(c, analyzers, default(CancellationToken)).Verify(expected);
            return c;
        }

        public static TCompilation VerifyDiagnostics<TCompilation>(this TCompilation c, bool suppressSerializabilityVerification, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var diagnostics = c.GetDiagnostics();
            diagnostics.Verify(suppressSerializabilityVerification, expected);
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
