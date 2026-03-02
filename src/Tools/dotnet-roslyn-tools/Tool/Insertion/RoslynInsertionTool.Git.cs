// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Microsoft.RoslynTools.Insertion;

internal static partial class RoslynInsertionTool
{
    private static string GetNewBranchName() => $"{Options.InsertionBranchName}{Options.VisualStudioBranchName.Split('/').Last()}.{DateTime.Now:yyyyMMddHHmmss}";

    private static async Task<GitPullRequest> CreatePlaceholderVSBranchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gitClient = VisualStudioRepoConnection.GitClient;
        var repository = await gitClient.GetRepositoryAsync(project: Options.VisualStudioRepoProjectName, repositoryId: "VS", cancellationToken: cancellationToken);

        var refs = await gitClient.GetRefsAsync(
            repository.Id,
            filter: $"heads/{Options.VisualStudioBranchName}",
            cancellationToken: cancellationToken);
        var sourceBranch = refs.Single(r => r.Name == $"refs/heads/{Options.VisualStudioBranchName}");

        var branchName = GetNewBranchName();

        _ = await gitClient.CreatePushAsync(new GitPush()
        {
            RefUpdates = [
                new GitRefUpdate()
                {
                    Name = $"refs/heads/{branchName}",
                    OldObjectId = sourceBranch.ObjectId,
                }
            ],
            Commits = [
                new GitCommitRef()
                {
                    Comment = $"PLACEHOLDER INSERTION FOR {Options.InsertionName}",
                    Changes =
                    [
                        new GitChange()
                        {
                            ChangeType = VersionControlChangeType.Delete,
                            Item = new GitItem() { Path = "/Init.ps1" }
                        },
                    ]
                },
            ],
        }, repository.Id, cancellationToken: cancellationToken);

        return await CreateVSPullRequestAsync(
            branchName,
            $"PLACEHOLDER INSERTION FOR {Options.InsertionName}",
            "Not Specified",
            reviewerId: Options.ReviewerGUID,
            cancellationToken);
    }
}
