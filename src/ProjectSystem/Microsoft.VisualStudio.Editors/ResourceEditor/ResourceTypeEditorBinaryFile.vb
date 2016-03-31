Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Drawing
Imports Microsoft.VisualStudio.Imaging

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class ResourceTypeEditorBinaryFile
        Inherits ResourceTypeEditorFileBase

        'The resource value type that is used for binary files
        Friend Shared ReadOnly BinaryFileValueType As System.Type = GetType(Byte())

        ' file with those extension are safe to open 
        Private SafeExtensions() As String = {
            ".avi",
            ".emf",
            ".mp3",
            ".mid",
            ".wmf",
            ".wma"
        }

        'The shared Thumbnail Image
        Private Shared m_ThumbnailForBinaryFile As Image

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
            Return BinaryFileValueType.AssemblyQualifiedName
        End Function



        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail.</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' For files, we have exactly two thumbnails - one for text files, one for binary files
        ''' </remarks>
        Public Overrides Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            ValidateResourceValue(Resource, BinaryFileValueType)
            Debug.Assert(Resource.IsLink)
            If m_ThumbnailForBinaryFile Is Nothing Then
                m_ThumbnailForBinaryFile = Common.Utils.GetImageFromImageService(KnownMonikers.BinaryFile, 48, 48, background)
            End If
            Return m_ThumbnailForBinaryFile
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
            'This type editor handle all file extensions, no matter what they are (but at lowest priority).
            Return ExtensionPriorities.Lowest
        End Function


        ''' <summary>
        ''' Gets a friendly description to display to the user that indicates the type of a
        '''   particular resource.  E.g., "BMP Image".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly description of the resource's type.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlyTypeDescription(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, BinaryFileValueType)
            Debug.Assert(Resource.IsLink)
            Return SR.GetString(SR.RSE_Type_BinaryFile)
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, BinaryFileValueType)
            Debug.Assert(Resource.GetValueType().Equals(BinaryFileValueType))
            Debug.Assert(BinaryFileValueType.Equals(GetType(Byte())), "Need to change implementation - type of binary files has changed")
            Debug.Assert(Resource.IsLink)

            Return GetLinkedResourceFriendlySize(Resource)
        End Function

        ''' <summary>
        '''  Returns a list of extensions. If the file matched one of them, it should be safe to open the file in VS. Otherwise, it could
        '''  be a harmful operation to open the file if it comes from someone else. For example, the resource file could link to a file containing script.
        '''  The resource designer could open the file in an external editor, which could run the script, and do harmful things on the dev machine.
        ''' </summary>
        ''' <returns> a file extension list
        ''' </returns>
        Public Overrides Function GetSafeFileExtensionList() As String()
            Return SafeExtensions
        End Function



    End Class
End Namespace
