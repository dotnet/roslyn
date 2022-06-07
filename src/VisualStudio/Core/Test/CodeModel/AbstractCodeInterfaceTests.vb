' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeInterfaceTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeInterface2)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeInterface2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeInterface2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeInterface2) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeInterface2) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeInterface2) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeInterface2) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeInterface2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeInterface2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeInterface2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeInterface2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetParts(codeElement As EnvDTE80.CodeInterface2) As EnvDTE.CodeElements
            Return codeElement.Parts
        End Function

        Protected Overrides Function AddEvent(codeElement As EnvDTE80.CodeInterface2, data As EventData) As EnvDTE80.CodeEvent
            Return codeElement.AddEvent(data.Name, data.FullDelegateName, data.CreatePropertyStyleEvent, data.Position, data.Access)
        End Function

        Protected Overrides Function AddFunction(codeElement As EnvDTE80.CodeInterface2, data As FunctionData) As EnvDTE.CodeFunction
            Return codeElement.AddFunction(data.Name, data.Kind, data.Type, data.Position, data.Access)
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeInterface2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeInterface2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function AddBase(codeElement As EnvDTE80.CodeInterface2, base As Object, position As Object) As EnvDTE.CodeElement
            Return codeElement.AddBase(base, position)
        End Function

        Protected Overrides Sub RemoveBase(codeElement As EnvDTE80.CodeInterface2, element As Object)
            codeElement.RemoveBase(element)
        End Sub

    End Class
End Namespace
