' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.MoveType
Imports Microsoft.CodeAnalysis.Editor.UnitTests.MoveType
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Public Class BasicMoveTypeTestsBase
        Inherits AbstractMoveTypeTest

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(
                initialMarkup,
                parameters.parseOptions,
                If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overloads Function TestRenameTypeToMatchFileAsync(
            originalCode As XElement,
            Optional expectedCode As XElement = Nothing,
            Optional expectedCodeAction As Boolean = True
        ) As Task

            Dim expectedText As String = Nothing
            If Not expectedCode Is Nothing Then
                expectedText = expectedCode.ConvertTestSourceTag()
            End If

            Return MyBase.TestRenameTypeToMatchFileAsync(
                originalCode.ConvertTestSourceTag(), expectedText, expectedCodeAction)
        End Function

        Protected Overloads Function TestRenameFileToMatchTypeAsync(
            originalCode As XElement,
            Optional expectedDocumentName As String = Nothing,
            Optional expectedCodeAction As Boolean = True
        ) As Task

            Return MyBase.TestRenameFileToMatchTypeAsync(
                originalCode.ConvertTestSourceTag(), expectedDocumentName, expectedCodeAction)
        End Function

        Protected Overloads Function TestMoveTypeToNewFileAsync(
            originalCode As XElement,
            expectedSourceTextAfterRefactoring As XElement,
            expectedDocumentName As String,
            destinationDocumentText As XElement,
            Optional destinationDocumentContainers As ImmutableArray(Of String) = Nothing,
            Optional expectedCodeAction As Boolean = True,
            Optional index As Integer = 0
        ) As Task

            Dim originalCodeText = originalCode.ConvertTestSourceTag()
            Dim expectedSourceText = expectedSourceTextAfterRefactoring.ConvertTestSourceTag()
            Dim expectedDestinationText = destinationDocumentText.ConvertTestSourceTag()

            Return MyBase.TestMoveTypeToNewFileAsync(
                originalCodeText,
                expectedSourceText,
                expectedDocumentName,
                expectedDestinationText,
                destinationDocumentContainers,
                expectedCodeAction,
                index)
        End Function
    End Class
End Namespace
