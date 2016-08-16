' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class SyntaxListBuilder
        Inherits AbstractSyntaxListBuilder

        Friend Sub New(size As Integer)
            MyBase.New(size)
        End Sub

        Friend Shadows Function Any(kind As SyntaxKind) As Boolean
            Return MyBase.Any(kind)
        End Function

        Friend Sub RemoveLast()
            Me.Count -= 1
            Me.Nodes(Count) = Nothing
        End Sub
    End Class
End Namespace