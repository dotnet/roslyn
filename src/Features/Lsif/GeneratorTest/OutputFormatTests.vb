' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class OutputFormatTests
        <Fact>
        Public Async Function TestLineModeOutput() As Task
            Dim stringWriter = New StringWriter()
            Dim jsonWriter = New LineModeLsifJsonWriter(stringWriter)

            Dim dir = "Z:\" & ChrW(&HE25B)

            Await TestLsifOutput.GenerateForWorkspaceAsync(
                EditorTestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath=<%= dir & "\TestProject.csproj" %>>
                            <Document Name="A.cs" FilePath=<%= dir & "\a.cs" %>/>
                        </Project>
                    </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition), jsonWriter)

            AssertEx.EqualOrDiff(
<<<<<<< HEAD
"{""hoverProvider"":true,""declarationProvider"":false,""definitionProvider"":true,""referencesProvider"":true,""typeDefinitionProvider"":false,""documentSymbolProvider"":true,""foldingRangeProvider"":true,""diagnosticProvider"":false,""semanticTokensProvider"":{""tokenTypes"":[""namespace"",""type"",""class"",""enum"",""interface"",""struct"",""typeParameter"",""parameter"",""variable"",""property"",""enumMember"",""event"",""function"",""method"",""macro"",""keyword"",""modifier"",""comment"",""string"",""number"",""regexp"",""operator"",""class name"",""constant name"",""delegate name"",""enum member name"",""enum name"",""event name"",""excluded code"",""extension method name"",""extension name"",""field name"",""interface name"",""json - array"",""json - comment"",""json - constructor name"",""json - keyword"",""json - number"",""json - object"",""json - operator"",""json - property name"",""json - punctuation"",""json - string"",""json - text"",""keyword - control"",""label name"",""local name"",""method name"",""module name"",""namespace name"",""operator - overloaded"",""parameter name"",""preprocessor keyword"",""preprocessor text"",""property name"",""punctuation"",""record class name"",""record struct name"",""regex - alternation"",""regex - anchor"",""regex - character class"",""regex - comment"",""regex - grouping"",""regex - other escape"",""regex - quantifier"",""regex - self escaped character"",""regex - text"",""roslyn test code markdown"",""string - escape character"",""string - verbatim"",""struct name"",""text"",""type parameter name"",""whitespace"",""xml doc comment - attribute name"",""xml doc comment - attribute quotes"",""xml doc comment - attribute value"",""xml doc comment - cdata section"",""xml doc comment - comment"",""xml doc comment - delimiter"",""xml doc comment - entity reference"",""xml doc comment - name"",""xml doc comment - processing instruction"",""xml doc comment - text"",""xml literal - attribute name"",""xml literal - attribute quotes"",""xml literal - attribute value"",""xml literal - cdata section"",""xml literal - comment"",""xml literal - delimiter"",""xml literal - embedded expression"",""xml literal - entity reference"",""xml literal - name"",""xml literal - processing instruction"",""xml literal - text""],""tokenModifiers"":[""static"",""deprecated""]},""id"":1,""type"":""vertex"",""label"":""capabilities""}
=======
"{""hoverProvider"":true,""declarationProvider"":false,""definitionProvider"":true,""referencesProvider"":true,""typeDefinitionProvider"":false,""documentSymbolProvider"":true,""foldingRangeProvider"":true,""diagnosticProvider"":false,""semanticTokensProvider"":{""tokenTypes"":[""namespace"",""type"",""class"",""enum"",""interface"",""struct"",""typeParameter"",""parameter"",""variable"",""property"",""enumMember"",""event"",""function"",""method"",""macro"",""keyword"",""modifier"",""comment"",""string"",""number"",""regexp"",""operator"",""decorator"",""class name"",""constant name"",""delegate name"",""enum member name"",""enum name"",""event name"",""excluded code"",""extension method name"",""field name"",""interface name"",""json - array"",""json - comment"",""json - constructor name"",""json - keyword"",""json - number"",""json - object"",""json - operator"",""json - property name"",""json - punctuation"",""json - string"",""json - text"",""keyword - control"",""label name"",""local name"",""method name"",""module name"",""namespace name"",""operator - overloaded"",""parameter name"",""preprocessor keyword"",""preprocessor text"",""property name"",""punctuation"",""record class name"",""record struct name"",""regex - alternation"",""regex - anchor"",""regex - character class"",""regex - comment"",""regex - grouping"",""regex - other escape"",""regex - quantifier"",""regex - self escaped character"",""regex - text"",""roslyn test code markdown"",""string - escape character"",""string - verbatim"",""struct name"",""text"",""type parameter name"",""whitespace"",""xml doc comment - attribute name"",""xml doc comment - attribute quotes"",""xml doc comment - attribute value"",""xml doc comment - cdata section"",""xml doc comment - comment"",""xml doc comment - delimiter"",""xml doc comment - entity reference"",""xml doc comment - name"",""xml doc comment - processing instruction"",""xml doc comment - text"",""xml literal - attribute name"",""xml literal - attribute quotes"",""xml literal - attribute value"",""xml literal - cdata section"",""xml literal - comment"",""xml literal - delimiter"",""xml literal - embedded expression"",""xml literal - entity reference"",""xml literal - name"",""xml literal - processing instruction"",""xml literal - text""],""tokenModifiers"":[""static"",""deprecated""]},""id"":1,""type"":""vertex"",""label"":""capabilities""}
>>>>>>> origin/main
{""kind"":""csharp"",""resource"":""file:///Z:/%EE%89%9B/TestProject.csproj"",""name"":""TestProject"",""id"":2,""type"":""vertex"",""label"":""project""}
{""kind"":""begin"",""scope"":""project"",""data"":2,""id"":3,""type"":""vertex"",""label"":""$event""}
{""uri"":""file:///Z:/%EE%89%9B/a.cs"",""languageId"":""csharp"",""id"":4,""type"":""vertex"",""label"":""document""}
{""kind"":""begin"",""scope"":""document"",""data"":4,""id"":5,""type"":""vertex"",""label"":""$event""}
{""outV"":4,""inVs"":[],""id"":6,""type"":""edge"",""label"":""contains""}
{""result"":[],""id"":7,""type"":""vertex"",""label"":""foldingRangeResult""}
{""outV"":4,""inV"":7,""id"":8,""type"":""edge"",""label"":""textDocument/foldingRange""}
{""result"":{""data"":[]},""id"":9,""type"":""vertex"",""label"":""semanticTokensResult""}
{""outV"":4,""inV"":9,""id"":10,""type"":""edge"",""label"":""textDocument/semanticTokens/full""}
{""result"":[],""id"":11,""type"":""vertex"",""label"":""documentSymbolResult""}
{""outV"":4,""inV"":11,""id"":12,""type"":""edge"",""label"":""textDocument/documentSymbol""}
{""kind"":""end"",""scope"":""document"",""data"":4,""id"":13,""type"":""vertex"",""label"":""$event""}
{""outV"":2,""inVs"":[4],""id"":14,""type"":""edge"",""label"":""contains""}
{""kind"":""end"",""scope"":""project"",""data"":2,""id"":15,""type"":""vertex"",""label"":""$event""}
", stringWriter.ToString())
        End Function

        <Fact>
        Public Async Function TestJsonModeOutput() As Task
            Dim stringWriter = New StringWriter()
            Using jsonWriter = New JsonModeLsifJsonWriter(stringWriter)

                Await TestLsifOutput.GenerateForWorkspaceAsync(
                    EditorTestWorkspace.CreateWorkspace(
                        <Workspace>
                            <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj">
                                <Document Name="A.cs" FilePath="Z:\A.cs"/>
                            </Project>
                        </Workspace>, openDocuments:=False, composition:=TestLsifOutput.TestComposition), jsonWriter)
            End Using

            AssertEx.EqualOrDiff(
    "[
  {
    ""hoverProvider"": true,
    ""declarationProvider"": false,
    ""definitionProvider"": true,
    ""referencesProvider"": true,
    ""typeDefinitionProvider"": false,
    ""documentSymbolProvider"": true,
    ""foldingRangeProvider"": true,
    ""diagnosticProvider"": false,
    ""semanticTokensProvider"": {
      ""tokenTypes"": [
        ""namespace"",
        ""type"",
        ""class"",
        ""enum"",
        ""interface"",
        ""struct"",
        ""typeParameter"",
        ""parameter"",
        ""variable"",
        ""property"",
        ""enumMember"",
        ""event"",
        ""function"",
        ""method"",
        ""macro"",
        ""keyword"",
        ""modifier"",
        ""comment"",
        ""string"",
        ""number"",
        ""regexp"",
        ""operator"",
        ""decorator"",
        ""class name"",
        ""constant name"",
        ""delegate name"",
        ""enum member name"",
        ""enum name"",
        ""event name"",
        ""excluded code"",
        ""extension method name"",
        ""extension name"",
        ""field name"",
        ""interface name"",
        ""json - array"",
        ""json - comment"",
        ""json - constructor name"",
        ""json - keyword"",
        ""json - number"",
        ""json - object"",
        ""json - operator"",
        ""json - property name"",
        ""json - punctuation"",
        ""json - string"",
        ""json - text"",
        ""keyword - control"",
        ""label name"",
        ""local name"",
        ""method name"",
        ""module name"",
        ""namespace name"",
        ""operator - overloaded"",
        ""parameter name"",
        ""preprocessor keyword"",
        ""preprocessor text"",
        ""property name"",
        ""punctuation"",
        ""record class name"",
        ""record struct name"",
        ""regex - alternation"",
        ""regex - anchor"",
        ""regex - character class"",
        ""regex - comment"",
        ""regex - grouping"",
        ""regex - other escape"",
        ""regex - quantifier"",
        ""regex - self escaped character"",
        ""regex - text"",
        ""roslyn test code markdown"",
        ""string - escape character"",
        ""string - verbatim"",
        ""struct name"",
        ""text"",
        ""type parameter name"",
        ""whitespace"",
        ""xml doc comment - attribute name"",
        ""xml doc comment - attribute quotes"",
        ""xml doc comment - attribute value"",
        ""xml doc comment - cdata section"",
        ""xml doc comment - comment"",
        ""xml doc comment - delimiter"",
        ""xml doc comment - entity reference"",
        ""xml doc comment - name"",
        ""xml doc comment - processing instruction"",
        ""xml doc comment - text"",
        ""xml literal - attribute name"",
        ""xml literal - attribute quotes"",
        ""xml literal - attribute value"",
        ""xml literal - cdata section"",
        ""xml literal - comment"",
        ""xml literal - delimiter"",
        ""xml literal - embedded expression"",
        ""xml literal - entity reference"",
        ""xml literal - name"",
        ""xml literal - processing instruction"",
        ""xml literal - text""
      ],
      ""tokenModifiers"": [
        ""static"",
        ""deprecated""
      ]
    },
    ""id"": 1,
    ""type"": ""vertex"",
    ""label"": ""capabilities""
  },
  {
    ""kind"": ""csharp"",
    ""resource"": ""file:///Z:/TestProject.csproj"",
    ""name"": ""TestProject"",
    ""id"": 2,
    ""type"": ""vertex"",
    ""label"": ""project""
  },
  {
    ""kind"": ""begin"",
    ""scope"": ""project"",
    ""data"": 2,
    ""id"": 3,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""uri"": ""file:///Z:/A.cs"",
    ""languageId"": ""csharp"",
    ""id"": 4,
    ""type"": ""vertex"",
    ""label"": ""document""
  },
  {
    ""kind"": ""begin"",
    ""scope"": ""document"",
    ""data"": 4,
    ""id"": 5,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""outV"": 4,
    ""inVs"": [],
    ""id"": 6,
    ""type"": ""edge"",
    ""label"": ""contains""
  },
  {
    ""result"": [],
    ""id"": 7,
    ""type"": ""vertex"",
    ""label"": ""foldingRangeResult""
  },
  {
    ""outV"": 4,
    ""inV"": 7,
    ""id"": 8,
    ""type"": ""edge"",
    ""label"": ""textDocument/foldingRange""
  },
  {
    ""result"": {
      ""data"": []
    },
    ""id"": 9,
    ""type"": ""vertex"",
    ""label"": ""semanticTokensResult""
  },
  {
    ""outV"": 4,
    ""inV"": 9,
    ""id"": 10,
    ""type"": ""edge"",
    ""label"": ""textDocument/semanticTokens/full""
  },
  {
    ""result"": [],
    ""id"": 11,
    ""type"": ""vertex"",
    ""label"": ""documentSymbolResult""
  },
  {
    ""outV"": 4,
    ""inV"": 11,
    ""id"": 12,
    ""type"": ""edge"",
    ""label"": ""textDocument/documentSymbol""
  },
  {
    ""kind"": ""end"",
    ""scope"": ""document"",
    ""data"": 4,
    ""id"": 13,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""outV"": 2,
    ""inVs"": [
      4
    ],
    ""id"": 14,
    ""type"": ""edge"",
    ""label"": ""contains""
  },
  {
    ""kind"": ""end"",
    ""scope"": ""project"",
    ""data"": 2,
    ""id"": 15,
    ""type"": ""vertex"",
    ""label"": ""$event""
  }
]", stringWriter.ToString())
        End Function
    End Class
End Namespace
