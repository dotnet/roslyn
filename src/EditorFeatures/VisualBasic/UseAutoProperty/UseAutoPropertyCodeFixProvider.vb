' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
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

        Protected Overrides Async Function UpdatePropertyAsync(project As Project,
                                                         compilation As Compilation,
                                                         fieldSymbol As IFieldSymbol,
                                                         propertySymbol As IPropertySymbol,
                                                         propertyDeclaration As PropertyBlockSyntax,
                                                         isWrittenToOutsideOfConstructor As Boolean,
                                                         cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim statement = propertyDeclaration.PropertyStatement
            If Not isWrittenToOutsideOfConstructor AndAlso Not propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock) Then
                Dim generator = SyntaxGenerator.GetGenerator(project)
                statement = DirectCast(generator.WithModifiers(statement, DeclarationModifiers.ReadOnly), PropertyStatementSyntax)
            End If

            Dim initializer = Await GetFieldInitializer(fieldSymbol, cancellationToken).ConfigureAwait(False)
            If initializer IsNot Nothing Then
                statement = statement.WithInitializer(SyntaxFactory.EqualsValue(initializer))
            End If

            Return statement
        End Function

        Private Async Function GetFieldInitializer(fieldSymbol As IFieldSymbol, cancellationToken As CancellationToken) As Task(Of ExpressionSyntax)
            Dim identifier = TryCast(Await fieldSymbol.DeclaringSyntaxReferences(0).GetSyntaxAsync(cancellationToken).ConfigureAwait(False), ModifiedIdentifierSyntax)
            Dim declarator = TryCast(identifier?.Parent, VariableDeclaratorSyntax)
            Return declarator?.Initializer?.Value
        End Function
    End Class
End Namespace