' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class MergedNamespaceOrTypeDeclaration
        Inherits Declaration

        Protected Sub New(name As String)
            MyBase.New(name)
        End Sub
    End Class
End Namespace
