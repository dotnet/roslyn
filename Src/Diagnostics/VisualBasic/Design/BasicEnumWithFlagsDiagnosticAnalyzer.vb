' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ''' <summary>
    ''' 1) CA1027: Mark enums with FlagsAttribute
    ''' 2) CA2217: Do not mark enums with FlagsAttribute
    ''' </summary>
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(EnumWithFlagsDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicEnumWithFlagsDiagnosticAnalyzer
        Inherits EnumWithFlagsDiagnosticAnalyzer

        Protected Overrides Function GetDiagnosticLocation(type As SyntaxNode) As Location
            Return DirectCast(type, EnumStatementSyntax).Identifier.GetLocation()
        End Function
    End Class
End Namespace