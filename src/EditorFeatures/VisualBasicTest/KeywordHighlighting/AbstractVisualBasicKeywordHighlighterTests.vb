' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public MustInherit Class AbstractVisualBasicKeywordHighlighterTests
        Inherits AbstractKeywordHighlighterTests

        Protected Overrides Function GetOptions() As IEnumerable(Of ParseOptions)
            Return {TestOptions.Regular}
        End Function

        Protected Overloads Function TestAsync(element As XElement) As Threading.Tasks.Task
            Return TestAsync(element.NormalizedValue)
        End Function

        Protected Overrides Function CreateWorkspaceFromFileAsync(code As String, options As ParseOptions) As Threading.Tasks.Task(Of TestWorkspace)
            Return TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code, DirectCast(options, ParseOptions))
        End Function
    End Class
End Namespace
