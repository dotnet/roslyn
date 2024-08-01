' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseAutoProperty), [Shared]>
    Friend NotInheritable Class VisualBasicUseAutoPropertyCodeFixProvider
        Inherits AbstractUseAutoPropertyCodeFixProvider(Of TypeBlockSyntax, PropertyBlockSyntax, ModifiedIdentifierSyntax, ConstructorBlockSyntax, ExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetPropertyDeclaration(node As SyntaxNode) As PropertyBlockSyntax
            If TypeOf node Is PropertyStatementSyntax Then
                node = node.Parent
            End If

            Return DirectCast(node, PropertyBlockSyntax)
        End Function

        Protected Overrides Function GetNodeToRemove(identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return Utilities.GetNodeToRemove(identifier)
        End Function

        Protected Overrides Function GetFormattingRules(document As Document, node As PropertyBlockSyntax) As ImmutableArray(Of AbstractFormattingRule)
            Return Nothing
        End Function

        Protected Overrides Function RewriteFieldReferencesInProperty([property] As PropertyBlockSyntax, fieldLocations As LightweightRenameLocations, cancellationToken As CancellationToken) As PropertyBlockSyntax
            ' Only called to rewrite to `field` (which VB does not support).
            Return [property]
        End Function

        Protected Overrides Async Function UpdatePropertyAsync(
                propertyDocument As Document,
                compilation As Compilation,
                fieldSymbol As IFieldSymbol,
                propertySymbol As IPropertySymbol,
                propertyDeclaration As PropertyBlockSyntax,
                isWrittenToOutsideOfConstructor As Boolean,
                isTrivialGetAccessor As Boolean,
                isTrivialSetAccessor As Boolean,
                cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim statement = propertyDeclaration.PropertyStatement

            Dim generator = SyntaxGenerator.GetGenerator(propertyDocument.Project)
            Dim canBeReadOnly = Not isWrittenToOutsideOfConstructor AndAlso Not propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock)

            statement = DirectCast(generator.WithModifiers(statement, generator.GetModifiers(propertyDeclaration).WithIsReadOnly(canBeReadOnly)), PropertyStatementSyntax)

            Dim initializer = Await GetFieldInitializerAsync(fieldSymbol, cancellationToken).ConfigureAwait(False)
            If initializer.equalsValue IsNot Nothing Then
                statement = statement.WithTrailingTrivia(SyntaxFactory.Space) _
                    .WithInitializer(initializer.equalsValue) _
                    .WithTrailingTrivia(statement.GetTrailingTrivia.Where(Function(x) x.Kind <> SyntaxKind.EndOfLineTrivia)) _
                    .WithAppendedTrailingTrivia(initializer.equalsValue.GetTrailingTrivia())
            End If

            If initializer.asNewClause IsNot Nothing Then
                statement = statement.WithAsClause(initializer.asNewClause)
            End If

            If initializer.arrayBounds IsNot Nothing Then
                Dim semanticsModel = Await propertyDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                Dim arrayType = TryCast(semanticsModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken).Type, IArrayTypeSymbol)
                If arrayType IsNot Nothing Then
                    Dim arrayCreation = SyntaxFactory.ArrayCreationExpression(
                    Nothing,
                    arrayType.ElementType.GenerateTypeSyntax(),
                    initializer.arrayBounds,
                    If(TryCast(initializer.equalsValue?.Value, CollectionInitializerSyntax), SyntaxFactory.CollectionInitializer()))
                    statement = statement.WithTrailingTrivia(SyntaxFactory.Space).
                                      WithInitializer(SyntaxFactory.EqualsValue(arrayCreation))
                End If
            End If

            Return statement
        End Function

        Private Shared Async Function GetFieldInitializerAsync(fieldSymbol As IFieldSymbol, cancellationToken As CancellationToken) As Task(Of (equalsValue As EqualsValueSyntax, asNewClause As AsNewClauseSyntax, arrayBounds As ArgumentListSyntax))
            Dim identifier = TryCast(Await fieldSymbol.DeclaringSyntaxReferences(0).GetSyntaxAsync(cancellationToken).ConfigureAwait(False), ModifiedIdentifierSyntax)
            Dim declarator = TryCast(identifier?.Parent, VariableDeclaratorSyntax)
            Dim initializer = declarator?.Initializer
            Dim arrayBounds = identifier?.ArrayBounds

            ' We are only interested in the AsClause if it's being used as an initializer:
            '  Dim x As String -- no need to preserve the clause since it will already be the same on the property
            '  Dim x As New Guid("...") -- need to preserve the clause since it's being used as an initializer
            Dim asNewClause = TryCast(declarator.AsClause, AsNewClauseSyntax)
            Return (initializer, asNewClause, arrayBounds)
        End Function
    End Class
End Namespace
