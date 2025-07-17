' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.MoveType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Public Class BasicMoveTypeTestsBase
        Inherits AbstractMoveTypeTest

        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Return parameters.WithCompilationOptions(If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
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
            If expectedCode IsNot Nothing Then
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
