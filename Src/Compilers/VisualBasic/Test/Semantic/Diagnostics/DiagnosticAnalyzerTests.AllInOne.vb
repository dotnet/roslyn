' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        <WorkItem(897137)>
        <Fact>
        Public Sub DiagnosticAnalyzerAllInOne()
            Dim source = TestResource.AllInOneVisualBasicBaseline
            Dim analyzer = New BasicTrackingDiagnosticAnalyzer()
            CreateCompilationWithMscorlib({source}).VerifyAnalyzerDiagnostics({analyzer})
            analyzer.VerifyAllInterfaceMembersWereCalled()
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds()
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds()
            analyzer.VerifyAnalyzeCodeBlockCalledForAllSymbolAndMethodKinds()
        End Sub

        <WorkItem(896273)>
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
        Public Sub AnalyzerDriverIsSafeAgainstAnalyzerExceptions()
            Dim compilation = CreateCompilationWithMscorlib({TestResource.AllInOneVisualBasicCode})
            ThrowingDiagnosticAnalyzer(Of SyntaxKind).VerifyAnalyzerEngineIsSafeAgainstExceptions(compilation)
        End Sub
    End Class

    Class BasicTrackingDiagnosticAnalyzer
        Inherits TrackingDiagnosticAnalyzer(Of SyntaxKind)
        Shared ReadOnly omittedSyntaxKindRegex As Regex = New Regex(
            "End|Exit|Empty|Imports|Option|Module|Sub|Function|Inherits|Implements|Handles|Argument|Yield|" +
            "Print|With|Label|Stop|Continue|Resume|SingleLine|Error|Clause|Forever|Re[Dd]im|Mid|Type|Cast|Exponentiate|Erase|Date|Concatenate|Like|Divide|UnaryPlus")

        Protected Overrides Function IsAnalyzeCodeBlockSupported(symbolKind As SymbolKind, methodKind As MethodKind, returnsVoid As Boolean) As Boolean
            Return symbolKind <> SymbolKind.Event AndAlso
                methodKind <> MethodKind.Destructor AndAlso methodKind <> MethodKind.ExplicitInterfaceImplementation
        End Function

        Protected Overrides Function IsAnalyzeNodeSupported(syntaxKind As SyntaxKind) As Boolean
            Return MyBase.IsAnalyzeNodeSupported(syntaxKind) AndAlso Not omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString())
        End Function
    End Class
End Namespace
