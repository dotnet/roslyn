' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractRootCodeModelTests
        Inherits AbstractCodeModelObjectTests(Of EnvDTE80.CodeModel2)

        Protected Async Function TestRootCodeModel(workspaceDefinition As XElement, action As Action(Of EnvDTE80.CodeModel2)) As Task
            Using state = Await CreateCodeModelTestStateAsync(workspaceDefinition)
                Dim rootCodeModel = TryCast(state.RootCodeModel, EnvDTE80.CodeModel2)
                Assert.NotNull(rootCodeModel)

                action(rootCodeModel)
            End Using
        End Function

        Protected Async Function TestRootCodeModelWithCodeFile(code As XElement, action As Action(Of EnvDTE80.CodeModel2)) As Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim rootCodeModel = TryCast(state.RootCodeModel, EnvDTE80.CodeModel2)
                Assert.NotNull(rootCodeModel)

                action(rootCodeModel)
            End Using
        End Function

        Protected Overrides Async Function TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object)) As Task
            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim codeElements = rootCodeModel.CodeElements
                    Assert.Equal(expectedChildren.Length, rootCodeModel.CodeElements.Count)

                    For i = 1 To codeElements.Count
                        expectedChildren(i - 1)(codeElements.Item(i))
                    Next
                End Sub)
        End Function

        Protected Overloads Async Function TestChildren(code As XElement, ParamArray names() As String) As Task
            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Assert.Equal(names.Length, rootCodeModel.CodeElements.Count)

                    Dim actualNames = rootCodeModel.CodeElements.OfType(Of EnvDTE.CodeElement).Select(Function(e) e.Name).ToArray()

                    For i = 0 To names.Length - 1
                        Assert.Contains(names(i), actualNames)
                    Next
                End Sub)
        End Function

        Protected Async Function TestCreateCodeTypeRef(type As Object, data As CodeTypeRefData) As Task
            Await TestCreateCodeTypeRef(<code></code>, type, data)
        End Function

        Protected Async Function TestCreateCodeTypeRef(code As XElement, type As Object, data As CodeTypeRefData) As Task
            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim codeTypeRef = rootCodeModel.CreateCodeTypeRef(type)

                    TestCodeTypeRef(codeTypeRef, data)
                End Sub)
        End Function

        Protected Async Function TestCreateCodeTypeRefThrows(Of TException As Exception)(type As Object) As Task
            Await TestCreateCodeTypeRef(Of TException)(<code></code>, type)
        End Function

        Protected Async Function TestCreateCodeTypeRef(Of TException As Exception)(code As XElement, type As Object) As Task
            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Assert.Throws(Of TException)(
                        Sub()
                            rootCodeModel.CreateCodeTypeRef(type)
                        End Sub)
                End Sub)
        End Function

        Protected Async Function TestCodeTypeFromFullName(workspaceDefinition As XElement, fullName As String, action As Action(Of EnvDTE.CodeType)) As Task
            Await TestRootCodeModel(workspaceDefinition,
                Sub(rootCodeModel)
                    action(rootCodeModel.CodeTypeFromFullName(fullName))
                End Sub)
        End Function

    End Class
End Namespace

