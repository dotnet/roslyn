Option Explicit On
Option Strict On
Option Compare Binary

Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A base class for resource type editors that support strings, or more specifically,
    '''   which should have their resources displayed in a string table.
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class ResourceTypeEditorStringBase
        Inherits ResourceTypeEditorInternalBase



        '======================================================================
        '= PROPERTIES =                                                       =
        '======================================================================



        ''' <summary>
        ''' Returns whether this resource type should be displayed in a string table or not.
        ''' </summary>
        ''' <value>True if this resources handled by this ResourceTypeEditor should be displayed
        '''   in a string table, and False if they should be displayed in a listview.</value>
        ''' <remarks></remarks>
        Public NotOverridable Overrides ReadOnly Property DisplayInStringTable() As Boolean
            Get
                Return True
            End Get
        End Property


        ''' <summary>
        ''' Gets whether or not the strings handled by this resource type editor
        '''   are allowed to be edited by the user.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property StringValueCanBeEdited() As Boolean



        ''' <summary>
        ''' Indicates whether the resources edited by this resource editor type are allowed to have their
        '''   Persistence property changed.
        ''' </summary>
        ''' <param name="ResourceContentFile">The resource file that contains the resource</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides ReadOnly Property CanChangePersistenceProperty(ResourceContentFile As ResourceTypeEditor.IResourceContentFile) As Boolean
            Get
                Return False
            End Get
        End Property


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
            Return GetType(String).AssemblyQualifiedName
        End Function


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
        Public MustOverride Function StringGetFormattedCellValue(ByVal Resource As Resource, ByVal ResourceValue As Object) As String


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
        Public Overridable Function StringParseFormattedCellValue(ByVal Resource As Resource, ByVal FormattedValue As String) As Object
            Debug.Fail("StringParseFormattedCellValue(): This function should not have been called, or else it was not overridden when it should have been")
            Return Resource.TryGetValue() 'defensive
        End Function

    End Class

End Namespace
