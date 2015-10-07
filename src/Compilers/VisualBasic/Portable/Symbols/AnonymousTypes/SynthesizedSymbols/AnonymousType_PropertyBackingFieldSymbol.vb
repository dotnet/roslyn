' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousTypePropertyBackingFieldSymbol
            Inherits SynthesizedBackingFieldBase(Of PropertySymbol)

            Public Sub New([property] As PropertySymbol)
                MyBase.New([property], "$" & [property].Name, isShared:=False)
            End Sub

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return Me._propertyOrEvent.IsReadOnly
                End Get
            End Property

            Public Overrides ReadOnly Property MetadataName As String
                Get
                    ' To be sure that when we emitting the name, it's 
                    ' casing is in sync with that of the property
                    Return "$" & Me._propertyOrEvent.Name
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return _propertyOrEvent.Type
                End Get
            End Property
        End Class
    End Class
End Namespace
