// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Reads and writes completion-only evidence for one ProjectDataBuild attempt.
/// </summary>
public static class ProjectDataBuildReceipt
{
	public const int SchemaVersion = 2;
	public const string AggregateCompletionFileName = "aggregate.completed";

	private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	public static string GetReceiptFilePath(string receiptDirectory, string projectFilePath)
	{
		ThrowIfNullOrWhiteSpace(receiptDirectory, nameof(receiptDirectory));
		ThrowIfNullOrWhiteSpace(projectFilePath, nameof(projectFilePath));

		string normalizedProjectPath = NormalizeProjectPath(projectFilePath);
		using SHA256 sha256 = SHA256.Create();
		string fileName = HexEncoder.ToLowerHex(sha256.ComputeHash(Utf8NoBom.GetBytes(normalizedProjectPath))) + ".completed";
		return Path.Combine(receiptDirectory, fileName);
	}

	public static string GetAggregateCompletionFilePath(string receiptDirectory)
	{
		ThrowIfNullOrWhiteSpace(receiptDirectory, nameof(receiptDirectory));
		return Path.Combine(receiptDirectory, AggregateCompletionFileName);
	}

	public static string Write(string receiptDirectory, string attemptId, string projectFilePath)
	{
		ValidateInputs(receiptDirectory, attemptId);
		ThrowIfNullOrWhiteSpace(projectFilePath, nameof(projectFilePath));

		Directory.CreateDirectory(receiptDirectory);
		string fullProjectPath = Path.GetFullPath(projectFilePath);
		string receiptPath = GetReceiptFilePath(receiptDirectory, fullProjectPath);
		string content =
			$"version={SchemaVersion}\n" +
			$"attempt={attemptId}\n" +
			$"project={fullProjectPath}\n";
		WriteAllTextAtomically(receiptPath, content);
		return receiptPath;
	}

	public static bool TryRead(
		string receiptDirectory,
		string attemptId,
		string projectFilePath,
		out ProjectDataBuildReceiptData receipt)
	{
		receipt = new ProjectDataBuildReceiptData();
		try
		{
			ValidateInputs(receiptDirectory, attemptId);
			ThrowIfNullOrWhiteSpace(projectFilePath, nameof(projectFilePath));

			string receiptPath = GetReceiptFilePath(receiptDirectory, projectFilePath);
			if (!File.Exists(receiptPath))
			{
				return false;
			}

			Dictionary<string, string> values = ReadValues(receiptPath);
			if (values.Count != 3 ||
				!TryGetInt(values, "version", out int version) ||
				version != SchemaVersion ||
				!values.TryGetValue("attempt", out string? receiptAttemptId) ||
				!string.Equals(receiptAttemptId, attemptId, StringComparison.Ordinal) ||
				!values.TryGetValue("project", out string? receiptProjectPath) ||
				!PathsEqual(receiptProjectPath, projectFilePath))
			{
				return false;
			}

			receipt = new ProjectDataBuildReceiptData
			{
				AttemptId = receiptAttemptId,
				ProjectFilePath = receiptProjectPath,
				ReceiptFilePath = receiptPath,
			};
			return true;
		}
		catch (Exception ex) when (IsRecoverableEvidenceException(ex))
		{
			System.Diagnostics.Trace.TraceWarning(
				"[ProjectDataBuildReceipt] Failed to read completion receipt for {0}: {1}",
				projectFilePath,
				ex.Message);
			return false;
		}
	}

	public static void WriteAggregateCompletion(string receiptDirectory, string attemptId)
	{
		ValidateInputs(receiptDirectory, attemptId);
		Directory.CreateDirectory(receiptDirectory);
		string content =
			$"version={SchemaVersion}\n" +
			$"attempt={attemptId}\n";
		WriteAllTextAtomically(GetAggregateCompletionFilePath(receiptDirectory), content);
	}

	public static bool TryReadAggregateCompletion(string receiptDirectory, string attemptId)
	{
		try
		{
			ValidateInputs(receiptDirectory, attemptId);
			string receiptPath = GetAggregateCompletionFilePath(receiptDirectory);
			if (!File.Exists(receiptPath))
			{
				return false;
			}

			Dictionary<string, string> values = ReadValues(receiptPath);
			return values.Count == 2 &&
				TryGetInt(values, "version", out int version) &&
				version == SchemaVersion &&
				values.TryGetValue("attempt", out string? receiptAttemptId) &&
				string.Equals(receiptAttemptId, attemptId, StringComparison.Ordinal);
		}
		catch (Exception ex) when (IsRecoverableEvidenceException(ex))
		{
			System.Diagnostics.Trace.TraceWarning(
				"[ProjectDataBuildReceipt] Failed to read aggregate completion for attempt {0}: {1}",
				attemptId,
				ex.Message);
			return false;
		}
	}

	private static void ValidateInputs(string receiptDirectory, string attemptId)
	{
		ThrowIfNullOrWhiteSpace(receiptDirectory, nameof(receiptDirectory));
		ThrowIfNullOrWhiteSpace(attemptId, nameof(attemptId));
	}

	private static Dictionary<string, string> ReadValues(string path)
	{
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		foreach (string line in File.ReadAllLines(path))
		{
			int separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			{
				throw new FormatException($"Receipt '{path}' contains a malformed line.");
			}

			string key = line.Substring(0, separatorIndex);
			if (values.ContainsKey(key))
			{
				throw new FormatException($"Receipt '{path}' contains a duplicate '{key}' field.");
			}

			values.Add(key, line.Substring(separatorIndex + 1));
		}

		return values;
	}

	private static bool TryGetInt(Dictionary<string, string> values, string key, out int value)
	{
		value = 0;
		return values.TryGetValue(key, out string? text) && int.TryParse(text, out value);
	}

	private static string NormalizeProjectPath(string projectFilePath)
	{
		string fullPath = Path.GetFullPath(projectFilePath);
		return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
			? fullPath
			: fullPath.ToUpperInvariant();
	}

	private static bool PathsEqual(string left, string right)
		=> string.Equals(
			Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
			Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
			RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

	internal static void WriteAllTextAtomically(string path, string content)
	{
		string? directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(tempPath, content, Utf8NoBom);
			IOException? lastWriteError = null;
			for (int attempt = 0; attempt < 8; attempt++)
			{
				try
				{
					if (File.Exists(path))
					{
						if (string.Equals(File.ReadAllText(path, Utf8NoBom), content, StringComparison.Ordinal))
						{
							return;
						}

						File.Replace(tempPath, path, destinationBackupFileName: null);
					}
					else
					{
						File.Move(tempPath, path);
					}

					return;
				}
				catch (IOException ex)
				{
					lastWriteError = ex;
					Thread.Sleep(1);
				}
			}

			throw new IOException($"Failed to atomically write ProjectData build receipt '{path}' after concurrent write retries.", lastWriteError);
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

	private static bool IsRecoverableEvidenceException(Exception ex)
		=> ex is IOException
			or UnauthorizedAccessException
			or FormatException
			or ArgumentException
			or NotSupportedException
			or InvalidOperationException
			or CryptographicException;

	private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
		}
	}
}

public sealed class ProjectDataBuildReceiptData
{
	public string AttemptId { get; set; } = string.Empty;
	public string ProjectFilePath { get; set; } = string.Empty;
	public string ReceiptFilePath { get; set; } = string.Empty;
}
