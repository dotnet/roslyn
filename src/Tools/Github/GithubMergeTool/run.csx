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

private static async Task RunAsync()
{
    DotnetBotGithubAuthToken = await GetSecret("dotnet-bot-github-auth-token");

    // Roslyn branches
    await MakeRoslynPr("dev15.0.x", "dev15.1.x");
    await MakeRoslynPr("dev15.1.x", "master");
    await MakeRoslynPr("master", "dev16");
}

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    Log = log;

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    RunAsync().GetAwaiter().GetResult();
}
