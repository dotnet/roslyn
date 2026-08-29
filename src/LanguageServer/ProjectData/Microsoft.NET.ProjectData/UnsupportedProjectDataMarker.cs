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

	public static bool TryReadValid(
		string projectFilePath,
		CancellationToken cancellationToken,
		out UnsupportedProjectDataMarkerData marker)
	{
		marker = new UnsupportedProjectDataMarkerData();
		cancellationToken.ThrowIfCancellationRequested();
		try
		{
			string markerPath = GetMarkerFilePath(projectFilePath);
			if (!File.Exists(markerPath))
			{
				return false;
			}

			Dictionary<string, string> values = ReadValues(markerPath, cancellationToken);
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

			string projectFingerprint = ComputeProjectFingerprint(projectFilePath, cancellationToken);
			if (!values.TryGetValue("projectFingerprint", out string? markerProjectFingerprint) ||
				!string.Equals(markerProjectFingerprint, projectFingerprint, StringComparison.Ordinal))
			{
				return false;
			}

			string inputsFingerprint = ComputeAncestorInputsFingerprintCore(projectFilePath, cancellationToken);
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
			cancellationToken.ThrowIfCancellationRequested();
			return true;
		}
		catch (Exception ex) when (IsRecoverableMarkerException(ex))
		{
			cancellationToken.ThrowIfCancellationRequested();
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
			$"projectFingerprint={ComputeProjectFingerprint(projectFilePath, CancellationToken.None)}\n" +
			$"inputsFingerprint={ComputeAncestorInputsFingerprintCore(projectFilePath, CancellationToken.None)}\n" +
			$"reason={reason}\n";

		WriteAllTextAtomically(markerPath, content);
		return markerPath;
	}

	public static void Delete(string projectFilePath)
	{
		try
		{
			string markerPath = GetMarkerFilePath(projectFilePath);
			File.Delete(markerPath);
		}
		catch (Exception ex) when (IsRecoverableMarkerException(ex))
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to delete unsupported-project marker for {0}: {1}",
				projectFilePath,
				ex.Message);
		}
	}

	public static string ComputeProjectFingerprint(string projectFilePath, CancellationToken cancellationToken)
		=> ComputeFileFingerprint(Path.GetFullPath(projectFilePath), cancellationToken);

	public static string ComputeAncestorInputsFingerprint(string projectFilePath, CancellationToken cancellationToken)
		=> ComputeAncestorInputsFingerprintCore(projectFilePath, cancellationToken);

	private static string ComputeAncestorInputsFingerprintCore(string projectFilePath, CancellationToken cancellationToken)
	{
		string? directory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath));
		List<string> inputs = [];
		while (!string.IsNullOrEmpty(directory))
		{
			cancellationToken.ThrowIfCancellationRequested();
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
			cancellationToken.ThrowIfCancellationRequested();
			builder.AppendLine(NormalizePathForFingerprint(input));
			builder.AppendLine(ComputeFileFingerprint(input, cancellationToken));
		}

		cancellationToken.ThrowIfCancellationRequested();
		return ComputeStringFingerprint(builder.ToString());
	}

	private static Dictionary<string, string> ReadValues(string markerPath, CancellationToken cancellationToken)
	{
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		foreach (string line in File.ReadAllLines(markerPath))
		{
			cancellationToken.ThrowIfCancellationRequested();
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

	private static string ComputeFileFingerprint(string path, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using SHA256 sha256 = SHA256.Create();
		byte[] hash = sha256.ComputeHash(stream);
		cancellationToken.ThrowIfCancellationRequested();
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

	private static bool IsRecoverableMarkerException(Exception ex)
		=> ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or NotSupportedException or InvalidOperationException or CryptographicException;

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
