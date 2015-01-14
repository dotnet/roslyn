' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicEscapingReducer
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New(optionSet, cancellationToken)
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                CancellationToken.ThrowIfCancellationRequested()

                Return SimplifyToken(
                    token,
                    newToken:=MyBase.VisitToken(token),
                    simplifyFunc:=AddressOf TryUnescapeToken)
            End Function

        End Class
    End Class
End Namespace
