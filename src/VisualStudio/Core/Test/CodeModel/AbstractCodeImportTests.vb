' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeImportTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeImport)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeImport) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeImport) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeImport) As String
            Throw New NotSupportedException()
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeImport) As String
            Throw New NotSupportedException()
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeImport) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeImport) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeImport) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeImport) As Action(Of String)
            Return Sub(value) codeElement.Name = value
        End Function

        Protected Sub TestNamespace(code As XElement, expectedNamespace As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of EnvDTE80.CodeImport)()
                Assert.NotNull(codeElement)

                Assert.Equal(expectedNamespace, codeElement.Namespace)
            End Using
        End Sub

    End Class
End Namespace
