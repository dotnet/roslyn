// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class SuppressMessageAttributeCompilerTests : SuppressMessageAttributeTests
    {
        protected override Task VerifyAsync(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] diagnostics, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, string rootNamespace = null)
        {
            Assert.True(analyzers != null && analyzers.Length > 0, "Must specify at least one diagnostic analyzer to test suppression");
            var compilation = CreateCompilation(source, language, analyzers, rootNamespace);
            compilation.VerifyAnalyzerDiagnostics(analyzers, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics, expected: diagnostics);
            return Task.FromResult(false);
        }

        protected override bool ConsiderArgumentsForComparingDiagnostics => true;

        private static Compilation CreateCompilation(string source, string language, DiagnosticAnalyzer[] analyzers, string rootNamespace)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";
            string projectName = "TestProject";

            var syntaxTree = language == LanguageNames.CSharp ?
                CSharpSyntaxTree.ParseText(source, path: fileName) :
                VisualBasicSyntaxTree.ParseText(source, path: fileName);

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef },
                    options: new VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        rootNamespace: rootNamespace));
            }
        }

    }
}
