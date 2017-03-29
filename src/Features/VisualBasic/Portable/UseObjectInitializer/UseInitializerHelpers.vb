' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
