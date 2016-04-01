' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    '''  a sub class of DisplayNameAttribute to help localizating the property name...
    ''' </Summary>
    <AttributeUsage(AttributeTargets.All)> _
    Friend Class VBDisplayNameAttribute
        Inherits DisplayNameAttribute

        Private _replaced As Boolean

        Public Sub New(ByVal description As String)
            MyBase.New(description)
        End Sub

        Public Overrides ReadOnly Property DisplayName() As String
            Get
                If Not _replaced Then
                    _replaced = True
                    DisplayNameValue = SR.ResourceManager.GetString(MyBase.DisplayNameValue)
                End If
                Return DisplayNameValue
            End Get
        End Property
    End Class

End Namespace

