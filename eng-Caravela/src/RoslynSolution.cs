// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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
            this.PackRequiresExplicitBuild = true;
        }


        public override bool Build(BuildContext context, BuildOptions options)
        {
            return ExecuteScript(context, options, "-build");
        }

        private static bool ExecuteScript(BuildContext context, BaseBuildSettings options, string args)
        {
            var argsBuilder = new StringBuilder();
            argsBuilder.Append($"-c {options.BuildConfiguration}");
            argsBuilder.Append(' ');
            argsBuilder.Append(args);

            return ToolInvocationHelper.InvokePowershell(
                           context.Console,
                           Path.Combine(context.RepoDirectory, "eng", "build.ps1"),
                           argsBuilder.ToString(),
                           context.RepoDirectory);
        }

        public override bool Pack(BuildContext context, BuildOptions options)
        {
            return ExecuteScript(context, options, "-pack");
        }

        public override bool Restore(BuildContext context, BaseBuildSettings options)
        {
            return ExecuteScript(context, options, "-restore");
        }

        public override bool Test(BuildContext context, TestOptions options)
        {
            string filter = "";

            if (!options.Properties.ContainsKey("TestAll"))
            {
                filter = "Category!=OuterLoop";
            }

            // We run Caravela's unit tests.
            var project = Path.Combine(context.RepoDirectory, "src", "Caravela", "Caravela.Compiler.UnitTests", "Caravela.Compiler.UnitTests.csproj");
            return DotNetHelper.Run(context, options, project, "test", $"--filter \"{filter}\"");


        }
    }
}
