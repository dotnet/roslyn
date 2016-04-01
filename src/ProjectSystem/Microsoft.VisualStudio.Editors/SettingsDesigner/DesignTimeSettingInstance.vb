' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports Microsoft.VisualStudio.Editors.PropertyPages

Imports IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' A DesignTimeSettingInstance wraps a single setting. Each setting will generate 
    ''' a property with the appropriate attributes in the generated file.
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable()> _
    Friend Class DesignTimeSettingInstance
        Inherits System.ComponentModel.Component
        Implements ICustomTypeDescriptor, System.Runtime.Serialization.ISerializable

        ''' <summary>
        ''' Application or user scoped setting?
        ''' </summary>
        ''' <remarks></remarks>
        <System.ComponentModel.TypeConverter(GetType(ScopeConverter))> _
        Friend Enum SettingScope
            ' Don't use zero in the enum since that hides CType(NULL, SettingScope) 
            ' issues...
            User = 1
            Application = 2
        End Enum

        ''' <summary>
        ''' The name of this setting instance
        ''' </summary>
        ''' <remarks></remarks>
        Private _name As String

        ''' <summary>
        ''' The type name (as persisted in the .settings file) of this setting
        ''' </summary>
        ''' <remarks></remarks>
        Private _settingTypeName As String = GetType(String).FullName

        ''' <summary>
        ''' The setting scope (application or user) for this setting
        ''' </summary>
        ''' <remarks></remarks>
        Private _settingScope As SettingScope = SettingScope.User

        ''' <summary>
        ''' Is this setting a roaming setting?
        ''' </summary>
        ''' <remarks></remarks>
        Private _roaming As Boolean = False

        ''' <summary>
        ''' The serialized representation of this setting
        ''' </summary>
        ''' <remarks></remarks>
        Private _serializedValue As String = ""

        ''' <summary>
        ''' The setting provider if any
        ''' </summary>
        ''' <remarks></remarks>
        Private _provider As String

        ''' <summary>
        ''' The description for this setting
        ''' </summary>
        ''' <remarks></remarks>
        Private _description As String

        ''' <summary>
        ''' Flag indicating if we want to add the serialized value as a
        ''' DefaultSettingValue attribute on the setting. If the setting
        ''' contains sensitive information, the user can set this to false
        ''' through the property grid...
        ''' </summary>
        ''' <remarks></remarks>
        Private _generateDefaultValueInCode As Boolean = True

#Region "Cached property descriptors with this instance as the owner"
        Private _generateDefaultValueInCodePropertyDescriptor As New GenerateDefaultValueInCodePropertyDescriptor(Me)
        Private _namePropertyDescriptor As New NamePropertyDescriptor(Me)
        Private _descriptionPropertyDescriptor As New DescriptionPropertyDescriptor(Me)
        Private _providerPropertyDescriptor As New ProviderPropertyDescriptor(Me)
        Private _roamingPropertyDescriptor As New RoamingPropertyDescriptor(Me)
        Private _scopePropertyDescriptor As New ScopePropertyDescriptor(Me)
        Private _serializedValuePropertyDescriptor As New SerializedValuePropertyDescriptor(Me)
        Private _settingTypeNamePropertyDescriptor As New SettingTypeNamePropertyDescriptor(Me)
#End Region

        Public Sub New()

        End Sub

