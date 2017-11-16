// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticDescriptorCreationAnalyzerTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""MyDiagnosticId"", ""MyDiagnosticTitle"", ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";
            DiagnosticResult[] expected = new[] { GetCSharpRS1007ExpectedDiagnostic(11, 9), GetCSharpRS1015ExpectedDiagnostic(11, 9)};
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(""MyDiagnosticId"", ""MyDiagnosticTitle"", ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:= true)

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";
            DiagnosticResult[] expected = new[] { GetBasicRS1007ExpectedDiagnostic(10, 66), GetBasicRS1015ExpectedDiagnostic(10, 70) };
            VerifyBasic(source, expected);
        }

        [Fact]
        public void CSharp_VerifyDiagnostic_NamedArgumentCases()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""MyDiagnosticId"", messageFormat: ""MyDiagnosticMessage"", title: ""MyDiagnosticTitle"", helpLinkUri: null, category: ""MyDiagnosticCategory"", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""MyDiagnosticId"", messageFormat: ""MyDiagnosticMessage"", title: ""MyDiagnosticTitle"", category: ""MyDiagnosticCategory"", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";
            DiagnosticResult[] expected = new[] {
                GetCSharpRS1007ExpectedDiagnostic(11, 9), 
                GetCSharpRS1015ExpectedDiagnostic(11, 118),
                GetCSharpRS1007ExpectedDiagnostic(14, 9),
                GetCSharpRS1015ExpectedDiagnostic(14, 9)
            };

            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnostic_NamedArgumentCases()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(""MyDiagnosticId"", title:=""MyDiagnosticTitle"", helpLinkUri:=Nothing, messageFormat:=""MyDiagnosticMessage"", category:=""MyDiagnosticCategory"", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:= true)
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new DiagnosticDescriptor(""MyDiagnosticId"", title:=""MyDiagnosticTitle"", messageFormat:=""MyDiagnosticMessage"", category:=""MyDiagnosticCategory"", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:= true)

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";
            DiagnosticResult[] expected = new[] {
                GetBasicRS1007ExpectedDiagnostic(10, 66),
                GetBasicRS1015ExpectedDiagnostic(10, 137),
                GetBasicRS1007ExpectedDiagnostic(11, 67),
                GetBasicRS1015ExpectedDiagnostic(11, 71)
            };

            VerifyBasic(source, expected);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableString dummyLocalizableTitle = new LocalizableResourceString(""dummyName"", null, null);

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri = ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, true, ""MyDiagnosticDescription"", ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(helpLinkUri: ""HelpLink"", id: ""MyDiagnosticId"", messageFormat:""MyDiagnosticMessage"", title: dummyLocalizableTitle, category: ""MyDiagnosticCategory"", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(helpLinkUri: ""HelpLink"", title: dummyLocalizableTitle);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}
";
            VerifyCSharp(source, TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer

    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = new LocalizableResourceString(""dummyName"", Nothing, Nothing)
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, True, ""MyDiagnosticDescription"", ""HelpLink"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = new DiagnosticDescriptor(helpLinkUri:=""HelpLink"", id:=""MyDiagnosticId"", title:=dummyLocalizableTitle, messageFormat:=""MyDiagnosticMessage"", category:=""MyDiagnosticCategory"", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:=true)
    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = new DiagnosticDescriptor(helpLinkUri:=""HelpLink"", title:=dummyLocalizableTitle)

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";
            VerifyBasic(source, TestValidationMode.AllowCompileErrors);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DiagnosticDescriptorCreationAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DiagnosticDescriptorCreationAnalyzer();
        }

        private static DiagnosticResult GetCSharpRS1015ExpectedDiagnostic(int line, int column)
        {
            return GetRS1015ExpectedDiagnostic(LanguageNames.CSharp, line, column);
        }

        private static DiagnosticResult GetBasicRS1015ExpectedDiagnostic(int line, int column)
        {
            return GetRS1015ExpectedDiagnostic(LanguageNames.VisualBasic, line, column);
        }

        private static DiagnosticResult GetCSharpRS1007ExpectedDiagnostic(int line, int column)
        {
            return GetRS1007ExpectedDiagnostic(LanguageNames.CSharp, line, column);
        }

        private static DiagnosticResult GetBasicRS1007ExpectedDiagnostic(int line, int column)
        {
            return GetRS1007ExpectedDiagnostic(LanguageNames.VisualBasic, line, column);
        }

        private static DiagnosticResult GetRS1007ExpectedDiagnostic(string language, int line, int column)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage, DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName),
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }

        private static DiagnosticResult GetRS1015ExpectedDiagnostic(string language, int line, int column)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.ProvideHelpUriInDescriptorRuleId,
                Message = CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorMessage,
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
