' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class ArgumentSyntax
        Public Function GetExpression() As ExpressionSyntax
            Select Case Kind
                Case SyntaxKind.OmittedArgument
                    Return SyntaxFactory.MissingExpression

                Case SyntaxKind.RangeArgument
                    Return DirectCast(Me, RangeArgumentSyntax).UpperBound
            End Select

            ' The code could check for NodeKind.SimpleArgument and
            ' throw if the kind did not match but the directcast
            ' will throw if this isn't a simple argument so why
            ' add the check?

            Return DirectCast(Me, SimpleArgumentSyntax).Expression
        End Function
    End Class
End Namespace
