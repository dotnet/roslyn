// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticDescriptorCreationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticDescriptorCreationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticDescriptorCreationAnalyzerTests
    {
        #region RS1007 (UseLocalizableStringsInDescriptorRuleId) and RS1015 (ProvideHelpUriInDescriptorRuleId)

        [Fact]
        public async Task RS1007_RS1015_CSharp_VerifyDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}",
                GetCSharpRS1007ExpectedDiagnostic(11, 9),
                GetCSharpRS1015ExpectedDiagnostic(11, 9),
                GetCSharpRS1028ResultAt(11, 9));
        }

        [Fact]
        public async Task RS1007_RS1015_VisualBasic_VerifyDiagnostic()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
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
",
                GetBasicRS1007ExpectedDiagnostic(10, 66),
                GetBasicRS1015ExpectedDiagnostic(10, 70),
                GetBasicRS1028ResultAt(10, 70));
        }

        [Fact]
        public async Task RS1007_RS1015_CSharp_VerifyDiagnostic_NamedArgumentCases()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}",
                GetCSharpRS1007ExpectedDiagnostic(11, 9),
                GetCSharpRS1028ResultAt(11, 9),
                GetCSharpRS1015ExpectedDiagnostic(11, 118),
                GetCSharpRS1007ExpectedDiagnostic(14, 9),
                GetCSharpRS1015ExpectedDiagnostic(14, 9),
                GetCSharpRS1028ResultAt(14, 9));
        }

        [Fact]
        public async Task RS1007_RS1015_VisualBasic_VerifyDiagnostic_NamedArgumentCases()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
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
",
                GetBasicRS1007ExpectedDiagnostic(10, 66),
                GetBasicRS1028ResultAt(10, 70),
                GetBasicRS1015ExpectedDiagnostic(10, 137),
                GetBasicRS1007ExpectedDiagnostic(11, 67),
                GetBasicRS1015ExpectedDiagnostic(11, 71),
                GetBasicRS1028ResultAt(11, 71));
        }

        [Fact]
        public async Task RS1007_RS1015_CSharp_NoDiagnosticCases()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableString dummyLocalizableTitle = new LocalizableResourceString(""dummyName"", null, null);

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""MyDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, true, ""MyDiagnosticDescription"", ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(helpLinkUri: ""HelpLink"", id: ""MyDiagnosticId"", messageFormat:""MyDiagnosticMessage"", title: dummyLocalizableTitle, category: ""MyDiagnosticCategory"", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

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
",
                GetCSharpRS1028ResultAt(13, 9),
                GetCSharpRS1028ResultAt(16, 9),
                GetCSharpRS1028ResultAt(19, 9));
        }

        [Fact]
        public async Task RS1007_RS1015_VisualBasic_NoDiagnosticCases()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
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

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
",
                GetBasicRS1028ResultAt(12, 70),
                GetBasicRS1028ResultAt(13, 71),
                GetBasicRS1028ResultAt(14, 71));
        }

        #endregion

        #region RS1017 (DiagnosticIdMustBeAConstantRuleId) and RS1019 (UseUniqueDiagnosticIdRuleId)

        [Fact]
        public async Task RS1017_RS1019_CSharp_VerifyDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly string NonConstantDiagnosticId = ""NonConstantDiagnosticId"";
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(NonConstantDiagnosticId, dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor, descriptor2);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer2 : DiagnosticAnalyzer
{
    private static LocalizableString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");


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
}",
                GetCSharpRS1028ResultAt(14, 9),
                GetCSharpRS1017ExpectedDiagnostic(14, 34, "descriptor"),
                GetCSharpRS1028ResultAt(17, 9),
                GetCSharpRS1028ResultAt(38, 9),
                GetCSharpRS1019ExpectedDiagnostic(38, 34, "DuplicateDiagnosticId", "MyAnalyzer"));
        }

        [Fact]
        public async Task RS1017_RS1019_VisualBasic_VerifyDiagnostic()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
    Private Shared ReadOnly NonConstantDiagnosticId = ""NonConstantDiagnosticId""
    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(NonConstantDiagnosticId, dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor, descriptor2)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer2
	Inherits DiagnosticAnalyzer
    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
