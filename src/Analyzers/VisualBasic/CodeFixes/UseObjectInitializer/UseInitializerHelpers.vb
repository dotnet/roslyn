' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
    End Module
End Namespace
