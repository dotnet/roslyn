// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;

namespace Microsoft.NET.ProjectData;

public static partial class ProjectDataDonorIndex
{
	private static readonly int ProcessId = GetCurrentProcessId();

	private static ProjectDataDonorIndexFile ReadIndex(string indexPath)
	{
		if (!File.Exists(indexPath))
		{
			return new ProjectDataDonorIndexFile { Version = CurrentVersion };
		}

		using FileStream stream = new(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using JsonDocument document = JsonDocument.Parse(stream);
		JsonElement root = document.RootElement;
		if (root.ValueKind != JsonValueKind.Object)
		{
			throw new JsonException($"Donor index '{indexPath}' must contain a JSON object.");
		}

		int version = GetInt32(root, "version") ?? 0;
		ProjectDataDonorIndexFile index = new() { Version = version };
		if (version != CurrentVersion)
		{
			return index;
		}

		if (root.TryGetProperty("entries", out JsonElement entries))
		{
			RequireValueKind(entries, JsonValueKind.Array, "entries");
			foreach (JsonElement item in entries.EnumerateArray())
			{
				index.Entries.Add(ReadEntry(item));
			}
		}

		return index;
	}

	private static ProjectDataDonorIndexFile ReadIndexForWrite(string indexPath, out string? recoveryMessage)
	{
		recoveryMessage = null;
		try
		{
			ProjectDataDonorIndexFile index = ReadIndex(indexPath);
			if (index.Version == CurrentVersion)
			{
				return index;
			}

			if (index.Version > CurrentVersion)
			{
				throw new InvalidOperationException(
					$"Cannot update unsupported donor index version {index.Version}; expected {CurrentVersion}.");
			}
		}
		catch (JsonException)
		{
		}

		string quarantinePath = indexPath + ".corrupt-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
		File.Move(indexPath, quarantinePath);
		recoveryMessage = $"Recovered corrupt donor index '{indexPath}'; the original was preserved at '{quarantinePath}'.";
		return new ProjectDataDonorIndexFile { Version = CurrentVersion };
	}

	private static ProjectDataDonorIndexEntry ReadEntry(JsonElement element)
	{
		RequireValueKind(element, JsonValueKind.Object, "index entry");

		string? path = GetString(element, "path");
		if (path is not { Length: > 0 } entryPath)
		{
			throw new JsonException("Donor index entries must contain a non-empty string path.");
		}

		string normalizedPath;
		try
		{
			normalizedPath = Path.GetFullPath(entryPath);
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			throw new JsonException("Donor index entries must contain a valid path.", ex);
		}

		return new ProjectDataDonorIndexEntry
		{
			Path = normalizedPath,
			NewestMtimeMs = GetInt64(element, "newestMtimeMs"),
			UpdatedUtc = GetDateTimeOffset(element, "updatedUtc"),
		};
	}

	private static void WriteIndex(string indexPath, ProjectDataDonorIndexFile index)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
		string tempPath = indexPath + "." + ProcessId + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using (FileStream stream = File.Create(tempPath))
			using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
			{
				writer.WriteStartObject();
				writer.WriteNumber("version", CurrentVersion);
				writer.WritePropertyName("entries");
				writer.WriteStartArray();
				foreach (ProjectDataDonorIndexEntry entry in index.Entries)
				{
					WriteEntry(writer, entry);
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			ReplaceOrMove(tempPath, indexPath);
		}
		finally
		{
			try
			{
				File.Delete(tempPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Trace.TraceWarning("Failed to delete temporary donor index file {0}: {1}", tempPath, ex.Message);
			}
		}
	}

	private static int GetCurrentProcessId()
	{
		using Process process = Process.GetCurrentProcess();
		return process.Id;
	}

	private static void WriteEntry(Utf8JsonWriter writer, ProjectDataDonorIndexEntry entry)
	{
		writer.WriteStartObject();
		writer.WriteString("path", entry.Path);
		WriteOptionalNumber(writer, "newestMtimeMs", entry.NewestMtimeMs);
		writer.WriteString("updatedUtc", (entry.UpdatedUtc ?? DateTimeOffset.UtcNow).UtcDateTime);
		writer.WriteEndObject();
	}

	private static void WriteOptionalNumber(Utf8JsonWriter writer, string propertyName, long? value)
	{
		if (value.HasValue)
		{
			writer.WriteNumber(propertyName, value.Value);
		}
	}

	private static void ReplaceOrMove(string tempPath, string indexPath)
	{
		if (File.Exists(indexPath))
		{
			File.Replace(tempPath, indexPath, destinationBackupFileName: null);
			return;
		}

		File.Move(tempPath, indexPath);
	}

	private static string? GetString(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out JsonElement property) || property.ValueKind == JsonValueKind.Null)
		{
			return null;
		}

		RequireValueKind(property, JsonValueKind.String, name);
		return property.GetString();
	}

	private static int? GetInt32(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out JsonElement property))
		{
			return null;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int value))
		{
			return value;
		}

		throw new JsonException($"Donor index property '{name}' must be a 32-bit integer.");
	}

	private static long? GetInt64(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out JsonElement property))
		{
			return null;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out long value))
		{
			return value;
		}

		throw new JsonException($"Donor index property '{name}' must be a 64-bit integer.");
	}

	private static DateTimeOffset? GetDateTimeOffset(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out JsonElement property))
		{
			return null;
		}

		if (property.ValueKind == JsonValueKind.String && property.TryGetDateTimeOffset(out DateTimeOffset value))
		{
			return value;
		}

		throw new JsonException($"Donor index property '{name}' must be a date-time string.");
	}

	private static void RequireValueKind(JsonElement element, JsonValueKind expectedKind, string propertyName)
	{
		if (element.ValueKind != expectedKind)
		{
			throw new JsonException($"Donor index property '{propertyName}' must be {expectedKind}.");
		}
	}

	private sealed class ProjectDataDonorIndexFile
	{
		public int Version { get; set; }
		public List<ProjectDataDonorIndexEntry> Entries { get; } = [];

		public void UpsertEntry(ProjectDataDonorIndexEntry entry)
		{
			ProjectDataDonorIndexEntry? existingEntry = this.Entries.FirstOrDefault(existing => PathsEqual(existing.Path, entry.Path));
			if (existingEntry?.NewestMtimeMs > entry.NewestMtimeMs)
			{
				entry.NewestMtimeMs = existingEntry.NewestMtimeMs;
			}

			this.Entries.RemoveAll(existing => PathsEqual(existing.Path, entry.Path));
			this.Entries.Insert(0, entry);
		}
	}
}
