// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using Microsoft.CodeAnalysis.Test.Utilities;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

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

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void AnalyzerDiagnosticsWithAndWithoutLocation()
        {
            var source = @"
public class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var outputDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(outputDir.Path, "ErrorLog.txt");
            var outputFilePath = Path.Combine(outputDir.Path, "test.dll");

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] {
                "/nologo", "/t:library", $"/out:{outputFilePath}", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}", "/sarifversion:2" },
               analyzers: ImmutableArray.Create<DiagnosticAnalyzer>(new AnalyzerForErrorLogTest()));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput);
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedOutput =
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
            expectedOutput = FormatOutputText(
                expectedOutput,
                cmd,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogResultsText(cmd.Compilation),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText());

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(outputFilePath);
            CleanupAllGeneratedFiles(errorLogFile);
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
