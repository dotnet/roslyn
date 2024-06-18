// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests
{
    public static partial class CSharpPerformanceCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        internal const string PerformanceSensitiveAttributeSource = @"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    internal sealed class PerformanceSensitiveAttribute : Attribute
    {
        public PerformanceSensitiveAttribute(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; }
        public string Constraint { get; set; }
        public bool AllowCaptures { get; set; }
        public bool AllowGenericEnumeration { get; set; }
        public bool AllowLocks { get; set; }
        public bool OftenCompletesSynchronously { get; set; }
        public bool IsParallelEntry { get; set; }
    }
}";

        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestState =
                {
                    Sources =
                    {
                        source,
                        ("PerformanceSensitiveAttribute.cs", PerformanceSensitiveAttributeSource)
                    },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestState =
                {
                    Sources =
                    {
                        source,
                        ("PerformanceSensitiveAttribute.cs", PerformanceSensitiveAttributeSource)
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        fixedSource,
                        ("PerformanceSensitiveAttribute.cs", PerformanceSensitiveAttributeSource)
                    },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
