using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

string usage = @"usage: BuildNuGets.csx <binaries-dir> <build-version> <output-directory>";

if (Args.Count() != 3)
{
    Console.WriteLine(usage);
    Environment.Exit(1);
}

var binDir = Path.GetFullPath(Args[0]).TrimEnd('\\');
var buildVersion = Args[1].Trim();
var nuspecDirPath = Path.GetFullPath("../../src/NuGet");
var outDir = Path.GetFullPath(Args[2]).TrimEnd('\\');

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
        $@"-OutputDirectory ""{outDir}"" " +
        "-ExcludeEmptyDirectories " +
        $@"-prop licenseUrl=""{licenseUrl}"" " +
        $@"-prop version=""{buildVersion}"" " +
        $"-prop authors={authors} " +
        $@"-prop projectURL=""{projectURL}"" " +
        $@"-prop tags=""{tags}""";
    var nugetExePath = Path.GetFullPath("../../nuget.exe");
    var p = new Process();
    p.StartInfo.FileName = nugetExePath;
    p.StartInfo.Arguments = nugetArgs;
    p.StartInfo.UseShellExecute = false;

    Console.WriteLine($"Running: nuget.exe {nugetArgs}");
    p.Start();
    procs.Add(p);
}

int exit = 0;

foreach (var p in procs)
{
    p.WaitForExit();
    exit += p.ExitCode;
}

Environment.Exit(exit);
