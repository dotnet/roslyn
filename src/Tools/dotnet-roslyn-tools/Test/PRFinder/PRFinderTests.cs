// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.UnitTests.PRFinder;

public class PRFinderTests
{
    /// <summary>
    /// Snap PRs in roslyn are performed via "take source tree" strategy.
    /// This test verifies the list of PRs for such a non-standard commit is correct.
    /// </summary>
    [Theory]
    [InlineData(null, "5,4,3")]
    [InlineData("product.cs", "4,3")]
    [InlineData("other.cs", "5")]
    public async Task SnapExcludesOldTargetPRs(string? path, string expectedPRNumbers)
    {
        using var temp = new TempRoot();
        using var repo = new Repository(Repository.Init(temp.CreateDirectory().Path));
        repo.Network.Remotes.Add("origin", "https://github.com/dotnet/roslyn");
        var sequence = 0;
        var baseline = CreateCommit(message: "Baseline (#1)", tree: CreateTree(content: "base"));
        var oldTarget = CreateCommit(message: "Old target update (#2)", tree: CreateTree(content: "discarded"), parents: [baseline]);
        var newSource = CreateCommit(message: "New source update (#3)", tree: CreateTree(content: "source"), parents: [baseline]);

        // Take the source tree while retaining the old target as the first parent of both merge commits.
        var snap = CreateCommit(message: "Replace content", tree: newSource.Tree, parents: [oldTarget, newSource]);
        var snapBranchTip = CreateCommit(message: "Prepare insertion", tree: snap.Tree, parents: [snap]);
        var snapPR = CreateCommit(message: "Promote compiler content (#4)", tree: snapBranchTip.Tree, parents: [oldTarget, snapBranchTip]);
        var head = CreateCommit(message: "Post replacement update (#5)", tree: CreateTree(content: "source", includeOtherFile: true), parents: [snapPR]);
        using var connections = new RemoteConnections(new RoslynToolsSettings { GitHubToken = "test-token" }, NullLogger.Instance, loginToAzureDevOps: false);
        var description = new StringBuilder();

        var result = await Microsoft.RoslynTools.PRFinder.PRFinder.FindPRsAsync(
            startRef: baseline.Sha,
            endRef: head.Sha,
            path: path,
            format: Microsoft.RoslynTools.PRFinder.PRFinder.DefaultFormat,
            labels: [],
            connections: connections,
            logger: NullLogger.Instance,
            repoPath: repo.Info.WorkingDirectory,
            builder: description);

        Assert.Equal(0, result);
        Assert.Equal(
            expectedPRNumbers.Split(','),
            Regex.Matches(description.ToString(), @"https://github.com/dotnet/roslyn/pull/(\d+)")
                .Select(match => match.Groups[1].Value));

        Tree CreateTree(string content, bool includeOtherFile = false)
        {
            var definition = new TreeDefinition();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var blob = repo.ObjectDatabase.CreateBlob(stream);
            definition.Add("product.cs", blob, Mode.NonExecutableFile);
            if (includeOtherFile)
            {
                definition.Add("other.cs", blob, Mode.NonExecutableFile);
            }

            return repo.ObjectDatabase.CreateTree(definition);
        }

        Commit CreateCommit(string message, Tree tree, params Commit[] parents)
        {
            var committer = message.Contains("(#", StringComparison.Ordinal) ? "GitHub" : "Contributor";
            var signature = new Signature(committer, "test@example.invalid", DateTimeOffset.UnixEpoch.AddMinutes(sequence++));
            return repo.ObjectDatabase.CreateCommit(signature, signature, message, tree, parents, prettifyMessage: false);
        }
    }
}
