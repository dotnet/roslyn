' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Roslyn.Test.Utilities
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class AbstractTextViewFilterTests
        <Fact, WorkItem(617826), Trait(Traits.Feature, Traits.Features.Venus), Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub MapPointsInProjection()
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim projected = workspace.CreateProjectionBufferDocument(<text><![CDATA[
@{|S1:|}
    <span>@{|S2:|}</span>
{|S3:|}
<h2>Default</h2>
                                                         ]]></text>.Value.Replace(vbLf, vbCrLf), {doc}, LanguageNames.CSharp)

                Dim matchingSpan = projected.SelectedSpans.Single()
                TestSpan(workspace, projected, projected.CursorPosition.Value, matchingSpan.End)
                TestSpan(workspace, projected, matchingSpan.End, projected.CursorPosition.Value)
            End Using
        End Sub

        Private Shared Sub TestSpan(workspace As TestWorkspace, projected As TestHostDocument, startPosition As Integer, endPosition As Integer)
            Dim braceMatcher = VisualStudioTestExportProvider.ExportProvider.GetExportedValue(Of IBraceMatchingService)()
            Dim initialLine = projected.InitialTextSnapshot.GetLineFromPosition(startPosition)
            Dim initialLineNumber = initialLine.LineNumber
            Dim initialIndex = startPosition - initialLine.Start.Position
            Dim spans() = {New VsTextSpan()}
            Assert.Equal(0, AbstractVsTextViewFilter.GetPairExtentsWorker(
                         projected.GetTextView(),
                         workspace,
                         braceMatcher,
                         initialLineNumber,
                         initialIndex,
                         spans,
                         CancellationToken.None))

            ' Note - we only set either the start OR the end to the result, the other gets set to the source.
            Dim resultLine = projected.InitialTextSnapshot.GetLineFromPosition(endPosition)
            Dim resultIndex = endPosition - resultLine.Start.Position
            If endPosition > startPosition Then
                Assert.Equal(initialLineNumber, spans(0).iStartLine)
                Assert.Equal(initialIndex, spans(0).iStartIndex)
                Assert.Equal(resultLine.LineNumber, spans(0).iEndLine)
                Assert.Equal(resultIndex, spans(0).iEndIndex)
            Else
                Assert.Equal(resultLine.LineNumber, spans(0).iStartLine)
                Assert.Equal(resultIndex, spans(0).iStartIndex)
                Assert.Equal(initialLineNumber, spans(0).iEndLine)
                Assert.Equal(initialIndex, spans(0).iEndIndex)
            End If
        End Sub
    End Class
End Namespace
