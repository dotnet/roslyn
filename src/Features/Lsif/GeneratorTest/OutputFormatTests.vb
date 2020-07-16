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

            Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj">
                            <Document Name="A.cs" FilePath="Z:\A.cs"/>
                        </Project>
                    </Workspace>), jsonWriter)

            AssertEx.EqualOrDiff(
"{""kind"":""csharp"",""resource"":""file:///Z:/TestProject.csproj"",""id"":1,""type"":""vertex"",""label"":""project""}
{""kind"":""begin"",""scope"":""project"",""data"":1,""id"":2,""type"":""vertex"",""label"":""$event""}
{""uri"":""file:///Z:/A.cs"",""languageId"":""csharp"",""id"":3,""type"":""vertex"",""label"":""document""}
{""kind"":""begin"",""scope"":""document"",""data"":3,""id"":4,""type"":""vertex"",""label"":""$event""}
{""outV"":3,""inVs"":[],""id"":5,""type"":""edge"",""label"":""contains""}
{""kind"":""end"",""scope"":""document"",""data"":3,""id"":6,""type"":""vertex"",""label"":""$event""}
{""outV"":1,""inVs"":[3],""id"":7,""type"":""edge"",""label"":""contains""}
{""kind"":""end"",""scope"":""project"",""data"":1,""id"":8,""type"":""vertex"",""label"":""$event""}
", stringWriter.ToString())
        End Function

        <Fact>
        Public Async Function TestJsonModeOutput() As Task
            Dim stringWriter = New StringWriter()
            Using jsonWriter = New JsonModeLsifJsonWriter(stringWriter)

                Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj">
                            <Document Name="A.cs" FilePath="Z:\A.cs"/>
                        </Project>
                    </Workspace>), jsonWriter)
            End Using

            AssertEx.EqualOrDiff(
    "[
  {
    ""kind"": ""csharp"",
    ""resource"": ""file:///Z:/TestProject.csproj"",
    ""id"": 1,
    ""type"": ""vertex"",
    ""label"": ""project""
  },
  {
    ""kind"": ""begin"",
    ""scope"": ""project"",
    ""data"": 1,
    ""id"": 2,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""uri"": ""file:///Z:/A.cs"",
    ""languageId"": ""csharp"",
    ""id"": 3,
    ""type"": ""vertex"",
    ""label"": ""document""
  },
  {
    ""kind"": ""begin"",
    ""scope"": ""document"",
    ""data"": 3,
    ""id"": 4,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""outV"": 3,
    ""inVs"": [],
    ""id"": 5,
    ""type"": ""edge"",
    ""label"": ""contains""
  },
  {
    ""kind"": ""end"",
    ""scope"": ""document"",
    ""data"": 3,
    ""id"": 6,
    ""type"": ""vertex"",
    ""label"": ""$event""
  },
  {
    ""outV"": 1,
    ""inVs"": [
      3
    ],
    ""id"": 7,
    ""type"": ""edge"",
    ""label"": ""contains""
  },
  {
    ""kind"": ""end"",
    ""scope"": ""project"",
    ""data"": 1,
    ""id"": 8,
    ""type"": ""vertex"",
    ""label"": ""$event""
  }
]", stringWriter.ToString())
        End Function
    End Class
End Namespace
