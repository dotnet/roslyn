// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.NET.ProjectData.Tasks;

[UnsupportedOSPlatform("windows")]
internal static partial class UnixUserProfile
{
	internal static unsafe string GetHomeDirectory()
	{
		// Environment.GetFolderPath also reads process HOME. Query the effective user's passwd
		// entry directly so a task without HOME cannot inherit another project's environment.
		uint userId = GetEffectiveUserId();
		for (int bufferSize = 256; ; bufferSize = checked(bufferSize * 2))
		{
			byte[] buffer = new byte[bufferSize];
			fixed (byte* pointer = buffer)
			{
				int error = GetPasswordEntry(userId, out PasswordEntry entry, pointer, bufferSize);
				if (error == 0)
				{
					return Marshal.PtrToStringUTF8(entry.HomeDirectory) ?? "/";
				}

				if (error == -1)
				{
					return "/";
				}

				const int ErrorRange = 0x10047;
				if (ConvertErrorPlatformToPal(error) != ErrorRange)
				{
					throw new IOException("Unable to determine the operating system user profile.", new Win32Exception(error));
				}
			}
		}
	}

	// System.Native supplies the same passwd layout on each Unix platform:
	// https://github.com/dotnet/runtime/blob/v10.0.0/src/native/libs/System.Native/pal_uid.h
	[StructLayout(LayoutKind.Sequential)]
	private readonly struct PasswordEntry
	{
		public readonly nint Name;
		public readonly nint Password;
		public readonly uint UserId;
		public readonly uint GroupId;
		public readonly nint UserInfo;
		public readonly nint HomeDirectory;
		public readonly nint Shell;
	}

	[LibraryImport("System.Native", EntryPoint = "SystemNative_GetEUid")]
	private static partial uint GetEffectiveUserId();

	[LibraryImport("System.Native", EntryPoint = "SystemNative_GetPwUidR")]
	private static unsafe partial int GetPasswordEntry(uint userId, out PasswordEntry entry, byte* buffer, int bufferSize);

	[LibraryImport("System.Native", EntryPoint = "SystemNative_ConvertErrorPlatformToPal")]
	private static partial int ConvertErrorPlatformToPal(int error);
}
