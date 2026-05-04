' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeParameterTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeParameter2)

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeParameter2) As String
            Throw New NotImplementedException()
        End Function

        Protected Function GetDefaultValueSetter(codeElement As EnvDTE80.CodeParameter2) As Action(Of String)
            Return Sub(defaultValue) codeElement.DefaultValue = defaultValue
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeParameter2) As String
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeParameter2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeParameter2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeParameter2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeParameter2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeParameter2) As Action(Of String)
            Throw New NotImplementedException()
        End Function

        Protected Function GetParameterKindSetter(codeElement As EnvDTE80.CodeParameter2) As Action(Of EnvDTE80.vsCMParameterKind)
            Return Sub(kind) codeElement.ParameterKind = kind
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeParameter2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeParameter2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetTypeProp(codeElement As EnvDTE80.CodeParameter2) As EnvDTE.CodeTypeRef
            Return codeElement.Type
        End Function

        Protected Overrides Function GetTypePropSetter(codeElement As EnvDTE80.CodeParameter2) As Action(Of EnvDTE.CodeTypeRef)
            Return Sub(value) codeElement.Type = value
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeParameter2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Sub TestParameterKind(code As XElement, kind As EnvDTE80.vsCMParameterKind)
            TestElement(code,
                Sub(codeElement)
                    Assert.Equal(kind, codeElement.ParameterKind)
                End Sub)
        End Sub

        Protected Sub TestDefaultValue(code As XElement, expectedValue As String)
            TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expectedValue, codeElement.DefaultValue)
                End Sub)
        End Sub

        Protected Async Function TestSetParameterKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMParameterKind) As Task
            Await TestSetParameterKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMParameterKind)())
        End Function

        Protected Async Function TestSetParameterKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMParameterKind, action As SetterAction(Of EnvDTE80.vsCMParameterKind)) As Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim parameterKindSetter = GetParameterKindSetter(codeElement)
                    action(kind, parameterKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetDefaultValue(code As XElement, expected As XElement, defaultValue As String) As Task
            Await TestSetDefaultValue(code, expected, defaultValue, NoThrow(Of String)())
        End Function

        Protected Async Function TestSetDefaultValue(code As XElement, expectedCode As XElement, defaultValue As String, action As SetterAction(Of String)) As Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim defaultValueSetter = GetDefaultValueSetter(codeElement)
                    action(defaultValue, defaultValueSetter)
                End Sub)
        End Function

        Friend Sub TestGetParameterPassingMode(code As XElement, expected As PARAMETER_PASSING_MODE)
            TestElement(code,
                Sub(codeElement)
                    Dim parameterKind = TryCast(codeElement, IParameterKind)
                    Assert.NotNull(parameterKind)

                    Dim actual = parameterKind.GetParameterPassingMode()
                    Assert.Equal(expected, actual)
                End Sub)
        End Sub

        Friend Async Function TestSetParameterPassingMode(code As XElement, expectedCode As XElement, passingMode As PARAMETER_PASSING_MODE) As Task
            Await TestSetParameterPassingMode(code, expectedCode, passingMode, NoThrow(Of PARAMETER_PASSING_MODE)())
        End Function

        Friend Async Function TestSetParameterPassingMode(code As XElement, expectedCode As XElement, passingMode As PARAMETER_PASSING_MODE, action As SetterAction(Of PARAMETER_PASSING_MODE)) As Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim setter = Sub(mode As PARAMETER_PASSING_MODE)
                                     Dim parameterKind = TryCast(codeElement, IParameterKind)
                                     Assert.NotNull(parameterKind)

                                     parameterKind.SetParameterPassingMode(mode)
                                 End Sub

                    action(passingMode, setter)
                End Sub)
        End Function

        Friend Sub TestGetParameterArrayCount(code As XElement, expected As Integer)
            TestElement(code,
                Sub(codeElement)
                    Dim parameterKind = TryCast(codeElement, IParameterKind)
                    Assert.NotNull(parameterKind)

                    Dim actual = parameterKind.GetParameterArrayCount()
                    Assert.Equal(expected, actual)
                End Sub)
        End Sub

        Friend Sub TestGetParameterArrayDimensions(code As XElement, index As Integer, expected As Integer)
            TestElement(code,
                Sub(codeElement)
                    Dim parameterKind = TryCast(codeElement, IParameterKind)
                    Assert.NotNull(parameterKind)

                    Dim actual = parameterKind.GetParameterArrayDimensions(index)
                    Assert.Equal(expected, actual)
                End Sub)
        End Sub

        Friend Async Function TestSetParameterArrayDimensions(code As XElement, expectedCode As XElement, dimensions As Integer) As Task
            Await TestSetParameterArrayDimensions(code, expectedCode, dimensions, NoThrow(Of Integer)())
        End Function

        Friend Async Function TestSetParameterArrayDimensions(code As XElement, expectedCode As XElement, dimensions As Integer, action As SetterAction(Of Integer)) As Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim setter = Sub(d As Integer)
                                     Dim parameterKind = TryCast(codeElement, IParameterKind)
                                     Assert.NotNull(parameterKind)

                                     parameterKind.SetParameterArrayDimensions(d)
                                 End Sub

                    action(dimensions, setter)
                End Sub)
        End Function
    End Class
End Namespace
