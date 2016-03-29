'------------------------------------------------------------------------------
' <copyright from='1997' to='2001' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'

Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.WCFReference.Interop
Imports Microsoft.VisualStudio.Editors.Package
Imports Microsoft.VisualStudio.XmlEditor
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualStudio.Editors
<Assembly: System.Runtime.InteropServices.GuidAttribute("832BFEE6-9036-423E-B90A-EA4C582DA1D2")> 

Namespace Microsoft.VisualStudio.Editors


    '*
    '* This is the Visual Studio package for the Microsoft.VisualStudio.Editors assembly.  It will be CoCreated by
    '* Visual Studio during package load in response to the GUID contained below.
    '*

    '*
    '* IMPORTANT NOTE:
    '* We are not currently using RegPkg.exe to register this assembly, so those attributes have been removed 
    '*   from here for the moment.  Instead, you need to make registration changes 
    '*   to SetupAuthoring/vb/Registry/Microsoft.VisualStudio.Editors.*.ddr
    '*
    '* In the future, we should consider moving to a RegPkg.exe model
    < _
        System.Runtime.InteropServices.GuidAttribute("67909B06-91E9-4F3E-AB50-495046BE9A9A"), _
        CLSCompliantAttribute(False), _
        ProvideOptionPage(GetType(EditorToolsOptionsPage), "VisualBasic", "Editor", 1001, 1005, False) _
    > _
    Friend Class VBPackage
        Inherits Microsoft.VisualStudio.Shell.Package
        Implements Editors.IVBPackage

        Private m_PermissionSetService As VBAttributeEditor.PermissionSetService
        Private m_XmlIntellisenseService As XmlIntellisense.XmlIntellisenseService
        Private m_BuildEventCommandLineDialogService As PropertyPages.BuildEventCommandLineDialogService
        Private m_VBReferenceChangedService As VBRefChangedSvc.VBReferenceChangedService
        Private m_ResourceEditorRefactorNotify As ResourceEditor.ResourceEditorRefactorNotify
        Private m_UserConfigCleaner As UserConfigCleaner
        Private m_AddImportsDialogService As AddImports.AddImportsDialogService

        Private Const ProjectDesignerSUOKey As String = "ProjectDesigner"

        ' Map between unique project GUID and the last viewed tab in the project designer...
        Private m_lastViewedProjectDesignerTab As System.Collections.Generic.Dictionary(Of Guid, Byte)

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            ' Make sure we persist this 
            AddOptionKey(ProjectDesignerSUOKey)
        End Sub

        ''' <summary>
        ''' Initialize package (register editor factories, add services)
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            Debug.Assert(s_Instance Is Nothing, "VBPackage initialized multiple times?")
            s_Instance = Me
            MyBase.Initialize()

            'Register editor factories
            Try
                MyBase.RegisterEditorFactory(New SettingsDesigner.SettingsDesignerEditorFactory)
            Catch ex As Exception
                Debug.Fail("Exception registering settings designer editor factory: " & ex.ToString())
                Throw
            End Try
            Try
                MyBase.RegisterEditorFactory(New ResourceEditor.ResourceEditorFactory)
            Catch ex As Exception
                Debug.Fail("Exception registering registry editor editor factory: " & ex.ToString())
                Throw
            End Try
            Try
                MyBase.RegisterEditorFactory(New Microsoft.VisualStudio.Editors.ApplicationDesigner.ApplicationDesignerEditorFactory)
            Catch ex As Exception
                Debug.Fail("Exception registering application designer editor factory: " & ex.ToString())
                Throw
            End Try
            Try
                MyBase.RegisterEditorFactory(New PropPageDesigner.PropPageDesignerEditorFactory)
            Catch ex As Exception
                Debug.Fail("Exception registering property page designer editor factory: " & ex.ToString())
                Throw
            End Try

            ' Create callback for deferred service loading
            Dim CallBack As ServiceCreatorCallback = New ServiceCreatorCallback(AddressOf OnCreateService)

            ' The VSIP package is a service container
            Dim ServiceContainer As IServiceContainer = CType(Me, IServiceContainer)

            ' Expose Permission Set Service
            ServiceContainer.AddService(GetType(VBAttributeEditor.Interop.IVbPermissionSetService), CallBack, True)

            ' Expose Xml Intellisense Service
            ServiceContainer.AddService(GetType(XmlIntellisense.IXmlIntellisenseService), CallBack, True)

            ' Expose IVsBuildEventCommandLineDialogService
            ServiceContainer.AddService(GetType(Editors.Interop.IVsBuildEventCommandLineDialogService), CallBack, True)

            ' Expose IVsRefactorNotify through the ResourceEditorFactory
            ServiceContainer.AddService(GetType(ResourceEditor.ResourceEditorRefactorNotify), CallBack, True)

            'Expose Add Imports Dialog Service
            ServiceContainer.AddService(GetType(AddImports.IVBAddImportsDialogService), CallBack, True)

            ' Expose VBReferenceChangedService
            ServiceContainer.AddService(GetType(VBRefChangedSvc.Interop.IVbReferenceChangedService), CallBack, True)

            m_UserConfigCleaner = New UserConfigCleaner(Me)
        End Sub 'New

        Public ReadOnly Property MenuCommandService() As IMenuCommandService Implements IVBPackage.MenuCommandService
            Get
                Return TryCast(Me.GetService(GetType(IMenuCommandService)), IMenuCommandService)
            End Get
        End Property

        ''' <summary>
        ''' Callback to expose services to the shell
        ''' </summary>
        ''' <param name="container"></param>
        ''' <param name="serviceType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnCreateService(ByVal container As IServiceContainer, ByVal serviceType As Type) As Object

            ' Is the Permission Set Service being requested?
            If serviceType Is GetType(VBAttributeEditor.Interop.IVbPermissionSetService) Then
                If (m_PermissionSetService Is Nothing) Then
                    m_PermissionSetService = New VBAttributeEditor.PermissionSetService(container)
                End If

                ' Return cached Permission Set Service
                Return m_PermissionSetService
            End If

            ' Is the Xml Intellisense Service being requested?
            If serviceType Is GetType(XmlIntellisense.IXmlIntellisenseService) Then
                ' Return cached Xml Intellisense Service
                Return GetXmlIntellisenseService(container)
            End If

            ' Is the IVsBuildEventCommandLineDialogService being requested?
            If serviceType Is GetType(Editors.Interop.IVsBuildEventCommandLineDialogService) Then
                If (m_BuildEventCommandLineDialogService Is Nothing) Then
                    m_BuildEventCommandLineDialogService = New PropertyPages.BuildEventCommandLineDialogService(container)
                End If

                ' Return cached BuildEventCommandLineDialogService
                Return m_BuildEventCommandLineDialogService
            End If

            If serviceType Is GetType(ResourceEditor.ResourceEditorRefactorNotify) Then
                If (m_ResourceEditorRefactorNotify Is Nothing) Then
                    m_ResourceEditorRefactorNotify = New ResourceEditor.ResourceEditorRefactorNotify()
                End If

                ' return cached refactor-notify implementer
                Return m_ResourceEditorRefactorNotify
            End If

            If serviceType Is GetType(AddImports.IVBAddImportsDialogService) Then
                If (m_AddImportsDialogService Is Nothing) Then
                    m_AddImportsDialogService = New AddImports.AddImportsDialogService(Me)
                End If

                Return m_AddImportsDialogService
            End If

            ' Lazy-init VBReferenceChangedService and return the cached service.
            If serviceType Is GetType(VBRefChangedSvc.Interop.IVbReferenceChangedService) Then
                If m_VBReferenceChangedService Is Nothing Then
                    m_VBReferenceChangedService = New VBRefChangedSvc.VBReferenceChangedService()
                End If

                Return m_VBReferenceChangedService
            End If

            Debug.Fail("VBPackage was requested to create a package it has no knowledge about: " & serviceType.ToString())
            Return Nothing
        End Function

        ''' <summary>
        ''' Get or Create an XmlIntellisenseService object
        ''' </summary>
        ''' <param name="container"></param>
        ''' <remarks>
        ''' This code is factored out of OnCreateService in order to delay loading Microsoft.VisualStudio.XmlEditor.dll
        ''' </remarks>
        Private Function GetXmlIntellisenseService(ByVal container As IServiceContainer) As XmlIntellisense.XmlIntellisenseService
            If (m_XmlIntellisenseService Is Nothing) Then
                ' Xml Intellisense Service is only available if the Xml Editor Schema Service is available as well
                Dim schemaService As XmlSchemaService = DirectCast(container.GetService(GetType(XmlSchemaService)), XmlSchemaService)

                If schemaService IsNot Nothing Then
                    m_XmlIntellisenseService = New XmlIntellisense.XmlIntellisenseService(container, schemaService)
                End If
            End If

            ' Return cached Xml Intellisense Service
            Return m_XmlIntellisenseService
        End Function

        ''' <summary>
        ''' Dispose our resources....
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If m_UserConfigCleaner IsNot Nothing Then
                    m_UserConfigCleaner.Dispose()
                    m_UserConfigCleaner = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Protected Shared s_Instance As VBPackage

        Public Shared ReadOnly Property Instance() As VBPackage
            Get
                Return s_Instance
            End Get
        End Property

