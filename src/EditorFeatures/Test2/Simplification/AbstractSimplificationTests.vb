' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <[UseExportProvider]>
    Public MustInherit Class AbstractSimplificationTests

        Private Protected Shared Async Function TestAsync(definition As XElement, expected As XElement, Optional options As OptionsCollection = Nothing, Optional csharpParseOptions As CSharpParseOptions = Nothing) As System.Threading.Tasks.Task
            Using workspace = Await CreateTestWorkspaceAsync(definition, csharpParseOptions)
                Dim simplifiedDocument = Await SimplifyAsync(workspace, options).ConfigureAwait(False)
                Await AssertCodeEqual(expected, simplifiedDocument)
            End Using
        End Function

        Protected Shared Async Function CreateTestWorkspaceAsync(definition As XElement, Optional csharpParseOptions As CSharpParseOptions = Nothing) As Task(Of EditorTestWorkspace)
            Dim workspace = EditorTestWorkspace.Create(definition)

            If csharpParseOptions IsNot Nothing Then
                For Each project In workspace.CurrentSolution.Projects
                    Await workspace.ChangeSolutionAsync(workspace.CurrentSolution.WithProjectParseOptions(project.Id, csharpParseOptions))
                Next
            End If

            Return workspace
        End Function

        Protected Shared Function SimplifyAsync(workspace As EditorTestWorkspace) As Task(Of Document)
            Return SimplifyAsync(workspace, Nothing)
        End Function

        Private Shared Async Function SimplifyAsync(workspace As EditorTestWorkspace, options As OptionsCollection) As Task(Of Document)
            Dim hostDocument = workspace.Documents.Single()

            Dim spansToAddSimplifierAnnotation = hostDocument.AnnotatedSpans.Where(Function(kvp) kvp.Key.StartsWith("Simplify", StringComparison.Ordinal))

            Dim explicitSpanToSimplifyAnnotatedSpans = hostDocument.AnnotatedSpans.Where(Function(kvp) Not spansToAddSimplifierAnnotation.Contains(kvp))
            If explicitSpanToSimplifyAnnotatedSpans.Count <> 1 OrElse explicitSpanToSimplifyAnnotatedSpans.Single().Key <> "SpanToSimplify" Then
                For Each span In explicitSpanToSimplifyAnnotatedSpans
                    If span.Key <> "SpanToSimplify" Then
                        Assert.True(False, "Encountered unexpected span annotation: " + span.Key)
                    End If
                Next
            End If

            Dim explicitSpansToSimplifyWithin = If(explicitSpanToSimplifyAnnotatedSpans.Any(),
                                                    explicitSpanToSimplifyAnnotatedSpans.Single().Value,
                                                    Nothing)

            Return Await SimplifyAsync(workspace, spansToAddSimplifierAnnotation, explicitSpansToSimplifyWithin, options)
        End Function

        Private Shared Async Function SimplifyAsync(
            workspace As EditorTestWorkspace,
            listOfLabelToAddSimplifierAnnotationSpans As IEnumerable(Of KeyValuePair(Of String, ImmutableArray(Of TextSpan))),
            explicitSpansToSimplifyWithin As ImmutableArray(Of TextSpan),
            options As OptionsCollection) As Task(Of Document)

            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()

            Dim root = Await document.GetSyntaxRootAsync()

            For Each labelToAddSimplifierAnnotationSpans In listOfLabelToAddSimplifierAnnotationSpans
                Dim simplifyKind = labelToAddSimplifierAnnotationSpans.Key
                Dim spansToAddSimplifierAnnotation = labelToAddSimplifierAnnotationSpans.Value

                Select Case simplifyKind
                    Case "Simplify"
                        For Each span In spansToAddSimplifierAnnotation
                            Dim node = root.FindToken(span.Start).Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyToken"
                        For Each span In spansToAddSimplifierAnnotation
                            Dim token = root.FindToken(span.Start)
                            root = root.ReplaceToken(token, token.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyParent"
                        For Each span In spansToAddSimplifierAnnotation
                            Dim node = root.FindToken(span.Start).Parent.Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyParentParent"
                        For Each span In spansToAddSimplifierAnnotation
                            Dim node = root.FindToken(span.Start).Parent.Parent.Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyExtension"
                        For Each span In spansToAddSimplifierAnnotation
                            Dim node = GetExpressionSyntaxWithSameSpan(root.FindToken(span.Start).Parent, span.End)
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next
                End Select
            Next

            options?.SetGlobalOptions(workspace.GlobalOptions)

            document = document.WithSyntaxRoot(root)

#Disable Warning RS0030 ' Do Not used banned APIs
            Dim optionSet = options?.ToOptionSet()
            Dim simplifiedDocument As Document
            If Not explicitSpansToSimplifyWithin.IsDefaultOrEmpty Then
                simplifiedDocument = Await Simplifier.ReduceAsync(document, explicitSpansToSimplifyWithin, optionSet)
            Else
                simplifiedDocument = Await Simplifier.ReduceAsync(document, Simplifier.Annotation, optionSet)
            End If
#Enable Warning RS0030

            Return simplifiedDocument
        End Function

        Protected Shared Async Function AssertCodeEqual(expected As XElement, simplifiedDocument As Document) As Task
            Dim actualText = (Await simplifiedDocument.GetTextAsync()).ToString()
            Assert.Equal(expected.NormalizedValue.Trim(), actualText.Trim())
        End Function

        Private Shared Function GetExpressionSyntaxWithSameSpan(node As SyntaxNode, spanEnd As Integer) As SyntaxNode
            While Not node Is Nothing And Not node.Parent Is Nothing And node.Parent.SpanStart = node.SpanStart
                node = node.Parent
                If node.Span.End = spanEnd Then
                    Exit While
                End If
            End While

            Return node
        End Function

    End Class
End Namespace
