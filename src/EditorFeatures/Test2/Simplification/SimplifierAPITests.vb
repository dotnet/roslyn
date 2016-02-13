' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace
Imports Microsoft.CodeAnalysis.Simplification.Simplifier
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports Xunit.Assert

Public Class SimplifierAPITests
    Inherits WorkspaceTestBase

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestExpandAsync()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = ExpandAsync(Of SyntaxNode)(Nothing, Nothing).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "node"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestExpandAsync2()
        Dim node = GetSyntaxNode()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = ExpandAsync(node, Nothing).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestExpand()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = Expand(Of SyntaxNode)(Nothing, Nothing, Nothing)
            End Sub,
            Sub(exception) Equal(exception.ParamName, "node"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestExpand2()
        Dim node = GetSyntaxNode()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = Expand(node, Nothing, Nothing)
            End Sub,
            Sub(exception) Equal(exception.ParamName, "semanticModel"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestExpand3()
        Dim node = GetSyntaxNode()
        Dim semanticModel = GetSemanticModel()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = Expand(node, semanticModel, Nothing)
            End Sub,
            Sub(exception) Equal(exception.ParamName, "workspace"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestTokenExpandAsync()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = ExpandAsync(Nothing, Nothing).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestTokenExpand()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = Expand(Nothing, Nothing, Nothing)
            End Sub,
            Sub(exception) Equal(exception.ParamName, "semanticModel"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestTokenExpand2()
        Dim semanticModel = GetSemanticModel()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim expandedNode = Expand(Nothing, semanticModel, Nothing)
            End Sub,
            Sub(exception) Equal(exception.ParamName, "workspace"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(Nothing).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync2()
        Dim syntaxAnnotation As SyntaxAnnotation = Nothing
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(Nothing, syntaxAnnotation).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync3()
        Dim syntaxAnnotation As SyntaxAnnotation = Nothing
        Dim document = GetDocument()
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(document, syntaxAnnotation).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "annotation"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync4()
        Dim textSpan As TextSpan = Nothing
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(Nothing, textSpan).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync5()
        Dim spans As IEnumerable(Of TextSpan) = Nothing
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(Nothing, spans).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "document"))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Sub TestReduceAsync6()
        Dim document = GetDocument()
        Dim spans As IEnumerable(Of TextSpan) = Nothing
        AssertThrows(Of ArgumentNullException)(
            Sub()
                Dim simplifiedNode = ReduceAsync(document, spans).Result
            End Sub,
            Sub(exception) Equal(exception.ParamName, "spans"))
    End Sub

    Private Function GetDocument() As Document
        CreateFiles(GetSimpleCSharpSolutionFiles())
        Dim sol = Create(properties:=New Dictionary(Of String, String) From {{"Configuration", "Release"}}).OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result
        Return sol.Projects.First.Documents.First
    End Function

    Private Function GetSemanticModel() As SemanticModel
        Return GetDocument().GetSemanticModelAsync().Result
    End Function

    Private Function GetSyntaxNode() As SyntaxNode
        Return IdentifierName(Identifier("Test"))
    End Function
End Class