#Region "SQM helpers"
        ''' <summary>
        ''' In case you want to get hold of a IVsSqm service, but you don't have a service provider around
        ''' this is a nice helper method...
        ''' </summary>
        ''' <value></value>
        ''' <remarks>May return nothing if the package can't find the service</remarks>
        Friend Shared ReadOnly Property VsLog() As Microsoft.VisualStudio.Shell.Interop.IVsSqm
            Get
                Try
                    If Instance IsNot Nothing Then
                        Return DirectCast(Instance.GetService(GetType(Microsoft.VisualStudio.Shell.Interop.SVsLog)), Microsoft.VisualStudio.Shell.Interop.IVsSqm)
                    End If
                Catch Ex As InvalidCastException
                    Debug.Fail("Failed to cast returned Service to an IVsSqm - SQM logging will be disabled")
                End Try
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Log a SQM datapoint. Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Friend Shared Sub LogSqmDatapoint(ByVal dataPointId As UInteger, ByVal value As UInteger)
            If VsLog IsNot Nothing Then
                VsLog.SetDatapoint(dataPointId, value)
            End If
        End Sub

        ''' <summary>
        ''' Increment a SQM datapoint.Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Friend Shared Sub IncrementSqmDatapoint(ByVal dataPointId As UInteger, Optional ByVal value As UInteger = 1)
            If VsLog IsNot Nothing Then
                VsLog.IncrementDatapoint(dataPointId, value)
            End If
        End Sub

        ''' <summary>
        ''' Add an item to a SQM stream. Helps out so you don't have to have a service provider around
        ''' </summary>
        ''' <param name="dataPointId"></param>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Public Shared Sub AddSqmItemToStream(ByVal dataPointId As UInteger, Optional ByVal value As UInteger = 1)
            If VsLog IsNot Nothing Then
                VsLog.AddItemToStream(dataPointId, value)
            End If
        End Sub
