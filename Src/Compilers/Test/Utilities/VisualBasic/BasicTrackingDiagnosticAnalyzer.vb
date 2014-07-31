Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Test.Utilities

Public Class BasicTrackingDiagnosticAnalyzer
    Inherits TrackingDiagnosticAnalyzer(Of SyntaxKind)
    Shared ReadOnly omittedSyntaxKindRegex As Regex = New Regex(
        "End|Exit|Empty|Imports|Option|Module|Sub|Function|Inherits|Implements|Handles|Argument|Yield|" +
        "Print|With|Label|Stop|Continue|Resume|SingleLine|Error|Clause|Forever|Re[Dd]im|Mid|Type|Cast|Exponentiate|Erase|Date|Concatenate|Like|Divide|UnaryPlus")

    Protected Overrides Function IsOnCodeBlockSupported(symbolKind As SymbolKind, methodKind As MethodKind, returnsVoid As Boolean) As Boolean
        Return MyBase.IsOnCodeBlockSupported(symbolKind, methodKind, returnsVoid) AndAlso
            methodKind <> methodKind.Destructor AndAlso
            methodKind <> methodKind.ExplicitInterfaceImplementation
    End Function

    Protected Overrides Function IsAnalyzeNodeSupported(syntaxKind As SyntaxKind) As Boolean
        Return MyBase.IsAnalyzeNodeSupported(syntaxKind) AndAlso Not omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString())
    End Function
End Class
