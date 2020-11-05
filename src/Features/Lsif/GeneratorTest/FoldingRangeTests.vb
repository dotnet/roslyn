' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServer.Protocol
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
}|}", Nothing)>
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
}|}", Nothing)>
        <InlineData("
{|foldingRange:#region
#endregion|}", Nothing)>
        <InlineData("
using {|foldingRange:System;
using System.Linq;|}", FoldingRangeKind.Imports)>
        <InlineData("
using {|foldingRange:S = System.String; 
using System.Linq;|}", FoldingRangeKind.Imports)>
        <InlineData("
{|foldingRange:// Comment Line 1
// Comment Line 2|}", Nothing)>
        Public Async Function TestFoldingRanges(code As String, rangeKind As FoldingRangeKind?) As Task
            Using workspace = TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>)

                Dim annotatedLocations = AbstractLanguageServerProtocolTests.GetAnnotatedLocations(workspace, workspace.CurrentSolution)
                Dim expectedRanges = annotatedLocations("foldingRange").Select(Function(location) CreateFoldingRange(rangeKind, location.Range)).ToArray()

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(workspace)
                Dim actualRanges = lsif.GetFoldingRanges(document)

                AssertEx.SetEqual(expectedRanges, actualRanges, FoldingRangeComparer.Instance, itemInspector:=AddressOf GetStringForAssert)
            End Using
        End Function

        Private Shared Function CreateFoldingRange(kind As FoldingRangeKind?, range As Range) As FoldingRange
            Return New FoldingRange() With
            {
                .Kind = kind,
                .StartCharacter = range.Start.Character,
                .EndCharacter = range.End.Character,
                .StartLine = range.Start.Line,
                .EndLine = range.End.Line
            }
        End Function

        Private Shared Function GetStringForAssert(range As FoldingRange) As String
            Dim rangeKindStr = $"Kind: {If(range.Kind?.ToString(), "null")}"
            Dim startLineStr = $"StartLine: {range.StartLine}"
            Dim endLineStr = $"EndLine: {range.EndLine}"
            Dim startCharStr = $"StartCharacter: {range.StartCharacter}"
            Dim endCharStr = $"EndCharacter: {range.EndCharacter}"
            Return $"[{String.Join(", ", rangeKindStr, startLineStr, endLineStr, startCharStr, endCharStr)}]"
        End Function

        Private NotInheritable Class FoldingRangeComparer
            Implements IEqualityComparer(Of FoldingRange)

            Public Shared ReadOnly Instance As New FoldingRangeComparer

            Private Sub New()
            End Sub

            Public Shadows Function Equals(x As FoldingRange, y As FoldingRange) As Boolean Implements IEqualityComparer(Of FoldingRange).Equals
                Return x.Kind.GetValueOrDefault = y.Kind.GetValueOrDefault AndAlso
                    x.StartLine = y.StartLine AndAlso
                    x.EndLine = y.EndLine AndAlso
                    x.StartCharacter.GetValueOrDefault = y.StartCharacter.GetValueOrDefault AndAlso
                    x.EndCharacter.GetValueOrDefault = y.EndCharacter.GetValueOrDefault
            End Function

            Public Shadows Function GetHashCode(obj As FoldingRange) As Integer Implements IEqualityComparer(Of FoldingRange).GetHashCode
                Return Hash.CombineValues(
                {
                    If(obj.Kind?.GetHashCode(), 0),
                    obj.StartLine.GetHashCode(),
                    obj.EndLine.GetHashCode(),
                    If(obj.StartCharacter?.GetHashCode(), 0),
                    If(obj.EndCharacter?.GetHashCode(), 0)
                })
            End Function
        End Class
    End Class
End Namespace
