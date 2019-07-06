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
    }
}
