'------------------------------------------------------------------------------
' <copyright from='2003' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Imports System
Imports System.CodeDom.Compiler
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Configuration
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell.design.serialization
Imports Microsoft.VisualStudio.TextManager.Interop

Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Designer loader for settings files
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SettingsDesignerLoader
        Inherits DesignerFramework.BaseDesignerLoader
        Implements INameCreationService, IVsDebuggerEvents

#Region "Private fields"
        ''' References to the app.config file (if any)
        Private m_AppConfigDocData As DocData

        Private m_ServiceProvider As ServiceProvider

        Private m_Flushing As Boolean

        Private Const prjKindVenus As String = "{E24C65DC-7377-472b-9ABA-BC803B73C61A}"

        ' Set flag if we make changes to the settings object during load that should
        ' set the docdata to dirty immediately after we have loaded.
        Private m_ModifiedDuringLoad As Boolean

        ' Cached IVsDebugger from shell in case we don't have a service provider at
        ' shutdown so we can undo our event handler
        Private m_VsDebugger As IVsDebugger
        Private m_VsDebuggerEventsCookie As UInteger

        ' Current Debug mode
        Private m_currentDebugMode As Shell.Interop.DBGMODE = DBGMODE.DBGMODE_Design

        ' BUGFIX: Dev11#45255 
        ' Hook up to build events so we can enable/disable the property 
        ' page while building
        Private WithEvents m_buildEvents As EnvDTE.BuildEvents
        Private m_readOnly As Boolean
#End Region

#Region "Base class overrides"

        ''' <summary>
        ''' Initialize the designer loader. This is called just after begin load, so we shoud
        ''' have a loader host here.
        ''' This is the place where we add services!
        ''' NOTE: Remember to call RemoveService on any service object we don't own, when the Loader is disposed
        '''  Otherwise, the service container will dispose those objects.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            Dim projectItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)

            ' Add my services
            LoaderHost.AddService(GetType(INameCreationService), Me)
            LoaderHost.AddService(GetType(ComponentSerializationService), New DesignerFramework.GenericComponentSerializationService(Nothing))

            ' Add our dynamic type service...
            Dim typeDiscoveryService As ITypeDiscoveryService = Nothing
            Dim dynamicTypeService As Microsoft.VisualStudio.Shell.Design.DynamicTypeService = _
                DirectCast(m_ServiceProvider.GetService(GetType(Microsoft.VisualStudio.Shell.Design.DynamicTypeService)), Microsoft.VisualStudio.Shell.Design.DynamicTypeService)
            If dynamicTypeService IsNot Nothing Then
                typeDiscoveryService = dynamicTypeService.GetTypeDiscoveryService(Me.VsHierarchy, Me.ProjectItemid)
            End If
         
            Dim cm As EnvDTE.CodeModel = Nothing
            If projectItem IsNot Nothing AndAlso projectItem.ContainingProject IsNot Nothing AndAlso projectItem.ContainingProject.CodeModel IsNot Nothing Then
                cm = projectItem.ContainingProject.CodeModel
            End If

            ' Try to add our typename resolution component
            If cm IsNot Nothing Then
                LoaderHost.AddService(GetType(SettingTypeNameResolutionService), New SettingTypeNameResolutionService(cm.Language))
            Else
                LoaderHost.AddService(GetType(SettingTypeNameResolutionService), New SettingTypeNameResolutionService(""))
            End If

            ' Add settings type cache...
            If cm IsNot Nothing Then
                LoaderHost.AddService(GetType(SettingsTypeCache), New SettingsTypeCache(Me.VsHierarchy, Me.ProjectItemid, dynamicTypeService.GetTypeResolutionService(VsHierarchy, ProjectItemid), cm.IsCaseSensitive))
            Else
                LoaderHost.AddService(GetType(SettingsTypeCache), New SettingsTypeCache(Me.VsHierarchy, Me.ProjectItemid, dynamicTypeService.GetTypeResolutionService(VsHierarchy, ProjectItemid), True))
            End If
            LoaderHost.AddService(GetType(SettingsValueCache), New SettingsValueCache(System.Globalization.CultureInfo.InvariantCulture))

            ' Listen for change notifications
            Dim ComponentChangeService As IComponentChangeService = CType(GetService(GetType(IComponentChangeService)), IComponentChangeService)
            AddHandler ComponentChangeService.ComponentAdded, AddressOf Me.ComponentAddedHandler
            AddHandler ComponentChangeService.ComponentChanging, AddressOf Me.ComponentChangingHandler
            AddHandler ComponentChangeService.ComponentChanged, AddressOf Me.ComponentChangedHandler
            AddHandler ComponentChangeService.ComponentRemoved, AddressOf Me.ComponentRemovedHandler

        End Sub

        ''' <summary>
        ''' Initialize this instance
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="Hierarchy">Hierarchy (project) for item to load</param>
        ''' <param name="ItemId">ItemId in Hierarchy to load</param>
        ''' <param name="punkDocData">Document data to load</param>
        ''' <remarks></remarks>
        Friend Overrides Sub InitializeEx(ByVal ServiceProvider As Shell.ServiceProvider, ByVal moniker As String, ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal punkDocData As Object)
            MyBase.InitializeEx(ServiceProvider, moniker, Hierarchy, ItemId, punkDocData)

            m_ServiceProvider = ServiceProvider

            SetSingleFileGenerator()
        End Sub

        ''' <summary>
        ''' Overrides base Dispose.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Dispose()
            'Remove services we proffered.
            '
            'Note: LoaderHost.RemoveService does not raise any exceptions if the service we're trying to
            '  remove isn't already there, so there's no need for a try/catch.
            LoaderHost.RemoveService(GetType(INameCreationService))
            LoaderHost.RemoveService(GetType(ComponentSerializationService))

            LoaderHost.RemoveService(GetType(SettingTypeNameResolutionService))
            LoaderHost.RemoveService(GetType(SettingsTypeCache))
            LoaderHost.RemoveService(GetType(SettingsValueCache))
            MyBase.Dispose()
        End Sub

        ''' <summary>
        ''' Name of base component this loader (de)serializes
        ''' </summary>
        ''' <returns>Name of base component this loader (de)serializes</returns>
        ''' <remarks></remarks>
        Protected Overrides Function GetBaseComponentClassName() As String
            Return GetType(DesignTimeSettings).Name
        End Function

        ''' <summary>
        ''' Flush any changes to underlying docdata(s)
        ''' </summary>
        ''' <param name="SerializationManager"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub HandleFlush(ByVal SerializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)
            Try
                m_Flushing = True
                Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
                If Designer IsNot Nothing Then
                    Designer.CommitPendingChanges(True, False)
                Else
                    Debug.Fail("Failed to get designer from my root component!")
                End If

                Dim SettingsWriter As DocDataTextWriter = Nothing
                Try

                    SettingsWriter = New DocDataTextWriter(m_DocData)

                    SettingsSerializer.Serialize(RootComponent, GeneratedClassNamespace(), GeneratedClassName, SettingsWriter, DesignerFramework.DesignUtil.GetEncoding(DocData))
                Finally
                    If SettingsWriter IsNot Nothing Then
                        SettingsWriter.Close()
                    End If
                End Try

                ' Flush values to app.config file
                FlushAppConfig()
            Finally
                m_Flushing = False
            End Try
        End Sub

        ''' <summary>
        ''' Load the settings designer. Create a new DesignTimeSettings instance, add it to my loader host, deserialize
        ''' the contents of "my" docdata.
        ''' </summary>
        ''' <param name="SerializationManager">My serialization manager</param>
        ''' <remarks></remarks>
        Protected Overrides Sub HandleLoad(ByVal SerializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)
            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "SettingsDesignerLoader: Start loading settings")
            Debug.Assert(LoaderHost IsNot Nothing, "Asked to load settings designer without a LoaderHost!?")
            LoaderHost.CreateComponent(GetType(DesignTimeSettings))
            Debug.Assert(RootComponent IsNot Nothing, "Failed to create DesignTimeSettings root component - failure should throw exception!?")

            ' Let's check the size of the buffer to read...
            Dim BufSize As Integer
            VSErrorHandler.ThrowOnFailure(DocData.Buffer.GetSize(BufSize))

            '...and if it is NOT empty, we should try to deserialize it
            If BufSize > 0 Then
                ' We have data - let's deserialize!
                Dim SettingsReader As DocDataTextReader = Nothing
                Try
                    SettingsReader = New DocDataTextReader(m_DocData)
                    SettingsSerializer.Deserialize(RootComponent, SettingsReader, False)
                Catch ex As Exception
                    If SerializationManager IsNot Nothing Then
                        ex.HelpLink = HelpIDs.Err_LoadingSettingsFile
                        SerializationManager.ReportError(ex)
                    End If
                    Throw New InvalidOperationException(SR.GetString(SR.SD_Err_CantLoadSettingsFile), ex)
                Finally
                    If SettingsReader IsNot Nothing Then
                        SettingsReader.Close()
                    End If
                End Try

                Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "SettingsDesignerLoader: Done loading settings reader")
            Else
                ' The buffer was empty - no panic, this is probably just a new file
            End If


            AttachAppConfigDocData(False)


            If m_AppConfigDocData IsNot Nothing Then
                Common.Switches.TraceSDSerializeSettings(TraceLevel.Verbose, "Loading app.config")
                LoadAppConfig()
            End If
        End Sub

#End Region

#Region "Private helper properties"
        ''' <summary>
        ''' Get access to "our" root component
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property RootComponent() As DesignTimeSettings
            Get
                Try
                    If LoaderHost Is Nothing Then
                        Debug.Fail("No loader host?")
                        Return Nothing
                    Else
                        Return CType(LoaderHost.RootComponent, DesignTimeSettings)
                    End If
                Catch Ex As ObjectDisposedException
                    Debug.Fail("Our loader host is disposed!")
                    Throw
                End Try
            End Get
        End Property

        ''' <summary>
        ''' Get the current EndDTE.Project instance for the project containing the .settings
        ''' file
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly Property EnvDTEProject() As EnvDTE.Project
            Get
                Return Common.DTEUtils.EnvDTEProject(VsHierarchy)
            End Get
        End Property

        Friend ReadOnly Property GeneratedClassName() As String
            Get
                Return SettingsDesigner.GeneratedClassName(VsHierarchy, ProjectItemid, RootComponent, DocData.Name)
            End Get
        End Property

        Private ReadOnly Property GeneratedClassNamespace() As String
            Get
                Return ProjectUtils.GeneratedSettingsClassNamespace(VsHierarchy, ProjectItemid)
            End Get
        End Property

        Private ReadOnly Property GeneratedClassNamespace(ByVal IncludeRootNamespace As Boolean) As String
            Get
                Return ProjectUtils.GeneratedSettingsClassNamespace(VsHierarchy, ProjectItemid, IncludeRootNamespace)
            End Get
        End Property

