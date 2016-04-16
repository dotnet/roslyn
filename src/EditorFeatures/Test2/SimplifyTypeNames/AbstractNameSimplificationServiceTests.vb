' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Compilers
Imports Microsoft.CodeAnalysis.Services.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Services.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Services.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Services.Editor.UnitTests.CodeActions.SimplifyTypeNames
    Partial Public Class AbstractNameSimplificationServiceTests

        Protected Shadows Sub Test(definition As XElement, expected As String, Optional codeActionIndex As Integer = 0, Optional compareTokens As Boolean = True)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(definition)
                Dim document = GetDocument(workspace)

                Dim updatedRoot = SimplificationService.Simplify(document).GetSyntaxRoot()
                Dim language As String = document.LanguageServices.Language

                Dim actualText = If(compareTokens, updatedRoot.ToString(), updatedRoot.ToFullString())

                If compareTokens Then
                    AssertEx.TokensAreEqual(expected, actualText, If(language = LanguageNames.VisualBasic, LanguageNames.CSharp, LanguageNames.VisualBasic))
                Else
                    Assert.Equal(expected, actualText)
                End If
            End Using
        End Sub

        Protected Shared Function GetDocument(workspace As TestWorkspace) As Document
            Dim buffer = workspace.Documents.First().TextBuffer

            Dim document As Document = Nothing
            workspace.CurrentSolution.TryGetDocumentWithSpecificText(buffer.CurrentSnapshot.AsText(), document)

            Return document
        End Function
    End Class
End Namespace
