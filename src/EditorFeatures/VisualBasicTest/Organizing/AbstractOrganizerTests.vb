' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.Organizing
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Organizing
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Organizing
    Public MustInherit Class AbstractOrganizerTests

        Protected Async Function CheckAsync(initial As XElement, final As XElement) As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspace.CreateVisualBasicAsync(initial.NormalizedValue)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim newRoot = Await (Await OrganizingService.OrganizeAsync(document)).GetSyntaxRootAsync()
                Assert.Equal(final.NormalizedValue, newRoot.ToFullString())
            End Using
        End Function
    End Class
End Namespace
