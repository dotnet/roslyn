' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        <WorkItem(897137, "DevDiv")>
        <Fact>
        Public Sub DiagnosticAnalyzerAllInOne()
            Dim source = TestResource.AllInOneVisualBasicBaseline
            Dim analyzer = New BasicTrackingDiagnosticAnalyzer()
            CreateCompilationWithMscorlib({source}).VerifyAnalyzerDiagnostics({analyzer})
            analyzer.VerifyAllAnalyzerMembersWereCalled()
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds()
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds()
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds()
        End Sub

        <WorkItem(896273, "DevDiv")>
        <Fact>
        Public Sub DiagnosticAnalyzerEnumBlock()
            Dim source =
<project><file><![CDATA[
Public Enum E
    Zero
    One
    Two
End Enum
]]></file></project>
            CreateCompilationWithMscorlib(source).VerifyAnalyzerDiagnostics({New BasicTrackingDiagnosticAnalyzer()})
        End Sub

        <Fact>
        <WorkItem(759)>
        Public Sub AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
            Dim compilation = CreateCompilationWithMscorlib({TestResource.AllInOneVisualBasicCode})
            ThrowingDiagnosticAnalyzer(Of SyntaxKind).VerifyAnalyzerEngineIsSafeAgainstExceptions(
                Function(analyzer) compilation.GetAnalyzerDiagnostics({analyzer}, logAnalyzerExceptionAsDiagnostics:=True))
        End Sub

        <Fact>
        Public Sub AnalyzerOptionsArePassedToAllAnalyzers()
            Dim sourceText = New StringText(String.Empty, encodingOpt:=Nothing)
            Dim additionalTexts As AdditionalText() = {New TestAdditionalText("myfilepath", sourceText)}
            Dim options = New AnalyzerOptions(additionalTexts.ToImmutableArray())

            Dim compilation = CreateCompilationWithMscorlib({TestResource.AllInOneVisualBasicCode})
            Dim analyzer = New OptionsDiagnosticAnalyzer(Of SyntaxKind)(options)
            compilation.GetAnalyzerDiagnostics({analyzer}, options)
            analyzer.VerifyAnalyzerOptions()
        End Sub

        Private NotInheritable Class TestAdditionalText
            Inherits AdditionalText

            Private ReadOnly _path As String
            Private ReadOnly _text As SourceText

            Public Sub New(path As String, text As SourceText)
                _path = path
                _text = text
            End Sub

            Public Overrides ReadOnly Property Path As String
                Get
                    Return _path
                End Get
            End Property

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                Return _text
            End Function
        End Class
    End Class
End Namespace
