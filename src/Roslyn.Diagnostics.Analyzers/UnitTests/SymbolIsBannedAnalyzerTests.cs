// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class SymbolIsBannedAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        private TestAdditionalDocument GetBannedApiAdditionalTextFile(string bannedApiText = "", string bannedApiFilePath = SymbolIsBannedAnalyzer.BannedSymbolsFileName)
        {
            return new TestAdditionalDocument(
                filePath: bannedApiFilePath,
                fileName: SymbolIsBannedAnalyzer.BannedSymbolsFileName,
                text: bannedApiText);
        }


        private void VerifyBasic(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(bannedApiText, SymbolIsBannedAnalyzer.BannedSymbolsFileName);
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(SymbolIsBannedAnalyzer.BannedSymbolsFileName, bannedApiText);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string bannedApiText, string bannedApiFilePath, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(bannedApiFilePath, bannedApiText);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }


        #region Diagnostic tests

        [Fact]
        public void NoDiagnosticReportedForEmptyBannedText()
        {
            var source = @"";

            var bannedText = @"";

            VerifyCSharp(source, bannedText);
        }

        [Fact]
        public void DiagnosticReportedForDuplicateBannedApiLines()
        {
            var source = @"";
            var bannedText = @"
System.Console
System.Console";

            VerifyCSharp(source, bannedText,
                GetResultAt(
                    SymbolIsBannedAnalyzer.BannedSymbolsFileName,
                    SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.Id,
                    string.Format(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.MessageFormat.ToString(), "System.Console"),
                    "(3,1)", "(2,1)"));
        }


        #endregion

    }
}
