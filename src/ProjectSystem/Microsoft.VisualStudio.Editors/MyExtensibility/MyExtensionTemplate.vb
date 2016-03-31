Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics
Imports System.Xml
Imports System.Xml.Schema
Imports System.Xml.Serialization
Imports Microsoft.VisualStudio.Editors.MyExtensibility.EnvDTE90Interop
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensionTemplate
    ''' <summary>
    ''' Information about a specific extension template: triggering assembly, ID, version, template.
    ''' </summary>
    Friend Class MyExtensionTemplate
        Implements INamedDescribedObject

        ''' ;CreateInstance
        ''' <summary>
        ''' Shared factory method to create a MyExtensionTemplate. Return Nothing if the Template is invalid.
        ''' </summary>
        Public Shared Function CreateInstance(ByVal template As Template) As MyExtensionTemplate
            ' CONSIDER: (HuyN) - ID can be nothing, fall back to name instead?

            If template Is Nothing Then
                Return Nothing
            End If
            If StringIsNullEmptyOrBlank(template.FilePath) Then
                Return Nothing
            End If
            If String.IsNullOrEmpty(template.CustomData) Then
                Return Nothing
            End If

            Dim templateID As String = Nothing
            Dim templateVersion As Version = Nothing
            Dim assemblyFullName As String = Nothing

            Try
                Dim xmlDocument As New XmlDocument()
                Using reader As System.Xml.XmlReader = System.Xml.XmlReader.Create(New System.IO.StringReader(template.CustomData))
                    xmlDocument.Load(reader)
                End Using

                Dim extensionNodes As XmlNodeList = xmlDocument.GetElementsByTagName(MY_EXTENSION_TEMPLATE_ELEMENT_NAME)
                If extensionNodes.Count <= 0 Then
                    Return Nothing
                End If

                Dim extensionElement As XmlElement = Nothing
                For Each node As XmlNode In extensionNodes
                    extensionElement = TryCast(node, XmlElement)
                    If extensionElement IsNot Nothing Then
                        Exit For
                    End If
                Next

                If extensionElement Is Nothing Then
                    Return Nothing
                End If

                templateID = GetAttributeValue(extensionElement, ID_ATTRIBUTE_NAME)
                Dim templateVersionString As String = GetAttributeValue(extensionElement, VERSION_ATTRIBUTE_NAME)
                If StringIsNullEmptyOrBlank(templateVersionString) Then
                    Return Nothing
                End If
                templateVersion = GetVersion(templateVersionString)
                assemblyFullName = NormalizeAssemblyFullName( _
                    GetAttributeValue(extensionElement, ASM_FULLNAME_ATTRIBUTE_NAME))

            Catch ex As XmlException ' Only ignore load or parse error in the XML.
                Return Nothing
            End Try

            If StringIsNullEmptyOrBlank(templateID) Then
                Return Nothing
            End If
            If templateVersion Is Nothing Then
                Return Nothing
            End If

            Return New MyExtensionTemplate(templateID, templateVersion, template, assemblyFullName)
        End Function

        Public ReadOnly Property AssemblyFullName() As String
            Get
                Return m_AssemblyFullName
            End Get
        End Property

        Public ReadOnly Property ID() As String
            Get
                Return m_ID
            End Get
        End Property

        Public ReadOnly Property Version() As Version
            Get
                Return m_Version
            End Get
        End Property

        Public ReadOnly Property Description() As String Implements INamedDescribedObject.Description
            Get
                Return m_Template.Description
            End Get
        End Property

        ''' ;DisplayName
        ''' <summary>
        ''' The extension template's name specified in .vstemplate file, or the template ID specified in CustomData.xml.
        ''' </summary>
        Public ReadOnly Property DisplayName() As String Implements INamedDescribedObject.DisplayName
            Get
                If StringIsNullEmptyOrBlank(m_Template.Name) Then
                    Return m_ID
                Else
                    Return m_Template.Name.Trim()
                End If
            End Get
        End Property

        Public ReadOnly Property FilePath() As String
            Get
                Return m_Template.FilePath
            End Get
        End Property

        Public ReadOnly Property BaseName() As String
            Get
                Return m_Template.BaseName
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Dim extensionTemplate As MyExtensionTemplate = TryCast(obj, MyExtensionTemplate)

            If extensionTemplate IsNot Nothing Then
                Return StringEquals(Me.FilePath, extensionTemplate.FilePath)
            End If

            Return MyBase.Equals(obj)
        End Function

        ''' ;New
        ''' <summary>
        ''' Private constructor to avoid create this class directly.
        ''' </summary>
        Private Sub New( _
                ByVal id As String, ByVal version As Version, _
                ByVal template As Template, ByVal assemblyFullName As String)

            Debug.Assert(Not StringIsNullEmptyOrBlank(id), "Invalid id!")
            Debug.Assert(version IsNot Nothing, "Invalid version!")
            Debug.Assert(template IsNot Nothing, "Invalid tempalte!")
            Debug.Assert(Not StringIsNullEmptyOrBlank(template.FilePath), "Invalid template.FilePath!")

            m_AssemblyFullName = assemblyFullName
            m_ID = id
            m_Version = version
            m_Template = template
        End Sub

        Private m_ID As String ' Extension ID
        Private m_Version As Version ' Extension version
        Private m_Template As Template ' VSCore Template file.
        Private m_AssemblyFullName As String ' Full name of the triggering assembly.

        ' Element and attribute names for extension template information in template's custom data.
        Private Const MY_EXTENSION_TEMPLATE_ELEMENT_NAME As String = "VBMyExtensionTemplate"
        Private Const ID_ATTRIBUTE_NAME As String = "ID"
        Private Const VERSION_ATTRIBUTE_NAME As String = "Version"
        Private Const ASM_FULLNAME_ATTRIBUTE_NAME As String = "AssemblyFullName"
    End Class

End Namespace

