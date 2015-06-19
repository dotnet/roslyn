' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundNoOpStatement

        Public Sub New(syntax As VisualBasicSyntaxNode)
            MyClass.New(syntax, NoOpStatementFlavor.Default)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, hasErrors As Boolean)
            MyClass.New(syntax, NoOpStatementFlavor.Default, hasErrors)
        End Sub

    End Class

End Namespace


