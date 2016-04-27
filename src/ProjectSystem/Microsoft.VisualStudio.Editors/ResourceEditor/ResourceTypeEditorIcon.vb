' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Drawing
Imports System.IO

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A resource type editor that handles icons.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditorIcon
        Inherits ResourceTypeEditorInternalBase

        'File extension constants (should be treated as non-case-sensitive)
        Public Const EXT_ICO As String = ".ico"

        'All common file extensions handled by this resource type editor
        Private _extensions() As String = {EXT_ICO}



        '======================================================================
        '= METHODS =                                                          =
        '======================================================================

        Private Overloads Sub ValidateResourceValue(Resource As IResource)
            ValidateResourceValue(Resource, GetType(Icon), GetType(Byte()))
        End Sub

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
            If ResourceContentFile.IsSupportedType(GetType(Icon)) Then
                Return GetType(Icon).AssemblyQualifiedName
            Else
                Return GetType(Byte()).AssemblyQualifiedName
            End If
        End Function



        ''' <summary>
        ''' Determines whether a specific ResourceValue can be saved by this ResourceTypeEditor into 
        '''   a file or not.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>True iff the resource type editor supports saving the specific resource value to a file.</returns>
        ''' <remarks></remarks>
        Public Overrides Function CanSaveResourceToFile(ByVal Resource As IResource) As Boolean
            ValidateResourceValue(Resource)
            Return True
        End Function




        ''' <summary>
        ''' Saves a given resource value to a file.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="FilePath">File path and name of the file to save to.</param>
        ''' <remarks>Caller is responsible for handling exceptions raised by this method.</remarks>
        Public Overrides Sub SaveResourceToFile(ByVal Resource As IResource, ByVal FilePath As String)
            ValidateResourceValue(Resource)
            Dim Icon As Icon = DirectCast(Resource.GetValue(), Icon)
            Dim IconStream As New MemoryStream
            Icon.Save(IconStream)

            Dim FileStream As New FileStream(FilePath, FileMode.Create)
            Try
                If IconStream.Length > Integer.MaxValue Then
                    Throw New OutOfMemoryException
                End If
                FileStream.Write(IconStream.ToArray(), 0, CInt(IconStream.Length))
            Finally
                FileStream.Close()
            End Try
        End Sub


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
            SaveFileFromManifestResource("BlankIcon", FilePath)
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
            ValidateResourceValue(Resource)
            Return EXT_ICO
        End Function

        ''' <summary>
        '''  Returns a list of extensions. If the file matched one of them, it should be safe to open the file in VS. Otherwise, it could
        '''  be a harmful operation to open the file if it comes from someone else. For example, the resource file could link to a file containing script.
        '''  The resource designer could open the file in an external editor, which could run the script, and do harmful things on the dev machine.
        ''' </summary>
        ''' <returns> a file extension list
        ''' </returns>
        Public Overrides Function GetSafeFileExtensionList() As String()
            Return _extensions
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
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Icon), _extensions)
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
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_FilterSave_Icon), New String() {EXT_ICO})
        End Function


        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail. Not used since we display the actual resource as an icon.</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' This bitmap will be used as
        '''   the basis for creating a thumbnail image (the image returned by this function will not be needed
        '''   after the thumbnail image is created).
        ''' Default implementation returns an empty bitmap.
        ''' </remarks>
        Public Overrides Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            'Convert the icon to a bitmap
            ValidateResourceValue(Resource)
            Dim Icon As Icon = DirectCast(Resource.GetValue(), Icon)
            Return Icon.ToBitmap()
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
            If MatchAgainstListOfExtensions(Extension, _extensions) Then
                Return ExtensionPriorities.Normal
            Else
                Return ExtensionPriorities.NotHandled
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
            ValidateResourceValue(Resource)
            Return SR.GetString(SR.RSE_Type_Icon)
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource)
            Dim Icon As Icon = DirectCast(Resource.GetValue(), Icon)
            Return String.Format(SR.GetString(SR.RSE_GraphicSizeFormat, Icon.Width, Icon.Height))
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
            Return "Icon"
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
                ' We don't support changing the persistence type unless Icon is supported because embedding
                ' a resource as a byte array is ambiguous in scenarios where the resource designer reads
                ' the resource back from the file.
                Return ResourceContentFile.IsSupportedType(GetType(Icon))
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
                Return New Icon(MemoryStream)
            End Using
        End Function

    End Class
End Namespace
