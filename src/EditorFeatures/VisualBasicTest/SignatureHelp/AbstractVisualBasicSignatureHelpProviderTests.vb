' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public MustInherit Class AbstractVisualBasicSignatureHelpProviderTests
        Inherits AbstractSignatureHelpProviderTests(Of VisualBasicTestWorkspaceFixture)

        ' We want to skip script testing in all VB stuff for now.

        Protected Overrides Function TestAsync(markupWithPositionAndOptSpan As String, Optional expectedOrderedItemsOrNull As IEnumerable(Of SignatureHelpTestItem) = Nothing, Optional usePreviousCharAsTrigger As Boolean = False, Optional sourceCodeKind As Microsoft.CodeAnalysis.SourceCodeKind? = Nothing, Optional experimental As Boolean = False) As Threading.Tasks.Task
            If (sourceCodeKind.HasValue) Then
                Return MyBase.TestAsync(markupWithPositionAndOptSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger, sourceCodeKind, experimental)
            Else
                Return MyBase.TestAsync(markupWithPositionAndOptSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger, Microsoft.CodeAnalysis.SourceCodeKind.Regular, experimental)
            End If
        End Function

        Protected Overrides Function VerifyCurrentParameterNameAsync(markupWithPosition As String, expectedParameterName As String, Optional sourceCodeKind As Microsoft.CodeAnalysis.SourceCodeKind? = Nothing) As Threading.Tasks.Task
            If (sourceCodeKind.HasValue) Then
                Return MyBase.VerifyCurrentParameterNameAsync(markupWithPosition, expectedParameterName, sourceCodeKind)
            Else
                Return MyBase.VerifyCurrentParameterNameAsync(markupWithPosition, expectedParameterName, Microsoft.CodeAnalysis.SourceCodeKind.Regular)
            End If
        End Function

        Protected Overrides Function CreateExperimentalParseOptions() As ParseOptions
            ' There are no experimental features at this time.
            Return New VisualBasicParseOptions()
        End Function
    End Class
End Namespace
