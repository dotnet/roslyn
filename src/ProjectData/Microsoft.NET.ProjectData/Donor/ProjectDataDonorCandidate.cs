// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

public readonly struct ProjectDataDonorCandidate
{
	public ProjectDataDonorCandidate(string filePath, string workspaceRoot)
	{
		this.FilePath = filePath;
		this.WorkspaceRoot = workspaceRoot;
	}

	public string FilePath { get; }

	public string WorkspaceRoot { get; }
}
