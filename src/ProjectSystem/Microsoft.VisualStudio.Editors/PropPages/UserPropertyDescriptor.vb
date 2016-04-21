' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel


Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A very simple property descriptor class for user-defined properties handled directly by the page 
    '''   (PropertyControlData.IsUserPersisted = True).
    ''' Should be returned by an overriden GetUserDefinedPropertyDescriptor.
    ''' This is used mainly in integrating with the Undo/Redo capabilities.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class UserPropertyDescriptor
        Inherits PropertyDescriptor

        Private _propertyType As System.Type
        Private _isReadOnly As Boolean

        Public Sub New(ByVal Name As String, ByVal PropertyType As System.Type)
            MyBase.New(Name, New System.Attribute() {})
            _propertyType = PropertyType
        End Sub

        Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
            Return False
        End Function

        Public Overrides ReadOnly Property ComponentType() As System.Type
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetValue(ByVal component As Object) As Object
            'Note: this function never gets called and does not need to be implemented (the call is
            '  intercepted by the project designer)
            Debug.Fail("This should not get called")
            Return Nothing
        End Function

        Public Overrides ReadOnly Property IsReadOnly() As Boolean
            Get
                Return _isReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property PropertyType() As System.Type
            Get
                Return _propertyType
            End Get
        End Property

        Public Overrides Sub ResetValue(ByVal component As Object)
        End Sub

        Public Overrides Sub SetValue(ByVal component As Object, ByVal value As Object)
            'Note: this function never gets called and does not need to be implemented (the call is
            '  intercepted by the project designer)
            Debug.Fail("This should not get called")
        End Sub

        Public Overrides Function ShouldSerializeValue(ByVal component As Object) As Boolean
            Return True
        End Function

    End Class

End Namespace

