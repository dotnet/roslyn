// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;

namespace Build
{
    internal class RoslynSolution : Solution
    {
        public RoslynSolution() : base("Build.ps1")
        {
        }


        public override bool Build(BuildContext context, BuildSettings settings)
        {
            return ExecuteScript(context, settings, "-build");
        }

        private bool ExecuteScript(BuildContext context, BuildSettings settings, string args)
        {
            var configuration = context.Product.Configurations[settings.BuildConfiguration];

            var argsBuilder = new StringBuilder();

            argsBuilder.Append(CultureInfo.InvariantCulture, $"-c {configuration.MSBuildName}");
            argsBuilder.Append(' ');
            argsBuilder.Append(args);

            // The DOTNET_ROOT_X64 environment variable is used by Arcade.
            var toolOptions = new ToolInvocationOptions()
            {
                BlockedEnvironmentVariables = ImmutableArray.Create("MSBuildSDKsPath", "MSBUILD_EXE_PATH"),
                // Retry build when the file is locked by another process.
                Retry = new ToolInvocationRetry(
                    new Regex(".+The process cannot access the file.+because it is being used by another process."), 1 )
            };

            return ToolInvocationHelper.InvokePowershell(
                           context.Console,
                           Path.Combine(context.RepoDirectory, "eng", "build.ps1"),
                           argsBuilder.ToString(),
                           context.RepoDirectory,
                           toolOptions);
        }

        public override bool Pack(BuildContext context, BuildSettings settings)
        {
            return ExecuteScript(context, settings, "-build -pack");
        }

        public override bool Restore(BuildContext context, BuildSettings options)
        {
            return ExecuteScript(context, options, "-restore");
        }

        public override bool Test(BuildContext context, BuildSettings settings)
        {
            var filter = "";

            if (!settings.Properties.ContainsKey("TestAll"))
            {
                filter = "Category!=OuterLoop";
            }

            var binaryLogFilePath = Path.Combine(
               context.RepoDirectory,
               context.Product.LogsDirectory.ToString(),
               $"{this.Name}.test.binlog");

            // We run Metalama's unit tests.
            var project = Path.Combine(context.RepoDirectory, "src", "Metalama", "Metalama.Compiler.UnitTests", "Metalama.Compiler.UnitTests.csproj");
            return DotNetHelper.Run(context, settings, project, "test", $"--no-restore --filter \"{filter}\" -bl:{binaryLogFilePath}");
        }
    }
}
