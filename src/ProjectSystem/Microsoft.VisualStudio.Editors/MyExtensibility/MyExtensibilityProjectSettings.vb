' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.IO
Imports System.Windows.Forms
Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil
Imports Res = My.Resources.MyExtensibilityRes

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensibilityProjectSettings
    ''' <summary>
    ''' Handles adding My Namespace extension templates to project 
    ''' and manages My Namespace extension project items.
    ''' </summary>
    Friend Class MyExtensibilityProjectSettings

#Region " Public methods "
        Public Event ExtensionChanged()

        Public Shared Function CreateNew( _
                ByVal projectService As MyExtensibilityProjectService, ByVal serviceProvider As IServiceProvider, _
                ByVal project As Project, ByVal projectHierarchy As IVsHierarchy) As MyExtensibilityProjectSettings
            If projectService Is Nothing Then
                Return Nothing
            End If
            If serviceProvider Is Nothing Then
                Return Nothing
            End If
            If project Is Nothing Then
                Return Nothing
            End If
            If projectHierarchy Is Nothing Then
                Return Nothing
            End If
            Dim vsBuildPropertyStorage As IVsBuildPropertyStorage = TryCast(projectHierarchy, IVsBuildPropertyStorage)
            If vsBuildPropertyStorage Is Nothing Then
                Return Nothing
            End If

            Return New MyExtensibilityProjectSettings(projectService, serviceProvider, _
                project, projectHierarchy, vsBuildPropertyStorage)
        End Function

        ''' ;AddExtensionTemplate
        ''' <summary>
        ''' Add the given extension template to the current project.
        ''' </summary>
        Public Function AddExtensionTemplate(ByVal extensionTemplate As MyExtensionTemplate) As List(Of ProjectItem)
            Debug.Assert(extensionTemplate IsNot Nothing, "NULL extensionTemplate!")

            ' Start monitoring the project items being added from the template.
            _monitorState = MonitorState.AddTemplate
            If _projectItemsAddedFromTemplate IsNot Nothing Then
                _projectItemsAddedFromTemplate.Clear()
            End If

            ' Add the template.
            Me.GetMyExtensionsFolderProjectItem()
            Dim suggestedName As String = Me.GetProjectItemName(extensionTemplate.BaseName)
            Debug.Assert(_extensionFolderProjectItem IsNot Nothing, "Could not create MyExtensions folder!")
            Try
                _extensionFolderProjectItem.ProjectItems.AddFromTemplate(extensionTemplate.FilePath, suggestedName)
            Catch ex As Exception When Not Utils.IsUnrecoverable(ex)
                DesignerMessageBox.Show(_serviceProvider, ex, Nothing)
            End Try

            ' Stop monitoring project items being added.
            _monitorState = MonitorState.Normal

            Dim result As List(Of ProjectItem) = Nothing

            If _projectItemsAddedFromTemplate Is Nothing OrElse _projectItemsAddedFromTemplate.Count <= 0 Then
                ' No project items were added, show warning message and return nothing.
                DesignerMessageBox.Show(_serviceProvider, _
                    String.Format(Res.CouldNotAddProjectItemTemplate_Message, extensionTemplate.DisplayName), _
                    Res.CouldNotAddProjectItemTemplate_Title, _
                    MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
            ElseIf _vsBuildPropertyStorage IsNot Nothing Then
                ' Some project items were added, if IVsBuildPropertyStorage is available, attempt to set the attribute.
                result = New List(Of ProjectItem)
                For Each projectItemAdded As ProjectItem In _projectItemsAddedFromTemplate
                    Try
                        Dim extensionProjectItemID As UInteger = DTEUtils.ItemIdOfProjectItem( _
                            _projectHierarchy, projectItemAdded)
                        _vsBuildPropertyStorage.SetItemAttribute(extensionProjectItemID, s_MSBUILD_ATTR_ASSEMBLY, extensionTemplate.AssemblyFullName)
                        _vsBuildPropertyStorage.SetItemAttribute(extensionProjectItemID, s_MSBUILD_ATTR_ID, extensionTemplate.ID)
                        _vsBuildPropertyStorage.SetItemAttribute(extensionProjectItemID, s_MSBUILD_ATTR_VERSION, extensionTemplate.Version.ToString())

                        Me.AddExtensionProjectFile(extensionTemplate.AssemblyFullName, projectItemAdded, _
                            extensionTemplate.ID, extensionTemplate.Version, extensionTemplate.DisplayName, extensionTemplate.Description)

                        result.Add(projectItemAdded)
                    Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex) ' Ignore recoverable exceptions
                        DesignerMessageBox.Show(_serviceProvider, _
                            String.Format(Res.CouldNotSetExtensionAttributes_Message, _
                                projectItemAdded.Name, extensionTemplate.DisplayName), _
                            Res.CouldNotSetExtensionAttributes_Title, _
                            MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                    End Try
                Next
                If result.Count <= 0 Then
                    DesignerMessageBox.Show(_serviceProvider, _
                        String.Format(Res.CouldNotSetExtensionAttributes_AllItems, extensionTemplate.DisplayName), _
                        Res.CouldNotSetExtensionAttributes_Title, _
                        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                End If
            End If

            ' Remove MyExtensions folder if nothing was added.
            Me.RemoveMyExtensionsFolderIfEmpty()

            Return result
        End Function

        ''' ;GetExtensionProjectItemGroup
        ''' <summary>
        ''' Find out whether there is an extension project item group with the same extension ID 
        ''' in the current project. Return Nothing if none is found.
        ''' </summary>
        Public Function GetExtensionProjectItemGroup(ByVal extensionID As String) As MyExtensionProjectItemGroup
            If StringIsNullEmptyOrBlank(extensionID) OrElse _extProjItemGroups Is Nothing Then
                Return Nothing
            End If

            Dim extProjItemGroups As List(Of MyExtensionProjectItemGroup) = _extProjItemGroups.GetAllItems()
            If extProjItemGroups Is Nothing OrElse extProjItemGroups.Count <= 0 Then
                Return Nothing
            End If

            For Each extProjItemGroup As MyExtensionProjectItemGroup In extProjItemGroups
                If extProjItemGroup.IDEquals(extensionID) Then
                    Return extProjItemGroup
                End If
            Next

            Return Nothing
        End Function

        ''' ;GetExtensionProjectItemGroups
        ''' <summary>
        ''' Return a list of all extension project item groups in the current project to display in "My Extensions" property page.
        ''' </summary>
        Public Function GetExtensionProjectItemGroups() As List(Of MyExtensionProjectItemGroup)
            Dim result As List(Of MyExtensionProjectItemGroup) = Nothing
            If _extProjItemGroups IsNot Nothing Then
                result = _extProjItemGroups.GetAllItems()
            End If

            If result Is Nothing OrElse result.Count <= 0 Then
                result = Nothing
            End If
            Return result
        End Function

        ''' ;GetExtensionProjectItemGroups
        ''' <summary>
        ''' Return the extension project item groups added by the given assembly.
        ''' </summary>
        Public Function GetExtensionProjectItemGroups(ByVal assemblyFullName As String) _
                As List(Of MyExtensionProjectItemGroup)
            If assemblyFullName IsNot Nothing AndAlso _
                    _extProjItemGroups IsNot Nothing Then
                Return _extProjItemGroups.GetItems(assemblyFullName)
            Else
                Return Nothing
            End If
        End Function

        ''' ;RemoveExtensionProjectItemGroup
        ''' <summary>
        ''' Remove the given extension project item group from the current project.
        ''' </summary>
        Public Sub RemoveExtensionProjectItemGroup(ByVal extensionProjectItemGroup As MyExtensionProjectItemGroup)
            If extensionProjectItemGroup Is Nothing Then
                Exit Sub
            End If
            If _extensionFolderProjectItem Is Nothing Then
                Exit Sub
            End If

            ' Set the monitor state to pause raising event
            _monitorState = MonitorState.RemoveProjectItem

            If extensionProjectItemGroup.ExtensionProjectItems IsNot Nothing Then
                For i As Integer = 0 To extensionProjectItemGroup.ExtensionProjectItems.Count - 1
                    Try
                        extensionProjectItemGroup.ExtensionProjectItems(i).Delete()
                    Catch ex As Exception When Not Utils.IsUnrecoverable(ex)
                    End Try
                Next
            End If

            _extProjItemGroups.RemoveItem(extensionProjectItemGroup)

            Me.RemoveMyExtensionsFolderIfEmpty()

            ' Reset monitor state
            _monitorState = MonitorState.Normal
        End Sub

#End Region

#Region " Manage MyExtensions folder "

        ''' ;FindMyExtensionsFolderProjectItem
        ''' <summary>
        ''' Find "MyExtensions" folder and set m_ExtensionFolderProjectItem with the value.
        ''' This will reset m_ExtensionFolderProjectItem to Nothing first.
        ''' </summary>
        Private Sub FindMyExtensionsFolderProjectItem()
            _extensionFolderProjectItem = Nothing

            Dim parentProjectItems As ProjectItems = Me.GetParentProjectItems()
            Debug.Assert(parentProjectItems IsNot Nothing, "Could not find parent ProjectItems!")

            Dim result As ProjectItem = Nothing
            For Each projectItem As ProjectItem In parentProjectItems
                If StringEquals(projectItem.Name, s_EXTENSION_FOLDER_NAME) Then
                    _extensionFolderProjectItem = projectItem
                    Exit For
                End If
            Next
        End Sub

        ''' ;GetMyExtensionsFolderProjectItem
        ''' <summary>
        ''' Find or create "MyExtensions" folder. Assumption: Always succeed.
        ''' Result will be in m_ExtensionFolderProjectItem.
        ''' </summary>
        Private Sub GetMyExtensionsFolderProjectItem()
            If _extensionFolderProjectItem Is Nothing Then
                Me.FindMyExtensionsFolderProjectItem()

                If _extensionFolderProjectItem Is Nothing Then
                    Dim parentProjectItems As ProjectItems = Me.GetParentProjectItems()
                    Debug.Assert(parentProjectItems IsNot Nothing, "Could not find parent ProjectItems!")

                    _extensionFolderProjectItem = parentProjectItems.AddFolder(s_EXTENSION_FOLDER_NAME)
                End If
            End If

            Debug.Assert(_extensionFolderProjectItem IsNot Nothing, "Fail to create MyExtensions folder!")
        End Sub

        ''' ;GetParentProjectItems
        ''' <summary>
        ''' Return the ProjectItems that will be used to contain the MyExtensions folder.
        ''' Either under My Project folder or the current project. Assumption: Not NULL.
        ''' Query this every time since this folder may be excluded / removed without notice.
        ''' </summary>
        Private Function GetParentProjectItems() As ProjectItems
            ' Try to find "My Project" special folder first.
            Dim myProjectFolderProjectItem As ProjectItem = _
                MyApplicationProperties.GetProjectItemForProjectDesigner(_projectHierarchy)

            If myProjectFolderProjectItem Is Nothing Then
                Debug.Assert(_project.ProjectItems IsNot Nothing, "Current project ProjectItems is NULL!")
                Return _project.ProjectItems
            Else
                Debug.Assert(myProjectFolderProjectItem.ProjectItems IsNot Nothing, _
                    "My Project folder ProjectItems is NULL!")
                Return myProjectFolderProjectItem.ProjectItems
            End If
        End Function

        ''' ;RemoveMyExtensionsFolderIfEmpty
        ''' <summary>
        ''' Find MyExtensions folder, if it exists and is empty, remove it from the project.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RemoveMyExtensionsFolderIfEmpty()
            If _extensionFolderProjectItem IsNot Nothing AndAlso _extensionFolderProjectItem.ProjectItems.Count <= 0 Then
                _extensionFolderProjectItem.Delete()
                _extensionFolderProjectItem = Nothing
            End If
        End Sub

#End Region

#Region " Manage My Namespace extension project items "

        ''' ;AddExtensionProjectFile
        ''' <summary>
        ''' Add the given extension project file to the list of extension project item groups.
        ''' </summary>
        Private Sub AddExtensionProjectFile(ByVal assemblyFullName As String, ByVal extensionProjectItem As ProjectItem, _
                ByVal extensionID As String, ByVal extensionVersion As Version, _
                ByVal extensionName As String, ByVal extensionDescription As String)
            Debug.Assert(extensionProjectItem IsNot Nothing, "NULL extensionProjectItem!")
            Debug.Assert(Not String.IsNullOrEmpty(extensionID), "extensionID is NULL or empty!")

            If assemblyFullName Is Nothing Then
                assemblyFullName = String.Empty
            End If

            If _extProjItemGroups Is Nothing Then
                _extProjItemGroups = New AssemblyDictionary(Of MyExtensionProjectItemGroup)()
            End If

            Dim currentExtensionProjectItemGroup As MyExtensionProjectItemGroup = Nothing
            Dim extProjItemGroups As List(Of MyExtensionProjectItemGroup) = _extProjItemGroups.GetItems(assemblyFullName)
            If extProjItemGroups IsNot Nothing AndAlso extProjItemGroups.Count > 0 Then
                For Each extProjItemGroup As MyExtensionProjectItemGroup In extProjItemGroups
                    If extProjItemGroup.IDEquals(extensionID) Then
                        currentExtensionProjectItemGroup = extProjItemGroup
                        Exit For
                    End If
                Next
            End If
            If currentExtensionProjectItemGroup Is Nothing Then
                currentExtensionProjectItemGroup = New MyExtensionProjectItemGroup( _
                    extensionID, extensionVersion, extensionName, extensionDescription)
                _extProjItemGroups.AddItem(assemblyFullName, currentExtensionProjectItemGroup)
            End If
            currentExtensionProjectItemGroup.AddProjectItem(extensionProjectItem)
        End Sub

        ''' ;GetBuildAttribute
        ''' <summary>
        ''' Get the MSBuild attribute for the given project item ID.
        ''' </summary>
        Private Function GetBuildAttribute(ByVal projectItemID As UInteger, ByVal attributeName As String) As String
            Debug.Assert(Not String.IsNullOrEmpty(attributeName), "NULL attributeName")
            Debug.Assert(_vsBuildPropertyStorage IsNot Nothing, "NULL IVsBuildPropertyStrorage!")

            Dim result As String = Nothing
            _vsBuildPropertyStorage.GetItemAttribute(projectItemID, attributeName, result)
            If result IsNot Nothing Then
                result = result.Trim()
            End If
            Return result
        End Function

        ''' ;LoadExtensionProjectFiles
        ''' <summary>
        ''' Search for MyExtensions folder, if it exists, attempt to get extension attributes
        ''' from its ProjectItem.
        ''' </summary>
        Private Sub LoadExtensionProjectFiles()
            If _extProjItemGroups IsNot Nothing Then
                _extProjItemGroups.Clear()
            End If

            If _vsBuildPropertyStorage Is Nothing Then
                Exit Sub
            End If

            Me.FindMyExtensionsFolderProjectItem()
            If _extensionFolderProjectItem Is Nothing Then
                Exit Sub
            End If

            Debug.Assert(_projectService IsNot Nothing)
            For Each extensionProjectItem As ProjectItem In _extensionFolderProjectItem.ProjectItems
                Dim extensionProjectItemID As UInteger = DTEUtils.ItemIdOfProjectItem(_projectHierarchy, extensionProjectItem)

                Dim assemblyName As String = GetBuildAttribute(extensionProjectItemID, s_MSBUILD_ATTR_ASSEMBLY)
                If assemblyName Is Nothing Then
                    assemblyName = String.Empty
                End If
                Dim templateID As String = GetBuildAttribute(extensionProjectItemID, s_MSBUILD_ATTR_ID)
                Dim templateVersion As Version = GetVersion(GetBuildAttribute(extensionProjectItemID, s_MSBUILD_ATTR_VERSION))

                If String.IsNullOrEmpty(templateID) Then
                    Continue For
                End If

                Dim templateName As String = Nothing
                Dim templateDescription As String = Nothing
                _projectService.GetExtensionTemplateNameAndDescription(templateID, templateVersion, assemblyName, _
                    templateName, templateDescription)

                Me.AddExtensionProjectFile(assemblyName, extensionProjectItem, _
                    templateID, templateVersion, templateName, templateDescription)
            Next
        End Sub

#End Region

#Region " Handle project documents events "

        ''' ;AdviseTrackProjectDocumentsEvents
        ''' <summary>
        ''' Start listening to IVsTrackProjectDocumentsEvents2
        ''' </summary>
        Private Sub AdviseTrackProjectDocumentsEvents()
            Dim trackProjectDocumentsEvents As TrackProjectDocumentsEventsHelper = _
                MyExtensibilitySolutionService.Instance.TrackProjectDocumentsEvents
            If trackProjectDocumentsEvents IsNot Nothing Then
                AddHandler trackProjectDocumentsEvents.AfterAddFilesEx, AddressOf Me.OnAfterAddFilesEx
                AddHandler trackProjectDocumentsEvents.AfterRemoveDirectories, AddressOf Me.OnAfterRemoveDirectories
                AddHandler trackProjectDocumentsEvents.AfterRenameFiles, AddressOf Me.OnAfterRenameFiles
                AddHandler trackProjectDocumentsEvents.AfterRemoveFiles, AddressOf Me.OnAfterRemoveFiles
                AddHandler trackProjectDocumentsEvents.AfterRenameDirectories, AddressOf Me.OnAfterRenameDirectories
            End If
        End Sub

        ''' ;UnadviseTrackProjectDocumentsEvents
        ''' <summary>
        ''' Stop listening to IVsTrackProjectDocumentsEvents2
        ''' </summary>
        Friend Sub UnadviseTrackProjectDocumentsEvents()
            Dim trackProjectDocumentsEvents As TrackProjectDocumentsEventsHelper = _
                MyExtensibilitySolutionService.Instance.TrackProjectDocumentsEvents
            If trackProjectDocumentsEvents IsNot Nothing Then
                RemoveHandler trackProjectDocumentsEvents.AfterAddFilesEx, AddressOf Me.OnAfterAddFilesEx
                RemoveHandler trackProjectDocumentsEvents.AfterRemoveDirectories, AddressOf Me.OnAfterRemoveDirectories
                RemoveHandler trackProjectDocumentsEvents.AfterRenameFiles, AddressOf Me.OnAfterRenameFiles
                RemoveHandler trackProjectDocumentsEvents.AfterRemoveFiles, AddressOf Me.OnAfterRemoveFiles
                RemoveHandler trackProjectDocumentsEvents.AfterRenameDirectories, AddressOf Me.OnAfterRenameDirectories
            End If
        End Sub

        ''' ;OnAfterAddFilesEx
        ''' <summary>
        ''' Files added to the solution. If this file is under MyExtensions folder 
        ''' and in AddTemplate state, collect them into m_ProjectItemsAddedFromTemplate.
        ''' </summary>
        Private Sub OnAfterAddFilesEx(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSADDFILEFLAGS)
            Debug.Assert(rgpszMkDocuments IsNot Nothing, "NULL rgpszMkDocuments!")

            If _monitorState <> MonitorState.AddTemplate Then ' Return if not monitoring add templates.
                Exit Sub
            End If

            If _extensionFolderProjectItem IsNot Nothing Then
                Dim myExtensionsFolderPath As String = GetProjectItemPath(_extensionFolderProjectItem)
                ' MyExtensions maybe removed and becomes invalid and the path will be Nothing.
                If myExtensionsFolderPath IsNot Nothing Then
                    For Each filePath As String In rgpszMkDocuments
                        If IsUnderFolder(filePath, myExtensionsFolderPath) Then
                            Dim fileName As String = Path.GetFileName(filePath)
                            Dim subProjectItem As ProjectItem = _
                                DTEUtils.FindProjectItem(_extensionFolderProjectItem.ProjectItems, fileName)
                            Debug.Assert(subProjectItem IsNot Nothing, "Could not find subProjectItem!")
                            If _projectItemsAddedFromTemplate Is Nothing Then
                                _projectItemsAddedFromTemplate = New List(Of ProjectItem)()
                            End If
                            _projectItemsAddedFromTemplate.Add(subProjectItem)
                        End If
                    Next
                End If
            End If
        End Sub

        ''' ;OnAfterRemoveFiles
        ''' <summary>
        ''' Files removed from the solution. If the file is under MyExtensions folder and not in RemoveProjectItem state,
        ''' raise change event.
        ''' </summary>
        Private Sub OnAfterRemoveFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEFILEFLAGS)
            Debug.Assert(rgpszMkDocuments IsNot Nothing, "NULL rgpszMkDocuments!")

            If _monitorState = MonitorState.RemoveProjectItem Then
                ' If in RemoveProjectItem state, do not raise event since project service will raise event itself.
                Exit Sub
            End If

            Dim subItemChanged As Boolean = False
            If _extensionFolderProjectItem IsNot Nothing Then
                Dim myExtensionsFolderPath As String = GetProjectItemPath(_extensionFolderProjectItem)
                ' MyExtensions maybe removed and becomes invalid and the path will be Nothing.
                If myExtensionsFolderPath IsNot Nothing Then
                    For Each filePath As String In rgpszMkDocuments
                        If IsUnderFolder(filePath, myExtensionsFolderPath) Then
                            subItemChanged = True
                            Exit For
                        End If
                    Next
                End If
            End If

            If subItemChanged Then
                Me.RaiseChangeEvent()
            End If
        End Sub

        ''' ;OnAfterRenameFiles
        ''' <summary>
        ''' File (or directory??) renamed. Check and reload extension file list if neccessary.
        ''' </summary>
        Private Sub OnAfterRenameFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEFILEFLAGS)
            Debug.Assert(rgszMkNewNames IsNot Nothing, "NULL rgszMkNewNames!")
            Debug.Assert(rgszMkOldNames IsNot Nothing, "NULL rgszMkOldNames!")

            If _monitorState <> MonitorState.Normal Then
                Exit Sub
            End If

            ' Check if the extension folder was added back / removed by this rename.
            ' If it was, raise event and exit sub.
            Dim extensionFolderExistsBefore As Boolean = _extensionFolderProjectItem IsNot Nothing
            Me.FindMyExtensionsFolderProjectItem()
            Dim extensionFolderExistsAfter As Boolean = _extensionFolderProjectItem IsNot Nothing
            If extensionFolderExistsBefore <> extensionFolderExistsAfter Then
                Me.RaiseChangeEvent()
                Exit Sub
            End If

            ' If it was not, check if the change affect files underneath it and raise event if neccessary.
            ' Only care about when file is moved in / out of this folder.
            If _extensionFolderProjectItem IsNot Nothing Then
                Dim myExtensionsFolderPath As String = GetProjectItemPath(_extensionFolderProjectItem)
                If Not StringIsNullEmptyOrBlank(myExtensionsFolderPath) Then
                    Dim minLength As Integer = Math.Min(rgszMkOldNames.Length, rgszMkNewNames.Length)
                    For i As Integer = 0 To minLength - 1
                        Dim oldFileUnderMyExtension As Boolean = IsUnderFolder(rgszMkOldNames(i), myExtensionsFolderPath)
                        Dim newFileUnderMyExtension As Boolean = IsUnderFolder(rgszMkNewNames(i), myExtensionsFolderPath)
                        If oldFileUnderMyExtension <> newFileUnderMyExtension Then
                            Me.RaiseChangeEvent()
                            Exit For
                        End If
                    Next
                End If
            End If

        End Sub

        ''' ;OnAfterRemoveDirectories
        ''' <summary>
        ''' Directories removed from the solution. See if MyExtensions folder is removed.
        ''' </summary>
        Private Sub OnAfterRemoveDirectories(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEDIRECTORYFLAGS)
            Debug.Assert(rgpszMkDocuments IsNot Nothing, "NULL rgpszMkDocuments!")

            If _monitorState <> MonitorState.Normal Then
                Exit Sub
            End If

            If _extensionFolderProjectItem IsNot Nothing Then
                For Each dirPath As String In rgpszMkDocuments
                    Dim dirName As String = GetDirectoryName(dirPath)
                    If StringEquals(dirName, s_EXTENSION_FOLDER_NAME) Then
                        Me.FindMyExtensionsFolderProjectItem()
                        If _extensionFolderProjectItem Is Nothing Then
                            Me.RaiseChangeEvent()
                            Exit For
                        End If
                    End If
                Next
            End If
        End Sub

        ''' ;OnAfterRenameDirectories
        ''' <summary>
        ''' Directories renamed in the solution. See if MyExtensions folder is removed / readded.
        ''' </summary>
        Private Sub OnAfterRenameDirectories(ByVal cProjects As Integer, ByVal cDirs As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEDIRECTORYFLAGS)

            If _monitorState <> MonitorState.Normal Then
                Exit Sub
            End If

            Dim myExtensionsFolderExistsBefore As Boolean = _extensionFolderProjectItem IsNot Nothing
            Dim namesToCheck As String() = IIf(Of String())(myExtensionsFolderExistsBefore, rgszMkOldNames, rgszMkNewNames)
            For Each dirPath As String In namesToCheck
                Dim dirName As String = GetDirectoryName(dirPath)
                If StringEquals(dirName, s_EXTENSION_FOLDER_NAME) Then
                    Me.FindMyExtensionsFolderProjectItem()
                    Dim myExtensionsFolderExistsAfter As Boolean = _extensionFolderProjectItem IsNot Nothing
                    If myExtensionsFolderExistsAfter <> myExtensionsFolderExistsBefore Then
                        Me.RaiseChangeEvent()
                        Exit For
                    End If
                End If
            Next
        End Sub

        ''' ;GetProjectItemPath
        ''' <summary>
        ''' Return the full path to a project item. If the project item becomes unavailable, return Nothing.
        ''' </summary>
        Private Shared Function GetProjectItemPath(ByVal projectItem As ProjectItem) As String
            Debug.Assert(projectItem IsNot Nothing, "Null projectItem!")

            Dim itemPath As String = Nothing
            Try
                itemPath = projectItem.FileNames(1)
            Catch ex As Exception When Not IsUnrecoverable(ex)
            End Try
            If itemPath IsNot Nothing Then
                itemPath = RemoveEndingSeparator(itemPath)
            End If

            Return itemPath
        End Function

        Private Shared Function IsUnderFolder(ByVal filePath As String, ByVal folderPath As String) As Boolean
            If Not StringIsNullEmptyOrBlank(filePath) Then
                filePath = Path.GetFullPath(filePath)
                Dim directoryPath As String = RemoveEndingSeparator(Path.GetDirectoryName(filePath))
                Return StringEquals(directoryPath, folderPath)
            End If
            Return False
        End Function

        Private Sub RaiseChangeEvent()
            Me.LoadExtensionProjectFiles()
            RaiseEvent ExtensionChanged()
        End Sub

        Private Shared Function GetDirectoryName(ByVal pathStr As String) As String
            If StringIsNullEmptyOrBlank(pathStr) Then
                Return Nothing
            End If
            Return Path.GetFileName(RemoveEndingSeparator(pathStr))
        End Function

        Private Shared Function RemoveEndingSeparator(ByVal pathStr As String) As String
            Debug.Assert(pathStr IsNot Nothing, "NULL path!")
            Return pathStr.TrimEnd(New Char() {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar})
        End Function

        Private _monitorState As MonitorState = MonitorState.Normal
        Private _projectItemsAddedFromTemplate As List(Of ProjectItem)

        Private Enum MonitorState As Byte
            Normal
            AddTemplate
            RemoveProjectItem
        End Enum
#End Region

#Region " Private support methods "
        ''' ;GetProjectItemName
        ''' <summary>
        ''' Given a template base name, return a suitable project item name
        ''' by appending increasing postfix into the given name until no ProjectItem 
        ''' with the same name is found under the MyExtensions folder.
        ''' </summary>
        Private Function GetProjectItemName(ByVal templateBaseName As String) As String
            If StringIsNullEmptyOrBlank(templateBaseName) Then ' Verify and fix up input.
                templateBaseName = s_DEFAULT_CODE_FILE_NAME
            End If

            If _extensionFolderProjectItem Is Nothing Then ' If MyExtensions does not exist, no point to search for it.
                Return templateBaseName
            End If

            Dim baseName As String = Path.GetFileNameWithoutExtension(templateBaseName)
            Dim baseExtension As String = Path.GetExtension(templateBaseName)

            Dim candidateProjectItemName As String = templateBaseName
            Dim postfix As Integer = 1
            While (DTEUtils.FindProjectItem(_extensionFolderProjectItem.ProjectItems, candidateProjectItemName) IsNot Nothing)
                candidateProjectItemName = baseName & postfix.ToString() & baseExtension
                postfix += 1
            End While
            Return candidateProjectItemName
        End Function

        Private Sub New(ByVal projectService As MyExtensibilityProjectService, _
                ByVal serviceProvider As IServiceProvider, ByVal project As Project, _
                ByVal projectHierarchy As IVsHierarchy, ByVal vsBuildPropertyStorage As IVsBuildPropertyStorage)

            Debug.Assert(projectService IsNot Nothing, "projectService")
            Debug.Assert(serviceProvider IsNot Nothing, "serviceProvider")
            Debug.Assert(project IsNot Nothing, "project")
            Debug.Assert(projectHierarchy IsNot Nothing, "projectHierarchy")
            Debug.Assert(vsBuildPropertyStorage IsNot Nothing, "vsBuildPropertyStorage")

            _projectService = projectService
            _serviceProvider = serviceProvider
            _project = project
            _projectHierarchy = projectHierarchy
            _vsBuildPropertyStorage = vsBuildPropertyStorage

            Me.LoadExtensionProjectFiles()
            Me.AdviseTrackProjectDocumentsEvents()
        End Sub
#End Region

        Private _projectService As MyExtensibilityProjectService
        Private _serviceProvider As IServiceProvider ' Usually VBPackage.
        Private _project As EnvDTE.Project ' The associated project.
        Private _projectHierarchy As IVsHierarchy ' The associated project hierarchy.

        Private _vsBuildPropertyStorage As IVsBuildPropertyStorage ' Used to set the item extension attributes.

        Private _extensionFolderProjectItem As EnvDTE.ProjectItem ' "My Extensions" folder.

        ' The dictionary of MyExtensionProjectItemGroup, indexed by the triggering assemblies.
        Private _extProjItemGroups As AssemblyDictionary(Of MyExtensionProjectItemGroup)

        ' MyExtensions folder and Extensions.xml file name.
        Private Const s_EXTENSION_FOLDER_NAME As String = "MyExtensions"
        ' Extension code file's attributes in project file.
        Private Const s_MSBUILD_ATTR_ASSEMBLY As String = "VBMyExtensionAssembly"
        Private Const s_MSBUILD_ATTR_ID As String = "VBMyExtensionTemplateID"
        Private Const s_MSBUILD_ATTR_VERSION As String = "VBMyExtensionTemplateVersion"
        ' Default code file name.
        Private Const s_DEFAULT_CODE_FILE_NAME As String = "Code.vb"

    End Class
End Namespace
