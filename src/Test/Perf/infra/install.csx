// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load ".\assemblies.csx"
#load ".\ngen.csx"

using System;
using System.Diagnostics;
using System.IO;

InitUtilities();

// If we're being #load'ed by uninstall.csx, set the "uninstall" flag.
var uninstall = Environment.GetCommandLineArgs()[1] == "uninstall.csx";
var message = uninstall ? "Restoring previous copy of" : "Installing";

// TODO: Use actual command line argument parser so we can have help text, etc...
var sourceFolder = Args.Count == 1 ? Args[0] : @"C:\Roslyn\Binaries\Release";

foreach (var processName in new[] { "devenv", "msbuild", "VBCSCompiler"})
{
    var processes = Process.GetProcessesByName(processName);
    foreach (var p in processes)
    {
        Log($"Killing process \"{p.ProcessName}\", PID: {p.Id}");
        p.Kill();
    }
}

Log($"{message} Roslyn binaries to VS folder.");
var devenvFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft Visual Studio 14.0\Common7\IDE");
var destinationFolder = Path.Combine(devenvFolder, "PrivateAssemblies");
foreach (var file in IDEFiles)
{
    var destinationFile = CopyFile(Path.Combine(sourceFolder, file.Key), destinationFolder, uninstall);

    if (file.Value)
    {
        NGen(destinationFile, x86Only: true);
    }
}

var devenv = Path.Combine(devenvFolder, "devenv.exe");
ShellOutVital(devenv, "/clearcache");
ShellOutVital(devenv, "/updateconfiguration");
ShellOutVital(devenv, $"/resetsettingsfull {Path.Combine(sourceFolder, "Default.vssettings")} /command \"File.Exit\"");

Log($"{message} compilers in MSBuild folders.");
destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"MSBuild\14.0\Bin");
var destinationFolder64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"MSBuild\14.0\Bin\amd64");
foreach (var file in MSBuildFiles)
{
    var destinationFile = CopyFile(Path.Combine(sourceFolder, file.Key), destinationFolder, uninstall);

    if (file.Value)
    {
        // It may be surprising that the binary under the 32-bit folder is also ngen'ed
        // for x64, but that's what the Build Tools setup does, so we will mimic it.
        NGen(destinationFile);
    }

    destinationFile = CopyFile(file.Key, destinationFolder64, uninstall);

    if (file.Value)
    {
        // It may be surprising that the binary under the amd64 folder is also ngen'ed
        // for x86, but that's what the Build Tools setup does, so we will mimic it.
        NGen(destinationFile);
    }
}

string CopyFile(string sourceFile, string destinationFolder, bool uninstall)
{
    var fileName = Path.GetFileName(sourceFile);
    var destinationFile = Path.Combine(destinationFolder, fileName);
    var backupFolder = Path.Combine(destinationFolder, "backup");
    var backupFile = Path.Combine(backupFolder, fileName);

    // Elfie won't exist on machines without VS 2015 Update 2 RC (or later).
    // It's okay to skip backing it up if it doesn't exist.
    var shouldBackup = !(File.Exists(destinationFile) || (fileName == "Microsoft.CodeAnalysis.Elfie.dll"));
    if (uninstall)
    {
        if (shouldBackup)
        {
            File.Copy(backupFile, destinationFile, overwrite: true);
        }
    }
    else
    {
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        if (shouldBackup)
        {
            File.Copy(destinationFile, backupFile, overwrite: true);
        }

        File.Copy(sourceFile, destinationFile, overwrite: true);
    }

    return destinationFile;
}
