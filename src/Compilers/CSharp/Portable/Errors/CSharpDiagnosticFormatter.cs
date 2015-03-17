// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class CSharpDiagnosticFormatter : DiagnosticFormatter
    {
        private static readonly string CompilerAnalyzer = typeof(CSharpCompilerDiagnosticAnalyzer).ToString();

        internal CSharpDiagnosticFormatter(bool displayAnalyzer) :
            base(displayAnalyzer)
        {
        }

        protected override string GetAnalyzerIdentity(DiagnosticAnalyzer analyzer)
        {
            if (analyzer == null)
            {
                return CompilerAnalyzer;
            }

            return base.GetAnalyzerIdentity(analyzer);
        }

        public new static readonly CSharpDiagnosticFormatter Instance = new CSharpDiagnosticFormatter(displayAnalyzer: false);
    }
}
