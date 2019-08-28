' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.ExceptionServices
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser
    <[UseExportProvider]>
    Public MustInherit Class AbstractObjectBrowserTests

        Protected MustOverride ReadOnly Property LanguageName As String

        Protected Function GetWorkspaceDefinition(code As XElement) As XElement
            Return <Workspace>
                       <Project Language=<%= LanguageName %> CommonReferences="true">
                           <Document><%= code.Value.Trim() %></Document>
                       </Project>
                   </Workspace>
        End Function

        Protected Function GetWorkspaceDefinition(code As XElement, metaDataCode As XElement, commonReferences As Boolean) As XElement
            Return <Workspace>
                       <Project Language=<%= LanguageName %> CommonReferences=<%= commonReferences %>>
                           <Document><%= code.Value.Trim() %></Document>
                           <MetadataReferenceFromSource Language=<%= LanguageName %> CommonReferences="true">
                               <Document><%= metaDataCode.Value.Trim() %></Document>
                           </MetadataReferenceFromSource>
                       </Project>
                   </Workspace>
        End Function

        <HandleProcessCorruptedStateExceptions()>
        Friend Function CreateLibraryManager(definition As XElement) As TestState
            Dim workspace = TestWorkspace.Create(definition, exportProvider:=VisualStudioTestExportProvider.Factory.CreateExportProvider())
            Dim result As TestState = Nothing

            Try
                Dim vsWorkspace = New MockVisualStudioWorkspace(workspace)
                Dim mockComponentModel = New MockComponentModel(workspace.ExportProvider)
                mockComponentModel.ProvideService(Of VisualStudioWorkspace)(vsWorkspace)
                Dim mockServiceProvider = New MockServiceProvider(mockComponentModel)
                Dim libraryManager = CreateLibraryManager(mockServiceProvider, mockComponentModel, vsWorkspace)

                result = New TestState(workspace, libraryManager)
            Finally
                If result Is Nothing Then
                    workspace.Dispose()
                End If
            End Try

            Return result
        End Function

        Friend MustOverride Function CreateLibraryManager(serviceProvider As IServiceProvider, componentModel As IComponentModel, workspace As VisualStudioWorkspace) As AbstractObjectBrowserLibraryManager

        Friend Function ProjectNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Projects, .Name = name}
        End Function

        Friend Function NamespaceNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Namespaces, .Name = name}
        End Function

        Friend Function TypeNode(name As String) As NavInfoNodeDescriptor
            Return New NavInfoNodeDescriptor With {.Kind = ObjectListKind.Types, .Name = name}
        End Function

    End Class
End Namespace
