' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification.Simplifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend MustInherit Class AbstractVisualBasicSimplifier(Of TSyntax As SyntaxNode, TSimplifiedSyntax As SyntaxNode)
        Inherits AbstractSimplifier(Of TSyntax, TSimplifiedSyntax)

    End Class
End Namespace
