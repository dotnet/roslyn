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
}|}", "...")>
        <InlineData("
class C{|foldingRange:
{
    void M(){|implementation:
    {
        for (int i = 0; i < 10; i++){|foldingRange:
        {
            M();
        }|}
    }|}
}|}", "...")>
        <InlineData("
{|foldingRange:#region
#endregion|}", "#region")>
        <InlineData("
using {|imports:System;
using System.Linq;|}", "...")>
        <InlineData("
using {|imports:S = System.String; 
using System.Linq;|}", "...")>
        <InlineData("
{|foldingRange:// Comment Line 1
// Comment Line 2|}", "// Comment Line 1...")>
        Public Async Function TestFoldingRanges(code As String, collapsedText As String) As Task
            Using workspace = EditorTestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition)

                Dim annotatedLocations = Await AbstractLanguageServerProtocolTests.GetAnnotatedLocationsAsync(workspace, workspace.CurrentSolution)
                Dim expectedRanges = annotatedLocations.SelectMany(Function(kvp) kvp.Value.Select(Function(location) CreateFoldingRange(kvp.Key, location.Range, collapsedText))).OrderByDescending(Function(range) range.StartLine).ToArray()

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
            If kind IsNot Nothing AndAlso kind <> "foldingRange" Then
                foldingRange.Kind = New FoldingRangeKind(kind)
            End If
            Return foldingRange
        End Function
    End Class
End Namespace
