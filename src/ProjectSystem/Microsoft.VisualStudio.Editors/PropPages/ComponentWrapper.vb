' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    ''' A warp class to warp a Common object to a component, but still keep the right properties set to show on the property Grid
    ''' </Summary>
    Friend Class ComponentWrapper
        Inherits Component
        Implements ICustomTypeDescriptor

        Private _currentObject As Object

        Protected Sub New(ByVal realObject As Object)
            _currentObject = realObject
        End Sub

        ''' <Summary>
        ''' the original object
        ''' </Summary>
        Protected Friend Property CurrentObject() As Object
            Get
                Return _currentObject
            End Get
            Set(ByVal value As Object)
                Debug.Assert(value IsNot Nothing, "can not support Nothing")
                _currentObject = value
            End Set
        End Property

        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(_currentObject)
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return TypeDescriptor.GetClassName(_currentObject)
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return TypeDescriptor.GetComponentName(_currentObject)
        End Function

        Public Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter(_currentObject)
        End Function

        Public Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent(_currentObject)
        End Function

        Public Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty(_currentObject)
        End Function

        Public Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor(_currentObject, editorBaseType)
        End Function

        Public Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(_currentObject)
        End Function

        Public Function GetEvents1(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(_currentObject, attributes)
        End Function

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(_currentObject)
        End Function

        Public Function GetProperties1(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(_currentObject, attributes)
        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return _currentObject
        End Function
    End Class

End Namespace

