' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.LanguageServer.Protocol
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class FoldingRangeTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        ' Expected 'FoldingRangeKind' argument would likely change for some of the tests when
        ' https://github.com/dotnet/roslyn/projects/45#card-20049168 is implemented.
        <Theory>
        <InlineData("
class C{|foldingRange:
{
}|}", Nothing, "...")>
        <InlineData("
class C{|foldingRange:
{
    void M(){|foldingRange:
    {
        for (int i = 0; i < 10; i++){|foldingRange:
        {
            M();
        }|}
    }|}
}|}", Nothing, "...")>
        <InlineData("
{|foldingRange:#region
#endregion|}", Nothing, "#region")>
        <InlineData("
using {|foldingRange:System;
using System.Linq;|}", "imports", "...")>
        <InlineData("
using {|foldingRange:S = System.String; 
using System.Linq;|}", "imports", "...")>
        <InlineData("
{|foldingRange:// Comment Line 1
// Comment Line 2|}", Nothing, "// Comment Line 1...")>
        Public Async Function TestFoldingRanges(code As String, rangeKind As String, collapsedText As String) As Task
            Using workspace = EditorTestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition)

                Dim annotatedLocations = Await AbstractLanguageServerProtocolTests.GetAnnotatedLocationsAsync(workspace, workspace.CurrentSolution)
                Dim expectedRanges = annotatedLocations("foldingRange").Select(Function(location) CreateFoldingRange(rangeKind, location.Range, collapsedText)).OrderByDescending(Function(range) range.StartLine).ToArray()

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(workspace)
                Dim actualRanges = lsif.GetFoldingRanges(document)

                AbstractLanguageServerProtocolTests.AssertJsonEquals(expectedRanges, actualRanges)
            End Using
        End Function

        Private Shared Function CreateFoldingRange(kind As String, range As Range, collapsedText As String) As FoldingRange
            Dim foldingRange As FoldingRange = New FoldingRange() With
            {
                .StartCharacter = range.Start.Character,
                .EndCharacter = range.End.Character,
                .StartLine = range.Start.Line,
                .EndLine = range.End.Line,
                .CollapsedText = collapsedText
            }
            If kind IsNot Nothing Then
                foldingRange.Kind = New FoldingRangeKind(kind)
            End If
            Return foldingRange
        End Function
    End Class
End Namespace
