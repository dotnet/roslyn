// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV2ErrorLoggerTests : SarifErrorLoggerTests
    {
        protected override string[] VersionSpecificArguments => new[] { "/sarifversion:2" };

        internal override string GetExpectedOutputForNoDiagnostics(CommonCompiler cmd)
        {
            string expectedOutput =
@"{{
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
}}";
            return FormatOutputText(expectedOutput, cmd);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void NoDiagnostics()
        {
            NoDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnostics(CommonCompiler cmd, string sourceFile)
        {
            string expectedOutput =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
        {{
          ""ruleId"": ""CS5001"",
          ""ruleIndex"": 0,
          ""level"": ""error"",
          ""message"": {{
            ""text"": ""Program does not contain a static 'Main' method suitable for an entry point""
          }}
        }},
        {{
          ""ruleId"": ""CS0169"",
          ""ruleIndex"": 1,
          ""level"": ""warning"",
          ""message"": {{
            ""text"": ""The field 'C.x' is never used""
          }},
          ""locations"": [
            {{
              ""physicalLocation"": {{
                ""artifactLocation"": {{
                  ""uri"": ""{5}""
                }},
                ""region"": {{
                  ""startLine"": 4,
                  ""startColumn"": 17,
                  ""endLine"": 4,
                  ""endColumn"": 18
                }}
              }}
            }}
          ],
          ""properties"": {{
            ""warningLevel"": 3
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
              ""id"": ""CS5001"",
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
            }},
            {{
              ""id"": ""CS0169"",
              ""shortDescription"": {{
                ""text"": ""Field is never used""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry""
                ]
              }}
            }}
          ]
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}";

            return FormatOutputText(
              expectedOutput,
              cmd,
              AnalyzerForErrorLogTest.GetUriForPath(sourceFile));

        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnostics()
        {
            SimpleCompilerDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(CommonCompiler cmd, string sourceFile)
        {
            string expectedOutput =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
        {{
          ""ruleId"": ""CS5001"",
          ""ruleIndex"": 0,
          ""level"": ""error"",
          ""message"": {{
            ""text"": ""Program does not contain a static 'Main' method suitable for an entry point""
          }}
        }},
        {{
          ""ruleId"": ""CS0169"",
          ""ruleIndex"": 1,
          ""level"": ""warning"",
          ""message"": {{
            ""text"": ""The field 'C.x' is never used""
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
                  ""startColumn"": 17,
                  ""endLine"": 5,
                  ""endColumn"": 18
                }}
              }}
            }}
          ],
          ""properties"": {{
            ""warningLevel"": 3
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
              ""id"": ""CS5001"",
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
            }},
            {{
              ""id"": ""CS0169"",
              ""shortDescription"": {{
                ""text"": ""Field is never used""
              }},
              ""properties"": {{
                ""category"": ""Compiler"",
                ""tags"": [
                  ""Compiler"",
                  ""Telemetry""
                ]
              }}
            }}
          ]
        }}
      }},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}";

            return FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetUriForPath(sourceFile));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnosticsSuppressed()
        {
            SimpleCompilerDiagnosticsSuppressedImpl();
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(MockCSharpCompiler cmd)
        {
            string expectedOutput =
@"{{
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
}}";
            return FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogResultsText(cmd.Compilation),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText());
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void AnalyzerDiagnosticsWithAndWithoutLocation()
        {
            AnalyzerDiagnosticsWithAndWithoutLocationImpl();
        }

        private string FormatOutputText(
          string s,
          CommonCompiler compiler,
          params object[] additionalArguments)
        {
            var arguments = new object[] {
                compiler.GetToolName(),
                compiler.GetCompilerVersion(),
                compiler.GetAssemblyVersion(),
                compiler.GetAssemblyVersion().ToString(fieldCount: 3),
                compiler.GetCultureName()
            }.Concat(additionalArguments).ToArray();

            return string.Format(CultureInfo.InvariantCulture, s, arguments);
        }
    }
}
