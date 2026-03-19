' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExtractMethod

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Friend Class VisualBasicSyntaxTriviaService
        Inherits AbstractSyntaxTriviaService

        Public Shared ReadOnly Instance As New VisualBasicSyntaxTriviaService

        Private Sub New()
            MyBase.New(SyntaxKind.EndOfLineTrivia)
        End Sub
    End Class
End Namespace
