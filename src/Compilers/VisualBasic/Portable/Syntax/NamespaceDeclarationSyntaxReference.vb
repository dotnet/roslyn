' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' this is a SyntaxReference implementation that lazily translates the result (SyntaxNode) of the original syntax reference
    ''' to other one.
    ''' </summary>
    Friend Class NamespaceDeclarationSyntaxReference
        Inherits TranslationSyntaxReference

        Public Sub New(reference As SyntaxReference)
            MyBase.New(reference)
        End Sub

        Protected Overrides Function Translate(reference As SyntaxReference, cancellationToken As CancellationToken) As SyntaxNode
            Dim node = reference.GetSyntax(cancellationToken)

            ' If the node is a name syntax, it's something like "X" or "X.Y" in :
            '    Namespace X.Y.Z
            ' We want to return the full NamespaceStatementSyntax.
            While TypeOf node Is NameSyntax
                node = node.Parent
            End While

            Debug.Assert(TypeOf node Is CompilationUnitSyntax OrElse TypeOf node Is NamespaceStatementSyntax)
            Return node
        End Function
    End Class
End Namespace
