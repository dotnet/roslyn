using System;

namespace RoslynBuilder
{
	class BuildCscStep : IBuildStep
	{
		public void Execute()
		{
			Console.WriteLine("Building csc...");

			var cscCoreProject = KnownPaths.RoslynRoot.Combine("src", "Compilers", "CSharp", "CscCore", "CscCore.csproj");
			var msbuildArgs = $"{cscCoreProject.InQuotes()} /v:m /p:Configuration=\"Release\" /p:Platform=\"AnyCPU\" /p:SolutionName=\"Roslyn\" /p:SolutionDir=\"{KnownPaths.RoslynRoot}\"";
			var msbuildOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.MSBuild, msbuildArgs);

			Console.WriteLine(msbuildOutput);
			Console.WriteLine("Successfully built csc.");
		}
	}
}
