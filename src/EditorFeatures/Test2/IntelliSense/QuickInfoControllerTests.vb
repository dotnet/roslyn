' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Classification
Imports Moq
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class QuickInfoControllerTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub PresenterBuildIntellisenseQuickInfoItem()
            Using workspace = TestWorkspace.CreateCSharp(
                                "class Program
                                {
                                    static void Main(string[] args)
                                    {
                                        Console.Write|$$|Line(""test"");
                                    }
                                }")

                Dim view = workspace.Documents.Single().GetTextView()
                Dim snapshotPoint = New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value)

                Dim presenter As IIntelliSensePresenter(Of IQuickInfoPresenterSession, IAsyncQuickInfoSession) =
                    New QuickInfoPresenter(workspace.ExportProvider.GetExportedValue(Of ClassificationTypeMap)(),
                                           workspace.ExportProvider.GetExportedValue(Of IClassificationFormatMapService)(),
                                           Nothing, Nothing, Nothing)

                Dim quickInfoSession = New Mock(Of IAsyncQuickInfoSession)

                Dim codeAnalysisQuickInfoItem _
                    = QuickInfoItem.Create(New Text.TextSpan(0, 0), ImmutableArray.Create({"Method", "Public"}),
                        ImmutableArray.Create _
                            ({QuickInfoSection.Create("Description",
                                ImmutableArray.Create({
                                    New TaggedText("Keyword", "void"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Class", "Console"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Method", "WriteLine"),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Keyword", "string"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Parameter", "value"),
                                    New TaggedText("Punctuation", ")"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Punctuation", "+"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "18"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "overloads"),
                                    New TaggedText("Punctuation", ")")})),
                            QuickInfoSection.Create("DocumentationComments",
                                ImmutableArray.Create({New TaggedText("Text", "Writes the specified string value, followed by the current line terminator, to the standard output stream.")})),
                            QuickInfoSection.Create("Exception",
                                ImmutableArray.Create({
                                    New TaggedText("Text", "Exceptions"),
                                    New TaggedText("LineBreak", "\r\n"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Namespace", "System"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Namespace", "IO"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Class", "IOException")}))}))

                Dim presenterSession = presenter.CreateSession(view, view.TextBuffer, quickInfoSession.Object)

                'view.Caret.MoveTo(snapshotPoint)

                Dim intellisenseQuickInfo = presenterSession.BuildIntellisenseQuickInfoItemAsync(snapshotPoint, codeAnalysisQuickInfoItem).Result

                Assert.NotNull(intellisenseQuickInfo)
                Dim expectedStrValue = "void‎ Console‎.WriteLine‎(string‎ value‎)‎ ‎(‎+‎ 18‎ overloads‎)
Writes the specified string value, followed by the current line terminator, to the standard output stream.
Exceptions‎\r\n‎ System‎.IO‎.IOException"
                Assert.Equal(expectedStrValue, intellisenseQuickInfo.Item.ToString())

            End Using
        End Sub
    End Class
End Namespace
