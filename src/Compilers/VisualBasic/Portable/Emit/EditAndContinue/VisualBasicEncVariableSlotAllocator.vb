' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend Class VisualBasicLambdaSyntaxHelper
        Implements ILambdaSyntaxHelper

        Public Shared ReadOnly Instance As ILambdaSyntaxHelper = New VisualBasicLambdaSyntaxHelper()

        Private Sub New()
        End Sub

        Public Function GetLambda(lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode Implements ILambdaSyntaxHelper.GetLambda
            Return LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax)
        End Function

        Public Function TryGetCorrespondingLambdaBody(previousLambdaSyntax As SyntaxNode, lambdaOrLambdaBodySyntax As SyntaxNode) As SyntaxNode Implements ILambdaSyntaxHelper.TryGetCorrespondingLambdaBody
            Return LambdaUtilities.GetCorrespondingLambdaBody(lambdaOrLambdaBodySyntax, previousLambdaSyntax)
        End Function
    End Class
End Namespace