",
                GetBasicRS1028ResultAt(12, 70),
                GetBasicRS1017ExpectedDiagnostic(12, 91, "descriptor"),
                GetBasicRS1028ResultAt(13, 71),
                GetBasicRS1028ResultAt(29, 70),
                GetBasicRS1019ExpectedDiagnostic(29, 91, "DuplicateDiagnosticId", "MyAnalyzer"));
        }

        [Fact]
        public async Task RS1017_RS1019_CSharp_NoDiagnosticCases()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private const string ConstantDiagnosticId = ""ConstantDiagnosticId"";
    private static LocalizableString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(ConstantDiagnosticId, dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    // Allow multiple descriptors with same rule ID in the same analyzer.
    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage2"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor, descriptor2, descriptor3);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}
",
                GetBasicRS1028ResultAt(14, 9),
                GetBasicRS1028ResultAt(17, 9),
                GetBasicRS1028ResultAt(21, 9));
        }

        [Fact]
        public async Task RS1017_RS1019_VisualBasic_NoDiagnosticCases()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
    Const ConstantDiagnosticId As String = ""ConstantDiagnosticId""
    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new DiagnosticDescriptor(ConstantDiagnosticId, dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")
    ' Allow multiple descriptors with same rule ID in the same analyzer.
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = new DiagnosticDescriptor(""DuplicateDiagnosticId"", dummyLocalizableTitle, ""MyDiagnosticMessage2"", ""MyDiagnosticCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:=""HelpLink"")

	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Return ImmutableArray.Create(descriptor, descriptor2, descriptor3)
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
",
                GetBasicRS1028ResultAt(12, 70),
                GetBasicRS1028ResultAt(13, 71),
                GetBasicRS1028ResultAt(15, 71));
        }

        #endregion

        #region RS1018 (DiagnosticIdMustBeInSpecifiedFormatRuleId) and RS1020 (UseCategoriesFromSpecifiedRangeRuleId)

        [Fact]
        public async Task RS1018_RS1020_CSharp_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""Id1"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""DifferentPrefixId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""Prefix200"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithRange"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(""Prefix101"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor5 =
        new DiagnosticDescriptor(""MySecondPrefix400"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor6 =
        new DiagnosticDescriptor(""MyThirdPrefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor, descriptor2, descriptor3, descriptor4, descriptor5, descriptor6);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}
";
            string additionalText = @"
# FORMAT:
# 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

