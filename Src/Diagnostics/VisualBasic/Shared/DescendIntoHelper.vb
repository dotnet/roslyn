' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopRules.DiagnosticProviders.Utilities
    Friend Class DescendIntoHelper
        ' determines if the search descends only into the node's type-level children.
        Friend Shared Function DescendIntoOnlyTypeLevelDeclaration() As Func(Of SyntaxNode, Boolean)
            Return Function(n)
                       Return (Not n.IsKind(SyntaxKind.ConstructorBlock)) AndAlso
                              (Not n.IsKind(SyntaxKind.SubBlock)) AndAlso
                              (Not n.IsKind(SyntaxKind.FunctionBlock))
                   End Function
        End Function
    End Class
End Namespace