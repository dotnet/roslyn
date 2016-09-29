' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports EnvDTE80

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeEventTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeEvent)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeEvent) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeEvent) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeEvent) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeEvent) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeEvent) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeEvent) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeEvent) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetIsShared(codeElement As EnvDTE80.CodeEvent) As Boolean
            Return codeElement.IsShared
        End Function

        Protected Overrides Function GetIsSharedSetter(codeElement As EnvDTE80.CodeEvent) As Action(Of Boolean)
            Return Sub(value) codeElement.IsShared = value
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeEvent) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeEvent) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeEvent) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function GetOverrideKind(codeElement As CodeEvent) As vsCMOverrideKind
            Return codeElement.OverrideKind
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeEvent) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetPrototype(codeElement As EnvDTE80.CodeEvent, flags As EnvDTE.vsCMPrototype) As String
            Return codeElement.Prototype(flags)
        End Function

        Protected Overrides Function GetTypeProp(codeElement As EnvDTE80.CodeEvent) As EnvDTE.CodeTypeRef
            Return codeElement.Type
        End Function

        Protected Overrides Function GetTypePropSetter(codeElement As EnvDTE80.CodeEvent) As Action(Of EnvDTE.CodeTypeRef)
            Return Sub(value) codeElement.Type = value
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeEvent, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Async Function TestIsPropertyStyleEvent(code As XElement, expected As Boolean) As Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, codeElement.IsPropertyStyleEvent)
                End Sub)
        End Function

    End Class
End Namespace
