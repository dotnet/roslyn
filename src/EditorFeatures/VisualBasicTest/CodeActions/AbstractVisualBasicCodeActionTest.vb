' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

    Public MustInherit Class AbstractVisualBasicCodeActionTest
        Inherits AbstractCodeActionTest

        Private ReadOnly _compilationOptions As CompilationOptions =
            New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionInfer(True)

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(
            definition As String,
            parseOptions As ParseOptions,
            compilationOptions As CompilationOptions
        ) As TestWorkspace

            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(
                definition,
                parseOptions,
                If(compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Shared Function NewLines(input As String) As String
            Return input.Replace("\n", vbCrLf)
        End Function

        Protected Overloads Sub Test(initialMarkup As XElement, expected As XElement, Optional index As Integer = 0, Optional compareTokens As Boolean = True)
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()
            Dim expectedStr = expected.ConvertTestSourceTag()

            MyBase.Test(initialMarkupStr, expectedStr, parseOptions:=Nothing, compilationOptions:=_compilationOptions, index:=index, compareTokens:=compareTokens)
        End Sub

        Protected Overloads Sub TestMissing(initialMarkup As XElement)
            Dim initialMarkupStr = initialMarkup.ConvertTestSourceTag()

            MyBase.TestMissing(initialMarkupStr, parseOptions:=Nothing, compilationOptions:=_compilationOptions)
        End Sub

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