#End Region

#Region "Private helper functions"

        ''' <summary>
        ''' Get a DocData for the App.Config file (if any)
        ''' </summary>
        ''' <remarks></remarks>
        Private Function AttachAppConfigDocData(ByVal CreateIfNotExist As Boolean) As Boolean
            ' Now, Let's try and get to the app.config file
            If m_AppConfigDocData Is Nothing Then
                m_AppConfigDocData = AppConfigSerializer.GetAppConfigDocData(VBPackage.Instance, VsHierarchy, CreateIfNotExist, False, m_DocDataService)
                If m_AppConfigDocData IsNot Nothing Then
                    AddHandler m_AppConfigDocData.DataChanged, AddressOf Me.ExternalChange
                End If
            End If
            Return m_AppConfigDocData IsNot Nothing
        End Function


        ''' <summary>
        ''' Make sure that we have a custom tool associated with this file
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetSingleFileGenerator()
            Dim ProjectItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)
            If ProjectItem IsNot Nothing AndAlso ProjectItem.Properties IsNot Nothing Then
                Debug.Assert(ProjectItemid = ProjectUtils.ItemId(VsHierarchy, ProjectItem))
                Try
                    Dim CustomToolProperty As EnvDTE.Property = ProjectItem.Properties.Item("CustomTool")
                    If CustomToolProperty IsNot Nothing Then
                        Dim CurrentCustomTool As String = TryCast(CustomToolProperty.Value, String)
                        If CurrentCustomTool = "" Then
                            CustomToolProperty.Value = SettingsSingleFileGenerator.SingleFileGeneratorName
                        End If
                    End If
                Catch ex As System.ArgumentException
                    ' Venus doesn't like people looking for the "CustomTool" property of the project item...
                    ' Well, if that's the case we can't very well the the property either.... no big deal!
                End Try
            End If
        End Sub

