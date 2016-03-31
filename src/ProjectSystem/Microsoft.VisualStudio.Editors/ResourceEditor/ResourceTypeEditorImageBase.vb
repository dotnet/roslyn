Option Explicit On
Option Strict On
Option Compare Binary

Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A base class for resource type editors that handle classes derived from Image (bitmaps and theoretically metafiles).
    ''' </summary>
    ''' <remarks>Must be inherited.</remarks>
    Friend MustInherit Class ResourceTypeEditorImageBase
        Inherits ResourceTypeEditorInternalBase

        'File extension constants (should be treated as non-case-sensitive)
        Public Const EXT_BMP As String = ".bmp"
        Public Const EXT_JPG As String = ".jpg"
        Public Const EXT_JPEG As String = ".jpeg"
        Public Const EXT_PNG As String = ".png"
        Public Const EXT_TIF As String = ".tif"
        Public Const EXT_TIFF As String = ".tiff"
        Public Const EXT_GIF As String = ".gif"



        '======================================================================
        '= METHODS =                                                          =
        '======================================================================

        Private Overloads Sub ValidateResourceValue(Resource As IResource)
            ValidateResourceValue(Resource, GetType(Image), GetType(Byte()))
        End Sub

        ''' <summary>
        ''' Determines whether a specific ResourceValue can be saved by this ResourceTypeEditor into 
        '''   a file or not.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>True iff the resource type editor supports saving the specific resource value to a file.</returns>
        ''' <remarks>Handles all Image types</remarks>
        Public Overrides Function CanSaveResourceToFile(ByVal Resource As IResource) As Boolean
            ValidateResourceValue(Resource)
            Return True
        End Function


        ''' <summary>
        ''' Saves a given resource value to a file.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="FilePath">File path and name of the file to save to.</param>
        ''' <remarks>
        '''   Caller is responsible for handling exceptions raised by this method.
        '''   Handles all Image types.
        ''' </remarks>
        Public Overrides Sub SaveResourceToFile(ByVal Resource As IResource, ByVal FilePath As String)
            ValidateResourceValue(Resource)
            Dim Image As Image = CType(Resource.GetValue(), Image)
            Image.Save(FilePath, Image.RawFormat)
            Debug.Assert(New IO.FileInfo(FilePath).Length > 0, "Saved file has zero length")
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
        ''' <remarks>Handles all Image types.</remarks>
        Public Overrides Function GetResourceFileExtension(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource)

            Dim ImageFormat As ImageFormat = CType(Resource.GetValue(), Image).RawFormat

            If ImageFormat.Equals(ImageFormat.Bmp) Then
                Return EXT_BMP
            ElseIf ImageFormat.Equals(ImageFormat.Emf) Then
                Debug.Fail("How did we get an EMF image?")
            ElseIf ImageFormat.Equals(ImageFormat.Exif) Then
                Return EXT_JPG 'EXIF doesn't have an extension - it's just a JPEG
            ElseIf ImageFormat.Equals(ImageFormat.Gif) Then
                Return EXT_GIF
            ElseIf ImageFormat.Equals(ImageFormat.Jpeg) Then
                Return EXT_JPG
            ElseIf ImageFormat.Equals(ImageFormat.MemoryBmp) Then
                Return EXT_BMP
            ElseIf ImageFormat.Equals(ImageFormat.Png) Then
                Return EXT_PNG
            ElseIf ImageFormat.Equals(ImageFormat.Tiff) Then
                Return EXT_TIF
            ElseIf ImageFormat.Equals(ImageFormat.Wmf) Then
                Debug.Fail("How did we get a WMF image?")
            Else
                Debug.Fail("Unrecognized raw image format of image")
            End If

            'Defensive
            Return EXT_BMP
        End Function


        ''' <summary>
        ''' Gets a friendly description to display to the user that indicates the type of a
        '''   particular resource.  E.g., "BMP Image".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly description of the resource's type.</returns>
        ''' <remarks>Handles all Image types</remarks>
        Public Overrides Function GetResourceFriendlyTypeDescription(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource)

            Dim ImageFormat As ImageFormat = CType(Resource.GetValue(), Image).RawFormat
            If ImageFormat.Equals(ImageFormat.Bmp) Then
                Return SR.GetString(SR.RSE_Type_BMP)
            ElseIf ImageFormat.Equals(ImageFormat.Emf) Then
                Debug.Fail("How did we get an EMF image?")
            ElseIf ImageFormat.Equals(ImageFormat.Exif) Then
                Return SR.GetString(SR.RSE_Type_EXIF)
            ElseIf ImageFormat.Equals(ImageFormat.Gif) Then
                Return SR.GetString(SR.RSE_Type_GIF)
            ElseIf ImageFormat.Equals(ImageFormat.Jpeg) Then
                Return SR.GetString(SR.RSE_Type_JPEG)
            ElseIf ImageFormat.Equals(ImageFormat.MemoryBmp) Then
                Return SR.GetString(SR.RSE_Type_MEMBMP)
            ElseIf ImageFormat.Equals(ImageFormat.Png) Then
                Return SR.GetString(SR.RSE_Type_PNG)
            ElseIf ImageFormat.Equals(ImageFormat.Tiff) Then
                Return SR.GetString(SR.RSE_Type_TIFF)
            ElseIf ImageFormat.Equals(ImageFormat.Wmf) Then
                Debug.Fail("How did we get a WMF image?")
            Else
                Debug.Fail("Unrecognized raw image format of image")
            End If

            Return ""
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks>Handles all Image types</remarks>
        Public Overrides Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource)
            Dim Image As Image = DirectCast(Resource.GetValue(), Image)
            Return String.Format(SR.GetString(SR.RSE_GraphicSizeFormat, Image.Width, Image.Height))
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
            Return "Image"
        End Function


        ''' <summary>
        ''' Check whether this resource item is valid and can be added to the content file.
        '''  We don't want to add any unsupported resource item to a resource file. For example, the device project does not support some
        '''  type of resources, we should block the user to add a such resource item into a device project.
        ''' </summary>
        ''' <param name="NewResource">The IResource instance.  May not be Nothing.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="ResourceContentFile">The resource file contains the resource.</param>
        ''' <param name="Message">The message to explain why it can't be added to the project.</param>
        ''' <param name="HelpID">The help topic to explain why it can't be added to the project.</param>
        ''' <returns> The function should return true, if the resource item is valid. Otherwise, it should return False
        ''' </returns>
        ''' <remarks>We need call the function implemented by the base class before it returns true</remarks>
        Public Overrides Function IsResourceItemValid(ByVal NewResource As IResource, ByVal ResourceContentFile As IResourceContentFile, ByRef Message As String, ByRef HelpID As String) As Boolean
            ValidateResourceValue(NewResource)
            If ResourceContentFile.IsInsideDeviceProject() Then
                Dim Extension As String = String.Empty
                If NewResource.IsLink() Then
                    Extension = IO.Path.GetExtension(NewResource.LinkedFilePath)
                Else
                    Extension = GetResourceFileExtension(NewResource)
                End If

                If String.Compare(Extension, EXT_TIF, StringComparison.OrdinalIgnoreCase) = 0 OrElse _
                    String.Compare(Extension, EXT_TIFF, StringComparison.OrdinalIgnoreCase) = 0 Then

                    Message = SR.GetString(SR.RSE_Err_CantAddFileToDeviceProject_1Arg, Extension)
                    HelpID = HelpIDs.Err_CantAddFileToDeviceProject
                    Return False
                End If
            End If
            Return MyBase.IsResourceItemValid(NewResource, ResourceContentFile, Message, HelpID)
        End Function


    End Class
End Namespace
