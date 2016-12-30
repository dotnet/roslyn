using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynBuilder
{
	class PostprocessBuildStep : IBuildStep
	{
		public void Execute()
		{
			CopyCRTDependencies();
			DeleteUnnecessaryFiles();
			CrossGenAssemblies();
		}

		private static void CopyCRTDependencies()
		{
			var ucrtRedistPath = KnownPaths.Windows10SDK.Combine("Redist", "ucrt", "dlls", "x64");
			ucrtRedistPath.Files("api-ms-win-crt*").Copy(KnownPaths.CscBinariesDirectory);
			ucrtRedistPath.Combine("ucrtbase.dll").Copy(KnownPaths.CscBinariesDirectory);
		}

		private static void DeleteUnnecessaryFiles()
		{
			string[] unnecessaryFiles = new[]
			{
				"System.Private.CoreLib.dll",
				"CommonNetCoreReferences_DoNotUse.dll",
				"CommonNetCoreReferences_DoNotUse.pdb",
				"csc.cmd",
				"csc",
			};

			foreach (var file in unnecessaryFiles)
			{
				var filePath = KnownPaths.CscBinariesDirectory.Combine(file);
				if (filePath.FileExists())
					filePath.Delete();
			}
		}

		private void CrossGenAssemblies()
		{
			var assembliesToCrossGen = new List<NPath>();
			assembliesToCrossGen.Add(KnownPaths.CscBinariesDirectory.Combine("csc.exe"));

			var allAssemblies = KnownPaths.CscBinariesDirectory.Files("*.dll");
			var assembliesExceptCoreLib = allAssemblies.Where(f => !string.Equals(f.FileName, "System.Private.CoreLib.ni.dll", StringComparison.OrdinalIgnoreCase));
			assembliesToCrossGen.AddRange(assembliesExceptCoreLib);

			foreach (var assembly in allAssemblies)
			{
				var crossGenArgs = $"/JITPath {KnownPaths.CscBinariesDirectory.Combine("clrjit.dll").InQuotes()} /platform_assemblies_paths {KnownPaths.CscBinariesDirectory.InQuotes()} {assembly.InQuotes()}";

				try
				{
					var crossGenOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.CrossGen, crossGenArgs, KnownPaths.CscBinariesDirectory);
					Console.WriteLine(crossGenOutput);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					continue;
				}

				var outputPath = assembly.ChangeExtension(".ni.dll");
				if (outputPath.FileExists())
				{
					assembly.Delete();
					outputPath.Move(assembly);
				}
			}
		}
	}
}
