using System;

namespace RoslynBuilder
{
	class CrossGenNativeAssembliesStep : IBuildStep
	{
		public void Execute()
		{
			var allAssemblies = KnownPaths.CscWindowsBinariesDirectory.Files("*.dll");
			foreach (var assembly in allAssemblies)
			{
				var crossGenArgs = $"/nologo /JITPath {KnownPaths.CscWindowsBinariesDirectory.Combine("clrjit.dll").InQuotes()} /platform_assemblies_paths {KnownPaths.CscWindowsBinariesDirectory.InQuotes()} {assembly.InQuotes()}";

				try
				{
					var crossGenOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.CrossGen, crossGenArgs, KnownPaths.CscWindowsBinariesDirectory);
					Console.WriteLine(crossGenOutput);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					continue;
				}

				var outputPath = assembly.ChangeExtension(".ni.dll");
				if (!outputPath.Exists()) // If assembly had an entry point, crossgen will turn it into an .exe
					outputPath = assembly.ChangeExtension(".ni.exe");

				if (outputPath.FileExists())
				{
					assembly.Delete();
					outputPath.Move(assembly);
				}
			}
		}
	}
}
