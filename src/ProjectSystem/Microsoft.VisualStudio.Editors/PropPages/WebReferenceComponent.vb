Imports System.ComponentModel
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    ''' This is the class we wrapped a web reference and pushed to the propertyGrid
    ''' </Summary>
    Friend Class WebReferenceComponent
        Inherits Component
        Implements ICustomTypeDescriptor, IReferenceComponent, IUpdatableReferenceComponent

        Private m_page As ReferencePropPage
        Private m_projectItem As EnvDTE.ProjectItem

        Sub New(ByVal page As ReferencePropPage, ByVal projectItem As EnvDTE.ProjectItem)
            m_page = page
            m_projectItem = projectItem
        End Sub

        <VBDescription(My.Resources.Designer.ConstantResourceIDs.PPG_WebReferenceNameDescription)> _
        <MergablePropertyAttribute(False)> _
        <HelpKeyword("Folder Properties.FileName")> _
        Public Property Name() As String
            Get
                Try
                    Return m_projectItem.Name
                Catch ex As Exception
                    Debug.Fail(ex.Message)
                    Return String.Empty
                End Try
            End Get
            Set(ByVal value As String)
                m_projectItem.Name = value
                m_page.OnWebReferencePropertyChanged(Me)
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
        Private Function ShouldSerializeName() As Boolean
            Return False
        End Function

        Friend ReadOnly Property WebReference() As EnvDTE.ProjectItem
            Get
                Return m_projectItem
            End Get
        End Property

        <VBDisplayNameAttribute(My.Resources.Designer.ConstantResourceIDs.PPG_UrlBehaviorName)> _
        <VBDescriptionAttribute(My.Resources.Designer.ConstantResourceIDs.PPG_UrlBehaviorDescription)> _
        <HelpKeyword("Folder Properties.UrlBehavior")> _
        Public Property UrlBehavior() As UrlBehaviorType
            Get
                Dim prop As EnvDTE.[Property] = GetItemProperty("UrlBehavior")
                If prop IsNot Nothing Then
                    Return CType(CInt(prop.Value), UrlBehaviorType)
                Else
                    Debug.Fail("Why we can not find UrlBehavior")
                    Return UrlBehaviorType.Static
                End If
            End Get
            Set(ByVal value As UrlBehaviorType)
                Dim prop As EnvDTE.[Property] = GetItemProperty("UrlBehavior")
                If prop IsNot Nothing Then
                    prop.Value = CInt(value)
                    m_page.OnWebReferencePropertyChanged(Me)
                Else
                    Debug.Fail("Why we can not find UrlBehavior")
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
        Private Function ShouldSerializeUrlBehavior() As Boolean
            Return False
        End Function

        <VBDisplayNameAttribute(My.Resources.Designer.ConstantResourceIDs.PPG_WebReferenceUrlName)> _
        <VBDescription(My.Resources.Designer.ConstantResourceIDs.PPG_WebReferenceUrlDescription)> _
        <HelpKeyword("Folder Properties.WebReference")> _
        <MergablePropertyAttribute(False)> _
        Public Property WebReferenceURL() As String
            Get
                Dim prop As EnvDTE.[Property] = GetItemProperty("WebReference")
                If prop IsNot Nothing Then
                    Return CStr(prop.Value)
                Else
                    Debug.Fail("Why we can not find WebReference")
                    Return String.Empty
                End If
            End Get
            Set(ByVal value As String)
                If value Is Nothing Then
                    value = String.Empty
                End If

                Dim prop As EnvDTE.[Property] = GetItemProperty("WebReference")
                If prop IsNot Nothing Then
                    prop.Value = value
                    m_page.OnWebReferencePropertyChanged(Me)
                Else
                    Debug.Fail("Why we can not find WebReference")
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
        Private Function ShouldSerializeWebReferenceURL() As Boolean
            Return False
        End Function

        ' Access the property through EnvDTE.ProjectItem.Properties
        Private Function GetItemProperty(ByVal propertyName As String) As EnvDTE.[Property]
            Try
                Dim properties As EnvDTE.Properties = m_projectItem.Properties
                If properties IsNot Nothing Then
                    Return properties.Item(propertyName)
                End If
            Catch e As System.ArgumentException
                Debug.Fail(e.Message)
            End Try
            Return Nothing
        End Function

        ' Remove the webReference...
        Private Sub Remove() Implements IReferenceComponent.Remove
            m_projectItem.Remove()
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return Name
        End Function

        '''<summary>
        ''' Update the web reference
        '''</summary>
        Private Sub Update() Implements IUpdatableReferenceComponent.Update
            Dim referenceProperty As EnvDTE.[Property] = GetItemProperty("WebReferenceInterface")
            If referenceProperty IsNot Nothing Then
                Dim reference As VsWebSite.WebReference = TryCast(referenceProperty.Value, VsWebSite.WebReference)
                If reference IsNot Nothing Then
                    reference.Update()
                End If
            End If
        End Sub

#Region "System.ComponentModel.ICustomTypeDescriptor"
        ' we overrite the ICustomTypeDescriptor to replace the ClassName and ComponentName which are shown on the propertyGrid
        ' all other functions are implemented in its default way...

        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(Me.GetType())
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return SR.GetString(SR.PPG_WebReferenceTypeName)
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return Name
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

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(Me.GetType())
        End Function

        Public Function GetProperties1(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(Me.GetType(), attributes)
        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function
#End Region
    End Class

#Region "UrlBehaviorType"
    <TypeConverter(GetType(UrlBehaviorTypeConverter))> _
    Friend Enum UrlBehaviorType
        [Static]
        Dynamic
    End Enum

    ''' <Summary>
    '''  a TypeConvert to localize the UrlBehavior property...
    ''' </Summary>
    Friend Class UrlBehaviorTypeConverter
        Inherits TypeConverter

        Private Shared m_displayValues As String()

        ' a help collection to hold localized strings
        Private Shared ReadOnly Property DisplayValues() As String()
            Get
                If m_displayValues Is Nothing Then
                    m_displayValues = New String() {SR.GetString(SR.PPG_UrlBehavior_Static), SR.GetString(SR.PPG_UrlBehavior_Dynamic)}
                End If
                Return m_displayValues
            End Get
        End Property

        ' we only implement coverting from string...
        Public Overrides Function CanConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal sourceType As System.Type) As Boolean
            If sourceType Is GetType(String) Then
                Return True
            End If
            Return MyBase.CanConvertFrom(context, sourceType)
        End Function

        ' we only implement coverting to string...
        Public Overrides Function CanConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal destinationType As System.Type) As Boolean
            If destinationType Is GetType(String) Then
                Return True
            End If
            Return MyBase.CanConvertTo(context, destinationType)
        End Function

        ' we only implement coverting from string...
        Public Overrides Function ConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object) As Object
            If TypeOf value Is String Then
                Dim stringValue As String = CStr(value)
                For i As Integer = 0 To DisplayValues.Length - 1
                    If DisplayValues(i).Equals(stringValue) Then
                        Return CType(i, UrlBehaviorType)
                    End If
                Next
            End If
            Return MyBase.ConvertFrom(context, culture, value)
        End Function

        ' we only implement coverting to string...
        Public Overrides Function ConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object, ByVal destinationType As System.Type) As Object
            If destinationType Is GetType(String) Then
                Dim type As UrlBehaviorType = CType(value, UrlBehaviorType)
                Return DisplayValues(CInt(type))
            End If
            Return MyBase.ConvertTo(context, culture, value, destinationType)
        End Function

        ' standard value collection... will be used in the dropdown of the propertyGrid
        Public Overrides Function GetStandardValues(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.ComponentModel.TypeConverter.StandardValuesCollection
            Return New StandardValuesCollection(New UrlBehaviorType() {UrlBehaviorType.Static, UrlBehaviorType.Dynamic})
        End Function

        Public Overrides Function GetStandardValuesSupported(ByVal context As System.ComponentModel.ITypeDescriptorContext) As Boolean
            Return True
        End Function
    End Class
#End Region

End Namespace

