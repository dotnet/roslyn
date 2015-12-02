Imports System.Diagnostics
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

        Private m_PropertyType As System.Type
        Private m_IsReadOnly As Boolean

        Public Sub New(ByVal Name As String, ByVal PropertyType As System.Type)
            MyBase.New(Name, New System.Attribute() {})
            m_PropertyType = PropertyType
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
                Return m_IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property PropertyType() As System.Type
            Get
                Return m_PropertyType
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