#End Region

#Region "Component change notifications"
        ''' <summary>
        ''' Whenever someone added components to the host, we've gotta make sure that the component is
        ''' added to the settings
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentAddedHandler(ByVal sender As Object, ByVal e As ComponentEventArgs)
            If TypeOf e.Component Is DesignTimeSettingInstance Then
                ' Let's make sure our root component knows about this guy!
                Debug.Assert(RootComponent IsNot Nothing, "No root component when adding design time setting instances")
                RootComponent.Add(DirectCast(e.Component, DesignTimeSettingInstance))
            End If
        End Sub

        ''' <summary>
        ''' Indicate that a component is about to be changed
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentChangingHandler(ByVal sender As Object, ByVal e As ComponentChangingEventArgs)
            Dim instance As DesignTimeSettingInstance = TryCast(e.Component, DesignTimeSettingInstance)
            ' If this is a rename of a web reference, we have to check out the project file in order to update
            ' the corresponding property in it...
            If instance IsNot Nothing _
                AndAlso e.Member IsNot Nothing _
                AndAlso e.Member.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) _
                AndAlso String.Equals(instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) _
            Then
                If m_ServiceProvider IsNot Nothing _
                    AndAlso Me.ProjectItem IsNot Nothing _
                    AndAlso Me.ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso Me.ProjectItem.ContainingProject.FullName <> "" _
                Then
                    ' Check out the project file...
                    Dim filesToCheckOut As New List(Of String)(1)
                    filesToCheckOut.Add(Me.ProjectItem.ContainingProject.FullName)
                    DesignerFramework.SourceCodeControlManager.QueryEditableFiles(m_ServiceProvider, filesToCheckOut, True, False)
                End If
            End If
        End Sub

        ''' <summary>
        ''' When the name of a setting is changed, we have to rename the symbol...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentChangedHandler(ByVal sender As Object, ByVal e As ComponentChangedEventArgs)
            ' If the name property of a designtimesetting instance (or derived class) has changed in a project item
            ' that has "our" custom tool associated with it, we want to invoke a global rename of the setting
            '
            If TypeOf e.Component Is DesignTimeSettingInstance _
                AndAlso e.Member.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) _
                AndAlso Not String.Equals(TryCast(e.OldValue, String), TryCast(e.NewValue, String), StringComparison.Ordinal) _
            Then
                Dim SettingsFileProjectItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)

                If SettingsFileProjectItem IsNot Nothing AndAlso SettingsFileProjectItem.Properties IsNot Nothing Then
                    Dim CurrentCustomTool As String
                    Try
                        Dim CustomToolProperty As EnvDTE.Property = SettingsFileProjectItem.Properties.Item("CustomTool")
                        CurrentCustomTool = TryCast(CustomToolProperty.Value, String)
                    Catch ex As ArgumentException
                        ' No problems, this is probably just a venus project!
                        Return
                    End Try

                    ' We only rename the symbol if the current custom tool is our file generator...
                    If CurrentCustomTool IsNot Nothing AndAlso _
                        ( _
                            CurrentCustomTool.Equals(SettingsSingleFileGenerator.SingleFileGeneratorName, StringComparison.OrdinalIgnoreCase) _
                            OrElse CurrentCustomTool.Equals(PublicSettingsSingleFileGenerator.SingleFileGeneratorName, StringComparison.OrdinalIgnoreCase) _
                        ) _
                    Then
                        Dim GeneratedClassName As String = SettingsDesigner.FullyQualifiedGeneratedTypedSettingsClassName(VsHierarchy, ProjectItemid, RootComponent, SettingsFileProjectItem)
                        Dim FindSettingClassFilter As New ProjectUtils.KnownClassName(GeneratedClassName)
                        Dim ce As EnvDTE.CodeElement = ProjectUtils.FindElement(SettingsFileProjectItem, _
                                                                        False, _
                                                                        True, _
                                                                        FindSettingClassFilter)

                        If ce Is Nothing Then
                            ' If our custom tool haven't run yet, we won't find a typed wrapper class in the project...
                            ' Consider: should we force the custom generator to run here - it's probably too late since we already have changed the name...?
                            Return
                        End If


                        Dim FindSettingsPropertyFilter As New ProjectUtils.FindPropertyFilter(ce, DirectCast(e.OldValue, String))
                        Dim pce As EnvDTE.CodeElement = ProjectUtils.FindElement(SettingsFileProjectItem, True, True, FindSettingsPropertyFilter)

                        If pce Is Nothing Then
                            ' If we can't find the property in the strongly typed settings class, it may be because the file hasn't
                            ' been regenerated yet...
                            ' Consider: should we force the custom generator to run here - it's probably too late since we already have changed the name...?
                            Return
                        End If

                        Dim pce2 As EnvDTE80.CodeElement2 = TryCast(pce, EnvDTE80.CodeElement2)
                        If pce2 Is Nothing Then
                            Debug.Fail("Failed to get CodeElement2 interface from CodeElement - CodeModel doesn't support ReplaceSymbol?")
                        Else
                            Try
                                SettingsSingleFileGeneratorBase.AllowSymbolRename = True
                                pce2.RenameSymbol(DirectCast(e.NewValue, String))
                            Catch ex As COMException When ex.ErrorCode = Common.CodeModelUtils.HR_E_CSHARP_USER_CANCEL _
                                                          OrElse ex.ErrorCode = NativeMethods.E_ABORT _
                                                          OrElse ex.ErrorCode = NativeMethods.OLECMDERR_E_CANCELED _
                                                          OrElse ex.ErrorCode = NativeMethods.E_FAIL
                                ' We should ignore if the customer cancels this or we can not build the project...
                            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex, True)
                                DesignerFramework.DesignerMessageBox.Show(m_ServiceProvider, ex, DesignerFramework.DesignUtil.GetDefaultCaption(m_ServiceProvider))
                            Finally
                                SettingsSingleFileGeneratorBase.AllowSymbolRename = False
                            End Try
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whenever someone removed components from the host, we've gotta make sure that the component is
        ''' removed from the settings
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComponentRemovedHandler(ByVal sender As Object, ByVal e As ComponentEventArgs)
            If Me.LoaderHost IsNot Nothing AndAlso Me.LoaderHost.Loading Then
                ' If we are currently (re)loading the design surface, we don't want to force-run the custom tool
                ' (Loading is set to true during reload as well as during load)
                Return
            End If

            If TypeOf e.Component Is DesignTimeSettingInstance Then
                If RootComponent IsNot Nothing Then
                    ' Let's make sure our root component knows about this guy!
                    RootComponent.Remove(DirectCast(e.Component, DesignTimeSettingInstance))

                    ' We need to make sure that we run the custom tool whenever we remove a setting - if we don't 
                    ' we may run into problems later if we try to rename the setting to a setting that we
                    ' have already removed...
                    RunSingleFileGenerator(True)
                End If
            End If
        End Sub
