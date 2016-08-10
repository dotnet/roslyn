Imports System.IO
Imports System.Text
Imports Microsoft.Build.Framework
Imports Microsoft.Build.Utilities
Imports <xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">

Public Class UpdateNuGetTemplateVersions
    Inherits Task

    <Required()>
    Public Property ItemsToRewrite As ITaskItem()

    <Required()>
    Public Property AssemblyVersion As String

    <Output>
    Public Property NuGetPackagesReferenced As ITaskItem()

    <Required>
    Public Property IntermediatePath As String

    Private ReadOnly _newItems As New List(Of ITaskItem)

    <Output>
    Public ReadOnly Property NewItems As ITaskItem()
        Get
            Return _newItems.ToArray()
        End Get
    End Property

    Private ReadOnly _referencedNuGetPackages As New HashSet(Of String)

    <Output>
    Public ReadOnly Property ReferencedNuGetPackages As ITaskItem()
        Get
            Return _referencedNuGetPackages.Select(Function(s) New TaskItem(s)).ToArray()
        End Get
    End Property

    Public Overrides Function Execute() As Boolean
        Try
            If Directory.Exists(IntermediatePath) Then
                For Each file In New DirectoryInfo(IntermediatePath).GetFiles("*.*", SearchOption.AllDirectories)
                    file.IsReadOnly = False
                Next

                Directory.Delete(IntermediatePath, recursive:=True)
            End If

            For Each template In ItemsToRewrite
                Dim templateXml = UpdateNugetVersion(template.ItemSpec)
                Dim newDirectory As String = SaveTemplateXml(templateXml, template.ItemSpec, IntermediatePath, template.GetMetadata("OutputSubPath"), _newItems)

                ' For all other files, just copy them along
                Dim originalDirectory = Path.GetDirectoryName(Path.GetFullPath(template.ItemSpec))
                For Each extraFile In New DirectoryInfo(originalDirectory).GetFiles("*.*", SearchOption.AllDirectories)
                    If extraFile.Extension = ".vstemplate" Then
                        ' If this is one of our multi-project template files we need to do a rename
                        If IsReferencedTemplate(template, extraFile) Then
                            Dim referencedTemplateXml = UpdateNugetVersion(extraFile.FullName)
                            Dim relativePath = GetRelativePath(Path.GetFullPath(newDirectory), extraFile.FullName).Replace("..\", String.Empty)
                            Dim fullPath = Path.GetFullPath(Path.Combine(newDirectory, relativePath))
                            referencedTemplateXml.Save(fullPath)
                        Else
                            Continue For
                        End If
                    End If
                    CopyExtraFile(newDirectory, originalDirectory, extraFile.FullName)
                Next
            Next
            Return True
        Catch ex As Exception
            ' Show the stack trace in the build log to make diagnosis easier.
            Log.LogMessage($"Exception was thrown, stack trace was: {Environment.NewLine}{ex.StackTrace}")
            Throw
        End Try
    End Function

    Private Function GetRelativePath(fromPath As String, toPath As String) As String
        Dim fromAttr = GetPathAttribute(fromPath)
        Dim toAttr = GetPathAttribute(toPath)
        Dim path = New StringBuilder(260)

        If Not PathRelativePathTo(path, fromPath, fromAttr, toPath, toAttr) Then
            Throw New ArgumentException($"Paths {fromPath} and {toPath} do not have a common prefix")
        End If

        Return path.ToString()
    End Function

    Private Function GetPathAttribute(path As String) As Integer
        Dim directoryInfo = New DirectoryInfo(path)
        If directoryInfo.Exists Then
            ' This magic hexadecimal number represents a folder.
            Return &H10
        End If

        Dim fileInfo = New FileInfo(path)
        If fileInfo.Exists Then
            ' This magic hexadecimal number represents a file.
            Return &H80
        End If

        ' If the path doesn't exist assume it to be a folder.
        Return &H10
    End Function

    Private Declare Auto Function PathRelativePathTo Lib "shlwapi.dll" (pszPath As StringBuilder, pszFrom As String, dwAttrFrom As Integer, pszTo As String, dwAttrTo As Integer) As Boolean

    Private Function UpdateNugetVersion(fullPath As String) As XDocument
        Dim xml = XDocument.Load(fullPath)
        Dim packages = xml...<package>

        For Each package In packages
            If package.@id = "NuGet.CommandLine" Then
                package.@version = "2.8.5"
            ElseIf package.@id = "System.Collections.Immutable" Then
                package.@version = "1.1.36"
            ElseIf package.@id = "System.Reflection.Metadata" Then
                package.@version = "1.0.21"
            ElseIf package.@id = "Microsoft.Composition" Then
                package.@version = "1.0.27"
            ElseIf package.@id = "Microsoft.CodeAnalysis.Analyzers" Then
                package.@version = "1.1.0"
            Else
                package.@version = "1.0.0"
            End If

            _referencedNuGetPackages.Add(package.@id + "." + package.@version + ".nupkg")
        Next

        For Each assembly In xml...<Assembly>
            assembly.Value = assembly.Value.Replace("0.0.0.0", AssemblyVersion)
        Next
        Return xml
    End Function

    Private Shared Function IsReferencedTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return IsReferencedCSharpTemplate(template, extraFile) OrElse
               IsReferencedVisualBasicTemplate(template, extraFile)
    End Function

    Private Shared Function IsReferencedCSharpTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return (template.ItemSpec.Contains("CSharpDiagnostic.vstemplate") Or
                template.ItemSpec.Contains("CSRef.vstemplate")) AndAlso
               extraFile.FullName.Contains("CSharp") AndAlso
               (extraFile.FullName.Contains("DiagnosticAnalyzer") Or
                extraFile.FullName.Contains("Test") Or
                extraFile.FullName.Contains("Vsix") Or
                extraFile.FullName.Contains("CodeRefactoring"))
    End Function

    Private Shared Function IsReferencedVisualBasicTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return (template.ItemSpec.Contains("VBDiagnostic.vstemplate") Or
                template.ItemSpec.Contains("VBRef.vstemplate")) AndAlso
               extraFile.FullName.Contains("VisualBasic") AndAlso
               (extraFile.FullName.Contains("DiagnosticAnalyzer") Or
                extraFile.FullName.Contains("Test") Or
                extraFile.FullName.Contains("Vsix") Or
                extraFile.FullName.Contains("CodeRefactoring"))
    End Function



End Class
