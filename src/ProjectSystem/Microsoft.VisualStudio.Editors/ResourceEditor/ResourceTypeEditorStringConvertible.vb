Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Package
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A resource type editor that handles any resource value that we don't handle more specifically
    '''   by another resource type editor, but that is convertible from/to a string.  It allows the
    '''   user to edit the resource value as a string.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditorStringConvertible
        Inherits ResourceTypeEditorStringBase


        '======================================================================
        '= PROPERTIES =                                                       =
        '======================================================================



        ''' <summary>
        ''' Gets whether or not the strings handled by this resource type editor
        '''   are allowed to be edited by the user.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property StringValueCanBeEdited() As Boolean
            Get
                Return True
            End Get
        End Property




        '======================================================================
        '= METHODS =                                                          =
        '======================================================================




        ''' <summary>
        ''' Given a resource, returns a string formatted to display the value of the resource.  This is the inverse
        '''   of StringParseFormattedCellValue().
        ''' </summary>
        ''' <param name="Resource">The Resource instance for which this conversion is being made.</param>
        ''' <param name="ResourceValue">The resource value.  May not be Nothing.  The value of the resource to save.  Must be of the type handled by this ResourceTypeEditor.</param>
        ''' <returns>The formatted string.</returns>
        ''' <remarks>
        '''   Caller is responsible for displaying exceptions thrown by this method.
        '''   This function is not exposed publicly, because the class it is defined on is not public.
        ''' </remarks>
        Public Overrides Function StringGetFormattedCellValue(ByVal Resource As Resource, ByVal ResourceValue As Object) As String
            ValidateResourceValue(Resource)
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be nothing")
                Throw New InternalException
            End If

            Debug.Assert(Resource.IsConvertibleFromToString)
            Dim Converter As TypeConverter = Resource.GetTypeConverter
            If Converter Is Nothing Then
                Debug.Fail("ResourceTypeEditorStringConvertible.StringGetFormattedCellValue(): value should have been convertible to string")
                Throw New InternalException
            Else
                'This may throw an exception.  The caller is responsible for catching it.
                Return Converter.ConvertToString(ResourceValue)
            End If
        End Function


        ''' <summary>
        ''' Given a string, parses that string and converts it into a resource value.  This is the inverse
        '''   of StringGetFormatted().
        ''' </summary>
        ''' <param name="Resource">The Resource instance for which this conversion is being made.</param>
        ''' <param name="FormattedValue"></param>
        ''' <returns>The parsed resource value.</returns>
        ''' <remarks>
        '''   Caller is responsible for displaying exceptions thrown by this method.
        '''   This function only needs to be implemented if StringValueCanBeEdited returns True for the class.
        '''   This function is not exposed publicly, because the class it is defined on is not public.
        ''' </remarks>
        Public Overrides Function StringParseFormattedCellValue(ByVal Resource As Resource, ByVal FormattedValue As String) As Object
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be nothing")
                Throw New InternalException
            End If

            Dim Converter As TypeConverter = Resource.GetTypeConverter
            If Converter Is Nothing Then
                Debug.Fail("ResourceTypeEditorStringConvertible.StringParseFormattedCellValue(): value should have been convertible to string")
                Throw New InternalException
            Else
                'This may throw an exception.  The caller is responsible for catching it.
                Return Converter.ConvertFromString(FormattedValue)
            End If
        End Function

    End Class

End Namespace
