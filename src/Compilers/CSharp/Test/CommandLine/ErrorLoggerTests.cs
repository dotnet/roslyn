// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Microsoft.CodeAnalysis.DiagnosticExtensions;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class ErrorLoggerTests : CSharpTestBase
    {
        private readonly string _baseDirectory = TempRoot.Root;

        [Fact]
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

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", hello,
               $"/errorlog:{errorLogFile}" });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);

            Assert.Equal("", outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
            var expectedIssues = @"
      ""issues"": [
      ]
    }
  ]
}";
            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(hello);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [Fact]
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

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] {
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
      ""issues"": [
        {{
          ""ruleId"": ""CS0169"",
          ""locations"": [
            {{
              ""analysisTarget"": [
                {{
                  ""uri"": ""{0}"",
                  ""region"": {{
                    ""startLine"": 4,
                    ""startColumn"": 17,
                    ""endLine"": 4,
                    ""endColumn"": 18
                  }}
                }}
              ]
            }}
          ],
          ""fullMessage"": ""The field 'C.x' is never used"",
          ""properties"": {{
            ""severity"": ""Warning"",
            ""warningLevel"": ""3"",
            ""defaultSeverity"": ""Warning"",
            ""title"": ""Field is never used"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""False"",
            ""customTags"": ""Compiler;Telemetry""
          }}
        }},
        {{
          ""ruleId"": ""CS5001"",
          ""locations"": [
          ],
          ""fullMessage"": ""Program does not contain a static 'Main' method suitable for an entry point"",
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
}}", AnalyzerForErrorLogTest.GetEscapedUriForPath(sourceFile));

            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [Fact]
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

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] {
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
      ""issues"": [
        {{
          ""ruleId"": ""CS0169"",
          ""locations"": [
            {{
              ""analysisTarget"": [
                {{
                  ""uri"": ""{0}"",
                  ""region"": {{
                    ""startLine"": 5,
                    ""startColumn"": 17,
                    ""endLine"": 5,
                    ""endColumn"": 18
                  }}
                }}
              ]
            }}
          ],
          ""fullMessage"": ""The field 'C.x' is never used"",
          ""properties"": {{
            ""severity"": ""Warning"",
            ""warningLevel"": ""3"",
            ""defaultSeverity"": ""Warning"",
            ""title"": ""Field is never used"",
            ""category"": ""Compiler"",
            ""isEnabledByDefault"": ""True"",
            ""isSuppressedInSource"": ""True"",
            ""customTags"": ""Compiler;Telemetry""
          }}
        }},
        {{
          ""ruleId"": ""CS5001"",
          ""locations"": [
          ],
          ""fullMessage"": ""Program does not contain a static 'Main' method suitable for an entry point"",
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
}}", AnalyzerForErrorLogTest.GetEscapedUriForPath(sourceFile));

            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        [Fact]
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

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] {
                "/nologo", "/t:library", $"/out:{outputFilePath}", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}" },
               analyzer: new AnalyzerForErrorLogTest());

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput);
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            var expectedHeader = GetExpectedErrorLogHeader(actualOutput, cmd);
            var expectedIssues = AnalyzerForErrorLogTest.GetExpectedErrorLogIssuesText(cmd.Compilation);
            var expectedText = expectedHeader + expectedIssues;
            Assert.Equal(expectedText, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(outputFilePath);
            CleanupAllGeneratedFiles(errorLogFile);
        }
    }
}