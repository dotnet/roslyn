// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public abstract class SarifErrorLoggerTests : CommandLineTestBase
    {
        protected abstract string[] VersionSpecificArguments { get; }
        internal abstract string GetExpectedOutputForNoDiagnostics(CommonCompiler cmd);
        internal abstract string GetExpectedOutputForSimpleCompilerDiagnostics(CommonCompiler cmd, string sourceFile);

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

            string[] arguments = new[] { "/nologo", hello, $"/errorlog:{errorLogFile}" }
                .Concat(VersionSpecificArguments)
                .ToArray();

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

            string[] arguments = new[] { "/nologo", sourceFile, "/preferreduilang:en", $"/errorlog:{errorLogFile}" }
                .Concat(VersionSpecificArguments)
                .ToArray();

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
    }
}
