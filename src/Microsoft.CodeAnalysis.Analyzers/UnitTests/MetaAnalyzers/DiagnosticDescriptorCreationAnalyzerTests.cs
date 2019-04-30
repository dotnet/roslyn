// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticDescriptorCreationAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        #region RS1007 (UseLocalizableStringsInDescriptorRuleId) and RS1015 (ProvideHelpUriInDescriptorRuleId)

        [Fact]
        public void RS1007_RS1015_CSharp_VerifyDiagnostic()
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
            DiagnosticResult[] expected = new[] { GetCSharpRS1007ExpectedDiagnostic(11, 9), GetCSharpRS1015ExpectedDiagnostic(11, 9) };
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void RS1007_RS1015_VisualBasic_VerifyDiagnostic()
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
        public void RS1007_RS1015_CSharp_VerifyDiagnostic_NamedArgumentCases()
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
        public void RS1007_RS1015_VisualBasic_VerifyDiagnostic_NamedArgumentCases()
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
        public void RS1007_RS1015_CSharp_NoDiagnosticCases()
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
        public void RS1007_RS1015_VisualBasic_NoDiagnosticCases()
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

        #endregion

        #region RS1017 (DiagnosticIdMustBeAConstantRuleId) and RS1019 (UseUniqueDiagnosticIdRuleId)

        [Fact]
        public void RS1017_RS1019_CSharp_VerifyDiagnostic()
        {
            var source = @"
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
}";
            DiagnosticResult[] expected = new[] {
                // Test0.cs(14,34): warning RS1017: Diagnostic Id for rule 'descriptor' must be a non-null constant.
                GetCSharpRS1017ExpectedDiagnostic(14, 34, "descriptor"),
                // Test0.cs(38,34): warning RS1019: Diagnostic Id 'DuplicateDiagnosticId' is already used by analyzer 'MyAnalyzer'. Please use a different diagnostic ID.
                GetCSharpRS1019ExpectedDiagnostic(38, 34, "DuplicateDiagnosticId", "MyAnalyzer")
            };
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void RS1017_RS1019_VisualBasic_VerifyDiagnostic()
        {
            var source = @"
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
";
            DiagnosticResult[] expected = new[] {
                // Test0.vb(12,91): warning RS1017: Diagnostic Id for rule 'descriptor' must be a non-null constant.
                GetBasicRS1017ExpectedDiagnostic(12, 91, "descriptor"),
                // Test0.vb(29,91): warning RS1019: Diagnostic Id 'DuplicateDiagnosticId' is already used by analyzer 'MyAnalyzer'. Please use a different diagnostic ID.
                GetBasicRS1019ExpectedDiagnostic(29, 91, "DuplicateDiagnosticId", "MyAnalyzer")
            };
            VerifyBasic(source, expected);
        }

        [Fact]
        public void RS1017_RS1019_CSharp_NoDiagnosticCases()
        {
            var source = @"
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
";
            VerifyCSharp(source);
        }

        [Fact]
        public void RS1017_RS1019_VisualBasic_NoDiagnosticCases()
        {
            var source = @"
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
";
            VerifyBasic(source);
        }

        #endregion

        #region RS1018 (DiagnosticIdMustBeInSpecifiedFormatRuleId) and RS1020 (UseCategoriesFromSpecifiedRangeRuleId)

        [Fact]
        public void RS1018_RS1020_CSharp_VerifyDiagnostic()
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
            DiagnosticResult[] expected = new[] {
                // Test0.cs(13,87): warning RS1020: Category 'NotAllowedCategory' is not from the allowed categories specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1020ExpectedDiagnostic(13, 87, "NotAllowedCategory", AdditionalFileName),
                // Test0.cs(16,34): warning RS1018: Diagnostic Id 'DifferentPrefixId' belonging to category 'CategoryWithPrefix' is not in the required range and/or format 'PrefixXXXX' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1018ExpectedDiagnostic(16, 34, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                // Test0.cs(19,34): warning RS1018: Diagnostic Id 'Prefix200' belonging to category 'CategoryWithRange' is not in the required range and/or format 'Prefix0-Prefix99' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1018ExpectedDiagnostic(19, 34, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                // Test0.cs(22,34): warning RS1018: Diagnostic Id 'Prefix101' belonging to category 'CategoryWithId' is not in the required range and/or format 'Prefix100-Prefix100' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1018ExpectedDiagnostic(22, 34, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                // Test0.cs(25,34): warning RS1018: Diagnostic Id 'MySecondPrefix400' belonging to category 'CategoryWithPrefixRangeAndId' is not in the required range and/or format 'MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1018ExpectedDiagnostic(25, 34, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                // Test0.cs(28,34): warning RS1018: Diagnostic Id 'MyThirdPrefix' belonging to category 'CategoryWithPrefixRangeAndId' is not in the required range and/or format 'MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetCSharpRS1018ExpectedDiagnostic(28, 34, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName)
            };

            VerifyCSharp(source, GetAdditionalFile(additionalText), expected);
        }

        [Fact]
        public void RS1018_RS1020_VisualBasic_VerifyDiagnostic()
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
            DiagnosticResult[] expected = new[] {                
                // Test0.vb(12,144): warning RS1020: Category 'NotAllowedCategory' is not from the allowed categories specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1020ExpectedDiagnostic(12, 144, "NotAllowedCategory", AdditionalFileName),
                // Test0.vb(13,92): warning RS1018: Diagnostic Id 'DifferentPrefixId' belonging to category 'CategoryWithPrefix' is not in the required range and/or format 'PrefixXXXX' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1018ExpectedDiagnostic(13, 92, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                // Test0.vb(14,92): warning RS1018: Diagnostic Id 'Prefix200' belonging to category 'CategoryWithRange' is not in the required range and/or format 'Prefix0-Prefix99' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1018ExpectedDiagnostic(14, 92, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                // Test0.vb(15,92): warning RS1018: Diagnostic Id 'Prefix101' belonging to category 'CategoryWithId' is not in the required range and/or format 'Prefix100-Prefix100' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1018ExpectedDiagnostic(15, 92, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                // Test0.vb(16,92): warning RS1018: Diagnostic Id 'MySecondPrefix400' belonging to category 'CategoryWithPrefixRangeAndId' is not in the required range and/or format 'MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1018ExpectedDiagnostic(16, 92, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                // Test0.vb(17,92): warning RS1018: Diagnostic Id 'MyThirdPrefix' belonging to category 'CategoryWithPrefixRangeAndId' is not in the required range and/or format 'MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300' specified in the file 'DiagnosticCategoryAndIdRanges.txt'.
                GetBasicRS1018ExpectedDiagnostic(17, 92, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
            };

            VerifyBasic(source, GetAdditionalFile(additionalText), expected);
        }

        [Fact]
        public void RS1018_RS1020_CSharp_NoDiagnosticCases()
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
            VerifyCSharp(source, GetAdditionalFile(additionalText));
        }

        [Fact]
        public void RS1018_RS1020_VisualBasic_NoDiagnosticCases()
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

            VerifyBasic(source, GetAdditionalFile(additionalText));
        }

        #endregion

        #region RS1021 (AnalyzerCategoryAndIdRangeFileInvalidRuleId)

        [Fact]
        public void RS1021_VerifyDiagnostic()
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
            DiagnosticResult[] expected = new[] {
                // DiagnosticCategoryAndIdRanges.txt(6,1): warning RS1021: Invalid entry 'Category with spaces' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(6, 1, "Category with spaces", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(7,1): warning RS1021: Invalid entry 'Category with spaces and range: Prefix100-Prefix199' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(7, 1, "Category with spaces and range: Prefix100-Prefix199", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(10,1): warning RS1021: Invalid entry 'CategoryMultipleColons: IdWithColon:100' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(10, 1, "CategoryMultipleColons: IdWithColon:100", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(14,1): warning RS1021: Invalid entry 'DuplicateCategory1' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(14, 1, "DuplicateCategory1", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(16,1): warning RS1021: Invalid entry 'DuplicateCategory2: Prefix200-Prefix299' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(16, 1, "DuplicateCategory2: Prefix200-Prefix299", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(19,1): warning RS1021: Invalid entry 'CategoryWithBadId1: Prefix_100' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(19, 1, "CategoryWithBadId1: Prefix_100", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(20,1): warning RS1021: Invalid entry 'CategoryWithBadId2: Prefix_100-Prefix_199' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(20, 1, "CategoryWithBadId2: Prefix_100-Prefix_199", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(23,1): warning RS1021: Invalid entry 'CategoryWithBadId3: Prefix000NotAllowed' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(23, 1, "CategoryWithBadId3: Prefix000NotAllowed", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(24,1): warning RS1021: Invalid entry 'CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(24, 1, "CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed", AdditionalFileName),
                // DiagnosticCategoryAndIdRanges.txt(27,1): warning RS1021: Invalid entry 'CategoryWithBadId5: Prefix000-DifferentPrefix099' in analyzer category and diagnostic ID range specification file 'DiagnosticCategoryAndIdRanges.txt'.
                GetRS1021ExpectedDiagnostic(27, 1, "CategoryWithBadId5: Prefix000-DifferentPrefix099", AdditionalFileName),
            };

            VerifyCSharp(source, GetAdditionalFile(additionalText), expected);
        }

        #endregion

        #region Helpers
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

        private static DiagnosticResult GetCSharpRS1017ExpectedDiagnostic(int line, int column, string descriptorName)
        {
            return GetRS1017ExpectedDiagnostic(LanguageNames.CSharp, line, column, descriptorName);
        }

        private static DiagnosticResult GetCSharpRS1018ExpectedDiagnostic(int line, int column, string diagnosticId, string category, string format, string additionalFile)
        {
            return GetRS1018ExpectedDiagnostic(LanguageNames.CSharp, line, column, diagnosticId, category, format, additionalFile);
        }

        private static DiagnosticResult GetCSharpRS1019ExpectedDiagnostic(int line, int column, string duplicateId, string otherAnalyzerName)
        {
            return GetRS1019ExpectedDiagnostic(LanguageNames.CSharp, line, column, duplicateId, otherAnalyzerName);
        }

        private static DiagnosticResult GetCSharpRS1020ExpectedDiagnostic(int line, int column, string category, string additionalFile)
        {
            return GetRS1020ExpectedDiagnostic(LanguageNames.CSharp, line, column, category, additionalFile);
        }

        private static DiagnosticResult GetBasicRS1007ExpectedDiagnostic(int line, int column)
        {
            return GetRS1007ExpectedDiagnostic(LanguageNames.VisualBasic, line, column);
        }

        private static DiagnosticResult GetBasicRS1017ExpectedDiagnostic(int line, int column, string descriptorName)
        {
            return GetRS1017ExpectedDiagnostic(LanguageNames.VisualBasic, line, column, descriptorName);
        }

        private static DiagnosticResult GetBasicRS1018ExpectedDiagnostic(int line, int column, string diagnosticId, string category, string format, string additionalFile)
        {
            return GetRS1018ExpectedDiagnostic(LanguageNames.VisualBasic, line, column, diagnosticId, category, format, additionalFile);
        }

        private static DiagnosticResult GetBasicRS1019ExpectedDiagnostic(int line, int column, string duplicateId, string otherAnalyzerName)
        {
            return GetRS1019ExpectedDiagnostic(LanguageNames.VisualBasic, line, column, duplicateId, otherAnalyzerName);
        }

        private static DiagnosticResult GetBasicRS1020ExpectedDiagnostic(int line, int column, string category, string additionalFile)
        {
            return GetRS1020ExpectedDiagnostic(LanguageNames.VisualBasic, line, column, category, additionalFile);
        }

        private static DiagnosticResult GetRS1007ExpectedDiagnostic(string language, int line, int column)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.UseLocalizableStringsInDescriptorRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage)
                .WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);
        }

        private static DiagnosticResult GetRS1015ExpectedDiagnostic(string language, int line, int column)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.ProvideHelpUriInDescriptorRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorMessage);
        }

        private static DiagnosticResult GetRS1017ExpectedDiagnostic(string language, int line, int column, string descriptorName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.DiagnosticIdMustBeAConstantRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantMessage)
                .WithArguments(descriptorName);
        }

        private static DiagnosticResult GetRS1018ExpectedDiagnostic(string language, int line, int column, string diagnosticId, string category, string format, string additionalFile)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.DiagnosticIdMustBeInSpecifiedFormatRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeInSpecifiedFormatMessage)
                .WithArguments(diagnosticId, category, format, additionalFile);
        }

        private static DiagnosticResult GetRS1019ExpectedDiagnostic(string language, int line, int column, string duplicateId, string otherAnalyzerName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.UseUniqueDiagnosticIdRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdMessage)
                .WithArguments(duplicateId, otherAnalyzerName);
        }

        private static DiagnosticResult GetRS1020ExpectedDiagnostic(string language, int line, int column, string category, string additionalFile)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.UseCategoriesFromSpecifiedRangeRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.UseCategoriesFromSpecifiedRangeMessage)
                .WithArguments(category, additionalFile);
        }

        private static DiagnosticResult GetRS1021ExpectedDiagnostic(int line, int column, string invalidEntry, string additionalFile)
        {
            return new DiagnosticResult(DiagnosticIds.AnalyzerCategoryAndIdRangeFileInvalidRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(AdditionalFileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.AnalyzerCategoryAndIdRangeFileInvalidMessage)
                .WithArguments(invalidEntry, additionalFile);
        }

        private const string AdditionalFileName = "DiagnosticCategoryAndIdRanges.txt";
        private FileAndSource GetAdditionalFile(string source)
            => new FileAndSource() { Source = source, FilePath = AdditionalFileName };

        #endregion
    }
}
