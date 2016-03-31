Option Explicit On
Option Strict On
Option Compare Binary

Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Imaging.Interop

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class ResourceTypeEditorTextFile
        Inherits ResourceTypeEditorFileBase

        'The resource value type that is used for text files
        Friend Shared ReadOnly TextFileValueType As System.Type = GetType(String)

        'All common file extensions handled by this resource type editor.
        '  This is just a suggested list of files likely to be intended as text files.
        '  If the user doesn't like these showing up as text files, he can always change
        '  it to a binary file from the properties window.
        Public Const EXT_TXT As String = ".txt"
        Private SafeExtensions() As String = {
            EXT_TXT,
            ".text",
            ".rtf",
            ".xml"
        }
        Private AllExtensions() As String = {
            ".asa",
            ".asax",
            ".ascx",
            ".asm",
            ".asmx",
            ".asp",
            ".aspx",
            ".bas",
            ".bat",
            ".c",
            ".cc",
            ".cmd",
            ".cod",
            ".cpp",
            ".cls",
            ".config",
            ".cs",
            ".csproj",
            ".css",
            ".csv",
            ".ctl",
            ".cxx",
            ".dbs",
            ".def",
            ".dic",
            ".disco",
            ".diz",
            ".dob",
            ".dsp",
            ".dsr",
            ".dsw",
            ".dtd",
            ".etp",
            ".ext",
            ".fky",
            ".frm",
            ".h",
            ".hpp",
            ".hta",
            ".htc",
            ".htm",
            ".html",
            ".htt",
            ".hxx",
            ".i",
            ".idl",
            ".inc",
            ".inf",
            ".ini",
            ".inl",
            ".java",
            ".js",
            ".jsl",
            ".kci",
            ".lgn",
            ".log",
            ".lst",
            ".mak",
            ".map",
            ".me",
            ".mk",
            ".nvr",
            ".odh",
            ".odl",
            ".org",
            ".pag",
            ".pl",
            ".plg",
            ".prc",
            ".rc",
            ".rc2",
            ".rct",
            ".readme",
            ".reg",
            ".resx",
            ".rgs",
            ".rul",
            ".rtf",
            ".s",
            ".sct",
            ".settings",
            ".shtm",
            ".shtml",
            ".sql",
            ".srf",
            ".stm",
            ".tab",
            ".tdl",
            ".text",
            ".tlh",
            ".tli",
            ".trg",
            EXT_TXT,
            ".udf",
            ".user",
            ".usr",
            ".vap",
            ".vb",
            ".vbproj",
            ".vbs",
            ".vbsnippet",
            ".vcproj",
            ".vspscc",
            ".vsscc",
            ".vssdb",
            ".vssscc",
            ".viw",
            ".vjp",
            ".vjsproj",
            ".vsdisco",
            ".vssetings",
            ".wsf",
            ".wtx",
            ".xdr",
            ".xaml",
            ".xml",
            ".xmlproj",
            ".wsc",
            ".xsd",
            ".xsl",
            ".xslt"
        }

        'The shared Thumbnail Image
        Private Shared m_ThumbnailForTextFile As Image

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
            Return TextFileValueType.AssemblyQualifiedName
        End Function



        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' For files, we have exactly two thumbnails - one for text files, one for binary files
        ''' </remarks>
        Public Overrides Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            ValidateResourceValue(Resource, TextFileValueType)
            If m_ThumbnailForTextFile Is Nothing Then
                m_ThumbnailForTextFile = Common.GetImageFromImageService(KnownMonikers.TextFile, 48, 48, background)
            End If
            Return m_ThumbnailForTextFile
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
            If MatchAgainstListOfExtensions(Extension, AllExtensions) Then
                Return ExtensionPriorities.Low
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
            ValidateResourceValue(Resource, TextFileValueType)
            Debug.Assert(Resource.IsLink)
            Return SR.GetString(SR.RSE_Type_TextFile)
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            ValidateResourceValue(Resource, TextFileValueType)
            Debug.Assert(Resource.IsLink)
            Debug.Assert(Resource.GetValueType.Equals(TextFileValueType))
            Debug.Assert(TextFileValueType.Equals(GetType(String)), "Type of text files has changed?")

            Return GetLinkedResourceFriendlySize(Resource)
        End Function


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
            ValidateResourceValue(Resource, TextFileValueType)
            Return EXT_TXT
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
            'Too many text file extensions to show them all.  Just use *.txt
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Text), New String() {EXT_TXT})
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
            'Too many text file extensions to show them all.  Just use *.txt
            Return CreateSingleDialogFilter(SR.GetString(SR.RSE_Filter_Text), New String() {EXT_TXT})
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
            'For text files, all we need to do is create a blank one (Ansi).
            Dim Stream As FileStream = File.Create(FilePath)
            Stream.Close()
        End Sub


        ''' <summary>
        ''' Gets the prefix that is used for suggesting resource names to the user.  For instance,
        '''   if this function returns "id", then as the user asks to create a new resource
        '''   handled by this resource type editor, the suggested names could take the form 
        '''   of "id01", "id02", etc.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function GetSuggestedNamePrefix() As String
            Return "TextFile"
        End Function


    End Class
End Namespace
