' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public MustInherit Class AbstractVisualBasicKeywordHighlighterTests
        Inherits AbstractKeywordHighlighterTests

        Protected Overrides Function GetOptions() As IEnumerable(Of ParseOptions)
            Return SpecializedCollections.SingletonEnumerable(TestOptions.Regular)
        End Function

        Protected Overloads Function TestAsync(element As XElement) As Task
            Return TestAsync(element.NormalizedValue)
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(code As String, options As ParseOptions) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(code, options, composition:=Composition)
        End Function
    End Class
End Namespace
