// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Expands an SDK ref-pack directory into the list of managed metadata references
/// and CS analyzer references it contributes, by reading the pack's
/// <c>data/FrameworkList.xml</c> manifest. Mirrors what the SDK targets do at build
/// time (<c>ResolveTargetingPackAssets</c>) and what
/// <c>dotnet run file.cs</c> does in the C# file fast path.
/// </summary>
internal static class FrameworkListExpander
{
	private static readonly ConcurrentDictionary<string, Lazy<CachedExpansion>> Cache = new(StringComparers.Paths);

	internal sealed class ExpansionResult
	{
		public required ImmutableArray<string> ManagedAssemblyPaths { get; init; }
		public required ImmutableArray<string> AnalyzerCsPaths { get; init; }

		public static readonly ExpansionResult Empty = new()
		{
			ManagedAssemblyPaths = [],
			AnalyzerCsPaths = [],
		};
	}

	/// <summary>
	/// Reads <c>FrameworkList.xml</c> under <paramref name="packDir"/>/data/ and returns
	/// the absolute paths of <c>Type="Managed"</c> entries (metadata references) and
	/// <c>Type="Analyzer" Language="cs"</c> entries (analyzer references). Results are
	/// cached by <paramref name="packDir"/> for the lifetime of the process.
	/// </summary>
	public static ExpansionResult Expand(string packDir, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Wrap the IO+XML parse in Lazy so racing callers can't trigger duplicate parses.
		CachedExpansion expansion = Cache.GetOrAdd(
			packDir,
			static dir => new Lazy<CachedExpansion>(() => ParseFrameworkList(dir), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

		cancellationToken.ThrowIfCancellationRequested();
		expansion.ReportWarning(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		return expansion.Result;
	}

	private static CachedExpansion ParseFrameworkList(string packDir)
	{
		string manifestPath = Path.Join(packDir, "data", "FrameworkList.xml");
		if (!File.Exists(manifestPath))
		{
			return new(ExpansionResult.Empty);
		}

		XDocument doc;
		try
		{
			doc = XDocument.Load(manifestPath, LoadOptions.None);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
		{
			return new(ExpansionResult.Empty, manifestPath, ex.Message);
		}

		ImmutableArray<string>.Builder managed = ImmutableArray.CreateBuilder<string>();
		ImmutableArray<string>.Builder analyzers = ImmutableArray.CreateBuilder<string>();

		XElement? root = doc.Root;
		if (root is null)
		{
			return new(ExpansionResult.Empty);
		}

		foreach (XElement file in root.Elements("File"))
		{
			string? type = (string?)file.Attribute("Type");
			string? path = (string?)file.Attribute("Path");
			if (string.IsNullOrEmpty(path)) continue;

			string absolutePath = Path.Join(packDir, path.Replace('/', Path.DirectorySeparatorChar));

			if (string.Equals(type, "Managed", StringComparison.OrdinalIgnoreCase))
			{
				managed.Add(absolutePath);
			}
			else if (string.Equals(type, "Analyzer", StringComparison.OrdinalIgnoreCase))
			{
				string? language = (string?)file.Attribute("Language");
				if (string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase))
				{
					analyzers.Add(absolutePath);
				}
			}
		}

		return new(new ExpansionResult
		{
			ManagedAssemblyPaths = managed.ToImmutable(),
			AnalyzerCsPaths = analyzers.ToImmutable(),
		});
	}

	private sealed class CachedExpansion(ExpansionResult result, string? warningPath = null, string? warningMessage = null)
	{
		private readonly object warningGate = new();
		private bool warningReported;

		public ExpansionResult Result { get; } = result;

		public void ReportWarning(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (warningPath is null)
			{
				return;
			}

			lock (this.warningGate)
			{
				if (this.warningReported)
				{
					return;
				}

				cancellationToken.ThrowIfCancellationRequested();
				System.Diagnostics.Trace.TraceWarning(
					"[lscache] Failed to parse framework-list manifest at {0}: {1}",
					warningPath,
					warningMessage);
				this.warningReported = true;
			}
		}
	}
}
