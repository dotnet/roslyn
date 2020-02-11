' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
'Reads the tree from an XML file into a ParseTree object and sub-objects. Reports many kinds of errors
'during the reading process.
'-----------------------------------------------------------------------------------------------------------

Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Xml
Imports System.Xml.Schema
Imports <xmlns="http://schemas.microsoft.com/VisualStudio/Roslyn/Compiler">

Public Module ReadTree
    Private s_currentFile As String

    ' Read an XML file and return the resulting ParseTree object.
    Public Function TryReadTheTree(fileName As String, <Out> ByRef tree As ParseTree) As Boolean

        tree = Nothing

        Dim validationError As Boolean = False
        Dim xDoc = GetXDocument(fileName, validationError)
        If validationError Then
            Return False
        End If

        tree = New ParseTree
        Dim x = xDoc.<define-parse-tree>

        tree.FileName = fileName
        tree.NamespaceName = xDoc.<define-parse-tree>.@namespace
        tree.VisitorName = xDoc.<define-parse-tree>.@visitor
        tree.RewriteVisitorName = xDoc.<define-parse-tree>.@<rewrite-visitor>
        tree.FactoryClassName = xDoc.<define-parse-tree>.@<factory-class>
        tree.ContextualFactoryClassName = xDoc.<define-parse-tree>.@<contextual-factory-class>

        Dim defs = xDoc.<define-parse-tree>.<definitions>

        For Each struct In defs.<node-structure>
            If tree.NodeStructures.ContainsKey(struct.@name) Then
                tree.ReportError(struct, "node-structure with name ""{0}"" already defined", struct.@name)
            Else
                tree.NodeStructures.Add(struct.@name, New ParseNodeStructure(struct, tree))
            End If
        Next

        For Each al In defs.<node-kind-alias>
            If tree.Aliases.ContainsKey(al.@name) Then
                tree.ReportError(al, "node-kind-alias with name ""{0}"" already defined", al.@name)
            Else
                tree.Aliases.Add(al.@name, New ParseNodeKindAlias(al, tree))
            End If
        Next

        For Each en In defs.<enumeration>
            If tree.Enumerations.ContainsKey(en.@name) Then
                tree.ReportError(en, "enumeration with name ""{0}"" already defined", en.@name)
            Else
                tree.Enumerations.Add(en.@name, New ParseEnumeration(en, tree))
            End If
        Next

        tree.FinishedReading()
        Return True
    End Function

    ' Open the input XML file as an XDocument, using the reading options that we want.
    ' We use a schema to validate the input.
    Private Function GetXDocument(fileName As String, <Out> ByRef validationError As Boolean) As XDocument
        s_currentFile = fileName

        Dim xDoc As XDocument
        Using schemaReader = XmlReader.Create(GetType(ReadTree).GetTypeInfo().Assembly.GetManifestResourceStream("VBSyntaxModelSchema.xsd"))

            Dim readerSettings As New XmlReaderSettings()
            readerSettings.DtdProcessing = DtdProcessing.Prohibit

            Dim fileStream As New FileStream(fileName, FileMode.Open, FileAccess.Read)
            Using reader = XmlReader.Create(fileStream, readerSettings)
                xDoc = XDocument.Load(reader, LoadOptions.SetLineInfo Or LoadOptions.PreserveWhitespace)
            End Using
        End Using

        validationError = False
        Return xDoc
    End Function

End Module
