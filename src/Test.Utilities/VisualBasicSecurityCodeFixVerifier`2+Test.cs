// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Test.Utilities
{
    public static partial class VisualBasicSecurityCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            public Test()
            {
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = base.CreateParseOptions();
                return parseOptions.WithFeatures(parseOptions.Features.Concat(
                    new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));
            }
        }
    }
}
