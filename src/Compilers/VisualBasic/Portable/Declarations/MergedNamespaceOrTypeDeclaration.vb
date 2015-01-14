' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class MergedNamespaceOrTypeDeclaration
        Inherits Declaration

        Protected Sub New(name As String)
            MyBase.New(name)
        End Sub
    End Class
End Namespace
