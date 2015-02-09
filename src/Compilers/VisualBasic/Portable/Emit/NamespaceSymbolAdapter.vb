' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