#End Region

        'Used for accessing global services before a component in this assembly gets sited
        Public Shadows ReadOnly Property GetService(ByVal serviceType As Type) As Object Implements Editors.IVBPackage.GetService
            Get
                Return MyBase.GetService(serviceType)
            End Get
        End Property

#Region "Load/save package options"
        ''' <summary>
        ''' Load options
        ''' </summary>
        ''' <param name="key">Added in the constructor using AddOptionKey </param>
        ''' <param name="stream">Stream to read from</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnLoadOptions(ByVal key As String, ByVal stream As System.IO.Stream)
            If String.Equals(key, ProjectDesignerSUOKey, StringComparison.Ordinal) Then
                Dim reader As New IO.BinaryReader(stream)
                Dim buf(15) As Byte ' Space enough for a GUID - 16 bytes...
                Try
                    While reader.Read(buf, 0, buf.Length) = buf.Length
                        Dim projGuid As Guid
                        projGuid = New Guid(buf)
                        Dim tab As Byte = reader.ReadByte()
                        If m_lastViewedProjectDesignerTab Is Nothing Then
                            m_lastViewedProjectDesignerTab = New Collections.Generic.Dictionary(Of Guid, Byte)
                        End If
                        m_lastViewedProjectDesignerTab(projGuid) = tab
                    End While
                Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                    Debug.Fail(String.Format("Failed to read settings: {0}", ex))
                End Try
            Else
                MyBase.OnLoadOptions(key, stream)
            End If
        End Sub

        ''' <summary>
        ''' Save settings for this package
        ''' </summary>
        ''' <param name="key">Added in the constructor using AddOptionKey</param>
        ''' <param name="stream">Stream to read data from</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnSaveOptions(ByVal key As String, ByVal stream As System.IO.Stream)
            If String.Equals(key, ProjectDesignerSUOKey, StringComparison.Ordinal) Then
                ' This is the project designer's last active tab
                If m_lastViewedProjectDesignerTab IsNot Nothing Then
                    Dim hier As IVsHierarchy = Nothing
                    Dim sol As IVsSolution = TryCast(GetService(GetType(IVsSolution)), IVsSolution)
                    Debug.Assert(sol IsNot Nothing, "No solution!? We won't persist the last active tab in the project designer")
                    If sol IsNot Nothing Then
                        For Each projectGuid As Guid In m_lastViewedProjectDesignerTab.Keys
                            ' We check all current projects to see what the last active tab was
                            If Editors.Interop.NativeMethods.Succeeded(sol.GetProjectOfGuid(projectGuid, hier)) Then
                                Dim tab As Byte = m_lastViewedProjectDesignerTab(projectGuid)
                                If tab <> 0 Then
                                    ' We only need to persist this if the last tab was different than the 
                                    ' default value...
                                    Dim projGuidBytes() As Byte = projectGuid.ToByteArray()
                                    stream.Write(projGuidBytes, 0, projGuidBytes.Length)
                                    stream.WriteByte(tab)
                                End If
                            End If
                        Next
                    End If
                End If
            Else
                MyBase.OnSaveOptions(key, stream)
            End If
        End Sub
