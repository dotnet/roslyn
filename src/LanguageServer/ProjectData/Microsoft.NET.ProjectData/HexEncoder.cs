// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Lowercase-hex byte-to-string encoder used by cache file fingerprinting and hashing.
/// </summary>
/// <remarks>
/// .NET 5+ ships <c>Convert.ToHexString</c> and .NET 9+ ships <c>Convert.ToHexStringLower</c>,
/// but this file is source-linked into the ``Microsoft.NET.ProjectData.Tasks`` MSBuild task
/// assembly which targets ``netstandard2.0``. Neither BCL API exists there, so we keep a
/// manual implementation that compiles cleanly on both target frameworks.
/// </remarks>
internal static class HexEncoder
{
	private const string LowerHexChars = "0123456789abcdef";

	/// <summary>
	/// Converts <paramref name="bytes"/> to a lowercase hexadecimal string. The returned
	/// string has exactly <c>bytes.Length * 2</c> characters.
	/// </summary>
	public static string ToLowerHex(byte[] bytes)
	{
		char[] chars = new char[bytes.Length * 2];
		for (int i = 0; i < bytes.Length; i++)
		{
			chars[i * 2] = LowerHexChars[bytes[i] >> 4];
			chars[i * 2 + 1] = LowerHexChars[bytes[i] & 0xF];
		}
		return new string(chars);
	}
}
