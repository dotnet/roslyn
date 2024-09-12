' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Linq
Imports System.Text.Json
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests

    <UseExportProvider>
    Public NotInheritable Class SemanticTokensTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        <Theory>
        <InlineData("", "{""data"":[]}")>
        <InlineData("namespace Foo;", "{""data"":[0,0,9,15,0,0,10,3,49,0,0,3,1,55,0]}")>
        <InlineData("namespace " & vbCrLf & " Foo {}", "{""data"":[0,0,9,15,0,2,1,3,49,0,0,4,1,55,0,0,1,1,55,0]}")>
<<<<<<< HEAD
        <InlineData("public class Foo { System.Collections.IList FooList { get; set; } int FooMethod() { int x = 0; return x; } }", "{""data"":[0,0,6,15,0,0,7,5,15,0,0,6,3,22,0,0,4,1,55,0,0,2,6,49,0,0,6,1,21,0,0,1,11,49,0,0,11,1,21,0,0,1,5,32,0,0,6,7,54,0,0,8,1,55,0,0,2,3,15,0,0,3,1,55,0,0,2,3,15,0,0,3,1,55,0,0,2,1,55,0,0,2,3,15,0,0,4,9,47,0,0,9,1,55,0,0,1,1,55,0,0,2,1,55,0,0,2,3,15,0,0,4,1,46,0,0,2,1,21,0,0,2,1,19,0,0,1,1,55,0,0,2,6,44,0,0,7,1,46,0,0,1,1,55,0,0,2,1,55,0,0,2,1,55,0]}")>
=======
        <InlineData("public class Foo { System.Collections.IList FooList { get; set; } int FooMethod() { int x = 0; return x; } }", "{""data"":[0,0,6,15,0,0,7,5,15,0,0,6,3,23,0,0,4,1,55,0,0,2,6,49,0,0,6,1,21,0,0,1,11,49,0,0,11,1,21,0,0,1,5,32,0,0,6,7,54,0,0,8,1,55,0,0,2,3,15,0,0,3,1,55,0,0,2,3,15,0,0,3,1,55,0,0,2,1,55,0,0,2,3,15,0,0,4,9,47,0,0,9,1,55,0,0,1,1,55,0,0,2,1,55,0,0,2,3,15,0,0,4,1,46,0,0,2,1,21,0,0,2,1,19,0,0,1,1,55,0,0,2,6,44,0,0,7,1,46,0,0,1,1,55,0,0,2,1,55,0,0,2,1,55,0]}")>
>>>>>>> origin/main
        Public Async Function TestSemanticTokensData(code As String, expectedTokens As String) As Task
            ' This test performs LSIF specific validation of the semantic tokens output. As of this writing
            ' this feature is based on the same code path used to generate semantic tokens information in LSP
            ' so it is assumed that exhaustive test coverage comes from that scenario.
            '
            ' LSIF Cases of interest:
            ' - Empty file - generates empty data
            ' - Single line
            ' - Multi-line
            ' - Roslyn custom token type ('x' in 'int x' uses a Roslyn custom token type).
            ' - Token in last line with no trailing newline (ensure sufficient range was passed to LSP generator).

            Using semanticTokensWorkspace = EditorTestWorkspace.CreateWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        <Document Name="A.cs" FilePath="Z:\A.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition)

                Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(semanticTokensWorkspace)
                Dim document = semanticTokensWorkspace.CurrentSolution.Projects.Single().Documents.Single()

                Dim tokens = lsif.GetSemanticTokens(document)
                Dim serializedTokens = JsonSerializer.Serialize(tokens)

                Assert.Equal(expectedTokens, serializedTokens)
            End Using
        End Function
    End Class

End Namespace
