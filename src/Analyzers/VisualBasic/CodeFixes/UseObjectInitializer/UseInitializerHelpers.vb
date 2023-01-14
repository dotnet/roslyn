' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    Friend Module UseInitializerHelpers
        Public Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                initializer As ObjectCreationInitializerSyntax) As ObjectCreationExpressionSyntax

            If objectCreation.ArgumentList IsNot Nothing AndAlso
               objectCreation.ArgumentList.Arguments.Count = 0 Then

                objectCreation = objectCreation.WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia())).
                                                WithArgumentList(Nothing)
            End If

            Return objectCreation.WithoutTrailingTrivia().
                                  WithInitializer(initializer).
                                  WithTrailingTrivia(objectCreation.GetTrailingTrivia())
        End Function

        Public Sub AddExistingItems(objectCreation As ObjectCreationExpressionSyntax, nodesAndTokens As ArrayBuilder(Of SyntaxNodeOrToken))
            If TypeOf objectCreation.Initializer Is ObjectMemberInitializerSyntax Then
                Dim memberInitializer = DirectCast(objectCreation.Initializer, ObjectMemberInitializerSyntax)
                nodesAndTokens.AddRange(memberInitializer.Initializers.GetWithSeparators())
            ElseIf TypeOf objectCreation.Initializer Is ObjectCOllectioninitializersyntax Then
                Dim collectionInitializer = DirectCast(objectCreation.Initializer, ObjectCollectionInitializerSyntax)
                nodesAndTokens.AddRange(collectionInitializer.Initializer.Initializers.GetWithSeparators())
            End If

            ' If we have an odd number of elements already, add a comma at the end so that we can add the rest of the
            ' items afterwards without a syntax issue.
            If nodesAndTokens.Count Mod 2 = 1 Then
                Dim last = nodesAndTokens.Last()
                nodesAndTokens.RemoveLast()
                nodesAndTokens.Add(last.WithTrailingTrivia())
                nodesAndTokens.Add(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(last.GetTrailingTrivia()))
            End If
        End Sub
    End Module
End Namespace
