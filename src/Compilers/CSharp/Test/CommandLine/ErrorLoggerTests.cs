// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Microsoft.CodeAnalysis.DiagnosticExtensions;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class ErrorLoggerTests : CommandLineTestBase
    {
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void NoDiagnostics()
        {
            var helloWorldCS = @"using System;

class C
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""Hello, world"");
    }
}";
            var hello = Temp.CreateFile().WriteAllText(helloWorldCS).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            var cmd = CreateCSharpCompiler(new[] { "/nologo", hello, $"/errorlog:{errorLogFile}" });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);

            Assert.Equal("", outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
            var expectedIssues = @"
      ""results"": [
      ]
    }
  ]
}";
            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(hello);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnostics()
        {
            var source = @"
public class C
{
    private int x;
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] {
                "/nologo", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}" });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains("CS0169", actualConsoleOutput);
            Assert.Contains("CS5001", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
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

            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SimpleCompilerDiagnostics_Suppressed()
        {
            var source = @"
public class C
{
#pragma warning disable CS0169
    private int x;
#pragma warning restore CS0169
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] {
                "/nologo", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}" });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only report in the error log, not the console output.
            Assert.DoesNotContain("CS0169", actualConsoleOutput);
            Assert.Contains("CS5001", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
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

            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
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
                "/nologo", "/t:library", $"/out:{outputFilePath}", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}" },
               analyzers: ImmutableArray.Create<DiagnosticAnalyzer>(new AnalyzerForErrorLogTest()));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput);
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
            var expectedIssues = AnalyzerForErrorLogTest.GetExpectedErrorLogResultsText(cmd.Compilation);
            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(outputFilePath);
            CleanupAllGeneratedFiles(errorLogFile);
        }
    }
}
