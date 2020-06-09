' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend MustInherit Class NamespaceSymbol
        Implements Cci.INamespace

        Private ReadOnly Property INamedEntity_Name As String Implements INamedEntity.Name
            Get
                Return Me.MetadataName
            End Get
        End Property

        Private ReadOnly Property INamespaceSymbol_ContainingNamespace As Cci.INamespace Implements Cci.INamespace.ContainingNamespace
            Get
                Return Me.ContainingNamespace
            End Get
        End Property
    End Class
End Namespace
