using System;

namespace RoslynBuilder
{
	internal class PrepareStep : IBuildStep
	{
		public void Execute()
		{
			Console.WriteLine("Running Restore.cmd...");
			var output = Shell.ExecuteAndCaptureOutput(KnownPaths.RoslynRoot.Combine("Restore.cmd"), string.Empty, KnownPaths.RoslynRoot);
			Console.WriteLine(output);
			Console.WriteLine("Finished running Restore.cmd.");
		}
	}
}