#Region "ICustomTypeDescriptor implementation. For a detailed description, see the MSDN docs"
        Private Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return New AttributeCollection(New System.ComponentModel.Design.HelpKeywordAttribute("ApplicationSetting"))
        End Function

        Private Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return Me.GetType().FullName
        End Function

        Private Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return Me.Name
        End Function

        Private Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return Nothing
        End Function

        Private Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return Nothing
        End Function

        Private Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return _namePropertyDescriptor
        End Function

        Private Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return Nothing
        End Function

        Private Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(New EventDescriptor() {})
        End Function

        Private Function GetEvents(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return GetEvents()
        End Function

        Private Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return GetProperties(Nothing)
        End Function

        Private Function GetProperties(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return New PropertyDescriptorCollection(New PropertyDescriptor() { _
                                                            _namePropertyDescriptor, _
                                                            _roamingPropertyDescriptor, _
                                                            _descriptionPropertyDescriptor, _
                                                            _providerPropertyDescriptor, _
                                                            _scopePropertyDescriptor, _
                                                            _generateDefaultValueInCodePropertyDescriptor, _
                                                            _settingTypeNamePropertyDescriptor, _
                                                            _serializedValuePropertyDescriptor})
        End Function

        Private Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            If pd Is Nothing Then
                ' No property descriptor => should return the current instance...
                Return Me
            End If

            Dim dsicpd As DesignTimeSettingInstanceCustomPropertyDescriptorBase = TryCast(pd, DesignTimeSettingInstanceCustomPropertyDescriptorBase)
            If dsicpd IsNot Nothing Then
                Return dsicpd.Owner
            Else
                Debug.Fail(String.Format("Why did someone ask me what the owner of a property descriptor of type {0} is?", pd.GetType().FullName))
                Return Nothing
            End If
        End Function
#End Region

#Region "Custom property descriptors"
#Region "Abstract property descriptor base classes"
        ''' <summary>
        ''' Base implementation for our custom property descriptors. Adding some default
        ''' behavior and overrides to make it more explicit what we expect from each
        ''' propertydescriptor.
        ''' </summary>
        ''' <remarks></remarks>
        Private MustInherit Class DesignTimeSettingInstanceCustomPropertyDescriptorBase
            Inherits PropertyDescriptor

            Private _owner As DesignTimeSettingInstance

            Public Sub New(ByVal owner As DesignTimeSettingInstance, ByVal name As String)
                MyBase.New(name, New System.Attribute() {})
                _owner = owner
            End Sub

            Public ReadOnly Property Owner() As DesignTimeSettingInstance
                Get
                    Return _owner
                End Get
            End Property

            Public Overrides ReadOnly Property ComponentType() As System.Type
                Get
                    Return GetType(DesignTimeSettingInstance)
                End Get
            End Property

            Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
                Return False
            End Function

            Public Overrides Sub ResetValue(ByVal component As Object)
                Throw New NotSupportedException()
            End Sub

            Public Overrides Function ShouldSerializeValue(ByVal component As Object) As Boolean
                Return True
            End Function

            Public Overrides Function GetValue(ByVal component As Object) As Object
                Return GetValue(DirectCast(component, DesignTimeSettingInstance))
            End Function

            Protected Overrides Sub FillAttributes(ByVal attributeList As System.Collections.IList)
                MyBase.FillAttributes(attributeList)
                If DescriptionAttributeText <> "" Then
                    attributeList.Add(New DescriptionAttribute(DescriptionAttributeText))
                End If
            End Sub

            ''' <summary>
            ''' Override to set the description (shown in the properties window) for this
            ''' property
            ''' </summary>
            ''' <value></value>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Protected MustOverride ReadOnly Property DescriptionAttributeText() As String

            ''' <summary>
            ''' Override to get the value for the current component in a type-safe way
            ''' </summary>
            ''' <param name="component"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Protected MustOverride Overloads Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
        End Class

        ''' <summary>
        ''' Add Undo support (component change notifications and designer transactions w. descriptions)
        ''' whenever we change the value
        ''' </summary>
        ''' <remarks></remarks>
        Private MustInherit Class DesignTimeSettingInstanceCustomPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptorBase


            Public Sub New(ByVal owner As DesignTimeSettingInstance, ByVal name As String)
                MyBase.New(owner, name)
            End Sub


            ''' <summary>
            ''' Wrap the call to the derived SetValue in a designer transaction, providing the 
            ''' description to the transaction from the actual property descriptor
            ''' </summary>
            ''' <param name="component"></param>
            ''' <param name="value"></param>
            ''' <remarks></remarks>
            Public NotOverridable Overrides Sub SetValue(ByVal component As Object, ByVal value As Object)
                Dim instance As DesignTimeSettingInstance = DirectCast(component, DesignTimeSettingInstance)
                Dim ccsvc As System.ComponentModel.Design.IComponentChangeService = Nothing
                Dim host As System.ComponentModel.Design.IDesignerHost = Nothing
                If instance IsNot Nothing AndAlso instance.Site IsNot Nothing Then
                    ccsvc = DirectCast(instance.Site.GetService(GetType(System.ComponentModel.Design.IComponentChangeService)), _
                                        System.ComponentModel.Design.IComponentChangeService)
                    host = DirectCast(instance.Site.GetService(GetType(System.ComponentModel.Design.IDesignerHost)), _
                                        System.ComponentModel.Design.IDesignerHost)
                End If


                Dim undoTran As System.ComponentModel.Design.DesignerTransaction = Nothing
                Try
                    Dim oldValue As Object = GetValue(component)

                    If Object.Equals(oldValue, value) Then
                        ' We don't want to create an undounit if the values are equal...
                        Return
                    End If

                    ' Create transaction/fire component changing
                    If host IsNot Nothing Then
                        undoTran = host.CreateTransaction(UndoDescription)
                    End If
                    If ccsvc IsNot Nothing Then
                        ccsvc.OnComponentChanging(component, Me)
                    End If

                    SetValue(instance, value)

                    ' Fire component changed/close transaction
                    If ccsvc IsNot Nothing Then
                        ccsvc.OnComponentChanged(component, Me, oldValue, value)
                    End If
                    If undoTran IsNot Nothing Then
                        undoTran.Commit()
                        undoTran = Nothing
                    End If
                Finally
                    If undoTran IsNot Nothing Then
                        undoTran.Cancel()
                    End If
                End Try
            End Sub

            ''' <summary>
            ''' Override to provide the description showing up in the undo menu
            ''' </summary>
            ''' <value></value>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Protected MustOverride ReadOnly Property UndoDescription() As String

            ''' <summary>
            ''' Override for the actual set of the value
            ''' </summary>
            ''' <param name="component"></param>
            ''' <param name="value"></param>
            ''' <remarks></remarks>
            Protected MustOverride Overloads Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)


        End Class
#End Region


#Region "Exposed property descriptors"
        Private Class GenerateDefaultValueInCodePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "GenerateDefaultValueInCode")
            End Sub

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.GenerateDefaultValueInCode
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(Boolean)
                End Get
            End Property

            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetGenerateDefaultValueInCode(CBool(value))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_GenerateDefaultValueInCode)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_GenerateDefaultValueInCode)
                End Get
            End Property
        End Class

        Private Class NamePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Name")
            End Sub

            Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
                Return False
            End Function

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.Name
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return DesignTimeSettingInstance.IsNameReadOnly(owner)
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetName(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_NameChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Name)
                End Get
            End Property
        End Class

        Private Class RoamingPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Roaming")
            End Sub

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.Roaming
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return IsRoamingReadOnly(owner)
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(Boolean)
                End Get
            End Property

            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetRoaming(CBool(value))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_RoamingChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Roaming)
                End Get
            End Property
        End Class

        Private Class DescriptionPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Description")
            End Sub

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.Description
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetDescription(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_DescriptionChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Description)
                End Get
            End Property
        End Class

        Private Class ProviderPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Provider")
            End Sub

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.Provider
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetProvider(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_ProviderChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Provider)
                End Get
            End Property
        End Class

        Private Class ScopePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Scope")
            End Sub

            Protected Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.Scope
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    ' Connection string typed settings are application scoped only...
                    Return DesignTimeSettingInstance.IsScopeReadOnly(Owner, DesignTimeSettingInstance.ProjectSupportsUserScopedSettings(Owner))
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(DesignTimeSettingInstance.SettingScope)
                End Get
            End Property

            Protected Overrides Sub FillAttributes(ByVal attributeList As System.Collections.IList)
                MyBase.FillAttributes(attributeList)
                attributeList.Add(New TypeConverterAttribute(GetType(ScopeConverter)))
            End Sub
            Protected Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetScope(CType(value, DesignTimeSettingInstance.SettingScope))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_ScopeChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Scope)
                End Get
            End Property

            Public Overrides ReadOnly Property Converter() As TypeConverter
                Get
                    Return New ScopeConverter
                End Get
            End Property

        End Class

        Private Class SettingTypeNamePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "SettingTypeName")
            End Sub

            Protected Overloads Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.SettingTypeName
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    ' The type name is always read-only in the property grid...
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(System.String)
                End Get
            End Property

            Protected Overloads Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetSettingTypeName(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_TypeChanged)
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_SerializedSettingType)
                End Get
            End Property
        End Class

        Private Class SerializedValuePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(ByVal owner As DesignTimeSettingInstance)
                MyBase.New(owner, "SerializedValue")
            End Sub

            Protected Overloads Overrides Function GetValue(ByVal component As DesignTimeSettingInstance) As Object
                Return component.SerializedValue
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overloads Overrides Sub SetValue(ByVal component As DesignTimeSettingInstance, ByVal value As Object)
                component.SetSerializedValue(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription() As String
                Get
                    Return SR.GetString(SR.SD_UndoTran_SerializedValueChanged)
                End Get
            End Property

            Protected Overrides Sub FillAttributes(ByVal attributeList As System.Collections.IList)
                MyBase.FillAttributes(attributeList)
                attributeList.Add(New BrowsableAttribute(False))
            End Sub

            Protected Overrides ReadOnly Property DescriptionAttributeText() As String
                Get
                    Return SR.GetString(SR.SD_DESCR_Value)
                End Get
            End Property
        End Class

#End Region

#Region "Type converters used not only by the property descriptors"

        ''' <summary>
        ''' Translate to/from localized string representation of User and Application
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class ScopeConverter
            Inherits System.ComponentModel.EnumConverter

            Public Sub New()
                MyBase.New(GetType(DesignTimeSettingInstance.SettingScope))
            End Sub

            Public Overrides Function CanConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal type As System.Type) As Boolean
                If GetType(String).Equals(type) Then
                    Return True
                Else
                    Return MyBase.CanConvertFrom(context, type)
                End If
            End Function

            Public Overrides Function CanConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal type As System.Type) As Boolean
                If GetType(String).Equals(type) Then
                    Return True
                Else
                    Return MyBase.CanConvertTo(context, type)
                End If
            End Function

            Public Overrides Function ConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object) As Object
                If TypeOf value Is String Then
                    If String.Equals(DirectCast(value, String), SR.GetString(SR.SD_ComboBoxItem_ApplicationScope), StringComparison.Ordinal) Then
                        Return DesignTimeSettingInstance.SettingScope.Application
                    End If
                    If String.Equals(DirectCast(value, String), SR.GetString(SR.SD_ComboBoxItem_UserScope), StringComparison.Ordinal) Then
                        Return DesignTimeSettingInstance.SettingScope.User
                    End If
                End If
                Return MyBase.ConvertFrom(context, culture, value)
            End Function

            Public Overrides Function ConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object, ByVal destinationType As System.Type) As Object
                If GetType(String).Equals(destinationType) Then
                    Dim instance As DesignTimeSettingInstance = Nothing
                    If context IsNot Nothing Then
                        instance = TryCast(context.Instance, DesignTimeSettingInstance)
                    End If
                    Return ScopeConverter.ConvertToLocalizedString(instance, CType(value, DesignTimeSettingInstance.SettingScope))
                End If
                Return MyBase.ConvertTo(context, culture, value, destinationType)
            End Function


            ''' <summary>
            ''' Shared helper method to convert a scope value to a string suitable for
            ''' display in the UI. Since the string value may depend on the instance which
            ''' it is associated (if the provider is the web settings provider, it is different
            ''' than for other providers) we also pass in an DesignTimeSettingInstance
            ''' </summary>
            ''' <remarks>Returns the localized version for the scope</remarks>
            Public Shared Function ConvertToLocalizedString(ByVal instance As DesignTimeSettingInstance, ByVal scope As DesignTimeSettingInstance.SettingScope) As String
                Select Case scope
                    Case SettingScope.Application
                        If IsWebProvider(instance) Then
                            Static WebApplicationScopeString As String = Nothing
                            If WebApplicationScopeString Is Nothing Then
                                WebApplicationScopeString = SR.GetString(SR.SD_ComboBoxItem_WebApplicationScope)
                            End If
                            Return WebApplicationScopeString
                        Else
                            Static ApplicationScopeString As String = Nothing
                            If ApplicationScopeString Is Nothing Then
                                ApplicationScopeString = SR.GetString(SR.SD_ComboBoxItem_ApplicationScope)
                            End If
                            Return ApplicationScopeString
                        End If
                    Case SettingScope.User
                        If IsWebProvider(instance) Then
                            Static WebUserScopeString As String = Nothing
                            If WebUserScopeString Is Nothing Then
                                WebUserScopeString = SR.GetString(SR.SD_ComboBoxItem_WebUserScope)
                            End If
                            Return WebUserScopeString
                        Else
                            Static UserScopeString As String = Nothing
                            If UserScopeString Is Nothing Then
                                UserScopeString = SR.GetString(SR.SD_ComboBoxItem_UserScope)
                            End If
                            Return UserScopeString
                        End If
                    Case Else
                        Debug.Fail("Unknown scope")
                        Return scope.ToString()
                End Select
            End Function
        End Class
#End Region

#End Region

#Region "DesignTimeSettingInstance persisted fields/properties"

#Region "Name"
        ''' <summary>
        ''' The name of a setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property NameProperty() As PropertyDescriptor
            Get
                Return _namePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property Name() As String
            Get
                Return _name
            End Get
        End Property

        Public Sub SetName(ByVal value As String)
            If value = "" Then
                Throw New ArgumentException(SR.GetString(SR.SD_ERR_NameEmpty))
            End If
            If Not DesignTimeSettings.EqualIdentifiers(Name, value) AndAlso Site IsNot Nothing Then

                For Each probeComponent As IComponent In Site.Container.Components
                    Dim instance As DesignTimeSettingInstance = TryCast(probeComponent, DesignTimeSettingInstance)
                    If instance IsNot Nothing AndAlso DesignTimeSettings.EqualIdentifiers(instance.Name, value) Then
                        Throw New ArgumentException(SR.GetString(SR.SD_ERR_DuplicateName_1Arg, value))
                    End If
                Next

                Dim nameCreationService As System.ComponentModel.Design.Serialization.INameCreationService = TryCast( _
                    Site.GetService(GetType(System.ComponentModel.Design.Serialization.INameCreationService)), _
                    System.ComponentModel.Design.Serialization.INameCreationService)

                If nameCreationService IsNot Nothing AndAlso Not nameCreationService.IsValidName(value) Then
                    Throw New System.ArgumentException(SR.GetString(SR.SD_ERR_InvalidIdentifier_1Arg, value))
                End If
            End If
            _name = value
        End Sub
#End Region

#Region "Scope"

        ''' <summary>
        ''' The scope for a setitng
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ScopeProperty() As PropertyDescriptor
            Get
                Return _scopePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property Scope() As SettingScope
            Get
                Return _settingScope
            End Get
        End Property

        Public Sub SetScope(ByVal value As SettingScope)
            _settingScope = value
        End Sub
#End Region

#Region "TypeName"

        ''' <summary>
        ''' The name of the type for a setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property TypeNameProperty() As PropertyDescriptor
            Get
                Return _settingTypeNamePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property SettingTypeName() As String
            Get
                Return _settingTypeName
            End Get
        End Property

        Public Sub SetSettingTypeName(ByVal value As String)
            _settingTypeName = value
        End Sub
#End Region

#Region "SerializedValue"

        ''' <summary>
        ''' The serialized (string) representation of the value of this setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property SerializedValueProperty() As PropertyDescriptor
            Get
                Return Me._serializedValuePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property SerializedValue() As String
            Get
                Return _serializedValue
            End Get
        End Property

        Public Sub SetSerializedValue(ByVal value As String)
            _serializedValue = value
        End Sub
#End Region

#Region "Roaming"

        ''' <summary>
        ''' Flag indicating if this is a roaming setting 
        ''' (value is stored in the roaming user.config by the runtime)
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Roaming() As Boolean
            Get
                Return _roaming
            End Get
        End Property

        Public Sub SetRoaming(ByVal value As Boolean)
            _roaming = value
        End Sub
#End Region

#Region "Description"

        ''' <summary>
        ''' The description for this setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Description() As String
            Get
                Return _description
            End Get
        End Property

        Public Sub SetDescription(ByVal value As String)
            _description = value
        End Sub
#End Region

#Region "Provider"

        ''' <summary>
        ''' The settings provider for this setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Provider() As String
            Get
                Return _provider
            End Get
        End Property

        Public Sub SetProvider(ByVal value As String)
            _provider = value

            ' Setting the provider may actually change the scope display name...
            TypeDescriptor.Refresh(Me)
        End Sub
#End Region

#Region "Generate default value in code"

        ''' <summary>
        ''' Flag indicating if we should generate a defaultsettingsvalue attribute
        ''' on this setting
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property GenerateDefaultValueInCode() As Boolean
            Get
                Return _generateDefaultValueInCode
            End Get
        End Property

        Public Sub SetGenerateDefaultValueInCode(ByVal value As Boolean)
            _generateDefaultValueInCode = value
        End Sub
#End Region

#Region "Helper methods to determine the state of the current instance"

        ''' <summary>
        ''' Is the provided setting instance of type connection string?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsConnectionString(ByVal instance As DesignTimeSettingInstance) As Boolean
            If instance IsNot Nothing AndAlso String.Equals(instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is the provided setting instance using the Web settings provider?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsWebProvider(ByVal instance As DesignTimeSettingInstance) As Boolean
            If instance IsNot Nothing AndAlso String.Equals(instance.Provider, ServicesPropPageAppConfigHelper.ClientSettingsProviderName, StringComparison.Ordinal) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is the provided setting instance using the local file settings provider?
        ''' </summary>
        ''' <remarks>Returns true for a NULL setting instance</remarks>
        Friend Shared Function IsLocalFileSettingsProvider(ByVal instance As DesignTimeSettingInstance) As Boolean
            If instance Is Nothing Then
                Return True
            ElseIf instance.Provider = "" Then
                Return True
            ElseIf String.Equals(instance.Provider, GetType(Configuration.LocalFileSettingsProvider).Name, StringComparison.Ordinal) Then
                Return True
            ElseIf String.Equals(instance.Provider, GetType(Configuration.LocalFileSettingsProvider).FullName, StringComparison.Ordinal) Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Should the scope for the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsScopeReadOnly(ByVal instance As DesignTimeSettingInstance, ByVal projectSupportsUserScopedSettings As Boolean) As Boolean
            If IsConnectionString(instance) Then
                Return True
            End If

            If IsWebProvider(instance) Then
                Return True
            End If

            If IsLocalFileSettingsProvider(instance) AndAlso Not projectSupportsUserScopedSettings _
               AndAlso (instance Is Nothing OrElse instance.Scope = DesignTimeSettingInstance.SettingScope.Application) _
            Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Does the current project system support user scoped settings?
        ''' </summary>
        ''' <param name="instance"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function ProjectSupportsUserScopedSettings(ByVal instance As DesignTimeSettingInstance) As Boolean
            If instance Is Nothing OrElse instance.Site Is Nothing Then
                Return True
            Else
                Return ProjectSupportsUserScopedSettings(TryCast(instance.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy))
            End If
        End Function

        ''' <summary>
        ''' Does the current project system support user scoped settings?
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function ProjectSupportsUserScopedSettings(ByVal hierarchy As IVsHierarchy) As Boolean
            If hierarchy Is Nothing Then
                Return True
            Else
                Return Not Common.ShellUtil.IsWebProject(hierarchy)
            End If
        End Function

        ''' <summary>
        ''' Should the roaming mode for the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsRoamingReadOnly(ByVal instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            End If

            If instance IsNot Nothing AndAlso instance.Scope = DesignTimeSettingInstance.SettingScope.Application Then
                ' Application scoped settings can't be roaming, so we'll 
                ' indicate that this is a read-only property in that case...
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Should the name of the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsNameReadOnly(ByVal instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Should the type of the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsTypeReadOnly(ByVal instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            Else
                Return False
            End If
        End Function

#End Region

#End Region

#Region "ISerializable implementation"

        Private Const s_SERIALIZATION_DESCRIPTION As String = "Description"
        Private Const s_SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE As String = "GenerateDefaultValueInCode"
        Private Const s_SERIALIZATION_IS_ROAMING As String = "Roaming"
        Private Const s_SERIALIZATION_NAME As String = "Name"
        Private Const s_SERIALIZATION_PROVIDER As String = "Provider"
        Private Const s_SERIALIZATION_TYPE As String = "SerializedType"
        Private Const s_SERIALIZATION_VALUE As String = "SerializedValue"
        Private Const s_SERIALIZATION_SCOPE As String = "Scope"

        'See .NET Framework Developer's Guide, "Custom Serialization" for more information
        Protected Sub New(ByVal Info As System.Runtime.Serialization.SerializationInfo, ByVal Context As System.Runtime.Serialization.StreamingContext)
            _description = Info.GetString(s_SERIALIZATION_DESCRIPTION)
            _generateDefaultValueInCode = Info.GetBoolean(s_SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE)
            _name = Info.GetString(s_SERIALIZATION_NAME)
            _roaming = Info.GetBoolean(s_SERIALIZATION_IS_ROAMING)
            _provider = Info.GetString(s_SERIALIZATION_PROVIDER)
            _settingTypeName = Info.GetString(s_SERIALIZATION_TYPE)
            _serializedValue = Info.GetString(s_SERIALIZATION_VALUE)
            _settingScope = CType(Info.GetInt32(s_SERIALIZATION_SCOPE), SettingScope)
        End Sub


        <System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter:=True)> _
        Private Sub GetObjectData(ByVal Info As System.Runtime.Serialization.SerializationInfo, ByVal Context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
            Info.AddValue(s_SERIALIZATION_DESCRIPTION, _description)
            Info.AddValue(s_SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE, _generateDefaultValueInCode)
            Info.AddValue(s_SERIALIZATION_NAME, _name)
            Info.AddValue(s_SERIALIZATION_IS_ROAMING, _roaming)
            Info.AddValue(s_SERIALIZATION_PROVIDER, _provider)
            Info.AddValue(s_SERIALIZATION_TYPE, _settingTypeName)
            Info.AddValue(s_SERIALIZATION_VALUE, _serializedValue)
            Info.AddValue(s_SERIALIZATION_SCOPE, CType(_settingScope, Int32))
        End Sub

#End Region
    End Class

End Namespace
