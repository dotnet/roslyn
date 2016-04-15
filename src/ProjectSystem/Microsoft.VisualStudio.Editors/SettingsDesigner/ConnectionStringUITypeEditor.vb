' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.drawing.Design
Imports Microsoft.VisualStudio.Data.Core
Imports Microsoft.VisualStudio.Data.Services
Imports Microsoft.VisualStudio.Data.Services.SupportEntities
Imports Microsoft.VisualStudio.DataTools.Interop
Imports Microsoft.VSDesigner.VSDesignerPackage
Imports Microsoft.VSDesigner.Data.Local

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Simple UI type editor that launches a IVsDataConnectionDialog dialog to let 
    ''' the user create/edit connection strings
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ConnectionStringUITypeEditor
        Inherits UITypeEditor

        ''' <summary>
        ''' This is a modal dialog...
        ''' </summary>
        ''' <param name="context">The context parameter is ignored...</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function GetEditStyle(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
            Return UITypeEditorEditStyle.Modal
        End Function

        ''' <summary>
        ''' Edit the actual value
        ''' </summary>
        ''' <param name="context">The context parameter is ignored...</param>
        ''' <param name="ServiceProvider">
        ''' The following services are expected to be available from the service provider:
        '''   IVsDataConnectionDialogFactory
        '''   IVsDataProviderManager
        '''   IDTAdoDotNetProviderMapper
        '''   IUIService (will work without it)
        ''' </param>
        ''' <param name="oValue"></param>
        ''' <returns></returns>
        ''' <remarks>Does not use the IWindowsFormsEditorService service to show it's dialog...</remarks>
        Public Overrides Function EditValue(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal ServiceProvider As System.IServiceProvider, ByVal oValue As Object) As Object
            Dim dataConnectionDialogFactory As IVsDataConnectionDialogFactory = DirectCast(ServiceProvider.GetService(GetType(IVsDataConnectionDialogFactory)), IVsDataConnectionDialogFactory)

            If dataConnectionDialogFactory Is Nothing Then
                Debug.Fail("Couldn't get the IVsDataConnectionDialogFactory service from our provider...")
                Return oValue
            End If

            Dim providerManager As IVsDataProviderManager = DirectCast(ServiceProvider.GetService(GetType(IVsDataProviderManager)), IVsDataProviderManager)
            If providerManager Is Nothing Then
                Debug.Fail("Couldn't get the IVsProviderManager service from our provider")
                Return oValue
            End If

            Dim providerMapper As IDTAdoDotNetProviderMapper = DirectCast(ServiceProvider.GetService(GetType(IDTAdoDotNetProviderMapper)), IDTAdoDotNetProviderMapper)
            If providermapper Is Nothing Then
                Debug.Fail("Couldn't get the IDTAdoDotNetProviderMapper service from our provider")
                Return oValue
            End If

            Dim dataConnectionDialog As IVsDataConnectionDialog = dataConnectionDialogFactory.CreateConnectionDialog()

            If dataConnectionDialog Is Nothing Then
                Debug.Fail("Failed to get a IVsDataConnectionDialog from the IVsDataConnectionDialogFactory!?")
                Return oValue
            End If

            ' If this is a local data file, we've gotta let the connecitonstrinconverter service
            ' do it's magic to convert the path. Trying to convert a non-local data connection string
            ' should be a no-op, so we should be safe if we just try to get hold of the required services
            ' and give it a try...
            Dim dteProj As EnvDTE.Project = Nothing
            Dim connectionStringConverter As Microsoft.VSDesigner.Data.Local.IConnectionStringConverterService = _
                DirectCast(ServiceProvider.GetService(GetType(Microsoft.VSDesigner.Data.Local.IConnectionStringConverterService)), Microsoft.VSDesigner.Data.Local.IConnectionStringConverterService)
            If connectionStringConverter IsNot Nothing Then
                ' The SettingsDesignerLoader should have added the project item as a service in case someone needs to 
                ' get hold of it....
                Dim dteProjItem As EnvDTE.ProjectItem = Nothing
                dteProjItem = DirectCast(ServiceProvider.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
                If dteProjItem IsNot Nothing Then
                    dteProj = dteProjItem.ContainingProject
                Else
                    Debug.Fail("We failed to get the EnvDTE.ProjectItem service from our service provider. The settings designer loader should have added it...")
                End If
            End If

            dataConnectionDialog.AddSources(AddressOf New DataConnectionDialogFilterer(dteProj).IsCombinationSupported)
            dataConnectionDialog.LoadSourceSelection()
            dataConnectionDialog.LoadProviderSelections()

            Dim value As SerializableConnectionString
            Try
                value = TryCast(oValue, SerializableConnectionString)

                ' We keep track of if there was sensitive data in the string when we were called - if not, we've gotta prompt the user
                ' about the potential security implications if (s)he adds sensitive data
                Dim containedSensitiveData As Boolean = False

                ' If we have a value coming in, we should feed the dialog with this value
                If value IsNot Nothing Then
                    ' The normalized connection string contains the connection string after the connection string converter is done
                    ' munching it.
                    Dim normalizedConnectionString As String = Nothing
                    Try
                        If connectionStringConverter IsNot Nothing AndAlso dteProj IsNot Nothing AndAlso value.ProviderName <> "" Then
                            normalizedConnectionString = connectionStringConverter.ToDesignTime(dteProj, value.ConnectionString, value.ProviderName)
                        End If
                    Catch ex As ArgumentException
                    Catch ex As ConnectionStringConverterServiceException
                        ' Well, the user may very well type garbage into the connection string text box
                    Finally
                        ' If we couldn't find the service, or if something else went wrong, we fall back 
                        ' to the connection string as it is showing in the designer...
                        If normalizedConnectionString Is Nothing Then
                            normalizedConnectionString = value.ConnectionString
                        End If
                    End Try

                    Dim providerGuid As Guid = Guid.Empty
                    If value.ProviderName <> "" Then
                        ' Get the provider GUID (if any)
                        providerGuid = GetGuidFromInvariantProviderName(providerMapper, value.ProviderName, normalizedConnectionString, False)
                    End If

                    ' If we have a provider, we can feed the dialog with the initial values. 
                    If Not providerGuid.Equals(Guid.Empty) Then
                        dataConnectionDialog.LoadExistingConfiguration(providerGuid, normalizedConnectionString, False)

                        Dim oldConnectionStringProperties As IVsDataConnectionProperties = GetConnectionStringProperties(providerManager, providerGuid, normalizedConnectionString)
                        If oldConnectionStringProperties IsNot Nothing AndAlso ContainsSensitiveData(oldConnectionStringProperties) Then
                            ' If we already had sensitive data in the string comming in to this function, we don't have to prompt again...
                            containedSensitiveData = True
                        End If
                    ElseIf normalizedConnectionString <> "" Then
                        dataConnectionDialog.SafeConnectionString = normalizedConnectionString
                    End If
                End If

                If (dataConnectionDialog.ShowDialog() AndAlso dataConnectionDialog.DisplayConnectionString <> "") Then
                    ' If the user press OK and we have a connection string, lets return this new value!
                    dataConnectionDialog.SaveProviderSelections()
                    If (dataConnectionDialog.SaveSelection) Then
                        dataConnectionDialog.SaveSourceSelection()
                    End If
                    Dim newValue As New SerializableConnectionString
                    newValue.ProviderName = GetInvariantProviderNameFromGuid(providerMapper, dataConnectionDialog.SelectedProvider)
                    newValue.ConnectionString = GetConnectionString(ServiceProvider, dataConnectionDialog, Not containedSensitiveData)
                    If dteProj IsNot Nothing AndAlso connectionStringConverter IsNot Nothing Then
                        ' Go back to the runtime representation of the string...
                        newValue.ConnectionString = connectionStringConverter.ToRunTime(dteProj, newValue.ConnectionString, newValue.ProviderName)
                    End If

                    Return newValue
                Else
                    ' Well, we better return the old value...
                    Return oValue
                End If
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                'Just throw the exception, caller should handle this.
                Throw
            Finally
                If dataConnectionDialog IsNot Nothing Then
                    dataConnectionDialog.Dispose()
                End If
            End Try
            Return oValue
        End Function

        ''' <summary>
        ''' Determine if a given connection string contains sensitive information.
        ''' </summary>
        ''' <param name="ProviderManager"></param>
        ''' <param name="DataProvider"></param>
        ''' <param name="ConnectionString"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ContainsSensitiveData(ByVal ProviderManager As IVsDataProviderManager, ByVal DataProvider As Guid, ByVal ConnectionString As String) As Boolean
            If ConnectionString = "" Then
                Return False
            End If

            Try
                Dim DataConnectionProperties As IVsDataConnectionProperties = GetConnectionStringProperties(ProviderManager, DataProvider, ConnectionString)
                Return ContainsSensitiveData(DataConnectionProperties)
            Catch ex As Exception
                Common.Utils.RethrowIfUnrecoverable(ex)
            End Try
            ' The secure & safe assumption is that it does contain sensitive data
            Return True
        End Function

        ''' <summary>
        ''' Determine if a given connection string contains sensitive information.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="DataProvider"></param>
        ''' <param name="ConnectionString"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ContainsSensitiveData(ByVal ServiceProvider As IServiceProvider, ByVal DataProvider As Guid, ByVal ConnectionString As String) As Boolean
            Dim providerManager As IVsDataProviderManager = DirectCast(ServiceProvider.GetService(GetType(IVsDataProviderManager)), IVsDataProviderManager)
            If providerManager IsNot Nothing Then
                Return ContainsSensitiveData(providerManager, DataProvider, ConnectionString)
            Else
                Debug.Fail("Failed to get IVsDataProviderManager from provided ServiceProvider")
            End If
            ' The secure & safe assumption is that it does contain sensitive data
            Return True
        End Function

        ''' <summary>
        ''' Depending on if the user wants to store the password or not, we get the connection string 
        ''' in slightly different ways...
        ''' </summary>
        ''' <param name="Dialog">The IVsDataConnectionDialog instance to get the connection string from</param>
        ''' <returns>
        ''' Unencrypted ConnectionString with or without the user-entered password depending on if
        ''' there exists sensitive information in the string and whether the user chooses to persist it
        '''</returns>
        ''' <remarks></remarks>
        Private Function GetConnectionString(ByVal ServiceProvider As IServiceProvider, ByVal Dialog As IVsDataConnectionDialog, ByVal PromptIfContainsSensitiveData As Boolean) As String

            If Dialog Is Nothing Then
                Throw New ArgumentNullException("Dialog")
            End If

            If ServiceProvider Is Nothing Then
                Throw New ArgumentNullException("ServiceProvider")
            End If

            Dim SafeConnectionString As String = Dialog.SafeConnectionString

            If SafeConnectionString Is Nothing Then
                Debug.Fail("Failed to get SafeConnectionString from IVsDataConnectionDialog (got a NULL value:()")
                Return ""
            End If

            Dim RawConnectionString As String = DataProtection.DecryptString(Dialog.EncryptedConnectionString)
            If ContainsSensitiveData(ServiceProvider, Dialog.SelectedProvider, RawConnectionString) Then
                If Not PromptIfContainsSensitiveData OrElse _
                       DesignerFramework.DesignerMessageBox.Show(ServiceProvider, _
                                                             SR.GetString(SR.SD_IncludeSensitiveInfoInConnectionStringWarning), _
                                                             DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), _
                                                             Windows.Forms.MessageBoxButtons.YesNo, _
                                                             Windows.Forms.MessageBoxIcon.Warning, _
                                                             Windows.Forms.MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes _
                Then
                    Return RawConnectionString
                End If
            End If

            Return SafeConnectionString
        End Function

        ''' <summary>
        ''' Get connection string properties for a specific provider/connectoin string
        ''' </summary>
        ''' <param name="ProviderManager"></param>
        ''' <param name="ProviderGUID"></param>
        ''' <param name="ConnectionString"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetConnectionStringProperties(ByVal ProviderManager As IVsDataProviderManager, ByVal ProviderGUID As Guid, ByVal ConnectionString As String) As IVsDataConnectionProperties
            Dim provider As IVsDataProvider = Nothing
            If ProviderManager.Providers.ContainsKey(ProviderGUID) Then
                provider = ProviderManager.Providers(ProviderGUID)
            End If
            Dim DataConnectionProperties As IVsDataConnectionProperties
            DataConnectionProperties = TryCast(provider.CreateObject(GetType(IVsDataConnectionProperties)), IVsDataConnectionProperties)
            If DataConnectionProperties IsNot Nothing Then
                DataConnectionProperties.Parse(ConnectionString)
            End If
            Return DataConnectionProperties
        End Function

        ''' <summary>
        ''' Does the given connection string contain sensitive data?
        ''' </summary>
        ''' <param name="ConnectionProperties"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ContainsSensitiveData(ByVal ConnectionProperties As IVsDataConnectionProperties) As Boolean
            If ConnectionProperties Is Nothing Then
                Debug.Fail("We can't tell if it contains sensitive data if we didn't get a bag of properties!")
                Throw New ArgumentNullException()
            End If

            ' If the safe string's length is less than the full string's length, then it must strip something sensitive out...
            Return ConnectionProperties.ToSafeString().Trim.Length < ConnectionProperties.ToString().Trim.Length()
        End Function


