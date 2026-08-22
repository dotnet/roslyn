Option Explicit On
Option Infer On
Option Strict On

Imports System.IO
Imports System.Text
Imports System.Xml

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

#Disable Warning RS1035 ' Do not use APIs banned for analyzers

Namespace SourceGeneratorSamples

    <Generator(LanguageNames.VisualBasic)>
    Public Class SettingsXmlGenerator
        Implements ISourceGenerator

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize

        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            ' Using the context, get any additional files that end in .xmlsettings
            For Each settingsFile In context.AdditionalFiles.Where(Function(at) at.Path.EndsWith(".xmlsettings"))
                ProcessSettingsFile(settingsFile, context)
            Next
        End Sub

        Private Sub ProcessSettingsFile(xmlFile As AdditionalText, context As GeneratorExecutionContext)

            ' try and load the settings file
            Dim xmlDoc As New XmlDocument
            Dim text = xmlFile.GetText(context.CancellationToken).ToString()
            Try
                xmlDoc.LoadXml(text)
            Catch
                'TODO: issue a diagnostic that says we couldn't parse it
                Return
            End Try

            ' create a class in the XmlSetting class that represnts this entry, and a static field that contains a singleton instance.
            Dim fileName = Path.GetFileName(xmlFile.Path)
            Dim name = xmlDoc.DocumentElement.GetAttribute("name")

            Dim sb = New StringBuilder($"Option Explicit On
Option Strict On
Option Infer On

Imports System.Xml

Namespace Global.AutoSettings

  Partial Public Class XmlSettings

    Public Shared ReadOnly Property {name} As {name}Settings = New {name}Settings(""{fileName}"")

    Public Class {name}Settings

      Private m_xmlDoc As New XmlDocument()

      Private m_fileName As String

      Friend Sub New(fileName As String)
        m_fileName = fileName
        m_xmlDoc.Load(m_fileName)
      End Sub

      Public Function GetLocation() As String
        Return m_fileName
      End Function")

            For i = 0 To xmlDoc.DocumentElement.ChildNodes.Count - 1

                Dim setting = CType(xmlDoc.DocumentElement.ChildNodes(i), XmlElement)
                Dim settingName = setting.GetAttribute("name")
                Dim settingType = setting.GetAttribute("type")

                sb.Append($"

      Public ReadOnly Property {settingName} As {settingType}
        Get
          Return DirectCast(Convert.ChangeType(DirectCast(m_xmlDoc.DocumentElement.ChildNodes({i}), XmlElement).InnerText, GetType({settingType})), {settingType})
        End Get
      End Property")

            Next

            sb.Append("

    End Class

  End Class

End Namespace")

            context.AddSource($"Settings_{name}.g.vb", SourceText.From(sb.ToString(), Encoding.UTF8))

        End Sub

    End Class

End Namespace
