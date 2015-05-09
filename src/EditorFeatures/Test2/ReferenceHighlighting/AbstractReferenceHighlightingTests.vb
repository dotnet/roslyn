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
                                   Let spanType = If(tag.Tag.Type = DefinitionHighlightTag.TagId, "Definition",
                                       If(tag.Tag.Type = WrittenReferenceHighlightTag.TagId, "WrittenReference", "Reference"))
                                   Select spanType + ":" + tag.Span.Span.ToTextSpan().ToString()

                Dim expectedTags As New List(Of String)

                For Each hostDocument In workspace.Documents
                    For Each nameAndSpans In hostDocument.AnnotatedSpans
                        For Each span In nameAndSpans.Value
                            expectedTags.Add(nameAndSpans.Key + ":" + span.ToString())
                        Next
                    Next
                Next

                AssertEx.Equal(expectedTags, producedTags)
            End Using
        End Sub
    End Class
End Namespace
