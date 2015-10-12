using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

string usage = @"usage: BuildNuGets.csx <binaries-dir> <build-version>";

if (Args.Length != 2)
{
    Console.WriteLine(usage);
    Environment.Exit(1);
}

var binDir = Path.GetFullPath(Args[0]);
var buildVersion = Args[1].Trim();
var nuspecDirPath = Path.GetFullPath("../../src/NuGet");

var licenseUrl = @"http://go.microsoft.com/fwlink/?LinkId=529443";
var authors = @"Microsoft";
var projectURL = @"http://msdn.com/roslyn";
var tags = @"Roslyn CodeAnalysis Compiler CSharp VB VisualBasic Parser Scanner Lexer Emit CodeGeneration Metadata IL Compilation Scripting Syntax Semantics";

var files = Directory.GetFiles(nuspecDirPath, "*.nuspec");
var procs = new List<Process>(files.Length);

foreach (var file in files)
{
    var nugetArgs = $@"pack {file} " +
        $@"-BasePath ""{binDir}"" " +
        "-ExcludeEmptyDirectories " +
        $@"-prop licenseUrl=""{licenseUrl}"" " +
        $@"-prop version=""{buildVersion}"" " +
        $"-prop authors={authors} " +
        $@"-prop projectURL=""{projectURL}"" " +
        $@"-prop tags=""{tags}""";
    Console.WriteLine(nugetArgs);
    var p = Process.Start(Path.GetFullPath("../../nuget.exe"), nugetArgs);
}

foreach (var p in procs)
{
    p.WaitForExit();
}
