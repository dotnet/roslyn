' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeElementTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeElement2)

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeElement2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeElement2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeElement2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeElement2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeElement2) As Action(Of String)
            Return Function(value) codeElement.Name = value
        End Function

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeElement2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

    End Class
End Namespace
