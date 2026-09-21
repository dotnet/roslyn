// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Validates that evaluated package references agree with the resolved restore graph and that
/// resolved package folders still exist.
/// </summary>
public sealed class ValidateProjectDataPackagesTask : Microsoft.Build.Utilities.Task
{
	[Required]
	public string ProjectFilePath { get; set; } = string.Empty;

	public string AssetsFile { get; set; } = string.Empty;

	public string TargetFramework { get; set; } = string.Empty;

	public string TargetFrameworkMoniker { get; set; } = string.Empty;

	public bool ManagePackageVersionsCentrally { get; set; }

	public bool CentralPackageTransitivePinningEnabled { get; set; }

	public ITaskItem[] PackageReferences { get; set; } = [];

	public ITaskItem[] PackageVersions { get; set; } = [];

	public ITaskItem[] ResolvedPackages { get; set; } = [];

	public override bool Execute()
	{
		Dictionary<string, ITaskItem> resolvedPackagesById = this.GetResolvedPackagesById();
		Dictionary<string, string> centralVersionsById = this.GetCentralVersionsById();
		RestoreGraphRequests? restoredRequests = this.GetRestoreGraphRequests();
		Dictionary<string, string>? restoredRequestedVersionsById = restoredRequests?.DirectRequests;
		var currentPackageIds = new HashSet<string>(
			this.PackageReferences.Select(static packageReference => packageReference.ItemSpec),
			StringComparer.OrdinalIgnoreCase);
		List<string> missingPackages = [];
		List<string> missingRequestedVersions = [];
		List<string> incompatiblePackages = [];
		List<string> staleRequestedVersions = [];
		List<string> incompatibleCentralTransitiveVersions = [];
		List<string> staleCentralTransitiveVersions = [];
		bool centralTransitivePinningModeChanged = restoredRequests?.CentralPackageTransitivePinningEnabled is bool restoredPinningEnabled &&
			restoredPinningEnabled != this.CentralPackageTransitivePinningEnabled;

		foreach (ITaskItem packageReference in this.PackageReferences)
		{
			if (string.Equals(packageReference.GetMetadata("IsImplicitlyDefined"), "true", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string packageId = packageReference.ItemSpec;
			if (!resolvedPackagesById.TryGetValue(packageId, out ITaskItem? resolvedPackage))
			{
				missingPackages.Add(packageId);
				continue;
			}

			string requestedVersion = this.GetRequestedVersion(packageReference, centralVersionsById);
			if (string.IsNullOrWhiteSpace(requestedVersion))
			{
				missingRequestedVersions.Add(packageId);
				continue;
			}

			string resolvedVersion = GetResolvedVersion(resolvedPackage);
			if (!VersionRange.TryParse(requestedVersion, out VersionRange? requestedRange))
			{
				incompatiblePackages.Add($"{packageId} (invalid requested version '{requestedVersion}')");
				continue;
			}

			if (restoredRequestedVersionsById is not null)
			{
				if (!restoredRequestedVersionsById.TryGetValue(packageId, out string? restoredRequestedVersion))
				{
					staleRequestedVersions.Add($"{packageId} (current request '{requestedVersion}', missing from restored requests)");
				}
				else if (!VersionRange.TryParse(restoredRequestedVersion, out VersionRange? restoredRequestedRange) ||
					!requestedRange.Equals(restoredRequestedRange))
				{
					staleRequestedVersions.Add($"{packageId} (current request '{requestedVersion}', restored request '{restoredRequestedVersion}')");
				}
				else if (restoredRequests!.DirectAssetSelections.TryGetValue(packageId, out PackageAssetSelection? restoredAssetSelection))
				{
					PackageAssetSelection currentAssetSelection = GetCurrentAssetSelection(packageReference);
					if (!currentAssetSelection.Equals(restoredAssetSelection))
					{
						staleRequestedVersions.Add(
							$"{packageId} (current assets '{currentAssetSelection}', restored assets '{restoredAssetSelection}')");
					}
				}
			}

			if (!NuGetVersion.TryParse(resolvedVersion, out NuGetVersion? resolvedNuGetVersion))
			{
				incompatiblePackages.Add($"{packageId} (requested '{requestedVersion}', invalid resolved version '{resolvedVersion}')");
			}
			else if (!requestedRange.Satisfies(resolvedNuGetVersion))
			{
				incompatiblePackages.Add($"{packageId} (requested '{requestedVersion}', resolved '{resolvedVersion}')");
			}
			else if (requestedRange.Float is not null && !requestedRange.Float.Satisfies(resolvedNuGetVersion))
			{
				incompatiblePackages.Add($"{packageId} (requested '{requestedVersion}', resolved '{resolvedVersion}')");
			}
		}

		if (this.CentralPackageTransitivePinningEnabled && restoredRequests is not null)
		{
			var activeCentralTransitiveVersionsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, string> centralVersion in centralVersionsById)
			{
				if (!currentPackageIds.Contains(centralVersion.Key) &&
					restoredRequests.ResolvedVersions.TryGetValue(centralVersion.Key, out string? centralResolvedVersion) &&
					(restoredRequests.CentralTransitiveRequests.ContainsKey(centralVersion.Key) ||
						!restoredRequests.CentralVersions.TryGetValue(centralVersion.Key, out string? restoredCentralVersion) ||
						!AreEquivalentVersionRanges(centralVersion.Value, restoredCentralVersion)))
				{
					activeCentralTransitiveVersionsById[centralVersion.Key] = centralVersion.Value;
					this.ValidateCentralTransitiveVersion(
						centralVersion.Key,
						centralVersion.Value,
						centralResolvedVersion,
						incompatibleCentralTransitiveVersions);
				}
			}

			foreach (KeyValuePair<string, string> currentRequest in activeCentralTransitiveVersionsById)
			{
				if (!restoredRequests.CentralTransitiveRequests.TryGetValue(currentRequest.Key, out string? restoredRequest))
				{
					staleCentralTransitiveVersions.Add($"{currentRequest.Key} (current request '{currentRequest.Value}', missing from restored central transitive requests)");
				}
				else if (!AreEquivalentVersionRanges(currentRequest.Value, restoredRequest))
				{
					staleCentralTransitiveVersions.Add($"{currentRequest.Key} (current request '{currentRequest.Value}', restored request '{restoredRequest}')");
				}
			}

			foreach (KeyValuePair<string, string> restoredRequest in restoredRequests.CentralTransitiveRequests)
			{
				if (!activeCentralTransitiveVersionsById.ContainsKey(restoredRequest.Key))
				{
					staleCentralTransitiveVersions.Add($"{restoredRequest.Key} (restored request '{restoredRequest.Value}', no current active central transitive pin)");
				}
			}
		}

		if (restoredRequestedVersionsById is not null)
		{
			foreach (KeyValuePair<string, string> restoredRequest in restoredRequestedVersionsById)
			{
				if (!currentPackageIds.Contains(restoredRequest.Key) &&
					!restoredRequests!.AutoReferencedRequestIds.Contains(restoredRequest.Key))
				{
					staleRequestedVersions.Add($"{restoredRequest.Key} (restored request '{restoredRequest.Value}', no current PackageReference)");
				}
			}
		}

		if (missingPackages.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because the restore graph does not contain declared PackageReference items: {1}. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", missingPackages.OrderBy(static packageId => packageId, StringComparer.OrdinalIgnoreCase)));
		}

		if (missingRequestedVersions.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because declared PackageReference items have no evaluated version request: {1}. Restore may be stale or central package version metadata may be missing. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", missingRequestedVersions.OrderBy(static packageId => packageId, StringComparer.OrdinalIgnoreCase)));
		}

		if (incompatiblePackages.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because declared PackageReference versions are not satisfied by the restore graph: {1}. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", incompatiblePackages.OrderBy(static package => package, StringComparer.OrdinalIgnoreCase)));
		}

		if (staleRequestedVersions.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because declared PackageReference requests differ from the restore graph: {1}. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", staleRequestedVersions.OrderBy(static package => package, StringComparer.OrdinalIgnoreCase)));
		}

		if (incompatibleCentralTransitiveVersions.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because central transitive package version requests are not satisfied by the restore graph: {1}. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", incompatibleCentralTransitiveVersions.OrderBy(static package => package, StringComparer.OrdinalIgnoreCase)));
		}

		if (staleCentralTransitiveVersions.Count > 0)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because central transitive package version requests differ from the restore graph: {1}. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				string.Join("; ", staleCentralTransitiveVersions.OrderBy(static package => package, StringComparer.OrdinalIgnoreCase)));
		}

