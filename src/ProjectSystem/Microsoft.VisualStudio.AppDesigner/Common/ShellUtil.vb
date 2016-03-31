Imports EnvDTE
Imports VB = Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    ''' <summary>
    ''' Utilities relating to the Visual Studio shell, services, etc.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ShellUtil


        ''' <summary>
        ''' Gets a color from the shell's color service.  If for some reason this fails, returns the supplied
        '''   default color.
        ''' </summary>
        ''' <param name="VsUIShell">The IVsUIShell interface that must also implement IVsUIShell2 (if not, or if Nothing, default color is returned)</param>
        ''' <param name="VsSysColorIndex">The color index to look up.</param>
        ''' <param name="DefaultColor">The default color to return if the call fails.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetColor(ByVal VsUIShell As IVsUIShell, ByVal VsSysColorIndex As __VSSYSCOLOREX, ByVal DefaultColor As Color) As Color
            Return GetColor(TryCast(VsUIShell, IVsUIShell2), VsSysColorIndex, DefaultColor)
        End Function


        ''' <summary>
        ''' Gets a color from the shell's color service.  If for some reason this fails, returns the supplied
        '''   default color.
        ''' </summary>
        ''' <param name="VsUIShell2">The IVsUIShell2 interface to use (if Nothing, default color is returned)</param>
        ''' <param name="VsSysColorIndex">The color index to look up.</param>
        ''' <param name="DefaultColor">The default color to return if the call fails.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetColor(ByVal VsUIShell2 As IVsUIShell2, ByVal VsSysColorIndex As __VSSYSCOLOREX, ByVal DefaultColor As Color) As Color
            If VsUIShell2 IsNot Nothing Then
                Dim abgrValue As System.UInt32
                Dim Hr As Integer = VsUIShell2.GetVSSysColorEx(VsSysColorIndex, abgrValue)
                If VSErrorHandler.Succeeded(Hr) Then
                    Return COLORREFToColor(abgrValue)
                End If
            End If

            Debug.Fail("Unable to get color from the shell, using a predetermined default color instead." & VB.vbCrLf & "Color Index = " & VsSysColorIndex & ", Default Color = &h" & VB.Hex(DefaultColor.ToArgb))
            Return DefaultColor
        End Function

        Public Shared Function GetDesignerThemeColor(ByVal uiShellService As IVsUIShell5, ByVal themeCategory As Guid, ByVal themeColorName As String, ByVal colorType As __THEMEDCOLORTYPE, ByVal defaultColor As Color) As Color

            If uiShellService IsNot Nothing Then
                Dim rgbaValue As UInt32

                Dim hr As Int32 = VSErrorHandler.CallWithCOMConvention(
                    Sub()
                        rgbaValue = uiShellService.GetThemedColor(themeCategory, themeColorName, CType(colorType, System.UInt32))
                    End Sub)

                If VSErrorHandler.Succeeded(hr) Then
                    Return RGBAToColor(rgbaValue)
                End If
            End If

            Debug.Fail("Unable to get color from the shell, using a predetermined default color instead." & VB.vbCrLf & "Color Category = " & themeCategory.ToString() & ", Color Name = " & themeColorName & ", Color Type = " & colorType & ", Default Color = &h" & VB.Hex(DefaultColor.ToArgb))
            Return defaultColor
        End Function

        Private Shared Function RGBAToColor(ByVal rgbaValue As UInt32) As Color
            Return Color.FromArgb(CInt((rgbaValue And &HFF000000UI) >> 24), CInt(rgbaValue And &HFFUI), CInt((rgbaValue And &HFF00UI) >> 8), CInt((rgbaValue And &HFF0000UI) >> 16))
        End Function

        ''' <summary>
        ''' Converts a COLORREF value (as UInteger) to System.Drawing.Color
        ''' </summary>
        ''' <param name="abgrValue">The UInteger COLORREF value</param>
        ''' <returns>The System.Drawing.Color equivalent.</returns>
        ''' <remarks></remarks>
        Private Shared Function COLORREFToColor(ByVal abgrValue As System.UInt32) As Color
            Return Color.FromArgb(CInt(abgrValue And &HFFUI), CInt((abgrValue And &HFF00UI) >> 8), CInt((abgrValue And &HFF0000UI) >> 16))
        End Function


        ''' <summary>
        ''' Retrieves the window that should be used as the owner of all dialogs and messageboxes.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetDialogOwnerWindow(ByVal serviceProvider As IServiceProvider) As IWin32Window
            Dim dialogOwner As IWin32Window = Nothing
            Dim UIService As IUIService = DirectCast(serviceProvider.GetService(GetType(IUIService)), IUIService)
            If UIService IsNot Nothing Then
                dialogOwner = UIService.GetDialogOwnerWindow()
            End If

            Debug.Assert(dialogOwner IsNot Nothing, "Couldn't get DialogOwnerWindow")
            Return dialogOwner
        End Function


        ''' <summary>
        ''' Given an IVsCfg, get its configuration and platform names.
        ''' </summary>
        ''' <param name="Config">The IVsCfg to get the configuration and platform name from.</param>
        ''' <param name="ConfigName">[out] The configuration name.</param>
        ''' <param name="PlatformName">[out] The platform name.</param>
        ''' <remarks></remarks>
        Public Shared Sub GetConfigAndPlatformFromIVsCfg(ByVal Config As IVsCfg, ByRef ConfigName As String, ByRef PlatformName As String)
            Dim DisplayName As String = Nothing

            VSErrorHandler.ThrowOnFailure(Config.get_DisplayName(DisplayName))
            Debug.Assert(DisplayName IsNot Nothing AndAlso DisplayName <> "")

            'The configuration name and platform name are separated by a vertical bar.  The configuration
            '  part is the only portion that is user-defined.  Although the shell doesn't allow vertical bar
            '  in the configuration name, let's not take chances, so we'll find the last vertical bar in the
            '  string.
            Dim IndexOfBar As Integer = DisplayName.LastIndexOf("|"c)
            If IndexOfBar = 0 Then
                'It is possible that some old projects' configurations may not have the platform in the name.
                '  In this case, the correct thing to do is assume the platform is "Any CPU"
                ConfigName = DisplayName
                PlatformName = "Any CPU"
            Else
                ConfigName = DisplayName.Substring(0, IndexOfBar)
                PlatformName = DisplayName.Substring(IndexOfBar + 1)
            End If

            Debug.Assert(ConfigName <> "" AndAlso PlatformName <> "")
        End Sub


        ''' <summary>
        ''' Returns whether or not we're in simplified config mode for this project, which means that
        '''   we hide the configuration/platform comboboxes.
        ''' </summary>
        ''' <param name="ProjectHierarchy">The hierarchy to check</param>
        ''' <remarks></remarks>
        Public Shared Function GetIsSimplifiedConfigMode(ByVal ProjectHierarchy As IVsHierarchy) As Boolean
            Try
                If ProjectHierarchy IsNot Nothing Then
                    Dim Project As Project = DTEProjectFromHierarchy(ProjectHierarchy)
                    If Project IsNot Nothing Then
                        Return CanHideConfigurationsForProject(ProjectHierarchy) AndAlso Not ToolsOptionsShowAdvancedBuildConfigurations(Project.DTE)
                    End If
                End If
            Catch ex As Exception
                AppDesCommon.RethrowIfUnrecoverable(ex)
                Debug.Fail("Exception determining if we're in simplified configuration mode - default to advanced configs mode")
            End Try

            Return False 'Default to advanced configs
        End Function


        ''' <summary>
        ''' Returns whether it's permissible to hide configurations for this project.  This should normally
        '''   be returned as true until the user changes any of the default configurations (i.e., adds, deletes
        '''   or removes a configuration, at which point the project wants to show the advanced settings
        '''   from then on out).
        ''' </summary>
        ''' <param name="ProjectHierarchy">The project hierarchy to check</param>
        ''' <remarks></remarks>
        Private Shared Function CanHideConfigurationsForProject(ByVal ProjectHierarchy As IVsHierarchy) As Boolean
            Dim ReturnValue As Boolean = False 'If failed to get config value, default to not hiding configs

            Dim ConfigProviderObject As Object = Nothing
            Dim ConfigProvider As IVsCfgProvider2 = Nothing
            If VSErrorHandler.Succeeded(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, ConfigProviderObject)) Then
                ConfigProvider = TryCast(ConfigProviderObject, IVsCfgProvider2)
            End If

            If ConfigProvider IsNot Nothing Then
                Dim ValueObject As Object = Nothing

                'Ask the project system if configs can be hidden
                Dim hr As Integer = ConfigProvider.GetCfgProviderProperty(__VSCFGPROPID2.VSCFGPROPID_HideConfigurations, ValueObject)

                If VSErrorHandler.Succeeded(hr) AndAlso TypeOf ValueObject Is Boolean Then
                    ReturnValue = CBool(ValueObject)
                Else
                    Debug.Fail("Failed to get VSCFGPROPID_HideConfigurations from project config provider")
                    ReturnValue = False
                End If
            End If

            Return ReturnValue
        End Function

        ''' <summary>
        ''' Retrieves the current value of the "Show Advanced Build Configurations" options in
        '''   Tools.Options.
        ''' </summary>
        ''' <param name="DTE">The DTE extensibility object</param>
        ''' <remarks></remarks>
        Private Shared Function ToolsOptionsShowAdvancedBuildConfigurations(ByVal DTE As DTE) As Boolean
            'Now check for if the Tools option setting to show Advanced Config Settings is on
            Dim ShowAdvancedBuildIntValue As Integer = -1

            Dim ShowValue As Boolean
            Dim ProjAndSolutionProperties As EnvDTE.Properties
            Const EnvironmentCategory As String = "Environment"
            Const ProjectsAndSolution As String = "ProjectsandSolution"

            Try
                ProjAndSolutionProperties = DTE.Properties(EnvironmentCategory, ProjectsAndSolution)
                If ProjAndSolutionProperties IsNot Nothing Then
                    ShowValue = CBool(ProjAndSolutionProperties.Item("ShowAdvancedBuildConfigurations").Value)
                Else
                    Debug.Fail("Couldn't get ProjAndSolutionProperties property from DTE.Properties")
                    ShowValue = True 'If can't get to the property, assume advanced mode
                End If
            Catch ex As Exception
                AppDesCommon.RethrowIfUnrecoverable(ex)
                Debug.Fail("Couldn't get ShowAdvancedBuildConfigurations property from tools.options")
                Return True 'default to showing advanced
            End Try

            Return ShowValue
        End Function


        ''' <summary>
        ''' Given an IVsHierarchy, fetch the DTE Project for it, if it exists.  For project types that 
        '''   don't support this, returns Nothing (e.g. C++).
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function DTEProjectFromHierarchy(ByVal ProjectHierarchy As IVsHierarchy) As Project
            If ProjectHierarchy Is Nothing Then
                Return Nothing
            End If

            Dim hr As Integer
            Dim Obj As Object = Nothing
            hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, Obj)
            If VSErrorHandler.Succeeded(hr) Then
                Return TryCast(Obj, EnvDTE.Project)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' Given a DTE Project, get the hierarchy corresponding to it.
        ''' </summary>
        ''' <param name="sp"></param>
        ''' <param name="project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function VsHierarchyFromDTEProject(ByVal sp As IServiceProvider, ByVal project As Project) As IVsHierarchy
            Debug.Assert(sp IsNot Nothing)
            If sp Is Nothing OrElse project Is Nothing Then
                Return Nothing
            End If

            Dim vssolution As IVsSolution = TryCast(sp.GetService(GetType(IVsSolution)), IVsSolution)
            If vssolution IsNot Nothing Then
                Dim hierarchy As IVsHierarchy = Nothing
                If VSErrorHandler.Succeeded(vssolution.GetProjectOfUniqueName(project.UniqueName, hierarchy)) Then
                    Return hierarchy
                Else
                    Debug.Fail("Why didn't we get the hierarchy from the project?")
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Returns the IVsCfgProvider2 for the given project hierarchy
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetConfigProvider(ByVal ProjectHierarchy As IVsHierarchy) As IVsCfgProvider2
            'CONSIDER: This will not work for all project types because they do not support this property.
            Dim ConfigProvider As Object = Nothing
            If VSErrorHandler.Failed(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, ConfigProvider)) Then
                Return Nothing
            End If
            Return TryCast(ConfigProvider, IVsCfgProvider2)
        End Function

        ''' <summary>
        ''' Given a hierarhy, determine if this is a devices project...
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IsDeviceProject(ByVal hierarchy As IVsHierarchy) As Boolean
            If hierarchy Is Nothing Then
                Debug.Fail("I can't determine if this is a devices project from a NULL hierarchy!?")
                Return False
            End If

            Dim vsdProperty As Object = Nothing
            Dim hr As Integer = hierarchy.GetProperty(VSITEMID.ROOT, 8000, vsdProperty)
            If AppDesInterop.NativeMethods.Succeeded(hr) AndAlso vsdProperty IsNot Nothing AndAlso TryCast(vsdProperty, IVSDProjectProperties) IsNot Nothing Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Is this a Venus project?
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns>true if it is a venus project</returns>
        ''' <remarks></remarks>
        Public Shared Function IsVenusProject(ByVal hierarchy As IVsHierarchy) As [Boolean]

            If hierarchy Is Nothing Then
                Return False
            End If

            Try
                Dim project As EnvDTE.Project = DTEProjectFromHierarchy(hierarchy)

                If project Is Nothing Then
                    Return False
                End If

                If String.Equals(project.Kind, VsWebSite.PrjKind.prjKindVenusProject, System.StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Catch ex As Exception
                ' We failed. Assume that this isn't a web project...
            End Try
            Return False
        End Function


        ''' <summary>
        ''' Is this a Silver Light project
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IsSilverLightProject(ByVal hierarchy As IVsHierarchy) As [Boolean]
            Const SilverLightProjectGuid As String = "{A1591282-1198-4647-A2B1-27E5FF5F6F3B}"

            If hierarchy Is Nothing Then
                Return False
            End If

            Try
                ' VS SilverLight Projects are traditional vb/c# apps, but 'flavored' to add functionality
                ' for ASP.Net.  This flavoring is marked by adding a guid to the AggregateProjectType guids
                ' Get the project type guid list
                Dim guidList As New System.Collections.Generic.List(Of Guid)

                Dim SLPGuid As New Guid(SilverLightProjectGuid)

                Dim aggregatableProject As IVsAggregatableProject = TryCast(hierarchy, IVsAggregatableProject)
                If aggregatableProject IsNot Nothing Then
                    Dim guidStrings As String = Nothing
                    '  The project guids string looks like "{Guid 1};{Guid 2};...{Guid n}" with Guid n the inner most
                    aggregatableProject.GetAggregateProjectTypeGuids(guidStrings)

                    For Each guidString As String In guidStrings.Split(New Char() {";"c})
                        If guidString <> "" Then
                            ' Insert Guid to the front
                            Try
                                Dim flavorGuid As New Guid(guidString)
                                If SLPGuid.Equals(flavorGuid) Then
                                    Return True
                                End If
                            Catch ex As Exception
                                System.Diagnostics.Debug.Fail(String.Format("We received a broken guid string from IVsAggregatableProject: '{0}'", guidStrings))
                            End Try
                        End If
                    Next
                Else
                    '  Should not happen, but if they decide to make this project type non-flavored.
                    Dim typeGuid As Guid = Nothing
                    VSErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_TypeGuid, typeGuid))
                    If SLPGuid.Equals(typeGuid) Then
                        Return True
                    End If
                End If
            Catch ex As Exception
                ' We failed. Assume that this isn't a web project...
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Is this a web (Venus WSP or WAP project)
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IsWebProject(ByVal hierarchy As IVsHierarchy) As [Boolean]
            Const WebAppProjectGuid As String = "{349c5851-65df-11da-9384-00065b846f21}"

            If hierarchy Is Nothing Then
                Return False
            End If

            Try
                If IsVenusProject(hierarchy) Then
                    Return True
                End If

                ' VS WAP Projects are traditional vb/c# apps, but 'flavored' to add functionality
                ' for ASP.Net.  This flavoring is marked by adding a guid to the AggregateProjectType guids
                ' Get the project type guid list
                Dim guidList As New System.Collections.Generic.List(Of Guid)

                Dim WAPGuid As New Guid(WebAppProjectGuid)

                Dim aggregatableProject As IVsAggregatableProject = TryCast(hierarchy, IVsAggregatableProject)
                If aggregatableProject IsNot Nothing Then
                    Dim guidStrings As String = Nothing
                    '  The project guids string looks like "{Guid 1};{Guid 2};...{Guid n}" with Guid n the inner most
                    aggregatableProject.GetAggregateProjectTypeGuids(guidStrings)

                    For Each guidString As String In guidStrings.Split(New Char() {";"c})
                        If guidString <> "" Then
                            ' Insert Guid to the front
                            Try
                                Dim flavorGuid As New Guid(guidString)
                                If WAPGuid.Equals(flavorGuid) Then
                                    Return True
                                End If
                            Catch ex As Exception
                                System.Diagnostics.Debug.Fail(String.Format("We received a broken guid string from IVsAggregatableProject: '{0}'", guidStrings))
                            End Try
                        End If
                    Next
                Else
                    '  Should not happen, but if they decide to make this project type non-flavored.
                    Dim typeGuid As Guid = Nothing
                    VSErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_TypeGuid, typeGuid))
                    If Guid.Equals(WAPGuid, typeGuid) Then
                        Return True
                    End If
                End If
            Catch ex As Exception
                ' We failed. Assume that this isn't a web project...
            End Try
            Return False
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="fileName">IN: name of the file to get the document info from</param>
        ''' <param name="rdt">IN: Running document table to find the info in</param>
        ''' <param name="hierarchy">OUT: Hierarchy that the document was found in</param>
        ''' <param name="itemid">OUT: Found itemId</param>
        ''' <param name="readLocks">OUT: Number of read locks for the document</param>
        ''' <param name="editLocks">OUT: Number of edit locks on the document</param>
        ''' <param name="docCookie">OUT: A cookie for the doc, 0 if the doc isn't found in the RDT</param>
        ''' <remarks></remarks>
        Public Shared Sub GetDocumentInfo(ByVal fileName As String, ByVal rdt As IVsRunningDocumentTable, ByRef hierarchy As IVsHierarchy, ByRef readLocks As UInteger, ByRef editLocks As UInteger, ByRef itemid As UInteger, ByRef docCookie As UInteger)
            If fileName Is Nothing Then Throw New ArgumentNullException("fileName")
            If rdt Is Nothing Then Throw New ArgumentNullException("rdt")

            '
            ' Initialize out parameters...
            '
            readLocks = 0
            editLocks = 0
            itemid = VSITEMID.NIL
            docCookie = 0
            hierarchy = Nothing

            ' Now, look in the RDT to see if this doc data already has an edit lock on it.
            ' if it does, we keep it and we begin tracking changes.  Otherwise, we
            ' let it get disposed.
            '
            Dim flags As UInteger
            Dim localPunk As IntPtr = IntPtr.Zero
            Dim localFileName As String = Nothing

            Try
                VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), fileName, hierarchy, itemid, localPunk, docCookie))
            Finally
                If (localPunk <> IntPtr.Zero) Then
                    System.Runtime.InteropServices.Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            Try
                VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hierarchy, itemid, localPunk))
            Finally
                If (localPunk <> IntPtr.Zero) Then
                    System.Runtime.InteropServices.Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Get the name of a project item as well as a SFG generated child item (if any)
        ''' Used in order to check out all dependent files for a project item
        ''' </summary>
        ''' <param name="projectitem">The parent project item that is to be checked out</param>
        ''' <param name="suffix">Suffix added by the single file generator</param>
        ''' <param name="requireExactlyOneChild">
        ''' Only add the child item to the list of items to check out if there is exactly one child
        ''' project item.
        ''' </param>
        ''' <param name="exclude">
        ''' Predicate used to filter items that we don't want to check out.
        ''' The predicate is passed each full path to the project item, and if it returns
        ''' true, the item will not be added to the list of items to check out.
        ''' </param>
        ''' <returns>
        ''' The list of items that are to be checked out
        ''' </returns>
        ''' <remarks></remarks>
        Public Shared Function FileNameAndGeneratedFileName(ByVal projectitem As EnvDTE.ProjectItem, _
                                                            Optional ByVal suffix As String = ".Designer", _
                                                            Optional ByVal requireExactlyOneChild As Boolean = True, _
                                                            Optional ByVal exclude As Predicate(Of String) = Nothing) _
                               As Collections.Generic.List(Of String)

            Dim result As New List(Of String)

            If projectitem IsNot Nothing AndAlso projectitem.Name <> "" Then
                result.Add(DTEUtils.FileNameFromProjectItem(projectitem))
            End If

            ' For each child, check if the name matches the filename for the generated file
            If projectitem IsNot Nothing AndAlso projectitem.ProjectItems IsNot Nothing Then
                ' If we require exactly one child, we better check the number of children
                ' and bail if more than one child.
                If projectitem.ProjectItems.Count = 1 OrElse Not requireExactlyOneChild Then
                    For childNo As Integer = 1 To projectitem.ProjectItems.Count
                        Try
                            Dim childItemName As String = DTEUtils.FileNameFromProjectItem(projectitem.ProjectItems.Item(childNo))

                            ' Make sure that the filename matches what we expect.
                            If String.Equals( _
                                System.IO.Path.GetFileNameWithoutExtension(childItemName), _
                                System.IO.Path.GetFileNameWithoutExtension(DTEUtils.FileNameFromProjectItem(projectitem)) & suffix, _
                                StringComparison.OrdinalIgnoreCase) _
                            Then
                                ' If we've got a filter predicate, we remove anything that we've been
                                ' told we shouldn't check out...
                                Dim isExcluded As Boolean = exclude IsNot Nothing AndAlso exclude.Invoke(childItemName)
                                If Not isExcluded Then
                                    result.Add(childItemName)
                                End If
                            End If
                        Catch ex As ArgumentException
                            ' If the child name wasn't a file moniker, then we may throw an argument exception here...
                            '
                            ' Don't really care about that scenario!
                        End Try
                    Next
                End If
            End If

            Return result

        End Function

        '''<summary>
        ''' a fake IVSDProjectProperties definition. We only use this to check whether the project supports this interface, but don't pay attention to the detail.
        '''</summary>
        <System.Runtime.InteropServices.ComImport(), System.Runtime.InteropServices.ComVisible(False), System.Runtime.InteropServices.Guid("1A27878B-EE15-41CE-B427-58B10390C821"), System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)> _
        Private Interface IVSDProjectProperties
        End Interface

        ''' <summary>
        ''' Wrapper class for IVsShell.OnBroadcastMessage
        ''' </summary>
        ''' <remarks></remarks>
        Public Class BroadcastMessageEventsHelper
            Implements IVsBroadcastMessageEvents
            Implements IDisposable

            Public Event BroadcastMessage(ByVal msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr)

            'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
            Private m_CookieBroadcastMessages As UInteger
            Private m_ServiceProvider As IServiceProvider

            Public Sub New(ByVal sp As IServiceProvider)
                m_ServiceProvider = sp
                ConnectBroadcastEvents()
            End Sub


#Region "Helper methods to advise/unadvise broadcast messages from the IVsShell service"

            Public Sub ConnectBroadcastEvents()
                Dim VSShell As IVsShell = Nothing
                If m_ServiceProvider IsNot Nothing Then
                    VSShell = DirectCast(m_ServiceProvider.GetService(GetType(IVsShell)), IVsShell)
                End If
                If VSShell IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VSShell.AdviseBroadcastMessages(Me, m_CookieBroadcastMessages))
                Else
                    Debug.Fail("Unable to get IVsShell for broadcast messages")
                End If
            End Sub

            Private Sub DisconnectBroadcastMessages()
                If m_CookieBroadcastMessages <> 0 Then
                    Dim VsShell As IVsShell = DirectCast(m_ServiceProvider.GetService(GetType(IVsShell)), IVsShell)
                    If VsShell IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(VsShell.UnadviseBroadcastMessages(m_CookieBroadcastMessages))
                        m_CookieBroadcastMessages = 0
                    End If
                End If
            End Sub

#End Region

            ''' <summary>
            ''' Forward to overridable OnBrodcastMessage handler
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Private Function IVsBroadcastMessageEvents_OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr) As Integer Implements Shell.Interop.IVsBroadcastMessageEvents.OnBroadcastMessage
                OnBroadcastMessage(msg, wParam, lParam)
                Return AppDesInterop.NativeMethods.S_OK
            End Function

            ''' <summary>
            ''' Raise OnBroadcastMessage event. Can be overridden to implement custom handling of broadcast messages
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            ''' <remarks></remarks>
            Protected Overridable Sub OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr)
                RaiseEvent BroadcastMessage(msg, wParam, lParam)
            End Sub

