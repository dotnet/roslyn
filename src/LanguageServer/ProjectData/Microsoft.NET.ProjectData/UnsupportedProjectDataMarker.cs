// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Reads and writes the user-cache sidecar that records projects known not to produce ProjectData.
/// </summary>
public static class UnsupportedProjectDataMarker
{
	public const int SchemaVersion = 1;
	public const int RulesVersion = 2;

	private const string MarkerExtension = ".unsupported";
	private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static readonly string[] AncestorInputFileNames =
	[
		"Directory.Build.props",
		"Directory.Build.targets",
		"Directory.Packages.props",
		"global.json",
	];

	public static string GetMarkerFilePath(string projectFilePath) => UserFolderCachePath.Compute(projectFilePath) + MarkerExtension;

	public static bool TryReadValid(string projectFilePath, out UnsupportedProjectDataMarkerData marker)
	{
		marker = new UnsupportedProjectDataMarkerData();
		string markerPath = GetMarkerFilePath(projectFilePath);
		if (!File.Exists(markerPath))
		{
			return false;
		}

		try
		{
			Dictionary<string, string> values = ReadValues(markerPath);
			if (!TryGetInt(values, "version", out int version) || version != SchemaVersion)
			{
				return false;
			}

			if (!TryGetInt(values, "rulesVersion", out int rulesVersion) || rulesVersion != RulesVersion)
			{
				return false;
			}

			if (!values.TryGetValue("project", out string? markerProjectPath) ||
				!PathsEqual(markerProjectPath, Path.GetFullPath(projectFilePath)))
			{
				return false;
			}

			string projectFingerprint = ComputeProjectFingerprint(projectFilePath);
			if (!values.TryGetValue("projectFingerprint", out string? markerProjectFingerprint) ||
				!string.Equals(markerProjectFingerprint, projectFingerprint, StringComparison.Ordinal))
			{
				return false;
			}

			string inputsFingerprint = ComputeAncestorInputsFingerprint(projectFilePath);
			if (!values.TryGetValue("inputsFingerprint", out string? markerInputsFingerprint) ||
				!string.Equals(markerInputsFingerprint, inputsFingerprint, StringComparison.Ordinal))
			{
				return false;
			}

			marker = new UnsupportedProjectDataMarkerData
			{
				ProjectFilePath = markerProjectPath,
				Reason = values.TryGetValue("reason", out string? reason) ? reason : string.Empty,
				MarkerFilePath = markerPath,
				ProjectFingerprint = projectFingerprint,
				InputsFingerprint = inputsFingerprint,
			};
			return true;
		}
		catch (Exception ex) when (IsRecoverableMarkerReadException(ex))
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to read unsupported-project marker for {0}: {1}",
				projectFilePath,
				ex.Message);
			return false;
		}
	}

	public static string Write(string projectFilePath, string reason)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			reason = "Unsupported";
		}

		string markerPath = GetMarkerFilePath(projectFilePath);
		string? directory = Path.GetDirectoryName(markerPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string content =
			$"version={SchemaVersion}\n" +
			$"rulesVersion={RulesVersion}\n" +
			$"project={Path.GetFullPath(projectFilePath)}\n" +
			$"projectFingerprint={ComputeProjectFingerprint(projectFilePath)}\n" +
			$"inputsFingerprint={ComputeAncestorInputsFingerprint(projectFilePath)}\n" +
			$"reason={reason}\n";

		WriteAllTextAtomically(markerPath, content);
		return markerPath;
	}

	public static void Delete(string projectFilePath)
	{
		string markerPath = GetMarkerFilePath(projectFilePath);
		try
		{
			File.Delete(markerPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to delete unsupported-project marker for {0}: {1}",
				projectFilePath,
				ex.Message);
		}
	}

	public static string ComputeProjectFingerprint(string projectFilePath)
		=> ComputeFileFingerprint(Path.GetFullPath(projectFilePath));

	public static string ComputeAncestorInputsFingerprint(string projectFilePath)
	{
		string? directory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath));
		List<string> inputs = [];
		while (!string.IsNullOrEmpty(directory))
		{
			foreach (string fileName in AncestorInputFileNames)
			{
				string candidate = Path.Combine(directory, fileName);
				if (File.Exists(candidate))
				{
					inputs.Add(Path.GetFullPath(candidate));
				}
			}

			DirectoryInfo? parent = Directory.GetParent(directory);
			if (parent is null || string.Equals(parent.FullName, directory, StringComparison.Ordinal))
			{
				break;
			}

			directory = parent.FullName;
		}

		inputs.Sort(PathComparer);
		StringBuilder builder = new();
		foreach (string input in inputs)
		{
			builder.AppendLine(NormalizePathForFingerprint(input));
			builder.AppendLine(ComputeFileFingerprint(input));
		}

		return ComputeStringFingerprint(builder.ToString());
	}

	private static Dictionary<string, string> ReadValues(string markerPath)
	{
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		foreach (string line in File.ReadAllLines(markerPath))
		{
			int separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			{
				continue;
			}

			values[line.Substring(0, separatorIndex)] = line.Substring(separatorIndex + 1);
		}

		return values;
	}

	private static bool TryGetInt(Dictionary<string, string> values, string key, out int value)
	{
		value = 0;
		return values.TryGetValue(key, out string? text) && int.TryParse(text, out value);
	}

	private static string ComputeFileFingerprint(string path)
	{
		using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using SHA256 sha256 = SHA256.Create();
		byte[] hash = sha256.ComputeHash(stream);
		return HexEncoder.ToLowerHex(hash);
	}

	private static string ComputeStringFingerprint(string value)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hash = sha256.ComputeHash(Utf8NoBom.GetBytes(value));
		return HexEncoder.ToLowerHex(hash);
	}

	private static void WriteAllTextAtomically(string path, string content)
	{
		string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(tempPath, content, Utf8NoBom);
			if (File.Exists(path))
			{
				File.Replace(tempPath, path, destinationBackupFileName: null);
			}
			else
			{
				File.Move(tempPath, path);
			}
		}
		finally
		{
			try
			{
				File.Delete(tempPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
	}

	private static bool PathsEqual(string left, string right)
		=> string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), IsCaseSensitiveFileSystem() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

	private static bool IsRecoverableMarkerReadException(Exception ex)
		=> ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or NotSupportedException or CryptographicException;

	private static string NormalizePathForFingerprint(string path)
	{
		string normalized = Path.GetFullPath(path);
		return IsCaseSensitiveFileSystem()
			? normalized
			: normalized.ToLowerInvariant();
	}

	private static int PathComparer(string left, string right)
		=> string.Compare(left, right, IsCaseSensitiveFileSystem() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

	private static bool IsCaseSensitiveFileSystem() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}

public sealed class UnsupportedProjectDataMarkerData
{
	public string ProjectFilePath { get; set; } = string.Empty;
	public string Reason { get; set; } = string.Empty;
	public string MarkerFilePath { get; set; } = string.Empty;
	public string ProjectFingerprint { get; set; } = string.Empty;
	public string InputsFingerprint { get; set; } = string.Empty;
}
