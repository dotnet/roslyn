' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports System.Collections.Immutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Suppression
    Public MustInherit Class VisualBasicSuppressionTests
        Inherits AbstractSuppressionDiagnosticTest

        Private ReadOnly _compilationOptions As CompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True)

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(
            definition As String,
            parseOptions As ParseOptions,
            compilationOptions As CompilationOptions
        ) As TestWorkspace

            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(
                definition,
                DirectCast(parseOptions, ParseOptions),
                If(DirectCast(compilationOptions, CompilationOptions), New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Friend Overloads Sub Test(initial As XElement, expected As XCData, Optional isLine As Boolean = True, Optional isAddedDocument As Boolean = False)
            Dim initialMarkup = initial.ToString()
            Dim expectedMarkup = expected.Value
            Test(initialMarkup, expectedMarkup, isLine, isAddedDocument)
        End Sub

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

#Region "Pragma disable tests"
        Public MustInherit Class VisualBasicPragmaWarningDisableSuppressionTests
            Inherits VisualBasicSuppressionTests
            Protected NotOverridable Overrides ReadOnly Property CodeActionIndex() As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Class CompilerDiagnosticSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests
                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return Tuple.Create(Of DiagnosticAnalyzer, ISuppressionFixProvider)(Nothing, New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirective()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        [|Dim x As Integer|]
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' {WRN_UnusedLocal_Title}
        Dim x As Integer
#Enable Warning BC42024 ' {WRN_UnusedLocal_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' Unused local variable
        [|Dim x As Integer|]
#Enable Warning BC42024 ' Unused local variable
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective1()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        [|Dim x _
            As Integer|]
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' {WRN_UnusedLocal_Title}
        Dim x _
            As Integer
#Enable Warning BC42024 ' {WRN_UnusedLocal_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' Unused local variable
        [|Dim x _
            As Integer|]
#Enable Warning BC42024 ' Unused local variable
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective2()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
        If i < [|j.MaxValue|] AndAlso
            i > 0 Then
            Console.WriteLine(i)
        End If
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
        If i < j.MaxValue AndAlso
            i > 0 Then
#Enable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
            Console.WriteLine(i)
        End If
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
        If i < [|j.MaxValue|] AndAlso
            i > 0 Then
#Enable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
            Console.WriteLine(i)
        End If
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective3()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
        If i > 0 AndAlso
            i < [|j.MaxValue|] Then
            Console.WriteLine(i)
        End If
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
        If i > 0 AndAlso
            i < j.MaxValue Then
#Enable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
            Console.WriteLine(i)
        End If
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
        If i > 0 AndAlso
            i < [|j.MaxValue|] Then
#Enable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
            Console.WriteLine(i)
        End If
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective4()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        Dim [|x As Integer|],
            y As Integer
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' {WRN_UnusedLocal_Title}
        Dim x As Integer,
            y As Integer
#Enable Warning BC42024 ' {WRN_UnusedLocal_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' Unused local variable
        Dim [|x As Integer|],
            y As Integer
#Enable Warning BC42024 ' Unused local variable
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective5()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        Dim x As Integer,
            [|y As Integer|]
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' {WRN_UnusedLocal_Title}
        Dim x As Integer,
            y As Integer
#Enable Warning BC42024 ' {WRN_UnusedLocal_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method()
#Disable Warning BC42024 ' Unused local variable
        Dim x As Integer,
            [|y As Integer|]
#Enable Warning BC42024 ' Unused local variable
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective6()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
        Dim x = <root>
                    <condition value=<%= i < [|j.MaxValue|] %>/>
                </root>
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
        Dim x = <root>
                    <condition value=<%= i < j.MaxValue %>/>
                </root>
#Enable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method(i As Integer, j As Short)
#Disable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
        Dim x = <root>
                    <condition value=<%= i < [|j.MaxValue|] %>/>
                </root>
#Enable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestMultilineStatementPragmaWarningDirective7()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method(j As Short)
        Dim x = From i As Integer In {}
                Where i < [|j.MaxValue|]
                Select i
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method(j As Short)
#Disable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
        Dim x = From i As Integer In {{}}
                Where i < j.MaxValue
                Select i
#Enable Warning BC42025 ' {WRN_SharedMemberThroughInstance_Title}
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method(j As Short)
#Disable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
        Dim x = From i As Integer In {}
                Where i < [|j.MaxValue|]
                Select i
#Enable Warning BC42025 ' Access of shared member, constant member, enum member or nested type through an instance
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveWithExistingTrivia()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        ' Trivia previous line
        [|Dim x As Integer|]    ' Trivia same line
        ' Trivia next line
    End Sub
End Class]]>
                    Dim expected = $"
Imports System
Class C
    Sub Method()
        ' Trivia previous line
#Disable Warning BC42024 ' {WRN_UnusedLocal_Title}
        Dim x As Integer    ' Trivia same line
#Enable Warning BC42024 ' {WRN_UnusedLocal_Title}
        ' Trivia next line
    End Sub
End Class"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
Class C
    Sub Method()
        ' Trivia previous line
#Disable Warning BC42024 ' Unused local variable
        [|Dim x As Integer|]    ' Trivia same line
#Enable Warning BC42024 ' Unused local variable
        ' Trivia next line
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(970129)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionAroundSingleToken()
                    Dim source = <![CDATA[
Imports System
<Obsolete>
Class C
End Class

Module Module1
    Sub Main
      [|C|]
    End Sub
End Module]]>
                    Dim expected = $"
Imports System
<Obsolete>
Class C
End Class

Module Module1
    Sub Main
#Disable Warning BC40008 ' {WRN_UseOfObsoleteSymbolNoMessage1_Title}
        C
#Enable Warning BC40008 ' {WRN_UseOfObsoleteSymbolNoMessage1_Title}
    End Sub
End Module"

                    Test(source.Value, expected)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System
<Obsolete>
Class C
End Class

Module Module1
    Sub Main
#Disable Warning BC40008 ' Type or member is obsolete
      [|C|]
#Enable Warning BC40008 ' Type or member is obsolete
    End Sub
End Module]]>

                    TestMissing(fixedSource.Value)
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia1()
                    Dim source = <![CDATA[
Class C

' Comment
' Comment
''' <summary><see [|cref="abc"|]/></summary>
    Sub M() ' Comment  

    End Sub
End Class]]>
                    Dim expected = <![CDATA[
Class C

#Disable Warning BC42309
' Comment
' Comment
''' <summary><see cref="abc"/></summary>
    Sub M() ' Comment  
#Enable Warning BC42309

    End Sub
End Class]]>

                    Dim enableDocCommentProcessing = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose)
                    Test(source.Value, expected.Value, enableDocCommentProcessing)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Class C

