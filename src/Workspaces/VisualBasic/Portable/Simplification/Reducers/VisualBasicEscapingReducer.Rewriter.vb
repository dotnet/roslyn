' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
