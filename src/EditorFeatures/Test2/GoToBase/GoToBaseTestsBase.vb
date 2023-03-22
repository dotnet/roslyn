' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.GoToBase
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    Public MustInherit Class GoToBaseTestsBase
        Protected Shared Async Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True,
                                           Optional metadataDefinitions As String() = Nothing) As Task
            Await GoToHelpers.TestAsync(
                workspaceDefinition,
                testHost:=TestHost.InProcess,
                Async Function(document As Document, position As Integer, context As SimpleFindUsagesContext)
                    Dim gotoBaseService = document.GetLanguageService(Of IGoToBaseService)
                    Await gotoBaseService.FindBasesAsync(context, document, position, CancellationToken.None)
                End Function,
                shouldSucceed, metadataDefinitions)
        End Function

        Protected Shared Async Function TestAsync(source As String, language As String, Optional shouldSucceed As Boolean = True,
                                           Optional metadataDefinitions As String() = Nothing) As Task
            Await TestAsync(
                   <Workspace>
                       <Project Language=<%= language %> CommonReferences="true">
                           <Document>
                               <%= source %>
                           </Document>
                       </Project>
                   </Workspace>,
                shouldSucceed, metadataDefinitions)
        End Function
    End Class
End Namespace
