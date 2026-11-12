// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Reads the selected .NET SDK's bundled-version tables so cache files can
/// represent SDK-known NuGet packs by logical package name.
/// </summary>
internal static class SdkKnownPackResolver
{
	private static readonly ConcurrentDictionary<string, Lazy<SdkKnownPackSet>> Cache = new(StringComparers.Paths);
	private static readonly KnownPackItemMetadata[] KnownAnalyzerPackItemMetadata =
	[
		new("KnownILLinkPack", "ILLinkPackVersion"),
	];

	public static bool TryGetTargetingPackVersionForSdk(
		string? sdkPath,
		string packName,
		string? targetFramework,
		out string? targetingPackVersion)
	{
		targetingPackVersion = null;
		if (string.IsNullOrWhiteSpace(sdkPath) || string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(targetFramework))
			return false;

		return GetReferenceSet(sdkPath!).TryGetTargetingPackVersion(packName, targetFramework, out targetingPackVersion);
	}

	public static bool TryGetSdkAnalyzerPackVersionForSdk(
		string? sdkPath,
		string packageId,
		string? targetFramework,
		out string? packageVersion)
	{
		packageVersion = null;
		if (string.IsNullOrWhiteSpace(sdkPath)
			|| string.IsNullOrWhiteSpace(packageId)
			|| string.IsNullOrWhiteSpace(targetFramework))
		{
			return false;
		}

		return GetReferenceSet(sdkPath!).TryGetSdkAnalyzerPackVersion(packageId, targetFramework, out packageVersion);
	}

