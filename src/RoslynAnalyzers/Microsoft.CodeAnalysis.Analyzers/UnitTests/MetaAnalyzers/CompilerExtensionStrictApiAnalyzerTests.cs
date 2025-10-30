// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompilerExtensionStrictApiAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompilerExtensionStrictApiAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class CompilerExtensionStrictApiAnalyzerTests
    {
        private const string CompilerReferenceVersion = "4.6.0";

        public enum ImplementationLanguage
        {
            CSharp,
            VisualBasic,
        }

        public enum SupportedLanguage
        {
            CSharp,
            VisualBasic,
            CSharpAndVisualBasic,
        }

        public enum CompilerFeature
        {
            DiagnosticAnalyzer,
            DiagnosticSuppressor,
            ISourceGenerator,
            IIncrementalGenerator,
        }

        [Theory]
        [CombinatorialData]
        public Task CSharpFeatureDefinedWithCommonReference(CompilerFeature feature, SupportedLanguage supportedLanguage)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.CSharp, feature, supportedLanguage),
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public async Task CSharpFeatureDefinedWithMatchingLanguageReference(CompilerFeature feature, [CombinatorialValues(SupportedLanguage.CSharp, SupportedLanguage.VisualBasic)] SupportedLanguage supportedLanguage)
        {
            var matchingPackage = supportedLanguage switch
            {
                SupportedLanguage.CSharp => "Microsoft.CodeAnalysis.CSharp",
                SupportedLanguage.VisualBasic => "Microsoft.CodeAnalysis.VisualBasic",
                _ => throw new NotImplementedException(),
            };

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity(matchingPackage, CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.CSharp, feature, supportedLanguage),
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CSharpFeatureDefinedWithWorkspaceReference(CompilerFeature feature, SupportedLanguage supportedLanguage, bool relaxedValidation)
        {
            var test = new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.Common", CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.CSharp, feature, supportedLanguage),
            };

            if (relaxedValidation)
            {
                test.TestState.AnalyzerConfigFiles.Add(
                    ("/.globalconfig",
                    """
                    is_global = true

                    roslyn_correctness.assembly_reference_validation = relaxed
                    """));
            }
            else
            {
                test.TestState.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(CompilerExtensionStrictApiAnalyzer.DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule).WithLocation(0));
            }

            await test.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task CSharpFeatureDefinedWithMismatchedLanguageReference(CompilerFeature feature, SupportedLanguage supportedLanguage)
        {
            var (mismatchedPackage, descriptor) = supportedLanguage switch
            {
                SupportedLanguage.CSharp => ("Microsoft.CodeAnalysis.VisualBasic", CompilerExtensionStrictApiAnalyzer.DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule),
                SupportedLanguage.VisualBasic => ("Microsoft.CodeAnalysis.CSharp", CompilerExtensionStrictApiAnalyzer.DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule),
                SupportedLanguage.CSharpAndVisualBasic => ("Microsoft.CodeAnalysis.VisualBasic", CompilerExtensionStrictApiAnalyzer.DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule),
                _ => throw new NotImplementedException(),
            };

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity(mismatchedPackage, CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.CSharp, feature, supportedLanguage),
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(descriptor).WithLocation(0),
                },
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public Task VisualBasicFeatureDefinedWithCommonReference(CompilerFeature feature, SupportedLanguage supportedLanguage)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.VisualBasic, feature, supportedLanguage),
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public async Task VisualBasicFeatureDefinedWithMatchingLanguageReference(CompilerFeature feature, [CombinatorialValues(SupportedLanguage.CSharp, SupportedLanguage.VisualBasic)] SupportedLanguage supportedLanguage)
        {
            var matchingPackage = supportedLanguage switch
            {
                SupportedLanguage.CSharp => "Microsoft.CodeAnalysis.CSharp",
                SupportedLanguage.VisualBasic => "Microsoft.CodeAnalysis.VisualBasic",
                _ => throw new NotImplementedException(),
            };

            await new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity(matchingPackage, CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.VisualBasic, feature, supportedLanguage),
            }.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task VisualBasicFeatureDefinedWithWorkspaceReference(CompilerFeature feature, SupportedLanguage supportedLanguage, bool relaxedValidation)
        {
            var test = new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.Common", CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.VisualBasic, feature, supportedLanguage),
            };

            if (relaxedValidation)
            {
                test.TestState.AnalyzerConfigFiles.Add(
                    ("/.globalconfig",
                    """
                    is_global = true

                    roslyn_correctness.assembly_reference_validation = relaxed
                    """));
            }
            else
            {
                test.TestState.ExpectedDiagnostics.Add(VerifyVB.Diagnostic(CompilerExtensionStrictApiAnalyzer.DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule).WithLocation(0));
            }

            await test.RunAsync();
        }

        [Theory]
        [CombinatorialData]
        public async Task VisualBasicFeatureDefinedWithMismatchedLanguageReference(CompilerFeature feature, SupportedLanguage supportedLanguage)
        {
            var (mismatchedPackage, descriptor) = supportedLanguage switch
            {
                SupportedLanguage.CSharp => ("Microsoft.CodeAnalysis.VisualBasic", CompilerExtensionStrictApiAnalyzer.DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule),
                SupportedLanguage.VisualBasic => ("Microsoft.CodeAnalysis.CSharp", CompilerExtensionStrictApiAnalyzer.DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule),
                SupportedLanguage.CSharpAndVisualBasic => ("Microsoft.CodeAnalysis.CSharp", CompilerExtensionStrictApiAnalyzer.DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule),
                _ => throw new NotImplementedException(),
            };

            await new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20.AddPackages(ImmutableArray.Create(
                    new PackageIdentity(mismatchedPackage, CompilerReferenceVersion))),
                TestCode = DefineFeature(ImplementationLanguage.VisualBasic, feature, supportedLanguage),
                ExpectedDiagnostics =
                {
                    VerifyVB.Diagnostic(descriptor).WithLocation(0),
                },
            }.RunAsync();
        }

        private static string DefineFeature(ImplementationLanguage languageName, CompilerFeature feature, SupportedLanguage supportedLanguage)
        {
            var languageApplication = (languageName, supportedLanguage) switch
            {
                (_, SupportedLanguage.CSharp) => "LanguageNames.CSharp",
                (_, SupportedLanguage.VisualBasic) => "LanguageNames.VisualBasic",
                (ImplementationLanguage.CSharp, SupportedLanguage.CSharpAndVisualBasic) => "LanguageNames.CSharp, LanguageNames.VisualBasic",
                (ImplementationLanguage.VisualBasic, SupportedLanguage.CSharpAndVisualBasic) => "LanguageNames.VisualBasic, LanguageNames.CSharp",
                _ => throw new NotImplementedException(),
            };

            return (languageName, feature) switch
            {
                (ImplementationLanguage.CSharp, CompilerFeature.DiagnosticAnalyzer) =>
                    $$"""
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    [{|#0:DiagnosticAnalyzer({{languageApplication}})|}]
                    public class MyAnalyzer : DiagnosticAnalyzer
                    {
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
                        public override void Initialize(AnalysisContext context) { }
                    }
                    """,
                (ImplementationLanguage.CSharp, CompilerFeature.DiagnosticSuppressor) =>
                    $$"""
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    [{|#0:DiagnosticAnalyzer({{languageApplication}})|}]
                    public class MySuppressor : DiagnosticSuppressor
                    {
                        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }
                        public override void ReportSuppressions(SuppressionAnalysisContext context) { }
                    }
                    """,
                (ImplementationLanguage.CSharp, CompilerFeature.ISourceGenerator) =>
                    $$"""
                    using Microsoft.CodeAnalysis;

                    [{|#0:Generator({{languageApplication}})|}]
                    public class MyGenerator : ISourceGenerator
                    {
                        public void Initialize(GeneratorInitializationContext context) { }
                        public void Execute(GeneratorExecutionContext context) { }
                    }
                    """,
                (ImplementationLanguage.CSharp, CompilerFeature.IIncrementalGenerator) =>
                    $$"""
                    using Microsoft.CodeAnalysis;

                    [{|#0:Generator({{languageApplication}})|}]
                    public class MyGenerator : IIncrementalGenerator
                    {
                        public void Initialize(IncrementalGeneratorInitializationContext context) { }
                    }
                    """,
                (ImplementationLanguage.VisualBasic, CompilerFeature.DiagnosticAnalyzer) =>
                    $$"""
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    <{|#0:DiagnosticAnalyzer({{languageApplication}})|}>
                    Public Class MyAnalyzer
                        Inherits DiagnosticAnalyzer

                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """,
                (ImplementationLanguage.VisualBasic, CompilerFeature.DiagnosticSuppressor) =>
                    $$"""
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    <{|#0:DiagnosticAnalyzer({{languageApplication}})|}>
                    Public Class MySuppressor
                        Inherits DiagnosticSuppressor

                        Public Overrides ReadOnly Property SupportedSuppressions As ImmutableArray(Of SuppressionDescriptor)
                        Public Overrides Sub ReportSuppressions(context As SuppressionAnalysisContext)
                        End Sub
                    End Class
                    """,
                (ImplementationLanguage.VisualBasic, CompilerFeature.ISourceGenerator) =>
                    $$"""
                    Imports Microsoft.CodeAnalysis

                    <{|#0:Generator({{languageApplication}})|}>
                    Public Class MyGenerator
                        Implements ISourceGenerator

                        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
                        End Sub

                        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
                        End Sub
                    End Class
                    """,
                (ImplementationLanguage.VisualBasic, CompilerFeature.IIncrementalGenerator) =>
                    $$"""
                    Imports Microsoft.CodeAnalysis

                    <{|#0:Generator({{languageApplication}})|}>
                    Public Class MyGenerator
                        Implements IIncrementalGenerator

                        Public Sub Initialize(context As IncrementalGeneratorInitializationContext) Implements IIncrementalGenerator.Initialize
                        End Sub
                    End Class
                    """,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