#End Region

#Region "Load/save project designer's last active tab"
        ''' <summary>
        ''' Get the project guid (VSHPROPID_ProjectIDGuid) from a IVsHieararchy
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ProjectGUID(ByVal hierarchy As IVsHierarchy) As Guid
            Dim projGuid As Guid = Guid.Empty
            Try
                If hierarchy IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ProjectIDGuid, projGuid))
                End If
            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                ' This is a non-vital function - ignore if we fail to get the GUID...
                Debug.Fail(String.Format("Failed to get project guid: {0}", ex))
            End Try
            Return projGuid
        End Function

        ''' <summary>
        ''' Helper function for the project designer to get the last active tab for a project
        ''' </summary>
        ''' <param name="projectHierarchy"></param>
        ''' <returns>Last active tab number</returns>
        ''' <remarks></remarks>
        Public Function GetLastShownApplicationDesignerTab(ByVal projectHierarchy As IVsHierarchy) As Integer Implements Editors.IVBPackage.GetLastShownApplicationDesignerTab
            Dim value As Byte
            If m_lastViewedProjectDesignerTab IsNot Nothing AndAlso m_lastViewedProjectDesignerTab.TryGetValue(ProjectGUID(projectHierarchy), value) Then
                Return value
            Else
                ' Default to tab 0
                Return 0
            End If
        End Function

        ''' <summary>
        ''' Helper function for the project designer to scribble down the last active tab
        ''' </summary>
        ''' <param name="projectHierarchy">Hierarchy</param>
        ''' <param name="tab">Tab number</param>
        ''' <remarks></remarks>
        Public Sub SetLastShownApplicationDesignerTab(ByVal projectHierarchy As IVsHierarchy, ByVal tab As Integer) Implements Editors.IVBPackage.SetLastShownApplicationDesignerTab
            If m_lastViewedProjectDesignerTab Is Nothing Then
                m_lastViewedProjectDesignerTab = New System.Collections.Generic.Dictionary(Of Guid, Byte)
            End If
            ' Make sure we don't under/overflow...
            If tab > Byte.MaxValue OrElse tab < Byte.MinValue Then
                tab = 0
            End If
            m_lastViewedProjectDesignerTab(ProjectGUID(projectHierarchy)) = CByte(tab)
        End Sub