	private static SdkKnownPackSet GetReferenceSet(string sdkPath)
	{
		string normalizedSdkPath = NormalizeSdkPath(sdkPath);
		// Wrap the IO+XML parse in Lazy so racing callers can't trigger duplicate parses.
		return Cache.GetOrAdd(
			normalizedSdkPath,
			static path => new Lazy<SdkKnownPackSet>(() => ParseReferences(path), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
	}

	private static SdkKnownPackSet ParseReferences(string sdkPath)
	{
		string propsPath = Path.Combine(sdkPath, "Microsoft.NETCoreSdk.BundledVersions.props");
		if (!File.Exists(propsPath))
		{
			return SdkKnownPackSet.Empty;
		}

		XDocument doc;
		try
		{
			doc = XDocument.Load(propsPath, LoadOptions.None);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to parse known-pack metadata from {0}: {1}",
				propsPath,
				ex.Message);
			return SdkKnownPackSet.Empty;
		}

		List<KnownFrameworkReference> references = [];
		List<KnownSdkAnalyzerPack> analyzerPacks = [];
		foreach (XElement element in doc.Descendants())
		{
			if (element.Name.LocalName == "KnownFrameworkReference")
			{
				string? targetFramework = (string?)element.Attribute("TargetFramework");
				string? targetingPackName = (string?)element.Attribute("TargetingPackName");
				string? targetingPackVersion = (string?)element.Attribute("TargetingPackVersion");
				if (string.IsNullOrWhiteSpace(targetFramework)
					|| string.IsNullOrWhiteSpace(targetingPackName)
					|| string.IsNullOrWhiteSpace(targetingPackVersion))
				{
					continue;
				}

				references.Add(new KnownFrameworkReference(targetFramework!, targetingPackName!, targetingPackVersion!));
				continue;
			}

			if (TryGetKnownAnalyzerPackVersionMetadata(element.Name.LocalName, out string? versionMetadataName))
			{
				string? targetFramework = (string?)element.Attribute("TargetFramework");
				string? packageId = (string?)element.Attribute("Include");
				string? packageVersion = (string?)element.Attribute(versionMetadataName);
				if (string.IsNullOrWhiteSpace(targetFramework)
					|| string.IsNullOrWhiteSpace(packageId)
					|| string.IsNullOrWhiteSpace(packageVersion))
				{
					continue;
				}

				analyzerPacks.Add(new KnownSdkAnalyzerPack(targetFramework!, packageId!, packageVersion!));
			}
		}

		return new SdkKnownPackSet(references.ToArray(), analyzerPacks.ToArray());
	}

	private static string NormalizeSdkPath(string sdkPath)
	{
		string full = Path.GetFullPath(sdkPath);
		if (string.Equals(Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "Sdks", StringComparison.OrdinalIgnoreCase))
		{
			full = Path.GetDirectoryName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? full;
		}

		return full;
	}

	private static bool TryGetKnownAnalyzerPackVersionMetadata(string itemType, out string versionMetadataName)
	{
		foreach (KnownPackItemMetadata metadata in KnownAnalyzerPackItemMetadata)
		{
			if (string.Equals(metadata.ItemType, itemType, StringComparison.OrdinalIgnoreCase))
			{
				versionMetadataName = metadata.VersionMetadataName;
				return true;
			}
		}

		versionMetadataName = string.Empty;
		return false;
	}

	private sealed class SdkKnownPackSet
	{
		public static readonly SdkKnownPackSet Empty = new([], []);

		private readonly KnownFrameworkReference[] references;
		private readonly KnownSdkAnalyzerPack[] analyzerPacks;

		public SdkKnownPackSet(KnownFrameworkReference[] references, KnownSdkAnalyzerPack[] analyzerPacks)
		{
			this.references = references;
			this.analyzerPacks = analyzerPacks;
		}

		public bool TryGetTargetingPackVersion(string packName, string targetFramework, out string? targetingPackVersion)
		{
			foreach (KnownFrameworkReference reference in this.references)
			{
				if (TargetFrameworkMatches(reference.TargetFramework, targetFramework)
					&& string.Equals(reference.TargetingPackName, packName, StringComparison.OrdinalIgnoreCase))
				{
					targetingPackVersion = reference.TargetingPackVersion;
					return true;
				}
			}

			targetingPackVersion = null;
			return false;
		}

		public bool TryGetSdkAnalyzerPackVersion(string packageId, string targetFramework, out string? packageVersion)
		{
			foreach (KnownSdkAnalyzerPack pack in this.analyzerPacks)
			{
				if (TargetFrameworkMatches(pack.TargetFramework, targetFramework)
					&& string.Equals(pack.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
				{
					packageVersion = pack.PackageVersion;
					return true;
				}
			}

			packageVersion = null;
			return false;
		}

		private static bool TargetFrameworkMatches(string knownTargetFramework, string requestedTargetFramework)
		{
			if (string.Equals(knownTargetFramework, requestedTargetFramework, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			// Match on identifier + (major, minor) rather than identifier + major alone.
			// Earlier the fallback compared only the first digit run, so ``net4.8`` matched
			// ``net4.0`` and a request for ``net8.1`` could resolve against ``net8.0`` data.
			if (!TryParseTfm(knownTargetFramework, out string knownId, out Version knownVersion)
				|| !TryParseTfm(requestedTargetFramework, out string requestedId, out Version requestedVersion))
			{
				return false;
			}

			return string.Equals(knownId, requestedId, StringComparison.OrdinalIgnoreCase)
				&& knownVersion.Major == requestedVersion.Major
				&& knownVersion.Minor == requestedVersion.Minor;
		}

		private static bool TryParseTfm(string? tfm, out string identifier, out Version version)
		{
			identifier = string.Empty;
			version = new Version(0, 0);
			if (string.IsNullOrEmpty(tfm))
			{
				return false;
			}

			int versionStart = -1;
			for (int i = 0; i < tfm.Length; i++)
			{
				if (char.IsDigit(tfm[i]))
				{
					versionStart = i;
					break;
				}
			}
			if (versionStart <= 0)
			{
				return false;
			}

			identifier = tfm[..versionStart];
			string versionPart = tfm[versionStart..];
			// ``Version.TryParse`` requires at least ``major.minor``; pad bare ``net8`` → ``8.0``.
			if (!versionPart.Contains('.'))
			{
				versionPart += ".0";
			}
			if (Version.TryParse(versionPart, out Version? parsed))
			{
				version = parsed;
				return true;
			}
			return false;
		}
	}

	private readonly struct KnownFrameworkReference
	{
		public KnownFrameworkReference(string targetFramework, string targetingPackName, string targetingPackVersion)
		{
			this.TargetFramework = targetFramework;
			this.TargetingPackName = targetingPackName;
			this.TargetingPackVersion = targetingPackVersion;
		}

		public string TargetFramework { get; }
		public string TargetingPackName { get; }
		public string TargetingPackVersion { get; }
	}

	private readonly struct KnownSdkAnalyzerPack
	{
		public KnownSdkAnalyzerPack(string targetFramework, string packageId, string packageVersion)
		{
			this.TargetFramework = targetFramework;
			this.PackageId = packageId;
			this.PackageVersion = packageVersion;
		}

		public string TargetFramework { get; }
		public string PackageId { get; }
		public string PackageVersion { get; }
	}

	private readonly struct KnownPackItemMetadata
	{
		public KnownPackItemMetadata(string itemType, string versionMetadataName)
		{
			this.ItemType = itemType;
			this.VersionMetadataName = versionMetadataName;
		}

		public string ItemType { get; }
		public string VersionMetadataName { get; }
	}
}
