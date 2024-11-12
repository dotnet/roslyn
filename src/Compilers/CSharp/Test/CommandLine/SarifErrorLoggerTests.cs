// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public abstract class SarifErrorLoggerTests : CommandLineTestBase
    {
        protected abstract string ErrorLogQualifier { get; }
        internal abstract string GetExpectedOutputForNoDiagnostics(MockCSharpCompiler cmd);
        internal abstract string GetExpectedOutputForSimpleCompilerDiagnostics(MockCSharpCompiler cmd, string sourceFile);
        internal abstract string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(MockCSharpCompiler cmd, string sourceFile, params string[] suppressionKinds);
        internal abstract string GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(MockCSharpCompiler cmd);
        internal abstract string GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(MockCSharpCompiler cmd, string justification, string suppressionType, params string[] suppressionKinds);
        internal abstract string GetExpectedOutputForAnalyzerDiagnosticsWithWarnAsError(MockCSharpCompiler cmd);

        protected void NoDiagnosticsImpl()
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

            string[] arguments = new[] { "/nologo", hello, $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(arguments);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);

            Assert.Equal("", outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();

            string expectedOutput = GetExpectedOutputForNoDiagnostics(cmd);

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(hello);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void SimpleCompilerDiagnosticsImpl()
        {
            var source = @"
public class C
{
    private int x;
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains("CS0169", actualConsoleOutput);
            Assert.Contains("CS5001", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            var expectedOutput = GetExpectedOutputForSimpleCompilerDiagnostics(cmd, sourceFile);

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void SimpleCompilerDiagnosticsSuppressedImpl()
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

            string[] arguments = new[] { "/nologo", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("CS0169", actualConsoleOutput);
            Assert.Contains("CS5001", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(cmd, sourceFile, suppressionKinds: "inSource");

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsWithAndWithoutLocationImpl()
        {
            var source = @"
public class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var outputDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(outputDir.Path, "ErrorLog.txt");
            var outputFilePath = Path.Combine(outputDir.Path, "test.dll");

            string[] arguments = new[] { "/nologo", "/t:library", $"/out:{outputFilePath}", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput);
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            var expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(cmd);

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(outputFilePath);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsSuppressedWithJustificationImpl()
        {
            var source = @"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""ID1"", Justification = ""Justification1"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""ID2"", Justification = ""Justification2"")]
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(cmd, "Justification1", suppressionType: "SuppressMessageAttribute", suppressionKinds: "inSource");

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsSuppressedWithMissingJustificationImpl()
        {
            var source = @"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""ID1"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""ID2"")]
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(cmd, null, suppressionType: "SuppressMessageAttribute", suppressionKinds: "inSource");

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsSuppressedWithEmptyJustificationImpl()
        {
            var source = @"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""ID1"", Justification = """")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""ID2"", Justification = """")]
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(cmd, "", suppressionType: "SuppressMessageAttribute", suppressionKinds: "inSource");

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsSuppressedWithNullJustificationImpl()
        {
            var source = @"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""ID1"", Justification = null)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""ID2"", Justification = null)]
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            // Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("Category1", actualConsoleOutput);
            Assert.DoesNotContain("Category2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithSuppression(cmd, null, suppressionType: "SuppressMessageAttribute", suppressionKinds: "inSource");

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }

        protected void AnalyzerDiagnosticsWithWarnAsErrorImpl()
        {
            var source = @"
class C
{
}";
            var sourceFile = Temp.CreateFile().WriteAllText(source).Path;
            var errorLogDir = Temp.CreateDirectory();
            var errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt");

            string[] arguments = new[] { "/nologo", "/t:library", "/warnaserror", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}{ErrorLogQualifier}" };

            var cmd = CreateCSharpCompiler(null, WorkingDirectory, arguments,
               analyzers: new[] { new AnalyzerForErrorLogTest() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var exitCode = cmd.Run(outWriter);
            var actualConsoleOutput = outWriter.ToString().Trim();

            Assert.Contains("error ID1", actualConsoleOutput);
            Assert.Contains("error ID2", actualConsoleOutput);
            Assert.NotEqual(0, exitCode);

            var actualOutput = File.ReadAllText(errorLogFile).Trim();
            string expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithWarnAsError(cmd);

            Assert.Equal(expectedOutput, actualOutput);

            CleanupAllGeneratedFiles(sourceFile);
            CleanupAllGeneratedFiles(errorLogFile);
        }
    }
}
