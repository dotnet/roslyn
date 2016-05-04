' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports EnvDTE
Imports System.IO
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    'This file expands ResourcesFolderService with a few methods which are specific to the Resource Editor and
    '  do not need to be public to or ported for the Resource Picker.

    Friend Partial Class ResourcesFolderService

        ''' <summary>
        ''' Attempts to add a file to the project according to the registered ResourcesFolderBehavior for this project.
        ''' </summary>
        ''' <param name="EditorName">The name of the editor making this call.  Used in messagebox captions.</param>
        ''' <param name="Project">The project that the file should be added to.</param>
        ''' <param name="ResXProjectItem">The ProjectItem of the ResX file.</param>
        ''' <param name="MessageBoxOwner">The window to parent messageboxes to.</param>
        ''' <param name="SourceFilePath">The file and path of the file which should be added to the project.</param>
        ''' <returns>The final file and path of the file after it was added to the project, or else its original location if
        '''   it was not added to the project or was not copied while being added to the project.  Returns Nothing if the
        '''  user canceled the operation.</returns>
        ''' <remarks>The user is given the choic to cancel the operation when he is asked to overwrite an existing file or link.</remarks>
        Friend Shared Function AddFileToProject(ByVal EditorName As String, ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal MessageBoxOwner As IWin32Window, ByVal SourceFilePath As String, ByVal CopyFileIfExists As Boolean) As String
            Return AddFileToProjectHelper(EditorName, Project, ResXProjectItem, MessageBoxOwner, SourceFilePath, CopyFileIfExists)
        End Function


        ''' <summary>
        ''' Retrieves the destination path where imported files (e.g. .bmp files) will normally be placed 
        '''   when calling AddFileToProject, for the specified project
        ''' </summary>
        ''' <param name="Project">The Project to get the destination path for.  If this is Nothing or the Miscellaneous Files project, returns Nothing.</param>
        ''' <returns>The default destination path for this project, or Nothing if the Project is Nothing or the Miscellaneous Files project.</returns>
        ''' <remarks></remarks>
        Friend Shared Function GetAddFileDestinationPath(ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal CreateDirectoryIfDoesntExist As Boolean) As String
            If Project Is Nothing OrElse IsMiscellaneousProject(Project) OrElse Project.FullName = "" Then
                Return Nothing
            End If

            'Determine the behavior for this project.
            Dim Behavior As ResourcesFolderBehavior
            Dim ResourcesFolderName As String = Nothing
            GetProjectBehavior(Project, Behavior, ResourcesFolderName)

            Dim DestinationProjectItems As ProjectItems = Nothing
            Dim DestinationPath As String = Nothing
            GetDestinationFolder(Project, ResXProjectItem, Behavior, ResourcesFolderName, DestinationProjectItems, DestinationPath)

            If DestinationPath <> "" AndAlso CreateDirectoryIfDoesntExist AndAlso Not Directory.Exists(DestinationPath) Then
                Directory.CreateDirectory(DestinationPath)
            End If

            Return DestinationPath
        End Function


        ''' <summary>
        ''' Returns True iff the given file is within the directories of the given project on disk.
        ''' </summary>
        ''' <param name="Project">The Project to query.  Returns False if Project = Nothing or the project is the Miscellaneous Files project.</param>
        ''' <param name="FilePath">The file path to check to see if it's in the project's subdirectories.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsFileInProjectSubdirectories(ByVal Project As Project, ByVal FilePath As String) As Boolean
            If Project Is Nothing OrElse IsMiscellaneousProject(Project) OrElse Project.FullName = "" Then
                Return False
            End If

            Return IsSubdirectoryOf(Path.GetDirectoryName(FilePath), GetProjectDirectory(Project))
        End Function


    End Class

End Namespace
