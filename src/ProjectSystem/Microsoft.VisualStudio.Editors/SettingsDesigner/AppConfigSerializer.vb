' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Windows.Forms.Design
Imports System.Xml
Imports System.Configuration

Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Helper class for (de)serializing the contents app.config
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class AppConfigSerializer

        ''' <summary>
        ''' Indicate how we should handle conflicts between value in app.config file
        ''' and in settings object passed in
        ''' </summary>
        ''' <remarks></remarks>
        Friend Enum MergeValueMode
            UseSettingsFileValue    ' The value in the settings file wins
            UseAppConfigFileValue   ' The value in the app.config file wins
            Prompt                  ' Prompt the user
        End Enum

        ''' <summary>
        ''' Indicate what changes were performed during deserialization
        ''' </summary>
        ''' <remarks></remarks>
        <FlagsAttribute()> _
        Friend Enum DirtyState
            NoChange = 0            ' No changes made
            ValueAdded = 1          ' At least one value was modified
            ValueModified = 2       ' At least one setting was added
        End Enum

        ''' <summary>
        ''' Get settings from app.config file.
        ''' If the serialized representation in the app.config file differs from the value in the DesignTimeSettings
        ''' object passed in to the function, the user will be prompted if (s)he want to replace the value in the .settings
        ''' file with the value in app.config
        ''' If the setting name from the app.config file doesn't exist in the DesignTimeSettings object passed to the function,
        ''' new setting will be added to it. 
        ''' If one or more values have been added to the settings object, the user is notified after deserialization is complete.
        ''' </summary>
        ''' <param name="Settings"></param>
        ''' <param name="SectionName"></param>
        ''' <param name="AppConfigDocData"></param>
        ''' <param name="mergeMode"></param>
        ''' <param name="UIService"></param>
        ''' <returns>True if we made any changes to the object, false otherwise</returns>
        ''' <remarks></remarks>
        Friend Shared Function Deserialize(ByVal Settings As DesignTimeSettings, ByVal typeCache As SettingsTypeCache, ByVal valueCache As SettingsValueCache, ByVal SectionName As String, ByVal AppConfigDocData As DocData, ByVal mergeMode As MergeValueMode, Optional ByVal UIService As IUIService = Nothing) As DirtyState
            Dim objectDirty As DirtyState = DirtyState.NoChange

            ' Create a "master" list of existing settings....
            Dim ExistingSettings As New Dictionary(Of String, DesignTimeSettingInstance)
            For Each Instance As DesignTimeSettingInstance In Settings
                ExistingSettings(Instance.Name) = Instance
            Next

            ' We need to get our hands on a ConfigHelperService to deserialize the contents of the .config file...
            ' 
            Dim ConfigHelperService As New Microsoft.VisualStudio.Shell.Design.Serialization.ConfigurationHelperService()

            ' Let us get all settings that we know about, create SettingsPropeties for 'em
            ' and add 'em to a SettingsPropertyCollection.
            Dim UserScopedSettingProps As New System.Configuration.SettingsPropertyCollection()
            Dim AppScopedSettingProps As New System.Configuration.SettingsPropertyCollection()

            For Each Instance As DesignTimeSettingInstance In ExistingSettings.Values
                Dim settingType As System.Type = typeCache.GetSettingType(Instance.SettingTypeName)
                ' We need a name and a type to be able to serialize this guy... unless it is a connection string, of course!
                If Instance.Name <> "" AndAlso settingType IsNot Nothing AndAlso settingType IsNot GetType(SerializableConnectionString) Then
                    Dim NewProp As New System.Configuration.SettingsProperty(Instance.Name)
                    NewProp.PropertyType = settingType
                    NewProp.SerializeAs = ConfigHelperService.GetSerializeAs(settingType)
                    If Instance.Scope = DesignTimeSettingInstance.SettingScope.Application Then
                        AppScopedSettingProps.Add(NewProp)
                    Else
                        UserScopedSettingProps.Add(NewProp)
                    End If
                End If
            Next

            ' Deserialize conenction strings
            '
            ' First, we ask the config helper to read all the connection strings....
            Dim DeserializedConnectionStrings As ConnectionStringSettingsCollection = _
                ConfigHelperService.ReadConnectionStrings(AppConfigDocData.Name, AppConfigDocData, SectionName)

            ' Deserialize "normal" app scoped and user scoped settings
            Dim configFileMap As New ExeConfigurationFileMap
            configFileMap.ExeConfigFilename = AppConfigDocData.Name
            Dim DeserializedAppScopedSettingValues As SettingsPropertyValueCollection = _
                ConfigHelperService.ReadSettings(configFileMap, ConfigurationUserLevel.None, AppConfigDocData, SectionName, False, AppScopedSettingProps)
            Dim DeserializedUserScopedSettingValues As SettingsPropertyValueCollection = _
                ConfigHelperService.ReadSettings(configFileMap, ConfigurationUserLevel.None, AppConfigDocData, SectionName, True, UserScopedSettingProps)

            Dim valueSerializer As New SettingsValueSerializer()

            ' Then we go through each read setting to see if we have to add/change the setting
            For Each cs As System.Configuration.ConnectionStringSettings In DeserializedConnectionStrings
                Dim scs As New SerializableConnectionString
                scs.ConnectionString = cs.ConnectionString
                If cs.ProviderName <> "" Then
                    scs.ProviderName = cs.ProviderName
                End If
                If ExistingSettings.ContainsKey(cs.Name) Then
                    ' We already know about this guy - let's see if the values are different...
                    ' (by comparing the serialized values of the corresponding SerializedConnectionStrings)
                    Dim serializedValueInSettings As String = ExistingSettings.Item(cs.Name).SerializedValue
                    Dim serializedValueInAppConfig As String = valueSerializer.Serialize(scs, System.Globalization.CultureInfo.InvariantCulture)
                    Dim valueChanged As Boolean = String.Compare(serializedValueInSettings, serializedValueInAppConfig, StringComparison.Ordinal) <> 0
                    If valueChanged Then
                        ' Yep! Ask the user what (s)he wants to do...
                        Dim Instance As DesignTimeSettingInstance = ExistingSettings.Item(cs.Name)
                        objectDirty = objectDirty Or QueryReplaceValue(Settings, Instance, serializedValueInAppConfig, DesignTimeSettingInstance.SettingScope.Application, mergeMode, UIService)
                    End If
                Else
                    ' Only add this instance if the name is valid...
                    If Settings.IsValidName(cs.Name) Then
                        ' Found a new connection string! Let's create a new setting...
                        Dim Instance As New DesignTimeSettingInstance
                        Dim newValue As New SerializableConnectionString
                        If cs.ProviderName <> "" Then
                            newValue.ProviderName = cs.ProviderName
                        End If
                        newValue.ConnectionString = cs.ConnectionString
                        Instance.SetSerializedValue(valueSerializer.Serialize(newValue, System.Globalization.CultureInfo.InvariantCulture))
                        Instance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                        Instance.SetName(cs.Name)
                        Instance.SetSettingTypeName(SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString)
                        Settings.Add(Instance)
                        ' ...and set the flag to indicate that new settings were added....
                        objectDirty = objectDirty Or DirtyState.ValueAdded
                    End If
                End If
            Next


            ' Check if we need to add/change any application scoped settings
            '
            For Each SettingsValue As System.Configuration.SettingsPropertyValue In DeserializedAppScopedSettingValues
                objectDirty = objectDirty Or MergeAndMaybeAddValue(Settings, SettingsValue, DesignTimeSettingInstance.SettingScope.Application, ExistingSettings, mergeMode, UIService)
            Next

            ' Check if we need to add/change any user scoped settings
            '
            For Each SettingsValue As System.Configuration.SettingsPropertyValue In DeserializedUserScopedSettingValues
                objectDirty = objectDirty Or MergeAndMaybeAddValue(Settings, SettingsValue, DesignTimeSettingInstance.SettingScope.User, ExistingSettings, mergeMode, UIService)
            Next

            ' Tell the user if we found some new settings...
            ' CONSIDER: Include the name of the settings that were added...
            If ((objectDirty And DirtyState.ValueAdded) = DirtyState.ValueAdded) AndAlso mergeMode = MergeValueMode.Prompt Then
                If UIService IsNot Nothing Then
                    UIService.ShowMessage(SR.GetString(SR.SD_NewValuesAdded), DesignerFramework.DesignUtil.GetDefaultCaption(VBPackage.Instance), System.Windows.Forms.MessageBoxButtons.OK)
                Else
                    System.Windows.Forms.MessageBox.Show(SR.GetString(SR.SD_NewValuesAdded), DesignerFramework.DesignUtil.GetDefaultCaption(VBPackage.Instance), System.Windows.Forms.MessageBoxButtons.OK)
                End If
            End If

            Return objectDirty
        End Function

        ''' <summary>
        ''' Get a DocData for the special file App.Config The file will be added to the project if it doesn't already
        ''' exist.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="Writeable">
        ''' If the DocData should be write:able, and a DocDataService is provider, all write:able files added to the
        ''' DocDataService will be checked out
        ''' </param>
        ''' <param name="DocDataService">If specified, the DesignerDocDataService to add/get this DocData to/from</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function GetAppConfigDocData(ByVal ServiceProvider As IServiceProvider, ByVal Hierarchy As IVsHierarchy, ByVal CreateIfNotExists As Boolean, ByVal Writeable As Boolean, Optional ByVal DocDataService As DesignerDocDataService = Nothing) As DocData
            Dim ProjSpecialFiles As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            Dim AppConfigDocData As DocData = Nothing

            If ProjSpecialFiles IsNot Nothing Then
                Dim AppConfigItemId As UInteger
                Dim AppConfigFileName As String = Nothing

                Dim Flags As UInteger
                If CreateIfNotExists Then
                    Flags = CUInt(__PSFFLAGS.PSFF_CreateIfNotExist Or __PSFFLAGS.PSFF_FullPath)
                Else
                    Flags = CUInt(__PSFFLAGS.PSFF_FullPath)
                End If

                Try
                    VSErrorHandler.ThrowOnFailure(ProjSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, Flags, AppConfigItemId, AppConfigFileName))
                Catch ex As System.Runtime.InteropServices.COMException When ex.ErrorCode = Interop.win.OLE_E_PROMPTSAVECANCELLED
                    Throw New System.ComponentModel.Design.CheckoutException(SR.GetString(SR.DFX_UnableToCheckout), ex)
                Catch ex As Exception When Not TypeOf ex Is System.ComponentModel.Design.CheckoutException
                    ' VsWhidbey 224145, ProjSpecialFiles.GetFile(create:=true) fails on vbexpress sku
                    AppConfigItemId = VSITEMID.NIL
                    Debug.Fail(String.Format("ProjSpecialFiles.GetFile (create={0}) failed: {1}", CreateIfNotExists, ex))
                End Try
                If AppConfigItemId <> VSITEMID.NIL Then
                    If DocDataService IsNot Nothing Then
                        Dim Access As FileAccess
                        If Writeable Then
                            Access = FileAccess.ReadWrite
                        Else
                            Access = FileAccess.Read
                        End If
                        Try
                            AppConfigDocData = DocDataService.GetFileDocData(AppConfigFileName, Access, Nothing)
                        Catch ex As System.ComponentModel.Design.CheckoutException
                            Throw
                        Catch Ex As Exception
                            Debug.Fail(String.Format("DocDataService.GetFileDocData threw exception: {0}", Ex))
                            Throw
                        End Try
                    Else
                        Try
                            AppConfigDocData = New DocData(ServiceProvider, AppConfigFileName)
                        Catch ex As Exception
                            Debug.Fail(String.Format("DocData constructor threw exception: {0}", ex))
                            Throw
                        End Try
                    End If
                End If
            End If

            If AppConfigDocData IsNot Nothing AndAlso AppConfigDocData.Buffer Is Nothing Then
                ' Here we have a problem - we need the native DocData to implement the IVsTextBuffer
                ' so we can use DocDataTextReaders/Writers... If we can't get that, we may as well throw
                ' an exception that tells the user that things are broken here!
                AppConfigDocData.Dispose()
                Throw New NotSupportedException(SR.GetString(SR.DFX_IncompatibleBuffer))
            End If
            Return AppConfigDocData
        End Function

        ''' <summary>
        ''' Serialize the given Settings object to the AppConfigDocData
        ''' </summary>
        ''' <param name="Settings"></param>
        ''' <param name="ClassName"></param>
        ''' <param name="NamespaceName"></param>
        ''' <param name="AppConfigDocData"></param>
        ''' <remarks></remarks>
        Friend Shared Sub Serialize(ByVal Settings As DesignTimeSettings, ByVal typeCache As SettingsTypeCache, ByVal valueCache As SettingsValueCache, ByVal ClassName As String, ByVal NamespaceName As String, ByVal AppConfigDocData As DocData, ByVal Hierarchy As IVsHierarchy, ByVal SynchronizeUserConfig As Boolean)
            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Serializing {0} settings to App.Config", Settings.Count)
            If Settings Is Nothing Then
                Debug.Fail("Can't serialize NULL settings instance!")
                Throw New ArgumentNullException("Settings")
            End If

            If ClassName = "" Then
                Debug.Fail("Must provide a valid class name!")
                Throw Common.CreateArgumentException("ClassName")
            End If

            If NamespaceName Is Nothing Then
                Debug.Fail("Must provide a valid namespace name!")
                Throw New ArgumentNullException("NamespaceName")
            End If

            If AppConfigDocData Is Nothing Then
                Debug.Fail("Can't serialize to a NULL DocData")
                Throw New ArgumentNullException("AppConfigDocData")
            End If

            Dim AppConfigReader As DocDataTextReader = Nothing
            Dim AppConfigWriter As DocDataTextWriter = Nothing

            Dim ConfigHelperService As New ConfigurationHelperService(AddressOf typeCache.TypeTransformer)
            Dim FullyQualifiedClassName As String = ProjectUtils.FullyQualifiedClassName(NamespaceName, ClassName)
            AppConfigSerializer.Serialize(Settings, typeCache, valueCache, ConfigHelperService.GetSectionName(FullyQualifiedClassName, String.Empty), AppConfigDocData, Hierarchy, SynchronizeUserConfig)
        End Sub


        ''' <summary>
        ''' Write out any and all changes to the app.config file
        ''' </summary>
        ''' <param name="Settings"></param>
        ''' <param name="typeCache"></param>
        ''' <param name="valueCache"></param>
        ''' <param name="SectionName"></param>
        ''' <param name="AppConfigDocData"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ShouldSynchronizeUserConfig"></param>
        ''' <remarks></remarks>
        Private Shared Sub Serialize(ByVal Settings As DesignTimeSettings, ByVal typeCache As SettingsTypeCache, ByVal valueCache As SettingsValueCache, ByVal SectionName As String, ByVal AppConfigDocData As DocData, ByVal Hierarchy As IVsHierarchy, ByVal ShouldSynchronizeUserConfig As Boolean)
            ' Populate settingspropertyvalue collections for user & application scoped settings respectively
            Dim ExistingUserScopedSettings As New System.Configuration.SettingsPropertyValueCollection
            Dim ExistingApplicationScopedSettings As New System.Configuration.SettingsPropertyValueCollection
            Dim ExistingConnectionStringSettings As New System.Configuration.ConnectionStringSettingsCollection
            Dim AddedInstances As New Specialized.StringCollection()

            Dim ConfigHelperService As New ConfigurationHelperService(AddressOf typeCache.TypeTransformer)
            Dim valueSerializer As New SettingsValueSerializer()


            For Each Instance As DesignTimeSettingInstance In Settings
                Dim settingType As System.Type = typeCache.GetSettingType(Instance.SettingTypeName)
                Dim settingValue As Object = Nothing
                If settingType IsNot Nothing Then
                    settingValue = valueCache.GetValue(settingType, Instance.SerializedValue)
                End If

                If Instance.Name <> "" AndAlso settingType IsNot Nothing AndAlso settingValue IsNot Nothing Then
                    ' Let's check for connection strings...

                    If settingType Is GetType(SerializableConnectionString) Then
                        ' Yepp! Add to existing connection strings...
                        Dim scs As SerializableConnectionString = DirectCast(settingValue, SerializableConnectionString)
                        If scs IsNot Nothing Then
                            Dim conStringSetting As New ConnectionStringSettings(Instance.Name, scs.ConnectionString, scs.ProviderName)
                            ExistingConnectionStringSettings.Add(conStringSetting)
                        End If
                    Else
                        Dim Prop As New SettingsProperty(Instance.Name)
                        Prop.PropertyType = settingType
                        Prop.SerializeAs = ConfigHelperService.GetSerializeAs(Prop.PropertyType)
                        Dim PropVal As New SettingsPropertyValue(Prop)
                        PropVal.SerializedValue = Instance.SerializedValue
                        If Instance.Scope = DesignTimeSettingInstance.SettingScope.Application Then
                            ExistingApplicationScopedSettings.Add(PropVal)
                        Else
                            ExistingUserScopedSettings.Add(PropVal)
                        End If
                    End If
                End If
            Next

            ' Let us populate the settings with values from app.config file 
            Dim configFileMap As New ExeConfigurationFileMap

            configFileMap.ExeConfigFilename = AppConfigDocData.Name
            ConfigHelperService.WriteConnectionStrings(AppConfigDocData.Name, AppConfigDocData, SectionName, ExistingConnectionStringSettings)
            ConfigHelperService.WriteSettings(configFileMap, ConfigurationUserLevel.None, AppConfigDocData, SectionName, True, ExistingUserScopedSettings)
            ConfigHelperService.WriteSettings(configFileMap, ConfigurationUserLevel.None, AppConfigDocData, SectionName, False, ExistingApplicationScopedSettings)

            ' Maybe synchronize user config files...
            If ShouldSynchronizeUserConfig Then
                Try
                    Debug.Assert(Hierarchy IsNot Nothing, "Must have an IVsHierarchy in order to be able to sync user config files!")
                    SynchronizeUserConfig(SectionName, Hierarchy, ConfigHelperService, Settings, AppConfigDocData)
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                    ' Can't do very much here - but we shouldn't abort the save!
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Synchronize (remove) any entries in user.config that the settings designer doesn't consider to be part of this
        ''' settings class's user scoped settings collection
        ''' </summary>
        ''' <param name="SectionName"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ConfigHelperService"></param>
        ''' <param name="Settings"></param>
        ''' <param name="AppConfigDocData"></param>
        ''' <remarks></remarks>
        Private Shared Sub SynchronizeUserConfig(ByVal SectionName As String, ByVal Hierarchy As IVsHierarchy, ByVal ConfigHelperService As ConfigurationHelperService, ByVal Settings As DesignTimeSettings, ByVal AppConfigDocData As DocData)
            ' We list all the USER scoped settings that we know about and set the value to true or false depending
            ' on if the setting is roaming... The set of known settings is used both when scrubbing the file used
            ' in VSHost:ed and stand-alone sessions, so we only need to do it once...
            Dim SettingsTheDesignerKnownsAbout As New Generic.Dictionary(Of String, Boolean)

            For Each dsi As DesignTimeSettingInstance In Settings
                If dsi.Scope = DesignTimeSettingInstance.SettingScope.User Then
                    SettingsTheDesignerKnownsAbout(dsi.Name) = dsi.Roaming
                End If
            Next

            ' We need to synchronize the user config in two locations, since they are different depending on if the 
            ' application runs under VSHost or not...
            SynchronizeUserConfig(True, SectionName, Hierarchy, ConfigHelperService, SettingsTheDesignerKnownsAbout, AppConfigDocData)
            SynchronizeUserConfig(False, SectionName, Hierarchy, ConfigHelperService, SettingsTheDesignerKnownsAbout, AppConfigDocData)
        End Sub

        ''' <summary>
        ''' Synchronize (remove) any entries in user.config that the settings designer doesn't consider to be part of this
        ''' settings class's user scoped settings collection. Checks either the set of files used when running under VSHost or
        ''' when running "stand-alone"
        ''' </summary>
        ''' <param name="UnderVsHost"></param>
        ''' <param name="SectionName"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ConfigHelperService"></param>
        ''' <param name="SettingsTheDesignerKnownsAbout"></param>
        ''' <param name="AppConfigDocData"></param>
        ''' <remarks></remarks>
        Private Shared Sub SynchronizeUserConfig(ByVal UnderVsHost As Boolean, ByVal SectionName As String, ByVal Hierarchy As IVsHierarchy, ByVal ConfigHelperService As ConfigurationHelperService, ByVal SettingsTheDesignerKnownsAbout As Generic.Dictionary(Of String, Boolean), ByVal AppConfigDocData As DocData)
            Dim hierSp As IServiceProvider = Common.Utils.ServiceProviderFromHierarchy(Hierarchy)
            Dim project As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(Hierarchy)

            For Each BuildConfig As EnvDTE.Configuration In project.ConfigurationManager
                ' Grab the relevant file locations
                Dim map As New ExeConfigurationFileMap
                Try
                    map.ExeConfigFilename = AppConfigDocData.Name
                    map.LocalUserConfigFilename = ConfigHelperService.GetUserConfigurationPath(hierSp, project, ConfigurationUserLevel.PerUserRoamingAndLocal, UnderVsHost, BuildConfig)
                    map.RoamingUserConfigFilename = ConfigHelperService.GetUserConfigurationPath(hierSp, project, ConfigurationUserLevel.PerUserRoaming, UnderVsHost, BuildConfig)
                Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                    ' Can't really do anything - synchronize will fail...
                    Return
                End Try

                If map.LocalUserConfigFilename Is Nothing OrElse map.RoamingUserConfigFilename Is Nothing Then
                    ' We can't scrub the directories if we can't get the directory names
                    Return
                End If

                ' If we find a setting in the merged view that the designer doesn't know anything about,
                ' we need to "scrub" the user.config files...
                Dim scrubNeeded As Boolean = False

                ' While we are going through this, add the existing settings to a local and roaming user
                ' collection depending on if the roaming flag is set...
                Dim LocalUserSettings As New SettingsPropertyValueCollection
                Dim RoamingUserSettings As New SettingsPropertyValueCollection

                If SettingsTheDesignerKnownsAbout.Count = 0 Then
                    ' If the settings designer doesn't know anything about any settings, then there is a very high risk that there
                    ' isn't a section handler declared for the user scoped settings for this class. That means that the configuration
                    ' system won't find any settings in local/roaming user config files, even if they are in there (it will only find
                    ' settings if the section handler is declared)
                    '
                    ' We get around this by "scrubbing" the local/roaming user user.config files by serializing an empty 
                    ' SettingsPropertyValueCollection, since that effectively removes any old garbage that may be in there...
                    scrubNeeded = True
                Else
                    ' If we have one or more settings, the section handler should have been added in app.config, so we should
                    ' be able to find any old settings hanging around in the user.config file
                    Dim MergedViewSettings As System.Configuration.SettingsPropertyValueCollection = ConfigHelperService.ReadSettings(map, ConfigurationUserLevel.PerUserRoamingAndLocal, AppConfigDocData, SectionName, True, New SettingsPropertyCollection)

                    ' Strip out any and all settings that are not included in our "known" set of settings...
                    For Each prop As SettingsPropertyValue In MergedViewSettings
                        Dim roaming As Boolean
                        If Not SettingsTheDesignerKnownsAbout.TryGetValue(prop.Name, roaming) Then
                            ' Unknown setting - need to scrub!
                            scrubNeeded = True
                        Else
                            ' Known setting - put it in the appropriate bucket of settings in case we need to
                            ' scrub later on...
                            If roaming Then
                                RoamingUserSettings.Add(prop)
                            Else
                                LocalUserSettings.Add(prop)
                            End If
                        End If
                    Next
                End If

                If scrubNeeded Then
                    ' The "scrub" is really only us writing out the appropriate set of settingpropertyvalues. Anything that 
                    ' we didn't know about should already have been remove by now...
                    ConfigHelperService.WriteSettings(map, ConfigurationUserLevel.PerUserRoaming, AppConfigDocData, SectionName, True, RoamingUserSettings)
                    ConfigHelperService.WriteSettings(map, ConfigurationUserLevel.PerUserRoamingAndLocal, AppConfigDocData, SectionName, True, LocalUserSettings)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Load the contents of the stream pointed to by Reader into a XML DOM Document
        ''' </summary>
        ''' <param name="Reader"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function LoadAppConfigDocument(ByVal Reader As TextReader) As XmlDocument
            Dim AppConfigXmlDoc As New XmlDocument()
            AppConfigXmlDoc.PreserveWhitespace = False
            ' Load App.Config into XML Doc
            Dim XmlAppConfigReader As New XmlTextReader(Reader)

            ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
            XmlAppConfigReader.DtdProcessing = DtdProcessing.Prohibit
            XmlAppConfigReader.WhitespaceHandling = WhitespaceHandling.All
            AppConfigXmlDoc.Load(XmlAppConfigReader)
            Return AppConfigXmlDoc
        End Function

        ''' <summary>
        ''' If the value in the app.config file differs from the value in the .settings file,
        ''' or the scope has been changed,
        ''' propmpt the user if (s)he wants to update the value in the .settings file
        ''' </summary>
        ''' <param name="DeserializedPropertyValue"></param>
        ''' <param name="Scope">The scope in which the deserialized property value was found</param>
        ''' <param name="ExistingSettings"></param>
        ''' <param name="UIService"></param>
        ''' <returns>True if a new instance was added to the the root component</returns>
        ''' <remarks></remarks>
        Private Shared Function MergeAndMaybeAddValue(ByVal Settings As DesignTimeSettings, ByVal DeserializedPropertyValue As SettingsPropertyValue, ByVal Scope As DesignTimeSettingInstance.SettingScope, ByVal ExistingSettings As Dictionary(Of String, DesignTimeSettingInstance), ByVal mergeMode As MergeValueMode, ByVal UIService As System.Windows.Forms.Design.IUIService) As DirtyState
            If Not ExistingSettings.ContainsKey(DeserializedPropertyValue.Name) Then
                If Settings.IsValidName(DeserializedPropertyValue.Name) Then
                    ' This is a new setting - let's silently add it to our settings as new setting of type "string"
                    Debug.Assert(DeserializedPropertyValue.Deserialized = False)
                    Debug.Assert(TypeOf DeserializedPropertyValue.SerializedValue Is String OrElse DeserializedPropertyValue.SerializedValue Is Nothing, "Unknown serialized value type - expected ")
                    Dim NewInstance As New DesignTimeSettingInstance
                    NewInstance.SetScope(Scope)
                    NewInstance.SetName(DeserializedPropertyValue.Name)
                    NewInstance.SetSettingTypeName(GetType(String).FullName)
                    If DeserializedPropertyValue.SerializedValue IsNot Nothing Then
                        NewInstance.SetSerializedValue(DirectCast(DeserializedPropertyValue.SerializedValue, String))
                    End If
                    Settings.Add(NewInstance)

                    ' We have added a new instance!
                    Return DirtyState.ValueAdded
                End If
            Else
                ' This was an existing setting
                Debug.Assert(DeserializedPropertyValue.Deserialized = False OrElse TypeOf DeserializedPropertyValue.SerializedValue Is String, "Unknown deserialied type!")
                Dim Instance As DesignTimeSettingInstance = ExistingSettings(DeserializedPropertyValue.Name)
                ' Grab the locale-independent representation of this value...
                If DeserializedPropertyValue.Property.PropertyType Is Nothing Then
                    DeserializedPropertyValue.Property.PropertyType = GetType(String)
                End If
                Dim serializer As New SettingsValueSerializer
                Dim serializedAppConfigValue As String = serializer.Normalize(DirectCast(DeserializedPropertyValue.SerializedValue, String), DeserializedPropertyValue.Property.PropertyType)
                If Instance.Scope <> Scope OrElse Not String.Equals(Instance.SerializedValue, serializedAppConfigValue, System.StringComparison.Ordinal) Then
                    ' We have new mismatch between what's in the app.config file and what's in the .settings file - 
                    ' prompt the user!
                    Return QueryReplaceValue(Settings, Instance, serializedAppConfigValue, Scope, mergeMode, UIService)
                Else
                    ' This instance was already known to us!
                    Return DirtyState.NoChange
                End If
            End If
        End Function

        ''' <summary>
        ''' Ask the user if (s)he wants to replace the current value of a setting with a new value found in the app.config file...
        ''' </summary>
        ''' <param name="Settings">The owner of the setting</param>
        ''' <param name="Instance">The existing setting instance</param>
        ''' <param name="SerializedAppConfigValue">The serialized representation of the value found in the app.config file</param>
        ''' <param name="AppConfigScope">The scope of the value found in the app.cofig file</param>
        ''' <param name="UIService">An option UI service to help us pop up a nice dialog</param>
        ''' <remarks></remarks>
        Private Shared Function QueryReplaceValue(ByVal Settings As DesignTimeSettings, ByVal Instance As DesignTimeSettingInstance, ByVal SerializedAppConfigValue As String, ByVal AppConfigScope As DesignTimeSettingInstance.SettingScope, ByVal mergeMode As MergeValueMode, ByVal UIService As IUIService) As DirtyState
            Dim ReplaceValue As Boolean
            Select Case mergeMode
                Case MergeValueMode.UseAppConfigFileValue
                    ReplaceValue = True
                Case MergeValueMode.UseSettingsFileValue
                    ReplaceValue = False
                Case Else
                    Debug.Assert(mergeMode = MergeValueMode.Prompt, "Unknown MergeValueMode: " & mergeMode)
                    If UIService IsNot Nothing Then
                        ReplaceValue = (UIService.ShowMessage(SR.GetString(SR.SD_ReplaceValueWithAppConfigValue, Instance.SerializedValue, SerializedAppConfigValue), SR.GetString(SR.SD_ReplaceValueWithAppConfigValueTitle, Instance.Name), System.Windows.Forms.MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.Yes)
                    Else
                        ReplaceValue = (System.Windows.Forms.MessageBox.Show(SR.GetString(SR.SD_ReplaceValueWithAppConfigValueTitle, Instance.Name) & VisualBasic.vbNewLine & VisualBasic.vbNewLine & SR.GetString(SR.SD_ReplaceValueWithAppConfigValue, Instance.SerializedValue, SerializedAppConfigValue), DesignerFramework.DesignUtil.GetDefaultCaption(VBPackage.Instance), System.Windows.Forms.MessageBoxButtons.YesNo) = System.Windows.Forms.DialogResult.Yes)
                    End If
            End Select

            If ReplaceValue Then
                ' The user wants to keep the value in the app.config file.... and who are we to 
                ' say no to that....

                ' When the designer is loaded, it is normally called as part of IDLE processing.
                ' When we popped up the dialog box, the designer actually showed up with it's old values
                ' Since ComponentChange events are not fired during loading, anything we change here will not be shown in 
                ' the designer view.... ComponentAdded/Removed *are* fired however, so if we remove, change the setting
                ' and add it back to the loader host, the changes will be visible...
                Settings.Remove(Instance)
                Instance.SetSerializedValue(DirectCast(SerializedAppConfigValue, String))
                Instance.SetScope(AppConfigScope)
                Settings.Add(Instance)
                Return DirtyState.ValueModified
            Else
                Return DirtyState.NoChange
            End If
        End Function
    End Class
End Namespace
