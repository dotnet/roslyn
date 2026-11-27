// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Decides which analyzers expanded from a <c>[frameworkPacks]</c> pack manifest are enabled.
/// Shared by the writer, which records an exact allow-list with an input fingerprint, and the
/// reader, which trusts that allow-list only while the fingerprint still matches the slice's
/// command line.
/// </summary>
/// <remarks>
/// <para>A pack manifest lists every analyzer in the pack, including source generators the SDK
/// excludes unless a project opts in. Expanding all of them on read enables generators whose
/// interceptor namespaces were never allowed, which produces CS9137 in the editor even though the
/// command-line build succeeds.</para>
/// <para>Older writers carry an unknown allow-list section forward verbatim (see
/// <c>ForwardCompat</c>) while regenerating every section they understand, so the section can be
/// stale even under a current version stamp. The fingerprint records the
/// <c>InterceptorsNamespaces</c> value the allow-list was derived from: SDK opt-ins for these
/// generators also add their namespace, so a stale allow-list is detected and replaced by
/// <see cref="IsPermittedByInterceptorsNamespaces"/>.</para>
/// </remarks>
internal static class FrameworkAnalyzerSelection
{
	/// <summary>
	/// Key of the first line of a <c>[frameworkAnalyzerReferences]</c> section.
	/// </summary>
	public const string InterceptorsNamespacesKey = "interceptorsNamespaces=";

	private const string InterceptorsNamespacesFeature = "InterceptorsNamespaces";

	/// <summary>
	/// Framework-pack generators the SDK excludes by default, keyed by file name, with the
	/// namespace their generated interceptors are declared in. The SDK adds the namespace to
	/// <c>InterceptorsNamespaces</c> exactly when it enables the generator.
	/// </summary>
	private static readonly Dictionary<string, string> InterceptorGenerators = new(StringComparer.OrdinalIgnoreCase)
	{
		["Microsoft.AspNetCore.Http.RequestDelegateGenerator.dll"] = "Microsoft.AspNetCore.Http.Generated",
		["Microsoft.Extensions.Configuration.Binder.SourceGeneration.dll"] = "Microsoft.Extensions.Configuration.Binder.SourceGeneration",
	};

	/// <summary>
	/// Returns the effective <c>InterceptorsNamespaces</c> feature value, mirroring the compiler:
	/// <c>/features:</c> values are unquoted and split on commas, the last assignment wins, and a
	/// bare <c>/features</c> clears earlier features.
	/// </summary>
	public static string? GetInterceptorsNamespaces(IEnumerable<string> commandLineArguments)
	{
		string? value = null;
		foreach (string argument in commandLineArguments)
		{
			if (argument.Length < 2 || (argument[0] != '/' && argument[0] != '-'))
				continue;

			string option = argument.Substring(1);
			if (string.Equals(option, "features", StringComparison.OrdinalIgnoreCase))
			{
				value = null;
				continue;
			}

			const string FeaturesPrefix = "features:";
			if (!option.StartsWith(FeaturesPrefix, StringComparison.OrdinalIgnoreCase))
				continue;

			string features = Unquote(option.Substring(FeaturesPrefix.Length));
			foreach (string feature in features.Split([','], StringSplitOptions.RemoveEmptyEntries))
			{
				int equals = feature.IndexOf('=');
				if (equals > 0 && string.Equals(feature.Substring(0, equals), InterceptorsNamespacesFeature, StringComparison.Ordinal))
				{
					value = feature.Substring(equals + 1).Trim('"');
				}
			}
		}

		return value;
	}

	/// <summary>
	/// Returns the canonical fingerprint value for an <c>InterceptorsNamespaces</c> feature value:
	/// distinct non-empty namespaces, sorted ordinally and joined with semicolons.
	/// </summary>
	public static string NormalizeInterceptorsNamespaces(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return "";

		var namespaces = new SortedSet<string>(StringComparer.Ordinal);
		foreach (string ns in value!.Split([';'], StringSplitOptions.RemoveEmptyEntries))
		{
			string trimmed = ns.Trim();
			if (trimmed.Length > 0)
				namespaces.Add(trimmed);
		}

		return string.Join(";", namespaces);
	}

	/// <summary>
	/// Returns <see langword="false"/> only for a known framework interceptor generator whose
	/// namespace is not permitted by <paramref name="interceptorsNamespaces"/>, using the
	/// compiler's rule: a listed namespace permits itself and every nested namespace, and
	/// <c>global</c> permits all namespaces.
	/// </summary>
	public static bool IsPermittedByInterceptorsNamespaces(string analyzerFileName, string? interceptorsNamespaces)
	{
		if (!InterceptorGenerators.TryGetValue(analyzerFileName, out string? generatorNamespace))
			return true;

		if (string.IsNullOrEmpty(interceptorsNamespaces))
			return false;

		foreach (string ns in interceptorsNamespaces!.Split([';'], StringSplitOptions.RemoveEmptyEntries))
		{
			string trimmed = ns.Trim();
			if (trimmed == "global"
				|| string.Equals(trimmed, generatorNamespace, StringComparison.Ordinal)
				|| (generatorNamespace.Length > trimmed.Length
					&& generatorNamespace[trimmed.Length] == '.'
					&& generatorNamespace.StartsWith(trimmed, StringComparison.Ordinal)))
			{
				return true;
			}
		}

		return false;
	}

	private static string Unquote(string value)
		=> value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"'
			? value.Substring(1, value.Length - 2)
			: value;
}
