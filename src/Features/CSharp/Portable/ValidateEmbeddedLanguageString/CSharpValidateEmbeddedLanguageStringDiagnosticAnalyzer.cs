// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ValidateJsonString;

namespace Microsoft.CodeAnalysis.CSharp.ValidateEmbeddedLanguageString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpValidateEmbeddedLanguageStringDiagnosticAnalyzer : AbstractValidateEmbeddedLanguageStringDiagnosticAnalyzer
    {
        public CSharpValidateEmbeddedLanguageStringDiagnosticAnalyzer() 
            : base(CSharpEmbeddedLanguageProvider.Instance)
        {
        }
    }
}
