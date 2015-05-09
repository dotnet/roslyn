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

        Protected Sub Test(definition As XElement, expected As XElement, Optional simplifcationOptions As Dictionary(Of OptionKey, Object) = Nothing)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(definition)
                Dim hostDocument = workspace.Documents.Single()

                Dim spansToAddSimpliferAnnotation = hostDocument.AnnotatedSpans.Where(Function(kvp) kvp.Key.StartsWith("Simplify", StringComparison.Ordinal))

                Dim explicitSpanToSimplifyAnnotatedSpans = hostDocument.AnnotatedSpans.Where(Function(kvp) Not spansToAddSimpliferAnnotation.Contains(kvp))
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

                Test(workspace, spansToAddSimpliferAnnotation, explicitSpansToSimplifyWithin, expected, simplifcationOptions)
            End Using

        End Sub

        Private Sub Test(workspace As Workspace,
                         listOfLabelToAddSimpliferAnnotationSpans As IEnumerable(Of KeyValuePair(Of String, IList(Of TextSpan))),
                         explicitSpansToSimplifyWithin As IEnumerable(Of TextSpan),
                         expected As XElement,
                         simplifcationOptions As Dictionary(Of OptionKey, Object))
            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()

            Dim root = document.GetSyntaxRootAsync().Result

            For Each labelToAddSimpliferAnnotationSpans In listOfLabelToAddSimpliferAnnotationSpans
                Dim simplifyKind = labelToAddSimpliferAnnotationSpans.Key
                Dim spansToAddSimpliferAnnotation = labelToAddSimpliferAnnotationSpans.Value

                Select Case simplifyKind
                    Case "Simplify"
                        For Each span In spansToAddSimpliferAnnotation
                            Dim node = root.FindToken(span.Start).Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyToken"
                        For Each span In spansToAddSimpliferAnnotation
                            Dim token = root.FindToken(span.Start)
                            root = root.ReplaceToken(token, token.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyParent"
                        For Each span In spansToAddSimpliferAnnotation
                            Dim node = root.FindToken(span.Start).Parent.Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyParentParent"
                        For Each span In spansToAddSimpliferAnnotation
                            Dim node = root.FindToken(span.Start).Parent.Parent.Parent
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next

                    Case "SimplifyExtension"
                        For Each span In spansToAddSimpliferAnnotation
                            Dim node = GetExpressionSyntaxWithSameSpan(root.FindToken(span.Start).Parent, span.End)
                            root = root.ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation))
                        Next
                End Select
            Next

            Dim optionSet = workspace.Options
            If simplifcationOptions IsNot Nothing Then
                For Each entry In simplifcationOptions
                    optionSet = optionSet.WithChangedOption(entry.Key, entry.Value)
                Next
            End If

            document = document.WithSyntaxRoot(root)

            Dim simplifiedDocument As Document
            If explicitSpansToSimplifyWithin IsNot Nothing Then
                simplifiedDocument = Simplifier.ReduceAsync(document, explicitSpansToSimplifyWithin, optionSet).Result
            Else
                simplifiedDocument = Simplifier.ReduceAsync(document, Simplifier.Annotation, optionSet).Result
            End If

            Dim actualText = simplifiedDocument.GetTextAsync().Result.ToString()
            Assert.Equal(expected.NormalizedValue.Trim(), actualText.Trim())
        End Sub

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
