// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Microsoft.CodeAnalysis.DiagnosticExtensions;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV1ErrorLoggerTests : SarifErrorLoggerTests
    {
        protected override string ErrorLogQualifier => string.Empty;

        internal override string GetExpectedOutputForNoDiagnostics(MockCSharpCompiler cmd)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = @"
      ""results"": [
      ]
    }
  ]
}";
            return expectedHeader + expectedIssues;
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void NoDiagnostics()
        {
            NoDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnostics(MockCSharpCompiler cmd, string sourceFile)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = string.Format(@"
      ""results"": [
        {{
          ""ruleId"": ""CS5001"",
          ""level"": ""error"",
          ""message"": ""Program does not contain a static 'Main' method suitable for an entry point""
        }},
        {{
          ""ruleId"": ""CS0169"",
          ""level"": ""warning"",
          ""message"": ""The field 'C.x' is never used"",
          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{0}"",
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
      ""rules"": {{
        ""CS0169"": {{
          ""id"": ""CS0169"",
          ""shortDescription"": ""Field is never used"",
          ""defaultLevel"": ""warning"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0169)"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry""
            ]
          }}
        }},
        ""CS5001"": {{
          ""id"": ""CS5001"",
          ""defaultLevel"": ""error"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS5001)"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry"",
              ""NotConfigurable""
            ]
          }}
        }}
      }}
    }}
  ]
}}", AnalyzerForErrorLogTest.GetUriForPath(sourceFile));

            return expectedHeader + expectedIssues;
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnostics()
        {
            SimpleCompilerDiagnosticsImpl();
        }

        internal override string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(MockCSharpCompiler cmd, string sourceFile, params string[] suppressionKinds)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = string.Format(@"
      ""results"": [
        {{
          ""ruleId"": ""CS5001"",
          ""level"": ""error"",
          ""message"": ""Program does not contain a static 'Main' method suitable for an entry point""
        }},
        {{
          ""ruleId"": ""CS0169"",
          ""level"": ""warning"",
          ""message"": ""The field 'C.x' is never used"",
          ""suppressionStates"": [
            ""suppressedInSource""
          ],
          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{0}"",
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
      ""rules"": {{
        ""CS0169"": {{
          ""id"": ""CS0169"",
          ""shortDescription"": ""Field is never used"",
          ""defaultLevel"": ""warning"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0169)"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry""
            ]
          }}
        }},
        ""CS5001"": {{
          ""id"": ""CS5001"",
          ""defaultLevel"": ""error"",
          ""helpUri"": ""https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS5001)"",
          ""properties"": {{
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": true,
            ""tags"": [
              ""Compiler"",
              ""Telemetry"",
              ""NotConfigurable""
            ]
          }}
        }}
      }}
    }}
  ]
}}", AnalyzerForErrorLogTest.GetUriForPath(sourceFile));

            return expectedHeader + expectedIssues;
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnosticsSuppressed()
        {
            SimpleCompilerDiagnosticsSuppressedImpl();
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(MockCSharpCompiler cmd)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = AnalyzerForErrorLogTest.GetExpectedV1ErrorLogResultsAndRulesText(cmd.Compilation);
            return expectedHeader + expectedIssues;
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(MockCSharpCompiler cmd, string justification, string suppressionType, params string[] suppressionKinds)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = AnalyzerForErrorLogTest.GetExpectedV1ErrorLogWithSuppressionResultsAndRulesText(cmd.Compilation);
            return expectedHeader + expectedIssues;
        }

        internal override string GetExpectedOutputForAnalyzerDiagnosticsWithWarnAsError(MockCSharpCompiler cmd)
        {
            var expectedHeader = GetExpectedErrorLogHeader(cmd);
            var expectedIssues = AnalyzerForErrorLogTest.GetExpectedV1ErrorLogResultsAndRulesText(cmd.Compilation, warnAsError: true);
            return expectedHeader + expectedIssues;
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
    }
}
