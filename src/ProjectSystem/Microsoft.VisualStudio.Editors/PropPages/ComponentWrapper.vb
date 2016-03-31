Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    ''' A warp class to warp a Common object to a component, but still keep the right properties set to show on the property Grid
    ''' </Summary>
    Friend Class ComponentWrapper
        Inherits Component
        Implements ICustomTypeDescriptor

        Private m_currentObject As Object

        Protected Sub New(ByVal realObject As Object)
            m_currentObject = realObject
        End Sub

        ''' <Summary>
        ''' the original object
        ''' </Summary>
        Protected Friend Property CurrentObject() As Object
            Get
                Return m_currentObject
            End Get
            Set(ByVal value As Object)
                Debug.Assert(value IsNot Nothing, "can not support Nothing")
                m_currentObject = value
            End Set
        End Property

        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(m_currentObject)
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return TypeDescriptor.GetClassName(m_currentObject)
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return TypeDescriptor.GetComponentName(m_currentObject)
        End Function

        Public Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter(m_currentObject)
        End Function

        Public Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent(m_currentObject)
        End Function

        Public Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty(m_currentObject)
        End Function

        Public Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor(m_currentObject, editorBaseType)
        End Function

        Public Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(m_currentObject)
        End Function

        Public Function GetEvents1(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(m_currentObject, attributes)
        End Function

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(m_currentObject)
        End Function

        Public Function GetProperties1(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(m_currentObject, attributes)
        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return m_currentObject
        End Function
    End Class

End Namespace

