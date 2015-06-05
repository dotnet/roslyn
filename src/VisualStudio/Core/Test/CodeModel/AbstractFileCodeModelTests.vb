' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractFileCodeModelTests
        Inherits AbstractCodeModelObjectTests(Of EnvDTE80.FileCodeModel2)

        Protected Sub TestOperation(code As XElement, expectedCode As XElement, operation As Action(Of EnvDTE80.FileCodeModel2))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                operation(fileCodeModel)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()
                operation(fileCodeModel)
                fileCodeModel.EndBatch()

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestOperation(code As XElement, operation As Action(Of EnvDTE80.FileCodeModel2))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                operation(fileCodeModel)
            End Using

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()
                operation(fileCodeModel)
                fileCodeModel.EndBatch()
            End Using
        End Sub

        Protected Overrides Sub TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newAttribute = fileCodeModel.AddAttribute(data.Name, data.Value, data.Position)
                    Assert.NotNull(newAttribute)
                    Assert.Equal(data.Name, newAttribute.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddClass(code As XElement, expectedCode As XElement, data As ClassData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newClass = fileCodeModel.AddClass(data.Name, data.Position, data.Bases, data.ImplementedInterfaces, data.Access)
                    Assert.NotNull(newClass)
                    Assert.Equal(data.Name, newClass.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddDelegate(code As XElement, expectedCode As XElement, data As DelegateData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newDelegate = fileCodeModel.AddDelegate(data.Name, data.Type, data.Position, data.Access)
                    Assert.NotNull(newDelegate)
                    Assert.Equal(data.Name, newDelegate.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddEnum(code As XElement, expectedCode As XElement, data As EnumData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newEnum = fileCodeModel.AddEnum(data.Name, data.Position, data.Base, data.Access)
                    Assert.NotNull(newEnum)
                    Assert.Equal(data.Name, newEnum.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                        Sub()
                            fileCodeModel.AddFunction(data.Name, data.Kind, data.Type, data.Position, data.Access)
                        End Sub)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddImport(code As XElement, expectedCode As XElement, data As ImportData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newImport = fileCodeModel.AddImport(data.Namespace, data.Position, data.Alias)
                    Assert.NotNull(newImport)
                    Assert.Equal(data.Namespace, newImport.Namespace)

                    If data.Alias IsNot Nothing Then
                        Assert.Equal(data.Alias, newImport.Alias)
                    End If
                End Sub)
        End Sub

        Protected Overrides Sub TestAddInterface(code As XElement, expectedCode As XElement, data As InterfaceData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newInterface = fileCodeModel.AddInterface(data.Name, data.Position, data.Bases, data.Access)
                    Assert.NotNull(newInterface)
                    Assert.Equal(data.Name, newInterface.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddNamespace(code As XElement, expectedCode As XElement, data As NamespaceData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newNamespace = fileCodeModel.AddNamespace(data.Name, data.Position)
                    Assert.NotNull(newNamespace)
                    Assert.Equal(data.Name, newNamespace.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddStruct(code As XElement, expectedCode As XElement, data As StructData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newStruct = fileCodeModel.AddStruct(data.Name, data.Position, data.Bases, data.ImplementedInterfaces, data.Access)
                    Assert.NotNull(newStruct)
                    Assert.Equal(data.Name, newStruct.Name)
                End Sub)
        End Sub

        Protected Overrides Sub TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                        Sub()
                            fileCodeModel.AddVariable(data.Name, data.Type, data.Position, data.Access)
                        End Sub)
                End Sub)
        End Sub

        Protected Overrides Sub TestRemoveChild(code As XElement, expectedCode As XElement, element As Object)
            TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    fileCodeModel.Remove(element)
                End Sub)
        End Sub

    End Class
End Namespace
