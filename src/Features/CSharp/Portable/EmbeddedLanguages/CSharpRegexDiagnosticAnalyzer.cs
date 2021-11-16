﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRegexDiagnosticAnalyzer : AbstractRegexDiagnosticAnalyzer
    {
        public CSharpRegexDiagnosticAnalyzer()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}
