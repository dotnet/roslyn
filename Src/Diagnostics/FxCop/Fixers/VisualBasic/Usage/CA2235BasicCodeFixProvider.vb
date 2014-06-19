Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <ExportCodeFixProvider("CA2229 CodeFix provider", LanguageNames.VisualBasic)>
    Public Class CA2235BasicCodeFixProvider
        Inherits CA2235CodeFixProviderBase

        Protected Overrides Function GetFieldDeclarationNode(node As SyntaxNode) As SyntaxNode
            Dim fieldNode = node
            While fieldNode IsNot Nothing AndAlso fieldNode.VisualBasicKind() <> VisualBasic.SyntaxKind.FieldDeclaration
                fieldNode = fieldNode.Parent
            End While

            Return If(fieldNode.VisualBasicKind() = VisualBasic.SyntaxKind.FieldDeclaration, fieldNode, Nothing)
        End Function
    End Class
End Namespace