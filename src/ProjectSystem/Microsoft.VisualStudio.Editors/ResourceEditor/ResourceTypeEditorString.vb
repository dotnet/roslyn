' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Package

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A resource type editor for strings (displayed in a string table).  Does not handle
    '''   linked text files.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditorString
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
        '''   This function only needs to be implemented if StringValueCanBeEdited returns True for the class.
        '''   This function is not exposed publicly, because the class it is defined on is not public.
        ''' </remarks>
        Public Overrides Function StringGetFormattedCellValue(ByVal Resource As Resource, ByVal ResourceValue As Object) As String
            ValidateResourceValue(Resource, GetType(String))
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be nothing")
                Throw New InternalException
            End If

            'Nothing to format - we simply want to display the string's value.
            Return DirectCast(ResourceValue, String)
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

            'Nothing to parse.
            Return FormattedValue
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
            Return "String"
        End Function


    End Class

End Namespace
