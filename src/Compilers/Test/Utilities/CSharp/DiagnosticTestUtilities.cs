// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// OBSOLETE: Use DiagnosticDescription instead.
    /// </summary>
    public struct ErrorDescription
    {
        public bool IsWarning;
        public int Code, Line, Column;
        public string[] Parameters;

        public override string ToString()
        {
            return string.Format("Line={0}, Column={1}, Code={2}, IsWarning={3}", this.Line, this.Column, this.Code, this.IsWarning);
        }
    }

    public abstract class DiagnosticsUtils
    {
        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        public static CSharpCompilation VerifyErrorsAndGetCompilationWithMscorlib(string text, params ErrorDescription[] expectedErrorDesp)
        {
            return VerifyErrorsAndGetCompilationWithMscorlib(new string[] { text }, expectedErrorDesp);
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        protected internal static CSharpCompilation VerifyErrorsAndGetCompilationWithMscorlib(string[] srcs, params ErrorDescription[] expectedErrorDesp)
        {
            var comp = CSharpTestBase.CreateCompilation(srcs, parseOptions: TestOptions.RegularPreview);
            var actualErrors = comp.GetDiagnostics();
            VerifyErrorCodes(actualErrors, expectedErrorDesp);
            return comp;
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        protected internal static CSharpCompilation VerifyErrorsAndGetCompilationWithMscorlib(string text, IEnumerable<MetadataReference> refs, params ErrorDescription[] expectedErrorDesp)
        {
            return VerifyErrorsAndGetCompilationWithMscorlib(new List<string> { text }, refs, expectedErrorDesp);
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        protected internal static CSharpCompilation VerifyErrorsAndGetCompilationWithMscorlib(List<string> srcs, IEnumerable<MetadataReference> refs, params ErrorDescription[] expectedErrorDesp)
        {
            var synTrees = (from text in srcs
                            select SyntaxFactory.ParseSyntaxTree(SourceText.From(text, encoding: null, SourceHashAlgorithms.Default))).ToArray();

            return VerifyErrorsAndGetCompilationWithMscorlib(synTrees, refs, expectedErrorDesp);
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        protected internal static CSharpCompilation VerifyErrorsAndGetCompilationWithMscorlib(SyntaxTree[] trees, IEnumerable<MetadataReference> refs, params ErrorDescription[] expectedErrorDesp)
        {
            return VerifyErrorsAndGetCompilation(trees, refs.Concat(CSharpTestBase.MscorlibRef), expectedErrorDesp);
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        protected internal static CSharpCompilation VerifyErrorsAndGetCompilation(IEnumerable<SyntaxTree> synTrees, IEnumerable<MetadataReference> refs = null, params ErrorDescription[] expectedErrorDesp)
        {
            var comp = CSharpCompilation.Create(assemblyName: "DiagnosticsTest", options: TestOptions.ReleaseDll, syntaxTrees: synTrees, references: refs);
            var actualErrors = comp.GetDiagnostics();

            VerifyErrorCodes(actualErrors, expectedErrorDesp);

            return comp;
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        public static void VerifyErrorCodes(IEnumerable<Diagnostic> actualErrors, params ErrorDescription[] expectedErrorDesp)
        {
            if (expectedErrorDesp == null)
                return;

            int expectedLength = expectedErrorDesp.Length;
            int actualLength = actualErrors.Count();

            Assert.True(
                expectedLength == actualLength,
                String.Format(
                    "ErrCount {0} != {1}{2}Actual errors are:{2}{3}",
                    expectedLength,
                    actualLength,
                    Environment.NewLine,
                    actualLength == 0 ? "<none>" : string.Join(Environment.NewLine, actualErrors)));

            var actualSortedDesp = (from ae in
                                        (from e in actualErrors
                                         let lineSpan = e.Location.GetMappedLineSpan()
                                         select new ErrorDescription
                                         {
                                             Code = e.Code,
                                             Line = lineSpan.IsValid ? lineSpan.StartLinePosition.Line + 1 : 0,
                                             Column = lineSpan.IsValid ? lineSpan.StartLinePosition.Character + 1 : 0,
                                             IsWarning = e.Severity == DiagnosticSeverity.Warning,
                                             Parameters = (e.Arguments != null && e.Arguments.Count > 0 && e.Arguments[0] != null) ?
                                                e.Arguments.Select(x => x != null ? x.ToString() : null).ToArray() : Array.Empty<string>()
                                         })
                                    orderby ae.Code, ae.Line, ae.Column
                                    select ae).ToList();

            var expectedSortedDesp = (from ee in expectedErrorDesp
                                      orderby ee.Code, ee.Line, ee.Column
                                      select ee).ToList();

            int idx = 0;
            // actual >= expected
            foreach (var experr in expectedSortedDesp)
            {
                while (idx < actualSortedDesp.Count && actualSortedDesp[idx].Code < experr.Code)
                {
                    idx++;
                }

                if (idx >= actualSortedDesp.Count)
                {
                    idx = actualSortedDesp.Count - 1;
                }

                var acterr = actualSortedDesp[idx];

                Assert.Equal(experr.Code, acterr.Code);
                if (experr.Line > 0 && experr.Column > 0)
                {
                    Assert.True(experr.Line == acterr.Line, String.Format("Line {0}!={1}", experr.Line, acterr.Line));
                    Assert.True(experr.Column == acterr.Column, String.Format("Col {0}!={1}", experr.Column, acterr.Column));
                }

                Assert.True(experr.IsWarning == acterr.IsWarning, String.Format("IsWarning {0}!={1}", experr.IsWarning, acterr.IsWarning));

                //if the expected contains parameters, validate those too.
                if (experr.Parameters != null)
                {
                    Assert.True(experr.Parameters.SequenceEqual(acterr.Parameters), String.Format("Param: {0}!={1}", experr.Parameters.Count(), acterr.Parameters.Count()));
                }

                idx++;
            }
        }

        /// <summary>
        /// OBSOLETE: Use VerifyDiagnostics from Roslyn.Compilers.CSharp.Test.Utilities instead.
        /// </summary>
        public static void VerifyErrorCodesNoLineColumn(IEnumerable<Diagnostic> actualErrors, params ErrorDescription[] expectedErrorDesp)
        {
            if (expectedErrorDesp == null)
                return;

            // TODO: for now, we only expected actual errors including all expected errors
            // Assert.Equal(expectedErrorDesp.Length, actualErrors.Count);

            // allow actual errors contain more same errors, no line & column check
            Assert.InRange(expectedErrorDesp.Length, 0, actualErrors.Count());

            var expectedCodes = (from e in expectedErrorDesp
                                 orderby e.Code
                                 group e by e.Code).ToList();

            var actualCodes = (from e in actualErrors
                               orderby e.Code
                               group e by e.Code).ToList();

            foreach (var expectedGroup in expectedCodes)
            {
                var actualGroup = actualCodes.SingleOrDefault(x => x.Key == expectedGroup.Key);
                var actualGroupCount = actualGroup != null ? actualGroup.Count() : 0;
                // Same error code *should* be same error type: error/warning
                // In other words, 0 <= # of expected occurrences <= # of actual occurrences
                Assert.InRange(expectedGroup.Count(), 0, actualGroupCount);
            }
        }
    }
}
