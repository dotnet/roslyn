﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertAnonymousTypeToTupleCodeFixProvider)), [Shared]>
    Friend Class VisualBasicConvertAnonymousTypeToTupleCodeFixProvider
        Inherits AbstractConvertAnonymousTypeToTupleCodeFixProvider(Of
            ExpressionSyntax,
            TupleExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function ConvertToTuple(anonCreation As AnonymousObjectCreationExpressionSyntax) As TupleExpressionSyntax
            Return SyntaxFactory.TupleExpression(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTriviaFrom(anonCreation.Initializer.OpenBraceToken),
                ConvertInitializers(anonCreation.Initializer.Initializers),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(anonCreation.Initializer.CloseBraceToken)).
                    WithPrependedLeadingTrivia(anonCreation.GetLeadingTrivia())
        End Function

        Private Function ConvertInitializers(initializers As SeparatedSyntaxList(Of FieldInitializerSyntax)) As SeparatedSyntaxList(Of SimpleArgumentSyntax)
            Return SyntaxFactory.SeparatedList(initializers.Select(AddressOf ConvertInitializer), initializers.GetSeparators())
        End Function

        Private Function ConvertInitializer(field As FieldInitializerSyntax) As SimpleArgumentSyntax
            Return SyntaxFactory.SimpleArgument(
                GetNameEquals(field),
                GetExpression(field)).WithTriviaFrom(field)
        End Function

        Private Function GetNameEquals(field As FieldInitializerSyntax) As NameColonEqualsSyntax
            Dim namedField = TryCast(field, NamedFieldInitializerSyntax)
            If namedField Is Nothing Then
                Return Nothing
            End If

            Return SyntaxFactory.NameColonEquals(
                namedField.Name,
                SyntaxFactory.Token(SyntaxKind.ColonEqualsToken).WithTriviaFrom(namedField.EqualsToken))
        End Function

        Private Function GetExpression(field As FieldInitializerSyntax) As ExpressionSyntax
            Return If(TryCast(field, InferredFieldInitializerSyntax)?.Expression,
                      TryCast(field, NamedFieldInitializerSyntax)?.Expression)
        End Function
    End Class
End Namespace
