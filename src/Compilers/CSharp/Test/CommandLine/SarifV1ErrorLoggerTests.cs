// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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

        internal override string GetExpectedOutputForNoDiagnostics(CommonCompiler cmd)
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

        internal override string GetExpectedOutputForSimpleCompilerDiagnostics(CommonCompiler cmd, string sourceFile)
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

        internal override string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(CommonCompiler cmd, string sourceFile)
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

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void AnalyzerDiagnosticsWithAndWithoutLocation()
        {
            AnalyzerDiagnosticsWithAndWithoutLocationImpl();
        }
    }
}
