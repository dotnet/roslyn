Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Collections.Specialized
Imports System.Drawing
Imports System.IO

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A resource type editor that handles Bitmaps (GIF, JPG, BMP, etc).
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditorBitmap
        Inherits ResourceTypeEditorImageBase

        'All common file extensions handled by this resource type editor
        'We don't include an extension for EXIF, because they're just saved as JPEG files
        Private Extensions() As String = {
            EXT_BMP,
            EXT_GIF,
            EXT_JPEG, EXT_JPG,
            EXT_PNG,
            EXT_TIF, EXT_TIFF}

        ' Extensions supported in a device project
        ' NOTE: WinCE does not support TIF file...
        Private ExtensionsForDevice() As String = {
            EXT_BMP,
            EXT_GIF,
            EXT_JPEG, EXT_JPG,
            EXT_PNG}


        ' keep the mapping table from extension to resource ID
        Private Shared m_ManifestResourceList As ListDictionary


        '======================================================================
        '= METHODS =                                                          =
        '======================================================================




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
            If ResourceContentFile.IsSupportedType(GetType(Bitmap)) Then
                Return GetType(Bitmap).AssemblyQualifiedName
            Else
                Return GetType(Byte()).AssemblyQualifiedName
            End If
        End Function



        ''' <summary>
        ''' Loads a resource that this resource type editor handles from a file on disk.
        ''' </summary>
        ''' <param name="FilePath"></param>
        ''' <param name="ResourceContentFile">The resource file contains the resource item. </param>
        ''' <returns></returns>
        ''' <remarks>
        ''' Default implementation uses a ResXFileRef to automatically load the resource from the file.  Can be
        '''   overriden if this behavior is insufficient.
        ''' Exceptions should be handled by caller.
        '''</remarks>
        Public Overrides Function LoadResourceFromFile(ByVal FilePath As String, ByVal ResourceContentFile As IResourceContentFile) As Object
            'Use the base behavior, except verify that the Image that is returned is actually a bitmap,
            '  and not a metafile, etc. (could only happen if the user put the wrong extension on a file).
            Dim LoadedImage As Image = DirectCast(MyBase.LoadResourceFromFile(FilePath, ResourceContentFile), Image)
            If TypeOf LoadedImage Is Bitmap Then
                Return LoadedImage
            Else
                Throw NewException(SR.GetString(SR.RSE_Err_UnexpectedResourceType), HelpIDs.Err_UnexpectedResourceType)
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
            Dim ManifestResourceId As String = Nothing

            Dim resourceDictionary As ListDictionary = GetManifestResourceList()
            Dim extension As String = IO.Path.GetExtension(FilePath)
            If extension IsNot Nothing Then
                ManifestResourceId = CType(resourceDictionary.Item(extension), String)
            End If

            If ManifestResourceId Is Nothing Then
                ' default...
                ManifestResourceId = "BlankBmp"
            End If

            SaveFileFromManifestResource(ManifestResourceId, FilePath)
        End Sub

        ''' <summary>
        ''' Create extension -> resource ID mapping table
        ''' </summary>
        ''' <return></return>
        ''' <remarks></remarks>
        Private Shared Function GetManifestResourceList() As ListDictionary
            If m_ManifestResourceList Is Nothing Then
                m_ManifestResourceList = New ListDictionary(StringComparer.OrdinalIgnoreCase)

                m_ManifestResourceList.Add(EXT_BMP, "BlankBmp")
                m_ManifestResourceList.Add(EXT_GIF, "BlankGif")
                m_ManifestResourceList.Add(EXT_JPEG, "BlankJpeg")
                m_ManifestResourceList.Add(EXT_JPG, "BlankJpeg")
                m_ManifestResourceList.Add(EXT_PNG, "BlankPng")
                m_ManifestResourceList.Add(EXT_TIF, "BlankTiff")
                m_ManifestResourceList.Add(EXT_TIFF, "BlankTiff")
            End If
            Return m_ManifestResourceList
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
            If ResourceContentFile.IsInsideDeviceProject() Then
                Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Bitmap), ExtensionsForDevice)
            Else
                Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Bitmap), Extensions)
            End If
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
        ''' Gets a filter string for use with a file save dialog.  That filter should contain all commonly-supported
        '''   extensions handled by this resource type editor (but does not have to necessarily include all of
        '''   them).
        ''' </summary>
        ''' <returns>The filter string.</returns>
        ''' <remarks>
        ''' An example filter string:
        '''   
        '''   "Windows metafile (*.wmf)|*.wmf|Windows Enhanced Metafile (*.emf)|*.emf|Windows Bitmap (*.bmp;*.dib)|*.bmp;*.dib"
        ''' </remarks>
        Public Overrides Function GetSaveFileDialogFilter(ByVal Extension As String) As String
            Dim ExtensionIndex As Integer
            Dim FilterText As String = ""

            'Find out which filter to use based on the expected resource extension
            If MatchAgainstListOfExtensions(Extension, Extensions, ExtensionIndex) Then
                Select Case Extensions(ExtensionIndex)
                    Case EXT_BMP
                        FilterText = SR.GetString(SR.RSE_FilterSave_BMP)
                    Case EXT_PNG
                        FilterText = SR.GetString(SR.RSE_FilterSave_PNG)
                    Case EXT_GIF
                        FilterText = SR.GetString(SR.RSE_FilterSave_GIF)
                    Case EXT_JPG, EXT_JPEG
                        FilterText = SR.GetString(SR.RSE_FilterSave_JPEG)
                    Case EXT_TIF, EXT_TIFF
                        FilterText = SR.GetString(SR.RSE_FilterSave_TIFF)
                    Case Else
                        Debug.Fail("Unexpected extension " & Extensions(ExtensionIndex))
                End Select
            End If

            Return CreateSingleDialogFilter(FilterText, New String() {Extension})
        End Function


        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail. Not used for this overload since we return the actual resource as a thumbnail</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' This bitmap will be used as
        '''   the basis for creating a thumbnail image (the image returned by this function will not be needed
        '''   after the thumbnail image is created).
        ''' Default implementation returns an empty bitmap.
        ''' </remarks>
        Public Overrides Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            ValidateResourceValue(Resource, GetType(Bitmap), GetType(Byte()))

            Return DirectCast(Resource.GetValue(), Bitmap)
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
            Else
                Return ExtensionPriorities.NotHandled
            End If
        End Function

        ''' <summary>
        ''' Indicates whether the resources edited by this resource editor type are allowed to have their
        '''   Persistence property changed.
        ''' </summary>
        ''' <param name="ResourceContentFile">The resource file that contains the resource</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides ReadOnly Property CanChangePersistenceProperty(ResourceContentFile As ResourceTypeEditor.IResourceContentFile) As Boolean
            Get
                ' We don't support changing the persistence type unless Bitmap is supported because embedding
                ' a resource as a byte array is ambiguous in scenarios where the resource designer reads
                ' the resource back from the file.
                Return ResourceContentFile.IsSupportedType(GetType(Bitmap))
            End Get
        End Property

        ''' <summary>
        ''' Some resources may be stored as byte arrays in the resx (i.e. bitmaps on platforms that don't
        ''' support System.Drawing.Bitmap at runtime).  This method allows a resource type editor to
        ''' convert the byte array to a resource instance so that the resource can be modified in the
        ''' resource editor.
        ''' </summary>
        ''' <param name="Value">The byte array to convert</param>
        ''' <returns>
        ''' If the conversion is supported, the converted value is returned.  Otherwise Nothing is returned
        ''' </returns>
        Public Overrides Function ConvertByteArrayToResourceValue(Value() As Byte) As Object
            Using MemoryStream As New MemoryStream(Value)
                Return New Bitmap(MemoryStream)
            End Using
        End Function

    End Class

End Namespace
