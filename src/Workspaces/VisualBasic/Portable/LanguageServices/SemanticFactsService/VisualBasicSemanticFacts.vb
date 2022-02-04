' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class VisualBasicSemanticFacts
        Implements ISemanticFacts

        Public Function GenerateNameForExpression(semanticModel As SemanticModel,
                                                  expression As SyntaxNode,
                                                  capitalize As Boolean,
                                                  cancellationToken As CancellationToken) As String Implements ISemanticFacts.GenerateNameForExpression
            Return semanticModel.GenerateNameForExpression(
                DirectCast(expression, ExpressionSyntax), capitalize, cancellationToken)
        End Function
    End Class
End Namespace
