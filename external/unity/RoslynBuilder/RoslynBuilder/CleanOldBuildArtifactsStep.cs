using System.Linq;
using NiceIO;

namespace RoslynBuilder
{
	internal class CleanOldBuildArtifactsStep : IBuildStep
	{
		public void Execute()
		{
			foreach (var content in KnownPaths.RoslynRoot.Combine("Artifacts").Contents().Where(d => d.FileName != "RoslynBuilder"))
				content.Delete(DeleteMode.Soft);

			KnownPaths.RoslynRoot.Combine("Binaries").DeleteContents(DeleteMode.Soft);
		}
	}
}