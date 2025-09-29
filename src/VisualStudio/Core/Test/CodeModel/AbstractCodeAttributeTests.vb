' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeAttributeTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeAttribute2)

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeAttribute2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeAttribute2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeAttribute2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeAttribute2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeAttribute2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeAttribute2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeAttribute2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overridable Sub Delete(codeElement As EnvDTE80.CodeAttribute2)
            codeElement.Delete()
        End Sub

        Protected Overridable Function GetAttributeArguments(codeElement As EnvDTE80.CodeAttribute2) As EnvDTE.CodeElements
            Return codeElement.Arguments
        End Function

        Protected Overridable Function GetTarget(codeElement As EnvDTE80.CodeAttribute2) As String
            Return codeElement.Target
        End Function

        Protected Overridable Function GetValue(codeElement As EnvDTE80.CodeAttribute2) As String
            Return codeElement.Value
        End Function

        Protected Overridable Function AddAttributeArgument(codeElement As EnvDTE80.CodeAttribute2, data As AttributeArgumentData) As EnvDTE80.CodeAttributeArgument
            Return codeElement.AddArgument(data.Value, data.Name, data.Position)
        End Function

        Protected Sub TestAttributeArguments(code As XElement, ParamArray expectedAttributeArguments() As Action(Of Object))
            TestElement(code,
                Sub(codeElement)
                    Dim attributes = GetAttributeArguments(codeElement)
                    Assert.Equal(expectedAttributeArguments.Length, attributes.Count)

                    For i = 1 To attributes.Count
                        expectedAttributeArguments(i - 1)(attributes.Item(i))
                    Next
                End Sub)
        End Sub

        Protected Sub TestTarget(code As XElement, expectedTarget As String)
            TestElement(code,
                Sub(codeElement)
                    Dim target = GetTarget(codeElement)
                    Assert.Equal(expectedTarget, target)
                End Sub)
        End Sub

        Protected Async Function TestSetTarget(code As XElement, expectedCode As XElement, target As String) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    codeElement.Target = target
                End Sub)
        End Function

        Protected Sub TestValue(code As XElement, expectedValue As String)
            TestElement(code,
                Sub(codeElement)
                    Dim target = GetValue(codeElement)
                    Assert.Equal(expectedValue, target)
                End Sub)
        End Sub

        Protected Async Function TestSetValue(code As XElement, expectedCode As XElement, value As String) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    codeElement.Value = value
                End Sub)
        End Function

        Protected Async Function TestAddAttributeArgument(code As XElement, expectedCode As XElement, data As AttributeArgumentData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim attributeArgument = AddAttributeArgument(codeElement, data)
                    Assert.NotNull(attributeArgument)
                    Assert.Equal(data.Name, attributeArgument.Name)
                End Sub)
        End Function

        Protected Async Function TestDelete(code As XElement, expectedCode As XElement) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    codeElement.Delete()
                End Sub)
        End Function

        Protected Async Function TestDeleteAttributeArgument(code As XElement, expectedCode As XElement, indexToDelete As Integer) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim argument = CType(codeElement.Arguments.Item(indexToDelete), EnvDTE80.CodeAttributeArgument)
                    argument.Delete()
                End Sub)
        End Function

        Protected Function IsAttributeArgument(Optional name As String = Nothing, Optional value As String = Nothing) As Action(Of Object)
            Return _
                Sub(o)
                    Dim a = TryCast(o, EnvDTE80.CodeAttributeArgument)
                    Assert.NotNull(a)

                    If name IsNot Nothing Then
                        Assert.Equal(name, a.Name)
                    End If

                    If value IsNot Nothing Then
                        Assert.Equal(value, a.Value)
                    End If
                End Sub
        End Function

        Protected Sub TestAttributeArgumentStartPoint(code As XElement, index As Integer, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)))
            TestElement(code,
                Sub(codeElement)
                    Dim arg = CType(codeElement.Arguments.Item(index), EnvDTE80.CodeAttributeArgument)

                    Dim startPointGetter = Function(part As EnvDTE.vsCMPart) arg.GetStartPoint(part)

                    For Each action In expectedParts
                        action(startPointGetter)
                    Next
                End Sub)
        End Sub

        Protected Sub TestAttributeArgumentEndPoint(code As XElement, index As Integer, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)))
            TestElement(code,
                Sub(codeElement)
                    Dim arg = CType(codeElement.Arguments.Item(index), EnvDTE80.CodeAttributeArgument)

                    Dim endPointGetter = Function(part As EnvDTE.vsCMPart) arg.GetEndPoint(part)

                    For Each action In expectedParts
                        action(endPointGetter)
                    Next
                End Sub)
        End Sub

    End Class
End Namespace

