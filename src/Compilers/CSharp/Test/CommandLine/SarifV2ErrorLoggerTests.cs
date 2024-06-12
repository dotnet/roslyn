// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV2ErrorLoggerTests : SarifErrorLoggerTests
    {
        protected override string ErrorLogQualifier => ";version=2";

        internal override string GetExpectedOutputForNoDiagnostics(MockCSharpCompiler cmd)
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
            return FormatOutputText(expectedOutput, cmd, hasAnalyzers: false);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void NoDiagnostics()
        {
            NoDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnostics(MockCSharpCompiler cmd, string sourceFile)
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
              ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS5001)"",
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
              ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0169)"",
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
              hasAnalyzers: false,
              AnalyzerForErrorLogTest.GetUriForPath(sourceFile));

        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnostics()
        {
            SimpleCompilerDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(MockCSharpCompiler cmd, string sourceFile, params string[] suppressionKinds)
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
              ""kind"": ""inSource"",
              ""properties"": {{
                ""suppressionType"": ""Pragma Directive""
              }}
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
              ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS5001)"",
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
              ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0169)"",
              ""properties"": {{
                ""category"": ""Compiler""" + AnalyzerForErrorLogTest.GetExpectedV2SuppressionTextForRulesSection(suppressionKinds) + @",
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
                hasAnalyzers: false,
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
      ""properties"": {{
        ""analyzerExecutionTime"": ""{7}""
      }},
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
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogResultsText(cmd.Compilation),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture));
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(MockCSharpCompiler cmd, string justification, string suppressionType, params string[] suppressionKinds)
        {
            string expectedOutput =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
{5},
      ""properties"": {{
        ""analyzerExecutionTime"": ""{7}""
      }},
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
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogWithSuppressionResultsText(cmd.Compilation, justification, suppressionType),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture, suppressionKinds1: suppressionKinds));
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithWarnAsError(MockCSharpCompiler cmd)
        {
            string expectedOutput =
"""
{{
  "$schema": "http://json.schemastore.org/sarif-2.1.0",
  "version": "2.1.0",
  "runs": [
    {{
{5},
      "properties": {{
        "analyzerExecutionTime": "{8}"
      }},
      "tool": {{
        "driver": {{
          "name": "{0}",
          "version": "{1}",
          "dottedQuadFileVersion": "{2}",
          "semanticVersion": "{3}",
          "language": "{4}",
{6}
        }}
      }},
      {7},
      "columnKind": "utf16CodeUnits"
    }}
  ]
}}
""";
            return FormatOutputText(
                expectedOutput,
                cmd,
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogResultsText(cmd.Compilation, warnAsError: true),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogInvocationsText(
                    (AnalyzerForErrorLogTest.Descriptor1.Id, 0, ImmutableHashSet.Create(ReportDiagnostic.Error))));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void AnalyzerDiagnosticsWithAndWithoutLocation()
        {
            AnalyzerDiagnosticsWithAndWithoutLocationImpl();
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticsSuppressedWithJustification()
        {
            AnalyzerDiagnosticsSuppressedWithJustificationImpl();
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticsSuppressedWithMissingJustification()
        {
            AnalyzerDiagnosticsSuppressedWithMissingJustificationImpl();
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticsSuppressedWithEmptyJustification()
        {
            AnalyzerDiagnosticsSuppressedWithEmptyJustificationImpl();
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticsSuppressedWithNullJustification()
        {
            AnalyzerDiagnosticsSuppressedWithNullJustificationImpl();
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticsWithWarnAsError()
        {
            AnalyzerDiagnosticsWithWarnAsErrorImpl();
        }

        private string FormatOutputText(
          string s,
          MockCSharpCompiler compiler,
          bool hasAnalyzers,
          params object[] additionalArguments)
        {
            if (hasAnalyzers)
            {
                additionalArguments = additionalArguments.Append(compiler.GetAnalyzerExecutionTimeFormattedString());
            }

            var arguments = new object[] {
                compiler.GetToolName(),
                compiler.GetCompilerVersion(),
                compiler.GetAssemblyVersion(),
                compiler.GetAssemblyVersion().ToString(fieldCount: 3),
                compiler.GetCultureName()
            }.Concat(additionalArguments).ToArray();

            return string.Format(CultureInfo.InvariantCulture, s, arguments);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDisabledWithCommandLineOptions()
        {
            var source = @"
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", "/nowarn:ID1", "/nowarn:ID2", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Assert no diagnostics are reported as the analyzer has been disabled with nowarn.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.Equal(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedOutputMarkup =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""results"": [
      ],
      ""properties"": {{
        ""analyzerExecutionTime"": ""{7}""
      }},
      ""tool"": {{
        ""driver"": {{
          ""name"": ""{0}"",
          ""version"": ""{1}"",
          ""dottedQuadFileVersion"": ""{2}"",
          ""semanticVersion"": ""{3}"",
          ""language"": ""{4}"",
{5}
        }}
      }},
      {6},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}";
            var expectedOutput = FormatOutputText(
                expectedOutputMarkup,
                cmd,
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture,
                    suppressionKinds1: new[] { "external" }, suppressionKinds2: new[] { "external" }),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogInvocationsText(
                    (AnalyzerForErrorLogTest.Descriptor1.Id, 0, ImmutableHashSet.Create(ReportDiagnostic.Suppress)),
                    (AnalyzerForErrorLogTest.Descriptor2.Id, 1, ImmutableHashSet.Create(ReportDiagnostic.Suppress))));

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerPartiallyDisabledWithEditorconfig()
        {
            var source1 = @"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""ID1"", Justification = ""Justification1"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""ID2"", Justification = ""Justification2"")]
class C
{
}";
            var source2 = @"
class C2
{
}";
            var editorconfigText = @"
[source2.cs]
dotnet_diagnostic.ID1.severity = none
";
            var sourceDir = Temp.CreateDirectory();
            var sourceFile1 = sourceDir.CreateFile("source1.cs").WriteAllText(source1).Path;
            var sourceFile2 = sourceDir.CreateFile("source2.cs").WriteAllText(source2).Path;
            var editorconfigFile = sourceDir.CreateFile(".editorconfig").WriteAllText(editorconfigText).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile1, sourceFile2, $"/analyzerconfig:{editorconfigFile}", "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Assert suppressed/disabled diagnostics are not reported on the command line.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedOutputMarkup =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
{5},
      ""properties"": {{
        ""analyzerExecutionTime"": ""{8}""
      }},
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
      {7},
      ""columnKind"": ""utf16CodeUnits""
    }}
  ]
}}";
            var expectedOutput = FormatOutputText(
                expectedOutputMarkup,
                cmd,
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogWithSuppressionResultsText(cmd.Compilation, "Justification1", suppressionType: "SuppressMessageAttribute"),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture,
                    suppressionKinds1: new[] { "external", "inSource" }),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogInvocationsText(
                    (AnalyzerForErrorLogTest.Descriptor1.Id, 0, ImmutableHashSet.Create(ReportDiagnostic.Suppress, ReportDiagnostic.Warn))));

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile1);
            CleanupAllGeneratedFiles(sourceFile2);
            CleanupAllGeneratedFiles(editorconfigFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AnalyzerDiagnosticSuppressedWithDiagnosticSuppressor()
        {
            var source = @"
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new DiagnosticAnalyzer[] { new AnalyzerForErrorLogTest(), new SuppressorForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Assert suppressed/disabled diagnostics are not reported on the command line.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedOutputMarkup =
@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
{5},
      ""properties"": {{
        ""analyzerExecutionTime"": ""{7}""
      }},
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
            var expectedOutput = FormatOutputText(
                expectedOutputMarkup,
                cmd,
                hasAnalyzers: true,
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogWithSuppressionResultsText(cmd.Compilation,
                    null,
                    suppressionType: $"DiagnosticSuppressor {{ Suppression Id: {SuppressorForErrorLogTest.Descriptor1.Id}, Suppression Justification: {SuppressorForErrorLogTest.Descriptor1.Justification} }}"),
                AnalyzerForErrorLogTest.GetExpectedV2ErrorLogRulesText(cmd.DescriptorsWithInfo, CultureInfo.InvariantCulture, suppressionKinds1: new[] { "inSource" }));

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }
    }
}
