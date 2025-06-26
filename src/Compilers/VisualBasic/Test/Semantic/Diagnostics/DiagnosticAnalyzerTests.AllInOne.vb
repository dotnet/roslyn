' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        <WorkItem(897137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/897137")>
        <Fact>
        Public Sub DiagnosticAnalyzerAllInOne()
            Dim source = TestResource.AllInOneVisualBasicBaseline
            Dim analyzer = New BasicTrackingDiagnosticAnalyzer()
            Dim options = New AnalyzerOptions({DirectCast(New TestAdditionalText(), AdditionalText)}.ToImmutableArray())
            CreateCompilationWithMscorlib40({source}).VerifyAnalyzerDiagnostics({analyzer}, options)
            analyzer.VerifyAllAnalyzerMembersWereCalled()
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds()
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds(New HashSet(Of SyntaxKind)())
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds()
        End Sub

        <WorkItem(896273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/896273")>
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
            CreateCompilationWithMscorlib40(source).VerifyAnalyzerDiagnostics({New BasicTrackingDiagnosticAnalyzer()})
        End Sub

        <Fact>
        <WorkItem(759, "https://github.com/dotnet/roslyn/issues/759")>
        Public Sub AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
            Dim compilation = CreateCompilationWithMscorlib40({TestResource.AllInOneVisualBasicCode})
            Dim options = New AnalyzerOptions({CType(new TestAdditionalText(), AdditionalText)}.ToImmutableArray())
            ThrowingDiagnosticAnalyzer(Of SyntaxKind).VerifyAnalyzerEngineIsSafeAgainstExceptions(
                Function(analyzer) compilation.GetAnalyzerDiagnostics({analyzer}, options))
        End Sub

        <Fact>
        Public Sub AnalyzerOptionsArePassedToAllAnalyzers()
            Dim sourceText = New StringText(String.Empty, encodingOpt:=Nothing)
            Dim additionalTexts As AdditionalText() = {New TestAdditionalText("myfilepath", sourceText)}
            Dim options = New AnalyzerOptions(additionalTexts.ToImmutableArray())

            Dim compilation = CreateCompilationWithMscorlib40({TestResource.AllInOneVisualBasicCode})
            Dim analyzer = New OptionsDiagnosticAnalyzer(Of SyntaxKind)(options)
            compilation.GetAnalyzerDiagnostics({analyzer}, options)
            analyzer.VerifyAnalyzerOptions()
        End Sub
    End Class
End Namespace
