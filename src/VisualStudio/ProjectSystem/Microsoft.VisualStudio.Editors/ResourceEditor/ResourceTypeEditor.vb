'******************************************************************************
'* ResourceTypeEditor.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualStudio.Editors.Common.Utils


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This class is the base class for an extensible mechanism of displaying resources in the resource editor.
    '''   Although this extensibility mechanism is not currently exposed (it can be simply by marking this
    '''   class as Public), the architecture was designed to handle this easily if we decided to publicly
    '''   expose it in a future version.
    ''' </summary>
    ''' <remarks></remarks>
    <System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name:="FullTrust"),
    System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name:="FullTrust")>
    Friend MustInherit Class ResourceTypeEditor

        'Note: These comments aren't in the XML docs because I don't want it to accidentally end up in the public XML
        '   documentation for this class if it is ever made public.


        ' If the resource editor decides to support a pluggable architecture, new resource type editors can be 
        '   written and registered for use with new serializable types intended for use inside a .resx file.  
        '   The pluggable editor author would subclass ResourceTypeEditor.  This is similar in concept to 
        '   UITypeEditor which Windows Forms uses to allow class authors to create custom editors for use 
        '   in the property grid.  
        '   
        '   BrianPe suggests that if we make it public, ResourceTypeEditor should be placed in a separate DLL 
        '   from Microsoft.VisualStudio.Editors.dll (where the resource editor itself lives), to provide maximum 
        '   future flexibility (perhaps we want the resource editor to be hostable from Loc Studio, for example), 
        '   and also so that assemblies which contain custom editors only have to reference the DLL with 
        '   ResourceTypeEditor, and not the larger and more arbitrary Microsoft.VisualStudio.Editors.dll.  
        '   (Note that only the custom editor assembly actually has to reference ResourceTypeEditor – the 
        '   class which the editor edits can specify type names rather than actual types and thus does not 
        '   need a reference either to the ResourceTypeEditor assembly or to the assembly with its custom 
        '   editor.)  We could either create a new DLL or look for an existing DLL with a similar function of 
        '   providing public classes for VS-related functionality.  It is also possible, if ResourceTypeEditor 
        '   is generic enough, to have it added to a frameworks class, but Brian recommends against it if 
        '   possible because of the work and scrutiny involved.
        '   
        '   To associate a custom resource type editor to a class which may live inside a .resx file, the 
        '   author uses System.ComponentModel.EditorAttribute, e.g.:
        '   
        '       <Editor(GetType(MyFunkyResourceCustomResourceEditor), GetType(ResourceTypeEditor)), _
        '         Serializable()> _
        '       Public Class MyFunkyResource
        '   	…
        '       End Class
        '   
        '   This associates the MyFunkyResourceCustomResourceEditor custom editor with the .resx-persistable 
        '   class MyFunkyResource.  The resource editor uses TypeDescriptor.GetEditor() to find the editor 
        '   associated with each resource instance in the .resx file.
        '   
        '   To ensure our pluggable architecture is working, the native types that are supported in the 
        '   resource editor (bitmap, icon, etc.) are all implemented using this same architecture.  We use 
        '   TypeDescriptor.AddEditorTable in ResourceTypeEditor’s Shared Sub New to associate our intrinsic 
        '   type editors with their corresponding types in the frameworks classes (since we own editors for 
        '   classes that we do not own, such as Bitmap and therefore can’t place an attribute directly on 
        '   the class).  Note that it is not possible for 3rd parties to use TypeDescriptor.AddEditorTable.  
        '   Since a custom type editor is associated with that resource class that it edits by placing an 
        '   attribute on the resource class, in general a custom resource type editor must be created by the 
        '   author of the resource class.  However, BrianPe tells me that in Whidbey it will be possible to 
        '   get around this limitation through a TypeDescriptionProvider which would allow a 3rd party to
        '   alter the type descriptor on a class in another assembly.
        '   
        '   When we open a .resx file, we find all custom editors for all types in that .resx (the resource 
        '   type editor for a particular resource is cached in ResXResource), and use them to add to the 
        '   list of resource categories.  However, if there are other resource type editors available but 
        '   there is no actual instance of that type in the .resx file, we would not know to add them to 
        '   the categories list.  To make it possible for programmers to create new resources of such 
        '   custom types, we would need to allow the user to specify new types to add to the categories 
        '   list.  BrianPe has suggested we create a dialog which searches through the installed SDK’s 
        '   searching for public types with custom resource type editors.  (Note: He says we can steal 
        '   similar code from the toolbox code and modify it for our use probably within a couple of 
        '   days.)  Once a type has been added to the category list, it would persist through multiple 
        '   sessions of using the VS shell until it is explicitly removed.  Once the new type is installed 
        '   in the category list, it would be possible to create a new instance of that resource type in 
        '   the .resx file using the editor.  The advantage of the dialog is that 3rd parties aren’t 
        '   required to register themselves somehow.



        Private m_HashCodeCache As Integer
        Private m_IsHashCodeCached As Boolean


        ''' <summary>
        ''' Pre-defined values for use with GetExtensionPriority
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum ExtensionPriorities
            NotHandled = 0 'Extension is not handled by this resource type editor
            Lowest = 1     'Lowest priority (used by binary files for *.*)
            Low = 50       'Low priority used by built-in resource type editors
            Normal = 100   'Normal priority used by built-in resource type editors
        End Enum




        ''' <summary>
        ''' This gives a very simple view of a resource inside the resource editor.  It is used only for communicating
        '''   with resource type editors.  The type editors can query for the current value of the resource, whether
        '''   it's a link (and to what file), etc.  If the type editor doesn't need to know the current value for
        '''   an operation, that's a performance gain.
        ''' </summary>
        ''' <remarks></remarks>
        Public Interface IResource

            ''' <summary>
            ''' Attempts to get the type of the given resource.
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Function GetValueType() As Type

            ''' <summary>
            ''' Attempts to get the current value of the resource.
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Function GetValue() As Object

            ''' <summary>
            ''' Returns whether or not the resource is a link to a file (True) or a non-linked value (False).
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            ReadOnly Property IsLink() As Boolean

            ''' <summary>
            ''' If IsLinked = True, returns the absolute path and filename to the linked file.  Otherwise, returns Nothing.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            ReadOnly Property LinkedFilePath() As String

        End Interface


        ''' <summary>
        ''' This gives a very simple view of a resource file containing resources. 
        ''' </summary>
        ''' <remarks></remarks>
        Public Interface IResourceContentFile

            ''' <summary>
            ''' Returns whether the resource file is inside a device project
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            ReadOnly Property IsInsideDeviceProject() As Boolean

            ''' <summary>
            ''' Returns whether the provided type is supported in the project containing this resource file
            ''' </summary>
            Function IsSupportedType(Type As Type) As Boolean

        End Interface


        '======================================================================
        '= Constructors =                                                     =
        '======================================================================



        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
        End Sub


        ''' <summary>
        ''' Shared constructor.  This construct sets up an association between the types of resources
        '''   that the resource editor handles internally and the resource type editor classes that
        '''   handle them.
        ''' If we make this class public, a third party would associate his resource type editor with
        '''   the class that it handles by using the Editor() attribute on that class.  Since the
        '''   resource editor handles resource types that it does not own (e.g. Bitmap), this method
        '''   is not possible.
        ''' </summary>
        ''' <remarks></remarks>
        Shared Sub New()
            Dim IntrinsicEditors As New Hashtable
            Const ThisAssemblyRef As String = "Microsoft.VisualStudio.Editors, Version=" & ThisAssembly.Version _
                & ", Culture=neutral, PublicKeyToken=" + AssemblyRef.MicrosoftPublicKey

            ' Our set of intrinsic editors for resource type editors.
            IntrinsicEditors(GetType(Bitmap)) = GetType(ResourceTypeEditorBitmap).FullName & ", " _
                & ThisAssemblyRef
            IntrinsicEditors(GetType(Icon)) = GetType(ResourceTypeEditorIcon).FullName & ", " _
                & ThisAssemblyRef
            IntrinsicEditors(GetType(String)) = GetType(ResourceTypeEditorString).FullName & ", " _
                & ThisAssemblyRef

            ' Add our intrinsic editors to TypeDescriptor.
            '
            TypeDescriptor.AddEditorTable(GetType(ResourceTypeEditor), IntrinsicEditors)
        End Sub



        '======================================================================
        '= PROPERTIES =                                                       =
        '======================================================================


        ''' <summary>
        ''' Returns whether this resource type should be displayed in a string table or not.
        ''' </summary>
        ''' <value>True if this resources handled by this ResourceTypeEditor should be displayed
        '''   in a string table, and False if they should be displayed in a listview.</value>
        ''' <remarks></remarks>
        Public Overridable ReadOnly Property DisplayInStringTable() As Boolean
            Get
                Return False
            End Get
        End Property


        ''' <summary>
        '''  Whether all valid items share a same image
        ''' </summary>
        ''' <returns>true or false</returns>
        ''' <remarks>
        '''   If true, the GetImageForThumbnail property must return a same image object. We won't keep duplicated items in the cache to improve the performance of the designer.
        ''' </remarks>
        Public Overridable ReadOnly Property IsImageForThumbnailShared() As Boolean
            Get
                Return False
            End Get
        End Property




        '======================================================================
        '= METHODS =                                                          =
        '======================================================================



        ''' <summary>
        ''' Determines if two instances of ResourceTypeEditor are of the same type.  Two
        '''   instances of this class are considered equal if they are of the exact same
        '''   type.
        ''' </summary>
        ''' <param name="Object"></param>
        ''' <returns>True if and only if both instances are of the same type.</returns>
        ''' <remarks>Overloads Object.Equals to provide value semantics for equality.</remarks>
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public NotOverridable Overrides Function Equals(ByVal [Object] As Object) As Boolean
            Return [Object].GetType Is Me.GetType
        End Function


        ''' <summary>
        ''' Determines if two instances of ResourceTypeEditor are of the same type.  Two
        '''   instances of this class are considered equal if they are of the exact same
        '''   type.
        ''' </summary>
        ''' <param name="Editor"></param>
        ''' <returns>True if and only if both instances are of the same type.</returns>
        ''' <remarks>Overloads Object.Equals to provide value semantics for equality.</remarks>
        Public Overloads Function Equals(ByVal Editor As ResourceTypeEditor) As Boolean
            Return Me.Equals(CObj(Editor))
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
        Public MustOverride Function GetDefaultResourceTypeName(ByVal ResourceContentFile As IResourceContentFile) As String

        ''' <summary>
        ''' Returns a hash code for this resource type editor.  The algorithm is such that 
        '''   if A.Equals(B), then A and B will return the same hash code.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>This is overridden from Object's version of GetHashCode(), which is based on
        '''   referential equality, to equality based simply on the objects being of the same type.
        '''   This override is necessary in order to get proper behavior when using ResourceTypeEditor
        '''   instances as keys in a hash table (using value semantics for key look-up).
        ''' </remarks>
        Public NotOverridable Overrides Function GetHashCode() As Integer
            'Hash is based on the hash of the type name of this ResourceTypeEditor.  This can be
            '  somewhat expensive, so we cache it (it cannot change).
            If Not m_IsHashCodeCached Then
                m_IsHashCodeCached = True
                m_HashCodeCache = Me.GetType.AssemblyQualifiedName.GetHashCode
            End If
            Return m_HashCodeCache
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
        Public Overridable Function LoadResourceFromFile(ByVal FilePath As String, ByVal ResourceContentFile As IResourceContentFile) As Object
            'First, try to get a read lock on the file.  To do that, we open a dummy stream
            '  on the file in read mode.
            Dim DummyStream As FileStream = Nothing
            Try
                DummyStream = File.OpenRead(FilePath)

                'Create a ResXFileRef with the file path and resource type
                Dim DummyFileRef As New System.Resources.ResXFileRef(FilePath, GetDefaultResourceTypeName(ResourceContentFile))
                Dim Converter As TypeConverter = TypeDescriptor.GetConverter(DummyFileRef)
                If Converter.CanConvertFrom(GetType(String)) Then
                    '... and then use it to fetch the resource from the file
                    Dim NewValue As Object = Converter.ConvertFromInvariantString(DummyFileRef.ToString)
                    If NewValue IsNot Nothing Then
                        Return NewValue
                    Else
                        Debug.Fail("ResXFileRef conversion returned Nothing - should have thrown an exception instead")
                        Throw NewException(SR.GetString(SR.RSE_Err_LoadingResource_1Arg, FilePath), HelpIDs.Err_LoadingResource)
                    End If
                Else
                    Debug.Fail("ResXFileRef can't convert from string?")
                    Throw NewException(SR.GetString(SR.RSE_Err_LoadingResource_1Arg, FilePath), HelpIDs.Err_LoadingResource)
                End If
            Catch ex As Reflection.TargetInvocationException
                'Pull out the inner exception and rethrow that - the target invocation exception doesn't give us
                '  any new information (it's because of the Activator.CreateInstance call that happens in the
                '  ResXFileRef).
                Throw ex.InnerException
            Finally
                If DummyStream IsNot Nothing Then
                    DummyStream.Close()
                End If
            End Try
        End Function


        ''' <summary>
        ''' Determines whether a specific ResourceValue can be saved by this ResourceTypeEditor into 
        '''   a file or not.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>True iff the resource type editor supports saving the specific resource value to a file.</returns>
        ''' <remarks></remarks>
        Public Overridable Function CanSaveResourceToFile(ByVal Resource As IResource) As Boolean
            Return False
        End Function


        ''' <summary>
        ''' Saves a given resource value to a file.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="FilePath">File path and name of the file to save to.</param>
        ''' <remarks>Caller is responsible for handling exceptions raised by this method.
        ''' If the file already exists, it should be overwritten.''' </remarks>
        Public Overridable Sub SaveResourceToFile(ByVal Resource As IResource, ByVal FilePath As String)
            Throw New NotImplementedException
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
        Public Overridable Sub CreateNewResourceFile(ByVal FilePath As String)
            Throw New NotImplementedException
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
        Public Overridable Function GetResourceFileExtension(ByVal Resource As IResource) As String
            Return Nothing
        End Function

        ''' <summary>
        '''  Returns a list of extensions. If the file matched one of them, it should be safe to open the file in VS. Otherwise, it could
        '''  be a harmful operation to open the file if it comes from someone else. For example, the resource file could link to a file containing script.
        '''  The resource designer could open the file in an external editor, which could run the script, and do harmful things on the dev machine.
        ''' </summary>
        ''' <returns> a file extension list
        ''' </returns>
        Public Overridable Function GetSafeFileExtensionList() As String()
            Return Nothing
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
        Public Overridable Function GetOpenFileDialogFilter(ByVal ResourceContentFile As IResourceContentFile) As String
            Return ""
        End Function


        ''' <summary>
        ''' Gets a filter string for use with a file save dialog.  That filter should contain all commonly-supported
        '''   extensions handled by this resource type editor (but does not have to necessarily include all of
        '''   them).
        ''' </summary>
        ''' <param name="Extension">The file extension being used for saving the resource.</param>
        ''' <returns>The filter string.</returns>
        ''' <remarks>
        ''' An example filter string:
        '''   
        '''   "Windows metafile (*.wmf, *.emf)|*.wmf;*.emf"
        ''' </remarks>
        Public Overridable Function GetSaveFileDialogFilter(ByVal Extension As String) As String
            Return SR.GetString(SR.RSE_Filter_All) & " (*.*)|*.*"
        End Function


        ''' <summary>
        ''' Returns an image for displaying to the user for this resource.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <param name="background">The background color for this thumbnail. Not used for Bitmaps and Icons since we instead display the resource as a thumbnail.</param>
        ''' <returns>An image to use as the basis of creating a thumbnail for this resource</returns>
        ''' <remarks>
        ''' This bitmap will be used as
        '''   the basis for creating a thumbnail image (the image returned by this function will not be needed
        '''   after the thumbnail image is created).
        ''' Default implementation returns an empty bitmap.
        ''' </remarks>
        Public Overridable Function GetImageForThumbnail(ByVal Resource As IResource, background As Color) As Image
            Return New Bitmap(1, 1)
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
        Public Overridable Function GetExtensionPriority(ByVal Extension As String) As Integer
            Return ExtensionPriorities.NotHandled
        End Function


        ''' <summary>
        ''' Gets a friendly description to display to the user that indicates the type of a
        '''   particular resource.  E.g., "BMP Image".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly description of the resource's type.</returns>
        ''' <remarks></remarks>
        Public Overridable Function GetResourceFriendlyTypeDescription(ByVal Resource As IResource) As String
            Return ""
        End Function


        ''' <summary>
        ''' Gets a friendly size to display to the user for this particular resource.  E.g., "240 x 160".
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly size string.</returns>
        ''' <remarks></remarks>
        Public Overridable Function GetResourceFriendlySize(ByVal Resource As IResource) As String
            Return ""
        End Function



        ''' <summary>
        ''' Checks the resource's value for any errors.  If any error is found it should be indicated by throwing
        '''    an exception.  These errors will be displayed automatically by the callee in the task list.
        ''' For most resource type editors, the default implementation will be sufficient, which simply calls
        '''    IResource.GetValue() and lets any exceptions bubble up.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <remarks>
        ''' The default version of this function calls IResource.GetValue().
        ''' </remarks>
        Public Overridable Sub CheckValueForErrors(ByVal Resource As IResource)
            Call Resource.GetValue()
        End Sub


        ''' <summary>
        ''' Gets the prefix that is used for suggesting resource names to the user.  For instance,
        '''   if this function returns "id", then as the user asks to create a new resource
        '''   handled by this resource type editor, the suggested names could take the form 
        '''   of "id01", "id02", etc.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function GetSuggestedNamePrefix() As String
            'By default, this is simply "id"
            Return "id"
        End Function


        ''' <summary>
        ''' Attempts to call CanSaveResourceToFile, and returns False if there are any exceptions.
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function TryCanSaveResourceToFile(ByVal Resource As IResource) As Boolean
            Try
                Return CanSaveResourceToFile(Resource)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
            End Try

            Return False
        End Function


        ''' <summary>
        ''' Indicates whether the resources edited by this resource editor type are allowed to have their
        '''   Persistence property changed.
        ''' </summary>
        ''' <param name="ResourceContentFile">The resource file that contains the resource</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overridable ReadOnly Property CanChangePersistenceProperty(ResourceContentFile As IResourceContentFile) As Boolean
            Get
                Return True
            End Get
        End Property

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
        Public Overridable Function IsResourceItemValid(ByVal NewResource As IResource, ByVal ResourceContentFile As IResourceContentFile, ByRef Message As String, ByRef HelpID As String) As Boolean
            Return True
        End Function

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
        Public Overridable Function ConvertByteArrayToResourceValue(Value As Byte()) As Object
            Return Nothing
        End Function

    End Class
End Namespace
