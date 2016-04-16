' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Partial Class ArgumentSyntax
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
