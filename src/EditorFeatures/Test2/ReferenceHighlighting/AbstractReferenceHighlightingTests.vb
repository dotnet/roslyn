' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting

    Public MustInherit Class AbstractReferenceHighlightingTests
        Protected Sub VerifyHighlights(test As XElement, Optional optionIsEnabled As Boolean = True)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim tagProducer = New ReferenceHighlightingViewTaggerProvider.TagProducer()

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim snapshot = hostDocument.InitialTextSnapshot

                workspace.Options = workspace.Options.WithChangedOption(FeatureOnOffOptions.ReferenceHighlighting, hostDocument.Project.Language, optionIsEnabled)

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim producedTags = From tag In tagProducer.ProduceTagsAsync({New DocumentSnapshotSpan(document, New SnapshotSpan(snapshot, 0, snapshot.Length))},
                                                                             New SnapshotPoint(snapshot, caretPosition),
                                                                             workspace, document,
                                                                             cancellationToken:=Nothing).Result
                                   Order By tag.Span.Start
                                   Let spanType = If(tag.Tag.Type = DefinitionHighlightTag.TagId, "Definition", "Reference")
                                   Select spanType + ": " + tag.Span.Span.ToTextSpan().ToString()

                Dim expectedDefinitionSpans As New List(Of Tuple(Of String, TextSpan))

                For Each hostDocument In workspace.Documents
                    If hostDocument.AnnotatedSpans.ContainsKey("Definition") Then
                        For Each definitionSpan In hostDocument.AnnotatedSpans("Definition")
                            expectedDefinitionSpans.Add(Tuple.Create("Definition", definitionSpan))
                        Next
                    End If
                Next

                Dim expectedReferenceSpans = workspace.Documents.SelectMany(Function(d) d.SelectedSpans).Select(Function(s) Tuple.Create("Reference", s))

                Dim expectedTags = From span In expectedDefinitionSpans.Concat(expectedReferenceSpans)
                                   Order By span.Item2.Start
                                   Select span.Item1 + ": " + span.Item2.ToString()

                AssertEx.Equal(expectedTags, producedTags)
            End Using
        End Sub

    End Class

End Namespace
