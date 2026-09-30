// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TestRunner.Helix;

internal sealed class Options : CommonOptions
{
    public string HelixQueueName { get; private set; } = "";
    public string? HelixApiAccessToken { get; set; }
    public string? AccessToken { get; set; }
    public string? ProjectUri { get; set; }
    public string? PipelineDefinitionId { get; set; }
    public string? TargetBranchName { get; set; }

    internal static Options? Parse(string[] args, out bool helpShown)
    {
        var options = new Options();
        var optionSet = options.GetOptionSet();
        optionSet.Add("helixQueueName=", "Name of the Helix queue to run tests on (required)", s => options.HelixQueueName = s);
        optionSet.Add("helixApiAccessToken=", "Access token for internal Helix queues", s => options.HelixApiAccessToken = s);
        optionSet.Add("accessToken=", "Pipeline access token with permissions to view test history", s => options.AccessToken = s);
        optionSet.Add("projectUri=", "ADO project containing the pipeline", s => options.ProjectUri = s);
        optionSet.Add("pipelineDefinitionId=", "Pipeline definition id", s => options.PipelineDefinitionId = s);
        optionSet.Add("targetBranchName=", "Target branch of this pipeline run", s => options.TargetBranchName = s);

        if (!options.ParseCore(args, optionSet, "RunHelix", "Discovers and submits test assemblies to Helix for the external job monitor.", out helpShown))
        {
            return null;
        }

        if (string.IsNullOrEmpty(options.HelixQueueName))
        {
            ConsoleUtil.Error("--helixQueueName is required.");
            return null;
        }

        return options;
    }
}
