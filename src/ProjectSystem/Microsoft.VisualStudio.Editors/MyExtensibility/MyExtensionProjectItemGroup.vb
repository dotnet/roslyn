Option Strict On
Option Explicit On
Imports EnvDTE
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensionProjectItemGroup
    ''' <summary>
    ''' Contains information about a My Extension in a project,
    ''' including its extension ID, version, name, description; 
    ''' and the list of physical files in the project.
    ''' </summary>
    Friend Class MyExtensionProjectItemGroup
        Implements INamedDescribedObject

        ''' ;New
        ''' <summary>
        ''' fileName, extensionID and extensionVersion are required.
        ''' </summary>
        Public Sub New( _
                ByVal extensionID As String, ByVal extensionVersion As Version, _
                ByVal extensionName As String, ByVal extensionDescription As String)
            Debug.Assert(Not StringIsNullEmptyOrBlank(extensionID), "Invalid extensionID!")
            Debug.Assert(extensionVersion IsNot Nothing, "Invalid extensionVersion!")

            m_ExtensionID = extensionID
            m_ExtensionVersion = extensionVersion
            m_ExtensionName = extensionName
            m_ExtensionDescription = extensionDescription
        End Sub

        Public ReadOnly Property ExtensionProjectItems() As List(Of ProjectItem)
            Get
                Return m_ProjectItems
            End Get
        End Property

        Public ReadOnly Property ExtensionID() As String
            Get
                Return m_ExtensionID
            End Get
        End Property

        Public ReadOnly Property ExtensionVersion() As Version
            Get
                Return m_ExtensionVersion
            End Get
        End Property

        Public ReadOnly Property ExtensionDescription() As String Implements INamedDescribedObject.Description
            Get
                Return m_ExtensionDescription
            End Get
        End Property

        Public ReadOnly Property DisplayName() As String Implements INamedDescribedObject.DisplayName
            Get
                If StringIsNullEmptyOrBlank(m_ExtensionName) Then
                    Return m_ExtensionID
                Else
                    Return m_ExtensionName
                End If
            End Get
        End Property

        Public Sub AddProjectItem(ByVal projectItem As ProjectItem)
            If projectItem IsNot Nothing Then
                If m_ProjectItems Is Nothing Then
                    m_ProjectItems = New List(Of ProjectItem)
                End If
                m_ProjectItems.Add(projectItem)
            End If
        End Sub

        Public Function IDEquals(ByVal id As String) As Boolean
            Return String.Equals(m_ExtensionID, id, StringComparison.Ordinal)
        End Function

        Private Sub New()
        End Sub

        Private m_ExtensionID As String
        Private m_ExtensionVersion As Version
        Private m_ExtensionName As String
        Private m_ExtensionDescription As String
        Private m_ProjectItems As List(Of ProjectItem)
    End Class

End Namespace

