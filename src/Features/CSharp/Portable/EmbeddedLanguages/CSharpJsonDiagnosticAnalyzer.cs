// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpJsonDiagnosticAnalyzer : AbstractJsonDiagnosticAnalyzer
    {
        public CSharpJsonDiagnosticAnalyzer()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}
