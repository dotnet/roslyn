// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpAvoidOptSuffixForNullableEnableCode : AvoidOptSuffixForNullableEnableCode
    {
        protected override bool IsNullableEnabledContext(CompilationOptions compilationOptions, IEnumerable<ParseOptions?> parseOptions)
        {
            var hasAnyCSharp8Part = parseOptions.OfType<CSharpParseOptions>().Any(option => option.LanguageVersion >= LanguageVersion.CSharp8);

            return hasAnyCSharp8Part && IsNullableEnabledContext(compilationOptions);
        }

        private static bool IsNullableEnabledContext(CompilationOptions compilationOptions)
            => compilationOptions is CSharpCompilationOptions csharpCompilationOptions &&
                csharpCompilationOptions.NullableContextOptions == NullableContextOptions.Enable;
    }
}
