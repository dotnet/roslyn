Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty
    <[Shared]>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(UseAutoPropertyCodeFixProvider))>
    Friend Class UseAutoPropertyCodeFixProvider
        Inherits AbstractUseAutoPropertyCodeFixProvider(Of PropertyBlockSyntax, FieldDeclarationSyntax, ModifiedIdentifierSyntax, ConstructorBlockSyntax, ExpressionSyntax)

        Protected Overrides Function GetNodeToRemove(identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return Utilities.GetNodeToRemove(identifier)
        End Function

        Protected Overrides Function UpdateProperty(project As Project,
                                                    compilation As Compilation,
                                                    fieldSymbol As IFieldSymbol,
                                                    propertySymbol As IPropertySymbol,
                                                    propertyDeclaration As PropertyBlockSyntax,
                                                    isWrittenToOutsideOfConstructor As Boolean,
                                                    cancellationToken As CancellationToken) As SyntaxNode
            Dim statement = propertyDeclaration.PropertyStatement
            If Not isWrittenToOutsideOfConstructor AndAlso Not propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock) Then
                Dim generator = SyntaxGenerator.GetGenerator(project)
                statement = DirectCast(generator.WithModifiers(statement, DeclarationModifiers.ReadOnly), PropertyStatementSyntax)
            End If
            Return statement
        End Function
    End Class
End Namespace