' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractFileCodeModelTests
        Inherits AbstractCodeModelObjectTests(Of EnvDTE80.FileCodeModel2)

        Protected Async Function TestOperation(code As XElement, expectedCode As XElement, operation As Action(Of EnvDTE80.FileCodeModel2)) As Task
            Roslyn.Test.Utilities.WpfTestCase.RequireWpfFact($"Test calls TestOperation which means we're creating new CodeModel elements.")

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                operation(fileCodeModel)

                Dim text = (Await state.GetDocumentAtCursor().GetTextAsync()).ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()
                operation(fileCodeModel)
                fileCodeModel.EndBatch()

                Dim text = (Await state.GetDocumentAtCursor().GetTextAsync()).ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Function

        Protected Async Function TestOperation(code As XElement, operation As Action(Of EnvDTE80.FileCodeModel2)) As Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                operation(fileCodeModel)
            End Using

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()
                operation(fileCodeModel)
                fileCodeModel.EndBatch()
            End Using
        End Function

        Protected Overrides Async Function TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object)) As Task
            Await TestOperation(code,
                Sub(fileCodeModel)
                    Dim children = fileCodeModel.CodeElements
                    Assert.Equal(expectedChildren.Length, children.Count)

                    For i = 1 To children.Count
                        expectedChildren(i - 1)(children.Item(i))
                    Next
                End Sub)
        End Function

        Protected Overrides Async Function TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newAttribute = fileCodeModel.AddAttribute(data.Name, data.Value, data.Position)
                    Assert.NotNull(newAttribute)
                    Assert.Equal(data.Name, newAttribute.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddClass(code As XElement, expectedCode As XElement, data As ClassData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newClass = fileCodeModel.AddClass(data.Name, data.Position, data.Bases, data.ImplementedInterfaces, data.Access)
                    Assert.NotNull(newClass)
                    Assert.Equal(data.Name, newClass.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddDelegate(code As XElement, expectedCode As XElement, data As DelegateData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newDelegate = fileCodeModel.AddDelegate(data.Name, data.Type, data.Position, data.Access)
                    Assert.NotNull(newDelegate)
                    Assert.Equal(data.Name, newDelegate.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddEnum(code As XElement, expectedCode As XElement, data As EnumData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newEnum = fileCodeModel.AddEnum(data.Name, data.Position, data.Base, data.Access)
                    Assert.NotNull(newEnum)
                    Assert.Equal(data.Name, newEnum.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                        Sub()
                            fileCodeModel.AddFunction(data.Name, data.Kind, data.Type, data.Position, data.Access)
                        End Sub)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddImport(code As XElement, expectedCode As XElement, data As ImportData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newImport = fileCodeModel.AddImport(data.Namespace, data.Position, data.Alias)
                    Assert.NotNull(newImport)
                    Assert.Equal(data.Namespace, newImport.Namespace)

                    If data.Alias IsNot Nothing Then
                        Assert.Equal(data.Alias, newImport.Alias)
                    End If
                End Sub)
        End Function

        Protected Overrides Async Function TestAddInterface(code As XElement, expectedCode As XElement, data As InterfaceData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newInterface = fileCodeModel.AddInterface(data.Name, data.Position, data.Bases, data.Access)
                    Assert.NotNull(newInterface)
                    Assert.Equal(data.Name, newInterface.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddNamespace(code As XElement, expectedCode As XElement, data As NamespaceData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newNamespace = fileCodeModel.AddNamespace(data.Name, data.Position)
                    Assert.NotNull(newNamespace)
                    Assert.Equal(data.Name, newNamespace.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddStruct(code As XElement, expectedCode As XElement, data As StructData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Dim newStruct = fileCodeModel.AddStruct(data.Name, data.Position, data.Bases, data.ImplementedInterfaces, data.Access)
                    Assert.NotNull(newStruct)
                    Assert.Equal(data.Name, newStruct.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                        Sub()
                            fileCodeModel.AddVariable(data.Name, data.Type, data.Position, data.Access)
                        End Sub)
                End Sub)
        End Function

        Protected Overrides Async Function TestRemoveChild(code As XElement, expectedCode As XElement, element As Object) As Task
            Await TestOperation(code, expectedCode,
                Sub(fileCodeModel)
                    fileCodeModel.Remove(element)
                End Sub)
        End Function

    End Class
End Namespace
