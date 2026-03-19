' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundNoOpStatement

        Public Sub New(syntax As SyntaxNode)
            MyClass.New(syntax, NoOpStatementFlavor.Default)
        End Sub

        Public Sub New(syntax As SyntaxNode, hasErrors As Boolean)
            MyClass.New(syntax, NoOpStatementFlavor.Default, hasErrors)
        End Sub

    End Class

End Namespace

