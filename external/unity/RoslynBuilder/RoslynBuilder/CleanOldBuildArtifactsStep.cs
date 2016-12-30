using System;
using System.Linq;

namespace RoslynBuilder
{
	internal class CleanOldBuildArtifactsStep : IBuildStep
	{
		public void Execute()
		{
			foreach (var content in KnownPaths.RoslynRoot.Combine("Artifacts").Contents().Where(d => d.FileName != "RoslynBuilder"))
				content.Delete();

			KnownPaths.RoslynRoot.Combine("Binaries").DeleteContents();
		}
	}
}