#End Region

#Region "Clean up user.config files that may have been scattered around in a ZIP project"
        ''' <summary>
        ''' Helper class that monitors solution close events and cleans up any user.config files that
        ''' may have been by the Client Configuration API when the application is runs. 
        '''
        ''' The User.config files are created by the ClientConfig API, which is used by the runtime to
        ''' save user scoped settings created by the settings designer.
        ''' </summary>
        ''' <remarks></remarks>
        Private Class UserConfigCleaner
            Implements IVsSolutionEvents, IDisposable

            ' Our solution events cookie.
            Private m_cookie As UInteger

            ' A handle to the IVsSolution service providing the events
            Private m_solution As IVsSolution

            ' List of files to clean up when a ZIP project is discarded
            Private m_filesToCleanUp As New Collections.Generic.List(Of String)

            ''' <summary>
            ''' Create a new instance of this class
            ''' </summary>
            ''' <param name="sp"></param>
            ''' <remarks></remarks>
            Public Sub New(ByVal sp As IServiceProvider)
                m_solution = TryCast(sp.GetService(GetType(IVsSolution)), IVsSolution)
                Debug.Assert(m_solution IsNot Nothing, "Failed to get IVsSolution - clean up of user config files in ZIP projects will not work...")
                If m_solution IsNot Nothing Then
                    Dim hr As Integer = m_solution.AdviseSolutionEvents(Me, m_cookie)
#If DEBUG Then
                    Debug.Assert(Editors.Interop.NativeMethods.Succeeded(hr), "Failed to advise solution events - we won't clean up user config files in ZIP projects...")
#End If
                    If Not Editors.Interop.NativeMethods.Succeeded(hr) Then
                        m_cookie = 0
                    End If
                End If
            End Sub

            ''' <summary>
            ''' Unadvise solution events
            ''' </summary>
            ''' <remarks></remarks>
            Private Sub UnadviseSolutionEvents()
                If m_cookie <> 0 AndAlso m_solution IsNot Nothing Then
                    Dim hr As Integer = m_solution.UnadviseSolutionEvents(m_cookie)
#If DEBUG Then
                    Debug.Assert(Editors.Interop.NativeMethods.Succeeded(hr), "Failed to unadvise solution events - we may leak..")
#End If
                    If Editors.Interop.NativeMethods.Succeeded(hr) Then
                        m_cookie = 0
                    End If
                End If
            End Sub

            ''' <summary>
            ''' If we found any files to clean up in the OnBeforeCloseSolution, we better do so now that the
            ''' solution is actually closed...
            ''' </summary>
            ''' <param name="pUnkReserved"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Function OnAfterCloseSolution(ByVal pUnkReserved As Object) As Integer Implements Shell.Interop.IVsSolutionEvents.OnAfterCloseSolution
                SettingsDesigner.SettingsDesigner.DeleteFilesAndDirectories(m_filesToCleanUp, Nothing)
                m_filesToCleanUp.Clear()
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            ''' <summary>
            ''' Before the solutin is closed, we check if this is a ZIP project, and if so make a list of all files
            ''' we'll delete when the solution is closed
            ''' </summary>
            ''' <param name="pUnkReserved"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Function OnBeforeCloseSolution(ByVal pUnkReserved As Object) As Integer Implements Shell.Interop.IVsSolutionEvents.OnBeforeCloseSolution
                Try
                    m_filesToCleanUp.Clear()

                    Dim hr As Integer
                    ' Check if this is a deferred save project & there is only one project in the solution
                    Dim oBool As Object = Nothing
                    hr = m_solution.GetProperty(Microsoft.VisualStudio.Shell.Interop.__VSPROPID2.VSPROPID_DeferredSaveSolution, oBool)
#If DEBUG Then
                    Debug.Assert(Editors.Interop.NativeMethods.Succeeded(hr), "Failed to get VSPROPID_DeferredSaveSolution - we will not clean up user.config files...")
#End If
                    ErrorHandler.ThrowOnFailure(hr)

                    If oBool IsNot Nothing AndAlso CBool(oBool) Then
                        ' This is a ZIP project - let's find the projects and list all configuration files associated with it...
                        Dim projEnum As IEnumHierarchies = Nothing
                        ErrorHandler.ThrowOnFailure(m_solution.GetProjectEnum(CUInt(__VSENUMPROJFLAGS.EPF_ALLINSOLUTION), Guid.Empty, projEnum))
                        Dim hiers(0) As IVsHierarchy
                        Dim fetched As UInteger

                        Do While projEnum.Next(CUInt(hiers.Length), hiers, fetched) = Editors.Interop.NativeMethods.S_OK AndAlso fetched > 0
                            If hiers(0) IsNot Nothing Then
                                Dim dirs As Collections.Generic.List(Of String) = SettingsDesigner.SettingsDesigner.FindUserConfigDirectories(hiers(0))
                                m_filesToCleanUp.AddRange(SettingsDesigner.SettingsDesigner.FindUserConfigFiles(dirs))
                            End If
                        Loop
                    End If
                Catch ex As Exception When Not Common.IsUnrecoverable(ex)
#If DEBUG Then
                    Debug.Fail(String.Format("Failed when trying to clean up user.config files... {0}", ex))
#End If
                End Try
                Return Editors.Interop.NativeMethods.S_OK
            End Function



#Region "IVsSolutionEvents methods that simply return S_OK"

            Public Function OnAfterLoadProject(ByVal pStubHierarchy As Shell.Interop.IVsHierarchy, ByVal pRealHierarchy As Shell.Interop.IVsHierarchy) As Integer Implements Shell.Interop.IVsSolutionEvents.OnAfterLoadProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnAfterOpenProject(ByVal pHierarchy As Shell.Interop.IVsHierarchy, ByVal fAdded As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnAfterOpenProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnAfterOpenSolution(ByVal pUnkReserved As Object, ByVal fNewSolution As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnAfterOpenSolution
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnBeforeCloseProject(ByVal pHierarchy As Shell.Interop.IVsHierarchy, ByVal fRemoved As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnBeforeCloseProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnBeforeUnloadProject(ByVal pRealHierarchy As Shell.Interop.IVsHierarchy, ByVal pStubHierarchy As Shell.Interop.IVsHierarchy) As Integer Implements Shell.Interop.IVsSolutionEvents.OnBeforeUnloadProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnQueryCloseProject(ByVal pHierarchy As Shell.Interop.IVsHierarchy, ByVal fRemoving As Integer, ByRef pfCancel As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnQueryCloseProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnQueryCloseSolution(ByVal pUnkReserved As Object, ByRef pfCancel As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnQueryCloseSolution
                Return Editors.Interop.NativeMethods.S_OK
            End Function

            Public Function OnQueryUnloadProject(ByVal pRealHierarchy As Shell.Interop.IVsHierarchy, ByRef pfCancel As Integer) As Integer Implements Shell.Interop.IVsSolutionEvents.OnQueryUnloadProject
                Return Editors.Interop.NativeMethods.S_OK
            End Function
#End Region

            Private disposed As Boolean = False

            ' IDisposable
            Private Overloads Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposed Then
                    If disposing Then
                        UnadviseSolutionEvents()
                    End If
                End If
                Debug.Assert(m_cookie = 0, "We didn't unadvise solution events")
                Me.disposed = True
            End Sub

#Region " IDisposable Support "
            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

            Protected Overrides Sub Finalize()
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(False)
                MyBase.Finalize()
            End Sub
#End Region
        End Class 'UserConfigCleaner
#End Region

    End Class 'VBPackage

End Namespace
