// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Private, versioned stderr framing for provisional ProjectDataBuild diagnostics.
/// Frames are diagnostics-only and never provide terminal-state evidence.
/// </summary>
public static class ProjectDataBuildDiagnosticProtocol
{
	public const int Version = 1;
	public const string Prefix = "@@CSDEVKIT_PROJECTDATA_DIAGNOSTIC@@";

	private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	public static string Encode(string attemptId, ProjectDataBuildDiagnosticRecord diagnostic)
	{
		if (string.IsNullOrWhiteSpace(attemptId))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(attemptId));
		}

		if (diagnostic is null)
		{
			throw new ArgumentNullException(nameof(diagnostic));
		}

		return string.Join(
			"|",
			Prefix + Version.ToString(CultureInfo.InvariantCulture),
			EncodeField(attemptId),
			EncodeField(diagnostic.Phase),
			EncodeField(diagnostic.Severity),
			EncodeField(diagnostic.ProjectFilePath),
			EncodeField(diagnostic.FilePath),
			EncodeField(diagnostic.Code),
			diagnostic.Line.ToString(CultureInfo.InvariantCulture),
			diagnostic.Column.ToString(CultureInfo.InvariantCulture),
			EncodeField(diagnostic.Message));
	}

	public static bool TryDecode(string line, string expectedAttemptId, out ProjectDataBuildDiagnosticRecord diagnostic)
	{
		diagnostic = new ProjectDataBuildDiagnosticRecord();
		if (string.IsNullOrEmpty(line) ||
			string.IsNullOrEmpty(expectedAttemptId) ||
			!line.StartsWith(Prefix, StringComparison.Ordinal))
		{
			return false;
		}

		try
		{
			string[] fields = line.Split('|');
			if (fields.Length != 10 ||
				!string.Equals(fields[0], Prefix + Version.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
			{
				return false;
			}

			string attemptId = DecodeField(fields[1]);
			if (!string.Equals(attemptId, expectedAttemptId, StringComparison.Ordinal) ||
				!int.TryParse(fields[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lineNumber) ||
				!int.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int columnNumber))
			{
				return false;
			}

			diagnostic = new ProjectDataBuildDiagnosticRecord
			{
				Phase = DecodeField(fields[2]),
				Severity = DecodeField(fields[3]),
				ProjectFilePath = DecodeField(fields[4]),
				FilePath = DecodeField(fields[5]),
				Code = DecodeField(fields[6]),
				Line = lineNumber,
				Column = columnNumber,
				Message = DecodeField(fields[9]),
			};
			return true;
		}
		catch (Exception ex) when (ex is FormatException or ArgumentException)
		{
			return false;
		}
	}

	private static string EncodeField(string? value)
		=> Convert.ToBase64String(Utf8NoBom.GetBytes(value ?? string.Empty));

	private static string DecodeField(string value)
		=> Utf8NoBom.GetString(Convert.FromBase64String(value));
}
