' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <UseExportProvider>
    Public Class SimplifierAPITests

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestExpandAsync() As Task
            Await Assert.ThrowsAsync(Of ArgumentNullException)("node",
                Function()
                    Return Simplifier.ExpandAsync(Of SyntaxNode)(Nothing, Nothing)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestExpandAsync2() As Task
            Dim node = GetSyntaxNode()
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function() As Task
                    Return Simplifier.ExpandAsync(node, Nothing)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand()
            Assert.Throws(Of ArgumentNullException)("node",
                Sub()
                    Simplifier.Expand(Of SyntaxNode)(Nothing, Nothing, Nothing)
                End Sub)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand2()
            Dim node = GetSyntaxNode()
            Assert.Throws(Of ArgumentNullException)("semanticModel",
                Sub()
                    Simplifier.Expand(node, Nothing, Nothing)
                End Sub)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand3()
            Dim node = GetSyntaxNode()
            Dim semanticModel = GetSemanticModel()
            Assert.Throws(Of ArgumentNullException)("workspace",
                Sub()
                    Simplifier.Expand(node, semanticModel, Nothing)
                End Sub)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestTokenExpandAsync() As Task
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function()
                    Return Simplifier.ExpandAsync(Nothing, Nothing)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestTokenExpand()
            Assert.Throws(Of ArgumentNullException)("semanticModel",
                Sub()
                    Dim expandedNode = Simplifier.Expand(Nothing, Nothing, Nothing)
                End Sub)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestTokenExpand2()
            Dim semanticModel = GetSemanticModel()
            Assert.Throws(Of ArgumentNullException)("workspace",
                Sub()
                    Dim expandedNode = Simplifier.Expand(Nothing, semanticModel, Nothing)
                End Sub)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync() As Task
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function()
                    Return Simplifier.ReduceAsync(Nothing)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync2() As Task
            Dim syntaxAnnotation As SyntaxAnnotation = Nothing
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function()
                    Return Simplifier.ReduceAsync(Nothing, syntaxAnnotation)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync3() As Task
            Dim syntaxAnnotation As SyntaxAnnotation = Nothing
            Dim document = GetDocument()
            Await Assert.ThrowsAsync(Of ArgumentNullException)("annotation",
                Function()
                    Return Simplifier.ReduceAsync(document, syntaxAnnotation)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync4() As Task
            Dim textSpan As TextSpan = Nothing
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function()
                    Return Simplifier.ReduceAsync(Nothing, textSpan)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync5() As Task
            Dim spans As IEnumerable(Of TextSpan) = Nothing
            Await Assert.ThrowsAsync(Of ArgumentNullException)("document",
                Function()
                    Return Simplifier.ReduceAsync(Nothing, spans)
                End Function)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestReduceAsync6() As Task
            Dim document = GetDocument()
            Dim spans As IEnumerable(Of TextSpan) = Nothing
            Await Assert.ThrowsAsync(Of ArgumentNullException)("spans",
                Function() As Task
                    Return Simplifier.ReduceAsync(document, spans)
                End Function)
        End Function

        Private Shared Function GetDocument() As Document
            Dim workspace = New AdhocWorkspace()

            Dim solution = workspace.CreateSolution(SolutionId.CreateNewId())
            Dim project = workspace.AddProject("CSharpTest", LanguageNames.CSharp)

            Return workspace.AddDocument(project.Id, "CSharpFile.cs", SourceText.From("class C { }"))
        End Function

        Private Shared Function GetSemanticModel() As SemanticModel
            Return GetDocument().GetSemanticModelAsync().Result
        End Function

        Private Shared Function GetSyntaxNode() As SyntaxNode
            Return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Test"))
        End Function
    End Class
End Namespace
