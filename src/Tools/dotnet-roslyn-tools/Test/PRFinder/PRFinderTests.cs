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
    [Theory]
    [InlineData(null, "5,4,3")]
    [InlineData("product.cs", "4,3")]
    [InlineData("other.cs", "5")]
    public async Task CopiedTreesExcludeReplacedHistoryWithArbitraryMessages(string? path, string expected)
    {
        using var temp = new TempRoot();
        using var repo = new Repository(Repository.Init(temp.CreateDirectory().Path));
        repo.Network.Remotes.Add("origin", "https://github.com/dotnet/roslyn");
        var sequence = 0;
        var baseline = Commit("Baseline (#1)", Tree("base"));
        var discarded = Commit("Old target update (#2)", Tree("discarded"), baseline);
        var source = Commit("New source update (#3)", Tree("source"), baseline);
        var replacement = Commit("Replace content", source.Tree, discarded, source);
        var config = Commit("Prepare insertion", source.Tree, replacement);
        var promotion = Commit("Promote compiler content (#4)", config.Tree, discarded, config);
        var head = Commit("Post replacement update (#5)", Tree("source", includeOtherFile: true), promotion);
        using var connections = new RemoteConnections(new RoslynToolsSettings { GitHubToken = "test-token" }, NullLogger.Instance, loginToAzureDevOps: false);
        var description = new StringBuilder();

        var result = await Microsoft.RoslynTools.PRFinder.PRFinder.FindPRsAsync(
            baseline.Sha, head.Sha, path, Microsoft.RoslynTools.PRFinder.PRFinder.DefaultFormat,
            [], connections, NullLogger.Instance, repo.Info.WorkingDirectory, description);

        Assert.Equal(0, result);
        Assert.Equal(
            expected.Split(','),
            Regex.Matches(description.ToString(), @"https://github.com/dotnet/roslyn/pull/(\d+)")
                .Select(match => match.Groups[1].Value));

        Tree Tree(string content, bool includeOtherFile = false)
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

        Commit Commit(string message, Tree tree, params Commit[] parents)
        {
            var committer = message.Contains("(#", StringComparison.Ordinal) ? "GitHub" : "Contributor";
            var signature = new Signature(committer, "test@example.invalid", DateTimeOffset.UnixEpoch.AddMinutes(sequence++));
            return repo.ObjectDatabase.CreateCommit(signature, signature, message, tree, parents, prettifyMessage: false);
        }
    }
}
