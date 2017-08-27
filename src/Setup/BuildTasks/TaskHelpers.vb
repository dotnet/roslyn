Imports System.IO
Imports Microsoft.Build.Framework
Imports Microsoft.Build.Utilities

Module TaskHelpers
    Public Function SaveTemplateXml(xml As XDocument, templatePath As String, intermediatePath As String, metaDataValue As String, ByRef newItems As List(Of ITaskItem)) As String
        Dim newDirectory = Path.Combine(intermediatePath, Path.GetDirectoryName(templatePath))
        Directory.CreateDirectory(newDirectory)

        ' Rewrite the .vstemplate file and save it
        Dim newItemFilePath = Path.Combine(newDirectory, Path.GetFileName(templatePath))

        ' Work around the inability of the SDK to take an absolute path here
        Dim newItem = New TaskItem(newItemFilePath)
        newItem.SetMetadata("OutputSubPath", metaDataValue)
        newItems.Add(newItem)

        xml.Save(newItemFilePath)
        Return newDirectory
    End Function

    Public Sub CopyExtraFile(newDirectory As String, originalDirectory As String, extraFile As String)
        Dim relativePath = extraFile.Substring(originalDirectory.Length)

        ' This intentionally isn't using path.combine since relative path may lead with a \
        Dim fullPath = Path.GetFullPath(newDirectory + relativePath)

        Try
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
            File.Copy(extraFile, fullPath)

            ' Ensure the copy isn't read only
            Dim copyFileInfo As New FileInfo(fullPath)
            copyFileInfo.IsReadOnly = False
        Catch ex As Exception
        End Try
    End Sub

End Module
