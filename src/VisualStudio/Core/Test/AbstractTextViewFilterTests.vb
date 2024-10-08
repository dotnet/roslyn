' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceMatching
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Roslyn.Test.Utilities
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    <[UseExportProvider]>
    Public Class AbstractTextViewFilterTests
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/617826"), Trait(Traits.Feature, Traits.Features.Venus), Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub MapPointsInProjectionCSharp()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                            class C
                            {
                                static void M()
                                {
                                    {|S1:foreach (var x in new int[] { 1, 2, 3 })
                                    $${ |}
                                        Console.Write({|S2:item|});
                                    {|S3:[|}|]|}
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim projected = workspace.CreateProjectionBufferDocument(<text><![CDATA[
@{|S1:|}
    <span>@{|S2:|}</span>
{|S3:|}
<h2>Default</h2>
                                                         ]]></text>.Value.Replace(vbLf, vbCrLf), {doc})

                Dim matchingSpan = projected.SelectedSpans.Single()
                TestSpan(workspace, projected, projected.CursorPosition.Value, matchingSpan.End)
                TestSpan(workspace, projected, matchingSpan.End, projected.CursorPosition.Value)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceNavigatesToOuterPositionOfMatchingBraceCSharp()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine[|$$(Increment(5))|];
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, doc.CursorPosition.Value, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End, doc.CursorPosition.Value, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceFromLeftAndRightOfOpenAndCloseBracesCSharp()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine(Increment[|$$(5)|]);
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, span.Start, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.Start + 1, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End - 1, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceExtFindsTheInnerPositionOfCloseBraceAndOuterPositionOfOpenBraceCSharp()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine[|(Increment(5)|])$$;
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, caretPosition:=span.Start, startPosition:=span.Start, endPosition:=span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=doc.CursorPosition.Value, startPosition:=span.End, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceExtFromLeftAndRightOfOpenAndCloseBracesCSharp()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine(Increment[|(5)|]);
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()

                ' Test from left and right of Open parenthesis
                TestSpan(workspace, doc, caretPosition:=span.Start, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.Start + 1, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))

                ' Test from left and right of Close parenthesis
                TestSpan(workspace, doc, caretPosition:=span.End, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.End - 1, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceFromLeftAndRightOfOpenAndCloseBracesBasic()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
                        <Document>
                        Imports System

                        Module Program
                            Sub Main(args As String())
                                Console.WriteLine(Increment[|$$(5)|])
                            End Sub

                            Private Function Increment(v As Integer) As Integer
                                Return v + 1
                            End Function
                        End Module
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, span.Start, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.Start + 1, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End - 1, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceExtFromLeftAndRightOfOpenAndCloseBracesBasic()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
                        <Document>
                        Imports System

                        Module Program
                            Sub Main(args As String())
                                Console.WriteLine(Increment[|$$(5)|])
                            End Sub

                            Private Function Increment(v As Integer) As Integer
                                Return v + 1
                            End Function
                        End Module
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()

                ' Test from left and right of Open parenthesis
                TestSpan(workspace, doc, caretPosition:=span.Start, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.Start + 1, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))

                ' Test from left and right of Close parenthesis
                TestSpan(workspace, doc, caretPosition:=span.End, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.End - 1, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
            End Using
        End Sub

        Private Shared Sub TestSpan(workspace As EditorTestWorkspace, document As EditorTestHostDocument, startPosition As Integer, endPosition As Integer, Optional commandId As UInteger = Nothing)
            Dim braceMatcher = workspace.ExportProvider.GetExportedValue(Of IBraceMatchingService)()
            Dim initialLine = document.InitialTextSnapshot.GetLineFromPosition(startPosition)
            Dim initialLineNumber = initialLine.LineNumber
            Dim initialIndex = startPosition - initialLine.Start.Position
            Dim spans() = {New VsTextSpan()}
            Assert.Equal(0, AbstractVsTextViewFilter.GetPairExtentsWorker(
                         document.GetTextView(),
                         braceMatcher,
                         workspace.GlobalOptions,
                         initialLineNumber,
                         initialIndex,
                         spans,
                         commandId = CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT),
                         CancellationToken.None))

            ' Note - we only set either the start OR the end to the result, the other gets set to the source.
            Dim resultLine = document.InitialTextSnapshot.GetLineFromPosition(endPosition)
            Dim resultIndex = endPosition - resultLine.Start.Position
            AssertSpansMatch(startPosition, endPosition, initialLineNumber, initialIndex, spans, resultLine, resultIndex)
        End Sub

        Private Shared Sub TestSpan(workspace As EditorTestWorkspace, document As EditorTestHostDocument, caretPosition As Integer, startPosition As Integer, endPosition As Integer, Optional commandId As UInteger = Nothing)
            Dim braceMatcher = workspace.ExportProvider.GetExportedValue(Of IBraceMatchingService)()
            Dim initialLine = document.InitialTextSnapshot.GetLineFromPosition(caretPosition)
            Dim initialLineNumber = initialLine.LineNumber
            Dim initialIndex = caretPosition - initialLine.Start.Position
            Dim spans() = {New VsTextSpan()}
            Assert.Equal(0, AbstractVsTextViewFilter.GetPairExtentsWorker(
                         document.GetTextView(),
                         braceMatcher,
                         workspace.GlobalOptions,
                         initialLineNumber,
                         initialIndex,
                         spans,
                         commandId = CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT),
                         CancellationToken.None))
            'In extending selection (GotoBraceExt) scenarios we set both start AND end to the result.
            Dim startIndex = startPosition - initialLine.Start.Position
            Dim resultLine = document.InitialTextSnapshot.GetLineFromPosition(endPosition)
            Dim resultIndex = endPosition - resultLine.Start.Position
            AssertSpansMatch(startPosition, endPosition, initialLineNumber, startIndex, spans, resultLine, resultIndex)
        End Sub

        Private Shared Sub AssertSpansMatch(startPosition As Integer, endPosition As Integer, initialLineNumber As Integer, startIndex As Integer, spans() As VsTextSpan, resultLine As Text.ITextSnapshotLine, resultIndex As Integer)
            If endPosition > startPosition Then
                Assert.Equal(initialLineNumber, spans(0).iStartLine)
                Assert.Equal(startIndex, spans(0).iStartIndex)
                Assert.Equal(resultLine.LineNumber, spans(0).iEndLine)
                Assert.Equal(resultIndex, spans(0).iEndIndex)
            Else
                Assert.Equal(resultLine.LineNumber, spans(0).iStartLine)
                Assert.Equal(resultIndex, spans(0).iStartIndex)
                Assert.Equal(initialLineNumber, spans(0).iEndLine)
                Assert.Equal(startIndex, spans(0).iEndIndex)
            End If
        End Sub
    End Class
End Namespace
