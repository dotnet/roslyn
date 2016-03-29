'******************************************************************************
'* CategoryCollection.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Strict On
Option Explicit On

Imports Microsoft.VisualStudio.Editors.ResourceEditor
Imports System
Imports System.Collections
Imports System.Diagnostics
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A strongly-typed collection of Category, allowing indexing by both
    '''   index and string (programmatic name as key)
    ''' </summary>
    ''' <remarks>Inherits from CollectionBase, from which it inherits an ArrayList "InnerList"
    '''   indexable by integer</remarks>
    Friend NotInheritable Class CategoryCollection
        Inherits CollectionBase



        'A hashtable list of resources by name.
        Private m_InnerHashByName As New Hashtable 'Of String (case-sensitive)




        '======================================================================
        '= Properties =                                                       =
        '======================================================================




        ''' <summary>
        ''' Searches for a category by its index
        ''' </summary>
        ''' <param name="Index">The integer index to look up</param>
        ''' <value></value>
        ''' <remarks>Throws an exception if out of bounds.</remarks>
        Default Public ReadOnly Property Item(ByVal Index As Integer) As Category
            Get
                Return DirectCast(MyBase.InnerList(Index), Category)
            End Get
        End Property


        ''' <summary>
        ''' Searches for a category by its programmatic name.
        ''' </summary>
        ''' <param name="ProgrammaticCategoryName">Category name to search for.</param>
        ''' <value>The category if found, or else Nothing.</value>
        ''' <remarks>Does not throw an exception if not found.</remarks>
        Default Public ReadOnly Property Item(ByVal ProgrammaticCategoryName As String) As Category
            Get
                Return DirectCast(m_InnerHashByName(ProgrammaticCategoryName), Category)
            End Get
        End Property




        '======================================================================
        '= Methods =                                                          =
        '======================================================================




        ''' <summary>
        ''' Adds a category to the collection.
        ''' </summary>
        ''' <param name="Category">The category to add.</param>
        ''' <remarks></remarks>
        Public Sub Add(ByVal Category As Category)
            'Add to the inner list (indexable by integer index)
            MyBase.InnerList.Add(Category)

            'Add to the hash table (for indexing by programmatic name)
            m_InnerHashByName.Add(Category.ProgrammaticName, Category)
        End Sub


    End Class


End Namespace
