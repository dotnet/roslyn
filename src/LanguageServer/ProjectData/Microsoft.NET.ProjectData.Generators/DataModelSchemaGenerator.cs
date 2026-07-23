// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.ProjectData.Generators;

/// <summary>
/// Reads <c>project-data-schema.json</c> and generates strongly-typed constants
/// and accessors for the Data Model schema.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DataModelSchemaGenerator : IIncrementalGenerator
{
	private const string SchemaFileName = "project-data-schema.json";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValueProvider<(ImmutableArray<AdditionalText> Left, Compilation Right)> schemaAndCompilation = context.AdditionalTextsProvider.Collect()
			.Combine(context.CompilationProvider);

		context.RegisterSourceOutput(schemaAndCompilation, static (ctx, input) =>
		{
			ImmutableArray<AdditionalText> files = input.Left;
			Microsoft.CodeAnalysis.Compilation compilation = input.Right;

			AdditionalText? schemaFile = null;
			foreach (AdditionalText file in files)
			{
				if (Path.GetFileName(file.Path).Equals(SchemaFileName, StringComparison.OrdinalIgnoreCase))
				{
					schemaFile = file;
					break;
				}
			}

			if (schemaFile is null)
				return;

			string? text = schemaFile.GetText(ctx.CancellationToken)?.ToString();
			if (text is null)
				return;

			Schema? schema = ParseSchema(text);
			if (schema is null)
				return;

			// Derive namespace from the consuming project's assembly name.
			// CSDevKit → CSDevKit.Contracts.DataModel (its existing brokered DTO convention)
			// ProjectData reader/tasks → their root namespace
			string assemblyName = compilation.AssemblyName ?? "";
			string ns = assemblyName switch
			{
				"CSDevKit" => "CSDevKit.Contracts.DataModel",
				_ => assemblyName,
			};

			ctx.AddSource("ProjectProperties.g.cs", GeneratorSourceText.From(GenerateProjectProperties(schema, ns)));
			ctx.AddSource("ProjectItems.g.cs", GeneratorSourceText.From(GenerateProjectItems(schema, ns)));

			// Wire-format constants for the on-disk .lscache file. Emitted into the same
			// namespace as the other generated constants so the writer and reader can
			// reference a single source of truth and a rename cannot silently drop a
			// section.
			ctx.AddSource("CacheFormat.g.cs", GeneratorSourceText.From(GenerateCacheFormat(schema, ns)));
			ctx.AddSource("PathSentinels.g.cs", GeneratorSourceText.From(GeneratePathSentinels(schema.PathSentinels, ns)));

			// Only emit TypedProperties when the consuming project has KeyValueCollection;
			// the generated accessors cannot compile without the immutable model types.
			bool hasKeyValueCollection = compilation.GetTypeByMetadataName(
				$"{ns}.KeyValueCollection") is not null;
			if (hasKeyValueCollection)
			{
				ctx.AddSource("TypedProperties.g.cs", GeneratorSourceText.From(GenerateTypedProperties(schema, ns)));
			}
		});
	}

	private static PropertyDef ParseProperty(JsonElement p)
	{
		string name = p.GetProperty("name").GetString()!;
		string description = p.TryGetProperty("description", out JsonElement d) ? d.GetString() ?? "" : "";
		return new(name, description);
	}

	private static Schema? ParseSchema(string json)
	{
		try
		{
			using JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			List<PropertyDef> required = [];
			List<PropertyDef> optional = [];

			if (root.TryGetProperty("properties", out JsonElement propsEl))
			{
				if (propsEl.TryGetProperty("required", out JsonElement reqEl))
					foreach (JsonElement p in reqEl.EnumerateArray())
						required.Add(ParseProperty(p));
				if (propsEl.TryGetProperty("optional", out JsonElement optEl))
					foreach (JsonElement p in optEl.EnumerateArray())
						optional.Add(ParseProperty(p));
			}

			List<ItemDef> items = [];
			if (root.TryGetProperty("items", out JsonElement itemsEl))
			{
				foreach (JsonProperty item in itemsEl.EnumerateObject())
				{
					string desc = item.Value.TryGetProperty("description", out JsonElement descEl) ? descEl.GetString() ?? "" : "";
					bool isRequired = item.Value.TryGetProperty("required", out JsonElement reqEl) && reqEl.GetBoolean();
					List<string> metadata = [];
					if (item.Value.TryGetProperty("metadata", out JsonElement metaEl))
						foreach (JsonElement m in metaEl.EnumerateArray())
							metadata.Add(m.GetString()!);
					items.Add(new(item.Name, desc, isRequired, metadata));
				}
			}

			CacheFormatDef? cacheFormat = null;
			if (root.TryGetProperty("cacheFormat", out JsonElement cacheFormatEl))
			{
				List<SectionDef> sections = [];
				if (cacheFormatEl.TryGetProperty("sections", out JsonElement sectionsEl))
				{
					foreach (JsonElement section in sectionsEl.EnumerateArray())
					{
						sections.Add(new(
							section.GetProperty("name").GetString()!,
							section.TryGetProperty("description", out JsonElement secDescEl) ? secDescEl.GetString() ?? "" : "",
							section.TryGetProperty("itemType", out JsonElement secItemEl) ? secItemEl.GetString() : null));
					}
				}

				cacheFormat = new(
					VersionHeader: GetStringOrEmpty(cacheFormatEl, "versionHeader"),
					HashHeaderPrefix: GetStringOrEmpty(cacheFormatEl, "hashHeaderPrefix"),
					SliceSeparator: GetStringOrEmpty(cacheFormatEl, "sliceSeparator"),
					SectionOpen: GetStringOrEmpty(cacheFormatEl, "sectionOpen"),
					SectionClose: GetStringOrEmpty(cacheFormatEl, "sectionClose"),
					CommentChar: GetStringOrEmpty(cacheFormatEl, "commentChar"),
					ProjectHeaderPrefix: GetStringOrEmpty(cacheFormatEl, "projectHeaderPrefix"),
					LanguagePrefix: GetStringOrEmpty(cacheFormatEl, "languagePrefix"),
					PrimaryMarker: GetStringOrEmpty(cacheFormatEl, "primaryMarker"),
					LastDtbSucceededMarker: GetStringOrEmpty(cacheFormatEl, "lastDtbSucceededMarker"),
					Sections: sections);
			}

			PathSentinelsDef? pathSentinels = null;
			if (root.TryGetProperty("pathSentinels", out JsonElement sentinelsEl))
			{
				List<(string Name, string Value)> entries = [];
				foreach (JsonProperty entry in sentinelsEl.EnumerateObject())
				{
					if (entry.Name.StartsWith("$", StringComparison.Ordinal))
						continue;
					if (entry.Value.ValueKind != JsonValueKind.String)
						continue;
					entries.Add((entry.Name, entry.Value.GetString()!));
				}
				pathSentinels = new(entries);
			}

			return new(required, optional, items,
				cacheFormat ?? throw new InvalidOperationException("Schema is missing required 'cacheFormat' block."),
				pathSentinels ?? throw new InvalidOperationException("Schema is missing required 'pathSentinels' block."));
		}
		catch
		{
			return null;
		}
	}

	private static string GenerateProjectProperties(Schema schema, string ns)
	{
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Generated from project-data-schema.json by DataModelSchemaGenerator.");
		sb.AppendLine();
		sb.AppendLine($"namespace {ns};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>Strongly-typed constants for all Data Model property names.</summary>");
		sb.AppendLine("internal static class ProjectProperties");
		sb.AppendLine("{");

		foreach (PropertyDef p in schema.Required)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(p.Description)}</summary>");
			sb.AppendLine($"\tpublic const string {p.Name} = \"{p.Name}\";");
		}
		sb.AppendLine();
		foreach (PropertyDef p in schema.Optional)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(p.Description)}</summary>");
			sb.AppendLine($"\tpublic const string {p.Name} = \"{p.Name}\";");
		}

		sb.AppendLine();
		sb.Append("\tpublic static readonly string[] Required = [");
		sb.Append(string.Join(", ", schema.Required.Select(p => $"\"{p.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		sb.AppendLine("\t/// <summary>");
		sb.AppendLine("\t/// All Data Model property names. Every property is exported into the cache file's");
		sb.AppendLine("\t/// <c>[properties]</c> section by the writer (the generated <c>_ProjectDataProperties</c>");
		sb.AppendLine("\t/// MSBuild allow-list is the same set). This is also the forward-compatibility");
		sb.AppendLine("\t/// \"known [properties] key\" set: a key NOT listed here is treated as data authored by a");
		sb.AppendLine("\t/// newer writer and preserved losslessly rather than dropped.");
		sb.AppendLine("\t/// </summary>");
		sb.Append("\tpublic static readonly string[] All = [");
		sb.Append(string.Join(", ", schema.Required.Concat(schema.Optional).Select(p => $"\"{p.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string GenerateProjectItems(Schema schema, string ns)
	{
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Generated from project-data-schema.json by DataModelSchemaGenerator.");
		sb.AppendLine();
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine();
		sb.AppendLine($"namespace {ns};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>Strongly-typed constants for Data Model item types and metadata.</summary>");
		sb.AppendLine("internal static class ProjectItems");
		sb.AppendLine("{");

		foreach (ItemDef item in schema.Items)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(item.Description)}</summary>");
			if (item.Metadata.Count == 0)
				sb.AppendLine($"\tpublic const string {item.Name} = \"{item.Name}\";");
			else
			{
				sb.AppendLine($"\tpublic static class {item.Name}");
				sb.AppendLine("\t{");
				sb.AppendLine($"\t\tpublic const string ItemType = \"{item.Name}\";");
				foreach (string meta in item.Metadata)
					sb.AppendLine($"\t\tpublic const string {char.ToUpperInvariant(meta[0]) + meta.Substring(1)} = \"{meta}\";");
				sb.AppendLine("\t}");
			}
			sb.AppendLine();
		}

		sb.Append("\tpublic static readonly string[] AllItemTypes = [");
		sb.Append(string.Join(", ", schema.Items.Select(i => $"\"{i.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		sb.AppendLine("\t/// <summary>Item types that must be present (even if empty) in every valid snapshot.</summary>");
		sb.Append("\tpublic static readonly string[] RequiredItemTypes = [");
		sb.Append(string.Join(", ", schema.Items.Where(i => i.IsRequired).Select(i => $"\"{i.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		sb.AppendLine("\t/// <summary>");
		sb.AppendLine("\t/// Every metadata key the writer can emit on any item (the union across all item");
		sb.AppendLine("\t/// types). Used by forward-compatibility preservation to tell a known <c>@metadata</c>");
		sb.AppendLine("\t/// line from one written by a newer version that must be carried through losslessly.");
		sb.AppendLine("\t/// </summary>");
		sb.Append("\tpublic static readonly string[] AllMetadata = [");
		sb.Append(string.Join(", ", schema.Items
			.SelectMany(i => i.Metadata)
			.Distinct(StringComparer.Ordinal)
			.Select(m => $"\"{m}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		sb.AppendLine("\t/// <summary>");
		sb.AppendLine("\t/// Metadata keys the writer can emit, grouped by the item type that carries them (NOT a");
		sb.AppendLine("\t/// flattened union). Forward-compatibility preservation uses this to decide what is");
		sb.AppendLine("\t/// \"known\" <em>per item type</em>, so a newer version that reuses an existing metadata");
		sb.AppendLine("\t/// name on a different item type is still treated as unknown and carried through losslessly.");
		sb.AppendLine("\t/// </summary>");
		sb.AppendLine("\tpublic static readonly Dictionary<string, string[]> MetadataByItemType = new(System.StringComparer.Ordinal)");
		sb.AppendLine("\t{");
		foreach (ItemDef item in schema.Items.Where(i => i.Metadata.Count > 0))
		{
			string metas = string.Join(", ", item.Metadata.Select(m => $"\"{m}\""));
			sb.AppendLine($"\t\t[\"{item.Name}\"] = [{metas}],");
		}

		sb.AppendLine("\t};");

		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string GenerateTypedProperties(Schema schema, string ns)
	{
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine("// Generated from project-data-schema.json by DataModelSchemaGenerator.");
		sb.AppendLine();
		sb.AppendLine("using System;");
		sb.AppendLine();
		sb.AppendLine($"namespace {ns};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>Strongly-typed accessor over <see cref=\"KeyValueCollection\"/>.</summary>");
		sb.AppendLine("internal readonly ref struct TypedProperties");
		sb.AppendLine("{");
		sb.AppendLine("\tprivate readonly KeyValueCollection inner;");
		sb.AppendLine("\tpublic TypedProperties(KeyValueCollection inner) => this.inner = inner;");
		sb.AppendLine();

		foreach (PropertyDef p in schema.Required)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(p.Description)}</summary>");
			sb.AppendLine($"\tpublic string {p.Name} => this.inner[ProjectProperties.{p.Name}] ?? throw new InvalidOperationException(\"Required Data Model property '\" + ProjectProperties.{p.Name} + \"' is missing. The .lscache file may be incomplete or the property merge failed.\");");
		}
		sb.AppendLine();
		foreach (PropertyDef p in schema.Optional)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(p.Description)}</summary>");
			sb.AppendLine($"\tpublic string? {p.Name} => this.inner[ProjectProperties.{p.Name}];");
		}

		sb.AppendLine("}");
		sb.AppendLine();
		sb.AppendLine("internal static class TypedPropertiesExtensions");
		sb.AppendLine("{");
		sb.AppendLine("\tpublic static TypedProperties Typed(this ProjectDataSnapshot snapshot) => new(snapshot.Properties);");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string EscapeXml(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

	private static string GetStringOrEmpty(JsonElement el, string name)
		=> el.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

	private static string EscapeForCSharpString(string value)
		=> value.Replace("\\", "\\\\").Replace("\"", "\\\"");

	private static string PascalCase(string name)
	{
		if (string.IsNullOrEmpty(name))
			return name;
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static string GenerateCacheFormat(Schema schema, string ns)
	{
		CacheFormatDef cacheFormat = schema.CacheFormat;
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Generated from project-data-schema.json by DataModelSchemaGenerator.");
		sb.AppendLine();
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine();
		sb.AppendLine($"namespace {ns};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Wire-format tokens for the on-disk <c>.lscache</c> file. Shared by the writer");
		sb.AppendLine("/// (<c>Microsoft.NET.ProjectData.Tasks</c>) and the reader");
		sb.AppendLine("/// (<c>Microsoft.NET.ProjectData</c>) so renames cannot silently drop a section.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("internal static class CacheFormat");
		sb.AppendLine("{");
		sb.AppendLine($"\tpublic const string VersionHeader = \"{EscapeForCSharpString(cacheFormat.VersionHeader)}\";");
		sb.AppendLine($"\tpublic const string HashHeaderPrefix = \"{EscapeForCSharpString(cacheFormat.HashHeaderPrefix)}\";");
		sb.AppendLine($"\tpublic const string SliceSeparator = \"{EscapeForCSharpString(cacheFormat.SliceSeparator)}\";");
		sb.AppendLine($"\tpublic const string SectionOpen = \"{EscapeForCSharpString(cacheFormat.SectionOpen)}\";");
		sb.AppendLine($"\tpublic const string SectionClose = \"{EscapeForCSharpString(cacheFormat.SectionClose)}\";");
		if (cacheFormat.CommentChar.Length == 1)
			sb.AppendLine($"\tpublic const char CommentChar = '{EscapeForCSharpString(cacheFormat.CommentChar)}';");
		sb.AppendLine($"\tpublic const string ProjectHeaderPrefix = \"{EscapeForCSharpString(cacheFormat.ProjectHeaderPrefix)}\";");
		sb.AppendLine($"\tpublic const string LanguagePrefix = \"{EscapeForCSharpString(cacheFormat.LanguagePrefix)}\";");
		sb.AppendLine($"\tpublic const string PrimaryMarker = \"{EscapeForCSharpString(cacheFormat.PrimaryMarker)}\";");
		sb.AppendLine($"\tpublic const string LastDtbSucceededMarker = \"{EscapeForCSharpString(cacheFormat.LastDtbSucceededMarker)}\";");
		sb.AppendLine();
		sb.AppendLine("\t/// <summary>Section names emitted as <c>[name]</c> by the writer and matched without brackets by the reader.</summary>");
		sb.AppendLine("\tpublic static class Sections");
		sb.AppendLine("\t{");
		foreach (SectionDef section in cacheFormat.Sections)
		{
			if (!string.IsNullOrEmpty(section.Description))
				sb.AppendLine($"\t\t/// <summary>{EscapeXml(section.Description)}</summary>");
			sb.AppendLine($"\t\tpublic const string {PascalCase(section.Name)} = \"{EscapeForCSharpString(section.Name)}\";");
		}
		sb.AppendLine();
		sb.Append("\t\tpublic static readonly string[] All = [");
		sb.Append(string.Join(", ", cacheFormat.Sections.Select(s => $"\"{EscapeForCSharpString(s.Name)}\"")));
		sb.AppendLine("];");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// Item <c>@metadata</c> keys the writer can emit, keyed by the WIRE section that carries");
		sb.AppendLine("\t\t/// them (resolved through the section's <c>itemType</c> link in the schema). Used by");
		sb.AppendLine("\t\t/// forward-compatibility preservation to judge \"known\" metadata <em>per section</em>, so a");
		sb.AppendLine("\t\t/// newer writer that reuses an existing metadata name on a different item type is still");
		sb.AppendLine("\t\t/// preserved rather than mistaken for regenerable data. Only sections whose item type");
		sb.AppendLine("\t\t/// emits metadata appear here; a section absent from the map emits no metadata.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\tpublic static readonly Dictionary<string, string[]> MetadataBySection = new(System.StringComparer.Ordinal)");
		sb.AppendLine("\t\t{");
		foreach (SectionDef section in cacheFormat.Sections)
		{
			if (section.ItemType is null)
				continue;
			ItemDef? item = schema.Items.FirstOrDefault(i => i.Name == section.ItemType);
			if (item is null || item.Metadata.Count == 0)
				continue;
			string metas = string.Join(", ", item.Metadata.Select(m => $"\"{EscapeForCSharpString(m)}\""));
			sb.AppendLine($"\t\t\t[\"{EscapeForCSharpString(section.Name)}\"] = [{metas}],");
		}
		sb.AppendLine("\t\t};");
		sb.AppendLine("\t}");
		sb.AppendLine();
		sb.AppendLine("\t/// <summary>Returns <paramref name=\"name\"/> wrapped in section delimiters, e.g. <c>[sourceFiles]</c>.</summary>");
		sb.AppendLine("\tpublic static string SectionHeader(string name) => SectionOpen + name + SectionClose;");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string GeneratePathSentinels(PathSentinelsDef pathSentinels, string ns)
	{
		StringBuilder sb = new();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Generated from project-data-schema.json by DataModelSchemaGenerator.");
		sb.AppendLine();
		sb.AppendLine($"namespace {ns};");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Sentinel prefixes embedded in encoded paths in the <c>.lscache</c> file.");
		sb.AppendLine("/// The writer rewrites version-specific or location-specific paths to these sentinels");
		sb.AppendLine("/// so the cache is portable across machines and SDK upgrades; the reader resolves them");
		sb.AppendLine("/// via <c>CachePathResolver</c>.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("internal static class PathSentinels");
		sb.AppendLine("{");
		foreach ((string name, string value) in pathSentinels.Entries)
		{
			sb.AppendLine($"\tpublic const string {PascalCase(name)} = \"{EscapeForCSharpString(value)}\";");
		}
		sb.AppendLine("}");
		return sb.ToString();
	}

	private sealed record PropertyDef(string Name, string Description);
	private sealed record ItemDef(string Name, string Description, bool IsRequired, List<string> Metadata);
	private sealed record SectionDef(string Name, string Description, string? ItemType);
	private sealed record CacheFormatDef(
		string VersionHeader,
		string HashHeaderPrefix,
		string SliceSeparator,
		string SectionOpen,
		string SectionClose,
		string CommentChar,
		string ProjectHeaderPrefix,
		string LanguagePrefix,
		string PrimaryMarker,
		string LastDtbSucceededMarker,
		List<SectionDef> Sections);
	private sealed record PathSentinelsDef(List<(string Name, string Value)> Entries);
	private sealed record Schema(
		List<PropertyDef> Required,
		List<PropertyDef> Optional,
		List<ItemDef> Items,
		CacheFormatDef CacheFormat,
		PathSentinelsDef PathSentinels);
}
