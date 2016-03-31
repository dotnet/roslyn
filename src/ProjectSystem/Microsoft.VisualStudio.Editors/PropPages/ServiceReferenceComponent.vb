Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.WCFReference.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    ''' This is the class we wrapped a WCF service reference and pushed to the propertyGrid
    ''' </Summary>
    Friend Class ServiceReferenceComponent
        Inherits Component
        Implements ICustomTypeDescriptor, IReferenceComponent, IUpdatableReferenceComponent

        Private m_Collection As IVsWCFReferenceGroupCollection
        Private m_ReferenceGroup As IVsWCFReferenceGroup

        Sub New(ByVal collection As IVsWCFReferenceGroupCollection, ByVal referenceGroup As IVsWCFReferenceGroup)
            m_Collection = collection
            m_ReferenceGroup = referenceGroup
        End Sub

        <VBDescription(My.Resources.Designer.ConstantResourceIDs.PPG_ServiceReferenceNamespaceDescription)> _
        <MergablePropertyAttribute(False)> _
        <HelpKeyword("ServiceReference Properties.Namespace")> _
        Public Property [Namespace]() As String
            Get
                Return m_ReferenceGroup.GetNamespace()
            End Get
            Set(ByVal value As String)
                m_ReferenceGroup.SetNamespace(value)
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
        Private Function ShouldSerializeNamespace() As Boolean
            Return False
        End Function

        <VBDisplayNameAttribute(My.Resources.Designer.ConstantResourceIDs.PPG_ServiceReferenceUrlName)> _
        <VBDescription(My.Resources.Designer.ConstantResourceIDs.PPG_ServiceReferenceUrlDescription)> _
        <HelpKeyword("ServiceReference Properties.ServiceReferenceURL")> _
        <MergablePropertyAttribute(False)> _
        Public Property ServiceReferenceURL() As String
            Get
                If m_ReferenceGroup.GetReferenceCount() = 1 Then
                    Return m_ReferenceGroup.GetReferenceUrl(0)
                ElseIf m_ReferenceGroup.GetReferenceCount() > 1 Then
                    Return SR.GetString(SR.CSRDlg_MultipleURL)
                Else
                    Return ""
                End If
                Return String.Empty
            End Get
            Set(ByVal value As String)
                value = value.Trim()
                Dim currentCount As Integer = m_ReferenceGroup.GetReferenceCount()
                If currentCount = 1 Then
                    If value <> "" Then
                        Dim currentUrl As String = m_ReferenceGroup.GetReferenceUrl(0)
                        m_ReferenceGroup.SetReferenceUrl(0, value)
                        Try
                            m_ReferenceGroup.Update(Nothing)
                        Catch ex As Exception
                            m_ReferenceGroup.SetReferenceUrl(0, currentUrl)
                            Throw ex
                        End Try
                    Else
                        Throw New ArgumentException(SR.PPG_ServiceReferenceProperty_SetReferenceUrlEmpty)
                    End If
                ElseIf currentCount > 1 Then
                    Throw New NotSupportedException(SR.PPG_ServiceReferenceProperty_MultipleUrlNotSupported)
                Else
                    If value <> "" Then
                        m_ReferenceGroup.AddReference(Nothing, value)
                    ENd If
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
        Private Function ShouldSerializeServiceReferenceURL() As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Service reference instance
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property ReferenceGroup() As IVsWCFReferenceGroup
            Get
                Return m_ReferenceGroup
            End Get
        End Property

        ''' <summary>
        ''' Remove the service reference...
        ''' </summary>
        ''' <return></return>
        ''' <remarks></remarks>
        Private Sub Remove() Implements IReferenceComponent.Remove
            m_Collection.Remove(m_ReferenceGroup)
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return [Namespace]
        End Function

        '''<summary>
        ''' Update the web reference
        '''</summary>
        Private Sub Update() Implements IUpdatableReferenceComponent.Update
            m_ReferenceGroup.Update(Nothing)
        End Sub

#Region "System.ComponentModel.ICustomTypeDescriptor"
        ' we overrite the ICustomTypeDescriptor to replace the ClassName and ComponentName which are shown on the propertyGrid
        ' all other functions are implemented in its default way...

        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(Me.GetType())
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return SR.GetString(SR.PPG_ServiceReferenceTypeName)
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return [Namespace]
        End Function

        Public Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter(Me.GetType())
        End Function

        Public Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent(Me.GetType())
        End Function

        Public Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty(Me.GetType())
        End Function

        Public Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor(Me.GetType(), editorBaseType)
        End Function

        Public Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(Me.GetType())
        End Function

        Public Function GetEvents1(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(Me.GetType(), attributes)
        End Function

        ''' <summary>
        ''' Returns the Modified properties. 
        '''    - Makes the Metadata Location property readonly.
        ''' </summary>
        ''' <param name="orig">The original property list</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetModifiedPropertyList(ByVal orig As PropertyDescriptorCollection) As PropertyDescriptorCollection
            Dim modified As PropertyDescriptor() = New PropertyDescriptor(orig.Count - 1) {}

            Dim i As Integer = 0
            For Each prop As PropertyDescriptor In orig
                'Just modify the URL property to readonly if the reference has multiple URLs
                If prop.Name.Equals("ServiceReferenceURL", StringComparison.Ordinal) Then
                    modified(i) = TypeDescriptor.CreateProperty([GetType](), prop, New Attribute() {ReadOnlyAttribute.Yes})
                Else
                    modified(i) = orig(i)
                End If
                i += 1
            Next
            Return New PropertyDescriptorCollection(modified)
        End Function

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Dim orig As PropertyDescriptorCollection = TypeDescriptor.GetProperties(Me.GetType())

            If m_ReferenceGroup.GetReferenceCount() > 1 Then
                Return GetModifiedPropertyList(orig)
            Else
                Return orig
            End If
        End Function

        Public Function GetProperties1(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Dim orig As PropertyDescriptorCollection = TypeDescriptor.GetProperties(Me.GetType(), attributes)

            If m_ReferenceGroup.GetReferenceCount() > 1 Then
                Return GetModifiedPropertyList(orig)
            Else
                Return orig
            End If

        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function
#End Region
    End Class

End Namespace
