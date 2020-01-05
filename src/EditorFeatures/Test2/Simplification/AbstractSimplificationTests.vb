' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <[UseExportProvider]>
    Public MustInherit Class AbstractSimplificationTests

        Protected Async Function TestAsync(definition As XElement, expected As XElement, Optional options As Dictionary(Of OptionKey, Object) = Nothing, Optional csharpParseOptions As CSharpParseOptions = Nothing) As System.Threading.Tasks.Task
            Using workspace = TestWorkspace.Create(definition)
                Dim finalWorkspace = workspace

                If csharpParseOptions IsNot Nothing Then
                    For Each project In workspace.CurrentSolution.Projects
                        finalWorkspace.ChangeSolution(finalWorkspace.CurrentSolution.WithProjectParseOptions(project.Id, csharpParseOptions))
                    Next
                End If

                Await TestAsync(finalWorkspace, expected, options).ConfigureAwait(False)
            End Using
        End Function

        Protected Async Function TestAsync(workspace As TestWorkspace, expected As XElement, Optional options As Dictionary(Of OptionKey, Object) = Nothing) As System.Threading.Tasks.Task
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

            Await TestAsync(
                workspace, spansToAddSimplifierAnnotation,
                explicitSpansToSimplifyWithin, expected, options)
        End Function

        Private Async Function TestAsync(workspace As Workspace,
                         listOfLabelToAddSimplifierAnnotationSpans As IEnumerable(Of KeyValuePair(Of String, ImmutableArray(Of TextSpan))),
                         explicitSpansToSimplifyWithin As ImmutableArray(Of TextSpan),
                         expected As XElement,
                         options As Dictionary(Of OptionKey, Object)) As System.Threading.Tasks.Task
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

            Dim optionSet = workspace.Options
            If options IsNot Nothing Then
                For Each entry In options
                    optionSet = optionSet.WithChangedOption(entry.Key, entry.Value)
                Next
            End If

            document = document.WithSyntaxRoot(root)

            Dim simplifiedDocument As Document
            If Not explicitSpansToSimplifyWithin.IsDefaultOrEmpty Then
                simplifiedDocument = Await Simplifier.ReduceAsync(document, explicitSpansToSimplifyWithin, optionSet)
            Else
                simplifiedDocument = Await Simplifier.ReduceAsync(document, Simplifier.Annotation, optionSet)
            End If

            Dim actualText = (Await simplifiedDocument.GetTextAsync()).ToString()
            Assert.Equal(expected.NormalizedValue.Trim(), actualText.Trim())
        End Function

        Private Function GetExpressionSyntaxWithSameSpan(node As SyntaxNode, spanEnd As Integer) As SyntaxNode
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
