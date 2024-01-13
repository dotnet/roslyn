' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Organizing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Organizing
    <[UseExportProvider]>
    Public MustInherit Class AbstractOrganizerTests

        Protected Shared Async Function CheckAsync(initial As XElement, final As XElement) As System.Threading.Tasks.Task
            Using workspace = TestWorkspace.CreateVisualBasic(initial.NormalizedValue)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim newRoot = Await (Await OrganizingService.OrganizeAsync(document)).GetSyntaxRootAsync()
                Assert.Equal(final.NormalizedValue, newRoot.ToFullString())
            End Using
        End Function
    End Class
End Namespace
