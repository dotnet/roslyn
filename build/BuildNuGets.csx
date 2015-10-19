using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

string usage = @"usage: BuildNuGets.csx <binaries-dir> <build-version> <output-directory>";

if (Args.Count() != 3)
{
    Console.WriteLine(usage);
    Environment.Exit(1);
}

string ScriptRoot([CallerFilePath]string path = "") => Path.GetDirectoryName(path);

var slnRoot = Path.GetFullPath(Path.Combine(ScriptRoot(), "../"));

// Strip trailing '\' characters because if the path is later passed on the
// command line when surrounded by quotes (in case the path has spaces) some
// utilities will consider the '\"' as an escape sequence for the end quote

var binDir = Path.GetFullPath(Args[0]).TrimEnd('\\');
var buildVersion = Args[1].Trim();
var nuspecDirPath = Path.GetFullPath(Path.Combine(slnRoot, "src/NuGet"));
var outDir = Path.GetFullPath(Args[2]).TrimEnd('\\');

var licenseUrl = @"http://go.microsoft.com/fwlink/?LinkId=529443";
var authors = @"Microsoft";
var projectURL = @"http://msdn.com/roslyn";
var tags = @"Roslyn CodeAnalysis Compiler CSharp VB VisualBasic Parser Scanner Lexer Emit CodeGeneration Metadata IL Compilation Scripting Syntax Semantics";

var files = Directory.GetFiles(nuspecDirPath, "*.nuspec");

int exit = 0;

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
    var nugetExePath = Path.GetFullPath(Path.Combine(slnRoot, "nuget.exe"));
    var p = new Process();
    p.StartInfo.FileName = nugetExePath;
    p.StartInfo.Arguments = nugetArgs;
    p.StartInfo.UseShellExecute = false;

    Console.WriteLine($"Running: nuget.exe {nugetArgs}");
    p.Start();
    p.WaitForExit();
    exit = p.ExitCode;

    if (exit != 0)
    {
        break;
    }
}

Environment.Exit(exit);
