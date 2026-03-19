' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' This is a SyntaxReference implementation that lazily finds the beginning of the block (if any) of the original syntax reference
    ''' </summary>
    Friend Class BeginOfBlockSyntaxReference
        Inherits TranslationSyntaxReference

        Public Sub New(reference As SyntaxReference)
            MyBase.New(reference)
        End Sub

        Protected Overrides Function Translate(reference As SyntaxReference, cancellationToken As CancellationToken) As SyntaxNode
            Return SyntaxFacts.BeginOfBlockStatementIfAny(reference.GetSyntax(cancellationToken))
        End Function
    End Class
End Namespace
