' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractRootCodeModelTests
        Inherits AbstractCodeModelObjectTests(Of EnvDTE80.CodeModel2)

        Protected Sub TestRootCodeModel(workspaceDefinition As XElement, action As Action(Of EnvDTE80.CodeModel2))
            Using state = CreateCodeModelTestState(workspaceDefinition)
                Dim rootCodeModel = TryCast(state.RootCodeModel, EnvDTE80.CodeModel2)
                Assert.NotNull(rootCodeModel)

                action(rootCodeModel)
            End Using
        End Sub

        Protected Sub TestRootCodeModelWithCodeFile(code As XElement, action As Action(Of EnvDTE80.CodeModel2))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim rootCodeModel = TryCast(state.RootCodeModel, EnvDTE80.CodeModel2)
                Assert.NotNull(rootCodeModel)

                action(rootCodeModel)
            End Using
        End Sub

        Protected Overrides Sub TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object))
            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim codeElements = rootCodeModel.CodeElements
                    Assert.Equal(expectedChildren.Length, rootCodeModel.CodeElements.Count)

                    For i = 1 To codeElements.Count
                        expectedChildren(i - 1)(codeElements.Item(i))
                    Next
                End Sub)
        End Sub

        Protected Overloads Sub TestChildren(code As XElement, ParamArray names() As String)
            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim actualNames = rootCodeModel.CodeElements.OfType(Of EnvDTE.CodeElement).Select(Function(e) e.Name).ToArray()
                    Assert.Equal(names.Length, rootCodeModel.CodeElements.Count)

                    For i = 0 To names.Length - 1
                        Assert.Contains(names(i), actualNames)
                    Next
                End Sub)
        End Sub

        Protected Sub TestCreateCodeTypeRef(type As Object, data As CodeTypeRefData)
            TestCreateCodeTypeRef(<code></code>, type, data)
        End Sub

        Protected Sub TestCreateCodeTypeRef(code As XElement, type As Object, data As CodeTypeRefData)
            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim codeTypeRef = rootCodeModel.CreateCodeTypeRef(type)

                    TestCodeTypeRef(codeTypeRef, data)
                End Sub)
        End Sub

        Protected Sub TestCreateCodeTypeRefThrows(Of TException As Exception)(type As Object)
            TestCreateCodeTypeRef(Of TException)(<code></code>, type)
        End Sub

        Protected Sub TestCreateCodeTypeRef(Of TException As Exception)(code As XElement, type As Object)
            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Assert.Throws(Of TException)(
                        Sub()
                            rootCodeModel.CreateCodeTypeRef(type)
                        End Sub)
                End Sub)
        End Sub

        Protected Sub TestCodeTypeFromFullName(workspaceDefinition As XElement, fullName As String, action As Action(Of EnvDTE.CodeType))
            TestRootCodeModel(workspaceDefinition,
                Sub(rootCodeModel)
                    action(rootCodeModel.CodeTypeFromFullName(fullName))
                End Sub)
        End Sub

    End Class
End Namespace

