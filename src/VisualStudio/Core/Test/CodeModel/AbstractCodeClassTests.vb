' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractCodeClassTests
        Inherits AbstractCodeElementTests(Of EnvDTE80.CodeClass2)

        Protected Overrides Function GetStartPointFunc(codeElement As EnvDTE80.CodeClass2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetStartPoint(part)
        End Function

        Protected Overrides Function GetEndPointFunc(codeElement As EnvDTE80.CodeClass2) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
            Return Function(part) codeElement.GetEndPoint(part)
        End Function

        Protected Overrides Function GetAccess(codeElement As EnvDTE80.CodeClass2) As EnvDTE.vsCMAccess
            Return codeElement.Access
        End Function

        Protected Overrides Function GetAccessSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of EnvDTE.vsCMAccess)
            Return Sub(access) codeElement.Access = access
        End Function

        Protected Overrides Function GetAttributes(codeElement As EnvDTE80.CodeClass2) As EnvDTE.CodeElements
            Return codeElement.Attributes
        End Function

        Protected Overrides Function GetBases(codeElement As EnvDTE80.CodeClass2) As EnvDTE.CodeElements
            Return codeElement.Bases
        End Function

        Protected Overrides Function GetClassKind(codeElement As EnvDTE80.CodeClass2) As EnvDTE80.vsCMClassKind
            Return codeElement.ClassKind
        End Function

        Protected Overrides Function GetClassKindSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of EnvDTE80.vsCMClassKind)
            Return Sub(value) codeElement.ClassKind = value
        End Function

        Protected Overrides Function GetComment(codeElement As EnvDTE80.CodeClass2) As String
            Return codeElement.Comment
        End Function

        Protected Overrides Function GetCommentSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of String)
            Return Sub(value) codeElement.Comment = value
        End Function

        Protected Overrides Function GetDataTypeKind(codeElement As EnvDTE80.CodeClass2) As EnvDTE80.vsCMDataTypeKind
            Return codeElement.DataTypeKind
        End Function

        Protected Overrides Function GetDataTypeKindSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of EnvDTE80.vsCMDataTypeKind)
            Return Sub(value) codeElement.DataTypeKind = value
        End Function

        Protected Overrides Function GetDocComment(codeElement As EnvDTE80.CodeClass2) As String
            Return codeElement.DocComment
        End Function

        Protected Overrides Function GetDocCommentSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of String)
            Return Sub(value) codeElement.DocComment = value
        End Function

        Protected Overrides Function GetImplementedInterfaces(codeElement As EnvDTE80.CodeClass2) As EnvDTE.CodeElements
            Return codeElement.ImplementedInterfaces
        End Function

        Protected Overrides Function GetInheritanceKind(codeElement As EnvDTE80.CodeClass2) As EnvDTE80.vsCMInheritanceKind
            Return codeElement.InheritanceKind
        End Function

        Protected Overrides Function GetInheritanceKindSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of EnvDTE80.vsCMInheritanceKind)
            Return Sub(value) codeElement.InheritanceKind = value
        End Function

        Protected Overrides Function GetIsAbstract(codeElement As EnvDTE80.CodeClass2) As Boolean
            Return codeElement.IsAbstract
        End Function

        Protected Overrides Function GetIsAbstractSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of Boolean)
            Return Sub(value) codeElement.IsAbstract = value
        End Function

        Protected Overrides Function GetIsGeneric(codeElement As EnvDTE80.CodeClass2) As Boolean
            Return codeElement.IsGeneric
        End Function

        Protected Overrides Function GetIsShared(codeElement As EnvDTE80.CodeClass2) As Boolean
            Return codeElement.IsShared
        End Function

        Protected Overrides Function GetIsSharedSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of Boolean)
            Return Sub(value) codeElement.IsShared = value
        End Function

        Protected Overrides Function GetFullName(codeElement As EnvDTE80.CodeClass2) As String
            Return codeElement.FullName
        End Function

        Protected Overrides Function GetKind(codeElement As EnvDTE80.CodeClass2) As EnvDTE.vsCMElement
            Return codeElement.Kind
        End Function

        Protected Overrides Function GetName(codeElement As EnvDTE80.CodeClass2) As String
            Return codeElement.Name
        End Function

        Protected Overrides Function GetNameSetter(codeElement As EnvDTE80.CodeClass2) As Action(Of String)
            Return Sub(name) codeElement.Name = name
        End Function

        Protected Overrides Function GetNamespace(codeElement As EnvDTE80.CodeClass2) As EnvDTE.CodeNamespace
            Return codeElement.Namespace
        End Function

        Protected Overrides Function GetParent(codeElement As EnvDTE80.CodeClass2) As Object
            Return codeElement.Parent
        End Function

        Protected Overrides Function GetParts(codeElement As EnvDTE80.CodeClass2) As EnvDTE.CodeElements
            Return codeElement.Parts
        End Function

        Protected Overrides Function IsDerivedFrom(codeElement As EnvDTE80.CodeClass2, fullName As String) As Boolean
            Return codeElement.IsDerivedFrom(fullName)
        End Function

        Protected Overrides Function AddAttribute(codeElement As EnvDTE80.CodeClass2, data As AttributeData) As EnvDTE.CodeAttribute
            Return codeElement.AddAttribute(data.Name, data.Value, data.Position)
        End Function

        Protected Overrides Function AddEvent(codeElement As EnvDTE80.CodeClass2, data As EventData) As EnvDTE80.CodeEvent
            Return codeElement.AddEvent(data.Name, data.FullDelegateName, data.CreatePropertyStyleEvent, data.Position, data.Access)
        End Function

        Protected Overrides Function AddFunction(codeElement As EnvDTE80.CodeClass2, data As FunctionData) As EnvDTE.CodeFunction
            Return codeElement.AddFunction(data.Name, data.Kind, data.Type, data.Position, data.Access, data.Location)
        End Function

        Protected Overrides Function AddProperty(codeElement As EnvDTE80.CodeClass2, data As PropertyData) As EnvDTE.CodeProperty
            Return codeElement.AddProperty(data.GetterName, data.PutterName, data.Type, data.Position, data.Access, data.Location)
        End Function

        Protected Overrides Function AddVariable(codeElement As EnvDTE80.CodeClass2, data As VariableData) As EnvDTE.CodeVariable
            Return codeElement.AddVariable(data.Name, data.Type, data.Position, data.Access, data.Location)
        End Function

        Protected Overrides Sub RemoveChild(codeElement As EnvDTE80.CodeClass2, child As Object)
            codeElement.RemoveMember(child)
        End Sub

        Protected Overrides Function AddBase(codeElement As EnvDTE80.CodeClass2, base As Object, position As Object) As EnvDTE.CodeElement
            Return codeElement.AddBase(base, position)
        End Function

        Protected Overrides Sub RemoveBase(codeElement As EnvDTE80.CodeClass2, element As Object)
            codeElement.RemoveBase(element)
        End Sub

        Protected Overrides Function AddImplementedInterface(codeElement As EnvDTE80.CodeClass2, base As Object, position As Object) As EnvDTE.CodeInterface
            Return codeElement.AddImplementedInterface(base, position)
        End Function

        Protected Overrides Sub RemoveImplementedInterface(codeElement As EnvDTE80.CodeClass2, element As Object)
            codeElement.RemoveInterface(element)
        End Sub

        Protected Sub TestGetBaseName(code As XElement, expectedBaseName As String)
            TestElement(code,
                Sub(codeClass)
                    Dim codeClassBase = TryCast(codeClass, ICodeClassBase)
                    Assert.NotNull(codeClassBase)

                    Dim baseName As String = Nothing
                    Dim hRetVal = codeClassBase.GetBaseName(baseName)
                    Assert.Equal(VSConstants.S_OK, hRetVal)

                    Assert.Equal(expectedBaseName, baseName)
                End Sub)
        End Sub
    End Class
End Namespace
