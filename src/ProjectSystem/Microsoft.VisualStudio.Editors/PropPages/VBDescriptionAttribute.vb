' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    '''  a sub class of DescriptionAttribute to help localizating the description...
    ''' </Summary>
    <AttributeUsage(AttributeTargets.All)> _
    Friend Class VBDescriptionAttribute
        Inherits DescriptionAttribute

        Private _replaced As Boolean

        Public Sub New(ByVal description As String)
            MyBase.New(description)
        End Sub

        Public Overrides ReadOnly Property Description() As String
            Get
                If Not _replaced Then
                    _replaced = True
                    DescriptionValue = SR.ResourceManager.GetString(MyBase.DescriptionValue)
                End If
                Return DescriptionValue
            End Get
        End Property
    End Class

End Namespace

