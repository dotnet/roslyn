// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Resolves the shared user-cache path for MSBuild targets without creating the cache.
/// </summary>
public sealed class GetProjectDataCachePathTask : Microsoft.Build.Utilities.Task
{
	[Required]
	public string ProjectFilePath { get; set; } = string.Empty;

	[Output]
	public string CachePath { get; set; } = string.Empty;

	[Output]
	public string UnsupportedMarkerPath { get; set; } = string.Empty;

	public override bool Execute()
	{
		try
		{
			this.CachePath = UserFolderCachePath.Compute(this.ProjectFilePath);
			this.UnsupportedMarkerPath = UnsupportedProjectDataMarker.GetMarkerFilePath(this.ProjectFilePath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or InvalidOperationException or CryptographicException)
		{
			this.Log.LogWarning(
				"ProjectData: failed to resolve user-cache path for {0}: {1}",
				this.ProjectFilePath,
				ex.Message);
		}

		return true;
	}
}
