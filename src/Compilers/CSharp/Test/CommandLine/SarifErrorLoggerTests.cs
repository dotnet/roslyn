// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
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
        internal abstract string GetExpectedOutputForNoDiagnostics(CommonCompiler cmd);
        internal abstract string GetExpectedOutputForSimpleCompilerDiagnostics(CommonCompiler cmd, string sourceFile);
        internal abstract string GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(CommonCompiler cmd, string sourceFile);
        internal abstract string GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(MockCSharpCompiler cmd);

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
            string expectedOutput = GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(cmd, sourceFile);

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
               analyzers: ImmutableArray.Create<DiagnosticAnalyzer>(new AnalyzerForErrorLogTest()));

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
    }
}
