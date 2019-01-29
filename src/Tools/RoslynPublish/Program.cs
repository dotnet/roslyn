using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mono.Options;
using Microsoft.DotNet.VersionTools.Automation;

namespace RoslynPublish
{
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            try
            {
                return Go(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating versions: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static async Task<int> Go(string[] args)
        {
            string gitHubUserName = null;
            string gitHubEmail = null;
            string gitHubToken = null;
            string channel = null;
            string nugetDir = null;
            string owner = "dotnet";

            var optionSet = new OptionSet
            {
                {"c|channel=", "Roslyn channel to update", v => channel = v },
                {"gu|gitHubUserName=", "Github User Name for publish", v => gitHubUserName = v },
                {"gt|gitHubToken=", "Github token for publish", v => gitHubToken = v },
                {"ge|gitHubEmail=", "Github email for publish", v => gitHubEmail = v },
                {"nd|nugetDir=", "Directory containing NuGet packages", v => nugetDir = v },
                {"o|owner=", "Version repo to update (default dotnet)", v => owner = v }
            };

            if (!TryParse(optionSet, args))
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return 1;
            }

            var gitHubAuth = new GitHubAuth(authToken: gitHubToken, user: gitHubUserName, email: gitHubEmail);
            var updater = new GitHubVersionsRepoUpdater(gitHubAuth, owner, "versions");
            var packages = Directory.EnumerateFiles(nugetDir, searchPattern: "*.nupkg");
            var versionsPath = $"build-info/dotnet/roslyn/{channel}";
            await updater.UpdateBuildInfoAsync(
                packages,
                versionsPath,
                updateLatestPackageList: true,
                updateLatestVersion: true,
                updateLastBuildPackageList: true).ConfigureAwait(false);
            return 0;
        }

        private static bool TryParse(OptionSet optionSet, string[] args)
        {
            var parseSucceed = true;
            try
            {
                var rest = optionSet.Parse(args);
                if (rest.Count != 0)
                {
                    Console.WriteLine($"Unexpected values: {rest[0]}");
                    parseSucceed = false;
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                parseSucceed = false;
            }

            return parseSucceed;
        }

    }
}
