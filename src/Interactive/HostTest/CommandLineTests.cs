// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;
using Traits = Microsoft.CodeAnalysis.Test.Utilities.Traits;

namespace Roslyn.Interactive.CommandLine.UnitTests
{
    // TODO: When csi.exe and vbi.exe are made part of our Roslyn open source solution, these tests
    //       should be moved back to the respective CommandLineTests for each compiler.

    public class CommandLineTests : TestBase
    {
        private readonly TempDirectory _baseDirectory;

        public CommandLineTests()
        {
            _baseDirectory = Temp.CreateDirectory();
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void InteractiveCompilerCS()
        {
            TestCsi(new[] { "a + b", "/preferreduilang:en" },
"error CS2001: Source file '" + Path.Combine(_baseDirectory.Path, "a + b") + "' could not be found.",
 1);

            string guid = Guid.NewGuid().ToString("N");

            TestCsi(new[] { guid + ".csx", "/r:d.dll", "/preferreduilang:en" },
"error CS2001: Source file '" + Path.Combine(_baseDirectory.Path, guid) + ".csx' could not be found.",
 1);

            var logo =
@"Microsoft (R) Roslyn C# Interactive Compiler version " + FileVersionInfo.GetVersionInfo(typeof(CSharpCompilation).Assembly.Location).FileVersion + "\r\n" +
@"Copyright (C) Microsoft Corporation. All rights reserved.

";

            TestCsi(new[] { "/define:", "/help", "/r:*", "/preferreduilang:en" }, logo + "                        Roslyn Interactive Compiler Options", 0, (e, a) => a.StartsWith(e, StringComparison.Ordinal));

            TestCsi(new[] { "/r:d.dll", "/preferreduilang:en" }, logo + @"error CS7018: Expected at least one script (.csx file) but none specified
", 1);
            TestCsi(new[] { "/preferreduilang:en" }, logo + @"error CS7018: Expected at least one script (.csx file) but none specified
", 1);
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void BadUsings()
        {
            string script = Temp.CreateFile().WriteAllText("WriteLine(42);").Path;

            TestCsi(new[] { "/noconfig", "/preferreduilang:en", script, "/u:System.Console;Foo.Bar" }, @"
error CS0246: The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)
", 1);
        }

        private void TestCsi(string[] args, string expectedOutput, int expectedExitCode, Func<string, string, bool> equalityComparer = null)
        {
            ConsoleOutput.AssertEqual(
                () =>
                {
                    Console.Write(RunAndGetOutput("csi.exe", ConsolidateArguments(args), expectedExitCode, _baseDirectory.Path));
                },
                expectedOutput,
                "",
                equalityComparer);
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void InteractiveCompilerVB()
        {
            TestVbi(new[] { "a + b", "/preferreduilang:en" }, "vbc : error BC2001: file '" + Path.Combine(_baseDirectory.Path, "a + b") + "' could not be found", 1);

            var guid = System.Guid.NewGuid().ToString("N");
            TestVbi(new[] { guid + ".vbx", "/r:d.dll", "/preferreduilang:en" }, "vbc : error BC2001: file '" + Path.Combine(_baseDirectory.Path, guid) + ".vbx' could not be found", 1);

            var logo =
                "Microsoft (R) Roslyn Visual Basic Interactive Compiler version " + FileVersionInfo.GetVersionInfo(typeof(VisualBasicCompilation).Assembly.Location).FileVersion +
                "\r\nCopyright (C) Microsoft Corporation. All rights reserved.\r\n\r\n";

            TestVbi(new[] { "/define:", "/help", "/r:*", "/preferreduilang:en" }, logo + "                        Roslyn Interactive Compiler Options", 0, (e, a) => a.StartsWith(e, StringComparison.Ordinal));

            TestVbi(new[] { "/r:d.dll", "/preferreduilang:en" }, logo + "vbc : error BC36963: Expected at least one script (.vbx file) but none specified", 1);

            TestVbi(new[] { "/preferreduilang:en" }, logo + "vbc : error BC36963: Expected at least one script (.vbx file) but none specified", 1);
        }

        private void TestVbi(string[] args, string expectedOutput, int expectedExitCode, Func<string, string, bool> equalityComparer = null)
        {
            ConsoleOutput.AssertEqual(
                () =>
                {
                    Console.Write(RunAndGetOutput("vbi.exe", ConsolidateArguments(args), expectedExitCode, _baseDirectory.Path));
                },
                expectedOutput,
                "",
                equalityComparer);
        }
    }
}
