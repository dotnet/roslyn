// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests
{
    public static partial class VisualBasicPerformanceCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        private const string PerformanceSensitiveAttributeSource = """
            Imports System
            Imports System.Collections.Generic
            Imports System.Diagnostics
            Imports System.Threading.Tasks

            Namespace Global.Roslyn.Utilities
                <AttributeUsage(AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field, AllowMultiple:=True, Inherited:=False)>
                Friend NotInheritable Class PerformanceSensitiveAttribute
                    Inherits Attribute

                    Public Sub New(uri As String)
                        Me.Uri = uri
                    End Sub

                    Public ReadOnly Property Uri As String
                    Public Property Constraint As String
                    Public Property AllowCaptures As Boolean
                    Public Property AllowGenericEnumeration As Boolean
                    Public Property AllowLocks As Boolean
                    Public Property OftenCompletesSynchronously As Boolean
                    Public Property IsParallelEntry As Boolean
                End Class
            End Namespace
            """;

        public static DiagnosticResult Diagnostic()
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestState =
                {
                    Sources =
                    {
                        source,
                        ("PerformanceSensitiveAttribute.vb", PerformanceSensitiveAttributeSource)
                    },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFixAsync(source, [expected], fixedSource);

        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestState =
                {
                    Sources =
                    {
                        source,
                        ("PerformanceSensitiveAttribute.vb", PerformanceSensitiveAttributeSource)
                    },
                },
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
