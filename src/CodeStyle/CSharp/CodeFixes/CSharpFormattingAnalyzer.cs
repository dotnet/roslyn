// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpFormattingAnalyzer : AbstractFormattingAnalyzer
    {
        protected override Type GetAnalyzerImplType()
        {
            // Explained at the call site in the base class
            return typeof(CSharpFormattingAnalyzerImpl);
        }
    }
}
