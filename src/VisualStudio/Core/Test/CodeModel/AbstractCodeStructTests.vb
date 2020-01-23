' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeStructTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeStruct2)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeStruct2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeStruct2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetBases(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.CodeElements
            Return codeElement.Bases
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeStruct2) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetDataTypeKind(codeElement As EnvDTE80.CodeStruct2) As EnvDTE80.vsCMDataTypeKind
            Return codeElement.DataTypeKind
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeStruct2) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeStruct2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetImplementedInterfaces(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.CodeElements
            Return codeElement.ImplementedInterfaces
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeStruct2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeStruct2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetParts(codeElement As EnvDTE80.CodeStruct2) As EnvDTE.CodeElements
            Return codeElement.Parts
        End Function

        Protected Overrides Function AddFunction(codeElement As EnvDTE80.CodeStruct2, data As FunctionData) As EnvDTE.CodeFunction
            Return codeElement.AddFunction(data.Name, data.Kind, data.Type, data.Position, data.Access, data.Location)
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeStruct2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeStruct2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function AddImplementedInterface(codeElement As EnvDTE80.CodeStruct2, base As Object, position As Object) As EnvDTE.CodeInterface
            Return codeElement.AddImplementedInterface(base, position)
        End Function

        Protected Overrides Sub RemoveImplementedInterface(codeElement As EnvDTE80.CodeStruct2, element As Object)
            codeElement.RemoveInterface(element)
        End Sub

    End Class
End Namespace
