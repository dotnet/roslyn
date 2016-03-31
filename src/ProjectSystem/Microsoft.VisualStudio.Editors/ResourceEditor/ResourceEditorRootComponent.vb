Option Explicit On
Option Strict On
Option Compare Binary

Imports EnvDTE
Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.ResourceEditor


    ''' <summary>
    ''' This class represents the root component of the resource editor.  It could be thought of as
    '''    resepenting the .resx file itself, except that we may handle more than one .resx file in 
    '''    the resource editor at once.  
    '''    This class itself does not do much except to keep a reference to ResXResourceFile(s) being
    '''    edited.  If this were a "real" component, this class would represent the component whose
    '''    properties are being edited (think of a form, for instance).  Thus this class only
    '''    manages the state of the edited object.  Persistence is handled in the code loader, and the
    '''    UI is handled by the ResourceEditorRootDesigner.
    ''' </summary>
    ''' <remarks></remarks>
    <Designer(GetType(ResourceEditorRootDesigner), GetType(IRootDesigner))> _
    Friend NotInheritable Class ResourceEditorRootComponent
        Inherits Component


#Region "Fields"

        'True iff the root component is being torn down or is already torn down.
        Private m_TearingDown As Boolean

        'The resx resource file we are currently editing
        Private m_ResourceFile As ResourceFile

        'The resx resource file we are currently editing
        Private m_ResourceFileName As String

        'Cached reference to our associated root designer
        Private m_RootDesigner As ResourceEditorRootDesigner

        'Whether the resource file belongs to another file (form/userControl)
        Private m_IsDependentFile As Boolean

        'Whether the resource item belongs to a device project
        Private m_IsInsideDeviceProject As Boolean

        'Whether the resource item belongs to the global resource folder in ASP .Net application
        Private m_IsGlobalResourceInASP As Boolean

        ' ASP.Net ProjectKind GUID
        Private Shared ReadOnly ProjectGuid_ASPDotNet As New Guid("E24C65DC-7377-472b-9ABA-BC803B73C61A")


#End Region



#Region "Constructors/destructors"

        ''' <summary>
        ''' Override Dispose().
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal Disposing As Boolean)
            m_TearingDown = True

            If Disposing Then
                'Stop the resource file from listening to component change events.  It doesn't need to try to
                '  remove each component separately.  The base class call to Dispose here will remove all
                '  the subcomponents (Resources) from the host, and we'll dispose the Resources themselves
                '  in ResourceFile.Dispose() later.
                If m_ResourceFile IsNot Nothing Then
                    m_ResourceFile.ComponentChangeService = Nothing
                End If
            End If

            MyBase.Dispose(Disposing)
        End Sub

#End Region



#Region "Properties"


        '*****************************************************************************************************
        '*****************************************************************************************************
        '
        '  WARNING!!!!!
        '
        '  This class ends up being displayed in the properties window.  Because of that, we do not want
        '  any public properties on this class.  Use Friend instead, or they will show up in the properties
        '  window.
        '
        '*****************************************************************************************************
        '*****************************************************************************************************


        ''' <summary>
        ''' True iff the root component is being torn down or is already torn down.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property IsTearingDown() As Boolean
            Get
                Return m_TearingDown
            End Get
        End Property


        ''' <summary>
        ''' Gets the ResXResourceFile that is currently being edited.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property ResourceFile() As ResourceFile
            Get
                Debug.Assert(Not m_ResourceFile Is Nothing, "m_ResourceFile should have already been created!  SetResXResourceFile not called?")
                Return m_ResourceFile
            End Get
        End Property

        ''' <summary>
        ''' Gets the ResXResourceFileName that is currently being edited.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend Property ResourceFileName() As String
            Get
                Return m_ResourceFileName
            End Get
            Set
                m_ResourceFileName = value
            End Set
        End Property

        ''' <summary>
        ''' Returns the root designer that is associated with this component, i.e., the
        '''   designer which is showing the UI to the user which allows this component's
        '''   resx file to be edited by the user.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property RootDesigner() As ResourceEditorRootDesigner
            Get
                If m_RootDesigner Is Nothing Then
                    'Not yet cached - get this info from the designer host
                    Debug.Assert(Not Container Is Nothing)
                    Dim Host As IDesignerHost = CType(Container, IDesignerHost)
                    m_RootDesigner = CType(Host.GetDesigner(Me), ResourceEditorRootDesigner)
                End If

                Debug.Assert(Not m_RootDesigner Is Nothing, "Don't have an associated designer?!?")
                Return m_RootDesigner
            End Get
        End Property

        '''<summary>
        ''' Whether the resource file belongs to another file (form/userControl)
        '''</summary>
        Friend ReadOnly Property IsDependentFile() As Boolean
            Get
                Return m_IsDependentFile
            End Get
        End Property

        '''<summary>
        ''' Whether the resource item belongs to a device project
        '''</summary>
        Friend ReadOnly Property IsInsideDeviceProject() As Boolean
            Get
                Return m_IsInsideDeviceProject
            End Get
        End Property

        ''' <summary>
        '''  Whether the resource item belongs to the global resource folder in ASP .Net application
        ''' </summary>
        Friend ReadOnly Property IsGlobalResourceInASP() As Boolean
            Get
                Return m_IsGlobalResourceInASP
            End Get
        End Property

