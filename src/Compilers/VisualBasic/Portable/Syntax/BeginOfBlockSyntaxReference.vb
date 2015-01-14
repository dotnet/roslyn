' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Return SyntaxFacts.BeginOfBlockStatementIfAny(reference.GetSyntax())
        End Function
    End Class
End Namespace
