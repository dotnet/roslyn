' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDiagnosticAnalyzerApiUsageAnalyzer
        Inherits DiagnosticAnalyzerApiUsageAnalyzer(Of TypeSyntax)

        Protected Overrides Function IsNamedTypeDeclarationBlock(syntax As SyntaxNode) As Boolean
            Select Case syntax.Kind()
                Case SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.EnumBlock, SyntaxKind.InterfaceBlock
                    Return True

                Case Else
                    Return False
            End Select
        End Function
    End Class
End Namespace

