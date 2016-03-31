Option Explicit On
Option Strict On
Option Compare Binary

Imports EnvDTE
Imports EnvDTE.Constants
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' An interface for adding files to a project.  Used by both the Resource Editor and Resource Picker.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourcesFolderService

#Region "Fields and Enums"

        Private Enum ResourcesFolderBehavior
            ''' <summary>
            ''' The Resource editor/picker do not try to add resource files into the project.  A relative 
            '''   link is added into the resx file to the imported file�s original location on disk.  Same 
            '''   behavior as when the resource editor is opened on a resx that�s not in a project in 
            '''   the current solution.
            ''' </summary>
            ''' <remarks></remarks>
            AddNone = 0

            ''' <summary>
            ''' Imported files are added into a �Resources� folder at the top level of the project.
            ''' </summary>
            ''' <remarks>
            ''' If the file is in the project�s subdirectories, but not included 
            '''   in the project, it is added to the project in its current location.  
            ''' If the file is already in the project (in any subfolder), it is left where it is.
            ''' Otherwise, the file is added to the Resources folder (using ProjectItems.AddFromFileCopy)
            '''   in the top level of the Project.
            '''   If a Resources folder doesn 't exist, it is created.   
            ''' A link is made to the copied file, if it was copied, or else to the original location of the file.
            ''' </remarks>
            AddToResourcesFolder = 1

            ''' <summary>
            ''' Imported files are added to the root folder of the project
            ''' </summary>
            ''' <remarks>
            ''' If the file is in the project�s subdirectories, but not included 
            '''   in the project, it is added to the project in its current location.  
            ''' If the file is already in the project (in any subfolder), it is left where it is.
            ''' Otherwise, the file is added to the top-level folder of the project (using ProjectItems.AddFromFileCopy).
            ''' A link is made to the copied file, if it was copied, or else to the original location of the file.
            ''' </remarks>
            AddToProjectRoot = 2

            ''' <summary>
            ''' Imported files are added to the same folder in the project where the ResX file is located
            ''' </summary>
            ''' <remarks>
            ''' If the file is in the project�s subdirectories, but not included 
            '''   in the project, it is added to the project in its current location.  
            ''' If the file is already in the project (in any subfolder), it is left where it is.
            ''' Otherwise, the file is added to the same folder of the project where the ResX is located
            '''   (using ProjectItems.AddFromFileCopy).
            ''' A link is made to the copied file, if it was copied, or else to the original location of the file.
            ''' </remarks>
            AddToResXFolder = 3

        End Enum


        'The default name to use for the Resources file unless the registry says otherwise.
        Private Const DEFAULT_RESOURCE_FOLDER_NAME As String = "Resources"

        'Name of the "Projects" key in the Visual Studio registry subtree
        Private Const KEYPATH_PROJECTS As String = "Projects"

        'Name of the key in the registry that indicates what add-to-project behavior to use for a particular project
        Private Const KEYNAME_RESOURCESFOLDERBEHAVIOR As String = "ResourcesFolderBehavior"

        'Name of the key in the registry that indicates the name of the Resources folder for a particular project
        Private Const KEYNAME_RESOURCESFOLDERNAME As String = "ResourcesFolderName"

#End Region


#Region "Public API"


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
        Public Shared Function AddFileToProject(ByVal EditorName As String, ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal MessageBoxOwner As IWin32Window, ByVal SourceFilePath As String) As String
            Return AddFileToProjectHelper(EditorName, Project, ResXProjectItem, MessageBoxOwner, SourceFilePath, CopyFileIfExists:=False)
        End Function

#End Region


#Region "Implementation"


#Region "Constants from DTE.idl"

        'A Guid version of vsProjectItemKindPhysicalFolder, which as a projectitem kind indicates that the
        '  projectitem is a physical folder on disk (as opposed to a virtual folder, etc.)
        Private Shared ReadOnly Guid_vsProjectItemKindPhysicalFolder As New Guid(vsProjectItemKindPhysicalFolder)

#End Region

        'Trace switch for this class.
        Private Shared RFSSwitch As New TraceSwitch("ResourcesFolderService", "Traces the behavior of the Resources Folder Service")


