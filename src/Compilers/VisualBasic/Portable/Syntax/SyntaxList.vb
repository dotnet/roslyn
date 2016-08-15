' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Friend MustInherit Class SyntaxList
        Inherits VisualBasicSyntaxNode

        Friend Sub New(green As InternalSyntax.VisualBasicSyntaxNode, parent As SyntaxNode, position As Integer)
            MyBase.New(green, parent, position)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSyntaxVisitor(Of TResult)) As TResult
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSyntaxVisitor)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace