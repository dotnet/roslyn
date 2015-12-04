' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public MustInherit Class AbstractVisualBasicSignatureHelpProviderTests
        Inherits AbstractSignatureHelpProviderTests(Of VisualBasicTestWorkspaceFixture)

        ' We want to skip script testing in all VB stuff for now.

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

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

        Protected Overrides Sub VerifyTriggerCharacters(expectedTriggerCharacters() As Char, unexpectedTriggerCharacters() As Char, Optional sourceCodeKind As Microsoft.CodeAnalysis.SourceCodeKind? = Nothing)
            If (sourceCodeKind.HasValue) Then
                MyBase.VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters, sourceCodeKind)
            Else
                MyBase.VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters, Microsoft.CodeAnalysis.SourceCodeKind.Regular)
            End If
        End Sub

        Protected Overrides Function CreateExperimentalParseOptions() As ParseOptions
            ' There are no experimental features at this time.
            Return New VisualBasicParseOptions()
        End Function
    End Class
End Namespace
