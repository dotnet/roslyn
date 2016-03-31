Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualStudio.Imaging

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class ResourceTypeEditorAudio
        Inherits ResourceTypeEditorInternalBase

        'All common file extensions handled by this resource type editor.
        Public Const EXT_WAV As String = ".wav"
        Private Extensions() As String = {EXT_WAV}

        'The resource value type that is used for audio files
        Friend Shared ReadOnly AudioFileValueType As System.Type = GetType(MemoryStream)

        'The resource value type that is used for binary files
        Friend Shared ReadOnly BinaryFileValueType As System.Type = GetType(Byte())

        'The shared Thumbnail Image
        Private Shared m_ThumbnailForAudio As Image


        ''' <summary>
        '''  Whether all valid items share a same image
        ''' </summary>
        ''' <returns>true or false</returns>
        ''' <remarks>
        '''   If true, the GetImageForThumbnail property must return a same image object. We won't keep duplicated items in the cache to improve the performance of the designer.
        ''' </remarks>
        Public Overrides ReadOnly Property IsImageForThumbnailShared() As Boolean
            Get
                Return True
            End Get
        End Property


        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail.</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' For audio, we always show the exact same thumbnail image
        ''' </remarks>
        Public Overrides Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))
            If m_ThumbnailForAudio Is Nothing Then
                m_ThumbnailForAudio = Common.Utils.GetImageFromImageService(KnownMonikers.Sound, 48, 48, background)
            End If
            Return m_ThumbnailForAudio
        End Function


        ''' <summary>
        ''' Returns whether a given file extension can be handled by this resource type editor, and at what
        '''   priority.
        ''' </summary>
        ''' <param name="Extension">The file extension to check, including the dot (e.g., ".bmp")</param>
        ''' <returns>A positive integer if this extension can be handled.  The value indicates what priority
        '''   should be given to this resource editor.  The higher the value, the higher the priority.
        ''' </returns>
        ''' <remarks>Extension should be checked case-insensitively.</remarks>
        Public Overrides Function GetExtensionPriority(ByVal Extension As String) As Integer
            If MatchAgainstListOfExtensions(Extension, Extensions) Then
                Return ExtensionPriorities.Normal
            End If
        End Function


        ''' <summary>
        ''' Gets the type name of the main resource type that this resource type editor handles
        ''' </summary>
        ''' <param name="ResourceContentFile">The resource file contains the resource.</param>
        ''' <returns>The type name handled by this resource type editor.</returns>
        ''' <remarks>A single resource type editor may only handle a single resource type, with
        '''   the exception of the string type editors (in particular,
        '''   ResourceTypeEditorNonStringConvertible and ResourceTypeEditorStringConvertible).
        '''  Because the resource file in different platform doesn't support all types, we should pick up the right type for the platform it targets.
        ''' </remarks>
        Public Overrides Function GetDefaultResourceTypeName(ByVal ResourceContentFile As IResourceContentFile) As String
            If ResourceContentFile.IsInsideDeviceProject() Then
                Return BinaryFileValueType.AssemblyQualifiedName
            Else
                Return AudioFileValueType.AssemblyQualifiedName
            End If
        End Function


        ''' <summary>
        ''' Gets a friendly description to display to the user that indicates the type of a
        '''   particular resource.  E.g., "BMP Image".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly description of the resource's type.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlyTypeDescription(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))
            Return SR.GetString(SR.RSE_Type_Wave)
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))

            If Resource.IsLink Then
                Return GetLinkedResourceFriendlySize(Resource)
            Else
                Dim ResourceValue As Object = Resource.GetValue()
                Dim Length As Integer
                If TypeOf ResourceValue Is Byte() Then
                    Length = DirectCast(ResourceValue, Byte()).Length
                ElseIf TypeOf ResourceValue Is MemoryStream Then
                    Length = CInt(DirectCast(ResourceValue, MemoryStream).Length)
                Else
                    Debug.Fail("Unexpected audio type")
                    Return ""
                End If
                Return GetKBDisplay(Length)
            End If
        End Function


        ''' <summary>
        ''' Creates a new resource of the type handled by this ResourceTypeEditor at the file path 
        '''   specified.
        ''' </summary>
        ''' <param name="FilePath">File path and name of the file to save the new resource into.</param>
        ''' <remarks>
        ''' If this ResourceTypeEditor handles different resource types, it should inspect the 
        '''   file extension to determine which type of resource to create (e.g., .bmp vs .jpg).  It might
        '''   find any unexpected extension, in which case it should save to its default format.
        ''' This function need not be implemented if CanSaveResourceToFile() is implemented to return False.
        ''' Caller is responsible for handling exceptions.
        ''' </remarks>
        Public Overrides Sub CreateNewResourceFile(ByVal FilePath As String)
            SaveFileFromManifestResource("BlankWav", FilePath)
        End Sub


        ''' <summary>
        ''' Determines whether a specific ResourceValue can be saved by this ResourceTypeEditor into 
        '''   a file or not.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>True iff the resource type editor supports saving the specific resource value to a file.</returns>
        ''' <remarks></remarks>
        Public Overrides Function CanSaveResourceToFile(ByVal Resource As IResource) As Boolean
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))
            Return True
        End Function


        ''' <summary>
        ''' Saves a given resource value to a file.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="FilePath">File path and name of the file to save to.</param>
        ''' <remarks>Caller is responsible for handling exceptions raised by this method.</remarks>
        Public Overrides Sub SaveResourceToFile(ByVal Resource As IResource, ByVal FilePath As String)
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))

            Dim ResourceValue As Object = Resource.GetValue()
            Dim SourceBytes() As Byte
            Dim DestStream As FileStream = Nothing
            Try
                If TypeOf ResourceValue Is Byte() Then
                    SourceBytes = DirectCast(ResourceValue, Byte())
                ElseIf TypeOf ResourceValue Is MemoryStream Then
                    Dim SourceStream As MemoryStream = DirectCast(ResourceValue, MemoryStream)
                    'Be sure we're at the beginning of the stream
                    SourceStream.Seek(0, SeekOrigin.Begin)
                    SourceBytes = SourceStream.ToArray()

                    SourceStream.Seek(0, SeekOrigin.Begin) 'Reset to beginning
                Else
                    Debug.Fail("Unexpected audio resource type")
                    Throw NewException(SR.GetString(SR.RSE_Err_UnexpectedResourceType), HelpIDs.Err_UnexpectedResourceType)
                End If

                DestStream = New FileStream(FilePath, FileMode.Create, FileAccess.Write)
                Dim Writer As New BinaryWriter(DestStream)
                Writer.Write(SourceBytes)
                Writer.Flush()
            Finally
                If DestStream IsNot Nothing Then
                    DestStream.Close()
                End If

                'Don't close the source stream.  We don't own it.
            End Try
        End Sub


        ''' <summary>
        ''' Gets the proper file extension to use for a particular resource.  The extension returned
        '''   may be different for different resources using the same ResourceTypeEditor.  For example,
        '''   both BMP and JPG images uses the same type editor, but the extension for the individual
        '''   resources is different.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The file extension to use for this resource value, if anything, including the
        '''   period.  E.g. ".bmp".  Returns Nothing or an empty string if this is not applicable for 
        '''   this resource.</returns>
        ''' <remarks>The default implementation returns Nothing.</remarks>
        Public Overrides Function GetResourceFileExtension(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, GetType(Byte()), GetType(MemoryStream))
            Return EXT_WAV
        End Function

        ''' <summary>
        '''  Returns a list of extensions. If the file matched one of them, it should be safe to open the file in VS. Otherwise, it could
        '''  be a harmful operation to open the file if it comes from someone else. For example, the resource file could link to a file containing script.
        '''  The resource designer could open the file in an external editor, which could run the script, and do harmful things on the dev machine.
        ''' </summary>
        ''' <returns> a file extension list
        ''' </returns>
        Public Overrides Function GetSafeFileExtensionList() As String()
            Return Extensions
        End Function

        ''' <summary>
        ''' Gets a filter string for use with a file open dialog.  That filter should contain all commonly-supported
        '''   extensions handled by this resource type editor (but does not have to necessarily include all of
        '''   them).
        ''' </summary>
        ''' <param name="ResourceContentFile">The resource file contains the resource.</param>
        ''' <returns>The filter string.</returns>
        ''' <remarks>
        ''' An example filter string:
        '''   
        '''   "Metafiles (*.wmf, *.emf)|*.wmf;*.emf"
        ''' </remarks>
        Public Overrides Function GetOpenFileDialogFilter(ByVal ResourceContentFile As IResourceContentFile) As String
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Audio), Extensions)
        End Function


        ''' <summary>
        ''' Gets a filter string for use with a file save dialog.  That filter should contain all commonly-supported
        '''   extensions handled by this resource type editor (but does not have to necessarily include all of
        '''   them).
        ''' </summary>
        ''' <returns>The filter string.</returns>
        ''' <remarks>
        ''' An example filter string:
        '''   
        '''   "Windows metafile (*.wmf, *.emf)|*.wmf;*.emf"
        ''' </remarks>
        Public Overrides Function GetSaveFileDialogFilter(ByVal Extension As String) As String
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Audio), New String() {EXT_WAV})
        End Function


        ''' <summary>
        ''' Gets the prefix that is used for suggesting resource names to the user.  For instance,
        '''   if this function returns "id", then as the user asks to create a new resource
        '''   handled by this resource type editor, the suggested names could take the form 
        '''   of "id01", "id02", etc.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function GetSuggestedNamePrefix() As String
            Return "Sound"
        End Function


    End Class

End Namespace
