' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeNamespaceTests
        Inherits AbstractCodeElementTests(Of EnvDTE.CodeNamespace)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE.CodeNamespace) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE.CodeNamespace) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE.CodeNamespace) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetCommentSetter(codeElement As EnvDTE.CodeNamespace) As Action(Of String)
            Return Sub(value) codeElement.Comment = value
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE.CodeNamespace) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetDocCommentSetter(codeElement As EnvDTE.CodeNamespace) As Action(Of String)
            Return Sub(value) codeElement.DocComment = value
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE.CodeNamespace) As String
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE.CodeNamespace) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE.CodeNamespace) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE.CodeNamespace) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE.CodeNamespace) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Sub RemoveChild(codeElement As EnvDTE.CodeNamespace, member As Object)
            codeElement.Remove(member)
        End Sub

    End Class
End Namespace
