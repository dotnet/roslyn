' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub TestParentOperations()
            Dim sourceCode = TestResource.AllInOneVisualBasicCode

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(sourceCode, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim model = compilation.GetSemanticModel(tree)

            ' visit tree top down to gather child to parent map
            Dim parentMap = GetParentMap(model)

            ' go through all foundings to see whether parent Is correct
            For Each kv In parentMap
                Dim child = kv.Key
                Dim parent = kv.Value

                ' check parent property returns same parent we gathered by walking down operation tree
                Assert.Equal(child.Parent, parent)

                ' check SearchparentOperation return same parent
                Assert.Equal(DirectCast(child, Operation).SearchParentOperation(), parent)
            Next
        End Sub

        Private Function GetParentMap(model As SemanticModel) As Dictionary(Of IOperation, IOperation)
            ' get top operations first
            Dim topOperations = New HashSet(Of IOperation)()
            Dim root = model.SyntaxTree.GetRoot()

            CollectTopOperations(model, root, topOperations)

            ' dig down the each operation tree to create the parent operation map
            Dim map = New Dictionary(Of IOperation, IOperation)()
            For Each topOperation In topOperations
                ' this Is top of the operation tree
                map.Add(topOperation, Nothing)

                CollectParentOperation(topOperation, map)
            Next

            Return map
        End Function

        Private Sub CollectParentOperation(operation As IOperation, map As Dictionary(Of IOperation, IOperation))
            ' walk down to collect all parent operation map for this tree
            For Each child In operation.Children.WhereNotNull()
                map.Add(child, operation)
                CollectParentOperation(child, map)
            Next
        End Sub

        Private Shared Sub CollectTopOperations(model As SemanticModel, node As SyntaxNode, topOperations As HashSet(Of IOperation))
            For Each child In node.ChildNodes()

                Dim operation = model.GetOperationInternal(child)
                If operation IsNot Nothing Then
                    ' found top operation
                    topOperations.Add(operation)

                    ' don't dig down anymore
                    Continue For
                End If

                ' sub tree might have the top operation
                CollectTopOperations(model, child, topOperations)
            Next
        End Sub
    End Class
End Namespace

