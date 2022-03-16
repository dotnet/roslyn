// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Utilities;

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
            var configuration = context.Product.Configurations[settings.ResolvedBuildConfiguration];

            var argsBuilder = new StringBuilder();

            argsBuilder.Append( CultureInfo.InvariantCulture, $"-c {configuration.MSBuildName}");
            argsBuilder.Append(' ');
            argsBuilder.Append(args);

            // The DOTNET_ROOT_X64 environment variable is used by Arcade.
            var msbuildEnvironmentVariables = DotNetHelper.GetMsBuildFixingEnvironmentVariables()
                .Where(e => e.Key != "DOTNET_ROOT_X64")
                .ToImmutableDictionary();

            var toolOptions = new ToolInvocationOptions(msbuildEnvironmentVariables);

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

            // We run Metalama's unit tests.
            var project = Path.Combine(context.RepoDirectory, "src", "Metalama", "Metalama.Compiler.UnitTests", "Metalama.Compiler.UnitTests.csproj");
            return DotNetHelper.Run(context, settings, project, "test", $"--filter \"{filter}\"");
        }
    }
}
