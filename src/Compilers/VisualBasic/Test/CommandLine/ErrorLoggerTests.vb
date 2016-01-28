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
            Dim helloWorldVB As String = <text>
Imports System
Class C
    Shared Sub Main(args As String())
        Console.WriteLine("Hello, world")
    End Sub
End Class
</text>.Value

            Dim hello = Temp.CreateFile().WriteAllText(helloWorldVB).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                 $"/errorlog:{errorLogFile}",
                 hello})
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Assert.Equal("", outWriter.ToString().Trim())
            Assert.Equal(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = "
      ""issues"": [
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
            Dim source As String = <text>
Public Class C
    Public Sub Method()
        Dim x As Integer
    End Sub
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                "/preferreduilang:en",
                 $"/errorlog:{errorLogFile}",
                 sourceFilePath})
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = String.Format("
      ""issues"": [
        {{
          ""ruleId"": ""BC42024"",
          ""locations"": [
            {{
              ""analysisTarget"": [
                {{
                  ""uri"": ""{0}"",
                  ""region"": {{
                    ""startLine"": 4,
                    ""startColumn"": 13,
                    ""endLine"": 4,
                    ""endColumn"": 14
                  }}
                }}
              ]
            }}
          ],
          ""fullMessage"": ""Unused local variable: 'x'."",
          ""properties"": {{
            ""severity"": ""Warning"",
            ""warningLevel"": ""1"",
            ""defaultSeverity"": ""Warning"",
            ""title"": ""Unused local variable"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""False"",
            ""customTags"": ""Compiler;Telemetry""
          }}
        }},
        {{
          ""ruleId"": ""BC30420"",
          ""locations"": [
          ],
          ""fullMessage"": ""'Sub Main' was not found in '{1}'."",
          ""properties"": {{
            ""severity"": ""Error"",
            ""defaultSeverity"": ""Error"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""False"",
            ""customTags"": ""Compiler;Telemetry;NotConfigurable""
          }}
        }}
      ]
    }}
  ]
}}", AnalyzerForErrorLogTest.GetEscapedUriForPath(sourceFilePath), Path.GetFileNameWithoutExtension(sourceFilePath))

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        <Fact>
        Public Sub SimpleCompilerDiagnostics_Suppressed()
            Dim source As String = <text>
Public Class C
    Public Sub Method()
#Disable Warning BC42024
        Dim x As Integer
#Enable Warning BC42024
    End Sub
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                "/preferreduilang:en",
                 $"/errorlog:{errorLogFile}",
                 sourceFilePath})
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            ' Suppressed diagnostics are only report in the error log, not the console output.
            Assert.DoesNotContain("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = String.Format("
      ""issues"": [
        {{
          ""ruleId"": ""BC42024"",
          ""locations"": [
            {{
              ""analysisTarget"": [
                {{
                  ""uri"": ""{0}"",
                  ""region"": {{
                    ""startLine"": 5,
                    ""startColumn"": 13,
                    ""endLine"": 5,
                    ""endColumn"": 14
                  }}
                }}
              ]
            }}
          ],
          ""fullMessage"": ""Unused local variable: 'x'."",
          ""properties"": {{
            ""severity"": ""Warning"",
            ""warningLevel"": ""1"",
            ""defaultSeverity"": ""Warning"",
            ""title"": ""Unused local variable"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""True"",
            ""customTags"": ""Compiler;Telemetry""
          }}
        }},
        {{
          ""ruleId"": ""BC30420"",
          ""locations"": [
          ],
          ""fullMessage"": ""'Sub Main' was not found in '{1}'."",
          ""properties"": {{
            ""severity"": ""Error"",
            ""defaultSeverity"": ""Error"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""False"",
            ""customTags"": ""Compiler;Telemetry;NotConfigurable""
          }}
        }}
      ]
    }}
  ]
}}", AnalyzerForErrorLogTest.GetEscapedUriForPath(sourceFilePath), Path.GetFileNameWithoutExtension(sourceFilePath))

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        <Fact>
        Public Sub AnalyzerDiagnosticsWithAndWithoutLocation()
            Dim source As String = <text>
Imports System
Class C
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim outputDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(outputDir.Path, "ErrorLog.txt")
            Dim outputFilePath = Path.Combine(outputDir.Path, "test.dll")

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                {"/nologo",
                 "/preferreduilang:en",
                 "/t:library",
                 $"/out:{outputFilePath}",
                 $"/errorlog:{errorLogFile}",
                 sourceFilePath},
                analyzer:=New AnalyzerForErrorLogTest())
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput)
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()

            Dim expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd)
            Dim expectedIssues = AnalyzerForErrorLogTest.GetExpectedErrorLogIssuesText(cmd.Compilation)

            Dim expectedText = expectedHeader + expectedIssues
            Assert.Equal(expectedText, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(outputFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub
    End Class
End Namespace