		if (centralTransitivePinningModeChanged)
		{
			this.Log.LogError(
				"ProjectData: cannot write project data for '{0}' because central transitive package pinning mode differs from the restore graph: current '{1}', restored '{2}'. Run restore successfully before ProjectDataBuild.",
				this.ProjectFilePath,
				this.CentralPackageTransitivePinningEnabled,
				restoredRequests!.CentralPackageTransitivePinningEnabled);
		}

		return !this.Log.HasLoggedErrors;
	}

	private void ValidateCentralTransitiveVersion(
		string packageId,
		string requestedVersion,
		string resolvedVersion,
		List<string> incompatiblePackages)
	{
		if (!VersionRange.TryParse(requestedVersion, out VersionRange? requestedRange))
		{
			incompatiblePackages.Add($"{packageId} (invalid requested version '{requestedVersion}')");
			return;
		}

		if (!NuGetVersion.TryParse(resolvedVersion, out NuGetVersion? resolvedNuGetVersion))
		{
			incompatiblePackages.Add($"{packageId} (requested '{requestedVersion}', invalid resolved version '{resolvedVersion}')");
		}
		else if (!requestedRange.Satisfies(resolvedNuGetVersion) ||
			(requestedRange.Float is not null && !requestedRange.Float.Satisfies(resolvedNuGetVersion)))
		{
			incompatiblePackages.Add($"{packageId} (requested '{requestedVersion}', resolved '{resolvedVersion}')");
		}
	}

	private Dictionary<string, ITaskItem> GetResolvedPackagesById()
	{
		var result = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
		foreach (ITaskItem resolvedPackage in this.ResolvedPackages)
		{
			string packageId = resolvedPackage.GetMetadata("Name");
			if (string.IsNullOrWhiteSpace(packageId))
			{
				packageId = GetIdentityPart(resolvedPackage.ItemSpec);
			}

			if (!string.IsNullOrWhiteSpace(packageId))
			{
				result[packageId] = resolvedPackage;
			}

			string packagePath = resolvedPackage.GetMetadata("Path");
			if (string.IsNullOrWhiteSpace(packagePath))
			{
				this.Log.LogError(
					"ProjectData: cannot write project data for '{0}' because resolved package '{1}' has no package path. Run restore successfully before ProjectDataBuild.",
					this.ProjectFilePath,
					resolvedPackage.ItemSpec);
			}
			else if (!Directory.Exists(packagePath))
			{
				this.Log.LogError(
					"ProjectData: cannot write project data for '{0}' because package files are missing: {1} at {2}. Run restore successfully before ProjectDataBuild.",
					this.ProjectFilePath,
					resolvedPackage.ItemSpec,
					packagePath);
			}
		}

		return result;
	}

	private Dictionary<string, string> GetCentralVersionsById()
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (ITaskItem packageVersion in this.PackageVersions)
		{
			string version = packageVersion.GetMetadata("Version");
			if (!string.IsNullOrWhiteSpace(packageVersion.ItemSpec) && !string.IsNullOrWhiteSpace(version))
			{
				result[packageVersion.ItemSpec] = version;
			}
		}

		return result;
	}

	private RestoreGraphRequests? GetRestoreGraphRequests()
	{
		if (string.IsNullOrWhiteSpace(this.AssetsFile) || string.IsNullOrWhiteSpace(this.TargetFramework))
		{
			return null;
		}

		try
		{
			using FileStream stream = File.OpenRead(this.AssetsFile);
			using JsonDocument assetsFile = JsonDocument.Parse(stream);
			if (!TryGetProperty(assetsFile.RootElement, "project", out JsonElement project) ||
				!TryGetProperty(project, "frameworks", out JsonElement frameworks) ||
				!TryGetProperty(frameworks, this.TargetFramework, out JsonElement framework))
			{
				this.Log.LogError(
					"ProjectData: cannot write project data for '{0}' because restore graph '{1}' does not contain dependency requests for target framework '{2}'. Run restore successfully before ProjectDataBuild.",
					this.ProjectFilePath,
					this.AssetsFile,
					this.TargetFramework);
				return null;
			}

			var directRequests = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var directAssetSelections = new Dictionary<string, PackageAssetSelection>(StringComparer.OrdinalIgnoreCase);
			var autoReferencedRequestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (framework.ValueKind != JsonValueKind.Object)
			{
				this.LogAssetsFileError($"target framework '{this.TargetFramework}' must be a JSON object");
				return null;
			}

			bool hasCentralPackageEvidence = this.ManagePackageVersionsCentrally ||
				this.CentralPackageTransitivePinningEnabled ||
				this.PackageVersions.Length > 0 ||
				TryGetProperty(framework, "centralPackageVersions", out _) ||
				TryGetProperty(assetsFile.RootElement, "centralTransitiveDependencyGroups", out _);
			bool? restoredPinningEnabled = this.GetRestoredCentralPackageTransitivePinningMode(project, hasCentralPackageEvidence);
			if (hasCentralPackageEvidence && restoredPinningEnabled is null)
			{
				return null;
			}

			if (!TryGetProperty(framework, "dependencies", out JsonElement dependencies))
			{
				dependencies = default;
			}
			else if (dependencies.ValueKind != JsonValueKind.Object)
			{
				this.LogAssetsFileError($"dependency requests for target framework '{this.TargetFramework}' must be a JSON object");
				return null;
			}

			if (dependencies.ValueKind == JsonValueKind.Object)
			{
				foreach (JsonProperty dependency in dependencies.EnumerateObject())
				{
					if (!TryGetStringProperty(dependency.Value, "target", out string target))
					{
						this.LogAssetsFileError($"dependency request '{dependency.Name}' for target framework '{this.TargetFramework}' has no string target");
						return null;
					}

					if (!string.Equals(target, "Package", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					if (!TryGetStringProperty(dependency.Value, "version", out string version) ||
						string.IsNullOrWhiteSpace(version))
					{
						this.LogAssetsFileError($"package dependency request '{dependency.Name}' for target framework '{this.TargetFramework}' has no string version");
						return null;
					}

					if (TryGetBooleanProperty(dependency.Value, "autoReferenced", out bool autoReferenced) &&
						autoReferenced)
					{
						autoReferencedRequestIds.Add(dependency.Name);
					}

					directRequests[dependency.Name] = version;
					if (!this.TryGetRestoredAssetSelection(dependency, this.TargetFramework, out PackageAssetSelection? assetSelection))
					{
						return null;
					}

					directAssetSelections[dependency.Name] = assetSelection;
				}
			}

			Dictionary<string, string>? restoredCentralVersions = this.GetRestoredCentralVersions(framework);
			Dictionary<string, string>? centralTransitiveRequests = this.GetRestoredCentralTransitiveRequests(
				assetsFile.RootElement,
				directRequests);
			Dictionary<string, string>? resolvedVersions = this.GetRestoredResolvedVersions(assetsFile.RootElement);
			return restoredCentralVersions is null || centralTransitiveRequests is null || resolvedVersions is null
				? null
				: new RestoreGraphRequests(
					directRequests,
					directAssetSelections,
					autoReferencedRequestIds,
					restoredCentralVersions,
					centralTransitiveRequests,
					resolvedVersions,
					restoredPinningEnabled);
		}
		catch (IOException ex)
		{
			this.LogAssetsFileError(ex.Message);
		}
		catch (UnauthorizedAccessException ex)
		{
			this.LogAssetsFileError(ex.Message);
		}
		catch (JsonException ex)
		{
			this.LogAssetsFileError(ex.Message);
		}

		return null;
	}

	private bool TryGetRestoredAssetSelection(
		JsonProperty dependency,
		string targetFramework,
		out PackageAssetSelection assetSelection)
	{
		PackageAssetFlags include = PackageAssetFlags.All;
		if (TryGetProperty(dependency.Value, "include", out JsonElement includeElement))
		{
			if (includeElement.ValueKind != JsonValueKind.String)
			{
				this.LogAssetsFileError($"package dependency request '{dependency.Name}' for target framework '{targetFramework}' has no string include assets");
				assetSelection = null!;
				return false;
			}

			include = ParseRestoredAssetFlags(includeElement.GetString(), PackageAssetFlags.All);
		}

		PackageAssetFlags suppressParent = PackageAssetSelection.DefaultSuppressParent;
		if (TryGetProperty(dependency.Value, "suppressParent", out JsonElement suppressParentElement))
		{
			if (suppressParentElement.ValueKind != JsonValueKind.String)
			{
				this.LogAssetsFileError($"package dependency request '{dependency.Name}' for target framework '{targetFramework}' has no string private assets");
				assetSelection = null!;
				return false;
			}

			suppressParent = ParseRestoredAssetFlags(suppressParentElement.GetString(), PackageAssetSelection.DefaultSuppressParent);
		}

		assetSelection = new PackageAssetSelection(include, suppressParent);
		return true;
	}

	private bool? GetRestoredCentralPackageTransitivePinningMode(JsonElement project, bool required)
	{
		if (!TryGetProperty(project, "restore", out JsonElement restore))
		{
			if (required)
			{
				this.LogAssetsFileError("restore settings do not contain central transitive package pinning mode");
			}

			return null;
		}

		if (restore.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError("restore settings must be a JSON object");
			return null;
		}

		if (!TryGetProperty(restore, "CentralPackageTransitivePinningEnabled", out JsonElement pinningEnabled))
		{
			// NuGet 6.13 writes the property only when enabled; omission records false.
			return required ? false : null;
		}

		if (pinningEnabled.ValueKind != JsonValueKind.True &&
			pinningEnabled.ValueKind != JsonValueKind.False)
		{
			this.LogAssetsFileError("central transitive package pinning mode in restore settings must be a JSON boolean");
			return null;
		}

		return pinningEnabled.GetBoolean();
	}

	private Dictionary<string, string>? GetRestoredCentralVersions(JsonElement framework)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!this.CentralPackageTransitivePinningEnabled ||
			!TryGetProperty(framework, "centralPackageVersions", out JsonElement centralVersions))
		{
			return result;
		}

		if (centralVersions.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError($"central package versions for target framework '{this.TargetFramework}' must be a JSON object");
			return null;
		}

		foreach (JsonProperty centralVersion in centralVersions.EnumerateObject())
		{
			if (centralVersion.Value.ValueKind != JsonValueKind.String ||
				string.IsNullOrWhiteSpace(centralVersion.Value.GetString()))
			{
				this.LogAssetsFileError($"central package version '{centralVersion.Name}' for target framework '{this.TargetFramework}' has no string version");
				return null;
			}

			result[centralVersion.Name] = centralVersion.Value.GetString()!;
		}

		return result;
	}

	private Dictionary<string, string>? GetRestoredCentralTransitiveRequests(
		JsonElement assetsFile,
		IReadOnlyDictionary<string, string> directRequests)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!this.CentralPackageTransitivePinningEnabled ||
			!TryGetProperty(assetsFile, "centralTransitiveDependencyGroups", out JsonElement groups))
		{
			return result;
		}

		if (groups.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError("central transitive dependency groups must be a JSON object");
			return null;
		}

		if (!this.TryGetTargetFrameworkGroup(groups, out JsonElement group))
		{
			return result;
		}

		if (group.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError($"central transitive dependency group for target framework '{this.TargetFramework}' must be a JSON object");
			return null;
		}

		foreach (JsonProperty dependency in group.EnumerateObject())
		{
			if (directRequests.ContainsKey(dependency.Name))
			{
				continue;
			}

			if (!TryGetStringProperty(dependency.Value, "version", out string version) ||
				string.IsNullOrWhiteSpace(version))
			{
				this.LogAssetsFileError($"central transitive dependency request '{dependency.Name}' for target framework '{this.TargetFramework}' has no string version");
				return null;
			}

			result[dependency.Name] = version;
		}

		return result;
	}

	private Dictionary<string, string>? GetRestoredResolvedVersions(JsonElement assetsFile)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!this.CentralPackageTransitivePinningEnabled)
		{
			return result;
		}

		if (!TryGetProperty(assetsFile, "targets", out JsonElement targets) ||
			targets.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError("resolved target graphs must be a JSON object");
			return null;
		}

		if (!this.TryGetTargetFrameworkGroup(targets, out JsonElement target))
		{
			this.LogAssetsFileError($"resolved target graphs do not contain target framework '{this.TargetFramework}'");
			return null;
		}

		if (target.ValueKind != JsonValueKind.Object)
		{
			this.LogAssetsFileError($"resolved target graph for target framework '{this.TargetFramework}' must be a JSON object");
			return null;
		}

		foreach (JsonProperty dependency in target.EnumerateObject())
		{
			if (!TryGetStringProperty(dependency.Value, "type", out string dependencyType))
			{
				this.LogAssetsFileError($"resolved dependency '{dependency.Name}' for target framework '{this.TargetFramework}' has no string type");
				return null;
			}

			if (!string.Equals(dependencyType, "package", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			int separatorIndex = dependency.Name.LastIndexOf('/');
			if (separatorIndex <= 0 || separatorIndex == dependency.Name.Length - 1)
			{
				this.LogAssetsFileError($"resolved package identity '{dependency.Name}' for target framework '{this.TargetFramework}' has no concrete version");
				return null;
			}

			result[dependency.Name.Substring(0, separatorIndex)] = dependency.Name.Substring(separatorIndex + 1);
		}

		return result;
	}

	private bool TryGetTargetFrameworkGroup(JsonElement groups, out JsonElement group)
	{
		if (!string.IsNullOrWhiteSpace(this.TargetFrameworkMoniker) &&
			TryGetProperty(groups, this.TargetFrameworkMoniker, out group))
		{
			return true;
		}

		return TryGetProperty(groups, this.TargetFramework, out group);
	}

	private void LogAssetsFileError(string message)
	{
		this.Log.LogError(
			"ProjectData: cannot write project data for '{0}' because restore graph '{1}' could not be read: {2}. Run restore successfully before ProjectDataBuild.",
			this.ProjectFilePath,
			this.AssetsFile,
			message);
	}

	private string GetRequestedVersion(ITaskItem packageReference, IReadOnlyDictionary<string, string> centralVersionsById)
	{
		string versionOverride = packageReference.GetMetadata("VersionOverride");
		if (this.ManagePackageVersionsCentrally && !string.IsNullOrWhiteSpace(versionOverride))
		{
			return versionOverride;
		}

		string version = packageReference.GetMetadata("Version");
		if (!string.IsNullOrWhiteSpace(version))
		{
			return version;
		}

		return this.ManagePackageVersionsCentrally &&
			centralVersionsById.TryGetValue(packageReference.ItemSpec, out string? centralVersion)
				? centralVersion
				: string.Empty;
	}

	private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
	{
		if (element.ValueKind != JsonValueKind.Object)
		{
			value = default;
			return false;
		}

		foreach (JsonProperty property in element.EnumerateObject())
		{
			if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				value = property.Value;
				return true;
			}
		}

		value = default;
		return false;
	}

	private static bool TryGetBooleanProperty(JsonElement element, string propertyName, out bool value)
	{
		if (element.ValueKind == JsonValueKind.Object &&
			element.TryGetProperty(propertyName, out JsonElement property) &&
			(property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False))
		{
			value = property.GetBoolean();
			return true;
		}

		value = false;
		return false;
	}

	private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
	{
		if (element.ValueKind == JsonValueKind.Object &&
			element.TryGetProperty(propertyName, out JsonElement property) &&
			property.ValueKind == JsonValueKind.String)
		{
			value = property.GetString() ?? string.Empty;
			return true;
		}

		value = string.Empty;
		return false;
	}

	private static bool AreEquivalentVersionRanges(string left, string right)
		=> VersionRange.TryParse(left, out VersionRange? leftRange) &&
			VersionRange.TryParse(right, out VersionRange? rightRange) &&
			leftRange.Equals(rightRange);

	private static PackageAssetSelection GetCurrentAssetSelection(ITaskItem packageReference)
	{
		PackageAssetFlags include = ParseEvaluatedAssetFlags(
			packageReference.GetMetadata("IncludeAssets"),
			PackageAssetFlags.All);
		PackageAssetFlags exclude = ParseEvaluatedAssetFlags(
			packageReference.GetMetadata("ExcludeAssets"),
			PackageAssetFlags.None);
		PackageAssetFlags suppressParent = ParseEvaluatedAssetFlags(
			packageReference.GetMetadata("PrivateAssets"),
			PackageAssetSelection.DefaultSuppressParent);
		return new PackageAssetSelection(include & ~exclude, suppressParent);
	}

	private static PackageAssetFlags ParseEvaluatedAssetFlags(string? value, PackageAssetFlags defaultValue)
		=> ParseAssetFlags(value, defaultValue, ';', expandBuildTransitive: true);

	private static PackageAssetFlags ParseRestoredAssetFlags(string? value, PackageAssetFlags defaultValue)
		=> ParseAssetFlags(value, defaultValue, ',', expandBuildTransitive: false);

	private static PackageAssetFlags ParseAssetFlags(
		string? value,
		PackageAssetFlags defaultValue,
		char separator,
		bool expandBuildTransitive)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		PackageAssetFlags result = PackageAssetFlags.None;
		bool hasToken = false;
		foreach (string part in value!.Split([separator], StringSplitOptions.RemoveEmptyEntries))
		{
			string token = part.Trim();
			if (token.Length == 0)
			{
				continue;
			}

			hasToken = true;
			switch (token.ToLowerInvariant())
			{
				case "all":
					result |= PackageAssetFlags.All;
					break;
				case "runtime":
					result |= PackageAssetFlags.Runtime;
					break;
				case "compile":
					result |= PackageAssetFlags.Compile;
					break;
				case "build":
					result |= PackageAssetFlags.Build;
					break;
				case "contentfiles":
					result |= PackageAssetFlags.ContentFiles;
					break;
				case "native":
					result |= PackageAssetFlags.Native;
					break;
				case "analyzers":
					result |= PackageAssetFlags.Analyzers;
					break;
				case "buildtransitive":
					result |= PackageAssetFlags.BuildTransitive;
					if (expandBuildTransitive)
					{
						result |= PackageAssetFlags.Build;
					}
					break;
			}
		}

		return hasToken ? result : defaultValue;
	}

	private static string GetResolvedVersion(ITaskItem resolvedPackage)
	{
		string version = resolvedPackage.GetMetadata("Version");
		if (!string.IsNullOrWhiteSpace(version))
		{
			return version;
		}

		int separatorIndex = resolvedPackage.ItemSpec.LastIndexOf('/');
		return separatorIndex >= 0 ? resolvedPackage.ItemSpec.Substring(separatorIndex + 1) : string.Empty;
	}

	private static string GetIdentityPart(string resolvedPackageIdentity)
	{
		int separatorIndex = resolvedPackageIdentity.LastIndexOf('/');
		return separatorIndex >= 0 ? resolvedPackageIdentity.Substring(0, separatorIndex) : resolvedPackageIdentity;
	}

	private sealed class RestoreGraphRequests
	{
		public RestoreGraphRequests(
			Dictionary<string, string> directRequests,
			Dictionary<string, PackageAssetSelection> directAssetSelections,
			HashSet<string> autoReferencedRequestIds,
			Dictionary<string, string> centralVersions,
			Dictionary<string, string> centralTransitiveRequests,
			Dictionary<string, string> resolvedVersions,
			bool? centralPackageTransitivePinningEnabled)
		{
			this.DirectRequests = directRequests;
			this.DirectAssetSelections = directAssetSelections;
			this.AutoReferencedRequestIds = autoReferencedRequestIds;
			this.CentralVersions = centralVersions;
			this.CentralTransitiveRequests = centralTransitiveRequests;
			this.ResolvedVersions = resolvedVersions;
			this.CentralPackageTransitivePinningEnabled = centralPackageTransitivePinningEnabled;
		}

		public Dictionary<string, string> DirectRequests { get; }

		public Dictionary<string, PackageAssetSelection> DirectAssetSelections { get; }

		public HashSet<string> AutoReferencedRequestIds { get; }

		public Dictionary<string, string> CentralVersions { get; }

		public Dictionary<string, string> CentralTransitiveRequests { get; }

		public Dictionary<string, string> ResolvedVersions { get; }

		public bool? CentralPackageTransitivePinningEnabled { get; }
	}

	[Flags]
	private enum PackageAssetFlags
	{
		None = 0,
		Runtime = 1 << 0,
		Compile = 1 << 1,
		Build = 1 << 2,
		ContentFiles = 1 << 3,
		Native = 1 << 4,
		Analyzers = 1 << 5,
		BuildTransitive = 1 << 6,
		All = Runtime | Compile | Build | ContentFiles | Native | Analyzers | BuildTransitive,
	}

	private sealed class PackageAssetSelection : IEquatable<PackageAssetSelection>
	{
		public const PackageAssetFlags DefaultSuppressParent =
			PackageAssetFlags.Build | PackageAssetFlags.ContentFiles | PackageAssetFlags.Analyzers;

		public PackageAssetSelection(PackageAssetFlags include, PackageAssetFlags suppressParent)
		{
			this.Include = include;
			this.SuppressParent = suppressParent;
		}

		public PackageAssetFlags Include { get; }

		public PackageAssetFlags SuppressParent { get; }

		public bool Equals(PackageAssetSelection? other)
			=> other is not null &&
				this.Include == other.Include &&
				this.SuppressParent == other.SuppressParent;

		public override bool Equals(object? obj) => this.Equals(obj as PackageAssetSelection);

		public override int GetHashCode() => ((int)this.Include * 397) ^ (int)this.SuppressParent;

		public override string ToString() => $"include={this.Include}; private={this.SuppressParent}";
	}
}
