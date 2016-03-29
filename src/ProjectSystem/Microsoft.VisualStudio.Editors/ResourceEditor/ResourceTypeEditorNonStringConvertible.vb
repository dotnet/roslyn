'******************************************************************************
'* ResourceTypeEditorNonStringConvertible.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Package
Imports System
Imports System.Diagnostics

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This resource type editor handles all values in a resx file which are not more specifically handled
    '''   by another resource type editor, and are not convertible to/from a string.  The display is simply
    '''   to show the type of the resource, and to display a message telling the user that the value cannot
    '''   be edited.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditorNonStringConvertible
        Inherits ResourceTypeEditorStringBase




        '======================================================================
        '= PROPERTIES =                                                       =
        '======================================================================



        ''' <summary>
        ''' Returns whether this resource type should be displayed in a string table or not.
        ''' </summary>
        ''' <value>True if this resources handled by this ResourceTypeEditor should be displayed
        '''   in a string table, and False if they should be displayed in a listview.</value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property StringValueCanBeEdited() As Boolean
            Get
                'We don't understand this resource value, so we don't know how to let the user edit it.
                Return False
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
            ValidateResourceValue(Resource)

            'Show a message indicating the value can't be edited.
            Return SR.GetString(SR.RSE_NonEditableValue)
        End Function

    End Class
End Namespace
