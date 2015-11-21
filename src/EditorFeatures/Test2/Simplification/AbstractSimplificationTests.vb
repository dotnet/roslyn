' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public MustInherit Class AbstractSimplificationTests

        Protected Async Function TestAsync(definition As XElement, expected As XElement, Optional simplificationOptions As Dictionary(Of OptionKey, Object) = Nothing) As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(definition)
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

                Await TestAsync(workspace, spansToAddSimplifierAnnotation, explicitSpansToSimplifyWithin, expected, simplificationOptions)
            End Using

        End Function

        Private Async Function TestAsync(workspace As Workspace,
                         listOfLabelToAddSimplifierAnnotationSpans As IEnumerable(Of KeyValuePair(Of String, IList(Of TextSpan))),
                         explicitSpansToSimplifyWithin As IEnumerable(Of TextSpan),
                         expected As XElement,
                         simplificationOptions As Dictionary(Of OptionKey, Object)) As System.Threading.Tasks.Task
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
            If simplificationOptions IsNot Nothing Then
                For Each entry In simplificationOptions
                    optionSet = optionSet.WithChangedOption(entry.Key, entry.Value)
                Next
            End If

            document = document.WithSyntaxRoot(root)

            Dim simplifiedDocument As Document
            If explicitSpansToSimplifyWithin IsNot Nothing Then
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
