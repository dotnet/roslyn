// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Writes a completion receipt after the outer project-level ProjectDataBuild target completes.
/// </summary>
public sealed class WriteProjectDataBuildReceiptTask : Microsoft.Build.Utilities.Task
{
	[Required]
	public string ReceiptDirectory { get; set; } = string.Empty;

	[Required]
	public string AttemptId { get; set; } = string.Empty;

	[Required]
	public string ProjectFilePath { get; set; } = string.Empty;

	public override bool Execute()
	{
		try
		{
			string receiptPath = ProjectDataBuildReceipt.Write(this.ReceiptDirectory, this.AttemptId, this.ProjectFilePath);
			this.Log.LogMessage(MessageImportance.Low, "ProjectData: wrote completed receipt for {0}: {1}", this.ProjectFilePath, receiptPath);
			return true;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			this.Log.LogError(
				"ProjectData: failed to write completed receipt for {0} in attempt {1}: {2}",
				this.ProjectFilePath,
				this.AttemptId,
				ex.Message);
			return false;
		}
	}
}
