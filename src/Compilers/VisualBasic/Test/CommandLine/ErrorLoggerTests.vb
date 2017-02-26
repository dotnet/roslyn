' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit

Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.DiagnosticExtensions
Imports Roslyn.Test.Utilities.SharedResourceHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Public Class ErrorLoggerTests
        Inherits BasicTestBase

        Private ReadOnly _baseDirectory As String = TempRoot.Root

        <Fact>
        Public Sub NoDiagnostics()
            Dim helloWorldVB As String = "
Imports System
Class C
    Shared Sub Main(args As String())
        Console.WriteLine(""Hello, world"")
    End Sub
End Class
"

            Dim hello = Temp.CreateFile().WriteAllText(helloWorldVB).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd = New MockVisualBasicCompiler(Nothing,
                                                   _baseDirectory,
                                                   {"/nologo",
                                                     "/errorlog:" & errorLogFile,
                                                     hello
                                                   }
                                                 )

            Dim outWriter As New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Assert.Equal("", outWriter.ToString().Trim())
            Assert.Equal(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = "
      ""results"": [
      ]
    }
  ]
}"

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(hello)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        <Fact>
        Public Sub SimpleCompilerDiagnostics()
            Dim source As String = "
Public Class C
    Public Sub Method()
        Dim x As Integer
    End Sub
End Class
"

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd As New MockVisualBasicCompiler(Nothing,
                                                    _baseDirectory,
                                                    {"/nologo",
                                                      "/preferreduilang:en",
                                                      "/errorlog:" & errorLogFile,
                                                      sourceFilePath
                                                    }
                                                  )
            Dim outWriter As New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = MakeExpected(suppressions:="",
                                               uri:=AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath),
                                               message:=Path.GetFileNameWithoutExtension(sourceFilePath),
                                               startLine:=4,
                                               endLine:=4)

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        <Fact>
        Public Sub SimpleCompilerDiagnostics_Suppressed()
            Dim source As String = "
Public Class C
    Public Sub Method()
#Disable Warning BC42024
        Dim x As Integer
#Enable Warning BC42024
    End Sub
End Class
"

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd As New MockVisualBasicCompiler(Nothing,
                                                    _baseDirectory,
                                                    {"/nologo",
                                                      "/preferreduilang:en",
                                                      "/errorlog:" & errorLogFile,
                                                      sourceFilePath
                                                    })
            Dim outWriter As New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            ' Suppressed diagnostics are only report in the error log, not the console output.
            Assert.DoesNotContain("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = MakeExpected(suppressions:=
"          ""suppressionStates"": [
            ""suppressedInSource""
          ],
", uri:=AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath),
                                              message:=Path.GetFileNameWithoutExtension(sourceFilePath),
                                              startLine:=5,
                                              endLine:=5)

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        <Fact>
        Public Sub AnalyzerDiagnosticsWithAndWithoutLocation()
            Dim source As String = "
Imports System
Class C
End Class
"

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim outputDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(outputDir.Path, "ErrorLog.txt")
            Dim outputFilePath = Path.Combine(outputDir.Path, "test.dll")

            Dim cmd As New MockVisualBasicCompiler(Nothing,
                                                    _baseDirectory,
                                                    {"/nologo",
                                                      "/preferreduilang:en",
                                                      "/t:library",
                                                      "/out:" & outputFilePath,
                                                      "/errorlog:" & errorLogFile,
                                                      sourceFilePath
                                                    },
                                                    analyzer:=New AnalyzerForErrorLogTest()
                                                  )

            Dim outWriter As New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput)
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()
            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = AnalyzerForErrorLogTest.GetExpectedErrorLogResultsText(cmd.Compilation)
            Dim expectedText = expectedHeader + expectedIssues

            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(outputFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub


        Function MakeExpected(suppressions As String, uri As String, message As String, startLine As Int32, endLine As Int32) As String
            Return $"
      ""results"": [
        {{
          ""ruleId"": ""BC42024"",
          ""level"": ""warning"",
          ""message"": ""Unused local variable: 'x'."",
{suppressions}          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{uri}"",
                ""region"": {{
                  ""startLine"": {startLine},
                  ""startColumn"": 13,
                  ""endLine"": {endLine},
                  ""endColumn"": 14
                }}
              }}
            }}
          ],
          ""properties"": {{
            ""warningLevel"": 1
          }}
        }},
        {{
          ""ruleId"": ""BC30420"",
          ""level"": ""error"",
          ""message"": ""'Sub Main' was not found in '{message}'.""
        }}
      ],
      ""rules"": {{
        ""BC30420"": {{
          ""id"": ""BC30420"",
          ""defaultLevel"": ""error"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry"",
              ""NotConfigurable""
            ]
          }}
        }},
        ""BC42024"": {{
          ""id"": ""BC42024"",
          ""shortDescription"": ""Unused local variable"",
          ""defaultLevel"": ""warning"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry""
            ]
          }}
        }}
      }}
    }}
  ]
}}"
        End Function
    End Class
End Namespace