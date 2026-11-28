// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

public sealed class ProjectDataDonorWriteOptions
{
	public ProjectDataDonorWriteOptions()
		: this(ProjectDataEnvironment.Default)
	{
	}

	internal ProjectDataDonorWriteOptions(ProjectDataEnvironment environment)
	{
		this.Enabled = ProjectDataDonorConfiguration.IsEnabledByEnvironmentValue(
			environment.GetEnvironmentVariable(ProjectDataDonorConfiguration.EnabledEnvironmentVariableName));
	}

	public bool Enabled { get; set; }

	public string? IndexPath { get; set; }

	public string? WorkspaceRoot { get; set; }
}
