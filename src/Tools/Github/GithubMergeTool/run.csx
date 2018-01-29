// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "GithubMergeTool.dll"

#load "auth.csx"

using System;
using System.Net;
using System.Threading.Tasks;

private static string DotnetBotGithubAuthToken = null;

private static TraceWriter Log = null;

private static async Task MakeGithubPr(string repoOwner, string repoName, string srcBranch, string destBranch)
{    
    var gh = new GithubMergeTool.GithubMergeTool("dotnet-bot@users.noreply.github.com", DotnetBotGithubAuthToken);

    Log.Info($"Merging from {srcBranch} to {destBranch}");

    var result = await gh.CreateMergePr(repoOwner, repoName, srcBranch, destBranch);

    if (result != null)
    {
        if (result.StatusCode == (HttpStatusCode)422)
        {
            Log.Info("PR not created -- all commits are present in base branch");
        }
        else
        {
            Log.Error($"Error creating PR. GH response code: {result.StatusCode}");
        }
    }
    else
    {
        Log.Info("PR created successfully");
    }
}

private static Task MakeRoslynPr(string srcBranch, string destBranch) 
    => MakeGithubPr("dotnet", "roslyn", srcBranch, destBranch);

private static Task MakeLutPr(string srcBranch, string destBranch)
    => MakeGithubPr("dotnet", "testimpact", srcBranch, destBranch);

private static Task MakeProjectSystemPr(string srcBranch, string destBranch)
    => MakeGithubPr("dotnet", "project-system", srcBranch, destBranch);

private static Task MakeCliPr(string srcBranch, string destBranch)
    => MakeGithubPr("dotnet", "cli", srcBranch, destBranch);

private static async Task RunAsync()
{
    DotnetBotGithubAuthToken = await GetSecret("dotnet-bot-github-auth-token");

    // Roslyn branches
    await MakeRoslynPr("dev15.0.x", "dev15.1.x");
    await MakeRoslynPr("dev15.1.x", "dev15.2.x");
    await MakeRoslynPr("dev15.2.x", "dev15.3.x");
    await MakeRoslynPr("dev15.3.x", "master");
    await MakeRoslynPr("master", "dev16");
    await MakeRoslynPr("master", "dev15.5.x");
    await MakeRoslynPr("master", "features/ioperation");
    
    // LUT branches
    await MakeLutPr("dev15.3.x", "master");
    await MakeLutPr("master", "dev16");

    // Project system branches
    await MakeProjectSystemPr("dev15.0.x", "dev15.1.x");
    await MakeProjectSystemPr("dev15.1.x", "dev15.2.x");
    await MakeProjectSystemPr("dev15.2.x", "dev15.3.x");
    await MakeProjectSystemPr("dev15.3.x", "master");

    // Cli branches
    await MakeCliPr("rel/1.1.0", "release/2.0.0");
    await MakeCliPr("release/2.0.0", "master");
}

public static void Run(TimerInfo nightlyRun, TraceWriter log)
{
    Log = log;

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    RunAsync().GetAwaiter().GetResult();
}
