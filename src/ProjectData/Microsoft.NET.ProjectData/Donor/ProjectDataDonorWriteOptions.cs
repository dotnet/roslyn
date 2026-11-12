// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

public sealed class ProjectDataDonorWriteOptions
{
	public bool Enabled { get; set; } = ProjectDataDonorConfiguration.IsEnabledByEnvironmentValue(
		Environment.GetEnvironmentVariable(ProjectDataDonorConfiguration.EnabledEnvironmentVariableName));

	public string? IndexPath { get; set; }

	public string? WorkspaceRoot { get; set; }
}
