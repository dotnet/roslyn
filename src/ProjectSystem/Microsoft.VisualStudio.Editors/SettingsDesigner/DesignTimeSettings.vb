Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The root component for the settings designer.
    ''' The DesignTimeSettings class is a list of all the settings that are currently
    ''' available in a .Settings file.
    ''' </summary>
    ''' <remarks></remarks>
    < _
    Designer(GetType(SettingsDesigner), GetType(System.ComponentModel.Design.IRootDesigner)), _
    DesignerCategory("Designer") _
    > _
    Friend NotInheritable Class DesignTimeSettings
        Inherits Component
        Implements System.Collections.Generic.IEnumerable(Of DesignTimeSettingInstance)

        ' The namespace used the last time this instance was serialized
        Private m_persistedNamespace As String

        ''' <summary>
        ''' We may want to special-case handling of the generated class name to avoid
        ''' name clashes with updated projects...
        ''' </summary>
        ''' <remarks></remarks>
        Private m_useSpecialClassName As Boolean

        Private m_settings As New System.Collections.Generic.List(Of DesignTimeSettingInstance)(16)

        Private Function IEnumerableOfDesignTimeSettingInstance_GetEnumerator() As System.Collections.Generic.IEnumerator(Of DesignTimeSettingInstance) Implements System.Collections.Generic.IEnumerable(Of DesignTimeSettingInstance).GetEnumerator
            Return m_settings.GetEnumerator()
        End Function

        Private Function IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return IEnumerableOfDesignTimeSettingInstance_GetEnumerator()
        End Function

        <Browsable(False)> _
        Public ReadOnly Property Count() As Integer
            Get
                Return m_settings.Count
            End Get
        End Property


        ''' <summary>
        ''' Is the UseMySettingsClassName flag set in the underlying .settings file?
        ''' If so, we may want to special-case the class name...
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Property UseSpecialClassName() As Boolean
            Get
                Return m_useSpecialClassName
            End Get
            Set(ByVal value As Boolean)
                m_useSpecialClassName = value
            End Set
        End Property

        ''' <summary>
        ''' The namespace as persisted in the .settings file
        ''' </summary>
        ''' <value></value>
        ''' <remarks>May return NULL if no namespace was persisted!!!</remarks>
        Friend Property PersistedNamespace() As String
            Get
                Return m_persistedNamespace
            End Get
            Set(ByVal value As String)
                m_persistedNamespace = value
            End Set
        End Property

#Region "Valid/unique name handling"

        Private ReadOnly Property CodeProvider() As System.CodeDom.Compiler.CodeDomProvider
            Get
                Dim codeProviderInstance As System.CodeDom.Compiler.CodeDomProvider = Nothing

                Dim mdCodeDomProvider As Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider = TryCast(GetService(GetType(Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider)), _
                                            Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider)
                If mdCodeDomProvider IsNot Nothing Then
                    Try
                        codeProviderInstance = TryCast(mdCodeDomProvider.CodeDomProvider, System.CodeDom.Compiler.CodeDomProvider)
                    Catch ex As System.Runtime.InteropServices.COMException
                        ' Some project systems (i.e. C++) throws if you try to get the CodeDomProvider
                        ' property :(
                    End Try
                End If
                Return codeProviderInstance
            End Get
        End Property
        ''' <summary>
        ''' Is this a valid name for a setting in this collection?
        ''' </summary>
        ''' <param name="Name">Name to test</param>
        ''' <param name="instanceToIgnore">If we want to rename an existing setting, we want to that particular it from the unique name check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function IsValidName(ByVal Name As String, Optional ByVal checkForUniqueness As Boolean = False, Optional ByVal instanceToIgnore As DesignTimeSettingInstance = Nothing) As Boolean
            Return IsValidIdentifier(Name) AndAlso (Not checkForUniqueness OrElse IsUniqueName(Name, instanceToIgnore))
        End Function

        ''' <summary>
        ''' Is this a unique name 
        ''' </summary>
        ''' <param name="Name"></param>
        ''' <param name="IgnoreThisInstance"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function IsUniqueName(ByVal Name As String, Optional ByVal IgnoreThisInstance As DesignTimeSettingInstance = Nothing) As Boolean
            ' Empty name not considered unique!
            If Name = "" Then
                Return False
            End If

            For Each ExistingInstance As DesignTimeSettingInstance In Me
                If EqualIdentifiers(Name, ExistingInstance.Name) Then
                    If Not ExistingInstance Is IgnoreThisInstance Then
                        Return False
                    End If
                End If
            Next

            ' Since this component is also added to the designer host, we have to check this as well...
            '
            ' This *shouldn't* happen, so we assert here
            If Me.Site IsNot Nothing Then
                If EqualIdentifiers(Me.Site.Name, Name) Then
                    Debug.Fail("Why is the setting name equal to the DesignTimeSettings site name?")
                    Return False
                End If
            End If
            Return True
        End Function

        ''' <summary>
        ''' Is this a valid identifier for us to use?
        ''' </summary>
        ''' <param name="Name"></param>
        ''' <returns>
        ''' </returns>
        ''' <remarks>
        ''' We are more strict than the language specific code provider (if any) since the language specific code provider
        ''' may allow escaped identifiers. 
        ''' We need to know the un-escaped identifier ('cause that is what's going in to the app.config file), so we can't
        ''' allow that...
        ''' </remarks>
        Private Function IsValidIdentifier(ByVal Name As String) As Boolean
            If Name Is Nothing Then
                Return False
            End If

            If System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(Name) Then
                If CodeProvider IsNot Nothing Then
                    Return CodeProvider.IsValidIdentifier(Name)
                Else
                    Return True
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Determine if two identifiers are equal or not. We don't allow to identifiers to differ only in name
        ''' since we use the same name for the component as we do for the setting name, and the name of the component
        ''' is case insensitive... (adding two components with a site name that only differs in casing will cause the
        ''' DesignerHost to throw - this is consistent with how the windows forms designer handles component names)
        ''' </summary>
        ''' <param name="Id1"></param>
        ''' <param name="Id2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function EqualIdentifiers(ByVal Id1 As String, ByVal Id2 As String) As Boolean
            Return String.Equals(Id1, Id2, StringComparison.OrdinalIgnoreCase)
        End Function

        Friend Function CreateUniqueName(Optional ByVal Base As String = "Setting") As String
            If Base = "" Then
                Debug.Fail("Must give a valid base name or not supply any parameter at all (can't create a unique name from a null or empty string!)")
                Throw New ArgumentException()
            End If

            Dim ExistingNames As New System.Collections.Hashtable
            For Each Instance As DesignTimeSettingInstance In m_settings
                ExistingNames.Item(Instance.Name) = Nothing
            Next

            Dim SuggestedName As String = MakeValidIdentifier(Base)
            If Not ExistingNames.ContainsKey(SuggestedName) Then
                Return SuggestedName
            End If

            For i As Integer = 1 To Me.m_settings.Count + 1
                SuggestedName = MakeValidIdentifier(Base & i.ToString())
                If Not ExistingNames.ContainsKey(SuggestedName) Then
                    Return SuggestedName
                End If
            Next
            Debug.Fail("You should never reach this line of code!")
            Return ""
        End Function

        Private Function MakeValidIdentifier(ByVal name As String) As String
            If CodeProvider IsNot Nothing AndAlso Not IsValidIdentifier(name) Then
                Return CodeProvider.CreateValidIdentifier(name)
            Else
                Return name
            End If
        End Function


#End Region

#Region "Adding/removing settings"
        ''' <summary>
        ''' Add a new setting instance
        ''' </summary>
        ''' <param name="TypeName"></param>
        ''' <param name="SettingName"></param>
        ''' <param name="AllowMakeUnique">If true, we are allowed to change the name in order to make this setting unique</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function AddNew(ByVal TypeName As String, ByVal SettingName As String, ByVal AllowMakeUnique As Boolean) As DesignTimeSettingInstance
            Dim Instance As New DesignTimeSettingInstance
            Instance.SetSettingTypeName(TypeName)
            If Not IsUniqueName(SettingName) Then
                If Not AllowMakeUnique Then
                    Debug.Fail("Can't add two settings with the same name")
                    Throw Common.CreateArgumentException("AllowMakeUnique")
                Else
                    If SettingName = "" Then
                        SettingName = CreateUniqueName()
                    Else
                        SettingName = CreateUniqueName(SettingName)
                    End If
                End If
            End If

            Instance.SetName(SettingName)
            Add(Instance)
            Return Instance
        End Function

        ''' <summary>
        ''' Add a settings instance to our list of components
        ''' </summary>
        ''' <param name="Instance"></param>
        ''' <param name="MakeNameUnique"></param>
        ''' <remarks></remarks>
        Friend Sub Add(ByVal Instance As DesignTimeSettingInstance, Optional ByVal MakeNameUnique As Boolean = False)
            If Contains(Instance) Then
                Return
            End If

            If Not IsUniqueName(Instance.Name) Then
                If MakeNameUnique Then
                    Instance.SetName(CreateUniqueName(Instance.Name))
                Else
                    Throw New ArgumentException()
                End If
            End If

            If Not IsValidIdentifier(Instance.Name) Then
                Throw New ArgumentException()
            End If

            m_settings.Add(Instance)
            If Site IsNot Nothing AndAlso Site.Container IsNot Nothing Then
                ' Let's make sure we have this instance in "our" container (if any)
                If Instance.Site Is Nothing OrElse Not (Site.Container Is Instance.Site.Container) Then
                    Static uniqueNumber As Integer
                    uniqueNumber += 1
                    Dim newName As String = "Setting" & uniqueNumber.ToString()
                    Site.Container.Add(Instance, newName)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Do we already contain this instance? 
        ''' </summary>
        ''' <param name="instance"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' Useful to prevent adding the same setting multiple times
        ''' </remarks>
        Friend Function Contains(ByVal instance As DesignTimeSettingInstance) As Boolean
            Return m_settings.Contains(instance)
        End Function

        ''' <summary>
        ''' Remove a setting from our list of components...
        ''' </summary>
        ''' <param name="instance"></param>
        ''' <remarks></remarks>
        Friend Sub Remove(ByVal instance As DesignTimeSettingInstance)
            ' If the instance is site:ed, and it's containers components contains the instance, we better remove it...
            ' ...but only if our m_settings collection contains this instance...
            '
            ' Removing an instance from the site's container will fire a component removed event,
            ' which in turn will make us try and remove the item again. By removing the item from
            ' our internal collection and guarding against doing this multiple times, we avoid the
            ' nasty stack overflow...
            If m_settings.Remove(instance) AndAlso _
                instance.Site IsNot Nothing AndAlso _
                instance.Site.Container IsNot Nothing _
            Then
                instance.Site.Container.Remove(instance)
            End If

        End Sub
#End Region

    End Class

End Namespace
