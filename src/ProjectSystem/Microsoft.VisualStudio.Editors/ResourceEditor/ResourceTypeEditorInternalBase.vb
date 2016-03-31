Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Package
Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a base class used by all resource type editors internal to the resource editor.
    '''   It simply provides a few helpful functions.
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class ResourceTypeEditorInternalBase
        Inherits ResourceTypeEditor


        ''' <summary>
        ''' Given a list of extensions, returns true if a given extension is among them (case-insensitive)
        ''' </summary>
        ''' <param name="Extension">The extension to search for.</param>
        ''' <param name="Extensions">The list of extensions to match against.</param>
        ''' <param name="MatchedIndex">[out optional] Set to the index of the extension which matched, or -1 if none.</param>
        ''' <returns>True if the extension is found in the list.</returns>
        ''' <remarks></remarks>
        Protected Shared Function MatchAgainstListOfExtensions(ByVal Extension As String, ByVal Extensions() As String, Optional ByRef MatchedIndex As Integer = -1) As Boolean
            Debug.Assert(Extension = "" OrElse Extension.Chars(0) = "."c, "HandlesExtension: must start extension with dot")
            MatchedIndex = -1

            Dim Index As Integer = 0
            For Each CheckExtension As String In Extensions
                If String.Equals(Extension, CheckExtension, StringComparison.OrdinalIgnoreCase) Then
                    MatchedIndex = Index
                    Return True
                End If
                Index += 1
            Next

            Return False
        End Function


        ''' <summary>
        ''' Given a single filter text and a set of extensions, creates a single filter entry
        '''   for a file dialog.
        ''' </summary>
        ''' <param name="FilterText">The text for that filter entry, e.g. "Metafiles"</param>
        ''' <param name="Extensions">An array of extensions supported, e.g. {".wmf", ".emf")</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Shared Function CreateSingleDialogFilter(ByVal FilterText As String, ByVal Extensions() As String) As String
            Return Common.Utils.CreateDialogFilter(FilterText, Extensions)
        End Function


        ''' <summary>
        ''' Given a resource id from the manifest, saves the data in that resource into a file.
        ''' </summary>
        ''' <param name="ManifestResourceId">ID of the resource in the manifest, *not* including the namespace.</param>
        ''' <param name="FilePath">Path and name of the file to save to.</param>
        ''' <remarks>Caller responsible for handling exceptions.</remarks>
        Protected Shared Sub SaveFileFromManifestResource(ByVal ManifestResourceId As String, ByVal FilePath As String)
            Debug.Assert(ManifestResourceId <> "")
            Dim DataStream As Stream = GetType(ResourceEditorView).Assembly.GetManifestResourceStream(GetType(ResourceEditorView), ManifestResourceId)
            If Not DataStream Is Nothing Then
                Dim FileStream As New FileStream(FilePath, FileMode.Create)
                Try
                    Debug.Assert(DataStream.Length < Integer.MaxValue)
                    Dim Data(CInt(DataStream.Length) - 1) As Byte
                    DataStream.Read(Data, 0, CInt(DataStream.Length))
                    FileStream.Write(Data, 0, Data.Length)
                Finally
                    FileStream.Close()
                End Try
            Else
                Debug.Fail("Unable to find resource from manifest: " & ManifestResourceId)
                Throw New Package.InternalException(SR.GetString(SR.RSE_Err_Unexpected_NoResource_1Arg))
            End If
        End Sub


        ''' <summary>
        ''' Handles GetResourceFriendlySize for linked resources (for which returning the size of the file on disk
        '''   is appropriate).
        ''' </summary>
        ''' <param name="Resource">The IResource instance.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The friendly file size as string.</returns>
        ''' <remarks></remarks>
        Protected Shared Function GetLinkedResourceFriendlySize(ByVal Resource As IResource) As String
            If Resource.IsLink Then
                Dim SizeInBytes As Long = New FileInfo(Resource.LinkedFilePath).Length
                Return GetKBDisplay(SizeInBytes)
            Else
                Return ""
            End If
        End Function

        ''' <summary>
        ''' Verifies the ResourceValue argument.
        ''' </summary>
        ''' <param name="Resource">The IResource instance to validate</param>
        ''' <param name="ExpectedTypes">The type(s) which the resource is expected to be (optional).</param>
        ''' <remarks>Verifies that ResourceValue is not Nothing, and that it is of type ExpectedType.</remarks>
        Protected Shared Sub ValidateResourceValue(ByVal Resource As IResource, ByVal ParamArray ExpectedTypes() As System.Type)
            If Resource Is Nothing Then
                Debug.Fail("Resource should not be nothing")
                Throw New InternalException
            End If

#If DEBUG Then
            Try
                Dim ResourceValueType As Type = Resource.GetValueType() 'This shouldn't fail for anything not in the others category
                If ExpectedTypes IsNot Nothing AndAlso ExpectedTypes.Length > 0 Then
                    Dim MatchedAType As Boolean = False
                    For Each ExpectedType As Type In ExpectedTypes
                        If ResourceValueType.Equals(ExpectedType) OrElse ResourceValueType.IsSubclassOf(ExpectedType) Then
                            MatchedAType = True
                            Exit For
                        End If
                    Next

                    If Not MatchedAType Then
                        Debug.Fail("Resource value was not of the expected type(s).  Actual type found: " & Resource.GetValueType().Name)
                    End If
                End If
            Catch ex As Exception
                'Ignore exceptions here, because this is debug code and we don't want the debug behavior to end up being different from retail.
            End Try
#End If
        End Sub

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
            If ResourceContentFile.IsInsideDeviceProject Then
                Dim typeName As String = CType(NewResource, Resource).ValueTypeNameWithoutAssemblyInfo
                'CONSIDER: We should consider a general way to check whether the type is supported or not.
                If String.Equals(typeName, GetType(MemoryStream).FullName, StringComparison.Ordinal) Then
                    Message = SR.GetString(SR.RSE_Err_TypeIsNotSupported_1Arg, GetType(UnmanagedMemoryStream).Name)
                    HelpID = HelpIDs.Err_TypeIsNotSupported
                    Return False
                End If
            End If
            Return True
        End Function

    End Class
End Namespace
