using System;

namespace RoslynBuilder
{
	class PrepareArtifactsForBuildsZipStep : IBuildStep
	{
		public void Execute()
		{
			KnownPaths.CscBinariesDirectory.Copy(KnownPaths.BuildsZipDirectory.Combine("Binaries"));
			WriteRevisionFile();
			WriteReadMeFile();

			Console.WriteLine($"Roslyn was build to {KnownPaths.BuildsZipDirectory}.");
		}

		private static void WriteRevisionFile()
		{
			var gitRevisionFile = KnownPaths.BuildsZipDirectory.Combine("revision.txt");
			var gitRevision = Shell.ExecuteAndCaptureOutput("git.exe", "rev-parse HEAD", KnownPaths.RoslynRoot.ToString()).Trim();

			if (gitRevision.Length != 40)
				throw new Exception($"Something is wrong with git revision - its length is not 40 characters: {gitRevision}");

			gitRevisionFile.WriteAllText(gitRevision);
		}

		private void WriteReadMeFile()
		{
			KnownPaths.BuildsZipDirectory.Combine("ReadMe.txt").WriteAllLines(new[]
			{
				"We build roslyn from this repository: https://github.com/Unity-Technologies/Roslyn",
				"We use unity-master branch",
				"It is built on the Gitlab mirror https://gitlab.internal.unity3d.com/vm/roslyn pipeline - don't build it on your local machine"
			});
		}
	}
}