#End Region

#Region "Other change notifications"

        ''' <summary>
        ''' An external change was made to one of my docdatas. Reload designer!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ExternalChange(ByVal sender As Object, ByVal e As System.EventArgs)
            If Not m_Flushing Then
                Debug.Assert(m_AppConfigDocData IsNot Nothing, "Why did we get a change notification for a NULL App.Config DocData?")
                Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Queueing a reload due to an external change of the app.config DocData")
                Reload(ReloadOptions.NoFlush)
            End If
        End Sub

        ''' <summary>
        ''' Load the contents of the app.config file
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub LoadAppConfig()
            Debug.Assert(m_AppConfigDocData IsNot Nothing, "Can't load a non-existing app.config file!")
            Try
                Dim cfgHelper As New ConfigurationHelperService

                Dim objectDirty As AppConfigSerializer.DirtyState = _
                    AppConfigSerializer.Deserialize(RootComponent, _
                                                    DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache), _
                                                    DirectCast(GetService(GetType(SettingsValueCache)), SettingsValueCache), _
                                                    cfgHelper.GetSectionName(ProjectUtils.FullyQualifiedClassName(GeneratedClassNamespace(True), GeneratedClassName), String.Empty), _
                                                    m_AppConfigDocData, _
                                                    AppConfigSerializer.MergeValueMode.Prompt, _
                                                    CType(GetService(GetType(System.Windows.Forms.Design.IUIService)), System.Windows.Forms.Design.IUIService))
                If objectDirty <> AppConfigSerializer.DirtyState.NoChange Then
                    ' Set flag if we make changes to the settings object during load that should
                    ' set the docdata to dirty immediately after we have loaded.
                    ' 
                    ' Since component change notifications are ignored while we are loading the object,
                    ' we have to do this after the load is completed....
                    m_ModifiedDuringLoad = True
                End If
            Catch ex As System.Configuration.ConfigurationErrorsException
                ' We failed to load the app config xml document....
                DesignerFramework.DesignUtil.ReportError(m_ServiceProvider, SR.GetString(SR.SD_FailedToLoadAppConfigValues), HelpIDs.Err_LoadingAppConfigFile)
            Catch Ex As Exception
                Debug.Fail(String.Format("Failed to load app.config {0}", Ex))
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' If we made changes during load that will affect the docdata, this is the place to set the modified flag...
        ''' </summary>
        ''' <param name="successful"></param>
        ''' <param name="errors"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnEndLoad(ByVal successful As Boolean, ByVal errors As System.Collections.ICollection)
            MyBase.OnEndLoad(successful, errors)
            ConnectDebuggerEvents()
            ConnectBuildEvents()
            'test if in build process
            If IsInBuildProgress() Then
                SetReadOnlyMode(True, String.Empty)
                m_readOnly = True
            Else
                SetReadOnlyMode(False, String.Empty)
                m_readOnly = False
            End If
            If m_ModifiedDuringLoad AndAlso InDesignMode() Then
                Try
                    Me.OnModifying()
                    Me.Modified = True
                Catch ex As CheckoutException
                    ' What should we do here???
                End Try
            End If
            m_ModifiedDuringLoad = False
        End Sub

        ''' <summary>
        ''' Persist our values to the app.config file...
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub FlushAppConfig()
            If AttachAppConfigDocData(True) Then
                Debug.Assert(m_AppConfigDocData IsNot Nothing, "Why did AttachAppConfigDocData return true when we don't have an app.config docdata!?")
                Try
                    AppConfigSerializer.Serialize(RootComponent, _
                                DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache), _
                                DirectCast(GetService(GetType(SettingsValueCache)), SettingsValueCache), _
                                GeneratedClassName, _
                                GeneratedClassNamespace(True), _
                                m_AppConfigDocData, _
                                VsHierarchy, _
                                True)
                Catch Ex As Exception When Not Common.Utils.IsUnrecoverable(Ex)
                    ' We failed to flush values to the app config document....
                    DesignerFramework.DesignUtil.ReportError(m_ServiceProvider, SR.GetString(SR.SD_FailedToSaveAppConfigValues), HelpIDs.Err_SavingAppConfigFile)
                End Try
            End If
        End Sub


        ''' <summary>
        ''' Called when the document's window is activated or deactivated
        ''' </summary>
        ''' <param name="Activated"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDesignerWindowActivated(ByVal Activated As Boolean)
            MyBase.OnDesignerWindowActivated(Activated)

            Switches.TracePDFocus(TraceLevel.Warning, "SettingsDesignerLoader.OnDesignerWindowActivated")
            Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
            If Designer IsNot Nothing AndAlso Designer.HasView Then
                Dim view As SettingsDesignerView = DirectCast(Designer.GetView(ViewTechnology.Default), SettingsDesignerView)
                view.OnDesignerWindowActivated(Activated)
                If Not Activated Then
                    Switches.TracePDFocus(TraceLevel.Warning, "... SettingsDesignerLoader.OnDesignerWindowActivated: CommitPendingChanges()")
                    view.CommitPendingChanges(True, True)
                End If
            End If
        End Sub
#End Region

        ''' <summary>
        '''  Dispose any owned resources
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                ' The LoaderHost will remove all services all by itself...
                ' No need to worry about that here :)

                Dim ComponentChangeService As IComponentChangeService = CType(GetService(GetType(IComponentChangeService)), IComponentChangeService)

                If ComponentChangeService IsNot Nothing Then
                    RemoveHandler ComponentChangeService.ComponentAdded, AddressOf Me.ComponentAddedHandler
                    RemoveHandler ComponentChangeService.ComponentChanging, AddressOf Me.ComponentChangingHandler
                    RemoveHandler ComponentChangeService.ComponentChanged, AddressOf Me.ComponentChangedHandler
                    RemoveHandler ComponentChangeService.ComponentRemoved, AddressOf Me.ComponentRemovedHandler
                End If

                ' Unregister any change handlers that we've associated with the app.config file
                If m_AppConfigDocData IsNot Nothing Then
                    RemoveHandler m_AppConfigDocData.DataChanged, AddressOf Me.ExternalChange

                    ' 
                    ' DevDiv 79301:
                    ' If our primary docdata is not modified, but some other editor has made the app.config file docdata
                    ' dirty, and we happen to be the last editor open, then we need to dispose the app.config docdata in
                    ' order to save any changes.
                    '
                    ' The DesignerDocDataService that normally handles this will not save dependent files unless the primary
                    ' docdata is modified...
                    '
                    If m_docData IsNot Nothing AndAlso (Not m_docData.Modified) AndAlso m_appConfigDocData.Modified Then
                        m_appConfigDocData.Dispose()
                    Else
                        ' 
                        ' Please note that we should normally let the DesignerDocDataService's dispose dispose
                        ' the child docdata - this will be done in the BaseDesignerLoader's Dipose.
                        ' 
                    End If
                    m_AppConfigDocData = Nothing
                End If

                DisconnectDebuggerEvents()
            End If
            MyBase.Dispose(Disposing)
        End Sub

        Friend Function EnsureCheckedOut() As Boolean
            Try
                Dim ProjectReloaded As Boolean
                ManualCheckOut(ProjectReloaded)
                If ProjectReloaded Then
                    'If the project was reloaded, clients need to exit as soon as possible.
                    Return False
                End If

                AttachAppConfigDocData(True)

                Switches.TracePDFocus(TraceLevel.Warning, "[disabled] SettingsDesignerLoader EnsureCheckedOut hack: Me.LoaderHost.Activate()")

                Return True
            Catch ex As System.ComponentModel.Design.CheckoutException
                ' We failed to checkout the file (s)...
                Return False
            End Try
        End Function

        ''' <summary>
        ''' We somtimes want to check out the project file (to add app.config) and sometimes we only want to 
        ''' check out the app.config file itself...
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property ManagingDynamicSetOfFiles() As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Overridden in order to provide the app.config file name or the project name when as well as the project item
        ''' and dependent file...
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides ReadOnly Property FilesToCheckOut() As System.Collections.Generic.List(Of String)
            Get
                Dim result As List(Of String) = MyBase.FilesToCheckOut
                Dim projectItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(VsHierarchy, Me.ProjectItemid)
                Dim appConfigOrProjectName As String = ProjectUtils.AppConfigOrProjectFileNameForCheckout(projectItem, VsHierarchy)
                If appConfigOrProjectName <> "" Then
                    result.Add(appConfigOrProjectName)
                End If
                Return result
            End Get
        End Property


        Friend Function InDesignMode() As Boolean
            Return m_currentDebugMode = DBGMODE.DBGMODE_Design
        End Function

#Region "INameCreationService"
        Public Function CreateName(ByVal container As System.ComponentModel.IContainer, ByVal dataType As System.Type) As String Implements System.ComponentModel.Design.Serialization.INameCreationService.CreateName
            If dataType IsNot Nothing AndAlso String.Equals(dataType.AssemblyQualifiedName, GetType(DesignTimeSettings).AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase) Then
                Return ""
            End If

            Dim Settings As DesignTimeSettings = RootComponent
            If Settings IsNot Nothing Then
                Return Settings.CreateUniqueName()
            End If

            Debug.Fail("You should never reach this line of code!")
            Dim existingNames As New Hashtable
            For i As Integer = 0 To container.Components.Count - 1
                Dim instance As DesignTimeSettingInstance = TryCast(container.Components.Item(i), DesignTimeSettingInstance)
                If instance IsNot Nothing Then
                    existingNames(instance.Name) = Nothing
                End If
            Next

            For i As Integer = 1 To container.Components.Count + 1
                Dim SuggestedName As String = "Setting" & i.ToString()
                If Not existingNames.ContainsKey(SuggestedName) Then
                    Return SuggestedName
                End If
            Next
            Debug.Fail("You should never reach this line of code!")
            Return ""
        End Function

        Public Function IsValidName(ByVal name As String) As Boolean Implements System.ComponentModel.Design.Serialization.INameCreationService.IsValidName
            If RootComponent IsNot Nothing Then
                Return RootComponent.IsValidName(name)
            Else
                Return name <> ""
            End If
        End Function

        Public Sub ValidateName(ByVal name As String) Implements System.ComponentModel.Design.Serialization.INameCreationService.ValidateName
            If Not IsValidName(name) Then
                Throw Common.CreateArgumentException("name")
            End If
        End Sub
#End Region

#Region "ReadOnly during debug mode and build" ' BUGFIX: Dev11#45255 

        ''' <summary>
        ''' Start listening to build events and set our initial build status
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectBuildEvents()
            Dim dte As EnvDTE.DTE
            dte = CType(GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
            If dte IsNot Nothing Then
                m_buildEvents = dte.Events.BuildEvents
            Else
                Debug.Fail("No DTE - can't hook up build events - we don't know if start/stop building...")
            End If
        End Sub

        Friend Function IsReadOnly() As Boolean
            Return m_readOnly
        End Function

        ''' <summary>
        ''' A build has started - disable/enable page
        ''' </summary>
        Private Sub BuildBegin(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildBegin
            SetReadOnlyMode(True, String.Empty)
            m_readOnly = True
            RefreshView()
        End Sub

        ''' <summary>
        ''' A build has finished - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        ''' <remarks></remarks>
        Private Sub BuildDone(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildDone
            SetReadOnlyMode(False, String.Empty)
            m_readOnly = False
            RefreshView()
        End Sub

        ''' <summary>
        ''' Refresh the status of Settings view commands
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RefreshView()
            Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
            If Designer IsNot Nothing AndAlso Designer.HasView Then
                Dim view As SettingsDesignerView = DirectCast(Designer.GetView(ViewTechnology.Default), SettingsDesignerView)
                view.RefreshCommandStatus()
            End If
        End Sub

        ''' <summary>
        ''' Hook up with the debugger event mechanism to determine current debug mode
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectDebuggerEvents()
            If m_VsDebuggerEventsCookie = 0 Then
                m_VsDebugger = CType(GetService(GetType(IVsDebugger)), IVsDebugger)
                If m_VsDebugger IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.AdviseDebuggerEvents(Me, m_VsDebuggerEventsCookie))

                    Dim mode As DBGMODE() = New DBGMODE() {DBGMODE.DBGMODE_Design}
                    'Get the current mode
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.GetMode(mode))
                    OnModeChange(mode(0))
                Else
                    Debug.Fail("Cannot obtain IVsDebugger from shell")
                    OnModeChange(DBGMODE.DBGMODE_Design)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Unhook event notification for debugger 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectDebuggerEvents()
            Try
                If m_VsDebugger IsNot Nothing AndAlso m_VsDebuggerEventsCookie <> 0 Then
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.UnadviseDebuggerEvents(m_VsDebuggerEventsCookie))
                    m_VsDebuggerEventsCookie = 0
                    m_VsDebugger = Nothing
                End If
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
            End Try
        End Sub

        ''' <summary>
        ''' handle DebugMode change event, disable the designer when in debug mode...
        ''' </summary>
        ''' <param name="dbgmodeNew"></param>
        Private Function OnModeChange(ByVal dbgmodeNew As Shell.Interop.DBGMODE) As Integer Implements Shell.Interop.IVsDebuggerEvents.OnModeChange
            Try
                If dbgmodeNew = DBGMODE.DBGMODE_Design Then
                    SetReadOnlyMode(False, String.Empty)
                ElseIf m_currentDebugMode = DBGMODE.DBGMODE_Design Then
                    SetReadOnlyMode(True, SR.GetString(SR.SD_ERR_CantEditInDebugMode))
                End If
            Finally
                m_currentDebugMode = dbgmodeNew
            End Try

            ' Let's try to refresh whatever menus we have here...
            If LoaderHost IsNot Nothing AndAlso RootComponent IsNot Nothing Then
                Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
                If Designer IsNot Nothing Then
                    Designer.RefreshMenuStatus()
                End If
            End If
        End Function
#End Region

    End Class

End Namespace
