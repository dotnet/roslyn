// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Keeps the source-linked cache helpers independent of MSBuild while allowing tasks to
/// supply their isolated environment instead of the process environment.
/// </summary>
internal sealed class ProjectDataEnvironment
{
	internal static ProjectDataEnvironment Default { get; } = new(
		Environment.GetEnvironmentVariable,
		Environment.GetFolderPath,
		Path.GetFullPath);

	internal ProjectDataEnvironment(
		Func<string, string?> getEnvironmentVariable,
		Func<Environment.SpecialFolder, string> getFolderPath,
		Func<string, string> getFullPath)
	{
		this.GetEnvironmentVariable = getEnvironmentVariable;
		this.GetFolderPath = getFolderPath;
		this.GetFullPath = getFullPath;
	}

	internal Func<string, string?> GetEnvironmentVariable { get; }
	internal Func<Environment.SpecialFolder, string> GetFolderPath { get; }
	internal Func<string, string> GetFullPath { get; }
}
