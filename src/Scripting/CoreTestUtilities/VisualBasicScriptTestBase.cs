// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.TestUtilities;

public class VisualBasicScriptTestBase : ScriptTestBase
{
    private static readonly string[] s_defaultArgs = ["/R:System"];

    private protected CommandLineRunner CreateRunner(
        string[]? args = null,
        string input = "",
        string? responseFile = null,
        string? workingDirectory = null)
    {
        var io = new TestConsoleIO(input);

        var buildPaths = new BuildPaths(
            clientDir: AppContext.BaseDirectory,
            workingDir: workingDirectory ?? AppContext.BaseDirectory,
            sdkDir: RuntimeMetadataReferenceResolver.GetDesktopFrameworkDirectory(),
            tempDir: Path.GetTempPath());

        var compiler = new VisualBasicInteractiveCompiler(
            responseFile,
            buildPaths,
            args ?? s_defaultArgs,
            new NotImplementedAnalyzerLoader(),
            CreateFromFile);

        return new CommandLineRunner(
            io,
            compiler,
            VisualBasicScriptCompiler.Instance,
            VisualBasicObjectFormatter.Instance);
    }
}
