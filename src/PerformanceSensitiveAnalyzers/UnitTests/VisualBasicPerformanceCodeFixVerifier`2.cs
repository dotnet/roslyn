// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests
{
    public static partial class VisualBasicPerformanceCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        private const string PerformanceSensitiveAttributeSource = @"
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
";

        public static DiagnosticResult Diagnostic()
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(descriptor);

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
                    AdditionalReferences = { AdditionalMetadataReferences.Netstandard },
                },
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
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
                        ("PerformanceSensitiveAttribute.vb", PerformanceSensitiveAttributeSource)
                    },
                },
                FixedCode = fixedSource,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
