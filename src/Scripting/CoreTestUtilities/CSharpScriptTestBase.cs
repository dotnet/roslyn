// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.TestUtilities;

public class CSharpScriptTestBase : ScriptTestBase
{
    // default csi.rsp
    private static readonly string[] s_defaultArgs =
    [
        "/r:" + string.Join(";", GetReferences()),
        "/u:System;System.IO;System.Collections.Generic;System.Diagnostics;System.Dynamic;System.Linq;System.Linq.Expressions;System.Text;System.Threading.Tasks",
    ];

    private protected CommandLineRunner CreateRunner(
        string[]? args = null,
        string input = "",
        string? responseFile = null,
        string? workingDirectory = null)
    {
        var io = new TestConsoleIO(input);
        var clientDir = Path.GetDirectoryName(RuntimeUtilities.GetAssemblyLocation(typeof(CSharpScriptTestBase)))!;
        var buildPaths = new BuildPaths(
            clientDir: clientDir,
            workingDir: workingDirectory ?? clientDir,
            sdkDir: null,
            tempDir: Path.GetTempPath());

        var compiler = new CSharpInteractiveCompiler(
            responseFile,
            buildPaths,
            args?.Where(a => a != null).ToArray() ?? s_defaultArgs,
            new NotImplementedAnalyzerLoader(),
            CreateFromFile);
        return new CommandLineRunner(io, compiler, CSharpScriptCompiler.Instance, CSharpObjectFormatter.Instance, CreateFromFile);
    }

    private static IEnumerable<string> GetReferences()
    {
        if (GacFileResolver.IsAvailable)
        {
            // keep in sync with list in csi.rsp
            yield return "System";
            yield return "System.Core";
            yield return "Microsoft.CSharp";
        }
        else
        {
            // keep in sync with list in core csi.rsp
            yield return "System.Collections";
            yield return "System.Collections.Concurrent";
            yield return "System.Console";
            yield return "System.Diagnostics.Debug";
            yield return "System.Diagnostics.Process";
            yield return "System.Diagnostics.StackTrace";
            yield return "System.Globalization";
            yield return "System.IO";
            yield return "System.IO.FileSystem";
            yield return "System.IO.FileSystem.Primitives";
            yield return "System.Reflection";
            yield return "System.Reflection.Extensions";
            yield return "System.Reflection.Primitives";
            yield return "System.Runtime";
            yield return "System.Runtime.Extensions";
            yield return "System.Runtime.InteropServices";
            yield return "System.Text.Encoding";
            yield return "System.Text.Encoding.CodePages";
            yield return "System.Text.Encoding.Extensions";
            yield return "System.Text.RegularExpressions";
            yield return "System.Threading";
            yield return "System.Threading.Tasks";
            yield return "System.Threading.Tasks.Parallel";
            yield return "System.Threading.Thread";
            yield return "System.Linq";
            yield return "System.Linq.Expressions";
            yield return "System.Runtime.Numerics";
            yield return "System.Dynamic.Runtime";
            yield return "Microsoft.CSharp";
        }
    }
}
