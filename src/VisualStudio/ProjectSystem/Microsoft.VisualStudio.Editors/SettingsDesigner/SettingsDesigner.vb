'------------------------------------------------------------------------------
' <copyright from='2003' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.interop

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The root designer for settings
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SettingsDesigner
        Inherits DesignerFramework.BaseRootDesigner
        Implements IRootDesigner

        Friend Const SETTINGS_FILE_EXTENSION As String = ".settings"

        Friend Const ApplicationScopeName As String = "Application"
        Friend Const UserScopeName As String = "User"
        Friend Const CultureInvariantDefaultProfileName As String = "(Default)"
        Private Const SpecialClassName As String = "MySettings"

        ' Our view
        Private m_SettingsDesignerViewProperty As SettingsDesignerView

        ''' <summary>
        ''' Trace switch used by all SettingsDesigner components - should be moved to the common Swithces file
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Shared ReadOnly Property TraceSwitch() As Diagnostics.TraceSwitch
            Get
                Static MyTraceSwitch As New Diagnostics.TraceSwitch("SettingsDesigner", "Tracing for settings designer")
                Return MyTraceSwitch
            End Get
        End Property

        ''' <summary>
        ''' Demand-crete our designer view 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property View() As SettingsDesignerView
            Get
                If m_SettingsDesignerViewProperty Is Nothing Then
                    Debug.WriteLineIf(TraceSwitch.TraceVerbose, "Creating SettingsDesignerView")
                    m_SettingsDesignerViewProperty = New SettingsDesignerView
                    m_SettingsDesignerViewProperty.SetDesigner(Me)
                End If
                Return m_SettingsDesignerViewProperty
            End Get
        End Property

        ''' <summary>
        ''' Have we already created a view?
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property HasView() As Boolean
            Get
                Return m_SettingsDesignerViewProperty IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Publicly expose our view
        ''' </summary>
        ''' <param name="technology"></param>
        ''' <returns>The view for this root designer</returns>
        ''' <remarks></remarks>
        Public Function GetView(ByVal technology As System.ComponentModel.Design.ViewTechnology) As Object Implements System.ComponentModel.Design.IRootDesigner.GetView
            If technology <> ViewTechnology.Default Then
                Debug.Fail("Unsupported view technology!")
                Throw New NotSupportedException()
            End If

            Return View
        End Function


        ''' <summary>
        ''' Our supported technologies
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property SupportedTechnologies() As System.ComponentModel.Design.ViewTechnology() Implements System.ComponentModel.Design.IRootDesigner.SupportedTechnologies
            Get
                Return New ViewTechnology() {ViewTechnology.Default}
            End Get
        End Property

        ''' <summary>
        ''' Get access to all our settings
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property Settings() As DesignTimeSettings
            Get
                Return Component
            End Get
        End Property

        ''' <summary>
        ''' Commit any pending changes
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CommitPendingChanges(ByVal suppressValidationUI As Boolean, ByVal cancelOnValidationFailure As Boolean)
            If m_SettingsDesignerViewProperty IsNot Nothing Then
                m_SettingsDesignerViewProperty.CommitPendingChanges(suppressValidationUI, cancelOnValidationFailure)
            End If
        End Sub

