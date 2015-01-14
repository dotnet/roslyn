' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler generated backing field for an automatically implemented property.
    ''' </summary>
    Friend Class SynthesizedPropertyBackingFieldSymbol
        Inherits SynthesizedBackingFieldBase(Of SourcePropertySymbol)

        Public Sub New([property] As SourcePropertySymbol, name As String, isShared As Boolean)
            MyBase.New([property], name, isShared)
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _propertyOrEvent.Type
            End Get
        End Property
    End Class
End Namespace
