'******************************************************************************
'* ResourceComparer.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports System.Collections
Imports System.Diagnostics
Imports System.Globalization

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is an Icomparer implementation used to sort Resources for UI purposes (ResourceListView and
    '''    ResourceStringTable).
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceComparer
        Implements IComparer

        ''' <summary>
        ''' Sorts an ArrayList of Resoures for UI purposes
        ''' </summary>
        ''' <param name="Resources">ArrayList of Resources to source (will be sorted in place)</param>
        ''' <remarks></remarks>
        Public Shared Sub SortResources(ByVal Resources As ArrayList)
            Resources.Sort(New ResourceComparer)
        End Sub

        ''' <summary>
        ''' Compares two objects and returns a value indicating whether one is less than, equal to or greater than the other.
        ''' </summary>
        ''' <param name="x">First object to compare.</param>
        ''' <param name="y">Second object to compare.</param>
        ''' <returns>-1, 0 or 1, depending on whether x is less than, equal to or greater than y, respectively.</returns>
        ''' <remarks>This function gets called by ArrayList.Sort for each pair of resources to be sorted.</remarks>
        Private Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Debug.Assert(TypeOf x Is Resource AndAlso TypeOf y Is Resource, "ResourceComparer: expected Resources")
            Dim Resource1 As Resource = DirectCast(x, Resource)
            Dim Resource2 As Resource = DirectCast(y, Resource)

            'We currently only support sorting alphabetically according to Name.
            Return String.Compare(Resource1.Name, Resource2.Name, ignoreCase:=True, culture:=CultureInfo.CurrentUICulture)
        End Function
    End Class

End Namespace
