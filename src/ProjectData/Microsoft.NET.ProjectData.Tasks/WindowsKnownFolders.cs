// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.NET.ProjectData.Tasks;

[SupportedOSPlatform("windows")]
internal static partial class WindowsKnownFolders
{
	internal static string? TryGetPath(Environment.SpecialFolder folder)
	{
		// Environment.GetFolderPath falls back to process variables on lookup failure;
		// the task must use its isolated environment for those fallbacks instead.
		Guid folderId = folder switch
		{
			Environment.SpecialFolder.LocalApplicationData => new("F1B32785-6FBA-4FCF-9D55-7B8E7F157091"),
			Environment.SpecialFolder.UserProfile => new("5E6C858F-0E22-4760-9AFE-EA3317B67173"),
			Environment.SpecialFolder.ProgramFiles => new("905E63B6-C1BF-494E-B29C-65B732D3D21A"),
			Environment.SpecialFolder.ProgramFilesX86 => new("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E"),
			_ => throw new ArgumentOutOfRangeException(nameof(folder)),
		};
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