#Region "Mapping provider GUIDs <-> display names"

        Private Shared Function GetInvariantProviderNameFromGuid(ByVal ProviderMapper As IDTAdoDotNetProviderMapper, ByVal providerGuid As Guid) As String
            If ProviderMapper Is Nothing Then
                Debug.Fail("Failed to get a IDTAdoDotNetProviderMapper")
                Return providerGuid.ToString()
            End If

            Dim invariantName As String = providerMapper.MapGuidToInvariantName(providerGuid)
            If invariantName Is Nothing Or invariantName = "" Then
                Debug.Fail(String.Format("{0} is not an ADO.NET provider", providerGuid))
                Return providerGuid.ToString()
            End If

            Return invariantName
        End Function

        Private Shared Function GetGuidFromInvariantProviderName(ByVal ProviderMapper As IDTAdoDotNetProviderMapper, ByVal providerName As String, ByVal ConnectionString As String, ByVal EncryptedString As Boolean) As Guid
            If providerMapper Is Nothing Then
                Debug.Fail("Failed to get a IDTAdoDotNetProviderMapper")
                Return Guid.Empty
            End If

            Dim providerGuid as Guid = providerMapper.MapInvariantNameToGuid(providerName, connectionString, encryptedString)
            If providerGuid.Equals(Guid.Empty) Then
                Debug.Fail(String.Format("Couldn't find GUID for provider {0}", providerName))
                Try
                    ' Let's see if the provided name is a valid Guid?
                    Return New Guid(providerName)
                Catch ex As FormatException
                    ' Nope...
                End Try
                Return Guid.Empty
            End If

            Return providerGuid
        End Function
#End Region

        Private Class DataConnectionDialogFilterer
            Private _targetProject As EnvDTE.Project

            Public Sub New(ByVal project As EnvDTE.Project)
                Me._targetProject = project
            End Sub

            Public Function IsCombinationSupported(ByVal source As Guid, ByVal provider As Guid) As Boolean
                Return Microsoft.VSDesigner.Data.DataProviderProjectControl.IsProjectSupported(provider, Me._targetProject)
            End Function
        End Class

    End Class
End Namespace
