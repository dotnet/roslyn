// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticDescriptorCreationAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.DefineDiagnosticDescriptorArgumentsCorrectlyFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticDescriptorCreationAnalyzer,
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers.DefineDiagnosticDescriptorArgumentsCorrectlyFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DiagnosticDescriptorCreationAnalyzerTests
    {
        #region RS1007 (UseLocalizableStringsInDescriptorRuleId) and RS1015 (ProvideHelpUriInDescriptorRuleId)

        [Fact]
        public Task RS1007_RS1015_CSharp_VerifyDiagnosticAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true)|};

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
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(0),
                GetRS1028ResultAt(0));

        [Fact]
        public Task RS1007_RS1015_VisualBasic_VerifyDiagnosticAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = {|#0:new {|#1:DiagnosticDescriptor|}("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:= true)|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(1),
                GetRS1028ResultAt(1));

        [Fact]
        public Task RS1007_RS1015_CSharp_VerifyDiagnostic_NamedArgumentCasesAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", messageFormat: "MyDiagnosticMessage", title: "MyDiagnosticTitle", {|#2:helpLinkUri: null|}, category: "MyDiagnosticCategory", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true)|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#3:new DiagnosticDescriptor("MyDiagnosticId", messageFormat: "MyDiagnosticMessage", title: "MyDiagnosticTitle", category: "MyDiagnosticCategory", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true)|};

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
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1028ResultAt(0),
                GetRS1015ExpectedDiagnostic(2),
                GetRS1007ExpectedDiagnostic(3),
                GetRS1015ExpectedDiagnostic(3),
                GetRS1028ResultAt(3));

        [Fact]
        public Task RS1007_RS1015_VisualBasic_VerifyDiagnostic_NamedArgumentCasesAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = {|#0:new {|#1:DiagnosticDescriptor|}("MyDiagnosticId", title:="MyDiagnosticTitle", {|#2:helpLinkUri:=Nothing|}, messageFormat:="MyDiagnosticMessage", category:="MyDiagnosticCategory", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:= true)|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#3:new {|#4:DiagnosticDescriptor|}("MyDiagnosticId", title:="MyDiagnosticTitle", messageFormat:="MyDiagnosticMessage", category:="MyDiagnosticCategory", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:= true)|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1028ResultAt(1),
                GetRS1015ExpectedDiagnostic(2),
                GetRS1007ExpectedDiagnostic(3),
                GetRS1015ExpectedDiagnostic(4),
                GetRS1028ResultAt(4));

        [Fact]
        public Task RS1007_RS1015_CSharp_NoDiagnosticCasesAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableString dummyLocalizableTitle = new LocalizableResourceString("dummyName", null, null);

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, true, "MyDiagnosticDescription.", "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor(helpLinkUri: "HelpLink", id: "MyDiagnosticId", messageFormat:"MyDiagnosticMessage", title: dummyLocalizableTitle, category: "MyDiagnosticCategory", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true)|};

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

                """,
                GetRS1028ResultAt(0),
                GetRS1028ResultAt(1),
                GetRS1028ResultAt(2));

        [Fact]
        public Task RS1007_RS1015_VisualBasic_NoDiagnosticCasesAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = new LocalizableResourceString("dummyName", Nothing, Nothing)
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new {|#0:DiagnosticDescriptor|}("MyDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new {|#1:DiagnosticDescriptor|}("MyDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "MyDiagnosticDescription.", "HelpLink")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = new {|#2:DiagnosticDescriptor|}(helpLinkUri:="HelpLink", id:="MyDiagnosticId", title:=dummyLocalizableTitle, messageFormat:="MyDiagnosticMessage", category:="MyDiagnosticCategory", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:=true)

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1028ResultAt(0),
                GetRS1028ResultAt(1),
                GetRS1028ResultAt(2));

        #endregion

        #region RS1017 (DiagnosticIdMustBeAConstantRuleId) and RS1019 (UseUniqueDiagnosticIdRuleId)

        [Fact]
        public Task RS1017_RS1019_CSharp_VerifyDiagnosticAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly string NonConstantDiagnosticId = "NonConstantDiagnosticId";
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor({|#1:NonConstantDiagnosticId|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

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
                        {|#3:new DiagnosticDescriptor({|#4:"DuplicateDiagnosticId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};


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
                """,
                GetRS1028ResultAt(0),
                GetRS1017ExpectedDiagnostic(1, "descriptor"),
                GetRS1028ResultAt(2),
                GetRS1028ResultAt(3),
                GetRS1019ExpectedDiagnostic(4, "DuplicateDiagnosticId", "MyAnalyzer"));

        [Fact]
        public Task RS1017_RS1019_CSharp_VerifyDiagnostic_CreateHelperAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly string NonConstantDiagnosticId = "NonConstantDiagnosticId";
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        DiagnosticDescriptorHelper.Create({|#0:NonConstantDiagnosticId|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory");

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
                        DiagnosticDescriptorHelper.Create({|#1:"DuplicateDiagnosticId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory");


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
                """ + CSharpDiagnosticDescriptorCreationHelper,
                GetRS1017ExpectedDiagnostic(0, "descriptor"),
                GetRS1019ExpectedDiagnostic(1, "DuplicateDiagnosticId", "MyAnalyzer"));

        [Fact]
        public Task RS1017_RS1019_VisualBasic_VerifyDiagnosticAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Private Shared ReadOnly NonConstantDiagnosticId = "NonConstantDiagnosticId"
                    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new {|#0:DiagnosticDescriptor|}({|#1:NonConstantDiagnosticId|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new {|#2:DiagnosticDescriptor|}("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")

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
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new {|#3:DiagnosticDescriptor|}({|#4:"DuplicateDiagnosticId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1028ResultAt(0),
                GetRS1017ExpectedDiagnostic(1, "descriptor"),
                GetRS1028ResultAt(2),
                GetRS1028ResultAt(3),
                GetRS1019ExpectedDiagnostic(4, "DuplicateDiagnosticId", "MyAnalyzer"));

        [Fact]
        public Task RS1017_RS1019_VisualBasic_VerifyDiagnostic_CreateHelperAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Private Shared ReadOnly NonConstantDiagnosticId = "NonConstantDiagnosticId"
                    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#0:NonConstantDiagnosticId|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory")

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
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#1:"DuplicateDiagnosticId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory")

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """ + VisualBasicDiagnosticDescriptorCreationHelper,
                GetRS1017ExpectedDiagnostic(0, "descriptor"),
                GetRS1019ExpectedDiagnostic(1, "DuplicateDiagnosticId", "MyAnalyzer"));

        [Fact]
        public Task RS1017_RS1019_CSharp_NoDiagnosticCasesAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private const string ConstantDiagnosticId = "ConstantDiagnosticId";
                    private static LocalizableString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor(ConstantDiagnosticId, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    // Allow multiple descriptors with same rule ID in the same analyzer.
                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage2", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

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

                """,
                GetRS1028ResultAt(0),
                GetRS1028ResultAt(1),
                GetRS1028ResultAt(2));

        [Fact]
        public Task RS1017_RS1019_CSharp_NoDiagnosticCases_CreateHelperAsync()
            => VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private const string ConstantDiagnosticId = "ConstantDiagnosticId";
                    private static LocalizableString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        DiagnosticDescriptorHelper.Create(ConstantDiagnosticId, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory");

                    // Allow multiple descriptors with same rule ID in the same analyzer.
                    private static readonly DiagnosticDescriptor descriptor3 =
                        DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage2", "MyDiagnosticCategory");

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

                """ + CSharpDiagnosticDescriptorCreationHelper);

        [Fact]
        public Task RS1017_RS1019_VisualBasic_NoDiagnosticCasesAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Const ConstantDiagnosticId As String = "ConstantDiagnosticId"
                    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = new {|#0:DiagnosticDescriptor|}(ConstantDiagnosticId, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = new {|#1:DiagnosticDescriptor|}("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")
                    ' Allow multiple descriptors with same rule ID in the same analyzer.
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = new {|#2:DiagnosticDescriptor|}("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage2", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=true, helpLinkUri:="HelpLink")

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1028ResultAt(0),
                GetRS1028ResultAt(1),
                GetRS1028ResultAt(2));

        [Fact]
        public Task RS1017_RS1019_VisualBasic_NoDiagnosticCases_CreateHelperAsync()
            => VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                    Const ConstantDiagnosticId As String = "ConstantDiagnosticId"
                    Private Shared ReadOnly dummyLocalizableTitle As LocalizableString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create(ConstantDiagnosticId, dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage", "MyDiagnosticCategory")
                    ' Allow multiple descriptors with same rule ID in the same analyzer.
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("DuplicateDiagnosticId", dummyLocalizableTitle, "MyDiagnosticMessage2", "MyDiagnosticCategory")

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """ + VisualBasicDiagnosticDescriptorCreationHelper);

        #endregion

        #region RS1018 (DiagnosticIdMustBeInSpecifiedFormatRuleId) and RS1020 (UseCategoriesFromSpecifiedRangeRuleId)

        [Fact]
        public async Task RS1018_RS1020_CSharp_VerifyDiagnosticAsync()
        {
            var source = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", {|#1:"NotAllowedCategory"|}, DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor({|#3:"DifferentPrefixId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#4:new DiagnosticDescriptor({|#5:"Prefix200"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor4 =
                        {|#6:new DiagnosticDescriptor({|#7:"Prefix101"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor5 =
                        {|#8:new DiagnosticDescriptor({|#9:"MySecondPrefix400"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor6 =
                        {|#10:new DiagnosticDescriptor({|#11:"MyThirdPrefix"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

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

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1028ResultAt(0),
                        GetRS1020ExpectedDiagnostic(1, "NotAllowedCategory", AdditionalFileName),
                        GetRS1028ResultAt(2),
                        GetRS1018ExpectedDiagnostic(3, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetRS1028ResultAt(4),
                        GetRS1018ExpectedDiagnostic(5, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetRS1028ResultAt(6),
                        GetRS1018ExpectedDiagnostic(7, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetRS1028ResultAt(8),
                        GetRS1018ExpectedDiagnostic(9, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetRS1028ResultAt(10),
                        GetRS1018ExpectedDiagnostic(11, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName)
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_CSharp_VerifyDiagnostic_CreateHelperAsync()
        {
            var source = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        DiagnosticDescriptorHelper.Create("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", {|#0:"NotAllowedCategory"|});

                    private static readonly DiagnosticDescriptor descriptor2 =
                        DiagnosticDescriptorHelper.Create({|#1:"DifferentPrefixId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix");

                    private static readonly DiagnosticDescriptor descriptor3 =
                        DiagnosticDescriptorHelper.Create({|#2:"Prefix200"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange");

                    private static readonly DiagnosticDescriptor descriptor4 =
                        DiagnosticDescriptorHelper.Create({|#3:"Prefix101"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId");

                    private static readonly DiagnosticDescriptor descriptor5 =
                        DiagnosticDescriptorHelper.Create({|#4:"MySecondPrefix400"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId");

                    private static readonly DiagnosticDescriptor descriptor6 =
                        DiagnosticDescriptorHelper.Create({|#5:"MyThirdPrefix"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId");

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
                """ + CSharpDiagnosticDescriptorCreationHelper;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1020ExpectedDiagnostic(0, "NotAllowedCategory", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(1, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(2, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(3, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(4, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(5, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName)
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_VerifyDiagnosticAsync()
        {
            var source = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = New {|#0:DiagnosticDescriptor|}("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", {|#1:"NotAllowedCategory"|}, DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New {|#2:DiagnosticDescriptor|}({|#3:"DifferentPrefixId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New {|#4:DiagnosticDescriptor|}({|#5:"Prefix200"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New {|#6:DiagnosticDescriptor|}({|#7:"Prefix101"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New {|#8:DiagnosticDescriptor|}({|#9:"MySecondPrefix400"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New {|#10:DiagnosticDescriptor|}({|#11:"MyThirdPrefix"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor, descriptor2, descriptor3, descriptor4, descriptor5, descriptor6)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class

                """;
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1028ResultAt(0),
                        GetRS1020ExpectedDiagnostic(1, "NotAllowedCategory", AdditionalFileName),
                        GetRS1028ResultAt(2),
                        GetRS1018ExpectedDiagnostic(3, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetRS1028ResultAt(4),
                        GetRS1018ExpectedDiagnostic(5, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetRS1028ResultAt(6),
                        GetRS1018ExpectedDiagnostic(7, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetRS1028ResultAt(8),
                        GetRS1018ExpectedDiagnostic(9, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetRS1028ResultAt(10),
                        GetRS1018ExpectedDiagnostic(11, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_VerifyDiagnostic_CreateHelperAsync()
        {
            var source = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", {|#0:"NotAllowedCategory"|})
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#1:"DifferentPrefixId"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#2:"Prefix200"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#3:"Prefix101"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#4:"MySecondPrefix400"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create({|#5:"MyThirdPrefix"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor, descriptor2, descriptor3, descriptor4, descriptor5, descriptor6)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class

                """ + VisualBasicDiagnosticDescriptorCreationHelper;
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1020ExpectedDiagnostic(0, "NotAllowedCategory", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(1, "DifferentPrefixId", "CategoryWithPrefix", "PrefixXXXX", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(2, "Prefix200", "CategoryWithRange", "Prefix0-Prefix99", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(3, "Prefix101", "CategoryWithId", "Prefix100-Prefix100", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(4, "MySecondPrefix400", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                        GetRS1018ExpectedDiagnostic(5, "MyThirdPrefix", "CategoryWithPrefixRangeAndId", "MyFirstPrefixXXXX, MySecondPrefix0-MySecondPrefix99, MySecondPrefix300-MySecondPrefix300", AdditionalFileName),
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_CSharp_NoDiagnosticCasesAsync()
        {
            var source = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithNoIdRangeOrFormat", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2_2 =
                        {|#2:new DiagnosticDescriptor("Prefix101", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#3:new DiagnosticDescriptor("Prefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor4 =
                        {|#4:new DiagnosticDescriptor("Prefix100", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor5 =
                        {|#5:new DiagnosticDescriptor("MyFirstPrefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor6 =
                        {|#6:new DiagnosticDescriptor("MySecondPrefix050", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor7 =
                        {|#7:new DiagnosticDescriptor("MySecondPrefix300", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

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

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1028ResultAt(0),
                        GetRS1028ResultAt(1),
                        GetRS1028ResultAt(2),
                        GetRS1028ResultAt(3),
                        GetRS1028ResultAt(4),
                        GetRS1028ResultAt(5),
                        GetRS1028ResultAt(6),
                        GetRS1028ResultAt(7),
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_CSharp_NoDiagnosticCases_CreateHelperAsync()
        {
            var source = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        DiagnosticDescriptorHelper.Create("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithNoIdRangeOrFormat");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        DiagnosticDescriptorHelper.Create("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix");

                    private static readonly DiagnosticDescriptor descriptor2_2 =
                        DiagnosticDescriptorHelper.Create("Prefix101", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix");

                    private static readonly DiagnosticDescriptor descriptor3 =
                        DiagnosticDescriptorHelper.Create("Prefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange");

                    private static readonly DiagnosticDescriptor descriptor4 =
                        DiagnosticDescriptorHelper.Create("Prefix100", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId");

                    private static readonly DiagnosticDescriptor descriptor5 =
                        DiagnosticDescriptorHelper.Create("MyFirstPrefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId");

                    private static readonly DiagnosticDescriptor descriptor6 =
                        DiagnosticDescriptorHelper.Create("MySecondPrefix050", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId");

                    private static readonly DiagnosticDescriptor descriptor7 =
                        DiagnosticDescriptorHelper.Create("MySecondPrefix300", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId");

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
                """ + CSharpDiagnosticDescriptorCreationHelper;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_NoDiagnosticCasesAsync()
        {
            var source = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = New {|#0:DiagnosticDescriptor|}("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithNoIdRangeOrFormat", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New {|#1:DiagnosticDescriptor|}("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor2_2 As DiagnosticDescriptor = New {|#2:DiagnosticDescriptor|}("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New {|#3:DiagnosticDescriptor|}("Prefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New {|#4:DiagnosticDescriptor|}("Prefix100", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New {|#5:DiagnosticDescriptor|}("MyFirstPrefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New {|#6:DiagnosticDescriptor|}("MySecondPrefix050", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")
                    Private Shared ReadOnly descriptor7 As DiagnosticDescriptor = New {|#7:DiagnosticDescriptor|}("MySecondPrefix300", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault:=True, helpLinkUri:="HelpLink")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor, descriptor2, descriptor2_2, descriptor3, descriptor4, descriptor5, descriptor6, descriptor7)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class

                """;
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1028ResultAt(0),
                        GetRS1028ResultAt(1),
                        GetRS1028ResultAt(2),
                        GetRS1028ResultAt(3),
                        GetRS1028ResultAt(4),
                        GetRS1028ResultAt(5),
                        GetRS1028ResultAt(6),
                        GetRS1028ResultAt(7),
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        [Fact]
        public async Task RS1018_RS1020_VisualBasic_NoDiagnosticCases_CreateHelperAsync()
        {
            var source = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithNoIdRangeOrFormat")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix")
                    Private Shared ReadOnly descriptor2_2 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Prefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Prefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("Prefix100", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("MyFirstPrefix001", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("MySecondPrefix050", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId")
                    Private Shared ReadOnly descriptor7 As DiagnosticDescriptor = DiagnosticDescriptorHelper.Create("MySecondPrefix300", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor, descriptor2, descriptor2_2, descriptor3, descriptor4, descriptor5, descriptor6, descriptor7)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class

                """ + VisualBasicDiagnosticDescriptorCreationHelper;
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    CategoryWithNoIdRangeOrFormat
                    CategoryWithPrefix: Prefix
                    CategoryWithRange: Prefix000-Prefix099
                    CategoryWithId: Prefix100
                    CategoryWithPrefixRangeAndId: MyFirstPrefix, MySecondPrefix000-MySecondPrefix099, MySecondPrefix300

                    """) }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        #endregion

        #region RS1021 (AnalyzerCategoryAndIdRangeFileInvalidRuleId)

        [Fact]
        public async Task RS1021_VerifyDiagnosticAsync()
        {
            var source = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor("Id1", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("DifferentPrefixId", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefix", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor("Prefix200", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithRange", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor4 =
                        {|#3:new DiagnosticDescriptor("Prefix101", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor5 =
                        {|#4:new DiagnosticDescriptor("MySecondPrefix400", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

                    private static readonly DiagnosticDescriptor descriptor6 =
                        {|#5:new DiagnosticDescriptor("MyThirdPrefix", dummyLocalizableTitle, "MyDiagnosticMessage", "CategoryWithPrefixRangeAndId", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink")|};

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

                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (AdditionalFileName, """

                    # FORMAT:
                    # 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

                    # Illegal: spaces in category name
                    {|#6:Category with spaces|}
                    {|#7:Category with spaces and range: Prefix100-Prefix199|}

                    # Illegal: Multiple colons
                    {|#8:CategoryMultipleColons: IdWithColon:100|}

                    # Illegal: Duplicate category
                    DuplicateCategory1
                    {|#9:DuplicateCategory1|}
                    DuplicateCategory2: Prefix100-Prefix199
                    {|#10:DuplicateCategory2: Prefix200-Prefix299|}

                    # Illegal: ID cannot be non-alphanumeric
                    {|#11:CategoryWithBadId1: Prefix_100|}
                    {|#12:CategoryWithBadId2: Prefix_100-Prefix_199|}

                    # Illegal: Id cannot have letters after number
                    {|#13:CategoryWithBadId3: Prefix000NotAllowed|}
                    {|#14:CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed|}

                    # Illegal: Different prefixes in ID range
                    {|#15:CategoryWithBadId5: Prefix000-DifferentPrefix099|}

                    """) },
                    ExpectedDiagnostics =
                    {
                        GetRS1021ExpectedDiagnostic(6, "Category with spaces", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(7, "Category with spaces and range: Prefix100-Prefix199", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(8, "CategoryMultipleColons: IdWithColon:100", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(9, "DuplicateCategory1", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(10, "DuplicateCategory2: Prefix200-Prefix299", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(11, "CategoryWithBadId1: Prefix_100", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(12, "CategoryWithBadId2: Prefix_100-Prefix_199", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(13, "CategoryWithBadId3: Prefix000NotAllowed", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(14, "CategoryWithBadId4: Prefix000NotAllowed-Prefix099NotAllowed", AdditionalFileName),
                        GetRS1021ExpectedDiagnostic(15, "CategoryWithBadId5: Prefix000-DifferentPrefix099", AdditionalFileName),
                        GetRS1028ResultAt(0),
                        GetRS1028ResultAt(1),
                        GetRS1028ResultAt(2),
                        GetRS1028ResultAt(3),
                        GetRS1028ResultAt(4),
                        GetRS1028ResultAt(5),
                    }
                },
                SolutionTransforms = { WithoutEnableReleaseTrackingWarning }
            }.RunAsync();
        }

        #endregion

        #region RS1028 (ProvideCustomTagsInDescriptorRuleId)
        [Fact]
        public async Task ReportOnMissingCustomTagsAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using Microsoft.CodeAnalysis;
                public class MyAnalyzer
                {
                    internal static DiagnosticDescriptor Rule1 = {|#0:new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false)|};
                    internal static DiagnosticDescriptor Rule2 = {|#1:new DiagnosticDescriptor("", new LocalizableResourceString("", null, null),
                        new LocalizableResourceString("", null, null), "", DiagnosticSeverity.Warning, false)|};
                    public void SomeMethod()
                    {
                        var diag = new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false);
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(0),
                GetRS1028ResultAt(0),
                GetRS1015ExpectedDiagnostic(1),
                GetRS1028ResultAt(1));

            await VerifyBasicAnalyzerAsync("""

                Imports Microsoft.CodeAnalysis
                Public Class MyAnalyzer
                    Friend Shared Rule1 As DiagnosticDescriptor = {|#0:New {|#1:DiagnosticDescriptor|}("", "", "", "", DiagnosticSeverity.Warning, False)|}
                    Friend Shared Rule2 As DiagnosticDescriptor = New {|#2:DiagnosticDescriptor|}("", New LocalizableResourceString("", Nothing, Nothing), New LocalizableResourceString("", Nothing, Nothing), "", DiagnosticSeverity.Warning, False)
                    Public Sub SomeMethod()
                        Dim diag = New DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, False)
                    End Sub
                End Class
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(1),
                GetRS1028ResultAt(1),
                GetRS1015ExpectedDiagnostic(2),
                GetRS1028ResultAt(2));
        }

        [Fact]
        public Task DoNotReportOnNamedCustomTagsAsync()
            => VerifyCSharpAnalyzerAsync("""

                using Microsoft.CodeAnalysis;
                public class MyAnalyzer
                {
                    internal static DiagnosticDescriptor Rule1 = {|#0:new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false, customTags: "")|};
                    internal static DiagnosticDescriptor Rule2 = {|#1:new DiagnosticDescriptor("", new LocalizableResourceString("", null, null),
                        new LocalizableResourceString("", null, null), "", DiagnosticSeverity.Warning, false, customTags: "")|};
                    public void SomeMethod()
                    {
                        var diag = new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false, customTags: "");
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(1));

        [Fact]
        public async Task DoNotReportOnCustomTagsAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using Microsoft.CodeAnalysis;
                public class MyAnalyzer
                {
                    internal static DiagnosticDescriptor Rule1 = {|#0:new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false, null, {|#1:null|}, "")|};
                    internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("", new LocalizableResourceString("", null, null),
                        new LocalizableResourceString("", null, null), "", DiagnosticSeverity.Warning, false, new LocalizableResourceString("", null, null), "", "");
                    internal static DiagnosticDescriptor Rule3 = {|#2:new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false, null, {|#3:null|}, new[] { "", "" })|};
                    internal static DiagnosticDescriptor Rule4 = new DiagnosticDescriptor("", new LocalizableResourceString("", null, null),
                        new LocalizableResourceString("", null, null), "", DiagnosticSeverity.Warning, false, new LocalizableResourceString("", null, null), "", new[] { "", "" });
                    public void SomeMethod()
                    {
                        var diag = new DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, false, null, null, "");
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2),
                GetRS1015ExpectedDiagnostic(3));

            await VerifyBasicAnalyzerAsync("""

                Imports Microsoft.CodeAnalysis
                Public Class MyAnalyzer
                    Friend Shared Rule1 As DiagnosticDescriptor = {|#0:New DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, False, Nothing, {|#1:Nothing|}, "")|}
                    Friend Shared Rule2 As DiagnosticDescriptor = New DiagnosticDescriptor("", New LocalizableResourceString("", Nothing, Nothing), New LocalizableResourceString("", Nothing, Nothing), "", DiagnosticSeverity.Warning, False, New LocalizableResourceString("", Nothing, Nothing), "", "")
                    Friend Shared Rule3 As DiagnosticDescriptor = {|#2:New DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, False, Nothing, {|#3:Nothing|}, { "", "" })|}
                    Friend Shared Rule4 As DiagnosticDescriptor = New DiagnosticDescriptor("", New LocalizableResourceString("", Nothing, Nothing), New LocalizableResourceString("", Nothing, Nothing), "", DiagnosticSeverity.Warning, False, New LocalizableResourceString("", Nothing, Nothing), "", { "", "" })
                    Public Sub SomeMethod()
                        Dim diag = New DiagnosticDescriptor("", "", "", "", DiagnosticSeverity.Warning, False, Nothing, Nothing, "")
                    End Sub
                End Class
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1015ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2),
                GetRS1015ExpectedDiagnostic(3));
        }
        #endregion

        #region RS1029 (DoNotUseReservedDiagnosticIdRuleId)

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_AlreadyUsedId_DiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor1 =
                        new DiagnosticDescriptor({|#0:"CA0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        new DiagnosticDescriptor({|#1:"CS0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor3 =
                        new DiagnosticDescriptor({|#2:"BC0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor4 =
                        new DiagnosticDescriptor({|#3:"CA00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor5 =
                        new DiagnosticDescriptor({|#4:"CS00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor6 =
                        new DiagnosticDescriptor({|#5:"BC00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

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
                }
                """,
                GetRS1029ResultAt(0, "CA0"),
                GetRS1029ResultAt(1, "CS0"),
                GetRS1029ResultAt(2, "BC0"),
                GetRS1029ResultAt(3, "CA00000000000000000000"),
                GetRS1029ResultAt(4, "CS00000000000000000000"),
                GetRS1029ResultAt(5, "BC00000000000000000000"));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor({|#0:"CA0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor({|#1:"CS0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor({|#2:"BC0"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor({|#3:"CA00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor({|#4:"CS00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor({|#5:"BC00000000000000000000"|}, dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class
                """,
                GetRS1029ResultAt(0, "CA0"),
                GetRS1029ResultAt(1, "CS0"),
                GetRS1029ResultAt(2, "BC0"),
                GetRS1029ResultAt(3, "CA00000000000000000000"),
                GetRS1029ResultAt(4, "CS00000000000000000000"),
                GetRS1029ResultAt(5, "BC00000000000000000000"));
        }

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_DiagnosticIdSimilarButNotReserved_NoDiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor1 =
                        new DiagnosticDescriptor("CAA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        new DiagnosticDescriptor("CSA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor3 =
                        new DiagnosticDescriptor("BCA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor4 =
                        new DiagnosticDescriptor("CA00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor5 =
                        new DiagnosticDescriptor("CS00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor6 =
                        new DiagnosticDescriptor("BC00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

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
                }
                """);

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor("CAA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor("CSA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor("BCA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = New DiagnosticDescriptor("CA00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor5 As DiagnosticDescriptor = New DiagnosticDescriptor("CS00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor6 As DiagnosticDescriptor = New DiagnosticDescriptor("BC00A0", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class
                """);
        }

        [Fact, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        public async Task RS1029_DiagnosticIdSimilarButTooShort_NoDiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor1 =
                        new DiagnosticDescriptor("CA", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor2 =
                        new DiagnosticDescriptor("CS", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

                    private static readonly DiagnosticDescriptor descriptor3 =
                        new DiagnosticDescriptor("BC", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

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
                }
                """);

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor("CA", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor("CS", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = New DiagnosticDescriptor("BC", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class
                """);
        }

        [Theory, WorkItem(1727, "https://github.com/dotnet/roslyn-analyzers/issues/1727")]
        [InlineData("Microsoft.CodeAnalysis.VersionCheckAnalyzer")]
        [InlineData("Microsoft.CodeAnalysis.NetAnalyzers")]
        [InlineData("Microsoft.CodeAnalysis.CSharp.NetAnalyzers")]
        [InlineData("Microsoft.CodeAnalysis.VisualBasic.NetAnalyzers")]
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
        public async Task RS1029_CADiagnosticIdOnRoslynAnalyzers_NoDiagnosticAsync(string assemblyName)
        {
            await new VerifyCS.Test
            {
                TestCode = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static LocalizableResourceString dummyLocalizableTitle = null;

                    private static readonly DiagnosticDescriptor descriptor1 =
                        new DiagnosticDescriptor("CA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "");

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
                }
                """,
                SolutionTransforms =
                {
                    (solution, projectId) => solution.GetProject(projectId)!.WithAssemblyName(assemblyName).Solution,
                    WithoutEnableReleaseTrackingWarning,
                },
            }.RunAsync();

            await new VerifyVB.Test
            {
                TestCode = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared dummyLocalizableTitle As LocalizableResourceString = Nothing
                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor("CA0000", dummyLocalizableTitle, "MyDiagnosticMessage", "NotAllowedCategory", DiagnosticSeverity.Warning, True, Nothing, "HelpLink", "customTag")

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                    End Sub
                End Class
                """,
                SolutionTransforms =
                {
                    (solution, projectId) => solution.GetProject(projectId)!.WithAssemblyName(assemblyName).Solution,
                    WithoutEnableReleaseTrackingWarning,
                }
            }.RunAsync();
        }

        #endregion

        #region RS1031 (DefineDiagnosticTitleCorrectlyRule)

        [WindowsOnlyFact, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        public async Task RS1031_TitleStringEndsWithPeriod_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnosticTitle."|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = {|#4:"MyDiagnosticTitle."|};
                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#5:new DiagnosticDescriptor("MyDiagnosticId", {|#6:s_title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static string s_title = {|#7:"MyDiagnosticTitle."|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = "MyDiagnosticTitle";
                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#5:new DiagnosticDescriptor("MyDiagnosticId", s_title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static string s_title = "MyDiagnosticTitle";

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4),
                GetRS1007ExpectedDiagnostic(5),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(6).WithLocation(7));

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnosticTitle."|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = {|#4:"MyDiagnosticTitle."|}
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#5:new DiagnosticDescriptor("MyDiagnosticId", {|#6:s_title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly s_title As String = {|#7:"MyDiagnosticTitle."|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = "MyDiagnosticTitle"
                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#5:new DiagnosticDescriptor("MyDiagnosticId", s_title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly s_title As String = "MyDiagnosticTitle"

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4),
                GetRS1007ExpectedDiagnostic(5),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(6).WithLocation(7));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        public async Task RS1031_TitleStringEndsWithPeriod_ResxFile_DiagnosticAsync(
            string csharpLocalizableTitleExpression, string basicLocalizableTitleExpression,
            string csharpLocalizableFieldDeclaration = "", string basicLocalizableFieldDeclaration = "")
        {
            string additionalFileName = "Resources.resx";
            string additionalFileTextFormat = """

                <root>
                  <data name="AnalyzerTitle" xml:space="preserve">
                    <value>{0}</value>
                    <comment>Optional comment.</comment>
                  </data>
                </root>
                """;

            string csharpSourceFormat = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Resources
                {{
                    public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                    public static string AnalyzerTitle => string.Empty;
                }}

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                public sealed class MyAnalyzer : DiagnosticAnalyzer
                {{
                    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                        "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                    {1}
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                    public override void Initialize(AnalysisContext context) {{ }}
                }}
                """;
            await VerifyCSharpCodeFixAsync(
                source: string.Format(csharpSourceFormat, $"{{|#0:{csharpLocalizableTitleExpression}|}}", csharpLocalizableFieldDeclaration),
                fixedSource: string.Format(csharpSourceFormat, csharpLocalizableTitleExpression, csharpLocalizableFieldDeclaration),
                additionalFileName: additionalFileName,
                additionalFileText: string.Format(additionalFileTextFormat, "My Analyzer Title."),
                fixedAdditionalFileText: string.Format(additionalFileTextFormat, "My Analyzer Title"),
                expected:
                [
                    VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                ]);

            string basicSourceFormat = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Class Resources
                    Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                        Return Nothing
                    End Function
                    Public Shared Readonly Property AnalyzerTitle As String = ""
                End Class

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                    Private Shared Readonly Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                        "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Custom tags")
                    {1}
                    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class
                """;
            await VerifyBasicCodeFixAsync(
                source: string.Format(basicSourceFormat, $"{{|#0:{basicLocalizableTitleExpression}|}}", basicLocalizableFieldDeclaration),
                fixedSource: string.Format(basicSourceFormat, basicLocalizableTitleExpression, basicLocalizableFieldDeclaration),
                additionalFileName: additionalFileName,
                additionalFileText: string.Format(additionalFileTextFormat, "My Analyzer Title."),
                fixedAdditionalFileText: string.Format(additionalFileTextFormat, "My Analyzer Title"),
                expected:
                [
                    VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                ]);
        }

        [WindowsOnlyFact, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        public async Task RS1031_TitleIsMultiSentence_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnostic. Title."|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = {|#4:"MyDiagnostic. Title"|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnostic", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = "MyDiagnostic";

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4));

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnostic. Title."|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = {|#4:"MyDiagnostic. Title"|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnostic", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = "MyDiagnostic"

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        public async Task RS1031_TitleIsMultiSentence_ResxFile_DiagnosticAsync(
            string csharpLocalizableTitleExpression, string basicLocalizableTitleExpression,
            string csharpLocalizableFieldDeclaration = "", string basicLocalizableFieldDeclaration = "")
        {
            await VerifyTitleAsync("MyDiagnostic. Title.", "MyDiagnostic");
            await VerifyTitleAsync("MyDiagnostic. Title", "MyDiagnostic");
            return;

            //  Local functions

            async Task VerifyTitleAsync(string title, string fixedTitle)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerTitle" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerTitle => string.Empty;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpLocalizableTitleExpression}|}}", csharpLocalizableFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpLocalizableTitleExpression, csharpLocalizableFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                                    VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerTitle As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicLocalizableTitleExpression}|}}", basicLocalizableFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicLocalizableTitleExpression, basicLocalizableFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        [WindowsOnlyFact, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        public async Task RS1031_TitleIsMultiSentence_MultipleDescriptorsUsingSameTitle_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId1", {|#1:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId2", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = {|#4:"MyDiagnosticTitle. AnalyzerTitle."|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId1", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId2", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Title = "MyDiagnosticTitle";

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
        GetRS1007ExpectedDiagnostic(0),
        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(4),
        GetRS1007ExpectedDiagnostic(2),
        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4)
    );

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId1", {|#1:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId2", {|#3:Title|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = {|#4:"MyDiagnosticTitle. AnalyzerTitle."|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId1", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId2", Title, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Title As String = "MyDiagnosticTitle"

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
            GetRS1007ExpectedDiagnostic(0),
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(4),
            GetRS1007ExpectedDiagnostic(2),
            VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(4));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        public async Task RS1031_TitleIsMultiSentence_MultipleDescriptorsUsingSameTitle_ResxFile_DiagnosticAsync(string csharpTitleExpression, string basicTitleExpression)
        {
            string additionalFileName = "Resources.resx";
            string additionalFileTextFormat = """

                <root>
                  <data name="AnalyzerTitle" space="preserve">
                    <value>{0}</value>
                  </data>
                </root>
                """;

            string csharpSourceFormat = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Resources
                {{
                    public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                    public static string AnalyzerTitle => default;
                }}

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                public sealed class MyAnalyzer : DiagnosticAnalyzer
                {{
                    private static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor(
                        "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                    private static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor(
                        "RuleId", {1}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                    private static readonly LocalizableString Title = {2};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule1, Rule2);
                    public override void Initialize(AnalysisContext context) {{ }}
                }}
                """;
            await VerifyCSharpCodeFixAsync(
                source: string.Format(csharpSourceFormat, "{|#0:Title|}", "{|#1:Title|}", csharpTitleExpression),
                fixedSource: string.Format(csharpSourceFormat, "Title", "Title", csharpTitleExpression),
                additionalFileName: additionalFileName,
                additionalFileText: string.Format(additionalFileTextFormat, "MyDiagnostic. Title."),
                fixedAdditionalFileText: string.Format(additionalFileTextFormat, "MyDiagnostic"),
                expected:
                [
                    VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0),
                    VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1)
                ]);

            string basicSourceFormat = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Class Resources
                    Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                        Return Nothing
                    End Function
                    Public Shared ReadOnly Property AnalyzerTitle As String = Nothing
                End Class

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Public Class MyAnalyzer : Inherits DiagnosticAnalyzer
                    Private Shared ReadOnly Rule1 As DiagnosticDescriptor = New DiagnosticDescriptor(
                        "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags")
                    Private Shared ReadOnly Rule2 As DiagnosticDescriptor = New DiagnosticDescriptor(
                        "RuleId", {1}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags")
                    Private Shared ReadOnly Title As LocalizableString = {2}

                    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule1, Rule2)
                    Public Overrides Sub Initialize(context As AnalysisContext)
                    End Sub
                End Class
                """;
            await VerifyBasicCodeFixAsync(
                source: string.Format(basicSourceFormat, "{|#0:Title|}", "{|#1:Title|}", basicTitleExpression),
                fixedSource: string.Format(basicSourceFormat, "Title", "Title", basicTitleExpression),
                additionalFileName: additionalFileName,
                additionalFileText: string.Format(additionalFileTextFormat, "MyDiagnostic. Title."),
                fixedAdditionalFileText: string.Format(additionalFileTextFormat, "MyDiagnostic"),
                expected:
                [
                    VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0),
                    VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1)
                ]);
        }

        [WindowsOnlyFact, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        public async Task RS1031_TitleStringContainsLineReturn_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnostic\rTitle"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:"MyDiagnostic\nTitle"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#4:new DiagnosticDescriptor("MyDiagnosticId", {|#5:"MyDiagnostic\r\nTitle"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnostic", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnostic", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#4:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnostic", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(3),
                GetRS1007ExpectedDiagnostic(4),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(5).WithLocation(5));

            // NOTE: Code fix does not handle binary operations.
            var vbCode = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", {|#1:"MyDiagnostic" & vbCr & "Title"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", {|#3:"MyDiagnostic" & vbLf & "Title"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#4:new DiagnosticDescriptor("MyDiagnosticId", {|#5:"MyDiagnostic" & vbCrLf & "Title"|}, "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """;
            await VerifyBasicCodeFixAsync(vbCode, vbCode,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(3).WithLocation(3),
                GetRS1007ExpectedDiagnostic(4),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(5).WithLocation(5));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        [InlineData(
            "s_localizableTitle",
            "s_localizableTitle",
            "private static readonly LocalizableString s_localizableTitle = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle));",
            "Private Shared ReadOnly s_localizableTitle As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        public async Task RS1031_TitleStringContainsLineReturn_ResxFile_DiagnosticAsync(
            string csharpTitleExpression, string basicTitleExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyTitleAsync(
                """
                MyDiagnostic
                Title.
                """,
                @"MyDiagnostic");
            await VerifyTitleAsync(
                """
                MyDiagnostic
                Title
                """,
                @"MyDiagnostic");

            return;

            //  Local functions

            async Task VerifyTitleAsync(string title, string fixedTitle)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerTitle" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerTitle => string.Empty;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpTitleExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpTitleExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared Readonly Property AnalyzerTitle As String = ""
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Readonly Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Custom tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicTitleExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicTitleExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        [WindowsOnlyFact, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        public async Task RS1031_ValidTitleString_NoDiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "Title can contain A.B qualifications", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "Title can contain 'A.B' qualifications", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "Title can contain A.B qualifications", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "Title can contain 'A.B' qualifications", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));
        }

        [WindowsOnlyTheory, WorkItem(3958, "https://github.com/dotnet/roslyn-analyzers/issues/3958")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerTitle), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerTitle), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerTitle))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerTitle))")]
        public async Task RS1031_LeadingOrTailingWhitespace_DiagnosticAsync(string csharpTitleExpression, string basicTitleExpression)
        {
            await VerifyTitleAsync("Title with trailing space ", "Title with trailing space");
            await VerifyTitleAsync(" Title with leading space", "Title with leading space");
            await VerifyTitleAsync("\t    Title with leading and trailing spaces/tabs  \t     ", "Title with leading and trailing spaces/tabs");
            await VerifyTitleAsync("Title with trailing space. ", "Title with trailing space");
            return;

            //  Local functions

            async Task VerifyTitleAsync(string title, string fixedTitle)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerTitle" xml:space="preserve">
                        <value>{0}</value>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerTitle => string.Empty;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");

                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpTitleExpression}|}}"),
                    fixedSource: string.Format(csharpSourceFormat, csharpTitleExpression),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerTitle As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", {0}, "Message", "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Tags")

                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicTitleExpression}|}}"),
                    fixedSource: string.Format(basicSourceFormat, basicTitleExpression),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, title),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedTitle),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticTitleCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        #endregion // RS1031 (DefineDiagnosticTitleCorrectlyRule)

        #region RS1032 (DefineDiagnosticMessageCorrectlyRule)

        [WindowsOnlyFact, WorkItem(3576, "https://github.com/dotnet/roslyn-analyzers/issues/3576")]
        public async Task RS1032_MessageStringEndsWithPeriodAndIsNotMultiSentence_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"MyDiagnosticMessage."|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#3:Message|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Message = {|#4:"MyDiagnostic.Message."|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", Message, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Message = "MyDiagnostic.Message";

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(3).WithLocation(4));

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"MyDiagnosticMessage."|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#3:Message|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Message As String = {|#4:"MyDiagnostic.Message."|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", Message, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Const Message As String = "MyDiagnostic.Message"

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(3).WithLocation(4));
        }

        [WindowsOnlyTheory, WorkItem(3576, "https://github.com/dotnet/roslyn-analyzers/issues/3576")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        public async Task RS1032_MessageStringEndsWithPeriodAndIsNotMultiSentence_ResxFile_DiagnosticAsync(
            string csharpMessageExpression, string basicMessageExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyMessageAsync("My diagnostic message.", "My diagnostic message");
            await VerifyMessageAsync("MyDiagnostic.Message.", "MyDiagnostic.Message");
            return;

            //  Local functions

            async Task VerifyMessageAsync(string message, string fixedMessage)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerMessage" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerMessage => default;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpMessageExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpMessageExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerMessage As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicMessageExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicMessageExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        [WindowsOnlyFact, WorkItem(3576, "https://github.com/dotnet/roslyn-analyzers/issues/3576")]
        public async Task RS1032_MessageStringIsMultiSentenceAndDoesNotEndWithPeriod_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"Message is. Multi-sentence"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

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
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message is. Multi-sentence.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

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
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1));

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"Message is. Multi-sentence"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message is. Multi-sentence.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1));
        }

        [WindowsOnlyFact, WorkItem(3576, "https://github.com/dotnet/roslyn-analyzers/issues/3576")]
        public async Task RS1032_MessageStringContainsLineReturn_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"MyDiagnostic\rMessage"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#3:"MyDiagnostic\nMessage"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#4:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#5:"MyDiagnostic\r\nMessage"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnostic. Message.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnostic. Message.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#4:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnostic. Message.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(3).WithLocation(3),
                GetRS1007ExpectedDiagnostic(4),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(5).WithLocation(5));

            // NOTE: Code fix does not handle binary operations.
            var vbCode = """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#1:"MyDiagnostic" & vbCr & "Message"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#3:"MyDiagnostic" & vbLf & "Message"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#4:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", {|#5:"MyDiagnostic" & vbCrLf & "Message"|}, "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """;
            await VerifyBasicCodeFixAsync(vbCode, vbCode,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(3).WithLocation(3),
                GetRS1007ExpectedDiagnostic(4),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(5).WithLocation(5));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        public async Task RS1032_MessageStringContainsLineReturn_ResxFile_DiagnostiAsync(
            string csharpMessageExpression, string basicMessageExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyMessageAsync(
                """
                MyDiagnostic
                Message1.
                """, "MyDiagnostic. Message1.");
            await VerifyMessageAsync(
                """
                MyDiagnostic.
                Message2
                """, "MyDiagnostic. Message2.");
            return;

            //  Local functions

            async Task VerifyMessageAsync(string message, string fixedMessage)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerMessage" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerMessage => default;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpMessageExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpMessageExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerMessage As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicMessageExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicMessageExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        [WindowsOnlyFact, WorkItem(3576, "https://github.com/dotnet/roslyn-analyzers/issues/3576")]
        public async Task RS1032_ValidMessageString_NoDiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message is a. Multi-sentence.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message can contain A.B qualifications", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor4 =
                        {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message can contain 'A.B' qualifications", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3, descriptor4);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2),
                GetRS1007ExpectedDiagnostic(3));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message is a. Multi-sentence.", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message can contain A.B qualifications", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor4 As DiagnosticDescriptor = {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "Message can contain 'A.B' qualifications", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3, descriptor4)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2),
                GetRS1007ExpectedDiagnostic(3));
        }

        [WindowsOnlyTheory, WorkItem(3958, "https://github.com/dotnet/roslyn-analyzers/issues/3958")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(Resources.AnalyzerMessage), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerMessage), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        [InlineData(
            "s_localizableMessage",
            "s_localizableMessage",
            "private static readonly LocalizableString s_localizableMessage = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerMessage));",
            "Private Shared ReadOnly s_localizableMessage As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerMessage))")]
        public async Task RS1032_LeadingOrTrailingWhitespaces_DiagnosticAsync(
            string csharpMessageExpression, string basicMessageExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyMessageAsync("Message with trailing whitespace ", "Message with trailing whitespace");
            await VerifyMessageAsync(" Message with leading whitespace", "Message with leading whitespace");
            await VerifyMessageAsync("  \t    Message with leading and trailing spaces/tabs  \t    ", "Message with leading and trailing spaces/tabs");
            await VerifyMessageAsync("Message with period and trailing whitespace. ", "Message with period and trailing whitespace");
            return;

            //  Local functions

            async Task VerifyMessageAsync(string message, string fixedMessage)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerMessage" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerMessage => default;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, true, "Description.", "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpMessageExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpMessageExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerMessage As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", "Title", {0}, "Category", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicMessageExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicMessageExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, message),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedMessage),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticMessageCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        #endregion // RS1032 (DefineDiagnosticMessageCorrectlyRule)

        #region RS1033 (DefineDiagnosticDescriptionCorrectlyRule)

        [WindowsOnlyFact, WorkItem(3577, "https://github.com/dotnet/roslyn-analyzers/issues/3577")]
        public async Task RS1033_DescriptionStringDoesNotEndWithPunctuation_DiagnosticAsync()
        {
            await VerifyCSharpCodeFixAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, {|#1:description: {|#2:"MyDiagnosticDescription"|}|}, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, {|#4:description: Description|}, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Description = {|#5:"MyDiagnosticDescription"|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """, """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "MyDiagnosticDescription.", helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description, helpLinkUri: "HelpLink", customTags: "")|};
                    private const string Description = "MyDiagnosticDescription.";

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(1).WithLocation(2),
                GetRS1007ExpectedDiagnostic(3),
                VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(4).WithLocation(5));

            await VerifyBasicCodeFixAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, {|#1:"Description"|}, "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, {|#3:Description|}, "HelpLinkUrl", "Tag")|}
                    Private Const Description As String = {|#4:"Description"|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """, """

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, Description, "HelpLinkUrl", "Tag")|}
                    Private Const Description As String = "Description."

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(1).WithLocation(1),
                GetRS1007ExpectedDiagnostic(2),
                VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(3).WithLocation(4));
        }

        [WindowsOnlyTheory, WorkItem(3575, "https://github.com/dotnet/roslyn-analyzers/issues/3575")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerDescription), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerDescription), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableDescription",
            "s_localizableDescription",
            "private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableDescription As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerDescription), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerDescription))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerDescription))")]
        [InlineData(
            "s_localizableDescription",
            "s_localizableDescription",
            "private static readonly LocalizableString s_localizableDescription = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerDescription));",
            "Private Shared ReadOnly s_localizableDescription As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerDescription))")]
        public async Task RS1033_DescriptionStringDoesNotEndWithPunctuation_ResxFile_DiagnosticAsync(
            string csharpDescriptionExpression, string basicDescriptionExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyDescriptionAsync("MyDiagnosticDescription", "MyDiagnosticDescription.");
            await VerifyDescriptionAsync("MyDiagnostic. Description", "MyDiagnostic. Description.");
            return;

            //  Local functions

            async Task VerifyDescriptionAsync(string description, string fixedDescription)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerDescription" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerDescription => default;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", "Title", "Message", "Category", DiagnosticSeverity.Warning, true, {0}, "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpDescriptionExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpDescriptionExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, description),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedDescription),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerDescription As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", "Title", "Message", "Category", DiagnosticSeverity.Warning, True, {0}, "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicDescriptionExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicDescriptionExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, description),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedDescription),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        [WindowsOnlyFact, WorkItem(3577, "https://github.com/dotnet/roslyn-analyzers/issues/3577")]
        public async Task RS1033_DescriptionEndsWithPunctuation_NoDiagnosticAsync()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true,
                            description: "MyDiagnosticDescription.", helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true,
                            description: "MyDiagnosticDescription!", helpLinkUri: "HelpLink", customTags: "")|};

                    private static readonly DiagnosticDescriptor descriptor3 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true,
                            description: "MyDiagnosticDescription?", helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.VisualBasic

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory",
                        DiagnosticSeverity.Warning, True, "Description.", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory",
                        DiagnosticSeverity.Warning, True, "Description!", "HelpLinkUrl", "Tag")|}

                    Private Shared ReadOnly descriptor3 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory",
                        DiagnosticSeverity.Warning, True, "Description?", "HelpLinkUrl", "Tag")|}

                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Return ImmutableArray.Create(descriptor1, descriptor2, descriptor3)
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));
        }

        [WindowsOnlyTheory, WorkItem(3958, "https://github.com/dotnet/roslyn-analyzers/issues/3958")]
        [InlineData(
            "new LocalizableResourceString(nameof(Resources.AnalyzerDescription), null, typeof(Resources))",
            "New LocalizableResourceString(NameOf(Resources.AnalyzerDescription), Nothing, GetType(Resources))")]
        [InlineData(
            "s_localizableDescription",
            "s_localizableDescription",
            "private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), null, typeof(Resources));",
            "Private Shared ReadOnly s_localizableDescription As LocalizableString = New LocalizableResourceString(NameOf(Resources.AnalyzerDescription), Nothing, GetType(Resources))")]
        [InlineData(
            "Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerDescription))",
            "Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerDescription))")]
        [InlineData(
            "s_localizableDescription",
            "s_localizableDescription",
            "private static readonly LocalizableString s_localizableDescription = Resources.CreateLocalizableResourceString(nameof(Resources.AnalyzerDescription));",
            "Private Shared ReadOnly s_localizableDescription As LocalizableString = Resources.CreateLocalizableResourceString(NameOf(Resources.AnalyzerDescription))")]
        public async Task RS1033_LeadingOrTrailingWhitespaces_DiagnosticAsync(
            string csharpDescriptionExpression, string basicDescriptionExpression,
            string csharpFieldDeclaration = "", string basicFieldDeclaration = "")
        {
            await VerifyDescriptionAsync("Description with trailing space ", "Description with trailing space.");
            await VerifyDescriptionAsync(" Description with leading space", "Description with leading space.");
            await VerifyDescriptionAsync("  \t    Description with leading and trailing spaces/tabs  \t    ", "Description with leading and trailing spaces/tabs.");
            await VerifyDescriptionAsync("Description with period and trailing space. ", "Description with period and trailing space.");
            return;

            //  Local functions

            async Task VerifyDescriptionAsync(string description, string fixedDescription)
            {
                string additionalFileName = "Resources.resx";
                string additionalFileTextFormat = """

                    <root>
                      <data name="AnalyzerDescription" xml:space="preserve">
                        <value>{0}</value>
                        <comment>Optional comment.</comment>
                      </data>
                    </root>
                    """;

                string csharpSourceFormat = """

                    using System;
                    using System.Collections.Immutable;
                    using Microsoft.CodeAnalysis;
                    using Microsoft.CodeAnalysis.Diagnostics;

                    class Resources
                    {{
                        public static LocalizableResourceString CreateLocalizableResourceString(string resourceName) => default;
                        public static string AnalyzerDescription => default;
                    }}

                    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                    public sealed class MyAnalyzer : DiagnosticAnalyzer
                    {{
                        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                            "RuleId", "Title", "Message", "Category", DiagnosticSeverity.Warning, true, {0}, "HelpLinkUri", "Tags");
                        {1}
                        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {{ get; }} = ImmutableArray.Create(Rule);
                        public override void Initialize(AnalysisContext context) {{ }}
                    }}
                    """;
                await VerifyCSharpCodeFixAsync(
                    source: string.Format(csharpSourceFormat, $"{{|#0:{csharpDescriptionExpression}|}}", csharpFieldDeclaration),
                    fixedSource: string.Format(csharpSourceFormat, csharpDescriptionExpression, csharpFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, description),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedDescription),
                    expected:
                    [
                        VerifyCS.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(0)
                    ]);

                string basicSourceFormat = """

                    Imports System
                    Imports System.Collections.Immutable
                    Imports Microsoft.CodeAnalysis
                    Imports Microsoft.CodeAnalysis.Diagnostics

                    Class Resources
                        Public Shared Function CreateLocalizableResourceString(resourceName As String) As LocalizableResourceString
                            Return Nothing
                        End Function
                        Public Shared ReadOnly Property AnalyzerDescription As String = Nothing
                    End Class

                    <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                    Public NotInheritable Class MyAnalyzer : Inherits DiagnosticAnalyzer
                        Private Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                            "RuleId", "Title", "Message", "Category", DiagnosticSeverity.Warning, True, {0}, "HelpLinkUri", "Tags")
                        {1}
                        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)
                        Public Overrides Sub Initialize(context As AnalysisContext)
                        End Sub
                    End Class
                    """;
                await VerifyBasicCodeFixAsync(
                    source: string.Format(basicSourceFormat, $"{{|#0:{basicDescriptionExpression}|}}", basicFieldDeclaration),
                    fixedSource: string.Format(basicSourceFormat, basicDescriptionExpression, basicFieldDeclaration),
                    additionalFileName: additionalFileName,
                    additionalFileText: string.Format(additionalFileTextFormat, description),
                    fixedAdditionalFileText: string.Format(additionalFileTextFormat, fixedDescription),
                    expected:
                    [
                        VerifyVB.Diagnostic(DiagnosticDescriptorCreationAnalyzer.DefineDiagnosticDescriptionCorrectlyRule).WithLocation(0)
                    ]);
            }
        }

        #endregion // RS1033 (DefineDiagnosticDescriptionCorrectlyRule)

        #region RS1037 (AddCompilationEndCustomTagRule)
        [Fact, WorkItem(6282, "https://github.com/dotnet/roslyn-analyzers/issues/6282")]
        public async Task RS1037_WithRequiredCustomTag_NoDiagnostic()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "CompilationEnd")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
                        {
                            compilationStartAnalysisContext.RegisterCompilationEndAction(compilationEndAnalysisContext =>
                            {
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None));
                            });
                        });
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "CompilationEnd")|}

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterCompilationStartAction(Function(compilationStartAnalysisContext)
                            compilationStartAnalysisContext.RegisterCompilationEndAction(Function(compilationEndAnalysisContext)
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None))
                            End Function)
                        End Function)
                    End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0));
        }

        [Fact, WorkItem(6282, "https://github.com/dotnet/roslyn-analyzers/issues/6282")]
        public async Task RS1037_NonInlinedCustomTags_NoDiagnostic()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor1 =
                        {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: tags)|};
                    private static readonly string[] tags = new string[] { "" };

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
                        {
                            compilationStartAnalysisContext.RegisterCompilationEndAction(compilationEndAnalysisContext =>
                            {
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None));
                            });
                        });
                    }
                }
                """,
                GetRS1007ExpectedDiagnostic(0));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = {|#0:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", Tags)|}
                    Private Shared ReadOnly Tags As String() = { "" }

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterCompilationStartAction(Function(compilationStartAnalysisContext)
                            compilationStartAnalysisContext.RegisterCompilationEndAction(Function(compilationEndAnalysisContext)
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None))
                            End Function)
                        End Function)
                    End Sub
                End Class

                """,
                GetRS1007ExpectedDiagnostic(0));
        }

        [Fact, WorkItem(6282, "https://github.com/dotnet/roslyn-analyzers/issues/6282")]
        public async Task RS1037_WithoutRequiredCustomTag_Diagnostic()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor {|#0:descriptor1|} =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
                        {
                            compilationStartAnalysisContext.RegisterCompilationEndAction(compilationEndAnalysisContext =>
                            {
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None));

                                var diag2 = Diagnostic.Create(descriptor2, Location.None);
                            });
                        });
                    }
                }
                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly {|#0:descriptor1|} As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1, descriptor2)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterCompilationStartAction(Function(compilationStartAnalysisContext)
                            compilationStartAnalysisContext.RegisterCompilationEndAction(Function(compilationEndAnalysisContext)
                                compilationEndAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor1, Location.None))

                                Dim diag2 = Diagnostic.Create(descriptor2, Location.None)
                            End Function)
                        End Function)
                    End Sub
                End Class

                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));
        }

        [Fact, WorkItem(6282, "https://github.com/dotnet/roslyn-analyzers/issues/6282")]
        public async Task RS1037_DiagnosticStoredInLocal_WithInitializer_Diagnostic()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor {|#0:descriptor1|} =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor descriptor2 =
                        {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
                        {
                            compilationStartAnalysisContext.RegisterCompilationEndAction(compilationEndAnalysisContext =>
                            {
                                var diag1 = Diagnostic.Create(descriptor1, Location.None);
                                compilationEndAnalysisContext.ReportDiagnostic(diag1);

                                var diag2 = Diagnostic.Create(descriptor2, Location.None);
                            });
                        });
                    }
                }
                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly {|#0:descriptor1|} As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = {|#2:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1, descriptor2)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterCompilationStartAction(Function(compilationStartAnalysisContext)
                            compilationStartAnalysisContext.RegisterCompilationEndAction(Function(compilationEndAnalysisContext)
                                Dim diag1 = Diagnostic.Create(descriptor1, Location.None)
                                compilationEndAnalysisContext.ReportDiagnostic(diag1)

                                Dim diag2 = Diagnostic.Create(descriptor2, Location.None)
                            End Function)
                        End Function)
                    End Sub
                End Class

                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1007ExpectedDiagnostic(2));
        }

        [Fact, WorkItem(6282, "https://github.com/dotnet/roslyn-analyzers/issues/6282")]
        public async Task RS1037_DiagnosticStoredInLocal_WithAssignment_Diagnostic()
        {
            await VerifyCSharpAnalyzerAsync("""

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor {|#0:descriptor1|} =
                        {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};
                    private static readonly DiagnosticDescriptor {|#2:descriptor2|} =
                        {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(descriptor1, descriptor2);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
                        {
                            compilationStartAnalysisContext.RegisterCompilationEndAction(compilationEndAnalysisContext =>
                            {
                                var diag = Diagnostic.Create(descriptor1, Location.None);
                                compilationEndAnalysisContext.ReportDiagnostic(diag);

                                diag = Diagnostic.Create(descriptor2, Location.None);
                                compilationEndAnalysisContext.ReportDiagnostic(diag);
                            });
                        });
                    }
                }
                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1037ExpectedDiagnostic(2, "descriptor2"),
                GetRS1007ExpectedDiagnostic(3));

            await VerifyBasicAnalyzerAsync("""

                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private Shared ReadOnly {|#0:descriptor1|} As DiagnosticDescriptor = {|#1:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}
                    Private Shared ReadOnly {|#2:descriptor2|} As DiagnosticDescriptor = {|#3:new DiagnosticDescriptor("MyDiagnosticId", "MyDiagnosticTitle", "MyDiagnosticMessage", "MyDiagnosticCategory", DiagnosticSeverity.Warning, True, description:=Nothing, helpLinkUri:="HelpLinkUrl", "Tag")|}

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(descriptor1, descriptor2)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterCompilationStartAction(Function(compilationStartAnalysisContext)
                            compilationStartAnalysisContext.RegisterCompilationEndAction(Function(compilationEndAnalysisContext)
                                Dim diag = Diagnostic.Create(descriptor1, Location.None)
                                compilationEndAnalysisContext.ReportDiagnostic(diag)

                                diag = Diagnostic.Create(descriptor2, Location.None)
                                compilationEndAnalysisContext.ReportDiagnostic(diag)
                            End Function)
                        End Function)
                    End Sub
                End Class

                """,
                GetRS1037ExpectedDiagnostic(0, "descriptor1"),
                GetRS1007ExpectedDiagnostic(1),
                GetRS1037ExpectedDiagnostic(2, "descriptor2"),
                GetRS1007ExpectedDiagnostic(3));
        }

        #endregion // RS1037 (AddCompilationEndCustomTagRule)

        [Fact, WorkItem(6035, "https://github.com/dotnet/roslyn-analyzers/issues/6035")]
        public async Task VerifyFieldReferenceForFieldDefinedInSeparateFile()
        {
            var file1 = """

                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzer : DiagnosticAnalyzer
                {
                    private static readonly DiagnosticDescriptor descriptor =
                        {|#0:new DiagnosticDescriptor(HelperType.Id, HelperType.Title, HelperType.Message, HelperType.Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "HelpLink", customTags: "")|};

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
                """;

            var file2 = """

                public class HelperType
                {
                    public const string Id = "Id";
                    public const string Title = "Title";
                    public const string Message = "Message";
                    public const string Category = "Category";
                }
                """;

            var expected = new[] { GetRS1007ExpectedDiagnostic(0) };
            var test = new VerifyCS.Test
            {
                TestState = { Sources = { file1, file2 } },
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        #region Helpers

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.UseLocalizableStringsInDescriptorRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1007ExpectedDiagnostic(int markupKey) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.UseLocalizableStringsInDescriptorRule)
                .WithLocation(markupKey)
                .WithArguments(WellKnownTypeNames.MicrosoftCodeAnalysisLocalizableString);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.ProvideHelpUriInDescriptorRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1015ExpectedDiagnostic(int markupKey) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.ProvideHelpUriInDescriptorRule)
                .WithLocation(markupKey);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeAConstantRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1017ExpectedDiagnostic(int markupKey, string descriptorName) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeAConstantRule)
                .WithLocation(markupKey)
                .WithArguments(descriptorName);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeInSpecifiedFormatRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1018ExpectedDiagnostic(int markupKey, string diagnosticId, string category, string format, string additionalFile) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeInSpecifiedFormatRule)
                .WithLocation(markupKey)
                .WithArguments(diagnosticId, category, format, additionalFile);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.UseUniqueDiagnosticIdRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1019ExpectedDiagnostic(int markupKey, string duplicateId, string otherAnalyzerName) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.UseUniqueDiagnosticIdRule)
                .WithLocation(markupKey)
                .WithArguments(duplicateId, otherAnalyzerName);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.UseCategoriesFromSpecifiedRangeRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1020ExpectedDiagnostic(int markupKey, string category, string additionalFile) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.UseCategoriesFromSpecifiedRangeRule)
                .WithLocation(markupKey)
                .WithArguments(category, additionalFile);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.ProvideCustomTagsInDescriptorRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1028ResultAt(int markupKey) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.ProvideCustomTagsInDescriptorRule)
                .WithLocation(markupKey);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.AnalyzerCategoryAndIdRangeFileInvalidRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1021ExpectedDiagnostic(int markupKey, string invalidEntry, string additionalFile) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.AnalyzerCategoryAndIdRangeFileInvalidRule)
                .WithLocation(markupKey)
                .WithArguments(invalidEntry, additionalFile);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.DoNotUseReservedDiagnosticIdRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1029ResultAt(int markupKey, string ruleId) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.DoNotUseReservedDiagnosticIdRule)
                .WithLocation(markupKey)
                .WithArguments(ruleId);

        /// <summary>
        /// Creates an expected diagnostic for <inheritdoc cref="DiagnosticDescriptorCreationAnalyzer.AddCompilationEndCustomTagRule"/>
        /// </summary>
        private static DiagnosticResult GetRS1037ExpectedDiagnostic(int markupKey, string fieldName) =>
            new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.AddCompilationEndCustomTagRule)
                .WithLocation(markupKey)
                .WithArguments(fieldName);

        private static async Task VerifyCSharpAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyBasicAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestCode = source,
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyCSharpCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyBasicCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyCSharpCodeFixAsync(string source, string additionalFileName, string additionalFileText, string fixedSource, string fixedAdditionalFileText, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (additionalFileName, additionalFileText), },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    AdditionalFiles = { (additionalFileName, fixedAdditionalFileText), },
                },
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyBasicCodeFixAsync(string source, string additionalFileName, string additionalFileText, string fixedSource, string fixedAdditionalFileText, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (additionalFileName, additionalFileText), },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    AdditionalFiles = { (additionalFileName, fixedAdditionalFileText), },
                },
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
            };

            test.SolutionTransforms.Add(WithoutEnableReleaseTrackingWarning);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static readonly ImmutableDictionary<string, ReportDiagnostic> s_enableReleaseTrackingWarningDisabled = ImmutableDictionary<string, ReportDiagnostic>.Empty
            .Add(DiagnosticDescriptorCreationAnalyzer.EnableAnalyzerReleaseTrackingRule.Id, ReportDiagnostic.Suppress);

        private static Solution WithoutEnableReleaseTrackingWarning(Solution solution, ProjectId projectId)
        {
            var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(s_enableReleaseTrackingWarningDisabled));
            return solution.WithProjectCompilationOptions(projectId, compilationOptions);
        }

        private const string AdditionalFileName = "DiagnosticCategoryAndIdRanges.txt";

        private const string CSharpDiagnosticDescriptorCreationHelper = """

            internal static class DiagnosticDescriptorHelper
            {
                // Dummy DiagnosticDescriptor creation helper.
                public static DiagnosticDescriptor Create(
                    string id,
                    LocalizableString title,
                    LocalizableString messageFormat,
                    string category)
                => null;
            }
            """;
        private const string VisualBasicDiagnosticDescriptorCreationHelper = """

            Friend Partial Module DiagnosticDescriptorHelper
                ' Dummy DiagnosticDescriptor creation helper.
                Function Create(id As String, title As LocalizableString, messageFormat As LocalizableString, category As String) As DiagnosticDescriptor
                    Return Nothing
                End Function
            End Module
            """;

        #endregion
    }
}
