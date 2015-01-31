' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public MustInherit Class AbstractRootCodeModelTests
        Inherits AbstractCodeModelObjectTests(Of EnvDTE.CodeModel)

        Protected Sub TestRootCodeModel(code As XElement, action As Action(Of EnvDTE.CodeModel))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim rootCodeModel = state.RootCodeModel
                Assert.NotNull(rootCodeModel)

                action(rootCodeModel)
            End Using
        End Sub

        Protected Sub TestCodeElements(code As XElement, ParamArray expectedChildren() As Action(Of Object))
            TestRootCodeModel(code,
                Sub(rootCodeModel)
                    Dim codeElements = rootCodeModel.CodeElements
                    Assert.Equal(expectedChildren.Length, rootCodeModel.CodeElements.Count)

                    For i = 1 To codeElements.Count
                        expectedChildren(i - 1)(codeElements.Item(i))
                    Next
                End Sub)
        End Sub

        Protected Sub TestCodeElements(code As XElement, ParamArray names() As String)
            TestRootCodeModel(code,
                Sub(rootCodeModel)
                    Assert.Equal(names.Length, rootCodeModel.CodeElements.Count)

                    Dim actualNames = rootCodeModel.CodeElements.OfType(Of EnvDTE.CodeElement).Select(Function(e) e.Name).ToArray()

                    For i = 0 To names.Length - 1
                        Assert.Contains(names(i), actualNames)
                    Next
                End Sub)
        End Sub

        Protected Sub TestCreateCodeTypeRef(type As Object, data As CodeTypeRefData)
            TestCreateCodeTypeRef(<code></code>, type, data)
        End Sub

        Protected Sub TestCreateCodeTypeRef(code As XElement, type As Object, data As CodeTypeRefData)
            TestRootCodeModel(code,
                Sub(rootCodeModel)
                    Dim codeTypeRef = rootCodeModel.CreateCodeTypeRef(type)

                    TestCodeTypeRef(codeTypeRef, data)
                End Sub)
        End Sub

        Protected Sub TestCreateCodeTypeRefThrows(Of TException As Exception)(type As Object)
            TestCreateCodeTypeRef(Of TException)(<code></code>, type)
        End Sub

        Protected Sub TestCreateCodeTypeRef(Of TException As Exception)(code As XElement, type As Object)
            TestRootCodeModel(code,
                Sub(rootCodeModel)
                    Assert.Throws(Of TException)(
                        Sub()
                            rootCodeModel.CreateCodeTypeRef(type)
                        End Sub)
                End Sub)
        End Sub

    End Class
End Namespace

