﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Test.Utilities

Public Class BasicTrackingDiagnosticAnalyzer
    Inherits TrackingDiagnosticAnalyzer(Of SyntaxKind)
    Private Shared ReadOnly s_omittedSyntaxKindRegex As Regex = New Regex(
        "End|Exit|Empty|Imports|Option|Module|Sub|Function|Inherits|Implements|Handles|Argument|Yield|NameColonEquals|" &
        "Print|With|Label|Stop|Continue|Resume|SingleLine|Error|Clause|Forever|Re[Dd]im|Mid|Type|Cast|Exponentiate|Erase|Date|Concatenate|Like|Divide|UnaryPlus")

    Protected Overrides Function IsOnCodeBlockSupported(symbolKind As SymbolKind, methodKind As MethodKind, returnsVoid As Boolean) As Boolean
        Return MyBase.IsOnCodeBlockSupported(symbolKind, methodKind, returnsVoid) AndAlso
            methodKind <> MethodKind.Destructor AndAlso
            methodKind <> MethodKind.ExplicitInterfaceImplementation
    End Function

    Protected Overrides Function IsAnalyzeNodeSupported(syntaxKind As SyntaxKind) As Boolean
        Return MyBase.IsAnalyzeNodeSupported(syntaxKind) AndAlso Not s_omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString())
    End Function
End Class
