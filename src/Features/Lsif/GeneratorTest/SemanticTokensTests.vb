' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Newtonsoft.Json

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests

    <UseExportProvider>
    Public NotInheritable Class SemanticTokensTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        <Theory>
        <InlineData("", "{""data"":[]}")>
        <InlineData("namespace Foo;", "{""data"":[0,0,9,15,0,0,10,3,48,0,0,3,1,54,0]}")>
        <InlineData("namespace " & vbCrLf & " Foo {}", "{""data"":[0,0,9,15,0,2,1,3,48,0,0,4,1,54,0,0,1,1,54,0]}")>
        <InlineData("public class Foo { System.Collections.IList FooList { get; set; } int FooMethod() { int x = 0; return x; } }", "{""data"":[0,0,6,15,0,0,7,5,15,0,0,6,3,22,0,0,4,1,54,0,0,2,6,48,0,0,6,1,21,0,0,1,11,48,0,0,11,1,21,0,0,1,5,31,0,0,6,7,53,0,0,8,1,54,0,0,2,3,15,0,0,3,1,54,0,0,2,3,15,0,0,3,1,54,0,0,2,1,54,0,0,2,3,15,0,0,4,9,46,0,0,9,1,54,0,0,1,1,54,0,0,2,1,54,0,0,2,3,15,0,0,4,1,45,0,0,2,1,21,0,0,2,1,19,0,0,1,1,54,0,0,2,6,43,0,0,7,1,45,0,0,1,1,54,0,0,2,1,54,0,0,2,1,54,0]}")>
        Public Async Function TestSemanticTokensData(code As String, expectedTokens As String) As Task
            ' This test performs LSIF specific validation of the semantic tokens output. As of this writing
            ' this feature is based on the same code path used to generate semantic tokens information in LSP
            ' so it is assumed that exhaustive test coverage comes from that scenario.
            '
            ' LSIF Cases of interest:
            ' - Empty file - generates empty data
            ' - Single line
            ' - Multi-line
            ' - Roslyn custom token type ('x' in 'int x' uses a Roslyn custom token type.
            ' - Token in last line with no trailing newline (ensure sufficient range was passed to LSP generator).

            Using semanticTokensWorkspace = TestWorkspace.CreateWorkspace(
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
                Dim serializedTokens = JsonConvert.SerializeObject(tokens)

                Assert.Equal(expectedTokens, serializedTokens)
            End Using
        End Function

        <Fact>
        Public Async Function TestSemanticTokensCapability() As Task
            ' This test validates that the expected LSIF token types are generated.
            '
            ' Roslyn's notion of 'semantic tokens' doesn't follow the strict definition
            ' of syntax vs semantic, it's divided up into in-proc quick-tokenization
            ' colorizer and _everything else_, generated via LSP.
            '
            ' Because of this we emit both semantic and syntatic tokens so that consumers
            ' of the LSIF format have a single consistent palette of colors without missing
            ' some traditionally 'semantic' tokens.
            Const SerializedSemanticTokensCapabilities = "[""namespace"",""type"",""class"",""enum"",""interface"",""struct"",""typeParameter"",""parameter"",""variable"",""property"",""enumMember"",""event"",""function"",""method"",""macro"",""keyword"",""modifier"",""comment"",""string"",""number"",""regexp"",""operator"",""class name"",""constant name"",""delegate name"",""enum member name"",""enum name"",""event name"",""excluded code"",""extension method name"",""field name"",""interface name"",""json - array"",""json - comment"",""json - constructor name"",""json - keyword"",""json - number"",""json - object"",""json - operator"",""json - property name"",""json - punctuation"",""json - string"",""json - text"",""keyword - control"",""label name"",""local name"",""method name"",""module name"",""namespace name"",""operator - overloaded"",""parameter name"",""preprocessor keyword"",""preprocessor text"",""property name"",""punctuation"",""record class name"",""record struct name"",""regex - alternation"",""regex - anchor"",""regex - character class"",""regex - comment"",""regex - grouping"",""regex - other escape"",""regex - quantifier"",""regex - self escaped character"",""regex - text"",""string - escape character"",""string - verbatim"",""struct name"",""text"",""type parameter name"",""whitespace"",""xml doc comment - attribute name"",""xml doc comment - attribute quotes"",""xml doc comment - attribute value"",""xml doc comment - cdata section"",""xml doc comment - comment"",""xml doc comment - delimiter"",""xml doc comment - entity reference"",""xml doc comment - name"",""xml doc comment - processing instruction"",""xml doc comment - text"",""xml literal - attribute name"",""xml literal - attribute quotes"",""xml literal - attribute value"",""xml literal - cdata section"",""xml literal - comment"",""xml literal - delimiter"",""xml literal - embedded expression"",""xml literal - entity reference"",""xml literal - name"",""xml literal - processing instruction"",""xml literal - text""]"

            Using semanticTokensWorkspace = TestWorkspace.CreateWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        <Document Name="A.cs" FilePath="Z:\A.cs"/>
                    </Project>
                </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition)

                Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(semanticTokensWorkspace)

                Dim capabilitiesVertex = lsif.Vertices _
                    .OfType(Of Capabilities) _
                    .Single()

                Assert.Equal(SerializedSemanticTokensCapabilities, JsonConvert.SerializeObject(capabilitiesVertex.SemanticTokensProvider))
            End Using
        End Function
    End Class

End Namespace
