' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.DiagnosticExtensions

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    <Trait(Traits.Feature, Traits.Features.SarifErrorLogging)>
    Public Class SarifV1ErrorLoggerTests
        Inherits SarifErrorLoggerTests

        Protected Overrides ReadOnly Property ErrorLogQualifier As String
            Get
                Return String.Empty
            End Get
        End Property

        Friend Overrides Function GetExpectedOutputForNoDiagnostics(
            cmd As MockVisualBasicCompiler) As String

            Dim expectedHeader = GetExpectedErrorLogHeader(cmd)
            Dim expectedIssues = "
      ""results"": [
      ]
    }
  ]
}"

            Return expectedHeader + expectedIssues
        End Function

        <Fact>
        Public Sub NoDiagnostics()
            NoDiagnosticsImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForSimpleCompilerDiagnostics(
            cmd As MockVisualBasicCompiler,
            sourceFilePath As String) As String

            Dim expectedHeader = GetExpectedErrorLogHeader(cmd)
            Dim expectedIssues = String.Format("
      ""results"": [
        {{
          ""ruleId"": ""BC42024"",
          ""level"": ""warning"",
          ""message"": ""Unused local variable: 'x'."",
          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{0}"",
                ""region"": {{
                  ""startLine"": 4,
                  ""startColumn"": 13,
                  ""endLine"": 4,
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
          ""message"": ""'Sub Main' was not found in '{1}'.""
        }}
      ],
      ""rules"": {{
        ""BC30420"": {{
          ""id"": ""BC30420"",
          ""defaultLevel"": ""error"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC30420)"",
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
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC42024)"",
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
}}", AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath), Path.GetFileNameWithoutExtension(sourceFilePath))

            Return expectedHeader + expectedIssues
        End Function

        <Fact>
        Public Sub SimpleCompilerDiagnostics()
            SimpleCompilerDiagnosticsImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(
            cmd As MockVisualBasicCompiler,
            sourceFilePath As String,
            ParamArray suppressionKinds As String()) As String

            Dim expectedHeader = GetExpectedErrorLogHeader(cmd)
            Dim expectedIssues = String.Format("
      ""results"": [
        {{
          ""ruleId"": ""BC42024"",
          ""level"": ""warning"",
          ""message"": ""Unused local variable: 'x'."",
          ""suppressionStates"": [
            ""suppressedInSource""
          ],
          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{0}"",
                ""region"": {{
                  ""startLine"": 5,
                  ""startColumn"": 13,
                  ""endLine"": 5,
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
          ""message"": ""'Sub Main' was not found in '{1}'.""
        }}
      ],
      ""rules"": {{
        ""BC30420"": {{
          ""id"": ""BC30420"",
          ""defaultLevel"": ""error"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC30420)"",
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
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC42024)"",
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
}}", AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath), Path.GetFileNameWithoutExtension(sourceFilePath))

            Return expectedHeader + expectedIssues
        End Function

        <Fact>
        Public Sub SimpleCompilerDiagnosticsSuppressed()
            SimpleCompilerDiagnosticsSuppressedImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(
            cmd As MockVisualBasicCompiler) As String

            Dim expectedHeader = GetExpectedErrorLogHeader(cmd)
            Dim expectedIssues = AnalyzerForErrorLogTest.GetExpectedV1ErrorLogResultsAndRulesText(cmd.Compilation)

            Return expectedHeader + expectedIssues
        End Function

        <Fact>
        Public Sub AnalyzerDiagnosticsWithAndWithoutLocation()
            AnalyzerDiagnosticsWithAndWithoutLocationImpl()
        End Sub
    End Class
End Namespace
