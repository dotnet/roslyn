' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicEscapingReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
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