#Disable Warning BC42309
' Comment
' Comment
''' <summary><see [|cref="abc"|]/></summary>
    Sub M() ' Comment  
#Enable Warning BC42309

    End Sub
End Class]]>

                    TestMissing(fixedSource.Value, enableDocCommentProcessing)
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia2()
                    Dim source = <![CDATA['''[|<summary></summary>|]]]>
                    Dim expected = <![CDATA[#Disable Warning BC42312
  '''<summary></summary>
#Enable Warning BC42312]]>

                    Test(source.Value, expected.Value, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia3()
                    Dim source = <![CDATA[   '''[|<summary></summary>|]   ]]>
                    Dim expected = <![CDATA[#Disable Warning BC42312
  '''<summary></summary>   
#Enable Warning BC42312]]>

                    Test(source.Value, expected.Value, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia4()
                    Dim source = <![CDATA[

'''<summary><see [|cref="abc"|]/></summary>
Class C : End Class

]]>
                    Dim expected = <![CDATA[

#Disable Warning BC42309
'''<summary><see cref="abc"/></summary>
Class C : End Class
#Enable Warning BC42309

]]>

                    Test(source.Value, expected.Value, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia5()
                    Dim source = <![CDATA[class C1 : End Class
'''<summary><see [|cref="abc"|]/></summary>
Class C2 : End Class
Class C3 : End Class]]>
                    Dim expected = <![CDATA[class C1 : End Class
#Disable Warning BC42309
'''<summary><see cref="abc"/></summary>
Class C2 : End Class
#Enable Warning BC42309
Class C3 : End Class]]>

                    Test(source.Value, expected.Value, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
                End Sub

                <WorkItem(1066576)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestPragmaWarningDirectiveAroundTrivia6()
                    Dim source = <![CDATA[class C1 : End Class
Class C2 : End Class [|'''|]
Class C3 : End Class]]>
                    Dim expected = <![CDATA[class C1 : End Class
#Disable Warning BC42309
Class C2 : End Class '''
#Enable Warning BC42309

Class C3 : End Class]]>

                    Test(source.Value, expected.Value, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
                End Sub
            End Class

            Public Class UserHiddenDiagnosticSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests
                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestHiddenDiagnosticCannotBeSuppressed()
                    Dim source = <![CDATA[
Imports System
Class C
    Sub Method()
        Dim i as [|System.Int32|] = 0
        Console.WriteLine(i)
    End Sub
End Class]]>

                    TestMissing(source.Value)
                End Sub
            End Class

            Public Class UserInfoDiagnosticSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests

                Private Class UserDiagnosticAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private _descriptor As New DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault:=True)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(_descriptor)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ClassStatement)
                    End Sub

                    Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                        Dim classDecl = DirectCast(context.Node, ClassStatementSyntax)
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()))
                    End Sub
                End Class

                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New UserDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                
                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestInfoDiagnosticSuppressed()

                    Dim source = <![CDATA[
Imports System

[|Class C|]
    Sub Method()
    End Sub
End Class]]>
                    Dim expected = <![CDATA[
Imports System

#Disable Warning InfoDiagnostic ' InfoDiagnostic
Class C
#Enable Warning InfoDiagnostic ' InfoDiagnostic
    Sub Method()
    End Sub
End Class]]>

                    Test(source.Value, expected.Value)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System

#Disable Warning InfoDiagnostic ' InfoDiagnostic
[|Class C|]
#Enable Warning InfoDiagnostic ' InfoDiagnostic
    Sub Method()
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub
            End Class

            Public Class DiagnosticWithBadIdSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests

                Protected Overrides ReadOnly Property IncludeNoLocationDiagnostics As Boolean
                    Get
                        ' Analyzer driver generates a no-location analyzer exception diagnostic, which we don't intend to test here.
                        Return False
                    End Get
                End Property

                Private Class UserDiagnosticAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private _descriptor As New DiagnosticDescriptor("#$DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", DiagnosticSeverity.Info, isEnabledByDefault:=True)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(_descriptor)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ClassStatement)
                    End Sub

                    Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                        Dim classDecl = DirectCast(context.Node, ClassStatementSyntax)
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()))
                    End Sub
                End Class

                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New UserDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                
                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestDiagnosticWithBadIdSuppressed()

                    ' Diagnostics with bad/invalid ID are not reported.
                    Dim source = <![CDATA[
Imports System

[|Class C|]
    Sub Method()
    End Sub
End Class]]>

                    TestMissing(source.Value)
                End Sub
            End Class

            Public Class UserWarningDiagnosticWithNameMatchingKeywordSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests
                Private Class UserDiagnosticAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private _descriptor As New DiagnosticDescriptor("REm", "REm Title", "REm", "REm", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(_descriptor)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ClassStatement)
                    End Sub

                    Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                        Dim classDecl = DirectCast(context.Node, ClassStatementSyntax)
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()))
                    End Sub
                End Class

                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New UserDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestWarningDiagnosticWithNameMatchingKeywordSuppressed()
                    Dim source = <![CDATA[
Imports System

[|Class C|]
    Sub Method()
    End Sub
End Class]]>
                    Dim expected = <![CDATA[
Imports System

#Disable Warning [REm] ' REm Title
Class C
#Enable Warning [REm] ' REm Title
    Sub Method()
    End Sub
End Class]]>

                    Test(source.Value, expected.Value)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System

#Disable Warning [REm] ' REm Title
[|Class C|]
#Enable Warning [REm] ' REm Title
    Sub Method()
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub
            End Class

            Public Class UserErrorDiagnosticSuppressionTests
                Inherits VisualBasicPragmaWarningDisableSuppressionTests
                Private Class UserDiagnosticAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private _descriptor As New DiagnosticDescriptor("ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", DiagnosticSeverity.[Error], isEnabledByDefault:=True)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(_descriptor)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ClassStatement)
                    End Sub

                    Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                        Dim classDecl = DirectCast(context.Node, ClassStatementSyntax)
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()))
                    End Sub
                End Class

                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New UserDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestErrorDiagnosticCanBeSuppressed()
                    Dim source = <![CDATA[
Imports System

[|Class C|]
    Sub Method()
    End Sub
End Class]]>
                    Dim expected = <![CDATA[
Imports System

#Disable Warning ErrorDiagnostic ' ErrorDiagnostic
Class C
#Enable Warning ErrorDiagnostic ' ErrorDiagnostic
    Sub Method()
    End Sub
End Class]]>

                    Test(source.Value, expected.Value)

                    ' Also verify that the added directive does indeed suppress the diagnostic.
                    Dim fixedSource = <![CDATA[
Imports System

#Disable Warning ErrorDiagnostic ' ErrorDiagnostic
[|Class C|]
#Enable Warning ErrorDiagnostic ' ErrorDiagnostic
    Sub Method()
    End Sub
End Class]]>

                    TestMissing(fixedSource.Value)
                End Sub
            End Class
        End Class

#End Region

#Region "SuppressMessageAttribute tests"

        Public MustInherit Class VisualBasicGlobalSuppressMessageSuppressionTests
            Inherits VisualBasicSuppressionTests
            Protected NotOverridable Overrides ReadOnly Property CodeActionIndex() As Integer
                Get
                    Return 1
                End Get
            End Property

            Public Class CompilerDiagnosticSuppressionTests
                Inherits VisualBasicGlobalSuppressMessageSuppressionTests
                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return Tuple.Create(Of DiagnosticAnalyzer, ISuppressionFixProvider)(Nothing, New VisualBasicSuppressionCodeFixProvider())
                End Function

                
                <WorkItem(730770)>
                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestCompilerDiagnosticsCannotBeSuppressed()

                    Dim source = <![CDATA[
Class Class1
    Sub Method()
        [|Dim x|]
    End Sub
End Class]]>

                    TestActionCount(source.Value, 1)
                End Sub
            End Class

            Public Class UserHiddenDiagnosticSuppressionTests
                Inherits VisualBasicGlobalSuppressMessageSuppressionTests
                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestHiddenDiagnosticsCannotBeSuppressed()
                    Dim source = <![CDATA[
Imports System
Class Class1
    Sub Method()
        [|Dim x As System.Int32 = 0|]
    End Sub
End Class]]>

                    TestMissing(source.Value)
                End Sub
            End Class

            Public Class UserInfoDiagnosticSuppressionTests
                Inherits VisualBasicGlobalSuppressMessageSuppressionTests
                Private Class UserDiagnosticAnalyzer
                    Inherits DiagnosticAnalyzer

                    Private _descriptor As New DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault:=True)

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Return ImmutableArray.Create(_descriptor)
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ClassStatement, SyntaxKind.NamespaceStatement, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement, SyntaxKind.PropertyStatement, SyntaxKind.FieldDeclaration, SyntaxKind.EventStatement)
                    End Sub

                    Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                        Select Case context.Node.Kind()
                            Case SyntaxKind.ClassStatement
                                Dim classDecl = DirectCast(context.Node, ClassStatementSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()))
                                Exit Select

                            Case SyntaxKind.NamespaceStatement
                                Dim ns = DirectCast(context.Node, NamespaceStatementSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, ns.Name.GetLocation()))
                                Exit Select

                            Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                                Dim method = DirectCast(context.Node, MethodStatementSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, method.Identifier.GetLocation()))
                                Exit Select

                            Case SyntaxKind.PropertyStatement
                                Dim p = DirectCast(context.Node, PropertyStatementSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, p.Identifier.GetLocation()))
                                Exit Select

                            Case SyntaxKind.FieldDeclaration
                                Dim f = DirectCast(context.Node, FieldDeclarationSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, f.Declarators.First().Names.First.GetLocation()))
                                Exit Select

                            Case SyntaxKind.EventStatement
                                Dim e = DirectCast(context.Node, EventStatementSyntax)
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, e.Identifier.GetLocation()))
                                Exit Select
                        End Select
                    End Sub
                End Class

                Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)
                    Return New Tuple(Of DiagnosticAnalyzer, ISuppressionFixProvider)(New UserDiagnosticAnalyzer(), New VisualBasicSuppressionCodeFixProvider())
                End Function

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnSimpleType()
                    Dim source = <![CDATA[
Imports System
[|Class Class1|]
    Sub Method()
        Dim x
    End Sub
End Class]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:Class1"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:Class1"")>

[|Class Class1|]
    Sub Method()
        Dim x
    End Sub
End Class"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnNamespace()
                    Dim source = <![CDATA[
Imports System

[|Namespace N|]
    Class Class1
        Sub Method()
            Dim x
        End Sub
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""namespace"", Target:=""~N:N"")>
"

                    Test(source.Value, expected, index:=1)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""namespace"", Target:=""~N:N"")>

[|Namespace N|]
    Class Class1
        Sub Method()
            Dim x
        End Sub
    End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnTypeInsideNamespace()
                    Dim source = <![CDATA[
Imports System

Namespace N1
    Namespace N2
        [|Class Class1|]
            Sub Method()
                Dim x
            End Sub
        End Class
    End Namespace
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:N1.N2.Class1"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:N1.N2.Class1"")>

Namespace N1
    Namespace N2
        [|Class Class1|]
            Sub Method()
                Dim x
            End Sub
        End Class
    End Namespace
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnNestedType()
                    Dim source = <![CDATA[
Imports System

Namespace N
    Class Generic(Of T)
        [|Class Class1|]
            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:N.Generic`1.Class1"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:N.Generic`1.Class1"")>

Namespace N
    Class Generic(Of T)
        [|Class Class1|]
            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnMethod()
                    Dim source = <![CDATA[
Imports System

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method()
                Dim x
            End Sub|]
        End Class
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method"")>

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method()
                Dim x
            End Sub|]
        End Class
    End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnOverloadedMethod()
                    Dim source = <![CDATA[
Imports System

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method(y as Integer, ByRef z as Integer)
                Dim x
            End Sub|]

            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method(System.Int32,System.Int32@)"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method(System.Int32,System.Int32@)"")>

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method(y as Integer, ByRef z as Integer)
                Dim x
            End Sub|]

            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnGenericMethod()
                    Dim source = <![CDATA[
Imports System

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method(Of U)(y as U, ByRef z as Integer)
                Dim x
            End Sub|]

            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method``1(``0,System.Int32@)"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~M:N.Generic`1.Class1.Method``1(``0,System.Int32@)"")>

Namespace N
    Class Generic(Of T)
        Class Class1
            [|Sub Method(Of U)(y as U, ByRef z as Integer)
                Dim x
            End Sub|]

            Sub Method()
                Dim x
            End Sub
        End Class
    End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnProperty()
                    Dim source = <![CDATA[
Imports System

Namespace N
	Class Generic
		Private Class C
			[|Private ReadOnly Property P() As Integer|]
				Get
					Dim x As Integer = 0
				End Get
			End Property
        End Class
    End Class
End Namespace]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~P:N.Generic.C.P"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~P:N.Generic.C.P"")>

Namespace N
	Class Generic
		Private Class C
			[|Private ReadOnly Property P() As Integer|]
				Get
					Dim x As Integer = 0
				End Get
			End Property
		End Class
	End Class
End Namespace"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnField()
                    Dim source = <![CDATA[
Imports System

Class C
	[|Private ReadOnly F As Integer|]
End Class]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~F:C.F"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~F:C.F"")>

Class C
	[|Private ReadOnly F As Integer|]
End Class"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionOnEvent()
                    Dim source = <![CDATA[
Imports System

Public Class SampleEventArgs
	Public Sub New(s As String)
		Text = s
	End Sub
	Public Property Text() As [String]
		Get
			Return m_Text
		End Get
		Private Set
			m_Text = Value
		End Set
	End Property
	Private m_Text As [String]
End Class

Class C
	' Declare the delegate (if using non-generic pattern). 
	Public Delegate Sub SampleEventHandler(sender As Object, e As SampleEventArgs)

	' Declare the event. 
	[|Public Custom Event SampleEvent As SampleEventHandler|]
		AddHandler(ByVal value As SampleEventHandler)
		End AddHandler
		RemoveHandler(ByVal value As SampleEventHandler)
		End RemoveHandler
	End Event
End Class]]>
                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~E:C.SampleEvent"")>
"

                    Test(source.Value, expected)

                    ' Also verify that the added attribute does indeed suppress the diagnostic.
                    Dim fixedSource = $"
Imports System

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""member"", Target:=""~E:C.SampleEvent"")>

Public Class SampleEventArgs
	Public Sub New(s As String)
		Text = s
	End Sub
	Public Property Text() As [String]
		Get
			Return m_Text
		End Get
		Private Set
			m_Text = Value
		End Set
	End Property
	Private m_Text As [String]
End Class

Class C
	' Declare the delegate (if using non-generic pattern). 
	Public Delegate Sub SampleEventHandler(sender As Object, e As SampleEventArgs)

	' Declare the event. 
	[|Public Custom Event SampleEvent As SampleEventHandler|]
		AddHandler(ByVal value As SampleEventHandler)
		End AddHandler
		RemoveHandler(ByVal value As SampleEventHandler)
		End RemoveHandler
	End Event
End Class"

                    TestMissing(fixedSource)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionWithExistingGlobalSuppressionsDocument()
                    Dim source =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="CurrentDocument.vb"><![CDATA[
Imports System

Class Class1
End Class

[|Class Class2|]
End Class]]>
                            </Document>
                            <Document FilePath="GlobalSuppressions.vb"><![CDATA[
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification:="<Pending>", Scope:="type", Target:="Class1")>
]]>
                            </Document>
                        </Project>
                    </Workspace>

                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""<Pending>"", Scope:=""type"", Target:=""Class1"")>
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:Class2"")>
"

                    Test(source.ToString(), expected)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionWithExistingGlobalSuppressionsDocument2()
                    ' Own custom file named GlobalSuppressions.cs
                    Dim source =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="CurrentDocument.vb"><![CDATA[
Imports System

Class Class1
End Class

[|Class Class2|]
End Class]]>
                            </Document>
                            <Document FilePath="GlobalSuppressions.vb"><![CDATA[
' My own file named GlobalSuppressions.vb
Class Class3
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>

                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:Class2"")>
"

                    Test(source.ToString(), expected)
                End Sub

                <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
                Public Sub TestSuppressionWithExistingGlobalSuppressionsDocument3()
                    ' Own custom file named GlobalSuppressions.vb + existing GlobalSuppressions2.vb with global suppressions
                    Dim source =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="CurrentDocument.vb"><![CDATA[
Imports System

Class Class1
End Class

[|Class Class2|]
End Class]]>
                            </Document>
                            <Document FilePath="GlobalSuppressions.vb"><![CDATA[
' My own file named GlobalSuppressions.vb
Class Class3
End Class
]]>
                            </Document>
                            <Document FilePath="GlobalSuppressions2.vb"><![CDATA[
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage("InfoDiagnostic", "InfoDiagnostic:InfoDiagnostic", Justification:="<Pending>", Scope:="type", Target:="Class1")>
]]>
                            </Document>
                        </Project>
                    </Workspace>

                    Dim expected = $"
' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

<Assembly: Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""<Pending>"", Scope:=""type"", Target:=""Class1"")>
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification:=""{FeaturesResources.SuppressionPendingJustification}"", Scope:=""type"", Target:=""~T:Class2"")>
"

                    Test(source.ToString(), expected)
                End Sub
            End Class
        End Class
#End Region
    End Class
End Namespace
