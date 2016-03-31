Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Represents a category of resources (e.g., "Strings", "Audio", "Images").
    '''   These categories are displayed in buttons at the top of the editor, and
    '''   allow the user to filter the currently-displayed resources by 
    '''   a single category at a time.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class Category

        ''' <summary>
        ''' Fired when the value of the ResourcesExist property changes
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Public Event ResourcesExistChanged(ByVal sender As Object, ByVal e As EventArgs)


        ''' <summary>
        ''' Indicates whether this category displays its resources in a stringtable
        '''   or a listview.
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum Display
            StringTable 'Resources in this category displayed in a string table
            ListView    'Resources in this category displayed in a listview
        End Enum


        'Backing for public properties
        Private m_AssociatedResourceTypeEditors() As ResourceTypeEditor
        Private m_CategoryDisplay As Display
        Private ReadOnly m_LocalizedName As String
        Private ReadOnly m_ProgrammaticName As String
        Private m_ResourceCount As Integer = 0
        Private m_ResourceView As ResourceListView.ResourceView = ResourceListView.ResourceView.Thumbnail
        Private m_AllowNewEntriesInStringTable As Boolean 'applies only to Display.StringTable
        Private m_ShowTypeColumnInStringTable As Boolean 'applies only to Display.StringTable
        Private m_MenuCommand as MenuCommand
        Private m_addCommand As EventHandler
        Private m_Sorter As IComparer           ' how to sort resources in the category...


        '======================================================================
        '= Constructors =                                                     =
        '======================================================================




        ''' <summary>
        ''' Constructor for Category
        ''' </summary>
        ''' <param name="ProgrammaticName">A programmatic (non-localized) name for the category.  This name is never shown to the user, but is used for looking up a category by key.</param>
        ''' <param name="LocalizedName">A localized name for the category.  This name is the one shown to the user.</param>
        ''' <param name="CategoryDisplay">Whether this category uses a stringtable or listview.</param>
        ''' <param name="AssociatedResourceTypeEditors">A list of all resource type editors which are shown in this category (a resource type editor can only be shown in a single category).</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ProgrammaticName As String, ByVal LocalizedName As String, ByVal CategoryDisplay As Display, ByVal MenuCommand As MenuCommand, ByVal addCommand As EventHandler, ByVal ParamArray AssociatedResourceTypeEditors() As ResourceTypeEditor)
            Debug.Assert(ProgrammaticName <> "", "programmatic name must be non-empty")
            Debug.Assert(LocalizedName <> "", "localized name should be non-empty")
            Debug.Assert(Not AssociatedResourceTypeEditors Is Nothing, "Must be at least one resource type editor per category.")
            Debug.Assert(MenuCommand IsNot Nothing, "You must supply a MenuCommand")

            m_ProgrammaticName = ProgrammaticName
            m_LocalizedName = LocalizedName
            m_CategoryDisplay = CategoryDisplay
            m_ShowTypeColumnInStringTable = False
            m_MenuCommand = MenuCommand
            Me.m_addCommand = addCommand

            If AssociatedResourceTypeEditors IsNot Nothing Then
                m_AssociatedResourceTypeEditors = AssociatedResourceTypeEditors
            Else
                m_AssociatedResourceTypeEditors = New ResourceTypeEditor() {}
            End If
        End Sub




        '======================================================================
        '= Properties =                                                       =
        '======================================================================



        ''' <summary>
        ''' All resource type editors that are displayed in this category.  A particular
        '''   resource type editor may only be displayed in a single category.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property AssociatedResourceTypeEditors() As ResourceTypeEditor()
            Get
                Return m_AssociatedResourceTypeEditors
            End Get
        End Property


        ''' <summary>
        ''' Indicates whether this category displays its resources in a stringtable
        '''   or a listview.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property CategoryDisplay() As Display
            Get
                Return m_CategoryDisplay
            End Get
        End Property


        ''' <summary>
        ''' Command to execute if you want to show this category
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property CommandToShow() As MenuCommand
            Get
                Return m_MenuCommand
            End Get
        End Property

        ''' <summary>
        ''' The command used to add a new resources of appropriate type
        ''' for the current category
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property AddCommand() As EventHandler
            Get
                Return m_addCommand
            End Get
            Set(ByVal value As EventHandler)
                m_addCommand = value
            End Set
        End Property

        ''' <summary>
        ''' Returns the friendly (localized) category name, which is shown to the
        '''   user in the category buttons.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property LocalizedName() As String
            Get
                Return m_LocalizedName
            End Get
        End Property


        ''' <summary>
        ''' Returns a programmatic name which is never localized and never shown to
        '''   the user.  Used when searching for a category by name key.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ProgrammaticName() As String
            Get
                Return m_ProgrammaticName
            End Get
        End Property


        ''' <summary>
        ''' The number of resources currently in this category.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' The boldness of the category button associated with this category is automatically
        '''   updated with the resource count.  I.e., when the count is zero, the font on the
        '''   button will be normal.  When there is at least one resource in this category,
        '''   the button's font will be made bold.
        ''' </remarks>
        Public Property ResourceCount() As Integer
            Get
                Return m_ResourceCount
            End Get
            Set(ByVal Value As Integer)
                Dim ResourcesExisted As Boolean = ResourcesExist
                m_ResourceCount = Value

                If m_ResourceCount < 0 Then
                    Debug.Fail("Resources count is less than zero for category " & m_ProgrammaticName)
                    m_ResourceCount = 0
                End If

                If ResourcesExisted <> ResourcesExist Then
                    RaiseEvent ResourcesExistChanged(Me, New EventArgs)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Returns true iff there are resources in this category (ResourceCount > 0)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ResourcesExist() As Boolean
            Get
                Return m_ResourceCount > 0
            End Get
        End Property


        ''' <summary>
        ''' The current ResourceView for this category, if this category uses
        '''   a listview.  This is the "Details", "Icons", "List" option for
        '''   the display mode of the listview.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ResourceView() As ResourceListView.ResourceView
            Get
                Return m_ResourceView
            End Get
            Set(ByVal Value As ResourceListView.ResourceView)
                m_ResourceView = Value
            End Set
        End Property


        ''' <summary>
        ''' If this category uses a string table, indicates whether or not the "Type"
        '''   column is displayed (shows the type of resource, e.g. System.Drawing.Image)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ShowTypeColumnInStringTable() As Boolean
            Get
                Return m_ShowTypeColumnInStringTable
            End Get
            Set(ByVal Value As Boolean)
                Debug.Assert(m_CategoryDisplay = Display.StringTable, "This property only applies to string table categories")
                m_ShowTypeColumnInStringTable = Value
            End Set
        End Property

        ''' <summary>
        '''  how to sort the resource items in the category...
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property Sorter() As IComparer
            Get
                Return m_Sorter
            End Get
            Set
                m_Sorter = value
            End Set
        End Property

        ''' <summary>
        ''' If this category uses a stringtable, indicates whether or not the user is allowed
        '''   to add new entries in this string table, via an add/new row at the bottom of
        '''   the table.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property AllowNewEntriesInStringTable() As Boolean
            Get
                Return m_AllowNewEntriesInStringTable
            End Get
            Set(ByVal Value As Boolean)
                Debug.Assert(m_CategoryDisplay = Display.StringTable, "This property only applies to string table categories")
                m_AllowNewEntriesInStringTable = Value
            End Set
        End Property

        Public Function Compare(ByVal Resource1 As Resource, ByVal Resource2 As Resource) As Integer
            If m_Sorter IsNot Nothing
                Return m_Sorter.Compare(Resource1, Resource2)
            Else
                ' Name is the default order...
                Return String.Compare(Resource1.Name, Resource2.Name, ignoreCase:=True, culture:=CultureInfo.CurrentUICulture)
            End If
        End Function

    End Class
End Namespace