#Region "Tracing"

        ''' <summary>
        ''' Does debug-only tracing for this class (verbose level)
        ''' </summary>
        ''' <param name="Message">The message to displaying, including optional formatting parameters "{0}" etc.</param>
        ''' <param name="FormatArguments">Arguments for "{0}", "{1}", etc.</param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Friend Shared Sub Trace(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
            If FormatArguments.Length > 0 Then
                'Only use String.Format when we have specific format arguments, although we might accidently break on something like a stray "{" in a filename
                Message = String.Format(Message, FormatArguments)
            End If
            Debug.WriteLineIf(RFSSwitch.TraceVerbose, "Resources Folder Service: " & Message)
        End Sub

#End Region


#Region "Adding files to the project"

        ''' <summary>
        ''' Attempts to add a file to the project according to the registered ResourcesFolderBehavior for this project.
        ''' </summary>
        ''' <param name="EditorName">The name of the editor making this call.  Used in messagebox captions.</param>
        ''' <param name="Project">The project that the file should be added to.</param>
        ''' <param name="ResXProjectItem">The ProjectItem of the ResX file.</param>
        ''' <param name="MessageBoxOwner">The window to parent messageboxes to.</param>
        ''' <param name="SourceFilePath">The file and path of the file which should be added to the project.</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <returns>The final file and path of the file after it was added to the project, or else its original location if
        '''   it was not added to the project or was not copied while being added to the project.  Returns Nothing if the
        '''  user canceled the operation.</returns>
        ''' <remarks>The user is given the choic to cancel the operation when he is asked to overwrite an existing file or link.</remarks>
        Private Shared Function AddFileToProjectHelper(ByVal EditorName As String, ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal MessageBoxOwner As IWin32Window, ByVal SourceFilePath As String, ByVal CopyFileIfExists As Boolean) As String
            If EditorName = "" Then
                Throw New ArgumentNullException("EditorName")
            End If
            If SourceFilePath = "" Then
                Throw New ArgumentNullException("SourceFilePath")
            End If
            Debug.Assert(ResXProjectItem IsNot Nothing, "ResXProjectItem is Nothing!")
            Debug.Assert(Project IsNot Nothing, "Project is Nothing!")

            'Resolve the path if it's relative
            Dim FullSourceFilePath As String = Path.GetFullPath(SourceFilePath)

            'Verify the file exists
            If Not File.Exists(FullSourceFilePath) Then
                Throw New IO.FileNotFoundException(SR.GetString(SR.RFS_FindNotFound_File, FullSourceFilePath))
            End If

            'Determine the behavior for this project.  This handles the case of Project = Nothing and the Miscellaneous Files project.
            Dim Behavior As ResourcesFolderBehavior
            Dim ResourcesFolderName As String = Nothing
            GetProjectBehavior(Project, Behavior, ResourcesFolderName)

            If Behavior = ResourcesFolderBehavior.AddNone Then
                'The behavior is that we don't add files to the project.  Use the original file location for linking.
                Return FullSourceFilePath
            End If

            'First we check if the source file is already in the project or its location is within the
            '  project's subdirectories.  In these cases, we add the file to (or leave it in) the project 
            '  where it is, so there is no need to search for or create a Resources folder in these cases.

            Dim FinalFilePath As String

            Dim ExistingItem As ProjectItem = Nothing
            Try
                ExistingItem = Project.DTE.Solution.FindProjectItem(FullSourceFilePath)
            Catch ex As ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As Exception
                'Some project systems (C++ in show all files mode) throw here, just ignore it.
            End Try
            If ExistingItem IsNot Nothing AndAlso ExistingItem.ContainingProject Is Project Then
                'The file is already in the project.
                If CopyFileIfExists Then
                    'We need to make a copy of the file.
                    Return CopyFileWithinProject(Project, FullSourceFilePath, ExistingItem.Collection)
                Else
                    'We use the existing location to link to, and do nothing else.
                    Trace("File is already in the project, linking to original location: " & FullSourceFilePath)

                    'Sometimes the project system seems to give a false positive - i.e., the file may be
                    '  within the project's directory structure, but it might not actually be included in the
                    '  project.  Try to include it just in case, and ignore any errors if this fails.
                    Try
                        Project.ProjectItems.AddFromFile(FullSourceFilePath)
                    Catch ex As ThreadAbortException
                        Throw
                    Catch ex As StackOverflowException
                        Throw
                    Catch ex As OutOfMemoryException
                        Throw
                    Catch ex As Exception
                    End Try

                    'Since the file already exists in the project, the user might have the Build Action property
                    '  set to something specific.  Therefore we do not call SetBuildAction() in this case.
                    Return FullSourceFilePath
                End If
            ElseIf IsSubdirectoryOf(Path.GetDirectoryName(FullSourceFilePath), GetProjectDirectory(Project)) Then
                'The file is within the project's subdirectories structure.  

                If CopyFileIfExists Then
                    'We need to make a copy of the file.  Since we know the file's location is inside
                    '  the project's folders, we can make the copy in the same directory as the original
                    '  file's location.
                    Return CopyFileWithinProject(Project, FullSourceFilePath, Path.GetDirectoryName(FullSourceFilePath))
                Else
                    Try
                        'We add the file to the project where it is, and link to that same location.
                        Trace("File is already within the project's subdirectories, but is not included in the project.  Adding to project.")
                        Dim NewProjectItem As ProjectItem = Project.ProjectItems.AddFromFile(FullSourceFilePath)
                        SetBuildAction(NewProjectItem)
                        FinalFilePath = GetFileNameFromProjectItem(NewProjectItem, Path.GetExtension(FullSourceFilePath))
                    Catch ex As Exception
                        Throw New Exception(SR.GetString(SR.RFS_CantAddFileToProject_File_ExMsg, FullSourceFilePath, ex.Message), ex)
                    End Try
                End If
            Else
                'The file is not in the project.  We need to add the file to the project according to ResourcesFolderBehavior.
                FinalFilePath = AddOutsideFileToProject(EditorName, Project, ResXProjectItem, MessageBoxOwner, FullSourceFilePath, Behavior, ResourcesFolderName, CopyFileIfExists)
                If FinalFilePath Is Nothing Then
                    'User canceled.
                    Return Nothing
                End If
            End If

            Debug.Assert(FinalFilePath IsNot Nothing AndAlso FinalFilePath <> "")

            'Get the full path of the new project item
            If Not File.Exists(FinalFilePath) Then
                Debug.Fail("We added the file to the project, but the file can't be found at the location it's supposed to be at: " & FinalFilePath)
                Throw New Exception(SR.GetString(SR.RFS_CantAddFileToProject_File, FinalFilePath))
            End If

            'We're done.
            Debug.Assert(Path.GetExtension(FinalFilePath).Equals(Path.GetExtension(FullSourceFilePath), StringComparison.OrdinalIgnoreCase), _
                "The file's extension changed when it was added to the project.  Do we have the right file?")
            Trace("Final file path: " & FinalFilePath)
            Return FinalFilePath
        End Function


        ''' <summary>
        ''' Retrieves the given Project item's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetProjectItemProperty(ByVal Item As ProjectItem, ByVal PropertyName As String) As [Property]
            If Item.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As [Property] In Item.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to None.  If this project system doesn't
        '''   have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Private Shared Sub SetBuildAction(ByVal Item As ProjectItem)
            Const BuildActionPropertyName As String = "BuildAction"
            Const BuildActionNone As Integer = 0

            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, BuildActionPropertyName)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = BuildActionNone
            End If
        End Sub


        ''' <summary>
        ''' Makes a copy of a given file that is within a project's folders and adds the copy
        '''   to the project.
        ''' </summary>
        ''' <param name="Project">The project that the file is in.</param>
        ''' <param name="SourceFilePath">The path of the source file.</param>
        ''' <param name="DestinationFolder">The destination folder (ProjectItems) where the file should be copied.</param>
        ''' <returns></returns>
        ''' <remarks>Either DestinationFolder or DestinationFolderPath must be passed in.</remarks>
        Private Shared Function CopyFileWithinProject(ByVal Project As Project, ByVal SourceFilePath As String, ByVal DestinationFolder As ProjectItems) As String
            'Need to determine the destination disk path
            Dim DestinationFolderPath As String
            If Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(DestinationFolder.Kind)) Then
                DestinationFolderPath = GetFolderNameFromProjectItems(DestinationFolder)
            Else
                'DestinationFolderPath is not a physical folder on the disk.  It might be
                '  virtual.  Don't want to copy into that.  Copy into the root instead.
                DestinationFolderPath = GetProjectDirectory(Project)
                DestinationFolder = Project.ProjectItems
            End If

            Return CopyFileWithinProjectHelper(Project, SourceFilePath, DestinationFolder, DestinationFolderPath)
        End Function


        ''' <summary>
        ''' Makes a copy of a given file that is within a project's folders and adds the copy
        '''   to the project.
        ''' </summary>
        ''' <param name="Project">The project that the file is in.</param>
        ''' <param name="SourceFilePath">The path of the source file.</param>
        ''' <param name="DestinationFolderPath">The path of the destination folder where this item should be copied.</param>
        ''' <returns></returns>
        ''' <remarks>Either DestinationFolder or DestinationFolderPath must be passed in.</remarks>
        Private Shared Function CopyFileWithinProject(ByVal Project As Project, ByVal SourceFilePath As String, ByVal DestinationFolderPath As String) As String
            Debug.Assert(DestinationFolderPath IsNot Nothing AndAlso DestinationFolderPath <> "", "DestinationFolderPath was empty")

            'Try to locate the source file's directory in the project.
            Dim DestinationFolder As ProjectItems
            DestinationFolder = FindProjectItemsForFolderPath(Project, DestinationFolderPath)

            'If the folder isn't included in the project, DestinationFolder is still
            '  Nothing at this point.  That's fine, CopyFileWithinProjectHelper() can deal with it.

            Return CopyFileWithinProjectHelper(Project, SourceFilePath, DestinationFolder, DestinationFolderPath)
        End Function


        ''' <summary>
        ''' Makes a copy of a given file that is within a project's folders and adds the copy
        '''   to the project.
        ''' </summary>
        ''' <param name="Project">The project that the file is in.</param>
        ''' <param name="SourceFilePath">The path of the source file.</param>
        ''' <param name="DestinationFolder">The destination folder (ProjectItems) where the file should be copied.  Can be Nothing</param>
        ''' <param name="DestinationFolderPath">The path of the destination folder where this item should be copied.  Must not be empty.</param>
        ''' <returns></returns>
        ''' <remarks>Either DestinationFolder or DestinationFolderPath must be passed in.</remarks>
        Private Shared Function CopyFileWithinProjectHelper(ByVal Project As Project, ByVal SourceFilePath As String, ByVal DestinationFolder As ProjectItems, ByVal DestinationFolderPath As String) As String
            If DestinationFolderPath Is Nothing OrElse DestinationFolderPath = "" OrElse Project Is Nothing OrElse SourceFilePath Is Nothing OrElse SourceFilePath = "" Then
                Debug.Fail("Missing args")
                Return Nothing 'defensive
            End If

            Try
                'Get a unique name for the file
                Dim CopiedFilePath As String = MakeUniqueFileName(DestinationFolder, DestinationFolderPath, Path.GetFileName(SourceFilePath))

                'Copy
                Trace("Making copy of file ""{0}"" -> ""{1}""", SourceFilePath, CopiedFilePath)
                File.Copy(SourceFilePath, CopiedFilePath)

                'And add to the project
                Dim NewItem As ProjectItem = Project.ProjectItems.AddFromFile(CopiedFilePath)
                SetBuildAction(NewItem)
                Return GetFileNameFromProjectItem(NewItem, Path.GetExtension(CopiedFilePath))
            Catch ex As Exception
                Throw New Exception(SR.GetString(SR.RFS_CantAddFileToProject_File_ExMsg, SourceFilePath, ex.Message), ex)
            End Try
        End Function


        ''' <summary>
        ''' Adds a file from outside of the project subdirectories into the project, according to ResourcesFolderBehavior.
        ''' </summary>
        ''' <param name="EditorName">The name of the editor making this call.  Used in messagebox captions.</param>
        ''' <param name="Project">The project that the file should be added to.</param>
        ''' <param name="ResXProjectItem">The ProjectItem of the ResX file.</param>
        ''' <param name="MessageBoxOwner">The window to parent messageboxes to.</param>
        ''' <param name="FullSourceFilePath">The file and path of the file which should be added to the project.</param>
        ''' <param name="Behavior">The ResourcesFolderBehavior behavior for this project.</param>
        ''' <param name="ResourcesFolderName">The name of the Resources folder for this project.  Ignored if files are not to be added to the Resources folder.</param>
        ''' <param name="CopyFileIfExists">If True, then if a file already exists where the new file should be copied, 
        '''   the new file will copied with a unique name.  If False, the user will be asked whether the old file should be overwritten.</param>
        ''' <returns>The final file and path of the file after it was added to the project, or else its original location if
        '''   it was not added to the project or was not copied while being added to the project.  Returns Nothing if the
        '''  user canceled the operation.</returns>
        ''' <remarks>The user is given the choice to cancel the operation when he is asked to overwrite an existing file or link.</remarks>
        Private Shared Function AddOutsideFileToProject(ByVal EditorName As String, ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal MessageBoxOwner As IWin32Window, ByVal FullSourceFilePath As String, ByVal Behavior As ResourcesFolderBehavior, ByVal ResourcesFolderName As String, ByVal CopyFileIfExists As Boolean) As String
            Dim SourceFileNameOnly As String = Path.GetFileName(FullSourceFilePath)

            'Determine where to add files into the project
            Dim DestinationFolder As ProjectItems = Nothing
            Dim DestinationFolderPath As String = Nothing
            GetDestinationFolder(Project, ResXProjectItem, Behavior, ResourcesFolderName, DestinationFolder, DestinationFolderPath)
            If DestinationFolder Is Nothing Then
                'Just return the original file location
                Return FullSourceFilePath
            End If

            Try
                'Add the file to the project (for most projects, notable exception being C++, this copies the file
                '  into the project)
                Trace("Adding file to project via ProjectItems.AddFromFileCopy: " & FullSourceFilePath)
                Dim NewItem As ProjectItem = DestinationFolder.AddFromFileCopy(FullSourceFilePath)
                SetBuildAction(NewItem)
                Return GetFileNameFromProjectItem(NewItem, Path.GetExtension(FullSourceFilePath))
            Catch ex As ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As Exception
                'We had trouble adding the file.  Explore some corrective measures.
                Try
                    Dim FilePathToTryAgain As String = FullSourceFilePath

                    'Is there already a file at the disk location?
                    Dim DestinationFilePath As String = Path.Combine(DestinationFolderPath, SourceFileNameOnly)
                    Dim FileAlreadyExists As Boolean = File.Exists(DestinationFilePath)

                    'Is there already a projectitem with that name?  (Different from FileAlreadyExists if the projectitem is a link to a file.)
                    Dim ExistingLinkedItem As ProjectItem = QueryProjectItems(DestinationFolder, SourceFileNameOnly)
                    Dim ProjectItemAlreadyExists As Boolean = (ExistingLinkedItem IsNot Nothing)

                    If CopyFileIfExists AndAlso (FileAlreadyExists OrElse ProjectItemAlreadyExists) Then
                        'We need to copy the file

                        If ProjectItemAlreadyExists Then
                            Return CopyFileWithinProject(Project, FullSourceFilePath, ExistingLinkedItem.Collection)
                        Else
                            Return CopyFileWithinProject(Project, FullSourceFilePath, Path.GetDirectoryName(DestinationFilePath))
                        End If
                    ElseIf FileAlreadyExists Then
                        'Ask the user if it's okay to replace the file on disk.
                        If MessageBox.Show(MessageBoxOwner, SR.GetString(SR.RFS_QueryReplaceFile_File, DestinationFilePath), _
                                SR.GetString(SR.RFS_QueryReplaceFileTitle_Editor, EditorName), _
                                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) _
                                = DialogResult.Yes _
                        Then
                            'Delete the file at the destination location so we can try again.
                            'If the file at the destination location is actually included in the project, it's better
                            '  to go through the project system for the delete, so that source code control or anything
                            '  else knows exactly what we're doing.
                            Dim ExistingItemAtDestination As ProjectItem = Project.DTE.Solution.FindProjectItem(DestinationFilePath)
                            If ExistingItemAtDestination IsNot Nothing Then
                                ExistingItemAtDestination.Delete()
                            Else
                                'Not in the project - okay to just delete
                                File.Delete(DestinationFilePath)
                            End If

                            'Drop through to try again
                        Else
                            'User said no.  Cancel the add to project operation.
                            Return Nothing
                        End If
                    ElseIf ProjectItemAlreadyExists Then
                        'Ask user for permission to remove the current link.
                        If MessageBox.Show(MessageBoxOwner, SR.GetString(SR.RFS_QueryRemoveLink_Folder_Link, SourceFileNameOnly, DestinationFolderPath), _
                                    SR.GetString(SR.RFS_QueryRemoveLinkTitle_Editor, EditorName), _
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) _
                                    = DialogResult.Yes _
                        Then
                            '.Delete() is necessary to actually remove the link.  .Remove() won't do that.
                            ExistingLinkedItem.Delete()

                            'Drop through to try again
                        Else
                            'User said no.  Cancel.
                            Return Nothing
                        End If
                    Else
                        'There must have been some other reason for the failure.  Rethrow the original exception.
                        Throw ex
                    End If

                    'Try again to add the (possibly different) file to the project
                    Dim NewItem As ProjectItem = DestinationFolder.AddFromFileCopy(FilePathToTryAgain)
                    SetBuildAction(NewItem)
                    Return GetFileNameFromProjectItem(NewItem, Path.GetExtension(FilePathToTryAgain))
                Catch exFinalFailure As Exception
                    'Okay, we've hit an exception in our corrective measures.  Time to give up and report the exception.
                    Throw New Exception(SR.GetString(SR.RFS_CantAddFileToProject_File_ExMsg, FullSourceFilePath, exFinalFailure.Message), exFinalFailure)
                End Try
            End Try
        End Function


        ''' <summary>
        ''' Finds the Resources folder within a project.  If one does not already exist, it is created.
        ''' </summary>
        ''' <param name="Project">The project to look in.</param>
        ''' <param name="ResourcesFolderName">The name of the Resources folder (usually this is "Resources")</param>
        ''' <param name="ResourcesFolderProjectItems">[Out] Returns the ProjectItem collection that represents the Resources folder in the project</param>
        ''' <param name="ResourcesFolderPath">[Out] Returns the path on disk of the Resources folder.</param>
        ''' <remarks></remarks>
        Private Shared Sub GetOrCreateResourcesFolder(ByVal Project As Project, ByVal ResourcesFolderName As String, ByRef ResourcesFolderProjectItems As ProjectItems, ByRef ResourcesFolderPath As String)
            Dim AddToProjectRoot As Boolean = False

            Dim FoundProjectItem As ProjectItem
            'The Resources folder we're looking for is defined to always be at the top level
            '  of the project.  Search for it.
            FoundProjectItem = QueryProjectItems(Project.ProjectItems, ResourcesFolderName)
            If FoundProjectItem IsNot Nothing Then
                If Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(FoundProjectItem.Kind)) Then
                    'Okay, we found it.  Return its ProjectItems property.
                    Trace("Resources folder found: " & FoundProjectItem.Name)
                Else
                    'We found a Resources folder, but it's not a physical folder on disk.  We don't
                    '  deal well with that, so add to the project root instead.
                    Trace("Resources folder found, but it's not a physical disk folder.  Adding to project root instead.")
                    AddToProjectRoot = True
                End If
            End If

            If FoundProjectItem Is Nothing AndAlso Not AddToProjectRoot Then
                'We'll have to try creating it a Resources folder

                'Check to see if the Resources folder already exists on the disk ('cause that would cause
                '  our attempt to create one to fail).
                ResourcesFolderPath = Path.Combine(GetProjectDirectory(Project), ResourcesFolderName)
                If Directory.Exists(ResourcesFolderPath) Then
                    'Yep, the folder already exists on the disk.  We'll try to add it to the project.
                    Trace("Adding existing Resources folder directory to project: " & ResourcesFolderPath)

                    'This will add both the directory and all files in it.
                    Try
                        FoundProjectItem = Project.ProjectItems.AddFromDirectory(ResourcesFolderPath)
                    Catch ex As Exception
                        Throw New Exception(SR.GetString(SR.RFS_CantCreateResourcesFolder_Folder_ExMsg, ResourcesFolderName, ex.Message), ex)
                    End Try

                    'We didn't really want all those other files added to the project, so we try to remove them (but ignore
                    '  errors if they occur).
                    For Each ItemInFolder As ProjectItem In FoundProjectItem.ProjectItems
                        Try
                            ItemInFolder.Remove()
                        Catch ex As ThreadAbortException
                            Throw
                        Catch ex As StackOverflowException
                            Throw
                        Catch ex As Exception
                            Trace("Unable to remove extraneous item from Resources folder, ignoring: " & ItemInFolder.Name & ", " & ex.Message)
                        End Try
                    Next
                Else
                    'Second case - we need to actually create the folder
                    Try
                        Trace("Adding new Resources folder directory to project: " & ResourcesFolderName)
                        FoundProjectItem = Project.ProjectItems.AddFolder(ResourcesFolderName)
                    Catch ex As NotImplementedException
                        'Some projects, like C++, don't support adding a new folder.
                        'We'll simply have to add to the project's top-level node

                        Trace("Project doesn't implement ProjectItems.AddFolder().  Adding to top-level node instead.")
                        AddToProjectRoot = True
                    Catch ex As Exception
                        Throw New Exception(SR.GetString(SR.RFS_CantCreateResourcesFolder_Folder_ExMsg, ResourcesFolderName, ex.Message), ex)
                    End Try
                End If
            End If

            If AddToProjectRoot OrElse FoundProjectItem Is Nothing Then
                Debug.Assert(AddToProjectRoot, "Error in logic - FoundProjectItem should have been non-Nothing if AddToProject=False")

                'Add to the root of the project instead of a Resources folder.
                ResourcesFolderProjectItems = Project.ProjectItems
                ResourcesFolderPath = GetProjectDirectory(Project)
            Else
                Trace("Successfully added or created Resources folder: " & FoundProjectItem.Name & ", " & ResourcesFolderPath)
                ResourcesFolderProjectItems = FoundProjectItem.ProjectItems
                ResourcesFolderPath = GetFileNameFromFolderProjectItem(FoundProjectItem)
            End If
        End Sub

#End Region


#Region "Determine behavior for the project"

        ''' <summary>
        ''' Given a project, determines the appropriate behavior and Resources folder (if any) for this project,
        '''   depending on settings in the registry.
        ''' </summary>
        ''' <param name="Project">The project to check the behavior for.</param>
        ''' <param name="Behavior">[Out] Returns the add-to-project behavior to use for this project.</param>
        ''' <param name="ResourcesFolderName">[Out] Returns the name of the Resources folder to use for this project (only relevant if the behavior is to copy to the Resources folder)</param>
        ''' <remarks></remarks>
        Private Shared Sub GetProjectBehavior(ByVal Project As Project, ByRef Behavior As ResourcesFolderBehavior, ByRef ResourcesFolderName As String)
            'If we can't find evidence in the registry to say otherwise, our behavior will be to not do any
            '  copying of files into the project
            Behavior = ResourcesFolderBehavior.AddNone

            'We currently don't support anything but the default Resources folder name ("Resources")
            '  However, this could be changed later to be optionally pulled from the Registry.
            ResourcesFolderName = DEFAULT_RESOURCE_FOLDER_NAME

            If Project Is Nothing OrElse IsMiscellaneousProject(Project) Then
                'If there's no project or it's the miscellaneous files project, we simply get AddNone behavior.
                Exit Sub
            End If

            Try
                'First, get the base part of the registry path from ILocalRegistry.  It will look approximately like this:
                '
                '  SOFTWARE\Microsoft\VisualStudio\[CurrentVSVersion]\
                '
                Dim VsRootKeyPath As String = Project.DTE.RegistryRoot
                Debug.Assert(VsRootKeyPath <> "", "RegistryRoot returned bad string")

                'Get the path to the specific Project's node.  E.g.
                '
                '  SOFTWARE\Microsoft\VisualStudio\[CurrentVSVersion]\Projects\{guid}
                '
                Dim VSProjectsKeyPath As New StringBuilder(VsRootKeyPath)
                If VsRootKeyPath <> "" AndAlso Not VsRootKeyPath.EndsWith("\") Then
                    VSProjectsKeyPath.Append("\")
                End If
                VSProjectsKeyPath.Append(KEYPATH_PROJECTS)
                VSProjectsKeyPath.Append("\")
                VSProjectsKeyPath.Append(Project.Kind)

                'Open the key
                Dim VsProjectsKey As Win32.RegistryKey = Win32.Registry.LocalMachine.OpenSubKey(VSProjectsKeyPath.ToString(), writable:=False)
                If VsProjectsKey IsNot Nothing Then
                    Try
                        'Read the values
                        Dim DesiredBehavior As ResourcesFolderBehavior = DirectCast(VsProjectsKey.GetValue(KEYNAME_RESOURCESFOLDERBEHAVIOR, ResourcesFolderBehavior.AddNone), ResourcesFolderBehavior)
                        Dim DesiredResourcesFolderName As String = DirectCast(VsProjectsKey.GetValue(KEYNAME_RESOURCESFOLDERNAME, ResourcesFolderName), String)

                        'Validate the behavior from the registry
                        If System.Enum.IsDefined(GetType(ResourcesFolderBehavior), DesiredBehavior) Then
                            Behavior = DesiredBehavior
                        Else
                            Trace("ResourcesFolderBehavior in registry ({0}) is invalid - using default", CStr(DesiredBehavior))
                        End If

                        'Validate the resources folder name from the registry
                        Const MAX_RESOURCESFOLDERNAME_LENGTH As Integer = 40 'Reasonable value
                        Static InvalidPathCharsForVisualStudio() As Char = {"/"c, "?"c, ":"c, "&"c, "\"c, "*"c, """"c, "<"c, ">"c, "|"c, "#"c, "%"c}
                        If DesiredResourcesFolderName IsNot Nothing _
                                AndAlso DesiredResourcesFolderName.Length > 0 _
                                AndAlso DesiredResourcesFolderName.Length <= MAX_RESOURCESFOLDERNAME_LENGTH _
                                AndAlso DesiredResourcesFolderName.IndexOfAny(Path.GetInvalidPathChars) < 0 _
                                AndAlso DesiredResourcesFolderName.IndexOfAny(Path.GetInvalidFileNameChars) < 0 _
                                AndAlso DesiredResourcesFolderName.IndexOfAny(InvalidPathCharsForVisualStudio) < 0 _
                        Then
                            ResourcesFolderName = DesiredResourcesFolderName
                        Else
                            Trace("ResourcesFolderName in registry (""{0}"") is invalid - using default", DesiredResourcesFolderName)
                        End If
                    Finally
                        VsProjectsKey.Close()
                    End Try
                End If
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As Exception
                Debug.Fail("Exception thrown trying to get ResourcesFolderBehavior value from local registry: " & ex.Message)
                'Ignore any other exceptions - perhaps the key was the wrong data type, etc.  It will simply be treated as
                '  if the behavior is AddNone.
            End Try

#If DEBUG Then
            Static ProjectGuid_JSharp As New Guid("E6FDF86B-F3D1-11D4-8576-0002A516ECE8")
            Static ProjectGuid_VB As New Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")
            Static ProjectGuid_CSharp As New Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
            Static ProjectGuid_CPlusPlus As New Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942")
            Static ProjectGuid_ASPDotNet As New Guid("E24C65DC-7377-472b-9ABA-BC803B73C61A")
#End If

            Trace("ResourcesFolderBehavior = " & System.Enum.GetName(GetType(ResourcesFolderBehavior), Behavior))
            Trace("ResourcesFolderName= " & ResourcesFolderName)
        End Sub


        ''' <summary>
        ''' Determines the ProjectsItems collection in the project where an imported file should be placed, depending
        '''   on the specified behavior for the project.
        ''' </summary>
        ''' <param name="Project">The project where the imported file will be added (e.g., a bitmap file)</param>
        ''' <param name="Behavior">The behavior to use in determining the location to the place the file.</param>
        ''' <param name="ResourcesFolderName">The name of the Resources folder in this project.</param>
        ''' <param name="DestinationProjectItems">[Out] Returns the ProjectItem collection in the project where the file should be placed.</param>
        ''' <param name="DestinationPath">[Out] Returns the path on disk for this folder.</param>
        ''' <remarks>
        ''' If DestinationProjectItems is returned as Nothing, assume the same behavior as ResourcesFolderBehavior.AddNone.
        ''' </remarks>
        Private Shared Sub GetDestinationFolder(ByVal Project As Project, ByVal ResXProjectItem As ProjectItem, ByVal Behavior As ResourcesFolderBehavior, ByVal ResourcesFolderName As String, ByRef DestinationProjectItems As ProjectItems, ByRef DestinationPath As String)
            DestinationProjectItems = Nothing
            DestinationPath = Nothing

            Select Case Behavior
                Case ResourcesFolderBehavior.AddNone
                    Exit Sub

                Case ResourcesFolderBehavior.AddToProjectRoot
                    DestinationProjectItems = Project.ProjectItems
                    DestinationPath = GetProjectDirectory(Project)
                    Exit Sub

                Case ResourcesFolderBehavior.AddToResourcesFolder
                    GetOrCreateResourcesFolder(Project, ResourcesFolderName, DestinationProjectItems, DestinationPath)
                    Exit Sub

                Case ResourcesFolderBehavior.AddToResXFolder
                    If ResXProjectItem Is Nothing Then
                        Debug.Fail("ResXProjectItem shouldn't be Nothing!")
                        DestinationProjectItems = Project.ProjectItems
                    Else
                        DestinationProjectItems = ResXProjectItem.Collection
                        If Not Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(DestinationProjectItems.Kind)) Then
                            'We don't want to add to a non-physical folder.  Choose the root folder instead.
                            DestinationProjectItems = Project.ProjectItems
                        End If
                    End If
                    DestinationPath = GetFolderNameFromProjectItems(DestinationProjectItems)
                    Exit Sub

                Case Else
                    Debug.Fail("Unrecognized ResourcesFolderBehavior")
                    Exit Sub
            End Select
        End Sub

#End Region


#Region "Utilities"

        ''' <summary>
        ''' Given a collection of ProjectItem ("ProjectItems"), queries it for the ProjectItem
        '''   of a given key.  If not found, returns Nothing.
        ''' </summary>
        ''' <param name="ProjectItems">The collection of ProjectItem to check</param>
        ''' <param name="Name">The key to check for.</param>
        ''' <returns>The ProjectItem for the given key, if found, else Nothing.  Throws exceptions only in unexpected cases.</returns>
        ''' <remarks></remarks>
        Friend Shared Function QueryProjectItems(ByVal ProjectItems As ProjectItems, ByVal Name As String) As ProjectItem
            Try
                Return ProjectItems.Item(Name)
            Catch ex As ArgumentException
                'This is the expected exception if the key could not be found.
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As Exception
                'Any other error - shouldn't be the case, but it might depend on the project implementation
                Debug.Fail("Unexpected exception searching for an item in ProjectItems: " & ex.Message)
            End Try

            Return Nothing
        End Function


        ''' <summary>
        ''' Given a directory on disk and optionally a ProjectItems collection, creates a filename which
        '''   is unique in that directory and (if given) unique among the ProjectsItem's in the collection.
        ''' </summary>
        ''' <param name="ParentFolder">A ProjectItems collection to check for project item name uniqueness.  Can be Nothing.</param>
        ''' <param name="ParentFolderPath">A directory on disk to check for filename uniqueness.</param>
        ''' <param name="BaseFileName">The base filename to use.  This name will be modified until a unique name is found.</param>
        ''' <returns>The unique filename and path.</returns>
        ''' <remarks>The returned name is always munged.</remarks>
        Private Shared Function MakeUniqueFileName(ByVal ParentFolder As ProjectItems, ByVal ParentFolderPath As String, ByVal BaseFileName As String) As String
            Dim Append As Integer = 1

            Do
                Dim NewFileNamePath As String = Path.Combine(ParentFolderPath, Path.GetFileNameWithoutExtension(BaseFileName) & CStr(Append) & Path.GetExtension(BaseFileName))

                'Test if the filename is unique, both as a project item and as a location on disk.
                Dim IsUnique As Boolean = True

                If ParentFolder IsNot Nothing AndAlso QueryProjectItems(ParentFolder, Path.GetFileName(NewFileNamePath)) IsNot Nothing Then
                    IsUnique = False
                ElseIf File.Exists(NewFileNamePath) Then
                    IsUnique = False
                End If

                If IsUnique Then
                    Trace("Found unique name: " & NewFileNamePath)
                    Return NewFileNamePath
                End If

                Append += 1
            Loop
        End Function


        ''' <summary>
        ''' Retrieves the file name on disk for a ProjectItem.
        ''' </summary>
        ''' <param name="ProjectItem">The project item to check.</param>
        ''' <returns>The filename and path of the project item.</returns>
        ''' <remarks></remarks>
        Private Shared Function GetFileNameFromProjectItem(ByVal ProjectItem As ProjectItem, ByVal ExpectedExtension As String) As String
            'Look for a FileName with the expected extension.  It's unclear whether we can assume it will always be the
            '  first one.  (There can be multiple files for a ProjectItem for such cases as code behind, etc.)
            For i As Short = 1 To ProjectItem.FileCount 'this collection is 1-indexed
                Dim FileName As String = ProjectItem.FileNames(i)
                If Path.GetExtension(FileName).Equals(ExpectedExtension, StringComparison.OrdinalIgnoreCase) Then
                    Return FileName
                End If
            Next

            Debug.Fail("Didn't find a ProjectItem.FileName with the expected extension")
            Return ProjectItem.FileNames(1)
        End Function


        ''' <summary>
        ''' Retrieves the file name on disk for a ProjectItem.
        ''' </summary>
        ''' <param name="ProjectItem">The project item to check.</param>
        ''' <returns>The filename and path of the project item.</returns>
        ''' <remarks></remarks>
        Private Shared Function GetFileNameFromFolderProjectItem(ByVal ProjectItem As ProjectItem) As String
            If Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(ProjectItem.Kind)) Then
                'The FileNames property represents the actual full path of the directory if the folder
                '  is an actual physical folder on disk.
                Debug.Assert(ProjectItem.FileCount = 1, "Didn't expect multiple filenames for a folder ProjectItem")
                Return ProjectItem.FileNames(1) 'this collection is 1-indexed
            Else
                Debug.Fail("Trying to get filename of a non-physical folder in the project")
                Return ""
            End If
        End Function


        ''' <summary>
        ''' Retrieves the directory name on disk for a ProjectItems collection.
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectItems collection to check.  Must refer to a physical folder on disk.</param>
        ''' <returns>The directory name of the collection on disk.</returns>
        ''' <remarks></remarks>
        Friend Shared Function GetFolderNameFromProjectItems(ByVal ProjectItems As ProjectItems) As String
            If Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(ProjectItems.Kind)) Then
                If TypeOf ProjectItems.Parent Is Project Then
                    Return GetProjectDirectory(DirectCast(ProjectItems.Parent, Project))
                ElseIf TypeOf ProjectItems.Parent Is ProjectItem Then
                    Return GetFileNameFromFolderProjectItem(DirectCast(ProjectItems.Parent, ProjectItem))
                Else
                    Debug.Fail("Unexpected Parent type for ProjectItems")
                    Return Nothing
                End If
            Else
                Debug.Fail("Shouldn't call GetFileNameFromProjectItems for a ProjectItems collection that is not a physical disk folder.")
                Return ""
            End If
        End Function


        ''' <summary>
        ''' Given a project, determine if it is the Miscellaneous Files project
        ''' </summary>
        ''' <param name="Project"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function IsMiscellaneousProject(ByVal Project As Project) As Boolean
            If vsMiscFilesProjectUniqueName.Equals(Project.UniqueName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            If Project.FullName = "" Then
                Debug.Fail("This project is not the miscellaneous files project, but its FullName is empty!")
                Return True 'defensive
            End If

            Return False
        End Function


        ''' <summary>
        ''' Given a project, returns the project's directory on disk.
        ''' </summary>
        ''' <param name="Project">The project to query.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetProjectDirectory(ByVal Project As Project) As String
            'Some special cases.  In particular, note that the Miscellaneous Files project
            '  has a FullName value of the empty string.
            If Project Is Nothing OrElse Project.FullName Is Nothing OrElse Project.FullName = "" OrElse IsMiscellaneousProject(Project) Then
                Debug.Fail("Shouldn't be calling this with a null Project or with the Miscellaneous Files Project")
                Return ""
            End If

            Dim ProjectDirectory As String
            Try
                ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(Project.FullName))
            Catch ex As ArgumentException
                'In some scenarios Project.FullName does not give us an actual location on the local file
                '  system (e.g. when working with ASP.NET projects created on a URL instead of the local file
                '  system).  ASP.NET projects have a FullPath property which gives us what we want.  Let's try
                '  that before giving up.
                ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(CStr(Project.Properties.Item("FullPath").Value)))
            End Try

            Debug.Assert(Directory.Exists(ProjectDirectory), "Project's FullName property is not its path on disk?")
            Return ProjectDirectory
        End Function


        ''' <summary>
        ''' Determines whether one Directory path is a subdirectory of the other
        ''' </summary>
        ''' <param name="Directory1"></param>
        ''' <param name="Directory2"></param>
        ''' <returns>True if Directory1 is a subdirectory of Directory2</returns>
        ''' <remarks>
        ''' This does not handle cases where the directories are related by a mapped drive.
        ''' </remarks>
        Private Shared Function IsSubdirectoryOf(ByVal Directory1 As String, ByVal Directory2 As String) As Boolean
            Directory1 = Path.GetFullPath(Directory1)
            Directory2 = Path.GetFullPath(Directory2)

            While Directory1.Length >= Directory2.Length
                If Directory1.Equals(Directory2, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If

                'Get Directory1's parent path
                Dim Parent As String = Path.GetDirectoryName(Directory1)
                If Parent Is Nothing Then
                    Exit While
                End If
                Directory1 = Parent
            End While

            Return False
        End Function


        ''' <summary>
        ''' Given a ProjectItem, determines the parent ProjectItem collection that contains it.
        ''' </summary>
        ''' <param name="Project">The project in which to search</param>
        ''' <param name="FolderPathToFind">The folder path to search for</param>
        ''' <returns>The parent ProjectItem collection.</returns>
        ''' <remarks></remarks>
        Private Shared Function FindProjectItemsForFolderPath(ByVal Project As Project, ByVal FolderPathToFind As String) As ProjectItems
            Return FindProjectItemsForFolderPathHelper(Project.ProjectItems, EnsureBackslash(Path.GetFullPath(FolderPathToFind)))
        End Function


        ''' <summary>
        ''' Searches for a ProjectItem within a Project collection and all of its contained ProjectItems collections
        ''' </summary>
        ''' <param name="ProjectItemsTree">The ProjectItems subtree to search in.</param>
        ''' <param name="FullFolderPathToFind">The folder path to look for.  Must have had Path.GetFullPath() and EnsureBackslash() called on it.</param>
        ''' <returns>The parent ProjectItem collection, or Nothing if not found.</returns>
        ''' <remarks></remarks>
        Private Shared Function FindProjectItemsForFolderPathHelper(ByVal ProjectItemsTree As ProjectItems, ByVal FullFolderPathToFind As String) As ProjectItems
            Debug.Assert(FullFolderPathToFind.Equals(Path.GetFullPath(FullFolderPathToFind), StringComparison.OrdinalIgnoreCase), _
                "FullFolderPathToFind should have already had Path.GetFullPath() called on it")
            Debug.Assert(FullFolderPathToFind.Equals(EnsureBackslash(FullFolderPathToFind), StringComparison.OrdinalIgnoreCase), _
                "FullFolderPathToFind should have already had EnsureBackslash() called on it")

            If Guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(ProjectItemsTree.Kind)) Then
                Dim ProjectItemsFilePath As String = GetFolderNameFromProjectItems(ProjectItemsTree)
                If EnsureBackslash(Path.GetFullPath(ProjectItemsFilePath)).Equals(FullFolderPathToFind, StringComparison.OrdinalIgnoreCase) Then
                    Return ProjectItemsTree
                End If
            End If

            For Each ProjectItem As ProjectItem In ProjectItemsTree
                Dim SearchDeeper As ProjectItems = FindProjectItemsForFolderPathHelper(ProjectItem.ProjectItems, FullFolderPathToFind)
                If SearchDeeper IsNot Nothing Then
                    Return SearchDeeper
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Given a file directory path, ensures that it ends with a backslash
        ''' </summary>
        ''' <param name="FilePath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function EnsureBackslash(ByVal FilePath As String) As String
            If FilePath IsNot Nothing AndAlso Not FilePath.EndsWith(Path.DirectorySeparatorChar) Then
                Return FilePath & Path.DirectorySeparatorChar
            Else
                Return FilePath
            End If
        End Function

#End Region


#End Region

    End Class

End Namespace