#End Region



        ''' <summary>
        ''' Associates a ResXResourceFile (which represents a single editable .resx file) with this
        '''    component.  This effectively sets the .resx file which is being edited by the user.
        ''' </summary>
        ''' <param name="NewResourceFile"></param>
        ''' <remarks></remarks>
        Friend Sub LoadResXResourceFile(ByVal NewResourceFile As ResourceFile)
            Debug.Assert(NewResourceFile.RootComponent Is Me)
            Debug.Assert(Not NewResourceFile Is Nothing)
            Debug.Assert(m_ResourceFile Is Nothing, "ResourceEditorRootComponent.LoadResXResourceFile(): a resource file has already been loaded")

            'Set our reference to the new resx file
            m_ResourceFile = NewResourceFile
            m_IsDependentFile = IsDependentItem()
            m_IsInsideDeviceProject = IsInDeviceProject()
            m_IsGlobalResourceInASP = IsInGlobalResourceFolderInASP()

            'Now let the root designer know of the change.  It will
            '  cause the designer UI to be changed.
            If Not RootDesigner Is Nothing Then
                RootDesigner.SetResourceFile(NewResourceFile)
            End If
        End Sub

        ''' <summary>
        ''' Checks to see whether the RESX file belongs to another file item (like form, usercontrol...)
        ''' </summary>
        ''' <remarks></remarks>
        Private Function IsDependentItem() As Boolean
            Dim ProjectItem As ProjectItem = TryCast(RootDesigner.GetService(GetType(ProjectItem)), ProjectItem)
            Debug.Assert(ProjectItem IsNot Nothing, "ProjectItem not found!")

            ' check whether the RESX file belongs to anther file item (usually form.xx)
            If ProjectItem IsNot Nothing AndAlso ProjectItem.Collection IsNot Nothing Then
                Dim parent As Object = ProjectItem.Collection.Parent
                Dim parentItem As ProjectItem = TryCast(parent, ProjectItem)
                If parentItem IsNot Nothing Then
                    Dim kindString As String = parentItem.Kind
                    Try
                        Dim kindGuid As Guid = New Guid(kindString)
                        If kindGuid.Equals(new Guid(EnvDTE.Constants.vsProjectItemKindPhysicalFile)) Then
                            Return True
                        End If
                    Catch ex As Exception
                        Common.RethrowIfUnrecoverable(ex)
                    End Try
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Checks to see whether the RESX file is inside a device project...
        '''  We got a lot of limitation for this kind of projects
        ''' </summary>
        ''' <remarks></remarks>
        Private Function IsInDeviceProject() As Boolean
            Dim hierarchy As IVsHierarchy = DirectCast(RootDesigner.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            If hierarchy IsNot Nothing Then
                Return Common.ShellUtil.IsDeviceProject(hierarchy)
            End If
            Return False
        End Function

        ''' <summary>
        ''' Checks to see whether the RESX file is under "App_GlobalResources" in an ASP project
        ''' </summary>
        ''' <remarks></remarks>
        Private Function IsInGlobalResourceFolderInASP() As Boolean
            Try
                Dim projectItem As EnvDTE.ProjectItem = RootDesigner.DesignerLoader.ProjectItem
                If projectItem Is Nothing Then
                    Return False
                End If

                Dim project As EnvDTE.Project = projectItem.ContainingProject
                If project Is Nothing Then
                    Return False
                End If

                Dim projectKind As String = project.Kind
                If projectKind Is Nothing OrElse Not ProjectGuid_ASPDotNet.Equals(New Guid(projectKind)) Then
                    ' NOT Venus project
                    Return False
                End If

                ' Check the file is under "App_GlobalResources" directory or any of its sub directory...
                While projectItem IsNot Nothing AndAlso projectItem.Collection IsNot Nothing
                    projectItem = TryCast(projectItem.Collection.Parent, EnvDTE.ProjectItem)
                    If projectItem IsNot Nothing Then
                        Dim folderTypeProperty As EnvDTE.Property = projectItem.Properties.Item("FolderType")
                        If folderTypeProperty IsNot Nothing Then
                            Dim folderType As Integer = CInt(folderTypeProperty.Value)
                            If folderType = CInt(VsWebSite.webFolderType.webFolderTypeGlobalResources) Then
                                Return True
                            End If
                        End If
                    End If
                End While
            Catch ex As FormatException
                ' Ignore this ...
            Catch ex As Exception
                Common.RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.Message)
            End Try
            Return False
        End Function
    End Class

End Namespace
