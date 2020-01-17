// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Test.Utilities
{
    public static partial class CSharpSecurityCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            public Test()
            {
                // These analyzers run on generated code by default.
                TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;
            }

            protected override Project ApplyCompilationOptions(Project project)
            {
                var newProject = base.ApplyCompilationOptions(project);

                var parseOptions = newProject.ParseOptions.WithFeatures(
                    newProject.ParseOptions.Features.Concat(
                        new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));

                return newProject.WithParseOptions(parseOptions);
            }
        }
    }
}
