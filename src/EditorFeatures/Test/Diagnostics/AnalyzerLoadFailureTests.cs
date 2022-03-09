// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class AnalyzerLoadFailureTests
    {
        [Theory]
        [CombinatorialData]
        public void CanCreateDiagnosticForAnalyzerLoadFailure(
            AnalyzerLoadFailureEventArgs.FailureErrorCode errorCode,
            [CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic, null)] string? languageName)
        {
            // One potential value is None, which isn't actually a valid enum value to test.
            if (errorCode == AnalyzerLoadFailureEventArgs.FailureErrorCode.None)
            {
                return;
            }

            var expectsTypeName = errorCode is AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer or
                                               AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesFramework;

            const string analyzerTypeName = "AnalyzerTypeName";
            var eventArgs = new AnalyzerLoadFailureEventArgs(
                errorCode,
                message: errorCode.ToString(),
                typeNameOpt: expectsTypeName ? analyzerTypeName : null);

            // Ensure CreateAnalyzerLoadFailureDiagnostic doesn't fail when called. We don't assert much about the resulting
            // diagnostic -- this is primarly to ensure we don't forget to update it if a new error code is added.
            var diagnostic = DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(eventArgs, "Analyzer.dll", null, languageName);
            Assert.Equal(languageName, diagnostic.Language);

            if (expectsTypeName)
            {
                Assert.Contains(analyzerTypeName, diagnostic.Message);
            }
            else
            {
                Assert.DoesNotContain(analyzerTypeName, diagnostic.Message);
            }
        }
    }
}
