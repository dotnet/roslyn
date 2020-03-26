' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports EnvDTE

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeEnumTests
        Inherits AbstractCodeElementTests(Of EnvDTE.CodeEnum)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE.CodeEnum) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE.CodeEnum) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE.CodeEnum) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE.CodeEnum) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetBases(codeElement As EnvDTE.CodeEnum) As EnvDTE.CodeElements
            Return codeElement.Bases
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE.CodeEnum) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE.CodeEnum) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE.CodeEnum) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As CodeEnum) As vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE.CodeEnum) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE.CodeEnum) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function AddEnumMember(codeElement As EnvDTE.CodeEnum, data As EnumMemberData) As EnvDTE.CodeVariable
            Return codeElement.AddMember(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE.CodeEnum, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Sub RemoveChild(codeElement As CodeEnum, child As Object)
            codeElement.RemoveMember(child)
        End Sub

        Protected Overrides Function GetAccessSetter(codeElement As EnvDTE.CodeEnum) As Action(Of EnvDTE.vsCMAccess)
            Return Sub(access) codeElement.Access = access
        End Function

        Protected Overrides Function GetNameSetter(codeElement As CodeEnum) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function
    End Class
End Namespace
