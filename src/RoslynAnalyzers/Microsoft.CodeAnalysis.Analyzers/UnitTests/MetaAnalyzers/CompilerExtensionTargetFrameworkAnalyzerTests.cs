// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompilerExtensionTargetFrameworkAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CompilerExtensionTargetFrameworkAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class CompilerExtensionTargetFrameworkAnalyzerTests
    {
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

        public enum SupportedTargetFramework
        {
            // Excluding theoretically-supported frameworks that we can't test using Microsoft.CodeAnalysis.Testing.
            //NetStandard1_0,
            //NetStandard1_1,
            //NetStandard1_2,
            NetStandard1_3,
            NetStandard1_4,
            NetStandard1_5,
            NetStandard1_6,
            NetStandard2_0,
        }

        public enum UnsupportedTargetFramework
        {
            NetFramework4_7_2,
            NetStandard2_1,
            Net6_0,
            Net7_0,
            Net8_0,
        }

        [Theory]
        [CombinatorialData]
        public Task CSharpAnalyzerDefinedWithSupportedFramework(SupportedTargetFramework supportedFramework)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(supportedFramework).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(supportedFramework)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.CSharp, CompilerFeature.DiagnosticAnalyzer, SupportedLanguage.CSharp),
                        GetTargetFrameworkAttribute(ImplementationLanguage.CSharp, supportedFramework),
                    },
                },
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task CSharpFeatureDefinedWithSupportedFramework(CompilerFeature feature)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(SupportedTargetFramework.NetStandard2_0).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(SupportedTargetFramework.NetStandard2_0)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.CSharp, feature, SupportedLanguage.CSharp),
                        GetTargetFrameworkAttribute(ImplementationLanguage.CSharp, SupportedTargetFramework.NetStandard2_0),
                    },
                },
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task CSharpFeatureDefinedWithUnsupportedFramework(CompilerFeature feature, UnsupportedTargetFramework framework)
            => new VerifyCS.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(framework).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(framework)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.CSharp, feature, SupportedLanguage.CSharp),
                        GetTargetFrameworkAttribute(ImplementationLanguage.CSharp, framework),
                    },
                },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic().WithLocation(0).WithArguments(GetDisplayName(framework)),
                },
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task VisualBasicAnalyzerDefinedWithSupportedFramework(SupportedTargetFramework supportedFramework)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(supportedFramework).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(supportedFramework)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.VisualBasic, CompilerFeature.DiagnosticAnalyzer, SupportedLanguage.VisualBasic),
                        GetTargetFrameworkAttribute(ImplementationLanguage.VisualBasic, supportedFramework),
                    },
                },
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task VisualBasicFeatureDefinedWithSupportedFramework(CompilerFeature feature)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(SupportedTargetFramework.NetStandard2_0).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(SupportedTargetFramework.NetStandard2_0)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.VisualBasic, feature, SupportedLanguage.VisualBasic),
                        GetTargetFrameworkAttribute(ImplementationLanguage.VisualBasic, SupportedTargetFramework.NetStandard2_0),
                    },
                },
            }.RunAsync();

        [Theory]
        [CombinatorialData]
        public Task VisualBasicFeatureDefinedWithUnsupportedFramework(CompilerFeature feature, UnsupportedTargetFramework framework)
            => new VerifyVB.Test
            {
                ReferenceAssemblies = GetReferenceAssembliesForTargetFramework(framework).AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis.Common", GetCodeAnalysisPackageVersion(framework)))),
                TestState =
                {
                    Sources =
                    {
                        DefineFeature(ImplementationLanguage.VisualBasic, feature, SupportedLanguage.VisualBasic),
                        GetTargetFrameworkAttribute(ImplementationLanguage.VisualBasic, framework),
                    },
                },
                ExpectedDiagnostics =
                {
                    VerifyVB.Diagnostic().WithLocation(0).WithArguments(GetDisplayName(framework)),
                },
            }.RunAsync();

        private static ReferenceAssemblies GetReferenceAssembliesForTargetFramework(SupportedTargetFramework framework)
        {
            return framework switch
            {
                //SupportedTargetFramework.NetStandard1_0 => ReferenceAssemblies.NetStandard.NetStandard10,
                //SupportedTargetFramework.NetStandard1_1 => ReferenceAssemblies.NetStandard.NetStandard11,
                //SupportedTargetFramework.NetStandard1_2 => ReferenceAssemblies.NetStandard.NetStandard12,
                SupportedTargetFramework.NetStandard1_3 => ReferenceAssemblies.NetStandard.NetStandard13,
                SupportedTargetFramework.NetStandard1_4 => ReferenceAssemblies.NetStandard.NetStandard14,
                SupportedTargetFramework.NetStandard1_5 => ReferenceAssemblies.NetStandard.NetStandard15,
                SupportedTargetFramework.NetStandard1_6 => ReferenceAssemblies.NetStandard.NetStandard16,
                SupportedTargetFramework.NetStandard2_0 => ReferenceAssemblies.NetStandard.NetStandard20,
                _ => throw new ArgumentException("Unknown target framework"),
            };
        }

        private static ReferenceAssemblies GetReferenceAssembliesForTargetFramework(UnsupportedTargetFramework framework)
        {
            return framework switch
            {
                UnsupportedTargetFramework.NetFramework4_7_2 => ReferenceAssemblies.NetFramework.Net472.Default,
                UnsupportedTargetFramework.NetStandard2_1 => ReferenceAssemblies.NetStandard.NetStandard21,
                //UnsupportedTargetFramework.NetCoreApp3_1 => ReferenceAssemblies.NetCore.NetCoreApp31,
                UnsupportedTargetFramework.Net6_0 => ReferenceAssemblies.Net.Net60,
                UnsupportedTargetFramework.Net7_0 => ReferenceAssemblies.Net.Net70,
                UnsupportedTargetFramework.Net8_0 => ReferenceAssemblies.Net.Net80,
                _ => throw new ArgumentException("Unknown target framework"),
            };
        }

        private static string GetCodeAnalysisPackageVersion(SupportedTargetFramework framework)
        {
            return framework switch
            {
                //SupportedTargetFramework.NetStandard1_0 => throw new NotImplementedException(),
                //SupportedTargetFramework.NetStandard1_1 => throw new NotImplementedException(),
                //SupportedTargetFramework.NetStandard1_2 => throw new NotImplementedException(),
                SupportedTargetFramework.NetStandard1_3 => "2.8.2",
                SupportedTargetFramework.NetStandard1_4 => "2.8.2",
                SupportedTargetFramework.NetStandard1_5 => "2.8.2",
                SupportedTargetFramework.NetStandard1_6 => "2.8.2",
                SupportedTargetFramework.NetStandard2_0 => "4.6.0",
                _ => throw new ArgumentException("Unknown target framework"),
            };
        }

        private static string GetTargetFrameworkAttribute(ImplementationLanguage language, SupportedTargetFramework framework)
        {
            var (name, displayName) = framework switch
            {
                SupportedTargetFramework.NetStandard1_3 => (".NETStandard,Version=v1.3", ".NET Standard 1.3"),
                SupportedTargetFramework.NetStandard1_4 => (".NETStandard,Version=v1.4", ".NET Standard 1.4"),
                SupportedTargetFramework.NetStandard1_5 => (".NETStandard,Version=v1.5", ".NET Standard 1.5"),
                SupportedTargetFramework.NetStandard1_6 => (".NETStandard,Version=v1.6", ".NET Standard 1.6"),
                SupportedTargetFramework.NetStandard2_0 => (".NETStandard,Version=v2.0", ".NET Standard 2.0"),
                _ => throw new NotImplementedException(),
            };

            return language switch
            {
                ImplementationLanguage.CSharp => $"[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(\"{name}\", FrameworkDisplayName = \"{displayName}\")]",
                ImplementationLanguage.VisualBasic => $"<Assembly: Global.System.Runtime.Versioning.TargetFrameworkAttribute(\"{name}\", FrameworkDisplayName:=\"{displayName}\")>",
                _ => throw new NotImplementedException(),
            };
        }

        private static string GetTargetFrameworkAttribute(ImplementationLanguage language, UnsupportedTargetFramework framework)
        {
            var (name, displayName) = framework switch
            {
                UnsupportedTargetFramework.NetFramework4_7_2 => (".NETFramework,Version=v4.7.2", ".NET Framework 4.7.2"),
                UnsupportedTargetFramework.NetStandard2_1 => (".NETStandard,Version=v2.1", ".NET Standard 2.1"),
                UnsupportedTargetFramework.Net6_0 => (".NETCoreApp,Version=v6.0", ".NET 6.0"),
                UnsupportedTargetFramework.Net7_0 => (".NETCoreApp,Version=v7.0", ".NET 7.0"),
                UnsupportedTargetFramework.Net8_0 => (".NETCoreApp,Version=v8.0", ".NET 8.0"),
                _ => throw new NotImplementedException(),
            };

            return language switch
            {
                ImplementationLanguage.CSharp => $"[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(\"{name}\", FrameworkDisplayName = \"{displayName}\")]",
                ImplementationLanguage.VisualBasic => $"<Assembly: Global.System.Runtime.Versioning.TargetFrameworkAttribute(\"{name}\", FrameworkDisplayName:=\"{displayName}\")>",
                _ => throw new NotImplementedException(),
            };
        }

        private static string GetDisplayName(UnsupportedTargetFramework framework)
        {
            return framework switch
            {
                UnsupportedTargetFramework.NetFramework4_7_2 => ".NET Framework 4.7.2",
                UnsupportedTargetFramework.NetStandard2_1 => ".NET Standard 2.1",
                UnsupportedTargetFramework.Net6_0 => ".NET 6.0",
                UnsupportedTargetFramework.Net7_0 => ".NET 7.0",
                UnsupportedTargetFramework.Net8_0 => ".NET 8.0",
                _ => throw new NotImplementedException(),
            };
        }

        private static string GetCodeAnalysisPackageVersion(UnsupportedTargetFramework framework)
        {
            return framework switch
            {
                _ => "4.0.0",
            };
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
