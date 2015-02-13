' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports EnvDTE
Imports EnvDTE80
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeParameterTests
        Inherits AbstractCodeElementTests(Of CodeParameter2)

        Protected Overrides Function GetComment(codeElement As CodeParameter2) As String
            Throw New NotImplementedException()
        End Function

        Protected Function GetDefaultValueSetter(codeElement As CodeParameter2) As Action(Of String)
            Return Sub(defaultValue) codeElement.DefaultValue = defaultValue
        End Function

        Protected Overrides Function GetDocComment(codeElement As CodeParameter2) As String
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As CodeParameter2) As Func(Of vsCMPart, TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetFullName(codeElement As CodeParameter2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As CodeParameter2) As vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As CodeParameter2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As CodeParameter2) As Action(Of String)
            Throw New NotImplementedException()
        End Function

        Protected Function GetParameterKindSetter(codeElement As CodeParameter2) As Action(Of EnvDTE80.vsCMParameterKind)
            Return Sub(kind) codeElement.ParameterKind = kind
        End Function

        Protected Overrides Function GetParent(codeElement As CodeParameter2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetStartPointFunc(codeElement As CodeParameter2) As Func(Of vsCMPart, TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetTypeProp(codeElement As CodeParameter2) As CodeTypeRef
            Return codeElement.Type
        End Function

        Protected Overrides Function GetTypePropSetter(codeElement As CodeParameter2) As Action(Of CodeTypeRef)
            Return Sub(value) codeElement.Type = value
        End Function

        Protected Overrides Function AddAttribute(codeElement As CodeParameter2, data As AttributeData) As CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Sub TestParameterKind(code As XElement, kind As vsCMParameterKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of CodeParameter2)()
                Assert.NotNull(codeElement)

                Assert.Equal(kind, codeElement.ParameterKind)
            End Using
        End Sub

        Protected Sub TestDefaultValue(code As XElement, expectedValue As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of CodeParameter2)()
                Assert.NotNull(codeElement)

                Assert.Equal(expectedValue, codeElement.DefaultValue)
            End Using
        End Sub

        Protected Sub TestSetParameterKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMParameterKind)
            TestSetParameterKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMParameterKind)())
        End Sub

        Protected Sub TestSetParameterKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMParameterKind, action As SetterAction(Of EnvDTE80.vsCMParameterKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of CodeParameter2)()
                Assert.NotNull(codeElement)

                Dim parameterKindSetter = GetParameterKindSetter(codeElement)

                action(kind, parameterKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetDefaultValue(code As XElement, expected As XElement, defaultValue As String)
            TestSetDefaultValue(code, expected, defaultValue, NoThrow(Of String)())
        End Sub

        Protected Sub TestSetDefaultValue(code As XElement, expectedCode As XElement, defaultValue As String, action As SetterAction(Of String))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of CodeParameter2)()
                Assert.NotNull(codeElement)

                Dim defaultValueSetter = GetDefaultValueSetter(codeElement)

                action(defaultValue, defaultValueSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub
    End Class
End Namespace
