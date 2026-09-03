// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Writes the user-cache marker that records a project as intentionally unsupported by ProjectData.
/// </summary>
public sealed class WriteUnsupportedProjectDataMarkerTask : Microsoft.Build.Utilities.Task
{
	[Required]
	public string ProjectFilePath { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;

	[Output]
	public string MarkerPath { get; set; } = string.Empty;

	public override bool Execute()
	{
		try
		{
			this.MarkerPath = UnsupportedProjectDataMarker.Write(this.ProjectFilePath, this.Reason);
			this.Log.LogMessage(
				MessageImportance.Low,
				"ProjectData: wrote unsupported marker for {0}: {1} ({2})",
				this.ProjectFilePath,
				this.MarkerPath,
				this.Reason);
		}
		catch (Exception ex) when (IsRecoverableMarkerWriteException(ex))
		{
			this.Log.LogWarning(
				"ProjectData: failed to write unsupported marker for {0}: {1}",
				this.ProjectFilePath,
				ex.Message);
		}

		return true;
	}

	private static bool IsRecoverableMarkerWriteException(Exception ex)
		=> ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or InvalidOperationException or CryptographicException;
}
