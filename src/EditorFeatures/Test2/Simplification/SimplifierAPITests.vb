' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <UseExportProvider>
    Public Class SimplifierAPITests

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpandAsync()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.ExpandAsync(Of SyntaxNode)(Nothing, Nothing).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "node"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpandAsync2()
            Dim node = GetSyntaxNode()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.ExpandAsync(node, Nothing).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.Expand(Of SyntaxNode)(Nothing, Nothing, Nothing)
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "node"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand2()
            Dim node = GetSyntaxNode()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.Expand(node, Nothing, Nothing)
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "semanticModel"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestExpand3()
            Dim node = GetSyntaxNode()
            Dim semanticModel = GetSemanticModel()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.Expand(node, semanticModel, Nothing)
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "workspace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestTokenExpandAsync()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.ExpandAsync(Nothing, Nothing).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestTokenExpand()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.Expand(Nothing, Nothing, Nothing)
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "semanticModel"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestTokenExpand2()
            Dim semanticModel = GetSemanticModel()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim expandedNode = Simplifier.Expand(Nothing, semanticModel, Nothing)
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "workspace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(Nothing).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync2()
            Dim syntaxAnnotation As SyntaxAnnotation = Nothing
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(Nothing, syntaxAnnotation).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync3()
            Dim syntaxAnnotation As SyntaxAnnotation = Nothing
            Dim document = GetDocument()
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(document, syntaxAnnotation).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "annotation"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync4()
            Dim textSpan As TextSpan = Nothing
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(Nothing, textSpan).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync5()
            Dim spans As IEnumerable(Of TextSpan) = Nothing
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(Nothing, spans).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "document"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub TestReduceAsync6()
            Dim document = GetDocument()
            Dim spans As IEnumerable(Of TextSpan) = Nothing
            AssertEx.Throws(Of ArgumentNullException)(
                Sub()
                    Dim simplifiedNode = Simplifier.ReduceAsync(document, spans).Result
                End Sub,
                Sub(exception) Assert.Equal(exception.ParamName, "spans"))
        End Sub

        Private Function GetDocument() As Document
            Dim workspace = New AdhocWorkspace()

            Dim solution = workspace.CreateSolution(SolutionId.CreateNewId())
            Dim project = workspace.AddProject("CSharpTest", LanguageNames.CSharp)

            Return workspace.AddDocument(project.Id, "CSharpFile.cs", SourceText.From("class C { }"))
        End Function

        Private Function GetSemanticModel() As SemanticModel
            Return GetDocument().GetSemanticModelAsync().Result
        End Function

        Private Function GetSyntaxNode() As SyntaxNode
            Return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Test"))
        End Function
    End Class
End Namespace
