Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ''' <summary>
    ''' CA2217: Do not mark enums with FlagsAttribute
    ''' </summary>
    <ExportCodeFixProvider(EnumWithFlagsDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.VisualBasic)>
    Public Class EnumWithFlagsBasicCodeFixProvider
        Inherits EnumWithFlagsCodeFixProviderBase

        Friend Overrides Function GetUpdatedRoot(root As SyntaxNode, nodeToFix As SyntaxNode, newEnumTypeSyntax As SyntaxNode) As SyntaxNode
            If TypeOf nodeToFix Is EnumBlockSyntax Then
                If TypeOf newEnumTypeSyntax Is EnumStatementSyntax Then
                    Return MyBase.GetUpdatedRoot(root, nodeToFix, newEnumTypeSyntax.Parent)
                Else
                    Return MyBase.GetUpdatedRoot(root, nodeToFix, newEnumTypeSyntax)
                End If
            ElseIf TypeOf nodeToFix Is EnumStatementSyntax Then
                If TypeOf newEnumTypeSyntax Is EnumStatementSyntax Then
                    Return MyBase.GetUpdatedRoot(root, nodeToFix, newEnumTypeSyntax)
                Else
                    Return MyBase.GetUpdatedRoot(root, nodeToFix.Parent, newEnumTypeSyntax)
                End If
            End If

            Throw Contract.Unreachable
        End Function
    End Class
End Namespace