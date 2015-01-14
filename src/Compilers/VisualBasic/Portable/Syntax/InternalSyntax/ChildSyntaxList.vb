' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Structure ChildSyntaxList
        Private ReadOnly _node As VisualBasicSyntaxNode

        Friend Sub New(node As VisualBasicSyntaxNode)
            _node = node
        End Sub

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(_node)
        End Function
    End Structure
End Namespace
