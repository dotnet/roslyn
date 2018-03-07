using System;
using System.Collections.Generic;

namespace RoslynBuilder
{
	class Program
	{
		static int Main(string[] args)
		{
			var buildSteps = new List<IBuildStep>()
			{
				new CleanOldBuildArtifactsStep(),
				new PrepareStep(),
				new InstallCrossGenStep(),
				new BuildCscStep(),
				new CrossGenNativeAssembliesStep(),
				new MakeSureCompilerWorksStep(),
				new PrepareArtifactsForBuildsZipStep(),
			};

			foreach (var step in buildSteps)
			{
				try
				{
					step.Execute();
				}
				catch (Exception e)
				{
					Console.WriteLine($"Building roslyn failed at step {step}:{Environment.NewLine}{e}");
					return 1;
				}
			}

			return 0;
		}
	}
}
