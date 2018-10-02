﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpEmbeddedLanguageDiagnosticAnalyzer : AbstractEmbeddedLanguageDiagnosticAnalyzer
    {
        public CSharpEmbeddedLanguageDiagnosticAnalyzer()
            : base(CSharpEmbeddedLanguageFeaturesProvider.Instance)
        {
        }
    }
}
