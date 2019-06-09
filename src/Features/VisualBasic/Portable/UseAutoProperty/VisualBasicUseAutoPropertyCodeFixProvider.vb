' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicUseAutoPropertyCodeFixProvider)), [Shared]>
    Friend Class VisualBasicUseAutoPropertyCodeFixProvider
        Inherits AbstractUseAutoPropertyCodeFixProvider(Of TypeBlockSyntax, PropertyBlockSyntax, ModifiedIdentifierSyntax, ConstructorBlockSyntax, ExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetNodeToRemove(identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return Utilities.GetNodeToRemove(identifier)
        End Function

        Protected Overrides Function GetFormattingRules(document As Document) As IEnumerable(Of AbstractFormattingRule)
            Return Nothing
        End Function

        Protected Overrides Async Function UpdatePropertyAsync(propertyDocument As Document,
                                                         compilation As Compilation,
                                                         fieldSymbol As IFieldSymbol,
                                                         propertySymbol As IPropertySymbol,
                                                         propertyDeclaration As PropertyBlockSyntax,
                                                         isWrittenToOutsideOfConstructor As Boolean,
                                                         cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim statement = propertyDeclaration.PropertyStatement

            Dim generator = SyntaxGenerator.GetGenerator(propertyDocument.Project)
            Dim canBeReadOnly = Not isWrittenToOutsideOfConstructor AndAlso Not propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock)

            statement = DirectCast(generator.WithModifiers(statement, generator.GetModifiers(propertyDeclaration).WithIsReadOnly(canBeReadOnly)), PropertyStatementSyntax)

            Dim initializer = Await GetFieldInitializer(fieldSymbol, cancellationToken).ConfigureAwait(False)
            If initializer.equalsValue IsNot Nothing Then
                statement = statement.WithTrailingTrivia(SyntaxFactory.Space) _
                    .WithInitializer(initializer.equalsValue) _
                    .WithTrailingTrivia(statement.GetTrailingTrivia.Where(Function(x) x.Kind <> SyntaxKind.EndOfLineTrivia)) _
                    .WithAppendedTrailingTrivia(initializer.equalsValue.GetTrailingTrivia())
            End If

            If initializer.asNewClause IsNot Nothing Then
                statement = statement.WithAsClause(initializer.asNewClause)
            End If

            Return statement
        End Function

        Private Async Function GetFieldInitializer(fieldSymbol As IFieldSymbol, cancellationToken As CancellationToken) As Task(Of (equalsValue As EqualsValueSyntax, asNewClause As AsNewClauseSyntax))
            Dim identifier = TryCast(Await fieldSymbol.DeclaringSyntaxReferences(0).GetSyntaxAsync(cancellationToken).ConfigureAwait(False), ModifiedIdentifierSyntax)
            Dim declarator = TryCast(identifier?.Parent, VariableDeclaratorSyntax)
            Dim initializer = declarator?.Initializer

            ' We are only interested in the AsClause if it's being used as an initializer:
            '  Dim x As String -- no need to preserve the clause since it will already be the same on the property
            '  Dim x As New Guid("...") -- need to preserve the clause since it's being used as an initializer
            Dim asNewClause = TryCast(declarator.AsClause, AsNewClauseSyntax)
            Return (initializer, asNewClause)
        End Function
    End Class
End Namespace