CategoryWithNoIdRangeOrFormat
CategoryWithPrefix: Prefix
CategoryWithRange: Prefix000-Prefix099
CategoryWithId: Prefix100
CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, additionalText) },
                    ExpectedDiagnostics =
                    {
                        GetCSharpRS1028ResultAt(13, 9),
                        GetCSharpRS1020ExpectedDiagnostic(13, 87, "NotAllowedCategory", AdditionalFileName),
                        GetCSharpRS1028ResultAt(16, 9),
                        GetCSharpRS1018ExpectedDiagnostic(16, 34, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetCSharpRS1028ResultAt(19, 9),
                        GetCSharpRS1018ExpectedDiagnostic(19, 34, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetCSharpRS1028ResultAt(22, 9),
                        GetCSharpRS1018ExpectedDiagnostic(22, 34, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetCSharpRS1028ResultAt(25, 9),
                        GetCSharpRS1018ExpectedDiagnostic(25, 34, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetCSharpRS1028ResultAt(28, 9),
                        GetCSharpRS1018ExpectedDiagnostic(28, 34, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName)
                    }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_VerifyDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(""Id1"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""DifferentPrefixId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix200"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithRange"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix101"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor(""MySecondPrefix400"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyThirdPrefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor, descriptor2, descriptor3, descriptor4, descriptor5, descriptor6)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class
";
            string additionalText = @"
# FORMAT:
# 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

CategoryWithNoIdRangeOrFormat
CategoryWithPrefix: Prefix
CategoryWithRange: Prefix000-Prefix099
CategoryWithId: Prefix100
CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, additionalText) },
                    ExpectedDiagnostics =
                    {
                        GetBasicRS1028ResultAt(12, 70),
                        GetBasicRS1020ExpectedDiagnostic(12, 144, "NotAllowedCategory", AdditionalFileName),
                        GetBasicRS1028ResultAt(13, 71),
                        GetBasicRS1018ExpectedDiagnostic(13, 92, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetBasicRS1028ResultAt(14, 71),
                        GetBasicRS1018ExpectedDiagnostic(14, 92, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetBasicRS1028ResultAt(15, 71),
                        GetBasicRS1018ExpectedDiagnostic(15, 92, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetBasicRS1028ResultAt(16, 71),
                        GetBasicRS1018ExpectedDiagnostic(16, 92, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetBasicRS1028ResultAt(17, 71),
                        GetBasicRS1018ExpectedDiagnostic(17, 92, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                    }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""Id1"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithNoIdRangeOrFormat"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""Prefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2_2 =
        new DiagnosticDescriptor(""Prefix101"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""Prefix001"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithRange"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(""Prefix100"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor5 =
        new DiagnosticDescriptor(""MyFirstPrefix001"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor6 =
        new DiagnosticDescriptor(""MySecondPrefix050"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor7 =
        new DiagnosticDescriptor(""MySecondPrefix300"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor, descriptor2, descriptor2_2, descriptor3, descriptor4, descriptor5, descriptor6, descriptor7);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}
";
            string additionalText = @"
# FORMAT:
# 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

CategoryWithNoIdRangeOrFormat
CategoryWithPrefix: Prefix
CategoryWithRange: Prefix000-Prefix099
CategoryWithId: Prefix100
CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300
";
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, additionalText) },
                    ExpectedDiagnostics =
                    {
                        GetCSharpRS1028ResultAt(13, 9),
                        GetCSharpRS1028ResultAt(16, 9),
                        GetCSharpRS1028ResultAt(19, 9),
                        GetCSharpRS1028ResultAt(22, 9),
                        GetCSharpRS1028ResultAt(25, 9),
                        GetCSharpRS1028ResultAt(28, 9),
                        GetCSharpRS1028ResultAt(31, 9),
                        GetCSharpRS1028ResultAt(34, 9),
                    }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(""Id1"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithNoIdRangeOrFormat"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor2_2 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix001"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithRange"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor(""Prefix100"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyFirstPrefix001"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor(""MySecondPrefix050"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")
    Private Shared ReadOnly descriptor7 As DiagnosticDescriptor = New DiagnosticDescriptor(""MySecondPrefix300"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:=""HelpLink"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor, descriptor2, descriptor2_2, descriptor3, descriptor4, descriptor5, descriptor6, descriptor7)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class
";
            string additionalText = @"
# FORMAT:
# 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

CategoryWithNoIdRangeOrFormat
CategoryWithPrefix: Prefix
CategoryWithRange: Prefix000-Prefix099
CategoryWithId: Prefix100
CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, additionalText) },
                    ExpectedDiagnostics =
                    {
                        GetBasicRS1028ResultAt(12, 70),
                        GetBasicRS1028ResultAt(13, 71),
                        GetBasicRS1028ResultAt(14, 73),
                        GetBasicRS1028ResultAt(15, 71),
                        GetBasicRS1028ResultAt(16, 71),
                        GetBasicRS1028ResultAt(17, 71),
                        GetBasicRS1028ResultAt(18, 71),
                        GetBasicRS1028ResultAt(19, 71),
                    }
                }
            }.RunAsync();
        }

        #endregion

        #region RS1021 (AnalyzerCategoryAndIdRangeFileInvalidRuleId)

        [Fact]
        public async Task RS1021_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor =
        new DiagnosticDescriptor(""Id1"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""DifferentPrefixId"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefix"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""Prefix200"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithRange"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(""Prefix101"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor5 =
        new DiagnosticDescriptor(""MySecondPrefix400"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    private static readonly DiagnosticDescriptor descriptor6 =
        new DiagnosticDescriptor(""MyThirdPrefix"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""CategoryWithPrefixRangeAndId"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor, descriptor2, descriptor3, descriptor4, descriptor5, descriptor6);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}
";
            string additionalText = @"
# FORMAT:
# 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

# Illegal: spaces in category name
Category with spaces
Category with spaces and range: Prefix100-Prefix199

# Illegal: Multiple colons
CategoryMultipleColons: IdWithColon:100

# Illegal: Duplicate category
DuplicateCategory1
DuplicateCategory1
DuplicateCategory2: Prefix100-Prefix199
DuplicateCategory2: Prefix200-Prefix299

# Illegal: ID cannot be non-alphanumeric
CategoryWithBadId1: Prefix_100
CategoryWithBadId2: Prefix_100-Prefix_199

# Illegal: Id cannot have letters after number
CategoryWithBadId3: Prefix000NotAllowed
CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed

# Illegal: Different prefixes in ID range
CategoryWithBadId5: Prefix000-DifferentPrefix099
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, additionalText) },
                    ExpectedDiagnostics =
                    {
                        GetCSharpRS1021ExpectedDiagnostic(6, 1, "Category with spaces", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(7, 1, "Category with spaces and range: Prefix100-Prefix199", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(10, 1, "CategoryMultipleColons: IdWithColon:100", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(14, 1, "DuplicateCategory1", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(16, 1, "DuplicateCategory2: Prefix200-Prefix299", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(19, 1, "CategoryWithBadId1: Prefix_100", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(20, 1, "CategoryWithBadId2: Prefix_100-Prefix_199", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(23, 1, "CategoryWithBadId3: Prefix000NotAllowed", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(24, 1, "CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed", AdditionalFileName),
                        GetCSharpRS1021ExpectedDiagnostic(27, 1, "CategoryWithBadId5: Prefix000-DifferentPrefix099", AdditionalFileName),
                        GetCSharpRS1028ResultAt(13, 9),
                        GetCSharpRS1028ResultAt(16, 9),
                        GetCSharpRS1028ResultAt(19, 9),
                        GetCSharpRS1028ResultAt(22, 9),
                        GetCSharpRS1028ResultAt(25, 9),
                        GetCSharpRS1028ResultAt(28, 9),
                    }
                }
            }.RunAsync();
        }

        #endregion

        #region RS1028 (ProvideCustomTagsInDescriptorRuleId)
        [Fact]
        public async Task ReportOnMissingCustomTags()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.CodeAnalysis;
public class MyAnalyzer
{
    internal static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false);
    internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("""", new LocalizableResourceString("""", null, null),
        new LocalizableResourceString("""", null, null), """", DiagnosticSeverity.Warning, false);
    public void Foo()
    {
        var diag = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false);
    }
}",
                GetCSharpRS1007ExpectedDiagnostic(5, 50),
                GetCSharpRS1015ExpectedDiagnostic(5, 50),
                GetCSharpRS1028ResultAt(5, 50),
                GetCSharpRS1015ExpectedDiagnostic(6, 50),
                GetCSharpRS1028ResultAt(6, 50));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Microsoft.CodeAnalysis
Public Class MyAnalyzer
    Friend Shared Rule1 As DiagnosticDescriptor = New DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, False)
    Friend Shared Rule2 As DiagnosticDescriptor = New DiagnosticDescriptor("""", New LocalizableResourceString("""", Nothing, Nothing), New LocalizableResourceString("""", Nothing, Nothing), """", DiagnosticSeverity.Warning, False)
    Public Sub Foo()
        Dim diag = New DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, False)
    End Sub
End Class",
                GetBasicRS1007ExpectedDiagnostic(4, 51),
                GetBasicRS1015ExpectedDiagnostic(4, 55),
                GetBasicRS1028ResultAt(4, 55),
                GetBasicRS1015ExpectedDiagnostic(5, 55),
                GetBasicRS1028ResultAt(5, 55));
        }

        [Fact]
        public async Task DoNotReportOnNamedCustomTags()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.CodeAnalysis;
public class MyAnalyzer
{
    internal static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false, customTags: """");
    internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("""", new LocalizableResourceString("""", null, null),
        new LocalizableResourceString("""", null, null), """", DiagnosticSeverity.Warning, false, customTags: """");
    public void Foo()
    {
        var diag = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false, customTags: """");
    }
}",
                GetCSharpRS1007ExpectedDiagnostic(5, 50),
                GetCSharpRS1015ExpectedDiagnostic(5, 50),
                GetCSharpRS1015ExpectedDiagnostic(6, 50));

            // Named arguments are incompatible with ParamArray in VB.NET
        }

        [Fact]
        public async Task DoNotReportOnCustomTags()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.CodeAnalysis;
public class MyAnalyzer
{
    internal static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false, null, null, """");
    internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("""", new LocalizableResourceString("""", null, null),
        new LocalizableResourceString("""", null, null), """", DiagnosticSeverity.Warning, false, new LocalizableResourceString("""", null, null), """", """");
    internal static DiagnosticDescriptor Rule3 = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false, null, null, new[] { """", """" });
    internal static DiagnosticDescriptor Rule4 = new DiagnosticDescriptor("""", new LocalizableResourceString("""", null, null),
        new LocalizableResourceString("""", null, null), """", DiagnosticSeverity.Warning, false, new LocalizableResourceString("""", null, null), """", new[] { """", """" });
    public void Foo()
    {
        var diag = new DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, false, null, null, """");
    }
}",
                GetCSharpRS1007ExpectedDiagnostic(5, 50),
                GetCSharpRS1015ExpectedDiagnostic(5, 132),
                GetCSharpRS1007ExpectedDiagnostic(8, 50),
                GetCSharpRS1015ExpectedDiagnostic(8, 132));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Microsoft.CodeAnalysis
Public Class MyAnalyzer
    Friend Shared Rule1 As DiagnosticDescriptor = New DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, False, Nothing, Nothing, """")
    Friend Shared Rule2 As DiagnosticDescriptor = New DiagnosticDescriptor("""", New LocalizableResourceString("""", Nothing, Nothing), New LocalizableResourceString("""", Nothing, Nothing), """", DiagnosticSeverity.Warning, False, New LocalizableResourceString("""", Nothing, Nothing), """", """")
    Friend Shared Rule3 As DiagnosticDescriptor = New DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, False, Nothing, Nothing, { """", """" })
    Friend Shared Rule4 As DiagnosticDescriptor = New DiagnosticDescriptor("""", New LocalizableResourceString("""", Nothing, Nothing), New LocalizableResourceString("""", Nothing, Nothing), """", DiagnosticSeverity.Warning, False, New LocalizableResourceString("""", Nothing, Nothing), """", { """", """" })
    Public Sub Foo()
        Dim diag = New DiagnosticDescriptor("""", """", """", """", DiagnosticSeverity.Warning, False, Nothing, Nothing, """")
    End Sub
End Class",
                GetBasicRS1007ExpectedDiagnostic(4, 51),
                GetBasicRS1015ExpectedDiagnostic(4, 136),
                GetBasicRS1007ExpectedDiagnostic(6, 51),
                GetBasicRS1015ExpectedDiagnostic(6, 136));
        }
        #endregion

        #region RS1029 (DoNotUseReservedDiagnosticIdRuleId)

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_AlreadyUsedId_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""CA0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""CS0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""BC0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(""CA00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor5 =
        new DiagnosticDescriptor(""CS00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor6 =
        new DiagnosticDescriptor(""BC00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}",
                GetCSharpRS1029ResultAt(13, 34, "CA0"),
                GetCSharpRS1029ResultAt(16, 34, "CS0"),
                GetCSharpRS1029ResultAt(19, 34, "BC0"),
                GetCSharpRS1029ResultAt(22, 34, "CA00000000000000000000"),
                GetCSharpRS1029ResultAt(25, 34, "CS00000000000000000000"),
                GetCSharpRS1029ResultAt(28, 34, "BC00000000000000000000"));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""CA0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""CS0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor(""BC0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor(""CA00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor(""CS00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor(""BC00000000000000000000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class",
                GetBasicRS1029ResultAt(12, 92, "CA0"),
                GetBasicRS1029ResultAt(13, 92, "CS0"),
                GetBasicRS1029ResultAt(14, 92, "BC0"),
                GetBasicRS1029ResultAt(15, 92, "CA00000000000000000000"),
                GetBasicRS1029ResultAt(16, 92, "CS00000000000000000000"),
                GetBasicRS1029ResultAt(17, 92, "BC00000000000000000000"));
        }

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_DiagnosticIdSimilarButNotReserved_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""CAA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""CSA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""BCA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor4 =
        new DiagnosticDescriptor(""CA00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor5 =
        new DiagnosticDescriptor(""CS00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor6 =
        new DiagnosticDescriptor(""BC00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""CAA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""CSA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor(""BCA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor(""CA00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor(""CS00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor(""BC00A0"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class");
        }

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_DiagnosticIdSimilarButTooShort_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""CA"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""CS"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""BC"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}");

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""CA"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""CS"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")
    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor(""BC"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class");
        }

        [Theory, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        [InlineData("Microsoft.CodeAnalysis.VersionCheckAnalyzer")]
        [InlineData("Microsoft.CodeQuality.Analyzers")]
        [InlineData("Microsoft.CodeQuality.CSharp.Analyzers")]
        [InlineData("Microsoft.CodeQuality.VisualBasic.Analyzers")]
        [InlineData("Microsoft.NetCore.Analyzers")]
        [InlineData("Microsoft.NetCore.CSharp.Analyzers")]
        [InlineData("Microsoft.NetCore.VisualBasic.Analyzers")]
        [InlineData("Microsoft.NetFramework.Analyzers")]
        [InlineData("Microsoft.NetFramework.CSharp.Analyzers")]
        [InlineData("Microsoft.NetFramework.VisualBasic.Analyzers")]
        [InlineData("Text.Analyzers")]
        [InlineData("Text.CSharp.Analyzers")]
        [InlineData("Text.VisualBasic.Analyzers")]
        public async Task RS1029_CADiagnosticIdOnRoslynAnalyzers_NoDiagnostic(string assemblyName)
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static LocalizableResourceString dummyLocalizableTitle = null;

    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""CA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""HelpLink"", customTags: """");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}",
                SolutionTransforms =
                {
                    (solution, projectId) => solution.GetProject(projectId).WithAssemblyName(assemblyName).Solution,
                },
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestCode = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""CA0000"", dummyLocalizableTitle, ""MyDiagnosticMessage"", ""NotAllowedCategory"", DiagnosticSeverity.Warning, True, Nothing, ""HelpLink"", ""customTag"")

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
    End Sub
End Class",
                SolutionTransforms =
                {
                    (solution, projectId) => solution.GetProject(projectId).WithAssemblyName(assemblyName).Solution,
                },
            }.RunAsync();
        }

        #endregion

        #region Helpers

        private static DiagnosticResult GetCSharpRS1007ExpectedDiagnostic(int line, int column) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseLocalizableStringsInDescriptorRule)
                .WithLocation(line, column)
                .WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);

        private static DiagnosticResult GetBasicRS1007ExpectedDiagnostic(int line, int column) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseLocalizableStringsInDescriptorRule)
                .WithLocation(line, column)
                .WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);

        private static DiagnosticResult GetCSharpRS1015ExpectedDiagnostic(int line, int column) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.ProvideHelpUriInDescriptorRule)
                .WithLocation(line, column);

        private static DiagnosticResult GetBasicRS1015ExpectedDiagnostic(int line, int column) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.ProvideHelpUriInDescriptorRule)
                .WithLocation(line, column);

        private static DiagnosticResult GetCSharpRS1017ExpectedDiagnostic(int line, int column, string descriptorName) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeAConstantRule)
                .WithLocation(line, column)
                .WithArguments(descriptorName);

        private static DiagnosticResult GetBasicRS1017ExpectedDiagnostic(int line, int column, string descriptorName) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeAConstantRule)
                .WithLocation(line, column)
                .WithArguments(descriptorName);

        private static DiagnosticResult GetCSharpRS1018ExpectedDiagnostic(int line, int column, string diagnosticId, string category, string format, string additionalFile) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeInSpecifiedFormatRule)
                .WithLocation(line, column)
                .WithArguments(diagnosticId, category, format, additionalFile);

        private static DiagnosticResult GetBasicRS1018ExpectedDiagnostic(int line, int column, string diagnosticId, string category, string format, string additionalFile) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeInSpecifiedFormatRule)
                .WithLocation(line, column)
                .WithArguments(diagnosticId, category, format, additionalFile);

        private static DiagnosticResult GetCSharpRS1019ExpectedDiagnostic(int line, int column, string duplicateId, string otherAnalyzerName) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseUniqueDiagnosticIdRule)
                .WithLocation(line, column)
                .WithArguments(duplicateId, otherAnalyzerName);

        private static DiagnosticResult GetBasicRS1019ExpectedDiagnostic(int line, int column, string duplicateId, string otherAnalyzerName) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseUniqueDiagnosticIdRule)
                .WithLocation(line, column)
                .WithArguments(duplicateId, otherAnalyzerName);

        private static DiagnosticResult GetCSharpRS1020ExpectedDiagnostic(int line, int column, string category, string additionalFile) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseCategoriesFromSpecifiedRangeRule)
                .WithLocation(line, column)
                .WithArguments(category, additionalFile);

        private static DiagnosticResult GetBasicRS1020ExpectedDiagnostic(int line, int column, string category, string additionalFile) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.UseCategoriesFromSpecifiedRangeRule)
                .WithLocation(line, column)
                .WithArguments(category, additionalFile);

        private static DiagnosticResult GetCSharpRS1028ResultAt(int line, int column) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.ProvideCustomTagsInDescriptorRule)
                .WithLocation(line, column);

        private static DiagnosticResult GetBasicRS1028ResultAt(int line, int column) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.ProvideCustomTagsInDescriptorRule)
                .WithLocation(line, column);

        private static DiagnosticResult GetCSharpRS1021ExpectedDiagnostic(int line, int column, string invalidEntry, string additionalFile) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.AnalyzerCategoryAndIdRangeFileInvalidRule)
                .WithLocation(AdditionalFileName, line, column)
                .WithArguments(invalidEntry, additionalFile);

        private static DiagnosticResult GetCSharpRS1029ResultAt(int line, int column, string ruleId) =>
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DoNotUseReservedDiagnosticIdRule)
                .WithLocation(line, column)
                .WithArguments(ruleId);

        private static DiagnosticResult GetBasicRS1029ResultAt(int line, int column, string ruleId) =>
            VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DoNotUseReservedDiagnosticIdRule)
                .WithLocation(line, column)
                .WithArguments(ruleId);

        private const string AdditionalFileName = "DiagnosticCategoryAndIdRanges.txt";

        #endregion
    }
}
