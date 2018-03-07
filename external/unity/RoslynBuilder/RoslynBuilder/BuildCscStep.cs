using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace RoslynBuilder
{
	class BuildCscStep : IBuildStep
	{
		public void Execute()
		{
			BuildCsc("netcoreapp2.0", "win-x64", KnownPaths.CscWindowsBinariesDirectory);
			BuildCsc("netcoreapp2.0", "osx-x64", KnownPaths.CscMacBinariesDirectory);
			BuildCsc("netcoreapp2.0", "linux-x64", KnownPaths.CscLinuxBinariesDirectory);
			BuildCsc("net46", null, KnownPaths.CscNet46Directory);
		}

		private static void BuildCsc(string framework, string runtime, NPath outputDir)
		{
			var description = framework;
			if (!string.IsNullOrEmpty(runtime))
				description += "/" + runtime;

			Console.WriteLine($"Building csc for {description}...");

			var projectDir = KnownPaths.RoslynRoot.Combine("src", "Compilers", "CSharp", "csc");
			var commandLineArgs = new List<string>()
			{
				"publish",
				"--configuration", "Release",
				"--no-restore",
				projectDir.ToString(),
				$"-o", outputDir.ToString(),
				$"--framework", framework.ToString()
			};

			if (!string.IsNullOrEmpty(runtime))
			{
				commandLineArgs.Add("--self-contained");
				commandLineArgs.Add("--runtime");
				commandLineArgs.Add(runtime);
			 }

			commandLineArgs.Add("/p:UseShippingAssemblyVersion=true");
			commandLineArgs.Add("/p:OfficialBuild=true");
			commandLineArgs.Add("/p:SkipApplyOptimizations=true");

			var args = commandLineArgs.Select(a => $"\"{a}\"").Aggregate((a, b) => a + " " + b);
			var dotnetOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.DotNet, args);

			Console.WriteLine(dotnetOutput);
			Console.WriteLine($"Successfully built csc for {description}.");
		}
	}
}
