// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../../util/test_util.csx"
using System.IO;
using System;

// Shamelessly copied from MSDN https://msdn.microsoft.com/en-us/library/bb762914.aspx
static void Copy(string sourceDirName, string destDirName, bool copySubDirs)
{
    // Get the subdirectories for the specified directory.
    DirectoryInfo dir = new DirectoryInfo(sourceDirName);

    if (!dir.Exists)
    {
        throw new DirectoryNotFoundException(
            "Source directory does not exist or could not be found: "
            + sourceDirName);
    }

    DirectoryInfo[] dirs = dir.GetDirectories();
    // If the destination directory doesn't exist, create it.
    if (!Directory.Exists(destDirName))
    {
        Directory.CreateDirectory(destDirName);
    }

    // Get the files in the directory and copy them to the new location.
    FileInfo[] files = dir.GetFiles();
    foreach (FileInfo file in files)
    {
        string temppath = Path.Combine(destDirName, file.Name);
        file.CopyTo(temppath, false);
    }

    // If copying subdirectories, copy them and their contents to new location.
    if (copySubDirs)
    {
        foreach (DirectoryInfo subdir in dirs)
        {
            string temppath = Path.Combine(destDirName, subdir.Name);
            Copy(subdir.FullName, temppath, copySubDirs);
        }
    }
}

// The VS Git version provider will prevent CPU idle if the test solution
// being opened in in the Roslyn repo. Until we figure out how to disable it,
// work around by copying the test solution to a temporary directory.
string GetTempPath()
{
   string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
   return tempDirectory;
}

InitUtilities();

string pathToTao = Path.Combine(BinReleaseDirectory(), "tao.exe");
string temp = GetTempPath();
if (Directory.Exists(temp))
    Directory.Delete(temp, recursive: true);
Directory.CreateDirectory(temp);

// Tao needs a "PerfResults" directory
Directory.CreateDirectory(Path.Combine(temp, "test", "PerfResults"));

string testContents = File.ReadAllText(Path.Combine(MyWorkingDirectory(), "CSharpPerfSolutionLoad.xml"));

// Replace $project$ with a reference to the temp directory
string replaced = testContents.Replace("$project$", "./Test.sln");
string testFilePath = Path.Combine(temp, "test.xml");
File.WriteAllText(testFilePath, replaced);

// Copy the test sources over
Copy("TestSolution", temp, copySubDirs:true);

string args = $"-host:vs -roslynonly -rootsuffix:RoslynPerf -perf {testFilePath}";


ShellOutVital(pathToTao, args, Path.Combine(temp, "test"));
//Report(ReportKind.CompileTime, "compile duration (ms)", msToCompile);
