using System;

namespace RoslynBuilder
{
	class BuildCscStep : IBuildStep
	{
		public void Execute()
		{
			Console.WriteLine("Building csc...");

			var projectDir = KnownPaths.RoslynRoot.Combine("src", "Compilers", "CSharp", "csc");
			var args = $"publish --configuration Release --no-restore {projectDir.InQuotes()} -o {KnownPaths.CscBinariesDirectory.InQuotes()} --self-contained --framework netcoreapp2.0 --runtime win-x64 /p:UseShippingAssemblyVersion=true";
			var dotnetOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.DotNet, args);

			Console.WriteLine(dotnetOutput);
			Console.WriteLine("Successfully built csc.");
		}
	}
}
