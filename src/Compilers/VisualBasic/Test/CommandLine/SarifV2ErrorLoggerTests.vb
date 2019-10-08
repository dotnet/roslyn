' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    <Trait(Traits.Feature, Traits.Features.SarifErrorLogging)>
    Public Class SarifV2ErrorLoggerTests
        Inherits SarifErrorLoggerTests

        Protected Overrides ReadOnly Property ErrorLogQualifier As String
            Get
                Return ";version=2"
            End Get
        End Property

        Friend Overrides Function GetExpectedOutputForNoDiagnostics(
            cmd As CommonCompiler) As String

            Dim expectedOutput = "{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
      ],
      ""tool"": {{
        ""driver"": {{
          ""name"": ""{0}"",
          ""version"": ""{1}"",
          ""dottedQuadFileVersion"": ""{2}"",
          ""semanticVersion"": ""{3}"",
          ""language"": ""{4}""
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}"

            Return FormatOutputText(expectedOutput, cmd)
        End Function

        <Fact>
        Public Sub NoDiagnostics()
            NoDiagnosticsImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForSimpleCompilerDiagnostics(
            cmd As CommonCompiler,
            sourceFilePath As String) As String

            Dim expectedOutput = "{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
        {{
          ""ruleId"": ""BC42024"",
          ""ruleIndex"": 0,
          ""level"": ""warning"",
          ""message"": {{
            ""text"": ""Unused local variable: 'x'.""
          }},
          ""locations"": [
            {{
              ""physicalLocation"": {{
                ""artifactLocation"": {{
                  ""uri"": ""{5}""
                }},
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
          ""ruleIndex"": 1,
          ""level"": ""error"",
          ""message"": {{
            ""text"": ""'Sub Main' was not found in '{6}'.""
          }}
        }}
      ],
      ""tool"": {{
        ""driver"": {{
          ""name"": ""{0}"",
          ""version"": ""{1}"",
          ""dottedQuadFileVersion"": ""{2}"",
          ""semanticVersion"": ""{3}"",
          ""language"": ""{4}"",
          ""rules"": [
            {{
              ""id"": ""BC42024"",
              ""shortDescription"": {{
                ""text"": ""Unused local variable""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry""
                ]
              }}
            }},
            {{
              ""id"": ""BC30420"",
              ""defaultConfiguration"": {{
                ""level"": ""error""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry"",
                  ""NotConfigurable""
                ]
              }}
            }}
          ]
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}"

            Return FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath),
                Path.GetFileNameWithoutExtension(sourceFilePath))
        End Function

        <Fact>
        Public Sub SimpleCompilerDiagnostics()
            SimpleCompilerDiagnosticsImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(
            cmd As CommonCompiler,
            sourceFilePath As String) As String

            Dim expectedOutput = "{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
        {{
          ""ruleId"": ""BC42024"",
          ""ruleIndex"": 0,
          ""level"": ""warning"",
          ""message"": {{
            ""text"": ""Unused local variable: 'x'.""
          }},
          ""suppressions"": [
            {{
              ""kind"": ""inSource""
            }}
          ],
          ""locations"": [
            {{
              ""physicalLocation"": {{
                ""artifactLocation"": {{
                  ""uri"": ""{5}""
                }},
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
          ""ruleIndex"": 1,
          ""level"": ""error"",
          ""message"": {{
            ""text"": ""'Sub Main' was not found in '{6}'.""
          }}
        }}
      ],
      ""tool"": {{
        ""driver"": {{
          ""name"": ""{0}"",
          ""version"": ""{1}"",
          ""dottedQuadFileVersion"": ""{2}"",
          ""semanticVersion"": ""{3}"",
          ""language"": ""{4}"",
          ""rules"": [
            {{
              ""id"": ""BC42024"",
              ""shortDescription"": {{
                ""text"": ""Unused local variable""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry""
                ]
              }}
            }},
            {{
              ""id"": ""BC30420"",
              ""defaultConfiguration"": {{
                ""level"": ""error""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry"",
                  ""NotConfigurable""
                ]
              }}
            }}
          ]
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}"

            Return FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetUriForPath(sourceFilePath),
                Path.GetFileNameWithoutExtension(sourceFilePath))
        End Function

        <Fact>
        Public Sub SimpleCompilerDiagnosticsSuppressed()
            SimpleCompilerDiagnosticsSuppressedImpl()
        End Sub

        Friend Overrides Function GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(
            cmd As MockVisualBasicCompiler) As String

            Dim expectedOutput =
"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
{5},
      ""tool"": {{
        ""driver"": {{
          ""name"": ""{0}"",
          ""version"": ""{1}"",
          ""dottedQuadFileVersion"": ""{2}"",
          ""semanticVersion"": ""{3}"",
          ""language"": ""{4}"",
{6}
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}"
            Return FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogResultsText(cmd.Compilation),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText())
        End Function

        <Fact>
        Public Sub AnalyzerDiagnosticsWithAndWithoutLocation()
            AnalyzerDiagnosticsWithAndWithoutLocationImpl()
        End Sub

        Private Function FormatOutputText(s As String, compiler as CommonCompiler, ParamArray additionalArguments() As Object) As String
            Dim arguments = New List(Of Object) From {
                compiler.GetToolName(),
                compiler.GetCompilerVersion(),
                compiler.GetAssemblyVersion(),
                compiler.GetAssemblyVersion().ToString(fieldCount:=3),
                compiler.GetCultureName()
            }.Concat(additionalArguments).ToArray()

            Return string.Format(CultureInfo.InvariantCulture, s, arguments)
        End Function
    End Class
End Namespace