#Region "Standard dispose pattern - the only thing we need to do is to unadvise events..."

            Private disposed As Boolean = False

            ' IDisposable
            Private Overloads Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposed Then
                    If disposing Then
                        DisconnectBroadcastMessages()
                    End If
                End If
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
#End Region

        End Class

        ''' <summary>
        ''' Monitor and set font when font changes...
        ''' </summary>
        ''' <remarks></remarks>
        Public NotInheritable Class FontChangeMonitor
            Inherits BroadcastMessageEventsHelper

            ' Control that we are going to set the font on (if any)
            Private m_Control As System.Windows.Forms.Control

            Private m_ServiceProvider As IServiceProvider

            ''' <summary>
            ''' Create a new instance...
            ''' </summary>
            ''' <param name="sp"></param>
            ''' <param name="ctrl"></param>
            ''' <param name="SetFontInitially">If true, set the font of the provided control when this FontChangeMonitor is created</param>
            ''' <remarks></remarks>
            Public Sub New(ByVal sp As IServiceProvider, ByVal ctrl As System.Windows.Forms.Control, ByVal SetFontInitially As Boolean)
                MyBase.new(sp)

                Debug.Assert(sp IsNot Nothing, "Why did we get a NULL service provider!?")
                Debug.Assert(ctrl IsNot Nothing, "Why didn't we get a control to provide the dialog font for!?")

                m_ServiceProvider = sp
                m_Control = ctrl

                If SetFontInitially Then
                    m_Control.Font = GetDialogFont(sp)
                End If
            End Sub

            ''' <summary>
            ''' Override to get WM_SETTINGCHANGE notifications and set the font accordingly...
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            ''' <remarks></remarks>
            Protected Overrides Sub OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr)
                MyBase.OnBroadcastMessage(msg, wParam, lParam)

                If m_Control IsNot Nothing Then
                    If msg = AppDesInterop.win.WM_SETTINGCHANGE Then
                        ' Only set font if it is different from the current font...
                        Dim newFont As Font = GetDialogFont(m_ServiceProvider)
                        If Not newFont.Equals(m_Control.Font) Then
                            m_Control.Font = newFont
                        End If
                    End If
                End If
            End Sub

            ''' <summary>
            ''' Pick current dialog font...
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Shared ReadOnly Property GetDialogFont(ByVal ServiceProvider As IServiceProvider) As Font
                Get
                    If ServiceProvider IsNot Nothing Then
                        Dim uiSvc As System.Windows.Forms.Design.IUIService = CType(ServiceProvider.GetService(GetType(System.Windows.Forms.Design.IUIService)), System.Windows.Forms.Design.IUIService)
                        If uiSvc IsNot Nothing Then
                            Return CType(uiSvc.Styles("DialogFont"), Font)
                        End If
                    End If

                    Debug.Fail("Couldn't get a IUIService... cheating instead :)")

                    Return System.Windows.Forms.Form.DefaultFont
                End Get
            End Property
        End Class


        ''' <summary>
        ''' Determine if the specified custom tool is registered for the current project system
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy to check if the custom tool is registered for</param>
        ''' <param name="customToolName">Name of custom tool to look for</param>
        ''' <returns>True if registered, false otherwise</returns>
        ''' <remarks></remarks>
        Public Shared Function IsCustomToolRegistered(ByVal hierarchy As IVsHierarchy, ByVal customToolName As String) As Boolean
            If hierarchy Is Nothing Then Throw New ArgumentNullException("hierarchy")
            If customToolName Is Nothing Then Throw New ArgumentNullException("customToolName")

            ' All project systems support empty string (= no custom tool)
            If customToolName.Length = 0 Then Return True

            Dim sfgFactory As IVsSingleFileGeneratorFactory = TryCast(hierarchy, IVsSingleFileGeneratorFactory)
            If sfgFactory Is Nothing Then
                ' If the hierarchy doesn't support IVsSingleFileGeneratorFactory, then we assume that
                ' the custom tools aren't supported by the project system.
                Return False
            End If

            Dim pbGeneratesDesignTimeSource As Integer
            Dim pbGeneratesSharedDesignTimeSource As Integer
            Dim pbUseTempPEFlag As Integer
            Dim pguidGenerator As System.Guid

            Dim hr As Integer = sfgFactory.GetGeneratorInformation(customToolName, pbGeneratesDesignTimeSource, pbGeneratesSharedDesignTimeSource, pbUseTempPEFlag, pguidGenerator)

            If VSErrorHandler.Succeeded(hr) Then
                Return True
            Else
                Return False
            End If
        End Function


        ''' <summary>
        ''' Check if there's WCF import/generation error/warning. If there's one, force to display the error list page
        ''' </summary>
        ''' <param name="serviceProvider"></param>
        ''' <param name="proxyGenerationErrors"></param>
        ''' <returns>If serviceProvider, (Import)proxyGenerationErrors or no error, return S_OK,
        '''  else, return the result from IVsErrorList.BringToFront() </returns>
        ''' <remarks></remarks>
        Public Shared Function CheckAndDisplayWcfErrorList(ByVal serviceProvider As IServiceProvider, _
                                                           ByVal proxyGenerationErrors As IEnumerable(Of Microsoft.VSDesigner.WCFModel.ProxyGenerationError), _
                                                           ByVal importErrors As IEnumerable(Of Microsoft.VSDesigner.WCFModel.ProxyGenerationError)) As Integer

            If serviceProvider Is Nothing Then
                Return VSConstants.S_OK
            End If

            Dim totalNumOfErrors As Integer = 0

            If proxyGenerationErrors IsNot Nothing Then
                totalNumOfErrors = totalNumOfErrors + proxyGenerationErrors.Count()
            End If

            If importErrors IsNot Nothing Then
                totalNumOfErrors = totalNumOfErrors + importErrors.Count()
            End If

            Dim vsErrorList As Microsoft.VisualStudio.Shell.Interop.IVsErrorList
            Dim result As Integer = VSConstants.S_OK
            ' Get the service for Error List tab window
            vsErrorList = CType(serviceProvider.GetService(GetType(Microsoft.VisualStudio.Shell.Interop.SVsErrorList)), Microsoft.VisualStudio.Shell.Interop.IVsErrorList)

            If vsErrorList IsNot Nothing AndAlso totalNumOfErrors > 0 Then
                result = vsErrorList.BringToFront()
            End If

            Return result
        End Function

        Public Shared Function GetServiceProvider(ByVal dte As DTE) As IServiceProvider
            Return New Microsoft.VisualStudio.Shell.ServiceProvider(DirectCast(dte, Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
        End Function

    End Class

End Namespace
