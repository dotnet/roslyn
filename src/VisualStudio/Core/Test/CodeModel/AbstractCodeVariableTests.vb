' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeVariableTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeVariable2)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeVariable2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeVariable2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeVariable2) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeVariable2) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeVariable2) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetConstKind(codeElement As EnvDTE80.CodeVariable2) As EnvDTE80.vsCMConstKind
            Return codeElement.ConstKind
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeVariable2) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeVariable2) As String
            Return codeElement.FullName
        End Function

        Protected Function GetInitExpression(codeElement As EnvDTE80.CodeVariable2) As Object
            Return codeElement.InitExpression
        End Function

        Protected Function SetInitExpressionSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of Object)
            Return Sub(initExpression) codeElement.InitExpression = initExpression
        End Function

        Protected Overrides Function GetIsShared(codeElement As EnvDTE80.CodeVariable2) As Boolean
            Return codeElement.IsShared
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeVariable2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeVariable2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeVariable2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetPrototype(codeElement As EnvDTE80.CodeVariable2, flags As EnvDTE.vsCMPrototype) As String
            Return codeElement.Prototype(flags)
        End Function

        Protected Overrides Function GetAccessSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of EnvDTE.vsCMAccess)
            Return Sub(access) codeElement.Access = access
        End Function

        Protected Overrides Function GetConstKindSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of EnvDTE80.vsCMConstKind)
            Return Sub(value) codeElement.ConstKind = value
        End Function

        Protected Overrides Function GetIsSharedSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of Boolean)
            Return Sub(value) codeElement.IsShared = value
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeVariable2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function GetTypeProp(codeElement As EnvDTE80.CodeVariable2) As EnvDTE.CodeTypeRef
            Return codeElement.Type
        End Function

        Protected Overrides Function GetTypePropSetter(codeElement As EnvDTE80.CodeVariable2) As Action(Of EnvDTE.CodeTypeRef)
            Return Sub(value) codeElement.Type = value
        End Function

        Protected Sub TestIsConstant(code As XElement, expected As Boolean)
            TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, codeElement.IsConstant)
                End Sub)
        End Sub

        Protected Sub TestSetIsConstant(code As XElement, expectedCode As XElement, value As Boolean)
            TestSetIsConstant(code, expectedCode, value, NoThrow(Of Boolean)())
        End Sub

        Protected Sub TestSetIsConstant(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean))
            TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    action(value, Sub(v) codeElement.IsConstant = v)
                End Sub)
        End Sub

        Protected Sub TestInitExpression(code As XElement, expected As Object)
            TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, codeElement.InitExpression)
                End Sub)
        End Sub

        Protected Sub TestSetInitExpression(code As XElement, expectedCode As XElement, value As Object)
            TestSetInitExpression(code, expectedCode, value, NoThrow(Of Object)())
        End Sub

        Protected Sub TestSetInitExpression(code As XElement, expectedCode As XElement, value As Object, action As SetterAction(Of Object))
            TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    action(value, Sub(v) codeElement.InitExpression = v)
                End Sub)
        End Sub

    End Class
End Namespace
