' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeDelegateTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeDelegate2)

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeDelegate2) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeDelegate2) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeDelegate2) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeDelegate2) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeDelegate2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeDelegate2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeDelegate2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeDelegate2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeDelegate2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeDelegate2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetPrototype(codeElement As EnvDTE80.CodeDelegate2, flags As EnvDTE.vsCMPrototype) As String
            Return codeElement.Prototype(flags)
        End Function

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeDelegate2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetTypeProp(codeElement As EnvDTE80.CodeDelegate2) As EnvDTE.CodeTypeRef
            Return codeElement.Type
        End Function

        Protected Overrides Function GetTypePropSetter(codeElement As EnvDTE80.CodeDelegate2) As Action(Of EnvDTE.CodeTypeRef)
            Return Sub(value) codeElement.Type = value
        End Function

        Protected Overrides Function AddParameter(codeElement As EnvDTE80.CodeDelegate2, data As ParameterData) As EnvDTE.CodeParameter
            Return codeElement.AddParameter(data.Name, data.Type, data.Position)
        End Function

        Protected Overrides Function GetParameters(codeElement As EnvDTE80.CodeDelegate2) As EnvDTE.CodeElements
            Return codeElement.Parameters
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeDelegate2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Sub RemoveChild(codeElement As EnvDTE80.CodeDelegate2, child As Object)
            codeElement.RemoveParameter(child)
        End Sub

        Protected Sub TestBaseClass(code As XElement, expectedFullName As String)
            TestElement(code,
                Sub(codeElement)
                    Assert.NotNull(codeElement.BaseClass)
                    Assert.Equal(expectedFullName, codeElement.BaseClass.FullName)
                End Sub)
        End Sub

    End Class
End Namespace
