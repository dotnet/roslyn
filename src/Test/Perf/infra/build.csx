// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

// TODO: Use actual command line argument parser so we can have help text, etc...
var branch = Args.Length == 1 ? Args[0] : "master";
var repoFolder = Args.Lenth == 2 ? Args[1] : @"C:\Roslyn";

Directory.Delete(repoFolder, recursive: true);

// TODO: To be completely *accurate*, we should be building roslyn-internal
// (so that we can incorporate OptProf training data).  However, that would
// require credential management, and that doesn't seem worth the bother.
// For now, we'll just build the roslyn "Open" repo.  I assert that fully
// ngen'ing the binaries may result in slower perf, but the results should be
// consistent for the purposes of detecting regressions.  Long term, we should
// consume the OptProf data via nuget (https://github.com/dotnet/roslyn/issues/5283).
var repo = "https://github.com/dotnet/roslyn";

var result = ShellOut("git", $"clone {repo} master {repoFolder}");
if (!result.Succeeded)
{
    return result.Code;
}

result = ShellOut("msbuild", $"/m /t:rebuild /p:Configuration=Release /p:RealSignBuild=true /p:DelaySignBuild=false {Path.Combine(repoFolder, "Roslyn.sln")}");
if (!result.Succeeded)
{
    return result.Code;
}