#Region "Component overrides and shadows"

        ''' <summary>
        ''' Make component property type safe if we want to access the component through
        ''' a SettingsDesigner instance
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shadows ReadOnly Property Component() As DesignTimeSettings
            Get
                Return CType(MyBase.Component, DesignTimeSettings)
            End Get
        End Property
#End Region


        ''' <summary>
        ''' Show context menu
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Public Overloads Sub ShowContextMenu(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            MyBase.ShowContextMenu(Constants.MenuConstants.SettingsDesignerContextMenuID, e.X, e.Y)
        End Sub

        Protected Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                If m_SettingsDesignerViewProperty IsNot Nothing Then
                    Debug.WriteLineIf(TraceSwitch.TraceVerbose, "Disposing SettingsDesignerView")
                    m_SettingsDesignerViewProperty.Dispose()
                    m_SettingsDesignerViewProperty = Nothing
                End If
            End If
            MyBase.Dispose(Disposing)
        End Sub

#Region "Helper methods to determine the settings class name"

        '''<summary>
        '''Get the fully qualified settings class name
        '''</summary>
        '''<param name="Hierarchy"></param>
        '''<param name="Item"></param>
        '''<returns></returns>
        '''<remarks></remarks>
        Friend Shared Function FullyQualifiedGeneratedTypedSettingsClassName(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal Settings As DesignTimeSettings, ByVal Item As EnvDTE.ProjectItem) As String
            Dim Ns As String
            Ns = ProjectUtils.GeneratedSettingsClassNamespace(Hierarchy, ProjectUtils.ItemId(Hierarchy, Item), True)
            Return ProjectUtils.FullyQualifiedClassName(Ns, SettingsDesigner.GeneratedClassName(Hierarchy, ItemId, Settings, ProjectUtils.FileName(Item)))
        End Function


        ''' <summary>
        ''' Helper method to determine the generated class name...
        ''' 
        ''' If this is a VB project, and it is the default .settings file, and the magic UseMySettingsClassName flag is set in the .settings
        ''' file, we will use the name "MySettings" instead of basing the classname off the filename 
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="itemId"></param>
        ''' <param name="Settings"></param>
        ''' <param name="FullPath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function GeneratedClassName(ByVal Hierarchy As IVsHierarchy, ByVal itemId As UInteger, Optional ByVal Settings As DesignTimeSettings = Nothing, Optional ByVal FullPath As String = Nothing) As String
            Try
                If itemId = VSITEMID.NIL AndAlso FullPath = "" Then
                    Debug.Fail("Must supply either an itemid or a full path to determine the class name")
                    Return ""
                End If

                ' If we didn't get a full path, let's compute it from the hierarchy and itemid
                If FullPath = "" AndAlso itemId <> VSITEMID.NIL Then
                    Dim projItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(Hierarchy, itemId)
                    FullPath = Common.DTEUtils.FileNameFromProjectItem(projItem)
                End If

                '
                ' If this is a VB project, and it is the default settings file, and the default settings file has the magic
                ' UsMySettingsName flag set, we special-case the class name...
                '
                ' First, we have to figure out if this is a vb project...
                '
                Dim isVbProject As Boolean = False
                If Hierarchy IsNot Nothing Then
                    isVbProject = Common.Utils.IsVbProject(Hierarchy)
                End If

                If isVbProject AndAlso _
                    ((itemId <> VSITEMID.NIL AndAlso IsDefaultSettingsFile(Hierarchy, itemId)) _
                    OrElse (FullPath <> "" AndAlso IsDefaultSettingsFile(Hierarchy, FullPath))) _
                Then
                    '
                    ' Now, since this is a VB project, and it is the default settings file, 
                    ' we check the UseSpecialClassName flag
                    ' To do so, we've got to crack the .settings file open if this is not already
                    ' done...
                    '
                    Try
                        If Settings Is Nothing Then
                            ' 
                            ' No settings class provided - let's crack open the .settings file... 
                            '
                            Settings = New DesignTimeSettings()
                            Using Reader As New System.IO.StreamReader(FullPath)
                                SettingsSerializer.Deserialize(Settings, Reader, True)
                            End Using
                        End If

                        If Settings.UseSpecialClassName Then
                            Return SettingsDesigner.SpecialClassName
                        End If
                    Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        Debug.Fail(String.Format("Failed to crack open {0} to determine if we were supposed to use the ""Special"" settings class name: {1}", FullPath, ex))
                    End Try
                End If

                '
                ' Not a special case - let's return the "normal" class name which is based on the file name...
                '
                Return GeneratedClassNameFromPath(FullPath)
            Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                Debug.Fail(String.Format("Failed to determine if we were supposed to use the ""Special"" settings class name: {0}", ex))
            End Try
            Return ""
        End Function

        ''' <summary>
        ''' The class name is basically the file name minus the file extension...
        ''' </summary>
        ''' <param name="PathName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GeneratedClassNameFromPath(ByVal PathName As String) As String
            If PathName Is Nothing Then
                System.Diagnostics.Debug.Fail("Can't get a class name from an empty path!")
                Return ""
            End If
            Return System.IO.Path.GetFileNameWithoutExtension(PathName)
        End Function

        ''' <summary>
        ''' Is this the default settings file?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="itemId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsDefaultSettingsFile(ByVal Hierarchy As IVsHierarchy, ByVal itemId As UInteger) As Boolean
            If itemId = VSITEMID.NIL OrElse itemid = vsitemid.ROOT OrElse itemid = vsitemid.SELECTION Then
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return False
            End If

            Dim DefaultSettingsItemId As UInteger
            Dim DefaultSettingsFilePath As String = Nothing
            Dim hr As Integer = SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, 0, DefaultSettingsItemId, DefaultSettingsFilePath)

            If VSErrorHandler.Succeeded(hr) AndAlso itemId = DefaultSettingsItemId Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is this the "default" settings file
        ''' </summary>
        ''' <param name="FilePath">Fully qualified path of file to check</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsDefaultSettingsFile(ByVal Hierarchy As IVsHierarchy, ByVal FilePath As String) As Boolean
            If Hierarchy Is Nothing Then
                Debug.Fail("Passed in a NULL hiearchy - can't figure out if this is the default settings file")
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return False
            End If

            Dim DefaultSettingsItemId As UInteger
            Dim DefaultSettingsFilePath As String = Nothing

            Dim hr As Integer = SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), DefaultSettingsItemId, DefaultSettingsFilePath)
            If NativeMethods.Succeeded(hr) Then
                If DefaultSettingsItemId <> VSITEMID.NIL Then
                    Dim NormalizedDefaultSettingFilePath As String = System.IO.Path.GetFullPath(DefaultSettingsFilePath)
                    Dim NormalizedSettingFilePath As String = System.IO.Path.GetFullPath(FilePath)
                    Return String.Equals(NormalizedDefaultSettingFilePath, NormalizedSettingFilePath, StringComparison.OrdinalIgnoreCase)
                End If
            Else
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End If
            Return False
        End Function

#End Region

#Region "Sync user config files"

        ''' <summary>
        ''' Find all user config files that are associated with this application
        ''' </summary>
        ''' <param name="DIrectories"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function FindUserConfigFiles(ByVal Directories As List(Of String)) As List(Of String)
            Dim result As New List(Of String)
            For Each directory As String In Directories
                AddUserConfigFiles(directory, result)
            Next
            Return result
        End Function

        ''' <summary>
        ''' Find all directories that we are going to search through to find user.config files
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function FindUserConfigDirectories(ByVal hierarchy As IVsHierarchy) As List(Of String)
            Dim result As New List(Of String)
            Dim ConfigHelper As New Shell.Design.Serialization.ConfigurationHelperService

            ' No hierarchy - can't find any user.config files...
            If hierarchy Is Nothing Then
                Return result
            End If

            Dim hierSp As IServiceProvider = Common.Utils.ServiceProviderFromHierarchy(hierarchy)
            Dim project As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(hierarchy)

            If project Is Nothing OrElse project.ConfigurationManager Is Nothing Then
                Return result
            End If

            For Each BuildConfiguration As EnvDTE.Configuration In project.ConfigurationManager
                Try
                    '
                    ' Add all combinations of under VSHost/not under VSHost/Roaming/Local User paths...
                    '

                    Dim path As String
                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoaming, True, BuildConfiguration)
                    If path IsNot Nothing Then
                        path = System.IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If

                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoaming, False, BuildConfiguration)
                    If path IsNot Nothing Then
                        path = System.IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If

                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal, True, BuildConfiguration)
                    If path IsNot Nothing Then
                        path = System.IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If

                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal, False, BuildConfiguration)
                    If path IsNot Nothing Then
                        path = System.IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If
                Catch ex As ArgumentException
                    ' Failed to get one or more paths...
                End Try
            Next
            Return result
        End Function

        ''' <summary>
        ''' Find all user config files associated with this application given the conditions applied
        ''' </summary>
        ''' <param name="path"></param>
        ''' <param name="files"></param>
        ''' <remarks></remarks>
        Friend Shared Sub AddUserConfigFiles(ByVal path As String, ByVal files As List(Of String))
            Debug.WriteLineIf(Common.Switches.SDSyncUserConfig.TraceInfo, String.Format("SettingsDesigner::DeleteUserConfig, path={0}", path))

            If path = "" Then
                Return
            End If


            ' The path passed in to us is the path to the current active user.config file..
            Dim currentApplicationVersionDirectoryInfo As New System.IO.DirectoryInfo(path)

            ' The application may have scribbled user.config files in sibling directories to the current version's
            ' directory, so we'll start off from there...
            Dim applicationRootDirectoryInfo As System.IO.DirectoryInfo = currentApplicationVersionDirectoryInfo.Parent()

            ' If the parent directory doesn't exist, we are fine...
            If Not applicationRootDirectoryInfo.Exists Then
                Return
            End If

            For Each directory As IO.DirectoryInfo In applicationRootDirectoryInfo.GetDirectories()
                For Each file As IO.FileInfo In directory.GetFiles("user.config")
                    files.Add(file.FullName)
                Next
            Next
        End Sub

        ''' <summary>
        ''' Delete all user configs associated with all versions of the current project
        ''' </summary>
        ''' <param name="files">List of files to delete</param>
        ''' <param name="directories">List of directories to delete (if empty)</param>
        ''' <remarks></remarks>
        Friend Shared Function DeleteFilesAndDirectories(ByVal files As List(Of String), ByVal directories As List(Of String)) As Boolean
            Dim completeSuccess As Boolean = True
            If files IsNot Nothing Then
                For Each file As String In files
                    Try
                        System.IO.File.Delete(file)
                    Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        completeSuccess = False
                    End Try
                Next
            End If

            If directories IsNot Nothing Then
                For Each directory As String In directories
                    Try
                        System.IO.Directory.Delete(directory, False)
                    Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        completeSuccess = False
                    End Try
                Next
            End If
            Return completeSuccess
        End Function
#End Region
    End Class

End Namespace
