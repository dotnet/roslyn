// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.NET.ProjectData.Tasks;

[SupportedOSPlatform("windows")]
internal static partial class WindowsLocalApplicationData
{
	internal static string? TryGetPath()
	{
		// Query the known folder directly: Environment.GetFolderPath falls back to process
		// LOCALAPPDATA on failure, but the task must use its isolated environment instead.
		Guid folderId = new("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
		int result = GetKnownFolderPath(folderId, 0, 0, out nint path);
		try
		{
			return result == 0 ? Marshal.PtrToStringUni(path) : null;
		}
		finally
		{
			Marshal.FreeCoTaskMem(path);
		}
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHGetKnownFolderPath")]
	private static partial int GetKnownFolderPath(in Guid folderId, uint flags, nint token, out nint path);
}
